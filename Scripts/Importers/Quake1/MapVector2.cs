#if UNITY_EDITOR || RUNTIME_CSG

namespace Sabresaurus.SabreCSG.Importers.Quake1
{
    /// <summary>
    /// Represents a Quake 1 Vector2.
    /// </summary>
    public class MapVector2
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
        /// Initializes a new instance of the <see cref="MapVector2"/> class.
        /// </summary>
        /// <param name="x">The x-coordinate of the vector.</param>
        /// <param name="y">The y-coordinate of the vector.</param>
        public MapVector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents this instance.</returns>
        public override string ToString()
        {
            return "Quake 1 Vector3 (X=" + X + ", Y=" + Y + ")";
        }
    }
}

#endif