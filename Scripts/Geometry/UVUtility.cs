#if UNITY_EDITOR
using UnityEngine;
using System.Collections;

namespace Sabresaurus.SabreCSG
{
	/// <summary>
	/// Provides utilities for manipulating UVs in a standardised way, used by the Face tool
	/// </summary>
	public static class UVUtility
	{
		public class TransformData
		{
			public Vector2 Vector;
			public float Float1;

			public TransformData(Vector2 vector, float float1)
			{
				this.Vector = vector;
				this.Float1 = float1;
			}
		}

		public delegate Vector2 UVTransformation(Vector2 source, TransformData transformData);

		public static Vector2 RotateUV(Vector2 source, TransformData transformData)
		{
			return transformData.Vector + (source - transformData.Vector).Rotate(transformData.Float1);
		}

		public static Vector2 TranslateUV(Vector2 source, TransformData transformData)
		{
			return source + transformData.Vector;
		}
		
		public static Vector2 ScaleUV(Vector2 source, TransformData transformData)
		{
			return new Vector2(source.x / transformData.Vector.x, source.y / transformData.Vector.y);
		}
		
		public static Vector2 FlipUVX(Vector2 source, TransformData transformData)
		{
			source.x = 1-source.x;
			return source;
		}
		
		public static Vector2 FlipUVY(Vector2 source, TransformData transformData)
		{
			source.y = 1-source.y;
			return source;
		}

		public static Vector2 FlipUVXY(Vector2 source, TransformData transformData)
		{
			return new Vector2(source.y,source.x);
		}
	}
}
#endif