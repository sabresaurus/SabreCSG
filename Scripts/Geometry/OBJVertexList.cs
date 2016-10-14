#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
	public struct OBJFaceVertex
	{
		public int PositionIndex; // Starts at 1, not 0
		public int UVIndex; // Starts at 1, not 0
		public int NormalIndex; // Starts at 1, not 0
	}

	public class OBJVertexList
	{
		List<Vector3> positions = new List<Vector3>();
		List<Vector2> uvs = new List<Vector2>();
		List<Vector3> normals = new List<Vector3>();

		public List<Vector3> Positions {
			get {
				return positions;
			}
		}

		public List<Vector2> UVs {
			get {
				return uvs;
			}
		}

		public List<Vector3> Normals {
			get {
				return normals;
			}
		}

		public void ResetForNext ()
		{
            positions.Clear();
            uvs.Clear();
            normals.Clear();
        }
		
		/// <summary>
		/// This will find if the vertex is already contained and return its index, otherwise it adds it as new
		/// </summary>
		public OBJFaceVertex AddOrGet(Vertex vertex)
		{
			OBJFaceVertex face = new OBJFaceVertex();

			// Position
			for (int i = 0; i < positions.Count; i++)
			{
				if (positions[i] == vertex.Position)
				{
					face.PositionIndex = i+1;
				}
			}

			if(face.PositionIndex == 0)
			{
				positions.Add(vertex.Position);
				face.PositionIndex = positions.Count;
			}

			// UV
			for (int i = 0; i < uvs.Count; i++)
			{
				if (uvs[i] == vertex.UV)
				{
					face.UVIndex = i+1;
				}
			}
			
			if(face.UVIndex == 0)
			{
				uvs.Add(vertex.UV);
				face.UVIndex = uvs.Count;
			}

			// Normal
			for (int i = 0; i < normals.Count; i++)
			{
				if (normals[i] == vertex.Normal)
				{
					face.NormalIndex = i+1;
				}
			}
			
			if(face.NormalIndex == 0)
			{
				normals.Add(vertex.Normal);
				face.NormalIndex = normals.Count;
			}

			return face;
		}
//		
//		public int Add(Vertex vertex)
//		{
//			// None found, so add it and return the new index
//			vertices.Add(vertex);
//			return vertices.Count - 1;
//		}
	}
}
#endif