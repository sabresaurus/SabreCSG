#if UNITY_EDITOR || RUNTIME_CSG
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Sabresaurus.SabreCSG
{
	public abstract class VertexWeldOperation
	{
		private Polygon[] sourcePolygons;
		protected List<Vertex> sourceVertices;
		protected List<Polygon> polygons;
		protected Vector3[] previousPlaneNormals;
		protected Dictionary<Vertex, Polygon> vertexPolygonMappings;

		public VertexWeldOperation (Polygon[] sourcePolygons, List<Vertex> sourceVertices)
		{
			this.sourcePolygons = sourcePolygons;
			this.sourceVertices = sourceVertices;
		}

		// Once constructed, this method is used to retrieve the calculated polygons
		public List<Polygon> Execute()
		{
			BeginWeld();

			PerformWeld();

			EndWeld();

			return polygons;
		}

		protected void BeginWeld()
		{
			//polygons = new List<Polygon>(sourcePolygons.DeepCopy());
			polygons = new List<Polygon>(sourcePolygons);

			// Cache the previous polygon normals, so we can correctly calculate the new vertex normals
			previousPlaneNormals = new Vector3[polygons.Count];

			for (int i = 0; i < polygons.Count; i++) 
			{
				previousPlaneNormals[i] = polygons[i].Plane.normal;
			}

			// Calculate mappings from each vertex to the polygon it came from
			vertexPolygonMappings = GetPolygonMappings(sourcePolygons, sourceVertices);
		}

		public abstract void PerformWeld();

		protected void EndWeld()
		{
			for (int i = 0; i < polygons.Count; i++) 
			{
				// Remove any duplicate vertices
				polygons[i].RemoveExtraneousVertices();

				// Update the vertex normals
				if(polygons[i].Vertices.Length >= 3)
				{
					Vector3 previousPlaneNormal = previousPlaneNormals[i];

					// Polygon geometry has changed, inform the polygon that it needs to recalculate its cached plane
					polygons[i].CalculatePlane();

					Vector3 newPlaneNormal = polygons[i].Plane.normal;

					// Find the rotation from the original polygon plane to the new polygon plane
					Quaternion normalRotation = Quaternion.FromToRotation(previousPlaneNormal, newPlaneNormal);

					int vertexCount = polygons[i].Vertices.Length;
					// Rotate all the vertex normals by the new rotation
					for (int j = 0; j < vertexCount; j++) 
					{
						Vertex vertex = polygons[i].Vertices[j];
						vertex.Normal = normalRotation * vertex.Normal;
					}
				}
			}

			// Finally, remove faces that now have < 3 vertices
			for (int i = 0; i < polygons.Count; i++) 
			{
				if(polygons[i].Vertices.Length < 3)
				{
					polygons.RemoveAt(i);
					i--;
				}
			}
		}


		/// <summary>
		/// Given a set of polygons and a set of interesting vertices, this will create a dictionary mapping each interesting 
		/// vertex to the polygon it came from
		/// </summary>
		private static Dictionary<Vertex, Polygon> GetPolygonMappings(Polygon[] sourcePolygons, List<Vertex> sourceVertices)
		{
			Dictionary<Vertex, Polygon> mappings = new Dictionary<Vertex, Polygon>(sourceVertices.Count);

			for (int vertexIndex = 0; vertexIndex < sourceVertices.Count; vertexIndex++) 
			{
				// The vertex we are interested in
				Vertex vertex = sourceVertices[vertexIndex];
				// The polygon that contains the vertex
				Polygon matchedPolygon = null;
				// Walk through all the provided polygons and see if any contain the target vertex
				for (int polygonIndex = 0; polygonIndex < sourcePolygons.Length; polygonIndex++) 
				{
					if(sourcePolygons[polygonIndex].Vertices.Contains(vertex))
					{
						// Match found, this polygon contains the interested vertex
						matchedPolygon = sourcePolygons[polygonIndex];
						break;
					}
				}
				// Add the mapping to the dictionary
				mappings.Add(vertex, matchedPolygon);
			}
			return mappings;
		}
	}
}
#endif