#if UNITY_EDITOR || RUNTIME_CSG

namespace Sabresaurus.SabreCSG.Importers.Quake1
{
    /// <summary>
    /// Represents a Quake 1 Plane.
    /// </summary>
    public class MapPlane
    {
        /// <summary>
        /// The first point of the plane definition.
        /// </summary>
        public MapVector3 P1;

        /// <summary>
        /// The second point of the plane definition.
        /// </summary>
        public MapVector3 P2;

        /// <summary>
        /// The third point of the plane definition.
        /// </summary>
        public MapVector3 P3;

        /// <summary>
        /// Initializes a new instance of the <see cref="MapPlane"/> class.
        /// </summary>
        /// <param name="p1">The first point of the plane definition.</param>
        /// <param name="p2">The second point of the plane definition.</param>
        /// <param name="p3">The third point of the plane definition.</param>
        public MapPlane(MapVector3 p1, MapVector3 p2, MapVector3 p3)
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
            return "Quake 1 Plane (P1=" + P1 + ", P2=" + P2 + ", P3=" + P3 + ")";
        }
    }
}

#endif