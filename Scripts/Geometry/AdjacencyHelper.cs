#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Sabresaurus.SabreCSG
{
	/// <summary>
	/// Provides helper methods for determining polygon adjacency
	/// </summary>
	public static class AdjacencyHelper
	{
        const float FLOOR_LIMIT = 45; // Angle (degrees) limit from the vertical for what is considered a floor or ceiling
		
        /// <summary>
		/// Delegate that takes a polygon and specifies whether it is relevant to an adjacency test
		/// </summary>
		public delegate bool IsPolygonRelevant(Polygon candidatePolygon);

        /// <summary>
        /// Delegate that takes two polygons and specifies whether they can be considered relevant to each other in an adjacency test
        /// </summary>
        public delegate bool ArePolygonsRelevant(Polygon sourcePolygon, Polygon candidatePolygon);

        /// <summary>
        /// Finds the polygons in allBuiltPolygons that share an edge with one of the source polygons and point roughly horizontal
        /// </summary>
        /// <returns>The adjacent polygon Unique Indexes.</returns>
        /// <param name="allBuiltPolygons">All possible polygons.</param>
        /// <param name="selectedSourcePolygons">Selected source polygons.</param>
        /// <param name="filter">Filter to further refine acceptable adjacency criteria, pass null for no filter.</param>
        public static List<int> FindAdjacentWalls(List<Polygon> allBuiltPolygons, List<Polygon> selectedSourcePolygons, AdjacencyFilters.BaseFilter filter)
		{
			return FindAdjacentGeometry(allBuiltPolygons, selectedSourcePolygons, IsPolygonWall, null, true, filter);
		}

        /// <summary>
        /// Finds the polygons in allBuiltPolygons that share an edge with one of the source polygons and point roughly up
        /// </summary>
        /// <returns>The adjacent polygon Unique Indexes.</returns>
        /// <param name="allBuiltPolygons">All possible polygons.</param>
        /// <param name="selectedSourcePolygons">Selected source polygons.</param>
        /// <param name="filter">Filter to further refine acceptable adjacency criteria, pass null for no filter.</param>
        public static List<int> FindAdjacentFloors(List<Polygon> allBuiltPolygons, List<Polygon> selectedSourcePolygons, AdjacencyFilters.BaseFilter filter)
		{
			return FindAdjacentGeometry(allBuiltPolygons, selectedSourcePolygons, IsPolygonFloor, null, true, filter);
		}

        /// <summary>
        /// Finds the polygons in allBuiltPolygons that share an edge with one of the source polygons and point roughly down
        /// </summary>
        /// <returns>The adjacent polygon Unique Indexes.</returns>
        /// <param name="allBuiltPolygons">All possible polygons.</param>
        /// <param name="selectedSourcePolygons">Selected source polygons.</param>
        /// <param name="filter">Filter to further refine acceptable adjacency criteria, pass null for no filter.</param>
        public static List<int> FindAdjacentCeilings(List<Polygon> allBuiltPolygons, List<Polygon> selectedSourcePolygons, AdjacencyFilters.BaseFilter filter)
		{
			return FindAdjacentGeometry(allBuiltPolygons, selectedSourcePolygons, IsPolygonCeiling, null, true, filter);
		}

        /// <summary>
        /// Finds the polygons in allBuiltPolygons that share any edge with one of the source polygons
        /// </summary>
        /// <returns>The adjacent polygon Unique Indexes.</returns>
        /// <param name="allBuiltPolygons">All possible polygons.</param>
        /// <param name="selectedSourcePolygons">Selected source polygons.</param>
        /// <param name="filter">Filter to further refine acceptable adjacency criteria, pass null for no filter.</param>
        public static List<int> FindAdjacentAll(List<Polygon> allBuiltPolygons, List<Polygon> selectedSourcePolygons, AdjacencyFilters.BaseFilter filter)
		{
			return FindAdjacentGeometry(allBuiltPolygons, selectedSourcePolygons, null, null, false, filter);
		}

        /// <summary>
        /// Finds the polygons in allBuiltPolygons that share an edge and are coplanar with one of the source polygons
        /// </summary>
        /// <returns>The adjacent polygon Unique Indexes.</returns>
        /// <param name="allBuiltPolygons">All possible polygons.</param>
        /// <param name="selectedSourcePolygons">Selected source polygons.</param>
        /// <param name="filter">Filter to further refine acceptable adjacency criteria, pass null for no filter.</param>
        public static List<int> FindAdjacentCoplanar(List<Polygon> allBuiltPolygons, List<Polygon> selectedSourcePolygons, AdjacencyFilters.BaseFilter filter)
        {
            return FindAdjacentGeometry(allBuiltPolygons, selectedSourcePolygons, null, ArePolygonsCoplanar, false, filter);
        }

        /// <summary>
        /// Given possible polygons and specific source polygons, this takes two delegates for if an edge and a polygon is relevant and finds all the polygons that match its criteria (see the other FindAdjacent* methods in this file for examples)
        /// </summary>
        /// <param name="allBuiltPolygons">All possible polygons.</param>
        /// <param name="selectedSourcePolygons">Selected source polygons.</param>
        /// <param name="isEdgeRelevant">Is edge relevant.</param>
        /// <param name="isPolygonRelevant">Is polygon relevant.</param>
        /// <param name="applyPolygonRuleToSource">When True, isPolygonRelevant must be true for both polygons involved in the adjacency test</param>
        /// <param name="filter">Filter to further refine acceptable adjacency criteria, pass null for no filter.</param>
        public static List<int> FindAdjacentGeometry(List<Polygon> allBuiltPolygons, 
			List<Polygon> selectedSourcePolygons, 
			IsPolygonRelevant isPolygonRelevant, 
			ArePolygonsRelevant arePolygonsRelevant, 
            bool applyPolygonRuleToSource,
            AdjacencyFilters.BaseFilter filter)
		{
			List<Polygon> unselectedPolygons = new List<Polygon>(allBuiltPolygons);
			List<Polygon> selectedBuiltPolygons = new List<Polygon>();

			// Sort the built polygons into those that are selected and those that aren't
			// This starts off with all the polygons in the unselected list then picks out all those that are actually
			// selected.
			IEqualityComparer<Polygon> comparer = new Polygon.PolygonUIDComparer();
			for (int i = 0; i < unselectedPolygons.Count; i++) 
			{
				if(selectedSourcePolygons.Contains(unselectedPolygons[i], comparer))
				{
					selectedBuiltPolygons.Add(unselectedPolygons[i]);

					unselectedPolygons.RemoveAt(i);
					i--;
				}
			}

            // Now extract all the target edges of the selected polygons
            Dictionary<Polygon, Edge[]> groupedEdges = new Dictionary<Polygon, Edge[]>();
			
			for (int polygonIndex = 0; polygonIndex < selectedBuiltPolygons.Count; polygonIndex++) 
			{
				Edge[] edges = selectedBuiltPolygons[polygonIndex].GetEdges();
                groupedEdges[selectedBuiltPolygons[polygonIndex]] = edges;
			}

			// Walk through all the unselected polygons and see if any match the target edges (if a isPolygonRelevant
			// delegate is also supplied then the unselected polygon must also be considered relevant)
			for (int unselectedPolygonIndex = 0; unselectedPolygonIndex < unselectedPolygons.Count; unselectedPolygonIndex++) 
			{
                // If the polygon is not relevant, ignore it
				if(isPolygonRelevant != null && !isPolygonRelevant(unselectedPolygons[unselectedPolygonIndex]))
                {
                    continue;
                }

                // If the polygon is not acceptable to the filter, ignore it
                if (filter != null && !filter.IsPolygonAcceptable(unselectedPolygons[unselectedPolygonIndex]))
                {
                    continue;
                }

                // Grab all the edges of the polygon
                Edge[] edges = unselectedPolygons[unselectedPolygonIndex].GetEdges();

                foreach (KeyValuePair<Polygon, Edge[]> group in groupedEdges)
                {
                    if (applyPolygonRuleToSource
                        && isPolygonRelevant != null
                        && !isPolygonRelevant(group.Key))
                    {
                        continue;
                    }

                    if (arePolygonsRelevant != null
                        && !arePolygonsRelevant(group.Key, unselectedPolygons[unselectedPolygonIndex]))
                    {
                        continue;
                    }

                    Edge[] targetEdges = group.Value;

                    bool matched = false;
                    // Test each edge to see if it matches the target edges
                    for (int edgeIndex = 0; edgeIndex < edges.Length; edgeIndex++)
                    {
                        for (int targetEdgeIndex = 0; targetEdgeIndex < targetEdges.Length; targetEdgeIndex++)
                        {
                            if (edges[edgeIndex].Intersects(targetEdges[targetEdgeIndex]))
                            {
                                matched = true;
                                selectedBuiltPolygons.Add(unselectedPolygons[unselectedPolygonIndex]);
                                break;
                            }
                        }
                        if (matched)
                        {
                            break;
                        }
                    }
                    if (matched)
                    {
                        break;
                    }
                }
			}

			// Return all the unique set of polygon IDs
			return selectedBuiltPolygons.Select(polygon => polygon.UniqueIndex).Distinct().ToList();
		}

		static bool IsPolygonFloor(Polygon candidatePolygon)
        {
            return Vector3.Angle(candidatePolygon.Plane.normal, Vector3.up) <= (FLOOR_LIMIT + 0.01f); 
		}

		static bool IsPolygonCeiling(Polygon candidatePolygon)
        {
            return Vector3.Angle(candidatePolygon.Plane.normal, Vector3.down) <= (FLOOR_LIMIT + 0.01f);
		}

        static bool IsPolygonWall(Polygon candidatePolygon)
        {
            return Mathf.Abs(Vector3.Dot(candidatePolygon.Plane.normal, Vector3.up)) < 0.01f;
        }

        static bool ArePolygonsCoplanar(Polygon sourcePolygon, Polygon candidatePolygon)
        {
            return Vector3.Dot(sourcePolygon.Plane.normal, candidatePolygon.Plane.normal) > 0.99f;
        }

        /// <summary>
        /// Given an ordered series of vertices, a specific vertex and one of its neighbours, return the other neighbour
        /// </summary>
        /// <returns>The vertex adjacent to <c>sourceVertex</c> in the opposite direction to <c>neighbourToExclude</c>.</returns>
        /// <param name="vertices">Order vertices.</param>
        /// <param name="sourceVertex">Source vertex.</param>
        /// <param name="neighbourToExclude">Neighbour to exclude.</param>
        public static Vertex FindAdjacentVertex(Vertex[] vertices, Vertex sourceVertex, Vertex neighbourToExclude)
		{
			Vertex edge1AdjacentVertex = null;
			for (int i = 0; i < vertices.Length; i++) 
			{
				if(vertices[i] == sourceVertex)
				{
					int lastIndex = i-1;
					if(lastIndex < 0)
					{
						lastIndex = vertices.Length-1;
					}
					int nextIndex = i+1;
					if(nextIndex >= vertices.Length)
					{
						nextIndex = 0;
					}
					if(vertices[lastIndex] == neighbourToExclude)
					{
						edge1AdjacentVertex = vertices[nextIndex];
					}
					else
					{
						edge1AdjacentVertex = vertices[nextIndex];
					}
				}
			}
			return edge1AdjacentVertex;
		}
	}
}
#endif