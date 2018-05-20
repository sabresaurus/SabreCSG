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
        [SerializeField]
        public bool flipHorizontally = false;

        /// <summary>
        /// Whether the project was flipped vertically.
        /// </summary>
        [SerializeField]
        public bool flipVertically = false;

        /// <summary>
        /// The extrude depth used on the most recent extrude.
        /// </summary>
        [SerializeField]
        public float extrudeDepth = 1.0f;

        /// <summary>
        /// The extrude clip depth used on the most recent extrude.
        /// </summary>
        [SerializeField]
        public float extrudeClipDepth = 0.5f;

        /// <summary>
        /// The scale modifier values used on the most recent extrude.
        /// </summary>
        [SerializeField]
        public Vector2 extrudeScale = Vector2.one;

        /// <summary>
        /// The how many steps it takes to revolve 360 degrees.
        /// </summary>
        [SerializeField]
        public int revolve360 = 8;

        /// <summary>
        /// The amount of steps used.
        /// </summary>
        [SerializeField]
        public int revolveSteps = 4;

        /// <summary>
        /// The revolve distance as determined by the project's global pivot.
        /// </summary>
        [SerializeField]
        public int revolveDistance = 1;

        /// <summary>
        /// The revolve radius as determined by the project's global pivot.
        /// </summary>
        [SerializeField]
        public int revolveRadius = 1;

        /// <summary>
        /// The revolve direction (true is right, false is left).
        /// </summary>
        [SerializeField]
        public bool revolveDirection = true;

        /// <summary>
        /// Whether the spiral is like stairs or a smooth slope.
        /// </summary>
        [SerializeField]
        public bool revolveSpiralSloped = false;

        /// <summary>
        /// Whether the shape uses Convex Decomposition or Concave Shapes.
        /// </summary>
        [SerializeField]
        public bool convexBrushes = true;

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