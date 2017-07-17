#if UNITY_EDITOR || RUNTIME_CSG

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Reflection;

namespace Sabresaurus.SabreCSG
{
	/// <summary>
	/// Base class for CSG Model, can be used on its own for run-time deployed CSG
	/// </summary>
//	[ExecuteInEditMode]
	public class CSGModelBase : MonoBehaviour
	{
		public const string VERSION_STRING = "1.5.1";
		protected const string DEFAULT_FALLBACK_MATERIAL_PATH = "Materials/Default_Map";

		// Limit to how many vertices a Unity mesh can hold, before it must be split into a second mesh (just under 2^16)
		protected const int MESH_VERTEX_LIMIT = 65500; 

		[SerializeField,HideInInspector] 
		protected List<Brush> brushes = new List<Brush>(); // Store the sequence of brushes and their operation (e.g. add, subtract)

		[SerializeField,HideInInspector]
		protected List<Brush> builtBrushes = new List<Brush>();

		[SerializeField,HideInInspector]
		protected MaterialMeshDictionary materialMeshDictionary = new MaterialMeshDictionary();

		[SerializeField,HideInInspector]
		protected List<Mesh> collisionMeshDictionary = new List<Mesh>();

		// An additional hint to the builder to tell it rebuilding is required
		[SerializeField,HideInInspector]
		protected bool polygonsRemoved = false;

		[SerializeField,HideInInspector]
		protected CSGBuildSettings buildSettings = new CSGBuildSettings();

		[SerializeField,HideInInspector]
		protected CSGBuildSettings lastBuildSettings = new CSGBuildSettings();

		[NonSerialized]
		protected CSGBuildContext buildContextBehaviour;

		// A reference to a component which holds a lot of build time data that helps change built geometry on the fly
		// This is used by the surface tools heavily.
		[NonSerialized]
		protected CSGBuildContext.BuildContext buildContext;

		public CSGBuildContext.BuildContext BuildContext
		{
			get
			{
				if(buildContext == null)
				{
					SetUpBuildContext();
				}
				return buildContext;
			}
		}

		public BuildMetrics BuildMetrics
		{
			get
			{
				return BuildContext.buildMetrics;
			}
		}

		public int BrushCount 
		{
			get
			{
				int brushCount = 0;
				for (int i = 0; i < brushes.Count; i++) 
				{
					if(brushes[i] != null)
					{
						brushCount++;
					}
				}
				return brushCount;
			}
		}


		public PolygonEntry GetVisualPolygonEntry(int index)
		{
			int entryCount = BuildContext.VisualPolygonIndex.Length;

			if(entryCount == 0 || index >= entryCount || index < 0)
			{
				// Return null if no polygons have been built or the index is out of range
				return null;
			}
			else
			{
				return BuildContext.VisualPolygonIndex[index];
			}
		}

		public PolygonEntry GetCollisionPolygonEntry(int index)
		{
			int entryCount = BuildContext.CollisionPolygonIndex.Length;

			if(entryCount == 0 || index >= entryCount || index < 0)
			{
				// Return null if no polygons have been built or the index is out of range
				return null;
			}
			else
			{
				return BuildContext.CollisionPolygonIndex[index];
			}
		}

		public bool LastBuildHadTangents
		{
			get
			{
				return lastBuildSettings.GenerateTangents;
			}
		}

		/// <summary>
		/// Get the list of brushes the CSG Model knows about
		/// </summary>
		/// <returns>List of brushes.</returns>
		public List<Brush> GetBrushes()
		{
			return brushes;
		}

		void Awake()
		{
			SetUpBuildContext();	
		}

		void SetUpBuildContext()
		{
			// Get a reference to the build context (which holds post build helper data)
			buildContextBehaviour = this.AddOrGetComponent<CSGBuildContext>();
			buildContext = buildContextBehaviour.GetBuildContext();
		}

		protected virtual void Start()
		{
		}

		protected virtual void Update()
		{
			if(Application.isPlaying)
			{
				bool buildOccurred = CSGFactory.Tick();
				if(buildOccurred)
				{
					OnBuildComplete();
				}
			}
		}

