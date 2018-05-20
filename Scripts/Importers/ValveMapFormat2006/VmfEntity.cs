#if UNITY_EDITOR || RUNTIME_CSG

using System;
using System.Collections.Generic;

namespace Sabresaurus.SabreCSG.Importers.ValveMapFormat2006
{
    /// <summary>
    /// Represents a Hammer Entity.
    /// </summary>
    public class VmfEntity
    {
        public int Id = -1;

        /// <summary>
        /// The class name of the entity.
        /// </summary>
        public string ClassName;

        /// <summary>
        /// The solids in the entity if available.
        /// </summary>
        public List<VmfSolid> Solids = new List<VmfSolid>();

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents this instance.</returns>
        public override string ToString()
        {
            return "VmfEntity " + ClassName + " " + Id + " (" + Solids.Count + " Solids)";
        }
    }
}

#endif