#if UNITY_EDITOR || RUNTIME_CSG

using System.Collections.Generic;

namespace Sabresaurus.SabreCSG
{
    /// <summary>
    /// An implementation of <see cref="IEqualityComparer{T}"/> that checks whether two <see
    /// cref="Vertex"/> can be considered equal by their position alone.
    /// <para>
    /// Floating point inaccuracies are taken into account (see <see
    /// cref="Extensions.EqualsWithEpsilon(UnityEngine.Vector3, UnityEngine.Vector3)"/>).
    /// </para>
    /// </summary>
    /// <seealso cref="System.Collections.Generic.IEqualityComparer{Sabresaurus.SabreCSG.Vertex}"/>
    public class VertexPositionComparerEpsilon : IEqualityComparer<Vertex>
    {
        /// <summary>
        /// Checks whether two <see cref="Vertex"/> can be considered equal by their position.
        /// </summary>
        /// <param name="a">The first <see cref="Vertex"/>.</param>
        /// <param name="b">The second <see cref="Vertex"/>.</param>
        /// <returns><c>true</c> if the two <see cref="Vertex"/> can be considered equal; otherwise, <c>false</c>.</returns>
        public bool Equals(Vertex a, Vertex b)
        {
            return a.Position.EqualsWithEpsilon(b.Position);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <param name="obj">The object to hash.</param>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures
        /// like a hash table.
        /// </returns>
        public int GetHashCode(Vertex obj)
        {
            // The similarity or difference between two positions can only be calculated if both are supplied
            // when Distinct is called GetHashCode is used to determine which values are in collision first
            // therefore we return the same hash code for all values to ensure all comparisons must use
            // our Equals method to properly determine which values are actually considered equal
            return 1;
        }
    }
}

#endif