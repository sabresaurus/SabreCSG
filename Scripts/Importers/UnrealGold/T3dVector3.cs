#if UNITY_EDITOR || RUNTIME_CSG

using System;

namespace Sabresaurus.SabreCSG.Importers.UnrealGold
{
    /// <summary>
    /// Represents an Unreal Editor 1 vector type.
    /// </summary>
    public class T3dVector3
    {
        /// <summary>
        /// Gets or sets the x-coordinate.
        /// </summary>
        /// <value>The x-coordinate.</value>
        public float X { get; set; }

        /// <summary>
        /// Gets or sets the y-coordinate.
        /// </summary>
        /// <value>The y-coordinate.</value>
        public float Y { get; set; }

        /// <summary>
        /// Gets or sets the z-coordinate.
        /// </summary>
        /// <value>The z-coordinate.</value>
        public float Z { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="T3dVector3"/> class.
        /// </summary>
        public T3dVector3()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T3dVector3"/> class.
        /// </summary>
        /// <param name="x">The x-coordinate of the vector.</param>
        /// <param name="y">The y-coordinate of the vector.</param>
        /// <param name="z">The z-coordinate of the vector.</param>
        public T3dVector3(float x, float y, float z)
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
            return "X: " + X + ", Y: " + Y + ", Z: " + Z;
        }
    }
}

#endif