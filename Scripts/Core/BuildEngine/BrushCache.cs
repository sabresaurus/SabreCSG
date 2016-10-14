#if UNITY_EDITOR || RUNTIME_CSG
using UnityEngine;
using UnityEngine.Serialization;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;

namespace Sabresaurus.SabreCSG
{
	[System.Serializable]
	public class BrushCache
	{
		[SerializeField]
		CSGMode mode;

		[SerializeField]
		Polygon[] polygons;

		[SerializeField]
		SerializablePlane[] splitPlanes;

		[SerializeField]
		Bounds bounds;

		[SerializeField]
		bool built = false;

		[SerializeField, FormerlySerializedAs("intersectingBrushes")]
		List<Brush> intersectingVisualBrushes = new List<Brush>();

		[NonSerialized]
		List<BrushCache> intersectingVisualBrushCaches = null;

		[SerializeField]
		List<Brush> intersectingCollisionBrushes = new List<Brush>();

		[NonSerialized]
		List<BrushCache> intersectingCollisionBrushCaches = null;

		[SerializeField, FormerlySerializedAs("builtPolygons")]
		internal List<Polygon> builtVisualPolygons = new List<Polygon>();

		[SerializeField]
		internal List<Polygon> builtCollisionPolygons = new List<Polygon>();

		[SerializeField]
		bool collisionVisualEqual = false;

		internal List<KeyValuePair<Polygon, BrushCache>> stolenVisualPolygons = new List<KeyValuePair<Polygon, BrushCache>>();
		internal List<KeyValuePair<Polygon, BrushCache>> stolenCollisionPolygons = new List<KeyValuePair<Polygon, BrushCache>>();

		public CSGMode Mode {
			get {
				return mode;
			}
		}

		public bool Built {
			get {
				return built;
			}
		}

		public Polygon[] Polygons {
			get {
				return polygons;
			}
		}

		public Plane[] SplitPlanes 
		{
			get 
			{
				Plane[] planes = new Plane[splitPlanes.Length];
				for (int i = 0; i < splitPlanes.Length; i++) 
				{
					planes[i] = splitPlanes[i].UnityPlane;
				}
				return planes;
			}
		}

		public Bounds Bounds {
			get {
				return bounds;
			}
		}

		public List<Brush> IntersectingVisualBrushes 
		{
			get 
			{
				return intersectingVisualBrushes;
			}
		}

		public List<BrushCache> IntersectingVisualBrushCaches 
		{
			get 
			{
				// Recreate at run time
				if(intersectingVisualBrushCaches == null)
				{
					intersectingVisualBrushCaches = new List<BrushCache>(intersectingVisualBrushes.Count);
					for (int i = 0; i < intersectingVisualBrushes.Count; i++) 
					{
						if(intersectingVisualBrushes[i] != null)
						{
							intersectingVisualBrushCaches.Add(intersectingVisualBrushes[i].BrushCache);
						}
						else
						{
							// Add a null item anyway, to preserve indexing
							intersectingVisualBrushCaches.Add(null);
						}
					}
				}
				return intersectingVisualBrushCaches;
			}
		}

		public List<Brush> IntersectingCollisionBrushes 
		{
			get 
			{
				return intersectingCollisionBrushes;
			}
		}

		public List<BrushCache> IntersectingCollisionBrushCaches 
		{
			get 
			{
				// Recreate at run time
				if(intersectingCollisionBrushCaches == null)
				{
					intersectingCollisionBrushCaches = new List<BrushCache>(intersectingCollisionBrushes.Count);
					for (int i = 0; i < intersectingCollisionBrushes.Count; i++) 
					{
						if(intersectingCollisionBrushes[i] != null)
						{
							intersectingCollisionBrushCaches.Add(intersectingCollisionBrushes[i].BrushCache);
						}
						else
						{
							// Add a null item anyway, to preserve indexing
							intersectingCollisionBrushCaches.Add(null);
						}
					}
				}
				return intersectingCollisionBrushCaches;
			}
		}

		public List<Polygon> BuiltVisualPolygons 
		{
			get 
			{
				return builtVisualPolygons;
			}
		}

		public List<Polygon> BuiltCollisionPolygons 
		{
			get 
			{
				return builtCollisionPolygons;
			}
		}

		public bool CollisionVisualEqual
		{
			get
			{
				return collisionVisualEqual;
			}
		}

