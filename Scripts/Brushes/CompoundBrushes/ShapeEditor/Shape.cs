#if UNITY_EDITOR || RUNTIME_CSG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Sabresaurus.SabreCSG.ShapeEditor
{
    /// <summary>
    /// A 2D Shape Editor Shape.
    /// </summary>
    [Serializable]
    public class Shape
    {
        /// <summary>
        /// The segments of the shape.
        /// </summary>
        [SerializeField]
        public List<Segment> segments = new List<Segment>() {
                new Segment(-8, -8),
                new Segment( 8, -8),
                new Segment( 8,  8),
                new Segment(-8,  8),
            };

        /// <summary>
        /// The center pivot of the shape.
        /// </summary>
        public Pivot pivot = new Pivot();

        /// <summary>
        /// Calculates the pivot position so that it's centered on the shape.
        /// </summary>
        public void CalculatePivotPosition()
        {
            Vector2Int center = new Vector2Int();
            foreach (Segment segment in segments)
                center += segment.position;
            pivot.position = new Vector2Int(center.x / segments.Count, center.y / segments.Count);
        }

        /// <summary>
        /// Clones this shape and returns the copy.
        /// </summary>
        /// <returns>A copy of the shape.</returns>
        public Shape Clone()
        {
            // create a copy of the given shape using JSON.
            return JsonUtility.FromJson<Shape>(JsonUtility.ToJson(this));
        }
    }
}
#endif