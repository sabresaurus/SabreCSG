#if UNITY_EDITOR || RUNTIME_CSG

using System.Collections.Generic;

namespace Sabresaurus.SabreCSG.Importers.Quake1
{
    /// <summary>
    /// Represents a Quake 1 Brush.
    /// </summary>
    public class MapBrush
    {
        /// <summary>
        /// The sides of the brush.
        /// </summary>
        public List<MapBrushSide> Sides = new List<MapBrushSide>();

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents this instance.</returns>
        public override string ToString()
        {
            return "Quake 1 Brush " + " (" + Sides.Count + " Sides)";
        }
    }
}

#endif