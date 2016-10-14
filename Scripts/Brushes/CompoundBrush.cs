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