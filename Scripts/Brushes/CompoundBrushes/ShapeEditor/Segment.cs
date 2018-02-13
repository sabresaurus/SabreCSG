#if UNITY_EDITOR || RUNTIME_CSG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Sabresaurus.SabreCSG.ShapeEditor
{
    /// <summary>
    /// A 2D Shape Editor Segment.
    /// </summary>
    [Serializable]
    public class Segment : ISelectable
    {
        /// <summary>
        /// The position of the segment on the grid.
        /// </summary>
        [SerializeField]
        private Vector2Int _position;

        /// <summary>
        /// The position of the segment on the grid.
        /// </summary>
        public Vector2Int position
        {
            get { return _position; }
            set { _position = value; }
        }

        /// <summary>
        /// The segment type.
        /// </summary>
        [SerializeField]
        public SegmentType type = SegmentType.Linear;

        /// <summary>
        /// The first bezier pivot (see <see cref="SegmentType.Bezier"/>).
        /// </summary>
        [SerializeField]
        public Pivot bezierPivot1 = new Pivot();

        /// <summary>
        /// The second bezier pivot (see <see cref="SegmentType.Bezier"/>).
        /// </summary>
        [SerializeField]
        public Pivot bezierPivot2 = new Pivot();

        [SerializeField]
        public int bezierDetail = 3;

        /// <summary>
        /// Initializes a new instance of the <see cref="Segment"/> class.
        /// </summary>
        /// <param name="x">The x-coordinate on the grid.</param>
        /// <param name="y">The y-coordinate on the grid.</param>
        public Segment(int x, int y)
        {
            this.position = new Vector2Int(x, y);
        }
    }
}
#endif