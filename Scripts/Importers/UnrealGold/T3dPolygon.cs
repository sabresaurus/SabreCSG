#if UNITY_EDITOR || RUNTIME_CSG

using System;
using System.Collections.Generic;

namespace Sabresaurus.SabreCSG.Importers.UnrealGold
{
    /// <summary>
    /// Represents an Unreal Editor 1 polygon.
    /// </summary>
    public class T3dPolygon
    {
        /// <summary>
        /// Gets or sets the item.
        /// </summary>
        /// <value>The item.</value>
        public string Item { get; set; }

        /// <summary>
        /// Gets or sets the texture.
        /// </summary>
        /// <value>The texture.</value>
        public string Texture { get; set; }

        /// <summary>
        /// Gets or sets the polygon flags.
        /// </summary>
        /// <value>The polygon flags.</value>
        public T3dPolygonFlags Flags { get; set; }

        /// <summary>
        /// Gets or sets the origin.
        /// </summary>
        /// <value>The origin.</value>
        public T3dVector3 Origin { get; set; }

        /// <summary>
        /// Gets or sets the normal.
        /// </summary>
        /// <value>The normal.</value>
        public T3dVector3 Normal { get; set; }

        /// <summary>
        /// Gets or sets the texture u.
        /// </summary>
        /// <value>The texture u.</value>
        public T3dVector3 TextureU { get; set; }

        /// <summary>
        /// Gets or sets the texture v.
        /// </summary>
        /// <value>The texture v.</value>
        public T3dVector3 TextureV { get; set; }

        /// <summary>
        /// Gets or sets the pan u.
        /// </summary>
        /// <value>The pan u.</value>
        public int PanU { get; set; }

        /// <summary>
        /// Gets or sets the pan v.
        /// </summary>
        /// <value>The pan v.</value>
        public int PanV { get; set; }

        /// <summary>
        /// Gets or sets the vertices.
        /// </summary>
        /// <value>The vertices.</value>
        public List<T3dVector3> Vertices /*{ get; }*/ = new List<T3dVector3>();

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents this instance.</returns>
        public override string ToString()
        {
            return "Unreal Engine 1 Brush Polygon \"" + Item + "\" (" + Vertices.Count + " Vertices)";
        }
    }
}

#endif