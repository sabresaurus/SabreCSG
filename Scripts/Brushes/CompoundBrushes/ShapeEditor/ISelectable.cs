#if UNITY_EDITOR || RUNTIME_CSG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Sabresaurus.SabreCSG.ShapeEditor
{
    /// <summary>
    /// Any object that can be selected in the 2D Shape Editor.
    /// </summary>
    public interface ISelectable
    {
        /// <summary>
        /// The position of the object on the grid.
        /// </summary>
        Vector2Int position { get; set; }
    }
}
#endif