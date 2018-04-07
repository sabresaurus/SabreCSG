#if UNITY_EDITOR || RUNTIME_CSG
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

namespace Sabresaurus.SabreCSG
{
	/// <summary>
	/// Compound brushes are helpers that contain real brushes (PrimitiveBrush), they have some of the attributes of ordinary brushes which the contained brushes inherit.
	/// </summary>
	public abstract class CompoundBrush : BrushBase
	{
        [SerializeField]
        protected Bounds localBounds = new Bounds(Vector3.zero, new Vector3(2, 2, 2));

        protected List<PrimitiveBrush> generatedBrushes = new List<PrimitiveBrush>();

		private CSGModelBase parentCsgModel;

		// Derived compound brushes should define how many brushes they have
		public abstract int BrushCount
		{
			get;
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

        /// <summary>
        /// Gets the beautiful name of the brush used in auto-generation of the hierarchy name.
        /// </summary>
        /// <value>The beautiful name of the brush.</value>
        public override string BeautifulBrushName
        {
            get
            {
                return GetType().Name;
            }
        }

        protected virtual void Start()
		{
			generatedBrushes = new List<PrimitiveBrush>(GetComponentsInChildren<PrimitiveBrush>());
			for (int i = 0; i < generatedBrushes.Count; i++) 
			{
				generatedBrushes[i].SetBrushController(this);
			}
		}

		public override void Invalidate (bool polygonsChanged)
		{
            base.Invalidate(polygonsChanged);

			generatedBrushes.RemoveAll(item => item == null);
			if(generatedBrushes.Count > BrushCount)
			{
				// Trim off the extraneous brushes
				for (int i = BrushCount; i < generatedBrushes.Count; i++) 
				{
					if(generatedBrushes[i] != null)
					{
						DestroyImmediate(generatedBrushes[i].gameObject);
					}
				}
				generatedBrushes.RemoveRange(BrushCount, generatedBrushes.Count - BrushCount);
			}
			else if(generatedBrushes.Count < BrushCount)
			{
				// Add in new brushes to fill the gap
				while(generatedBrushes.Count < BrushCount)
				{
					PrimitiveBrush newBrush = CreateBrush();
					newBrush.transform.SetParent(this.transform, false);
					newBrush.SetBrushController(this);
					generatedBrushes.Add(newBrush);
				}
			}
		}

        public override Bounds GetBounds()
        {
            return localBounds;
        }

        public override void SetBounds(Bounds newBounds)
        {
            localBounds.center = Vector3.zero;
            localBounds.extents = newBounds.extents;

            transform.Translate(newBounds.center);
        }

        public override Bounds GetBoundsTransformed()
        {
            Vector3[] points = new Vector3[8];

            // Calculate the world positions of the bounds corners
            for (int i = 0; i < 8; i++)
            {
                Vector3 point = localBounds.center;
                Vector3 offset = localBounds.extents;

                if (i % 2 == 0)
                {
                    offset.x = -offset.x;
                }

                if (i % 4 < 2)
                {
                    offset.y = -offset.y;
                }

                if (i % 8 < 4)
                {
                    offset.z = -offset.z;
                }

                point += offset;

                // Transform to world space
                point = transform.TransformPoint(point);
                points[i] = point;
            }

            // Construct the bounds, starting with the first bounds corner
            Bounds bounds = new Bounds(points[0], Vector3.zero);

            // Add the rest of the corners to the bounds
            for (int i = 1; i < 8; i++)
            {
                bounds.Encapsulate(points[i]);
            }

            return bounds;
        }

        public override Bounds GetBoundsLocalTo(Transform otherTransform)
        {
            Vector3[] points = new Vector3[8];

            // Calculate the positions of the bounds corners local to the other transform
            for (int i = 0; i < 8; i++)
            {
                Vector3 point = localBounds.center;
                Vector3 offset = localBounds.extents;

                if (i % 2 == 0)
                {
                    offset.x = -offset.x;
                }

                if (i % 4 < 2)
                {
                    offset.y = -offset.y;
                }

                if (i % 8 < 4)
                {
                    offset.z = -offset.z;
                }

                point += offset;

                // Transform to world space then to the other transform's local space
                point = otherTransform.InverseTransformPoint(transform.TransformPoint(point));
                points[i] = point;
            }

            // Construct the bounds, starting with the first bounds corner
            Bounds bounds = new Bounds(points[0], Vector3.zero);

            // Add the rest of the corners to the bounds
            for (int i = 1; i < 8; i++)
            {
                bounds.Encapsulate(points[i]);
            }

            return bounds;
        }

        /// <summary>
        /// Gets all of the polygons from all brushes in this compound brush.
        /// </summary>
        /// <returns>All of the polygons from all brushes in this compound brush.</returns>
        public Polygon[] GetPolygons()
        {
            List<Polygon> polygons = new List<Polygon>();
            // iterate through all child brushes:
            foreach (Brush brush in GetComponentsInChildren<Brush>())
            {
                polygons.AddRange(GenerateTransformedPolygons(brush.transform, brush.GetPolygons()));
            }
            return polygons.ToArray();
        }

        /// <summary>
        /// Generates transformed polygons to match the compound brush position.
        /// </summary>
        /// <param name="t">The transform of a child brush.</param>
        /// <param name="polygons">The polygons of a child brush.</param>
        /// <returns>The transformed polygons.</returns>
        private Polygon[] GenerateTransformedPolygons(Transform t, Polygon[] polygons)
        {
            Polygon[] polygonsCopy = polygons.DeepCopy<Polygon>();

            Vector3 center = t.localPosition;
            Quaternion rotation = t.localRotation;
            Vector3 scale = t.lossyScale;

            for (int i = 0; i < polygonsCopy.Length; i++)
            {
                for (int j = 0; j < polygonsCopy[i].Vertices.Length; j++)
                {
                    polygonsCopy[i].Vertices[j].Position = rotation * polygonsCopy[i].Vertices[j].Position.Multiply(scale) + center;
                    polygonsCopy[i].Vertices[j].Normal = rotation * polygonsCopy[i].Vertices[j].Normal;
                }

                // Just updated a load of vertex positions, so make sure the cached plane is updated
                polygonsCopy[i].CalculatePlane();
            }

            return polygonsCopy;
        }

        public virtual PrimitiveBrush CreateBrush()
		{
			return GetCSGModel().CreateBrush(PrimitiveBrushType.Cube, Vector3.zero, Vector3.one).GetComponent<PrimitiveBrush>();
		}

		public override void OnUndoRedoPerformed ()
		{
			generatedBrushes = new List<PrimitiveBrush>(GetComponentsInChildren<PrimitiveBrush>());
			for (int i = 0; i < generatedBrushes.Count; i++) 
			{
				generatedBrushes[i].SetBrushController(this);
			}

			Invalidate(true);
		}

		/// <summary>
		/// Searches the main C# assembly for compound brushes that can be instantiated 
		/// </summary>
		/// <returns>All matched types.</returns>
		public static List<Type> FindAllInAssembly()
		{
			List<Type> matchedTypes = new List<Type>();

			Assembly[] allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (Assembly assembly in allAssemblies) 
			{
				// Walk through all the types in the main assembly
				if(assembly.FullName.StartsWith("Assembly-CSharp"))
				{
					Type[] types = assembly.GetTypes();

					for (int i = 0; i < types.Length; i++) 
					{
						// If the Type inherits from CompoundBrush and is not abstract
						if(!types[i].IsAbstract && types[i].IsSubclassOf(typeof(CompoundBrush)))
						{
                            // List of exceptions that already have an icon in the toolbar
                            if (types[i] == typeof(StairBrush)) continue;
                            if (types[i] == typeof(CurvedStairBrush)) continue;
                            if (types[i] == typeof(ShapeEditor.ShapeEditorBrush)) continue;

                            // Valid compound brush type found!
                            matchedTypes.Add(types[i]);
						}
					}
				}
			}

			return matchedTypes;
		}
	}
}
#endif