		/// <summary>
		/// Builds the brushes into final meshes
		/// </summary>
		/// <param name="forceRebuild">If set to <c>true</c> all brushes will be built and cached data ignored, otherwise SabreCSG will only rebuild brushes it knows have changed</param>
		/// <param name="buildInBackground">If set to <c>true</c> the majority of the build will occur in a background thread</param>
		public virtual void Build (bool forceRebuild, bool buildInBackground)
		{
			// If any of the build settings have changed, force all brushes to rebuild
			if(!lastBuildSettings.IsBuilt || CSGBuildSettings.AreDifferent(buildSettings, lastBuildSettings))
			{
				forceRebuild = true;
			}

			// Make sure we have the most accurate list of brushes, ignoring inactive objects
			brushes = new List<Brush>(transform.GetComponentsInChildren<Brush>(false));

			// Let each brush know it's about to be built
			for (int i = 0; i < brushes.Count; i++)
			{
				brushes[i].PrepareToBuild(brushes, forceRebuild);
			}

			// Perform a check to make sure the default material is OK
			Material defaultMaterial = GetDefaultMaterial();

			if(defaultMaterial == null)
			{
				Debug.LogError("Default fallback material file is missing, try reimporting SabreCSG");
			}

			BuildStatus buildStatus = CSGFactory.Build(brushes, 
				buildSettings, 
				buildContext, 
				this.transform, 
				materialMeshDictionary, 
				collisionMeshDictionary,
				polygonsRemoved,
				forceRebuild,
				OnBuildProgressChanged,
				OnFinalizeVisualMesh,
				OnFinalizeCollisionMesh,
				buildInBackground);

			if(buildStatus == BuildStatus.Complete)
			{
				OnBuildComplete();
			}
		}

		public virtual void OnBuildComplete()
		{
			polygonsRemoved = false;

			// Mark the brushes that have been built (so we can differentiate later if new brushes are built or not)
			builtBrushes.Clear();
			builtBrushes.AddRange(brushes);

			// Copy the last build settings, so that we can make minor changes to built meshes that are consistent
			// with how they were built. E.g. maintaining tangents as appropriate
			lastBuildSettings = buildSettings.ShallowCopy();
			lastBuildSettings.IsBuilt = true; // Make it clear that the lastBuildSettings refers to a completed build

			// Fire any post process build events
			FirePostBuildEvents();
		}


		void FirePostBuildEvents()
		{
			Transform meshGroupTransform = GetMeshGroupTransform();

			// Inform all methods with the PostProcessCSGBuildAttribute that a build just finished
			Assembly[] allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (Assembly assembly in allAssemblies) 
			{
				if(assembly.FullName.StartsWith("Assembly-CSharp"))
				{
					Type[] types = assembly.GetTypes();

					for (int i = 0; i < types.Length; i++) 
					{
						MethodInfo[] methods = types[i].GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
						for (int j = 0; j < methods.Length; j++) 
						{
							if(Attribute.IsDefined(methods[j], typeof(PostProcessCSGBuildAttribute)))
							{
								methods[j].Invoke(null, new object[] { meshGroupTransform } );
							}
						}
					}
				}
			}

			// Inform all the scripts implementing IPostBuildListener on this model and inside it that a build finished
			IPostBuildListener[] postBuildListeners = this.transform.GetComponentsInChildren<IPostBuildListener>();

			for (int i = 0; i < postBuildListeners.Length; i++) 
			{
				postBuildListeners[i].OnBuildFinished(meshGroupTransform);
			}
		}

		/// <summary>
		/// Called to alert the CSG Model that a new brush has been created
		/// </summary>
		public bool TrackBrush(Brush brush)
		{
			// If we don't already know about the brush, add it
			if (!brushes.Contains(brush))
			{
				brushes.Add(brush);
				return true;
			}
			else
			{
				return false;
			}
		}

		public void OnBrushDisabled(PrimitiveBrush brush)
		{
			polygonsRemoved = true;
		}

		public virtual bool AreBrushesVisible
		{
			get
			{
				return false;
			}
		}

		public Polygon RaycastBuiltPolygons(Ray ray)
		{
			if(BuildContext.VisualPolygons != null)
			{
				float distance = 0;
				return GeometryHelper.RaycastPolygons(BuildContext.VisualPolygons, ray, out distance);
			}
			else
			{
				return null;
			}
		}

        public List<PolygonRaycastHit> RaycastBuiltPolygonsAll(Ray ray)
        {
            if (BuildContext.VisualPolygons != null)
            {                
                return GeometryHelper.RaycastPolygonsAll(BuildContext.VisualPolygons, ray);
            }
            else
            {
                return null;
            }
        }

