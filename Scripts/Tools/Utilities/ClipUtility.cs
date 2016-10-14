#if UNITY_EDITOR || RUNTIME_CSG
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Sabresaurus.SabreCSG
{
	/// <summary>
	/// Provides utility methods to clip, split and edge loop brushes
	/// </summary>
	public static class ClipUtility
	{
		/// <summary>
		/// Inserts an edge loop into a brush where it intersects a specified plane local to the brush transform
		/// </summary>
		/// <param name="brush">The brush to add an edge loop to.</param>
		/// <param name="localPlane">Local plane on which edges will be created.</param>
		public static void InsertEdgeLoop(PrimitiveBrush brush, Plane localPlane)
		{
			// Clip the polygons against the plane
			List<Polygon> polygonsFront;
			List<Polygon> polygonsBack;

			if(PolygonFactory.SplitPolygonsByPlane(new List<Polygon>(brush.GetPolygons()), localPlane, true, out polygonsFront, out polygonsBack))
			{
				List<Polygon> allPolygons = new List<Polygon>();
				// Concat back into one list
				allPolygons.AddRange(polygonsFront);
				allPolygons.AddRange(polygonsBack);
				// Remove the inserted polygons
				for (int i = 0; i < allPolygons.Count; i++) 
				{
					if(allPolygons[i].ExcludeFromFinal)
					{
						allPolygons.RemoveAt(i);
						i--;
					}
				}
				// Update the brush with the new polygons
				brush.SetPolygons(allPolygons.ToArray(), true);
			}
		}

        /// <summary>
        /// Applies a clip or split operation to a brush by supplying a plane local to the brush. Clipping allows you to cut away and discard parts of the brush, while splitting allows you to split a brush in two.
        /// </summary>
        /// <returns>If keepBothSides is <c>true</c>, this returns the second brush if one was created.</returns>
        /// <param name="brush">Brush to clip/split</param>
        /// <param name="localPlane">Local plane to clip/split against</param>
        /// <param name="keepBothSides">If set to <c>true</c> split the brush and keep both sides.</param>
        /// <param name="resetPivots">If set to <c>true</c> make sure the pivot clipped brush and any new brush are at their center.</param>
        public static GameObject ApplyClipPlane(PrimitiveBrush brush, Plane localPlane, bool keepBothSides, bool resetPivots = true)
		{
			// Clip the polygons against the plane
			List<Polygon> polygonsFront;
			List<Polygon> polygonsBack;

			List<Polygon> polygonList = new List<Polygon>(brush.GetPolygons());

			if(PolygonFactory.SplitPolygonsByPlane(polygonList, localPlane, false, out polygonsFront, out polygonsBack))
			{
				// Update the brush with the new polygons
				brush.SetPolygons(polygonsFront.ToArray(), true);

				GameObject newObject = null;
				// If they have decided to split instead of clip, create a second brush with the other side
				if(keepBothSides)
				{
					newObject = brush.Duplicate();

					// Finally give the new brush the other set of polygons
					newObject.GetComponent<PrimitiveBrush>().SetPolygons(polygonsBack.ToArray(), true);
					newObject.transform.SetSiblingIndex(brush.transform.GetSiblingIndex());
                    // Reset the new brush's pivot
                    if (resetPivots)
                    {
                        newObject.GetComponent<PrimitiveBrush>().ResetPivot();
                    }
				}

                // Can't reset the first brush pivot until after the second brush is made, otherwise the second 
                // brush is effectively translated twice, ending up in the wrong position
                if (resetPivots)
                {
                    brush.ResetPivot();
                }

                return newObject;
			}
			else
			{
				return null;
			}
		}
	}
}
#endif