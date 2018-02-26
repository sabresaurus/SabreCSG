#if UNITY_EDITOR || RUNTIME_CSG

using System.Collections.Generic;

namespace Sabresaurus.SabreCSG
{
    /// <summary>
    /// An implementation of <see cref="IEqualityComparer{T}"/> that checks whether two <see
    /// cref="Polygon"/> can be considered equal by their unique index.
    /// </summary>
    /// <seealso cref="System.Collections.Generic.IEqualityComparer{Sabresaurus.SabreCSG.Polygon}"/>
    public class PolygonUIDComparer : IEqualityComparer<Polygon>
    {
        /// <summary>
        /// Checks whether two <see cref="Polygon"/> have the same unique index.
        /// </summary>
        /// <param name="a">The first <see cref="Polygon"/>.</param>
        /// <param name="b">The second <see cref="Polygon"/>.</param>
        /// <returns><c>true</c> if the two <see cref="Polygon"/> have the same unique index; otherwise, <c>false</c>.</returns>
        public bool Equals(Polygon a, Polygon b)
        {
            return a.UniqueIndex == b.UniqueIndex;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <param name="obj">The object to hash.</param>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures
        /// like a hash table.
        /// </returns>
        public int GetHashCode(Polygon obj)
        {
            return base.GetHashCode();
        }
    }
}

#endif