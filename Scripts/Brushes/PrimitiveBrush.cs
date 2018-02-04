#if UNITY_EDITOR || RUNTIME_CSG
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
    public enum PrimitiveBrushType { 
		Cube, 
		Sphere, 
		Cylinder, 
		Prism, 
		Custom,
		IcoSphere,
        Cone,
	};

	/// <summary>
	/// A simple brush that represents a single convex shape
	/// </summary>
    [ExecuteInEditMode]
    public class PrimitiveBrush : Brush
    {
        [SerializeField]
		Polygon[] polygons;

		[SerializeField,HideInInspector]
		int prismSideCount = 6;

		[SerializeField,HideInInspector]
		int cylinderSideCount = 20;

        [SerializeField, HideInInspector]
        int coneSideCount = 20;

        [SerializeField,HideInInspector]
		int sphereSideCount = 6;

		[SerializeField,HideInInspector]
		int icoSphereIterationCount = 1;

        [SerializeField,HideInInspector]
		PrimitiveBrushType brushType = PrimitiveBrushType.Cube;

		[SerializeField,HideInInspector]
		bool tracked = false;

		[SerializeField,HideInInspector]
		BrushOrder cachedBrushOrder = null;

		[SerializeField]
		BrushBase brushController = null;

		int cachedInstanceID = 0;

		private CSGModelBase parentCsgModel;

		[SerializeField]
		WorldTransformData cachedWorldTransform;

		[SerializeField]
		int objectVersionSerialized;

		int objectVersionUnserialized;

		public PrimitiveBrushType BrushType {
			get {
				return brushType;
			}
			set {
				brushType = value;
			}
		}

        public BrushBase BrushController
        {
            get
            {
                // If this brush is being controlled by something else, return the controller
                return brushController;
            }
        }

        public bool IsReadOnly
		{
			get
			{
				// If this brush is being controlled by something else, it's read only
				return (brushController != null);
			}
		}

		public void SetBrushController(BrushBase brushController)
		{
			this.brushController = brushController;
		}

		/// <summary>
		/// Provide new polygons for the brush
		/// </summary>
		/// <param name="polygons">New polygons.</param>
		/// <param name="breakTypeRelation">If the brush type has changed set this to <c>true</c>. For example if you change a cuboid into a wedge, the brush type should no longer be Cube. See BreakTypeRelation() for more details</param>
		public void SetPolygons(Polygon[] polygons, bool breakTypeRelation = true)
        {
            this.polygons = polygons;

            Invalidate(true);

			if(breakTypeRelation)
			{
				BreakTypeRelation();
			}
        }

		/// <summary>
		/// Brushes retain knowledge of what they were made from, so it's easy to adjust the side count on a prism for example, while retaining some of its transform information. If you start cutting away at a prism using the clip tool for instance, it should stop tracking it as following the initial form. This method allows you to tell the brush it is no longer tracking a base form.
		/// </summary>
		public void BreakTypeRelation()
		{
			brushType = PrimitiveBrushType.Custom;
		}

#if UNITY_EDITOR
		[UnityEditor.Callbacks.DidReloadScripts]
		static void OnReloadedScripts()
		{
			PrimitiveBrush[] brushes = FindObjectsOfType<PrimitiveBrush>();

			for (int i = 0; i < brushes.Length; i++) 
			{
				brushes[i].UpdateVisibility();
			}
		}
#endif

        void Start()
        {
			cachedWorldTransform = new WorldTransformData(transform);
			EnsureWellFormed();

			Invalidate(false);

			if(brushCache == null || brushCache.Polygons == null || brushCache.Polygons.Length == 0)
			{
				RecachePolygons(true);
			}

#if UNITY_EDITOR
#if UNITY_5_5_OR_NEWER
            // Unity 5.5 introduces a second possible selection highlight state, so the hiding API has changed
            UnityEditor.EditorUtility.SetSelectedRenderState(GetComponent<Renderer>(), UnityEditor.EditorSelectedRenderState.Hidden);
#else
            // Pre Unity 5.5 the only selection highlight was a wireframe
            UnityEditor.EditorUtility.SetSelectedWireframeHidden(GetComponent<Renderer>(), true);
#endif
#endif

			objectVersionUnserialized = objectVersionSerialized;
        }

		/// <summary>
		/// Reset the polygons to those specified in the brush type. For example if the brush type is a cube, the polygons are reset to a cube.
		/// </summary>
		public void ResetPolygons()
		{
			if (brushType == PrimitiveBrushType.Cube)
			{
				polygons = BrushFactory.GenerateCube();
			}
			else if (brushType == PrimitiveBrushType.Cylinder)
			{
				if(cylinderSideCount < 3)
				{
					cylinderSideCount = 3;
				}
				polygons = BrushFactory.GenerateCylinder(cylinderSideCount);
			}
			else if (brushType == PrimitiveBrushType.Sphere)
			{
				if(sphereSideCount < 3)
				{
					sphereSideCount = 3;
				}
				// Lateral only goes halfway around the sphere (180 deg), longitudinal goes all the way (360 deg)
				polygons = BrushFactory.GeneratePolarSphere(sphereSideCount, sphereSideCount * 2);
			}
			else if (brushType == PrimitiveBrushType.IcoSphere)
			{
				if(icoSphereIterationCount < 0)
				{
					icoSphereIterationCount = 0;
				}
				else if(icoSphereIterationCount > 2)
				{
					icoSphereIterationCount = 2;
				}

				polygons = BrushFactory.GenerateIcoSphere(icoSphereIterationCount);
			}
			else if (brushType == PrimitiveBrushType.Prism)
			{
				if(prismSideCount < 3)
				{
					prismSideCount = 3;
				}
				polygons = BrushFactory.GeneratePrism(prismSideCount);
			}
            else if (brushType == PrimitiveBrushType.Cone)
            {
                if (coneSideCount < 3)
                {
                    coneSideCount = 3;
                }
                polygons = BrushFactory.GenerateCone(coneSideCount);
            }
            else if(brushType == Sabresaurus.SabreCSG.PrimitiveBrushType.Custom)
			{
				// Do nothing
				Debug.LogError("PrimitiveBrushType.Custom is not a valid type for new brush creation");
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		void DrawPolygons(Color color, params Polygon[] polygons)
		{
			GL.Begin(GL.TRIANGLES);
			color.a = 0.7f;
			GL.Color(color);
			
			for (int j = 0; j < polygons.Length; j++) 
			{
				Polygon polygon = polygons[j];
				Vector3 position1 = polygon.Vertices[0].Position;
				
				for (int i = 1; i < polygon.Vertices.Length - 1; i++)
				{
					GL.Vertex(transform.TransformPoint(position1));
					GL.Vertex(transform.TransformPoint(polygon.Vertices[i].Position));
					GL.Vertex(transform.TransformPoint(polygon.Vertices[i + 1].Position));
				}
			}
			GL.End();
		}

#if UNITY_EDITOR
        public void OnRepaint(UnityEditor.SceneView sceneView, Event e)
        {
            // Selected brush green outline
			if(!isBrushConvex)
			{
				SabreCSGResources.GetSelectedBrushMaterial().SetPass(0);
				DrawPolygons(Color.red, polygons);
			}
        }
#endif

		public override Polygon[] GenerateTransformedPolygons()
		{
			Polygon[] polygonsCopy = polygons.DeepCopy<Polygon>();

			Vector3 center = transform.position;
			Quaternion rotation = transform.rotation;
			Vector3 scale = transform.localScale;

			for (int i = 0; i < polygons.Length; i++)
			{
				for (int j = 0; j < polygons[i].Vertices.Length; j++)
				{
					polygonsCopy[i].Vertices[j].Position = rotation * polygonsCopy[i].Vertices[j].Position.Multiply(scale) + center;
					polygonsCopy[i].Vertices[j].Normal = rotation * polygonsCopy[i].Vertices[j].Normal;
				}

				// Just updated a load of vertex positions, so make sure the cached plane is updated
				polygonsCopy[i].CalculatePlane();
			}

			return polygonsCopy;
		}

		public override void RecalculateBrushCache ()
		{
			RecachePolygons(true);

			RecalculateIntersections();
		}

		public override void RecachePolygons(bool markUnbuilt)
		{
			if(brushCache == null)
			{
				brushCache = new BrushCache();
			}
			Polygon[] cachedTransformedPolygons = GenerateTransformedPolygons();
			Bounds cachedTransformedBounds = GetBoundsTransformed();
			brushCache.Set(mode, cachedTransformedPolygons, cachedTransformedBounds, markUnbuilt);
		}

		public override void RecalculateIntersections()
		{
			CSGModelBase csgModel = GetCSGModel();
			if(csgModel != null)
			{
				List<Brush> brushes = GetCSGModel().GetBrushes();

				// Tracked brushes at edit time can be added in any order, so sort them
				IComparer<Brush> comparer = new BrushOrderComparer();
				for (int i = 0; i < brushes.Count; i++) 
				{
					if(brushes[i] == null)
					{
						brushes.RemoveAt(i);
						i--;
					}
				}

				for (int i = 0; i < brushes.Count; i++) 
				{
					brushes[i].UpdateCachedBrushOrder();
				}

				brushes.Sort(comparer);

				RecalculateIntersections(brushes, true);
			}
		}

		public override void RecalculateIntersections(List<Brush> brushes, bool isRootChange)
		{
			List<Brush> previousVisualIntersections = brushCache.IntersectingVisualBrushes;
			List<Brush> previousCollisionIntersections = brushCache.IntersectingCollisionBrushes;

			List<Brush> intersectingVisualBrushes = CalculateIntersectingBrushes(this, brushes, false);
			List<Brush> intersectingCollisionBrushes = CalculateIntersectingBrushes(this, brushes, true);

			brushCache.SetIntersection(intersectingVisualBrushes, intersectingCollisionBrushes);

			if(isRootChange)
			{
				// Brushes that are either newly intersecting or no longer intersecting, they need to recalculate their
				// intersections, but also rebuild
				List<Brush> brushesToRecalcAndRebuild = new List<Brush>();

				// Brushes that are still intersecting, these should recalculate their intersections any way in case 
				// sibling order has changed to make sure their intersection order is still correct
				List<Brush> brushesToRecalculateOnly = new List<Brush>();

				// Brushes that are either new or existing intersections
				for (int i = 0; i < intersectingVisualBrushes.Count; i++) 
				{
					if(intersectingVisualBrushes[i] != null)
					{
						if(!previousVisualIntersections.Contains(intersectingVisualBrushes[i]))
						{
							// It's a newly intersecting brush
							if(!brushesToRecalcAndRebuild.Contains(intersectingVisualBrushes[i]))
							{
								brushesToRecalcAndRebuild.Add(intersectingVisualBrushes[i]);
							}
						}
						else
						{
							// Intersection was already present
							if(!brushesToRecalculateOnly.Contains(intersectingVisualBrushes[i]))
							{
								brushesToRecalculateOnly.Add(intersectingVisualBrushes[i]);
							}
						}
					}
				}

				// Find any brushes that no longer intersect
				for (int i = 0; i < previousVisualIntersections.Count; i++) 
				{
					if(previousVisualIntersections[i] != null && !intersectingVisualBrushes.Contains(previousVisualIntersections[i]))
					{
						if(!brushesToRecalcAndRebuild.Contains(previousVisualIntersections[i]))
						{
							brushesToRecalcAndRebuild.Add(previousVisualIntersections[i]);
						}
					}
				}

				// Collision Pass

				// Brushes that are either new or existing intersections
				for (int i = 0; i < intersectingCollisionBrushes.Count; i++) 
				{
					if(intersectingCollisionBrushes[i] != null)
					{
						if(!previousCollisionIntersections.Contains(intersectingCollisionBrushes[i]))
						{
							// It's a newly intersecting brush
							if(!brushesToRecalcAndRebuild.Contains(intersectingCollisionBrushes[i]))
							{
								brushesToRecalcAndRebuild.Add(intersectingCollisionBrushes[i]);
							}
						}
						else
						{
							// Intersection was already present
							if(!brushesToRecalculateOnly.Contains(intersectingCollisionBrushes[i]))
							{
								brushesToRecalculateOnly.Add(intersectingCollisionBrushes[i]);
							}
						}
					}
				}

				// Find any brushes that no longer intersect
				for (int i = 0; i < previousCollisionIntersections.Count; i++) 
				{
					if(previousCollisionIntersections[i] != null && !intersectingCollisionBrushes.Contains(previousCollisionIntersections[i]))
					{
						if(!brushesToRecalcAndRebuild.Contains(previousCollisionIntersections[i]))
						{
							brushesToRecalcAndRebuild.Add(previousCollisionIntersections[i]);
						}
					}
				}

				// Notify brushes that are either newly intersecting or no longer intersecting that they need to recalculate and rebuild
				for (int i = 0; i < brushesToRecalcAndRebuild.Count; i++) 
				{
					// Brush intersection has changed
					brushesToRecalcAndRebuild[i].RecalculateIntersections(brushes, false);
					// Brush needs to be built
					brushesToRecalcAndRebuild[i].BrushCache.SetUnbuilt();
				}

				// Brushes that remain intersecting should recalc their intersection lists just in case sibling order has changed
				for (int i = 0; i < brushesToRecalculateOnly.Count; i++) 
				{
					// Brush intersection has changed
					brushesToRecalculateOnly[i].RecalculateIntersections(brushes, false);
				}
			}
		}


		// Fired by the CSG Model
        public override void OnUndoRedoPerformed()
        {			
			if(objectVersionSerialized != objectVersionUnserialized)
			{
	            Invalidate(true);
			}
        }

        void EnsureWellFormed()
        {
            if (polygons == null || polygons.Length == 0)
            {
				// Reset custom brushes back to a cube
				if(brushType == PrimitiveBrushType.Custom)
				{
					brushType = PrimitiveBrushType.Cube;
				}

				ResetPolygons();
            }
        }
			
//        public void OnDrawGizmosSelected()
//        {
//            // Ensure Edit Mode is on
//            GetCSGModel().EditMode = true;
//        }
//
//        public void OnDrawGizmos()
//        {
//            EnsureWellFormed();
//
//            //			Gizmos.color = Color.green;
//            //			for (int i = 0; i < PolygonFactory.hackyDisplay1.Count; i++) 
//            //			{
//            //				Gizmos.DrawSphere(PolygonFactory.hackyDisplay1[i], 0.2f);
//            //			}
//            //
//            //			Gizmos.color = Color.red;
//            //			for (int i = 0; i < PolygonFactory.hackyDisplay2.Count; i++) 
//            //			{
//            //				Gizmos.DrawSphere(PolygonFactory.hackyDisplay2[i], 0.2f);
//            //			}
//        }


		void OnDisable()
		{
			// OnDisable is called on recompilation, so make sure we only process when needed
			if(this.enabled == false || (gameObject.activeInHierarchy == false && transform.root.gameObject.activeInHierarchy == true))
			{
				GetCSGModel().OnBrushDisabled(this);
				// Copy the intersections list since the source list will change as we call recalculate on other brushes
				List<Brush> intersectingVisualBrushes = new List<Brush>(brushCache.IntersectingVisualBrushes);

				for (int i = 0; i < intersectingVisualBrushes.Count; i++) 
				{
					if(intersectingVisualBrushes[i] != null)
					{
						intersectingVisualBrushes[i].RecalculateIntersections();
						intersectingVisualBrushes[i].BrushCache.SetUnbuilt();
					}
				}
			}
		}

		void UpdateTracking()
		{
			CSGModelBase parentCSGModel = GetCSGModel();

			// Make sure the CSG Model knows about this brush. If they duplicated a brush in the hierarchy then this
			// allows us to make sure the CSG Model knows about it
			if(parentCSGModel != null)
			{
				bool newBrush = parentCSGModel.TrackBrush(this);

				if(newBrush)
				{
					MeshFilter meshFilter = gameObject.AddOrGetComponent<MeshFilter>();

					meshFilter.sharedMesh = new Mesh();
					brushCache = new BrushCache();
					EnsureWellFormed();
					RecalculateBrushCache();
				}
				Invalidate(false);
				tracked = true;
			}
			else
			{
				tracked = false;
			}
		}

		void OnEnable()
		{
			UpdateTracking();
		}

		void Update()
		{
			if(!tracked)
			{
				UpdateTracking();
			}

			// If the transform has changed, needs rebuild
			if(cachedWorldTransform.SetFromTransform(transform))
			{
				Invalidate(true);
			}
		}

		/// <summary>
		/// Tells the brush it has changed
		/// </summary>
		/// <param name="polygonsChanged">If set to <c>true</c> polygons will be recached.</param>
        public override void Invalidate(bool polygonsChanged)
        {
			base.Invalidate(polygonsChanged);
			if(!gameObject.activeInHierarchy)
			{
				return;
			}

			// Make sure there is a mesh filter on this object
			MeshFilter meshFilter = gameObject.AddOrGetComponent<MeshFilter>();
			MeshRenderer meshRenderer = gameObject.AddOrGetComponent<MeshRenderer>();

			// Used to use mesh colliders for ray collision, but not any more so clean them up
			MeshCollider[] meshColliders = GetComponents<MeshCollider>();

			if(meshColliders.Length > 0)
			{
				for (int i = 0; i < meshColliders.Length; i++) 
				{
					DestroyImmediate(meshColliders[i]);
				}
			} 

			bool requireRegen = false;

			// If the cached ID hasn't been set or we mismatch
			if(cachedInstanceID == 0
				|| gameObject.GetInstanceID() != cachedInstanceID)
			{
				requireRegen = true;
				cachedInstanceID = gameObject.GetInstanceID();
			}

			Mesh renderMesh = meshFilter.sharedMesh;

			if(requireRegen)
			{
				renderMesh = new Mesh();
			}

			if(polygons != null)
			{
				List<int> polygonIndices;
				BrushFactory.GenerateMeshFromPolygons(polygons, ref renderMesh, out polygonIndices);
			}

			if(mode == CSGMode.Subtract)
			{
				MeshHelper.Invert(ref renderMesh);
			}
			// Displace the triangles for display along the normals very slightly (this is so we can overlay built
			// geometry with semi-transparent geometry and avoid depth fighting)
			MeshHelper.Displace(ref renderMesh, 0.001f);

			meshFilter.sharedMesh = renderMesh;
				
			meshRenderer.receiveShadows = false;
			meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

			meshFilter.hideFlags = HideFlags.NotEditable;// | HideFlags.HideInInspector;
			meshRenderer.hideFlags = HideFlags.NotEditable;// | HideFlags.HideInInspector;

#if UNITY_EDITOR
			Material material;
			if(IsNoCSG)
			{
				material = SabreCSGResources.GetNoCSGMaterial();
			}
			else
			{
				if(this.mode == CSGMode.Add)
				{
					material = SabreCSGResources.GetAddMaterial();
				}
				else
				{
					material = SabreCSGResources.GetSubtractMaterial();
				}
			}
			if(meshRenderer.sharedMaterial != material)
			{
				meshRenderer.sharedMaterial = material;
			}
#endif
//			isBrushConvex = GeometryHelper.IsBrushConvex(polygons);

			if(polygonsChanged)
			{
				RecalculateBrushCache();
			}

			UpdateVisibility();

			objectVersionSerialized++;
			objectVersionUnserialized = objectVersionSerialized;

			if(cachedWorldTransform == null)
			{
				cachedWorldTransform = new WorldTransformData(transform);
			}
			cachedWorldTransform.SetFromTransform(transform);
        }

		public override void UpdateVisibility()
        {
			// Display brush if the CSG Model says to or if the brush isn't under a CSG Model
			CSGModelBase csgModel = GetCSGModel();
			bool isVisible = false;
			if(csgModel == null || csgModel.AreBrushesVisible)
			{
				isVisible = true;
			}
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.enabled = isVisible;
            }
        }

//		public Polygon GetPolygonFromTriangle(int triangleIndex)
//        {
//            int polygonIndex = polygonIndices[triangleIndex];
//            return polygons[polygonIndex];
//        }

        public override Bounds GetBounds()
        {
			if (polygons.Length > 0)
			{
				Bounds bounds = new Bounds(polygons[0].Vertices[0].Position, Vector3.zero);
				
				for (int i = 0; i < polygons.Length; i++)
				{
					for (int j = 0; j < polygons[i].Vertices.Length; j++)
					{
						bounds.Encapsulate(polygons[i].Vertices[j].Position);
					}
				}
				return bounds;
			}
			else
			{
				return new Bounds(Vector3.zero, Vector3.zero);
			}
        }

		public override void SetBounds (Bounds newBounds)
		{
			throw new NotImplementedException ();
		}

		public override Bounds GetBoundsTransformed()
		{
			if (polygons.Length > 0)
			{
				Bounds bounds = new Bounds(transform.TransformPoint(polygons[0].Vertices[0].Position), Vector3.zero);

				for (int i = 0; i < polygons.Length; i++)
				{
					for (int j = 0; j < polygons[i].Vertices.Length; j++)
					{
						bounds.Encapsulate(transform.TransformPoint(polygons[i].Vertices[j].Position));
					}
				}
				return bounds;
			}
			else
			{
				return new Bounds(Vector3.zero, Vector3.zero);
			}
		}

        public override Bounds GetBoundsLocalTo(Transform otherTransform)
        {
            if (polygons.Length > 0)
            {
                Bounds bounds = new Bounds(otherTransform.InverseTransformPoint(transform.TransformPoint(polygons[0].Vertices[0].Position)), Vector3.zero);

                for (int i = 0; i < polygons.Length; i++)
                {
                    for (int j = 0; j < polygons[i].Vertices.Length; j++)
                    {
                        bounds.Encapsulate(otherTransform.InverseTransformPoint(transform.TransformPoint(polygons[i].Vertices[j].Position)));
                    }
                }
                return bounds;
            }
            else
            {
                return new Bounds(Vector3.zero, Vector3.zero);
            }
        }

        public float CalculateExtentsInAxis(Vector3 worldAxis)
		{
			// Transform the world axis direction to local
			Vector3 localAxis = transform.InverseTransformDirection(worldAxis);

			float minDot = Vector3.Dot(polygons[0].Vertices[0].Position, localAxis);
			float maxDot = minDot;

			for (int i = 0; i < polygons.Length; i++)
			{
				for (int j = 0; j < polygons[i].Vertices.Length; j++)
				{
					float dot = Vector3.Dot(polygons[i].Vertices[j].Position, localAxis);
					minDot = Mathf.Min(dot, minDot);
					maxDot = Mathf.Max(dot, maxDot);
				}
			}

			return maxDot - minDot;
		}
		
		public override int[] GetPolygonIDs ()
		{
			int[] ids = new int[polygons.Length];
			for (int i = 0; i < polygons.Length; i++) 
			{
				ids[i] = polygons[i].UniqueIndex;
			}
			return ids;
		}

		public override Polygon[] GetPolygons ()
		{
			return polygons;
		}

		public override int AssignUniqueIDs (int startingIndex)
		{
			for (int i = 0; i < polygons.Length; i++) 
			{
				int uniqueIndex = startingIndex + i;
				polygons[i].UniqueIndex = uniqueIndex;
			}

			int assignedCount = polygons.Length;
			
			return assignedCount;
		}

		/// <summary>
		/// Resets the pivot to the center of the brush. The world position of vertices remains unchanged, but the brush position and local vertex positions are updated so that the pivot is at the center.
		/// </summary>
		public void ResetPivot()
		{			
			Vector3 delta = GetBounds().center;

			for (int i = 0; i < polygons.Length; i++) 
			{
				for (int j = 0; j < polygons[i].Vertices.Length; j++) 
				{
					polygons[i].Vertices[j].Position -= delta;
				}
			}

			// Bounds is aligned with the object
			transform.Translate(delta.Multiply(transform.localScale));

			// Counter the delta offset
			Transform[] childTransforms = transform.GetComponentsInChildren<Transform>(true);

			for (int i = 0; i < childTransforms.Length; i++) 
			{
				if(childTransforms[i] != transform)
				{
					childTransforms[i].Translate(-delta);
				}
			}

			// Only invalidate if it's actually been realigned
			if(delta != Vector3.zero)
			{
				Invalidate(true);
			}
		}
			
		/// <summary>
		/// Duplicates the brush game object and returns the new object.
		/// </summary>
		/// <returns>The game object of the new brush.</returns>
		public GameObject Duplicate()
		{
			GameObject newObject = Instantiate(this.gameObject);

			newObject.name = this.gameObject.name;

			newObject.transform.parent = this.transform.parent;

			return newObject;
		}

		public override void PrepareToBuild(List<Brush> brushes, bool forceRebuild)
		{
			if(forceRebuild)
			{
				brushCache.SetUnbuilt();
				RecachePolygons(true);
				RecalculateIntersections(brushes, false);
			}
		}

		/// <summary>
		/// Get the CSG Model that the brush is under
		/// </summary>
		/// <returns>The CSG Model.</returns>
		public CSGModelBase GetCSGModel()
		{
			if (parentCsgModel == null)
			{
				CSGModelBase[] models = transform.GetComponentsInParent<CSGModelBase>(true);
				if(models.Length > 0)
				{
					parentCsgModel = models[0];
				}
			}
			return parentCsgModel;
		}

		public override void UpdateCachedBrushOrder ()
		{
			Transform csgModelTransform = GetCSGModel().transform;

			List<int> reversePositions = new List<int>();

			Transform traversedTransform = transform;

			reversePositions.Add(traversedTransform.GetSiblingIndex());

			while(traversedTransform.parent != null && traversedTransform.parent != csgModelTransform)
			{
				traversedTransform = traversedTransform.parent;
				reversePositions.Add(traversedTransform.GetSiblingIndex());
			}

			BrushOrder brushOrder = new BrushOrder();
			int count = reversePositions.Count;
			brushOrder.Position = new int[count];
			for (int i = 0; i < count; i++) 
			{
				brushOrder.Position[i] = reversePositions[count-1-i];
			}

			cachedBrushOrder = brushOrder;
		}

		public override BrushOrder GetBrushOrder ()
		{
			if(cachedBrushOrder == null)
			{
				UpdateCachedBrushOrder();
			}

			return cachedBrushOrder;
		}

#if (UNITY_5_0 || UNITY_5_1)
		void OnDrawGizmosSelected()
		{
			CSGModel parentCSGModel = GetCSGModel() as CSGModel;

			if(parentCSGModel != null)
			{
				// Ensure Edit Mode is on
				parentCSGModel.EditMode = true;
			}
		}
#endif
    }
}

#endif