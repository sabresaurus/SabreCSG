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

        /// <summary>
        /// Whether the project was flipped horizontally.
        /// </summary>
        public bool flipHorizontally = false;

        /// <summary>
        /// Whether the project was flipped vertically.
        /// </summary>
        public bool flipVertically = false;

        /// <summary>
        /// Clones this project and returns the copy.
        /// </summary>
        /// <returns>A copy of the project.</returns>
        public Project Clone()
        {
            // create a copy of the given project using JSON.
            return JsonUtility.FromJson<Project>(JsonUtility.ToJson(this));
        }
    }
}
#endif