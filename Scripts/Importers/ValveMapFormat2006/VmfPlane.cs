#if UNITY_EDITOR || RUNTIME_CSG

using System;

namespace Sabresaurus.SabreCSG.Importers.ValveMapFormat2006
{
    /// <summary>
    /// Represents a Hammer Plane.
    /// </summary>
    public class VmfPlane
    {
        /// <summary>
        /// The first point of the plane definition.
        /// </summary>
        public VmfVector3 P1;

        /// <summary>
        /// The second point of the plane definition.
        /// </summary>
        public VmfVector3 P2;

        /// <summary>
        /// The third point of the plane definition.
        /// </summary>
        public VmfVector3 P3;

        /// <summary>
        /// Initializes a new instance of the <see cref="VmfPlane"/> class.
        /// </summary>
        /// <param name="p1">The first point of the plane definition.</param>
        /// <param name="p2">The second point of the plane definition.</param>
        /// <param name="p3">The third point of the plane definition.</param>
        public VmfPlane(VmfVector3 p1, VmfVector3 p2, VmfVector3 p3)
        {
            P1 = p1;
            P2 = p2;
            P3 = p3;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents this instance.</returns>
        public override string ToString()
        {
            return "VmfPlane (P1=" + P1 + ", P2=" + P2 + ", P3=" + P3 + ")";
        }
    }
}

#endif