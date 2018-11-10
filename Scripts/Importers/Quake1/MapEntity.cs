#if UNITY_EDITOR || RUNTIME_CSG

using System.Collections.Generic;

namespace Sabresaurus.SabreCSG.Importers.Quake1
{
    /// <summary>
    /// Represents a Quake 1 Entity.
    /// </summary>
    public class MapEntity
    {
        /// <summary>
        /// The class name of the entity.
        /// </summary>
        public string ClassName;

        /// <summary>
        /// The brushes in the entity if available.
        /// </summary>
        public List<MapBrush> Brushes = new List<MapBrush>();

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents this instance.</returns>
        public override string ToString()
        {
            return "Quake 1 Entity " + ClassName + " (" + Brushes.Count + " Brushes)";
        }
    }
}

#endif