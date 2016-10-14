#if UNITY_EDITOR || RUNTIME_CSG
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Sabresaurus.SabreCSG
{
	/// <summary>
	/// Provides utility methods for working with brush edges (vertex pairs)
	/// </summary>
	public static class EdgeUtility
	{
		/// <summary>
		/// When supplied with edges, this walks through supplied polygons and splits polygons where they contain two of the edges. Useful for splitting a polygon by two edge mid-points (as used by the Vertex tool's Connect Mid-Points button)
		/// </summary>
		/// <param name="polygons">Source polygons to walk through.</param>
		/// <param name="sourceEdges">Source edges.</param>
		/// <param name="finalPolygons">The complete new set of polygons (including those created).</param>
		/// <param name="newEdges">New edges created.</param>
		public static void SplitPolygonsByEdges(Polygon[] polygons, List<Edge> sourceEdges, out Polygon[] finalPolygons, out List<Edge> newEdges)
		{
			// First of all refine the list of edges to those that are on the polygons and share a polygon with another specified edge
			// Verification step, no more than two edges should be selected per face

			// Once the list of edges is refined, walk through each set of polygons
			// Where a polygon has two specified edges, it needs to be split in two
			// Where a polygon has one specified edge, it needs a vertex to be added
			List<Polygon> newPolygons = new List<Polygon>(polygons); // Complete set of new polygons
			newEdges = new List<Edge>(); // These are the new edges we create

			List<Edge> edges = new List<Edge>();

			// Pull out a list of edges that occur on any of the polygons at least twice.
			// This way we ignore edges on other brushes or edges which aren't possible to connect via a polygon
			for (int edge1Index = 0; edge1Index < sourceEdges.Count; edge1Index++) 
			{
				bool found = false;

				for (int i = 0; i < polygons.Length && !found; i++) 
				{
					Edge edge1 = sourceEdges[edge1Index];

					for (int edge2Index = 0; edge2Index < sourceEdges.Count && !found; edge2Index++) 
					{
						if(edge2Index != edge1Index) // Skip the same edge
						{
							Edge edge2 = sourceEdges[edge2Index];

							bool edge1Contained = Polygon.ContainsEdge(polygons[i], edge1);
							bool edge2Contained = Polygon.ContainsEdge(polygons[i], edge2);

							if(edge1Contained && edge2Contained)
							{
								if(!edges.Contains(edge1))
								{
									edges.Add(edge1);
								}

								if(!edges.Contains(edge2))
								{
									edges.Add(edge2);
								}
								found = true;
							}
						}
					}
				}
			}				

			// Now process each polygon
			for (int i = 0; i < polygons.Length; i++) 
			{
				Polygon polygon = polygons[i];

				List<Edge> edgesOnPolygon = new List<Edge>();
				for (int edgeIndex = 0; edgeIndex < edges.Count; edgeIndex++) 
				{
					Edge edge = edges[edgeIndex];
					if(Polygon.ContainsEdge(polygon, edge))
					{
						edgesOnPolygon.Add(edge);
					}
				}

				if(edgesOnPolygon.Count == 1)
				{
					Vertex newVertex;
					// Add vertex
					if(!SplitPolygonAtEdge(polygon, edgesOnPolygon[0], out newVertex))
					{
						Debug.LogError("Could not add vertex to adjacent polygon");
					}
				}
				else if(edgesOnPolygon.Count == 2)
				{
					// Split into two
					Edge edge1 = edgesOnPolygon[0];
					Edge edge2 = edgesOnPolygon[1];

					// First split the shared polygon
					Vector3 edge1Center = edge1.GetCenterPoint();
					Vector3 edge2Center = edge2.GetCenterPoint();

					Vector3 thirdPoint = edge1Center + polygon.Plane.normal;

					Plane splitPlane = new Plane(edge1Center, edge2Center, thirdPoint);

					Polygon splitPolygon1;
					Polygon splitPolygon2;
					Vertex edge1Vertex;
					Vertex edge2Vertex;

					Polygon.SplitPolygon(polygon, out splitPolygon1, out splitPolygon2, out edge1Vertex, out edge2Vertex, splitPlane);

					newEdges.Add(new Edge(edge1Vertex, edge2Vertex));
					newPolygons.Remove(polygon);
					newPolygons.Add(splitPolygon1);
					newPolygons.Add(splitPolygon2);
				}
			}

			finalPolygons = newPolygons.ToArray();
		}

		/// <summary>
		/// Adds a vertex to polygon at the center of the supplied edge, used by the Vertex tool's Split Edge button
		/// </summary>
		/// <returns><c>true</c>, if the edge was matched in the polygon and a vertex was added, <c>false</c> otherwise.</returns>
		/// <param name="polygon">Source polygon to add a vertex to.</param>
		/// <param name="edge">Edge to match and at a vertex to</param>
		/// <param name="newVertex">New vertex if one was created (first check the method returned <c>true</c>)</param>
		public static bool SplitPolygonAtEdge(Polygon polygon, Edge edge, out Vertex newVertex)
		{
			newVertex = null;

			List<Vertex> vertices = new List<Vertex>(polygon.Vertices);
			for (int i = 0; i < polygon.Vertices.Length; i++) 
			{
				Vector3 position1 = polygon.Vertices[i].Position;
				Vector3 position2 = polygon.Vertices[(i+1)%polygon.Vertices.Length].Position;

				if((edge.Vertex1.Position.EqualsWithEpsilon(position1) && edge.Vertex2.Position.EqualsWithEpsilon(position2))
					|| (edge.Vertex1.Position.EqualsWithEpsilon(position2) && edge.Vertex2.Position.EqualsWithEpsilon(position1)))
				{
					newVertex = Vertex.Lerp(polygon.Vertices[i], polygon.Vertices[(i+1) % polygon.Vertices.Length], 0.5f);
					vertices.Insert(i+1, newVertex);
					break;
				}
			}

			if(vertices.Count == polygon.Vertices.Length)
			{
				// Could not add vertex to adjacent polygon
				return false;
			}

			polygon.SetVertices(vertices.ToArray());

			return true;
		}


		/// <summary>
		/// Determines if two polygons share an edge, outputing the two respective shared edges if so.
		/// </summary>
		/// <returns><c>true</c>, if a shared edge was found, <c>false</c> otherwise.</returns>
		/// <param name="polygon1">Source polygon 1.</param>
		/// <param name="polygon2">Source polygon 2.</param>
		/// <param name="matchedEdge1">Matched edge from Polygon1.</param>
		/// <param name="matchedEdge2">Matched edge from Polygon2.</param>
		public static bool FindSharedEdge(Polygon polygon1, Polygon polygon2, out Edge matchedEdge1, out Edge matchedEdge2)
		{
			for (int i = 0; i < polygon1.Vertices.Length; i++) 
			{
				Edge edge1 = new Edge(polygon1.Vertices[i], polygon1.Vertices[(i+1) % polygon1.Vertices.Length]);

				for (int j = 0; j < polygon2.Vertices.Length; j++) 
				{
					Edge edge2 = new Edge(polygon2.Vertices[j], polygon2.Vertices[(j+1) % polygon2.Vertices.Length]);

					if(EdgeMatches(edge1, edge2))
					{
						matchedEdge1 = edge1;
						//						matchedEdge2 = edge2;
						matchedEdge2 = new Edge(edge2.Vertex2, edge2.Vertex1);

						return true;
					}
				}
			}

			// None found
			matchedEdge1 = null;
			matchedEdge2 = null;
			return false;
		}


		/// <summary>
		/// Determines if two provided edges are colinear and share a line segment.
		/// </summary>
		/// <returns><c>true</c>, if the two edges match, <c>false</c> otherwise.</returns>
		/// <param name="edge1">Edge1.</param>
		/// <param name="edge2">Edge2.</param>
		public static bool EdgeMatches(Edge edge1, Edge edge2)
		{
			// First of all determine if the two lines are collinear

			Vector3 direction1 = edge1.Vertex2.Position - edge1.Vertex1.Position;
			Vector3 direction2 = edge2.Vertex2.Position - edge2.Vertex1.Position;

			Vector3 direction1Normalized = direction1.normalized;
			Vector3 direction2Normalized = direction2.normalized;

			float dot = Vector3.Dot(direction1Normalized, direction2Normalized);

			// Are the lines parallel?
			if(dot > 0.999f || dot < -0.999f)
			{
				// The lines are parallel, next calculate perpendicular distance between them

				// Calculate a normal vector perpendicular to the line
				Vector3 normal;

				float upDot = Vector3.Dot(direction1Normalized, Vector3.up);

				if(Mathf.Abs(upDot) > 0.9f)
				{
					normal = Vector3.Cross(Vector3.forward, direction1Normalized).normalized;
				}
				else
				{
					normal = Vector3.Cross(Vector3.up, direction1Normalized).normalized;
				}

				// Calculate the tangent vector
				Vector3 tangent = Vector3.Cross(normal, direction1);

				// Take the offset from a point on each line
				Vector3 offset = edge2.Vertex2.Position - edge1.Vertex1.Position;

				// Find the perpendicular distance between the lines along both normal and tangent directions
				float normalDistance = Vector3.Dot(normal, offset);
				float tangentDistance = Vector3.Dot(tangent, offset);

				// If the perpendicular distance is very small
				if(Mathf.Abs(normalDistance) < 0.0001f
					&& Mathf.Abs(tangentDistance) < 0.0001f)
				{
					// Lines are colinear
					// Check if either segment contains one of the points from the other segment

					float signedDistance = 0;

					Plane edge1Plane1 = new Plane(direction1Normalized, edge1.Vertex2.Position);

					signedDistance = edge1Plane1.GetDistanceToPoint(edge2.Vertex1.Position);
					if(signedDistance >= 0)
					{
						return true;
					}

					signedDistance = edge1Plane1.GetDistanceToPoint(edge2.Vertex2.Position);
					if(signedDistance >= 0)
					{
						return true;
					}

					Plane edge1Plane2 = new Plane(-direction1Normalized, edge1.Vertex1.Position);

					signedDistance = edge1Plane2.GetDistanceToPoint(edge2.Vertex1.Position);
					if(signedDistance <= 0)
					{
						return true;
					}

					signedDistance = edge1Plane2.GetDistanceToPoint(edge2.Vertex2.Position);
					if(signedDistance <= 0)
					{
						return true;
					}

					Plane edge2Plane1 = new Plane(direction2Normalized, edge2.Vertex2.Position);

					signedDistance = edge2Plane1.GetDistanceToPoint(edge1.Vertex1.Position);
					if(signedDistance <= 0)
					{
						return true;
					}

					signedDistance = edge2Plane1.GetDistanceToPoint(edge1.Vertex2.Position);
					if(signedDistance <= 0)
					{
						return true;
					}

					Plane edge2Plane2 = new Plane(-direction2Normalized, edge2.Vertex1.Position);

					signedDistance = edge2Plane2.GetDistanceToPoint(edge1.Vertex1.Position);
					if(signedDistance >= 0)
					{
						return true;
					}

					signedDistance = edge2Plane2.GetDistanceToPoint(edge1.Vertex2.Position);
					if(signedDistance >= 0)
					{
						return true;
					}

					return false;
				}
				else
				{
					// Lines are not colinear, there is a perpendicular gap between them
					return false;
				}
			}
			else
			{
				// Lines are not parallel
				return false;
			}
		}
	}
}
#endif