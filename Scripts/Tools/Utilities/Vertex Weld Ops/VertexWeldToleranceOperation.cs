#if UNITY_EDITOR || RUNTIME_CSG
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Sabresaurus.SabreCSG
{
	public class VertexWeldToleranceOperation : VertexWeldOperation
	{
		protected float tolerance;

		// Takes the selected vertices and welds together any of them that are within the tolerance distance of 
		// other vertices. Duplicate vertices and polygons are then removed.
		public VertexWeldToleranceOperation (Polygon[] sourcePolygons, List<Vertex> sourceVertices, float tolerance)
			: base(sourcePolygons, sourceVertices)
		{
			this.tolerance = tolerance;
		}

		public override void PerformWeld ()
		{
			List<List<Vertex>> groupedVertices = new List<List<Vertex>>();

			VertexComparerTolerance comparer = new VertexComparerTolerance(tolerance);

			// Group the selected vertices into clusters
			for (int sourceVertexIndex = 0; sourceVertexIndex < sourceVertices.Count; sourceVertexIndex++) 
			{
				Vertex sourceVertex = sourceVertices[sourceVertexIndex];

				bool added = false;

				for (int groupIndex = 0; groupIndex < groupedVertices.Count; groupIndex++) 
				{
					if(groupedVertices[groupIndex].Contains(sourceVertex, comparer))
					{
						groupedVertices[groupIndex].Add(sourceVertex);
						added = true;
						break;
					}
				}

				if(!added)
				{
					groupedVertices.Add(new List<Vertex>() { sourceVertex } );
				}
			}


			for (int groupIndex = 0; groupIndex < groupedVertices.Count; groupIndex++) 
			{
				List<Vertex> vertices = groupedVertices[groupIndex];

				// Ignoring any groups that only contain one vertex
				if(vertices.Count > 1)
				{
					// New position for the vertices is their center
					Vector3 newPosition = Vector3.zero;
					for (int vertexIndex = 0; vertexIndex < vertices.Count; vertexIndex++) 
					{
						newPosition += vertices[vertexIndex].Position;
					}
					newPosition /= vertices.Count;

					// Update all the selected vertices UVs
					for (int vertexIndex = 0; vertexIndex < vertices.Count; vertexIndex++) 
					{
						Polygon polygon = vertexPolygonMappings[vertices[vertexIndex]];
						vertices[vertexIndex].UV = GeometryHelper.GetUVForPosition(polygon, newPosition);
					}

					// Update all the selected vertices to their new position
					for (int vertexIndex = 0; vertexIndex < vertices.Count; vertexIndex++) 
					{
						vertices[vertexIndex].Position = newPosition;
					}
				}
			}
		}

		private class VertexComparerTolerance : IEqualityComparer<Vertex>
		{
			float squareTolerance; // Using square distance for comparisons is quicker

			public VertexComparerTolerance (float tolerance)
			{
				this.squareTolerance = tolerance * tolerance;
			}

			public bool Equals (Vertex x, Vertex y)
			{
				float squareMagnitude = (x.Position - y.Position).sqrMagnitude;
				return (squareMagnitude <= squareTolerance);
			}

			public int GetHashCode (Vertex obj)
			{
				// The similarity or difference between two positions can only be calculated if both are supplied
				// when Distinct is called GetHashCode is used to determine which values are in collision first
				// therefore we return the same hash code for all values to ensure all comparisons must use 
				// our Equals method to properly determine which values are actually considered equal
				return 1;
			}
		}
	}
}
#endif