        public Brush FindBrushFromPolygon(Polygon sourcePolygon)
		{
			// Find which brush contains the source polygon
			for (int i = 0; i < brushes.Count; i++) 
			{
				if(brushes[i] != null)
				{
					if(Array.IndexOf(brushes[i].GetPolygonIDs(), sourcePolygon.UniqueIndex) != -1)
					{
						return brushes[i];
					}
				}
			}

			// None found
			return null;
		}

		// Consider getting rid of this accessor!
		public List<Polygon> VisualPolygons
		{
			get
			{
				return BuildContext.VisualPolygons;
			}
		}

		public List<Polygon> GetAllSourcePolygons()
		{
			// Find the source polygon unique indexes of all the visual polygons
			List<Polygon> visualPolygons = BuildContext.VisualPolygons;
			List<int> visualPolygonIndexes = new List<int>();

			for (int i = 0; i < visualPolygons.Count; i++) 
			{
				if(!visualPolygonIndexes.Contains(visualPolygons[i].UniqueIndex))
				{
					visualPolygonIndexes.Add(visualPolygons[i].UniqueIndex);
				}
			}

			List<Polygon> sourcePolygons = new List<Polygon>(visualPolygonIndexes.Count);

			for (int i = 0; i < visualPolygonIndexes.Count; i++) 
			{
				Polygon sourcePolygon = GetSourcePolygon(visualPolygonIndexes[i]);
				sourcePolygons.Add(sourcePolygon);
			}
			return sourcePolygons;
		}

		public Polygon[] BuiltPolygonsByIndex(int uniquePolygonIndex)
		{
//			if(CurrentSettings.NewBuildEngine)
//			{
//				// TODO: Optimise this once Nova 1 is removed
//				List<Polygon> foundPolygons = new List<Polygon>();
//				for (int i = 0; i < brushes.Count; i++) 
//				{
//					if(brushes[i] != null)
//					{
//						List<Polygon> brushPolygons = brushes[i].BrushCache.BuiltPolygons;
//						for (int j = 0; j < brushPolygons.Count; j++) 
//						{
//							if(brushPolygons[j].UniqueIndex == uniquePolygonIndex)
//							{
//								foundPolygons.Add(brushPolygons[j]);
//							}
//						}
//					}
//				}
//				return foundPolygons.ToArray();
//			}
//			else
			{
				if(BuildContext == null || BuildContext.VisualPolygons == null)
				{
					return new Polygon[0];
				}

				List<Polygon> matchedPolygons = new List<Polygon>();

				// Match all the polygons with the same index that are built
				for (int i = 0; i < BuildContext.VisualPolygons.Count; i++) 
				{
					Polygon poly = BuildContext.VisualPolygons[i];

					if(poly.UniqueIndex == uniquePolygonIndex && !poly.ExcludeFromFinal)
					{
						matchedPolygons.Add(poly);
					}
				}

				return matchedPolygons.ToArray();
			}
		}

//		public Polygon[] BuiltCollisionPolygonsByIndex(int uniquePolygonIndex)
//		{
//			if(buildContext == null || buildContext.collisionPolygons == null)
//			{
//				return new Polygon[0];
//			}
//
//			return buildContext.collisionPolygons.Where(poly => (poly.UniqueIndex == uniquePolygonIndex && !poly.ExcludeFromFinal)).ToArray();
//		}

		public List<PolygonRaycastHit> RaycastBrushesAll(Ray ray, bool testAllModels)
		{
			List<PolygonRaycastHit> hits = new List<PolygonRaycastHit>();

			List<Brush> brushesToTest;

			if(testAllModels)
			{
				brushesToTest = new List<Brush>();
				CSGModelBase[] csgModels = FindObjectsOfType<CSGModelBase>();
				for (int i = 0; i < csgModels.Length; i++) 
				{
					brushesToTest.AddRange(csgModels[i].brushes);
				}
			}
			else
			{
				brushesToTest = brushes;
			}

			for (int i = 0; i < brushesToTest.Count; i++)
			{
				if(brushesToTest[i] == null)
				{
					continue;
				}
//				Bounds bounds = brushes[i].GetBoundsTransformed();
//				if(bounds.IntersectRay(ray))
				{
					Polygon[] polygons = brushesToTest[i].GenerateTransformedPolygons();
					float hitDistance;
					Polygon hitPolygon = GeometryHelper.RaycastPolygons(new List<Polygon>(polygons), ray, out hitDistance);
					if(hitPolygon != null)
					{
						hits.Add(new PolygonRaycastHit() 
							{ 
								Distance = hitDistance,
								Point = ray.GetPoint(hitDistance),
								Normal = hitPolygon.Plane.normal,
								GameObject = brushesToTest[i].gameObject,
								Polygon = hitPolygon,
							}
						);
					}
				}
			}

			hits.Sort((x,y) => x.Distance.CompareTo(y.Distance));
			return hits;
		}