		public Dictionary<int, List<Polygon>> GetGroupedBuiltVisualPolygons()
		{
			Dictionary<int, List<Polygon>> groups = new Dictionary<int, List<Polygon>>();

			int firstIndex = polygons[0].UniqueIndex;
			int lastIndex = firstIndex + polygons.Length-1;

			for (int i = 0; i < builtVisualPolygons.Count; i++) 
			{
				Polygon polygon = builtVisualPolygons[i];

				if(polygon.UniqueIndex >= firstIndex && polygon.UniqueIndex <= lastIndex)
				{
					if(!groups.ContainsKey(polygon.UniqueIndex))
					{
						groups.Add(polygon.UniqueIndex, new List<Polygon>() { polygon } );
					}
					else
					{
						groups[polygon.UniqueIndex].Add(polygon);
					}
				}
			}
			return groups;
		}

		public Dictionary<int, List<Polygon>> GetGroupedBuiltCollisionPolygons()
		{
			Dictionary<int, List<Polygon>> groups = new Dictionary<int, List<Polygon>>();

			int firstIndex = polygons[0].UniqueIndex;
			int lastIndex = firstIndex + polygons.Length-1;

			for (int i = 0; i < builtCollisionPolygons.Count; i++) 
			{
				Polygon polygon = builtCollisionPolygons[i];

				int uniqueIndex = polygon.UniqueIndex;

				if(uniqueIndex >= firstIndex && uniqueIndex <= lastIndex)
				{
					if(!groups.ContainsKey(uniqueIndex))
					{
						groups.Add(uniqueIndex, new List<Polygon>() { polygon } );
					}
					else
					{
						groups[uniqueIndex].Add(polygon);
					}
				}
			}
			return groups;
		}

		public void Set(CSGMode mode, Polygon[] polygons, Bounds bounds, bool markUnbuilt)
		{
			this.mode = mode;
			this.polygons = polygons;
			this.bounds = bounds;


			// Calculate the split planes from the polygons. Note special care has to be taken here when dealing
			// with multiple coplanar polygons. 

			// First, group all the polygons by plane
			List<List<Polygon>> polygonsGroupedByPlane = new List<List<Polygon>>();

			for (int polygonIndex = 0; polygonIndex < polygons.Length; polygonIndex++) 
			{
				Polygon polygon = polygons[polygonIndex];
				bool found = false;
				// See if it already exists
				for (int i = 0; i < polygonsGroupedByPlane.Count; i++) 
				{
					Polygon existingPolygon = polygonsGroupedByPlane[i][0];
					if(MathHelper.PlaneEqualsLooser(existingPolygon.Plane, polygon.Plane))
					{
						polygonsGroupedByPlane[i].Add(polygon);
						found = true;
                        break;
					}
				}

				// Not added to an existing group, so add new
				if(!found)
				{
					// Add a new group for the polygon
					polygonsGroupedByPlane.Add(new List<Polygon>() { polygon } );
				}
			}

			splitPlanes = new SerializablePlane[polygons.Length];

			// Now calculate the split planes
			int planeIndex = 0;
			for (int i = 0; i < polygonsGroupedByPlane.Count; i++) 
			{
				// First polygon in a group always presents a split plane
				splitPlanes[planeIndex] = new SerializablePlane	(polygonsGroupedByPlane[i][0].Plane);
				planeIndex++;

				// Subsequent polygons in a group will form a split plane from their neighbouring coplanar polygon
				for (int j = 1; j < polygonsGroupedByPlane[i].Count; j++) 
				{					
					for (int k = 0; k < polygonsGroupedByPlane[i].Count; k++) 
					{
						// Don't test a polygon against itself
						if(j != k)
						{
							Plane tempPlane;
							// Found an edge between two coplanar polygons and constructed a plane between them
							if(PlaneBetweenPolygons(polygonsGroupedByPlane[i][j], polygonsGroupedByPlane[i][k], out tempPlane))
							{
								bool alreadyAdded = false;

								for (int existingPlane = 0; existingPlane < planeIndex; existingPlane++) 
								{
									if(MathHelper.PlaneEqualsLooserWithFlip(tempPlane, splitPlanes[existingPlane].UnityPlane))
									{
										alreadyAdded = true;
										break;
									}
								}

								if(alreadyAdded == false)
								{
									// Use the plane that separates the two polygons as the split plane
									splitPlanes[planeIndex] = new SerializablePlane	(tempPlane);
									planeIndex++;
									break;
								}
							}
						}
					}
				}
			}

			if(markUnbuilt)
			{
				built = false;
			}
		}

