#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Sabresaurus.SabreCSG
{
    public class AdjacencyFilters
    {
        public interface BaseFilter
        {
            bool IsPolygonAcceptable(Polygon polygonToTest);
        }

        public class MatchMaterial : BaseFilter
        {
            Material[] acceptableMaterials;

            public MatchMaterial(Material[] acceptableMaterials)
            {
                this.acceptableMaterials = acceptableMaterials;
            }

            public bool IsPolygonAcceptable(Polygon polygonToTest)
            {
                if(acceptableMaterials.Contains(polygonToTest.Material))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
#endif