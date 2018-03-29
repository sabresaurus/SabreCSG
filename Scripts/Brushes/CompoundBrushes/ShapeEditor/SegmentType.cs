#if UNITY_EDITOR || RUNTIME_CSG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sabresaurus.SabreCSG.ShapeEditor
{
    /// <summary>
    /// The type of 2D segment.
    /// </summary>
    public enum SegmentType
    {
        Linear,
        Bezier
    }
}
#endif