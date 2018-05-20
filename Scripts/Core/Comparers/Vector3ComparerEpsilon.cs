﻿#if UNITY_EDITOR || RUNTIME_CSG

using System.Collections.Generic;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
    /// <summary>
    /// An implementation of <see cref="IEqualityComparer{T}"/> that checks whether two <see
    /// cref="Vector3"/> can be considered equal.
    /// <para>Floating point inaccuracies are taken into account (see <see cref="MathHelper.EPSILON_3"/>).</para>
    /// </summary>
    /// <seealso cref="System.Collections.Generic.IEqualityComparer{UnityEngine.Vector3}"/>
    public class Vector3ComparerEpsilon : IEqualityComparer<Vector3>
    {
        /// <summary>
        /// Checks whether two <see cref="Vector3"/> can be considered equal.
        /// </summary>
        /// <param name="a">The first <see cref="Vector3"/>.</param>
        /// <param name="b">The second <see cref="Vector3"/>.</param>
        /// <returns><c>true</c> if the two <see cref="Vector3"/> can be considered equal; otherwise, <c>false</c>.</returns>
        public bool Equals(Vector3 a, Vector3 b)
        {
            return Mathf.Abs(a.x - b.x) < MathHelper.EPSILON_3
                && Mathf.Abs(a.y - b.y) < MathHelper.EPSILON_3
                && Mathf.Abs(a.z - b.z) < MathHelper.EPSILON_3;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <param name="obj">The object to hash.</param>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures
        /// like a hash table.
        /// </returns>
        public int GetHashCode(Vector3 obj)
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