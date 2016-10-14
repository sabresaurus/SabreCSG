#if UNITY_EDITOR || RUNTIME_CSG
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Sabresaurus.SabreCSG
{
	/// <summary>
	/// Used to manipulate polygon vertices
	/// </summary>
	public static class VertexUtility
	{
		/// <summary>
		/// Weld all the specified vertices to their average center.
		/// </summary>
		/// <returns>The output polygons after welding has occurred.</returns>
		/// <param name="sourcePolygons">Source polygons, typically the brush polygons.</param>
		/// <param name="sourceVertices">Source vertices to weld, typically this should all vertices that share the same position so that all the polygons are updated.</param>
		public static Polygon[] WeldVerticesToCenter(Polygon[] sourcePolygons, List<Vertex> sourceVertices)
		{
			// Picks the average position of the selected vertices and sets all their 
			// positions to that position. Duplicate vertices and polygons are then removed.
			VertexWeldOperation vertexOperation = new VertexWeldCentroidOperation(sourcePolygons, sourceVertices);
			return vertexOperation.Execute().ToArray();
		}

		/// <summary>
		/// Welds all specified vertices within a specific tolerance
		/// </summary>
		/// <returns>The nearby vertices.</returns>
		/// <param name="tolerance">Maximum distance between two vertices that will allow welding.</param>
		/// <param name="sourcePolygons">Source polygons, typically the brush polygons.</param>
		/// <param name="sourceVertices">Source vertices to weld, typically this should all vertices that share the same position so that all the polygons are updated.</param>
		public static Polygon[] WeldNearbyVertices(float tolerance, Polygon[] sourcePolygons, List<Vertex> sourceVertices)
		{
			// Takes the selected vertices and welds together any of them that are within the tolerance distance of 
			// other vertices. Duplicate vertices and polygons are then removed.
			VertexWeldOperation vertexOperation = new VertexWeldToleranceOperation(sourcePolygons, sourceVertices, tolerance);
			return vertexOperation.Execute().ToArray();
		}

		/// <summary>
		/// Determines if two index in a given range are contiguous (including wrapping)
		/// </summary>
		/// <returns><c>true</c>, if they are neighbours, <c>false</c> otherwise.</returns>
		/// <param name="index1">Index1.</param>
		/// <param name="index2">Index2.</param>
		/// <param name="length">Length.</param>
		private static bool AreNeighbours(int index1, int index2, int length)
		{
			// First check the wrap points
			if(index1 == length-1 && index2 == 0
				|| index2 == length-1 && index1 == 0)
			{
				return true;
			}
			else if(index2-index1 == 1
				|| index1-index2 == 1) // Now check normally
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Connect the specified vertices, creating new wedges between them (and splitting original polygons)
		/// </summary>
		/// <returns>The new polygons.</returns>
		/// <param name="polygons">Source polygons.</param>
		/// <param name="sourceVertices">Source vertices that will be connected when they are on a polygon and aren't next to each other.</param>
		/// <param name="newEdges">New edges created by connecting source vertices.</param>
		public static Polygon[] ConnectVertices(Polygon[] polygons, List<Vertex> sourceVertices, out List<Edge> newEdges)
		{
			List<Polygon> newPolygons = new List<Polygon>(polygons);

			newEdges = new List<Edge>();

			for (int i = 0; i < newPolygons.Count; i++) 
			{
				// Source vertices on the polygon
				int matchedIndex1 = -1;
				int matchedIndex2 = -1;

				for (int j = 0; j < sourceVertices.Count; j++) 
				{
					int index = System.Array.IndexOf(newPolygons[i].Vertices, sourceVertices[j]);

					if(index != -1)
					{
						if(matchedIndex1 == -1)
						{
							matchedIndex1 = index;
						}
						else if(matchedIndex2 == -1)
						{
							matchedIndex2 = index;
						}
					}
				}

				// Check that found two valid points and that they're not neighbours 
				// (neighbouring vertices can't be connected as they already are by an edge)
				if(matchedIndex1 != -1 && matchedIndex2 != -1
					&& !AreNeighbours(matchedIndex1, matchedIndex2, newPolygons[i].Vertices.Length)) 
				{
//					Vertex neighbourVertex = newPolygons[i].Vertices[(matchedIndex1 + 1) % newPolygons[i].Vertices.Length];
//
//					Vector3 vector1 = newPolygons[i].Vertices[matchedIndex1].Position - neighbourVertex.Position;
//					Vector3 vector2 = newPolygons[i].Vertices[matchedIndex2].Position - newPolygons[i].Vertices[matchedIndex1].Position;
//					Vector3 normal = Vector3.Cross(vector1, vector2).normalized;
//
//					Vector3 thirdPoint = newPolygons[i].Vertices[matchedIndex1].Position + normal;
					Vector3 thirdPoint = newPolygons[i].Vertices[matchedIndex1].Position + newPolygons[i].Plane.normal;

					// First split the shared polygon
					Plane splitPlane = new Plane(newPolygons[i].Vertices[matchedIndex1].Position, newPolygons[i].Vertices[matchedIndex2].Position, thirdPoint);

					Polygon splitPolygon1;
					Polygon splitPolygon2;
					Vertex newVertex1;
					Vertex newVertex2;

					if(Polygon.SplitPolygon(newPolygons[i], out splitPolygon1, out splitPolygon2, out newVertex1, out newVertex2, splitPlane))
					{
						newPolygons[i] = splitPolygon1;
						newPolygons.Insert(i+1, splitPolygon2);
						// Skip over new polygon
						i++;
						newEdges.Add(new Edge(newVertex1, newVertex2));
					}
					else
					{
						Debug.LogWarning("Split polygon failed");
					}
				}
			}
			return newPolygons.ToArray();
		}

//		public static Polygon[] RemoveVertices(Polygon[] polygons, List<Vertex> sourceVertices)
//		{
//			List<Polygon> polygonsToAdd = new List<Polygon>();
//
//			for (int j = 0; j < sourceVertices.Count; j++) 
//			{
//				// Every time we remove a vertex, track it's neighbours in case we need to generate new polygons
//				List<Vector3> neighbourVertices = new List<Vector3>();
//
//				for (int i = 0; i < polygons.Length; i++) 
//				{
//					List<Vertex> vertices = new List<Vertex>(polygons[i].Vertices);
//
//					for (int k = 0; k < vertices.Count; k++) 
//					{
//						// If the vertex should be deleted
//						if(vertices[k].Position.EqualsWithEpsilon(sourceVertices[j].Position))
//						{
//							// Find the neighbours
//							int previousNeighbour = MathHelper.Wrap(k-1, vertices.Count);
//							int nextNeighbour = MathHelper.Wrap(k+1, vertices.Count);
//
//							// Track the neighbours
//							neighbourVertices.Add(vertices[previousNeighbour].Position);
//							neighbourVertices.Add(vertices[nextNeighbour].Position);
//
//							// Delete the vertex
//							vertices.RemoveAt(k);
//							k--;
//						}
//					}
//
//					// Update the polygon with the deleted vertices
//					Vertex[] vertexArray = new Vertex[vertices.Count];
//					vertices.CopyTo(vertexArray);
//					polygons[i].SetVertices(vertexArray);
//				}
//
//				// If a vertex has been deleted that shared 3 faces
////				if(neighbourVertices.Count >= 6)
////				{
////					// Generate a polygon from the positions, stripping out duplicate positions
////					Polygon polygon = PolygonFactory.ConstructPolygon(neighbourVertices, true);
////
////					// If we were successful in generating a polygon
////					if(polygon != null)
////					{
////						polygonsToAdd.Add(polygon);
////					}
////				}
//			}
//
//			if(polygonsToAdd.Count > 0)
//			{
//				// Concat the new polygons into the array
//				int originalLength = polygons.Length;
//				System.Array.Resize(ref polygons, originalLength + polygonsToAdd.Count);
//				polygonsToAdd.CopyTo(polygons, originalLength);
//			}
//
//			return polygons;
//		}

		/// <summary>
		/// Displace the polygons along their polygon normals by the specified distance, adjusting vertex positions in 3 dimensions so that connected edges are preserved.
		/// </summary>
		/// <param name="polygons">Polygons to displace in situ.</param>
		/// <param name="distance">Distance to displace the polygons.</param>
		public static void DisplacePolygons(Polygon[] polygons, float distance)
		{
			// Used for determining if two vertices are the same
			Polygon.VertexComparerEpsilon vertexComparer = new Polygon.VertexComparerEpsilon();
			// Used for determining if two positions or normals are the same
			Polygon.Vector3ComparerEpsilon vectorComparer = new Polygon.Vector3ComparerEpsilon();

			// Group overlapping positions and also track their normals
			List<List<Vertex>> groupedVertices = new List<List<Vertex>>();
			List<List<Vector3>> groupedNormals = new List<List<Vector3>>();

			// Maps back from a vertex to the polygon it came from, used for UV calculation
			Dictionary<Vertex, Polygon> vertexPolygonMappings = new Dictionary<Vertex, Polygon>();

			for (int polygonIndex = 0; polygonIndex < polygons.Length; polygonIndex++) 
			{
				Vertex[] vertices = polygons[polygonIndex].Vertices;

				// Group the selected vertices into clusters
				for (int vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++) 
				{
					Vertex sourceVertex = vertices[vertexIndex];

					vertexPolygonMappings[sourceVertex] = polygons[polygonIndex];

					bool added = false;

					for (int groupIndex = 0; groupIndex < groupedVertices.Count; groupIndex++) 
					{
						if(groupedVertices[groupIndex].Contains(sourceVertex, vertexComparer))
						{
							groupedVertices[groupIndex].Add(sourceVertex);
							// Add the normal of the polygon if it hasn't already been added (this prevents issues with two polygons that are coplanar)
							if(!groupedNormals[groupIndex].Contains(polygons[polygonIndex].Plane.normal, vectorComparer))
							{
								groupedNormals[groupIndex].Add(polygons[polygonIndex].Plane.normal);
							}
							added = true;
							break;
						}
					}

					if(!added)
					{
						groupedVertices.Add(new List<Vertex>() { sourceVertex } );
						groupedNormals.Add(new List<Vector3>() { polygons[polygonIndex].Plane.normal } );
					}
				}
			}

			List<List<Vector3>> groupedPositions = new List<List<Vector3>>();
			List<List<Vector2>> groupedUV = new List<List<Vector2>>();

			// Calculate the new positions and UVs, but don't assign them as they must be calculated in one go
			for (int i = 0; i < groupedVertices.Count; i++) 
			{
				groupedPositions.Add(new List<Vector3>());
				groupedUV.Add(new List<Vector2>());

				for (int j = 0; j < groupedVertices[i].Count; j++) 
				{
					Vector3 position = groupedVertices[i][j].Position;
					for (int k = 0; k < groupedNormals[i].Count; k++) 
					{
						position += groupedNormals[i][k] * distance;
					}
					Polygon primaryPolygon = vertexPolygonMappings[groupedVertices[i][j]];

					Vector2 uv = GeometryHelper.GetUVForPosition(primaryPolygon, position);
					groupedPositions[i].Add(position);
					groupedUV[i].Add(uv);
				}
			}

			// Apply the new positions and UVs now that they've all been calculated
			for (int i = 0; i < groupedVertices.Count; i++) 
			{
				for (int j = 0; j < groupedVertices[i].Count; j++) 
				{
					Vertex vertex = groupedVertices[i][j];
					vertex.Position = groupedPositions[i][j];
					vertex.UV = groupedUV[i][j];
				}
			}

			// Polygon planes have moved, so recalculate them
			for (int polygonIndex = 0; polygonIndex < polygons.Length; polygonIndex++) 
			{
				polygons[polygonIndex].CalculatePlane();
			}
		}

		/// <summary>
		/// Translates the specified vertices by a position delta (local to the brush) and updates the UVs
		/// </summary>
		/// <param name="brush">Brush from which the vertices belong.</param>
		/// <param name="specifiedVertices">Specified vertices to be translated.</param>
		/// <param name="localDelta">Local positional delta.</param>
		public static void TranslateSpecifiedVertices(Brush brush, List<Vertex> specifiedVertices, Vector3 localDelta)
		{
			Polygon.Vector3ComparerEpsilon positionComparer = new Polygon.Vector3ComparerEpsilon();

			// Cache the positions as the position of vertices will change while in the for loop
			List<Vector3> specifiedPositions = specifiedVertices.Select(item => item.Position).ToList();

			// So we know which polygons need to have their normals recalculated
			List<Polygon> affectedPolygons = new List<Polygon>();

			Polygon[] polygons = brush.GetPolygons();

			for (int i = 0; i < polygons.Length; i++) 
			{
				Polygon polygon = polygons[i];

				int vertexCount = polygon.Vertices.Length;

				Vector3[] newPositions = new Vector3[vertexCount];
				Vector2[] newUV = new Vector2[vertexCount];

				for (int j = 0; j < vertexCount; j++) 
				{
					newPositions[j] = polygon.Vertices[j].Position;
					newUV[j] = polygon.Vertices[j].UV;
				}

				bool polygonAffected = false;

				for (int j = 0; j < vertexCount; j++) 
				{
					Vertex vertex = polygon.Vertices[j];
					if(specifiedPositions.Contains(vertex.Position, positionComparer))
					{
						Vector3 newPosition = vertex.Position + localDelta;

						newPositions[j] = newPosition;

						newUV[j] = GeometryHelper.GetUVForPosition(polygon, newPosition);

						polygonAffected = true;
					}
				}

				if(polygonAffected)
				{
					affectedPolygons.Add(polygon);
				}

				// Apply all the changes to the polygon
				for (int j = 0; j < vertexCount; j++) 
				{
					Vertex vertex = polygon.Vertices[j];
					vertex.Position = newPositions[j];
					vertex.UV = newUV[j];
				}

				polygon.CalculatePlane();
			}

			if(affectedPolygons.Count > 0)
			{
				for (int i = 0; i < affectedPolygons.Count; i++) 
				{
					affectedPolygons[i].ResetVertexNormals();
				}
			}
		}
	}
}
#endif