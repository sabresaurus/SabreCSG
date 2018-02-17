#if UNITY_EDITOR || RUNTIME_CSG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Sabresaurus.SabreCSG.ShapeEditor
{
    /// <summary>
    /// A 2D Shape Editor Pivot.
    /// </summary>
    [Serializable]
    public class Pivot : ISelectable
    {
        /// <summary>
        /// The position of the pivot on the grid.
        /// </summary>
        [SerializeField]
        private Vector2Int _position;

        /// <summary>
        /// The position of the pivot on the grid.
        /// </summary>
        public Vector2Int position
        {
            get { return _position; }
            set { _position = value; }
        }
    }
}
#endif