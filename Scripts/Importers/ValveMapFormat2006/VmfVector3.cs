#if UNITY_EDITOR || RUNTIME_CSG

using System;

namespace Sabresaurus.SabreCSG.Importers.ValveMapFormat2006
{
    /// <summary>
    /// Represents a Hammer Vector3.
    /// </summary>
    public class VmfVector3
    {
        /// <summary>
        /// The x-coordinate of the vector.
        /// </summary>
        public float X;

        /// <summary>
        /// The y-coordinate of the vector.
        /// </summary>
        public float Y;

        /// <summary>
        /// The z-coordinate of the vector.
        /// </summary>
        public float Z;

        /// <summary>
        /// Initializes a new instance of the <see cref="VmfVector3"/> class.
        /// </summary>
        /// <param name="x">The x-coordinate of the vector.</param>
        /// <param name="y">The y-coordinate of the vector.</param>
        /// <param name="z">The z-coordinate of the vector.</param>
        public VmfVector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents this instance.</returns>
        public override string ToString()
        {
            return "VmfVector3 (X=" + X + ", Y=" + Y + ", Z=" + Z + ")";
        }
    }
}

#endif