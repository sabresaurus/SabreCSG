#if UNITY_EDITOR || RUNTIME_CSG

using System;

namespace Sabresaurus.SabreCSG.Importers.ValveMapFormat2006
{
    /// <summary>
    /// Represents a Hammer Solid Side.
    /// </summary>
    public class VmfSolidSide
    {
        public int Id = -1;
        public VmfPlane Plane;
        public string Material;
        public float Rotation;
        public VmfAxis UAxis;
        public VmfAxis VAxis;
        public int LightmapScale;
        public int SmoothingGroups;

        // HACK:
        public bool HasDisplacement = false;

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents this instance.</returns>
        public override string ToString()
        {
            return "VMF Solid Side " + Id + " '" + Material + "' " + " " + Plane;
        }
    }
}

#endif