		private static bool PlaneBetweenPolygons(Polygon polygon1, Polygon polygon2, out Plane plane)
		{
//			Vertex[] vertices1 = polygon1.Vertices;
//			Vertex[] vertices2 = polygon2.Vertices;

			for (int i = 0; i < polygon1.Vertices.Length; i++) 
			{
				Vertex vertex1A = polygon1.Vertices[i];
				Vertex vertex1B = polygon1.Vertices[(i+1) % polygon1.Vertices.Length];

				for (int j = 0; j < polygon2.Vertices.Length; j++) 
				{
					Vertex vertex2A = polygon2.Vertices[j];
					Vertex vertex2B = polygon2.Vertices[(j+1) % polygon2.Vertices.Length];

					if((vertex1A.Position.EqualsWithEpsilon(vertex2A.Position)
						&& vertex1B.Position.EqualsWithEpsilon(vertex2B.Position))
						|| (vertex1A.Position.EqualsWithEpsilon(vertex2B.Position)
							&& vertex1B.Position.EqualsWithEpsilon(vertex2A.Position)))
					{
						Vector3 thirdPoint = vertex1A.Position + polygon1.Plane.normal;

						plane = new Plane(vertex1A.Position, vertex1B.Position, thirdPoint);

						return true;
					}
				}
			}

			// None matched, just return a default plane
			plane = new Plane();
			return false;
		}

		public void SetIntersection(List<Brush> intersectingVisualBrushes, List<Brush> intersectingCollisonBrushes)
		{
			// Visual
			this.intersectingVisualBrushes = intersectingVisualBrushes;

			intersectingVisualBrushCaches = new List<BrushCache>(intersectingVisualBrushes.Count);
			for (int i = 0; i < intersectingVisualBrushes.Count; i++) 
			{
				intersectingVisualBrushCaches.Add(intersectingVisualBrushes[i].BrushCache);
			}

			// Collision
			this.intersectingCollisionBrushes = intersectingCollisonBrushes;

			if(intersectingCollisonBrushes.ContentsEquals(intersectingVisualBrushes))
			{
				// Both intersections are equal, so no need to build distinct collision geometry
				collisionVisualEqual = true; 
				// Copy the visual brush caches into collision
				intersectingCollisionBrushCaches = new List<BrushCache>(intersectingVisualBrushCaches);
			}
			else
			{
				// The two intersection sets are not equal, must calculate collision geometry separately
				collisionVisualEqual = false;
				intersectingCollisionBrushCaches = new List<BrushCache>(intersectingCollisionBrushes.Count);
				for (int i = 0; i < intersectingCollisionBrushes.Count; i++) 
				{
					intersectingCollisionBrushCaches.Add(intersectingCollisionBrushes[i].BrushCache);
				}
			}
		}

		public int AssignUniqueIDs (int startingIndex, 
			bool isVisible,
			PolygonEntry[] oldVisualPolygonIndex, 
			PolygonEntry[] newVisualPolygonIndex,
			bool hasCollision,
			PolygonEntry[] oldCollisionPolygonIndex, 
			PolygonEntry[] newCollisionPolygonIndex)
		{
			int assignedCount = 0;

			for (int i = 0; i < polygons.Length; i++) 
			{
				int previousUniqueIndex = polygons[i].UniqueIndex;
				int newUniqueIndex = startingIndex + i;
				polygons[i].UniqueIndex = newUniqueIndex;

				// Preserve existing geometry if this brush has remained built or is additive
				if(built || mode == CSGMode.Add)
				{
					if(isVisible)
					{
						if(previousUniqueIndex != -1 && oldVisualPolygonIndex.Length > previousUniqueIndex)
						{
							// Transfer mapping from old index to new
							PolygonEntry entry = oldVisualPolygonIndex[previousUniqueIndex];

							newVisualPolygonIndex[newUniqueIndex] = entry;
						}
					}

					if(hasCollision)
					{
						if(previousUniqueIndex != -1 && oldCollisionPolygonIndex.Length > previousUniqueIndex)
						{
							// Transfer mapping from old index to new
							PolygonEntry entry = oldCollisionPolygonIndex[previousUniqueIndex];

							newCollisionPolygonIndex[newUniqueIndex] = entry;
						}
					}
				}
			}

			assignedCount = polygons.Length;

			return assignedCount;
		}

		public void ResetForBuild(PolygonEntry[] newVisualPolygonIndex, PolygonEntry[] newCollisionPolygonIndex)
		{
			builtVisualPolygons.Clear();
			builtCollisionPolygons.Clear();

			for (int i = 0; i < polygons.Length; i++) 
			{
				int polygonIndex = polygons[i].UniqueIndex;
				newVisualPolygonIndex[polygonIndex] = null;
				newCollisionPolygonIndex[polygonIndex] = null;
			}
		}

