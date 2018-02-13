#if UNITY_EDITOR || RUNTIME_CSG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Sabresaurus.SabreCSG.ShapeEditor
{
    /// <summary>
    /// A 2D Shape Editor Project.
    /// </summary>
    [Serializable]
    public class Project
    {
        /// <summary>
        /// The project version, in case of 'severe' future updates.
        /// </summary>
        [SerializeField]
        public int version = 1;

        /// <summary>
        /// The shapes in the project.
        /// </summary>
        [SerializeField]
        public List<Shape> shapes = new List<Shape>()
        {
            new Shape()
        };

        /// <summary>
        /// The global pivot in the project.
        /// </summary>
        [SerializeField]
        public Pivot globalPivot = new Pivot();
    }
}
#endif