        public List<BrushBase> ExtractBrushBases(List<Brush> sourceBrushes)
        {
            // Generate a list of editable brushes (in the case of compound brush's child brushes this would be the root controller)
            List<BrushBase> brushBases = new List<BrushBase>();
            for (int i = 0; i < sourceBrushes.Count; i++)
            {
                if(sourceBrushes[i].GetType() == typeof(PrimitiveBrush))
                {
                    // Get any controller (e.g. compound brush) that is driving the selected brush
                    BrushBase controller = ((PrimitiveBrush)sourceBrushes[i]).BrushController;
                    if (controller != null)
                    {
                        // Controller found, add it instead if it's not already in the list
                        if(!brushBases.Contains(controller))
                        {
                            brushBases.Add(controller);
                        }
                    }
                    else
                    {
                        // No controller found just add the brush
                        brushBases.Add(sourceBrushes[i]);
                    }
                }
                else
                {
                    // Not a primitive brush, so just add it
                    brushBases.Add(sourceBrushes[i]);
                }
            }
            return brushBases;
        }

		public bool HasBrushBeenBuilt(Brush candidateBrush)
		{
			return builtBrushes.Contains(candidateBrush);
		}

		/// <summary>
		/// Creates a brush under the CSG Model with the specified attributes.
		/// </summary>
		/// <returns>The created game object.</returns>
		/// <param name="brushType">Brush type.</param>
		/// <param name="localPosition">Local position of the brush's transform</param>
		/// <param name="localSize">Local bounds size of the brush (Optional, defaults to 2,2,2).</param>
		/// <param name="localRotation">Local rotation of the brush (Optional, defaults to identity quaternion).</param>
		/// <param name="material">Material to apply to all faces, (Optional, defaults to null for default material).</param>
		/// <param name="csgMode">Whether the brush is additive or subtractive (Optional, defaults to additive).</param>
		/// <param name="brushName">Name for the game object (Optional, defaults to "AppliedBrush").</param>
		public GameObject CreateBrush(PrimitiveBrushType brushType, Vector3 localPosition, Vector3 localSize = default(Vector3), Quaternion localRotation = default(Quaternion), Material material = null, CSGMode csgMode = CSGMode.Add, string brushName = null)
		{
			GameObject brushObject;
			if(!string.IsNullOrEmpty(brushName))
			{
				brushObject = new GameObject(brushName);
			}
			else
			{
				brushObject = new GameObject("AppliedBrush");
			}

			brushObject.transform.parent = this.transform;
			brushObject.transform.localPosition = localPosition;
			if(localRotation != default(Quaternion))
			{
				brushObject.transform.localRotation = localRotation;
			}
			PrimitiveBrush primitiveBrush = brushObject.AddComponent<PrimitiveBrush>();
			primitiveBrush.BrushType = brushType;
			primitiveBrush.Mode = csgMode;
			primitiveBrush.ResetPolygons();

			if(localSize != default(Vector3) 
				&& localSize != new Vector3(2,2,2))
			{
				BrushUtility.Resize(primitiveBrush, localSize);
			}
            else
            {
                // Resize automatically invalidates a brush with changed polygons set, if no resize took place we still need to make sure it happens
                primitiveBrush.Invalidate(true);
            }

            if (material != null)
			{
				SurfaceUtility.SetAllPolygonsMaterials(primitiveBrush, material);
			}

			return brushObject;
		}

		public GameObject CreateCompoundBrush<T>(Vector3 localPosition, Vector3 localSize = default(Vector3), Quaternion localRotation = default(Quaternion), Material material = null, CSGMode csgMode = CSGMode.Add, string brushName = null) where T : CompoundBrush
		{
			return CreateCompoundBrush(typeof(T), localPosition, localSize, localRotation, material, csgMode, brushName);
		}