		// Used to force a rebuild
		public void SetUnbuilt()
		{
			built = false;
		}

		public void SetBuilt()
		{
			built = true;
		}

		public void SetVisualBuiltPolygons(List<Polygon> builtVisualPolygons)
		{
			this.builtVisualPolygons = builtVisualPolygons;
		}

		public void SetCollisionBuiltPolygons(List<Polygon> builtCollisionPolygons)
		{
			this.builtCollisionPolygons = builtCollisionPolygons;
		}

        internal static void NotifyOfStolenVisualPolygon(BrushCache mainCache, BrushCache brushCacheSource, Polygon newPolygon)
        {
            mainCache.stolenVisualPolygons.Add(new KeyValuePair<Polygon, BrushCache>(newPolygon, brushCacheSource));
        }

        internal static void NotifyOfStolenCollisionPolygon(BrushCache mainCache, BrushCache brushCacheSource, Polygon newPolygon)
        {
            mainCache.stolenCollisionPolygons.Add(new KeyValuePair<Polygon, BrushCache>(newPolygon, brushCacheSource));
        }

        internal static void ReclaimStolenVisualPolygons(BrushCache mainCache)
        {
            List<KeyValuePair<Polygon, BrushCache>> stolenVisualPolygons = mainCache.stolenVisualPolygons;

            for (int i = 0; i < stolenVisualPolygons.Count; i++)
            {
                // TODO: Does this actually remove all of them?
                bool removed = stolenVisualPolygons[i].Value.builtVisualPolygons.Remove(stolenVisualPolygons[i].Key);
                if (removed)
                {
                    mainCache.builtVisualPolygons.Add(stolenVisualPolygons[i].Key);
                }

                stolenVisualPolygons.RemoveAt(i);
                i--;
            }
        }

        internal static void ReclaimStolenCollisionPolygons(BrushCache mainCache)
        {
            List<KeyValuePair<Polygon, BrushCache>> stolenCollisionPolygons = mainCache.stolenCollisionPolygons;

            for (int i = 0; i < stolenCollisionPolygons.Count; i++)
            {
                // TODO: Does this actually remove all of them?
                bool removed = stolenCollisionPolygons[i].Value.builtCollisionPolygons.Remove(stolenCollisionPolygons[i].Key);
                if (removed)
                {
                    mainCache.builtCollisionPolygons.Add(stolenCollisionPolygons[i].Key);
                }

                stolenCollisionPolygons.RemoveAt(i);
                i--;
            }
        }

        internal static Dictionary<int, List<Polygon>> OptimizeVisual(BrushCache mainCache)
        {
            // Get the polygons grouped by ID
            Dictionary<int, List<Polygon>> groupedPolygons = mainCache.GetGroupedBuiltVisualPolygons();
            List<Polygon> allPolygons = new List<Polygon>();

            foreach (KeyValuePair<int, List<Polygon>> row in groupedPolygons)
            {
                // Determine the polygon set that is optimal
                List<Polygon> newPolygons = Optimizer.CalculateConvexHulls(row.Value);
                // If the polygon set has actually changed
                if (newPolygons != row.Value)
                {
                    // Replace the grouped polygons with the optimal set
                    row.Value.Clear();
                    row.Value.AddRange(newPolygons);
                }
                // Add the new polygons to the total list
                allPolygons.AddRange(newPolygons);
            }

            // Set the built polygons for this cache from the newly calculated optimal polygons
            mainCache.SetVisualBuiltPolygons(allPolygons);

            return groupedPolygons;
        }

        internal static Dictionary<int, List<Polygon>> OptimizeCollision(BrushCache mainCache)
        {
            // Get the polygons grouped by ID
            Dictionary<int, List<Polygon>> groupedPolygons = mainCache.GetGroupedBuiltCollisionPolygons();
            List<Polygon> allPolygons = new List<Polygon>();

            foreach (KeyValuePair<int, List<Polygon>> row in groupedPolygons)
            {
                // Determine the polygon set that is optimal
                List<Polygon> newPolygons = Optimizer.CalculateConvexHulls(row.Value);
                // If the polygon set has actually changed
                if (newPolygons != row.Value)
                {
                    // Replace the grouped polygons with the optimal set
                    row.Value.Clear();
                    row.Value.AddRange(newPolygons);
                }
                // Add the new polygons to the total list
                allPolygons.AddRange(newPolygons);
            }

            // Set the built polygons for this cache from the newly calculated optimal polygons
            mainCache.SetVisualBuiltPolygons(allPolygons);

            return groupedPolygons;
        }
    }
}
#endif