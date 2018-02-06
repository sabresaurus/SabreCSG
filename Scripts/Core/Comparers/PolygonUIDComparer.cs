#if UNITY_EDITOR || RUNTIME_CSG

using System.Collections.Generic;

namespace Sabresaurus.SabreCSG
{
    public class PolygonUIDComparer : IEqualityComparer<Polygon>
    {
        public bool Equals(Polygon x, Polygon y)
        {
            return x.UniqueIndex == y.UniqueIndex;
        }

        public int GetHashCode(Polygon obj)
        {
            return base.GetHashCode();
        }
    }
}

#endif