		public GameObject CreateCompoundBrush(Type compoundBrushType, Vector3 localPosition, Vector3 localSize = default(Vector3), Quaternion localRotation = default(Quaternion), Material material = null, CSGMode csgMode = CSGMode.Add, string brushName = null)
		{
			// Make sure we're actually being asked to create a compound brush
			if(!typeof(CompoundBrush).IsAssignableFrom(compoundBrushType))
			{
				throw new ArgumentException("Specified type must be derived from CompoundBrush");
			}

			GameObject brushObject;
			if(!string.IsNullOrEmpty(brushName))
			{
				brushObject = new GameObject(brushName);
			}
			else
			{
				brushObject = new GameObject(compoundBrushType.Name);
			}

			brushObject.transform.parent = this.transform;
			brushObject.transform.localPosition = localPosition;
			if(localRotation != default(Quaternion))
			{
				brushObject.transform.localRotation = localRotation;
			}
			CompoundBrush compoundBrush = (CompoundBrush)brushObject.AddComponent(compoundBrushType);
			compoundBrush.Mode = csgMode;
			compoundBrush.Invalidate(true);
//			if(localSize != default(Vector3) 
//				&& localSize != new Vector3(2,2,2))
//			{
//				BrushUtility.Resize(compoundBrush, localSize);
//			}

			if(material != null)
			{
//				SurfaceUtility.SetAllPolygonsMaterials(compoundBrush, material);
			}

			return brushObject;
		}

		/// <summary>
		/// Create a brush at the origin using a specified set of polygons
		/// </summary>
		/// <returns>The custom brush game object.</returns>
		/// <param name="polygons">Polygons.</param>
		public GameObject CreateCustomBrush(Polygon[] polygons)
		{
			GameObject brushObject = new GameObject("AppliedBrush");
			brushObject.transform.parent = this.transform;
			PrimitiveBrush primitiveBrush = brushObject.AddComponent<PrimitiveBrush>();
			primitiveBrush.SetPolygons(polygons, true);

			return brushObject;
		}

		public Polygon GetSourcePolygon(int uniqueIndex)
		{
			for (int i = 0; i < brushes.Count; i++) 
			{
				if(brushes[i] != null)
				{
					Polygon[] polygons = brushes[i].GetPolygons();
					for (int j = 0; j < polygons.Length; j++) 
					{
						if(polygons[j].UniqueIndex == uniqueIndex)
						{
							return polygons[j];
						}
					}
				}
			}

			// None found
			return null;
		}

		public Transform GetMeshGroupTransform()
		{
			Transform meshGroup = transform.Find("MeshGroup");
			return meshGroup;
		}

		public void RefreshMeshGroup()
		{
			// For some reason mesh colliders don't update when you change the mesh, you have to flush them by
			// either setting the mesh null and resetting it, or turning the object off and on again
			Transform meshGroup = GetMeshGroupTransform();

			if(meshGroup != null && meshGroup.gameObject.activeInHierarchy)
			{
				meshGroup.gameObject.SetActive(false);
				meshGroup.gameObject.SetActive(true);
			}
		}

		public virtual void OnBuildProgressChanged(float progress)
		{
		}

		public virtual void OnFinalizeVisualMesh(GameObject newGameObject, Mesh mesh)
		{			
		}

		public virtual void OnFinalizeCollisionMesh(GameObject newGameObject, Mesh mesh)
		{			
		}

		public void NotifyPolygonsRemoved()
		{
			polygonsRemoved = true;
		}

		public Material GetDefaultMaterial()
		{
			// Make sure there is a default material set, if not use the fallback
			EnsureDefaultMaterialSet();

			// Return the active default material set for the Model
			return buildSettings.DefaultVisualMaterial;
		}

//		public void SetDefaultMaterial(Material newMaterial)
//		{
//			buildSettings.DefaultVisualMaterial = newMaterial;
//			// Make sure there is a material set, setting null with reset to default
//			EnsureDefaultMaterialSet();
//		}

		public void EnsureDefaultMaterialSet()
		{
			// Make sure there is a default material set, if not use the fallback
			if(buildSettings.DefaultVisualMaterial == null)
			{
				buildSettings.DefaultVisualMaterial = GetDefaultFallbackMaterial();
			}
		}

		public virtual Material GetDefaultFallbackMaterial()
		{
			return Resources.Load(DEFAULT_FALLBACK_MATERIAL_PATH) as Material;
		}

		public class RayHitComparer : IComparer
		{
			public int Compare(object x, object y)
			{
				return ((RaycastHit) x).distance.CompareTo(((RaycastHit) y).distance);
			}
		}
	}
}
#endif