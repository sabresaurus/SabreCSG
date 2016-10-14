#if UNITY_EDITOR || RUNTIME_CSG
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Sabresaurus.SabreCSG
{
	/// <summary>
	/// Provides helper methods for Unity Mesh objects
	/// </summary>
	public static class MeshHelper
	{
		/// <summary>
		/// Displace all the vertices along their normals by the displacement distance. Note this will create cracks if normals are faceted and displacement values that aren't very low are used.
		/// </summary>
		/// <param name="mesh">Mesh to displace.</param>
		/// <param name="displacement">Displacement distance.</param>
		public static void Displace(ref Mesh mesh, float displacement)
		{
			Vector3[] vertices = mesh.vertices;
			for (int i = 0; i < mesh.vertices.Length; i++)
			{
				vertices[i] += mesh.normals[i] * displacement;
			}
			mesh.vertices = vertices;
		}

		/// <summary>
		/// Flips the specified mesh inside out
		/// </summary>
		/// <param name="mesh">Mesh.</param>
		public static void Invert(ref Mesh mesh)
		{
			Vector3[] normals = mesh.normals;
			for (int i = 0; i < mesh.normals.Length; i++)
			{
				normals[i] *= -1;
			}
			int[] triangles = mesh.triangles;

			for (int i = 0; i < triangles.Length; i+=3) 
			{
				int x1 = triangles[i+0];
				int x2 = triangles[i+1];
				int x3 = triangles[i+2];

				triangles[i+2] = x1;
				triangles[i+1] = x2;
				triangles[i+0] = x3;
			}

			mesh.triangles = triangles;
			mesh.normals = normals;
		}
	}
}
#endif