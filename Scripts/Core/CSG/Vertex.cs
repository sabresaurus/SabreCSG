// Disable warnings for missing == and != operators
#pragma warning disable 0660
#pragma warning disable 0661

#if UNITY_EDITOR || RUNTIME_CSG
using UnityEngine;
using System.Collections;

namespace Sabresaurus.SabreCSG
{
	[System.Serializable]
	public class Vertex : IDeepCopyable<Vertex>
	{
	    public Vector3 Position; // Vertex position
	    public Vector2 UV;
	    //	public Vector2 UV2; // Second UV, i.e. lightmapping UVs
	    public Vector3 Normal;
	    //	public Vector3 Tangent; // Optional, needed for some shaders
		public Color32 Color = UnityEngine.Color.white; // Vertex colour, used for tinting individual verts

		public Vertex() {}

		public Vertex(Vector3 position, Vector3 normal, Vector2 uv)
		{
			this.Position = position;
			this.UV = uv;
			this.Normal = normal;
		}

		public Vertex(Vector3 position, Vector3 normal, Vector2 uv, Color32 color)
		{
			this.Position = position;
			this.UV = uv;
			this.Normal = normal;
			this.Color = color;
		}

		public void FlipNormal()
		{
			// Reverses the normal
			Normal = -Normal;
		}

	    public static Vertex Lerp(Vertex from, Vertex to, float t)
	    {
	        return new Vertex()
	        {
	            Position = Vector3.Lerp(from.Position, to.Position, t),
	            UV = Vector2.Lerp(from.UV, to.UV, t),
	            Normal = Vector3.Lerp(from.Normal, to.Normal, t),
				Color = Color32.Lerp(from.Color, to.Color, t),
	        };
	    }

	    public static bool operator ==(Vertex lhs, Vertex rhs)
	    {
			if(ReferenceEquals(lhs, rhs))
			{
				return true;
			}
			else if(ReferenceEquals(lhs, null) || ReferenceEquals(rhs, null))
			{
				return false;
			}

			return lhs.Position == rhs.Position && lhs.UV == rhs.UV && lhs.Normal == rhs.Normal && lhs.Color.Equals(rhs.Color);
	    }

	    public static bool operator !=(Vertex lhs, Vertex rhs)
	    {
			return !(lhs == rhs);
//			return lhs.Position != rhs.Position || lhs.UV != rhs.UV || lhs.Normal != rhs.Normal || !lhs.Color.Equals(rhs.Color);
	    }

		//	    public overtexPolygonMappingsverride bool Equals(object obj)
//	    {
//	        if (obj is Vertex)
//	        {
//	            return this == (Vertex)obj;
//	        }
//	        else
//	        {
//	            return false;
//	        }
//	    }
	
	//    public override int GetHashCode()
	//    {
	//        return base.GetHashCode();
	//    }

		public Vertex DeepCopy()
		{
			return new Vertex(Position, Normal, UV, Color);
		}

		public override string ToString ()
		{
			return string.Format (string.Format("[Vertex] Pos: {0},{1},{2}", Position.x,Position.y,Position.z));
		}
	}
}
#endif

// Disable warnings for missing == and != operators
#pragma warning restore 0660
#pragma warning restore 0661