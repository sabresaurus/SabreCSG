#if UNITY_EDITOR || RUNTIME_CSG

using System;
using System.Collections.Generic;

namespace Sabresaurus.SabreCSG.Importers.ValveMapFormat2006
{
    /// <summary>
    /// Represents a Hammer Solid.
    /// </summary>
    public class VmfSolid
    {
        public int Id = -1;

        /// <summary>
        /// The sides of the solid.
        /// </summary>
        public List<VmfSolidSide> Sides = new List<VmfSolidSide>();

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents this instance.</returns>
        public override string ToString()
        {
            return "VMF Solid " + Id + " (" + Sides.Count + " Sides)";
        }
    }
}

#endif