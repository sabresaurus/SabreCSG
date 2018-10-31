#if UNITY_EDITOR || RUNTIME_CSG
using System;
using System.Reflection;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
	[Serializable]
	public class PolygonEntry : IDeepCopyable<PolygonEntry>
	{
		[SerializeField]
		public Vector3[] Positions;

		[SerializeField]
		public Vector3[] Normals;

		[SerializeField]
		public Vector2[] UV;

		[SerializeField]
		public Color32[] Colors;

		[SerializeField]
		public int[] Triangles;

		[SerializeField]
		public Material Material;

		[SerializeField]
		public bool ExcludeFromBuild;

		// Visual
		[SerializeField]
		public Mesh BuiltMesh;

		[SerializeField]
		public int BuiltVertexOffset;

		[SerializeField]
		public int BuiltTriangleOffset;

		public PolygonEntry (Vector3[] positions, 
			Vector3[] normals, 
			Vector2[] uv, 
			Color32[] colors,
			int[] triangles, 
			Material material,
			bool excludeFromBuild
		)
		{
			this.Positions = positions;
			this.Normals = normals;
			this.UV = uv;
			this.Colors = colors;
			this.Triangles = triangles;
			this.Material = material;
			this.ExcludeFromBuild = excludeFromBuild;
		}

		public PolygonEntry DeepCopy()
		{
			return new PolygonEntry(Positions, Normals, UV, Colors, Triangles, Material, ExcludeFromBuild);
		}

		public static bool IsValid(PolygonEntry entry)
		{
			if(entry == null)
			{
				return false;
			}

			// Sometimes the positions array may be null, this is probably an issue with Unity's serializer 
			// populating an empty PolygonEntry but leaving Positions null
			if(entry.Positions == null || entry.UV == null)
			{
				return false;
			}

			return true;
		}

        public static bool IsValidAndBuilt(PolygonEntry entry)
        {
            if (IsValid(entry) && entry.BuiltMesh != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
#endif