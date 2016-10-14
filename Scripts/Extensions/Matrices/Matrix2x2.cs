#if UNITY_EDITOR || RUNTIME_CSG

namespace Sabresaurus.SabreCSG
{
	public struct Matrix2x2
	{
		public float m00;
		public float m10;
		public float m01;
		public float m11;

		public static Matrix2x2 Identity
		{
			get
			{
				return new Matrix2x2()
				{
					m00 = 1,
					m10 = 0,
					m01 = 0,
					m11 = 1,
				};
			}
		}

		public static Matrix2x2 Zero
		{
			get
			{
				return new Matrix2x2()
				{
					m00 = 0,
					m10 = 0,
					m01 = 0,
					m11 = 0,
				};
			}
		}

		public Matrix2x2 Inverse
		{
			get
			{
				float reciprocalDeterminant = 1f / Determinant;

				Matrix2x2 newMatrix = new Matrix2x2()
				{
					m00 = this.m11 * reciprocalDeterminant,
					m10 = -this.m10 * reciprocalDeterminant,
					m01 = -this.m01 * reciprocalDeterminant,
					m11 = this.m00 * reciprocalDeterminant,
				};

				return newMatrix;
			}
		}

		public float Determinant
		{
			get
			{
				return (m00 * m11) - (m01 * m10);
			}
		}

		public override string ToString ()
		{
			return m00+"\t"+
				m10+"\t"+
				m01+"\t"+
				m11;
		}
	}
}
#endif