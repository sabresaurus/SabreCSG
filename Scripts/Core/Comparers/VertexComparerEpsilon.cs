#if UNITY_EDITOR || RUNTIME_CSG

using System.Collections.Generic;

namespace Sabresaurus.SabreCSG
{
    public class VertexComparerEpsilon : IEqualityComparer<Vertex>
    {
        public bool Equals(Vertex x, Vertex y)
        {
            return x.Position.EqualsWithEpsilon(y.Position);
        }

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