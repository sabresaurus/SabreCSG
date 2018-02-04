#if UNITY_EDITOR || RUNTIME_CSG
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Sabresaurus.SabreCSG
{
	public class VertexWeldCentroidOperation : VertexWeldOperation
	{
		// Picks the average position of the selected vertices and sets all their 
		// positions to that position. Duplicate vertices and polygons are then removed.
		public VertexWeldCentroidOperation (Polygon[] sourcePolygons, List<Vertex> sourceVertices)
			: base(sourcePolygons, sourceVertices)
		{
			
		}

		public override void PerformWeld ()
		{
			// New position for the vertices is their center
			Vector3 newPosition = Vector3.zero;
			for (int i = 0; i < sourceVertices.Count; i++) 
			{
				newPosition += sourceVertices[i].Position;
			}
			newPosition /= sourceVertices.Count;

			// Update all the selected vertices UVs
			for (int i = 0; i < sourceVertices.Count; i++) 
			{
				Polygon polygon = vertexPolygonMappings[sourceVertices[i]];
				sourceVertices[i].UV = GeometryHelper.GetUVForPosition(polygon, newPosition);
			}

			// Update all the selected vertices to their new position
			for (int i = 0; i < sourceVertices.Count; i++) 
			{
				sourceVertices[i].Position = newPosition;
			}
		}
	}
}
#endif