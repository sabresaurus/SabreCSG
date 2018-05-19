#if UNITY_EDITOR || RUNTIME_CSG

using System;
using System.Collections.Generic;

namespace Sabresaurus.SabreCSG.Importers.UnrealGold
{
    /// <summary>
    /// Represents an Unreal Editor 1 brush.
    /// </summary>
    public class T3dBrush
    {
        /// <summary>
        /// Gets the name of the brush.
        /// </summary>
        /// <value>The name of the brush.</value>
        public string Name /*{ get; }*/;

        /// <summary>
        /// Gets or sets the brush polygons.
        /// </summary>
        /// <value>The brush polygons.</value>
        public List<T3dPolygon> Polygons /*{ get; }*/ = new List<T3dPolygon>();

        /// <summary>
        /// Initializes a new instance of the <see cref="T3dBrush"/> class.
        /// </summary>
        /// <param name="name">The name of the brush.</param>
        public T3dBrush(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents this instance.</returns>
        public override string ToString()
        {
            return "Unreal Engine 1 Brush Model \"" + Name + "\" (" + Polygons.Count + " Polygons)";
        }
    }
}

#endif