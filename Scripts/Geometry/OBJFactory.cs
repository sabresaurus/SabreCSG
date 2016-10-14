#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Sabresaurus.SabreCSG
{
	/// <summary>
	/// Export polygons to the OBJ format
	/// </summary>
	public static class OBJFactory
	{
		/// <summary>
		/// Exports the supplied polygons to an OBJ file which is written to disk
		/// </summary>
		/// <param name="path">Filepath to write to.</param>
		/// <param name="transform">If a transform is provided, the vertex positions and normals are converted from world space to local to the transform.</param>
		/// <param name="polygons">Polygons to export.</param>
		/// <param name="defaultMaterial">Default material to use if none present on polygon.</param>
		public static void ExportToFile (string path, Transform transform, List<Polygon> polygons, Material defaultMaterial)
		{
			string exportedText = ExportToString(transform, polygons, defaultMaterial);

			// Use a StreamWriter as File.WriteAllText isn't available on Web Player even in Editor
			using (StreamWriter sw = new StreamWriter(path)) 
			{
				sw.Write(exportedText);
			}
		}


		/// <summary>
		/// Exports the supplied polygons to a OBJ format string, typically you'll use ExportToFile rather than ExportToString
		/// </summary>
		/// <returns>The OBJ file contents.</returns>
		/// <param name="transform">If a transform is provided, the vertex positions and normals are converted from world space to local to the transform.</param>
		/// <param name="polygons">Polygons to export.</param>
		/// <param name="defaultMaterial">Default material to use if none present on polygon.</param>
		public static string ExportToString(Transform transform, List<Polygon> polygons, Material defaultMaterial)
		{
			// If a transform is provided, convert the world positions and normals to local to the transform
			if(transform != null)
			{
				for (int polygonIndex = 0; polygonIndex < polygons.Count; polygonIndex++) 
				{
					Polygon polygon = polygons[polygonIndex];
					for (int vertexIndex = 0; vertexIndex < polygon.Vertices.Length; vertexIndex++) 
					{
						Vertex vertex = polygon.Vertices[vertexIndex];
						vertex.Position = transform.InverseTransformPoint(vertex.Position);
						vertex.Normal = transform.InverseTransformDirection(vertex.Normal);
					}
				}
			}

			// Create polygon subsets for each material
			Dictionary<Material, List<Polygon>> polygonMaterialTable = new Dictionary<Material, List<Polygon>>();

			// Iterate through every polygon adding it to the appropiate material list
			foreach (Polygon polygon in polygons)
			{
				if(polygon.UserExcludeFromFinal)
				{
					continue;
				}

				Material material = polygon.Material;
				if(material == null)
				{
					material = defaultMaterial;
				}
				if (!polygonMaterialTable.ContainsKey(material))
				{
					polygonMaterialTable.Add(material, new List<Polygon>());
				}

				polygonMaterialTable[material].Add(polygon);
			}

			// Use a string builder as this should allow faster concatenation
			StringBuilder stringBuilder = new StringBuilder();

			OBJVertexList vertexList = new OBJVertexList();

			int positionIndexOffset = 0;
			int uvIndexOffset = 0;
			int normalIndexOffset = 0;

			int meshIndex = 1;
			// Create a separate mesh for polygons of each material so that we batch by material
			foreach (KeyValuePair<Material, List<Polygon>> polygonMaterialGroup in polygonMaterialTable)
			{                
				List<List<OBJFaceVertex>> faces = new List<List<OBJFaceVertex>>(polygonMaterialGroup.Value.Count);

				// Iterate through every polygon and triangulate
				foreach (Polygon polygon in polygonMaterialGroup.Value)
				{
					List<OBJFaceVertex> faceVertices = new List<OBJFaceVertex>(polygon.Vertices.Length);

					for (int i = 0; i < polygon.Vertices.Length; i++)
					{
						OBJFaceVertex faceVertex = vertexList.AddOrGet(polygon.Vertices[i]);
						faceVertices.Add(faceVertex);
					}

					faces.Add(faceVertices);
				}

				List<Vector3> positions = vertexList.Positions;
				List<Vector2> uvs = vertexList.UVs;
				List<Vector3> normals = vertexList.Normals;

				// Start a new group for the mesh
				stringBuilder.AppendLine("g Mesh"+meshIndex);

				// Write all the positions
				stringBuilder.AppendLine("# Vertex Positions: " + (positions.Count));

				for (int i = 0; i < positions.Count; i++) 
				{
					stringBuilder.AppendLine("v " + WriteVector3(positions[i]));
				}

				// Write all the texture coordinates (UVs)
				stringBuilder.AppendLine("# Vertex UVs: " + (uvs.Count));

				for (int i = 0; i < uvs.Count; i++) 
				{
					stringBuilder.AppendLine("vt " + WriteVector2(uvs[i]));
				}

				// Write all the normals
				stringBuilder.AppendLine("# Vertex Normals: " + (normals.Count));

				for (int i = 0; i < normals.Count; i++) 
				{
					stringBuilder.AppendLine("vn " + WriteVector3(normals[i]));
				}

				// Write all the faces
				stringBuilder.AppendLine("# Faces: " + faces.Count);
				
				for (int i = 0; i < faces.Count; i++) 
				{
					stringBuilder.Append("f ");
					for (int j = faces[i].Count-1; j >=0; j--) 
					{
						stringBuilder.Append((faces[i][j].PositionIndex + positionIndexOffset) + "/" + (faces[i][j].UVIndex + uvIndexOffset) + "/" + (faces[i][j].NormalIndex + normalIndexOffset) + " ");
					}
					stringBuilder.AppendLine();
				}

				// Add some padding between this and the next mesh
				stringBuilder.AppendLine();
				stringBuilder.AppendLine();

				meshIndex++;

				// Update the offsets so that the next pass only writes new vertex information
				positionIndexOffset += positions.Count;
				uvIndexOffset += uvs.Count;
				normalIndexOffset += normals.Count;

                vertexList.ResetForNext();
            }

			return stringBuilder.ToString();
		}

		private static string WriteVector3(Vector3 vector)
		{
			if(float.IsNaN(vector.x) || float.IsInfinity(vector.x))
			{
				vector.x = 0;
			}
			if(float.IsNaN(vector.y) || float.IsInfinity(vector.y))
			{
				vector.y = 0;
			}
			if(float.IsNaN(vector.z) || float.IsInfinity(vector.z))
			{
				vector.z = 0;
			}
					
			// Flip X coordinate to make it use the same as OBJ format
			// (Think this is a handedness difference between OBJ and Unity)
			vector.x = - vector.x;

			return vector.x + " " + vector.y + " " + vector.z;
		}

		private static string WriteVector2(Vector2 vector)
		{
			if(float.IsNaN(vector.x) || float.IsInfinity(vector.x))
			{
				vector.x = 0;
			}
			if(float.IsNaN(vector.y) || float.IsInfinity(vector.y))
			{
				vector.y = 0;
			}

			return vector.x + " " + vector.y;
		}
	}
}
#endif