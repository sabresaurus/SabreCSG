#if UNITY_EDITOR || RUNTIME_CSG
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
	public struct Matrix3x2
	{
		public float m00;
		public float m10;
		public float m01;
		public float m11;
		public float m02;
		public float m12;

		public Matrix3x2 Multiply(Matrix2x2 o)
		{
			return new Matrix3x2()
			{
				m00 = this.m00 * o.m00 + this.m10 * o.m01,
				m10 = this.m00 * o.m10 + this.m10 * o.m11,

				m01 = this.m01 * o.m00 + this.m11 * o.m01,
				m11 = this.m01 * o.m10 + this.m11 * o.m11,

				m02 = this.m02 * o.m00 + this.m12 * o.m01,
				m12 = this.m02 * o.m10 + this.m12 * o.m11,
			};
		}

		public Vector2 Multiply(Vector3 o)
		{
			return new Vector2()
			{
				x = this.m00 * o.x + this.m01 * o.y + this.m02 * o.z,
				y = this.m10 * o.x + this.m11 * o.y + this.m12 * o.z,
			};
		}

		public override string ToString ()
		{
			return m00+"\t"+
				m10+"\t"+
				m01+"\t"+
				m11+"\t"+
				m02+"\t"+
				m12;
		}
	}
}
#endif