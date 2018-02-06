#if UNITY_EDITOR || RUNTIME_CSG

using System.Collections.Generic;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
    public class Vector3ComparerEpsilon : IEqualityComparer<Vector3>
    {
        public bool Equals(Vector3 a, Vector3 b)
        {
            return Mathf.Abs(a.x - b.x) < MathHelper.EPSILON_3
                && Mathf.Abs(a.y - b.y) < MathHelper.EPSILON_3
                    && Mathf.Abs(a.z - b.z) < MathHelper.EPSILON_3;
        }

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