#if UNITY_EDITOR || RUNTIME_CSG
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
    /// <summary>
    /// Generates a curved staircase. Inspired by Unreal Editor 1 (1998).
    /// </summary>
    /// <remarks>Taking 256.0f unit chunks and 65536.0f rotations with integers down to the metric scale. My head hurts. ~Henry de Jongh.</remarks>
    /// <seealso cref="Sabresaurus.SabreCSG.CompoundBrush" />
    [ExecuteInEditMode]
	public class CurvedStairBrush : CompoundBrush
	{
        /// <summary>The radius in meters in the center of the staircase.</summary>
        [SerializeField]
        float innerRadius = 1.0f;

        /// <summary>
        /// Gets or sets the radius in meters in the center of the staircase.
        /// </summary>
        /// <value>The radius in meters in the center of the staircase.</value>
        public float InnerRadius { get { return innerRadius; } set { innerRadius = value; } }

        /// <summary>The height of each step.</summary>
        [SerializeField]
        float stepHeight = 0.0625f;

        /// <summary>
        /// Gets or sets the height of each step.
        /// </summary>
        /// <value>The height of each step.</value>
        public float StepHeight { get { return stepHeight; } set { stepHeight = value; } }

        /// <summary>The width of each step.</summary>
        [SerializeField]
        float stepWidth = 1.0f;

        /// <summary>
        /// Gets or sets the width of each step.
        /// </summary>
        /// <value>The width of each step.</value>
        public float StepWidth { get { return stepWidth; } set { stepWidth = value; } }

        /// <summary>The amount of curvature in degrees.</summary>
        [SerializeField]
        float angleOfCurve = 90.0f;

        /// <summary>
        /// Gets or sets the amount of curvature in degrees.
        /// </summary>
        /// <value>The amount of curvature in degrees.</value>
        public float AngleOfCurve { get { return angleOfCurve; } set { angleOfCurve = value; } }

        /// <summary>The amount of steps on the staircase.</summary>
        [SerializeField]
        int numSteps = 4;

        /// <summary>
        /// Gets or sets the amount of steps on the staircase.
        /// </summary>
        /// <value>The amount of steps on the staircase.</value>
        public int NumberOfSteps { get { return numSteps; } set { numSteps = value; } }

        /// <summary>An amount of height to add to the first stair step.</summary>
        [SerializeField]
        float addToFirstStep = 0.0f;

        /// <summary>
        /// Gets or sets an amount of height to add to the first stair step.
        /// </summary>
        /// <value>An amount of height to add to the first stair step.</value>
        public float AddToFirstStep { get { return addToFirstStep; } set { addToFirstStep = value; } }

        /// <summary>Whether the stairs are mirrored counter-clockwise.</summary>
        [SerializeField]
        bool counterClockwise = false;

        /// <summary>
        /// Gets or sets a value indicating whether the stairs are mirrored counter-clockwise.
        /// </summary>
        /// <value><c>true</c> if the stairs are mirrored counter-clockwise; otherwise, <c>false</c>.</value>
        public bool CounterClockwise { get { return counterClockwise; } set { counterClockwise = value; } }

        /// <summary>Whether the stairs reach down to the bottom.</summary>
        [SerializeField]
        bool fillToBottom = true;

        /// <summary>
        /// Gets or sets a value indicating whether the stairs reach down to the bottom.
        /// </summary>
        /// <value><c>true</c> if the stairs reach down to the bottom; otherwise, <c>false</c>.</value>
        public bool FillToBottom { get { return fillToBottom; } set { fillToBottom = value; } }

        /// <summary>Whether to generate stairs or a curved wall.</summary>
        [SerializeField]
        bool curvedWall = false;

        /// <summary>
        /// Gets or sets a value indicating whether to generate stairs or a curved wall.
        /// </summary>
        /// <value><c>true</c> if it generates stairs; otherwise, <c>false</c> to generate a curved wall.</value>
        public bool CurvedWall { get { return curvedWall; } set { curvedWall = value; } }

        /// <summary>Whether the floor is stairs or a smooth slope.</summary>
        [SerializeField]
        bool slopedFloor = false;

        /// <summary>
        /// Gets or sets a value indicating whether the floor is stairs or a smooth slope.
        /// </summary>
        /// <value><c>true</c> if the floor is a smooth slope; otherwise, <c>false</c> if the floor is stairs.</value>
        public bool SlopedFloor { get { return slopedFloor; } set { slopedFloor = value; } }

        /// <summary>Whether the ceiling is stairs or a smooth slope.</summary>
        [SerializeField]
        bool slopedCeiling = false;

        /// <summary>
        /// Gets or sets a value indicating whether the ceiling is stairs or a smooth slope.
        /// </summary>
        /// <value><c>true</c> if the ceiling is a smooth slope; otherwise, <c>false</c> if the ceiling is stairs.</value>
        public bool SlopedCeiling { get { return slopedCeiling; } set { slopedCeiling = value; } }

        /// <summary>The last known extents of the compound brush to detect user resizing the bounds.</summary>
        private Vector3 m_LastKnownExtents;
        /// <summary>The last known position of the compound brush to prevent movement on resizing the bounds.</summary>
        private Vector3 m_LastKnownPosition;

        /// <summary>
        /// Gets the beautiful name of the brush used in auto-generation of the hierarchy name.
        /// </summary>
        /// <value>The beautiful name of the brush.</value>
        public override string BeautifulBrushName
        {
            get
            {
                return "Curved Stairs Brush";
            }
        }

        protected override void Awake()
        {
            base.Awake();
            // get the last known extents and position (especially after scene changes).
            m_LastKnownExtents = localBounds.extents;
            m_LastKnownPosition = transform.localPosition;
        }

        public override int BrushCount 
		{
			get
			{
                // calculate the amount of steps and use that as the brush count we need.
                return numSteps;
			}
		}

		public override void UpdateVisibility ()
		{
		}

		public override void Invalidate (bool polygonsChanged)
		{
            base.Invalidate(polygonsChanged);

            ////////////////////////////////////////////////////////////////////
            // a little hack to detect the user manually resizing the bounds. //
            // we use this to automatically add steps for barnaby.            //
            // it's probably good to build a more 'official' way to detect    //
            // user scaling events in compound brushes sometime.              //
            if (m_LastKnownExtents != localBounds.extents)                    //
            {                                                                 //
                // undo any position movement.                                //
                transform.localPosition = m_LastKnownPosition;                //
                // user is trying to scale up.                                //
                if (localBounds.extents.y > m_LastKnownExtents.y)             //
                {                                                             //
                    numSteps += 1;                                            //
                    m_LastKnownExtents = localBounds.extents;                 //
                    Invalidate(true); // recursion! <3                        //
                    return;                                                   //
                }                                                             //
                // user is trying to scale down.                              //
                if (localBounds.extents.y < m_LastKnownExtents.y)             //
                {                                                             //
                    numSteps -= 1;                                            //
                    if (numSteps < 1) numSteps = 1;                           //
                    m_LastKnownExtents = localBounds.extents;                 //
                    Invalidate(true); // recursion! <3                        //
                    return;                                                   //
                }                                                             //
            }                                                                 //
            ////////////////////////////////////////////////////////////////////

            // local variables
            List<Vector3> vertexPositions = new List<Vector3>();
            Vector3 rotateStep = new Vector3();
            Vector3 vertex = new Vector3(), newVertex = new Vector3();
            float adjustment;
            int innerStart, outerStart, bottomInnerStart, bottomOuterStart;

            // begin
            rotateStep.z = angleOfCurve / numSteps;

            if (counterClockwise)
            {
                rotateStep.z *= -1;
            }

            // generate the inner curve points.
            innerStart = vertexPositions.Count;
            vertex.x = innerRadius;
            for (int x = 0; x < (numSteps + 1); x++)
            {
                if (x == 0)
                    adjustment = addToFirstStep;
                else
                    adjustment = 0;

                newVertex = Quaternion.Euler(rotateStep * x) * vertex;
                vertexPositions.Add(new Vector3(newVertex.x, vertex.z - adjustment, newVertex.y));
                if (curvedWall)
                    vertex.z = stepHeight * numSteps;
                else
                    vertex.z += stepHeight;
                vertexPositions.Add(new Vector3(newVertex.x, vertex.z, newVertex.y));
            }

            // generate the outer curve points.
            outerStart = vertexPositions.Count;
            vertex.x = innerRadius + stepWidth;
            vertex.z = 0;
            for (int x = 0; x < (numSteps + 1); x++)
            {
                if (x == 0)
                    adjustment = addToFirstStep;
                else
                    adjustment = 0;

                newVertex = Quaternion.Euler(rotateStep * x) * vertex;
                vertexPositions.Add(new Vector3(newVertex.x, vertex.z - adjustment, newVertex.y));
                if (curvedWall)
                    vertex.z = stepHeight * numSteps;
                else
                    vertex.z += stepHeight;
                vertexPositions.Add(new Vector3(newVertex.x, vertex.z, newVertex.y));
            }

            // generate the bottom inner curve points.
            bottomInnerStart = vertexPositions.Count;
            vertex.x = innerRadius;
            vertex.z = 0;
            for (int x = 0; x < (numSteps + 1); x++)
            {
                newVertex = Quaternion.Euler(rotateStep * x) * vertex;
                vertexPositions.Add(new Vector3(newVertex.x, vertex.z - addToFirstStep, newVertex.y));
            }

            // generate the bottom outer curve points.
            bottomOuterStart = vertexPositions.Count;
            vertex.x = innerRadius + stepWidth;
            for (int x = 0; x < (numSteps + 1); x++)
            {
                newVertex = Quaternion.Euler(rotateStep * x) * vertex;
                vertexPositions.Add(new Vector3(newVertex.x, vertex.z - addToFirstStep, newVertex.y));
            }

            // vertex indices to easily flip faces for the counter clockwise mode.
            int index0 = 0;
            int index1 = 1;
            int index2 = 2;
            int index3 = 3;

            // flip faces if counter clockwise mode is enabled.
            if (counterClockwise)
            {
                index0 = 2;
                index1 = 1;
                index2 = 0;
                index3 = 3;
            }

            // we force NoCSG mode if special NoCSG operators are used.
            if (curvedWall) { slopedFloor = false; slopedCeiling = false; }
            if (fillToBottom) { slopedCeiling = false; }
            if (slopedFloor || slopedCeiling)
            {
                this.IsNoCSG = true;
            }

            // we calculate the bounds of the output csg.
            Bounds csgBounds = new Bounds();

            // iterate through the brushes we received:
            int brushCount = BrushCount;
            for (int i = 0; i < brushCount; i++)
            {
                // copy our csg information to our child brushes.
                generatedBrushes[i].Mode = this.Mode;
                generatedBrushes[i].IsNoCSG = this.IsNoCSG;
                generatedBrushes[i].IsVisible = this.IsVisible;
                generatedBrushes[i].HasCollision = this.HasCollision;

                // retrieve the polygons from the current cube brush.
                Polygon[] polygons = generatedBrushes[i].GetPolygons();

                // +-----------------------------------------------------+
                // | Cube Polygons                                       |
                // +--------+--------+--------+--------+--------+--------+
                // | Poly:0 | Poly:1 | Poly:2 | Poly:3 | Poly:4 | Poly:5 |
                // +-----------------------------------------------------+
                // | Back   | Left   | Right  | Front  | Bottom | Top    |
                // +--------+--------+--------+--------+--------+--------+



                // retrieve the vertices of the top polygon.
                Vertex[] vertices = polygons[5].Vertices;

                // step top.
                if (slopedFloor)
                {
                    // create a smooth slope.
                    vertices[index0].Position = vertexPositions[outerStart + (i * 2) + 2 + 1];
                    vertices[index1].Position = vertexPositions[outerStart + (i * 2) + 1];
                    vertices[index2].Position = vertexPositions[innerStart + (i * 2) + 1];
                    vertices[index3].Position = vertexPositions[innerStart + (i * 2) + 2 + 1];
                }
                else
                {
                    // create blocky stairs.
                    vertices[index0].Position = vertexPositions[outerStart + (i * 2) + 2];
                    vertices[index1].Position = vertexPositions[outerStart + (i * 2) + 1];
                    vertices[index2].Position = vertexPositions[innerStart + (i * 2) + 1];
                    vertices[index3].Position = vertexPositions[innerStart + (i * 2) + 2];
                }

                // calculate a normal using a virtual plane.
                GenerateNormals(polygons[5]);

                // update uv coordinates to prevent distortions using barnaby's genius utilities.
                GenerateUvCoordinates(polygons[5]);



                // retrieve the vertices of the front polygon.
                vertices = polygons[3].Vertices;

                // step front.
                if (fillToBottom)
                {
                    // fill downwards to the bottom.
                    vertices[index0].Position = vertexPositions[outerStart + (i * 2) + 1];
                    vertices[index1].Position = vertexPositions[bottomOuterStart + i];
                    vertices[index2].Position = vertexPositions[bottomInnerStart + i];
                    vertices[index3].Position = vertexPositions[innerStart + (i * 2) + 1];
                }
                else
                {
                    // fill downwards to the step height.
                    vertices[index0].Position = vertexPositions[outerStart + (i * 2) + 1];
                    vertices[index1].Position = vertexPositions[outerStart + (i * 2) + 1] - new Vector3(0.0f, stepHeight, 0.0f);
                    vertices[index2].Position = vertexPositions[innerStart + (i * 2) + 1] - new Vector3(0.0f, stepHeight, 0.0f);
                    vertices[index3].Position = vertexPositions[innerStart + (i * 2) + 1];
                }
                if (slopedCeiling)
                {
                    // create a smooth ceiling slope.
                    vertices[index1].Position -= new Vector3(0.0f, stepHeight, 0.0f);
                    vertices[index2].Position -= new Vector3(0.0f, stepHeight, 0.0f);
                }

                // calculate a normal using a virtual plane.
                GenerateNormals(polygons[3]);

                // update uv coordinates to prevent distortions using barnaby's genius utilities.
                GenerateUvCoordinates(polygons[3]);



                // retrieve the vertices of the left polygon.
                vertices = polygons[1].Vertices;

                // inner curve.
                if (fillToBottom)
                {
                    // fill downwards to the bottom.
                    vertices[index0].Position = vertexPositions[bottomInnerStart + i + 1];
                    vertices[index1].Position = vertexPositions[innerStart + (i * 2) + 2];
                    vertices[index2].Position = vertexPositions[innerStart + (i * 2) + 1];
                    vertices[index3].Position = vertexPositions[bottomInnerStart + i];
                }
                else
                {
                    // fill downwards to the step height.
                    vertices[index0].Position = vertexPositions[innerStart + (i * 2) + 2] - new Vector3(0.0f, stepHeight, 0.0f);
                    vertices[index1].Position = vertexPositions[innerStart + (i * 2) + 2];
                    vertices[index2].Position = vertexPositions[innerStart + (i * 2) + 1];
                    vertices[index3].Position = vertexPositions[innerStart + (i * 2) + 1] - new Vector3(0.0f, stepHeight, 0.0f);
                }
                if (slopedFloor)
                {
                    // create a smooth floor slope.
                    vertices[index1].Position = vertexPositions[innerStart + (i * 2) + 2 + 1];
                }
                if (slopedCeiling)
                {
                    // create a smooth ceiling slope.
                    vertices[index3].Position = vertexPositions[innerStart + (i * 2) + 1] - new Vector3(0.0f, stepHeight * 2.0f, 0.0f);
                }

                // calculate a normal using a virtual plane.
                GenerateNormals(polygons[1]);

                // update uv coordinates to prevent distortions using barnaby's genius utilities.
                GenerateUvCoordinates(polygons[1]);



                // retrieve the vertices of the right polygon.
                vertices = polygons[2].Vertices;

                // outer curve.
                if (fillToBottom)
                {
                    // fill downwards to the bottom.
                    vertices[index0].Position = vertexPositions[outerStart + (i * 2) + 2];
                    vertices[index1].Position = vertexPositions[bottomOuterStart + i + 1];
                    vertices[index2].Position = vertexPositions[bottomOuterStart + i];
                    vertices[index3].Position = vertexPositions[outerStart + (i * 2) + 1];
                }
                else
                {
                    // fill downwards to the step height.
                    vertices[index0].Position = vertexPositions[outerStart + (i * 2) + 2];
                    vertices[index1].Position = vertexPositions[outerStart + (i * 2) + 2] - new Vector3(0.0f, stepHeight, 0.0f);
                    vertices[index2].Position = vertexPositions[outerStart + (i * 2) + 1] - new Vector3(0.0f, stepHeight, 0.0f);
                    vertices[index3].Position = vertexPositions[outerStart + (i * 2) + 1];
                }
                if (slopedFloor)
                {
                    // create a smooth floor slope.
                    vertices[index0].Position = vertexPositions[outerStart + (i * 2) + 2 + 1];
                }
                if (slopedCeiling)
                {
                    // create a smooth ceiling slope.
                    vertices[index2].Position = vertexPositions[outerStart + (i * 2) + 1] - new Vector3(0.0f, stepHeight * 2.0f, 0.0f);
                }

                // calculate a normal using a virtual plane.
                GenerateNormals(polygons[2]);

                // update uv coordinates to prevent distortions using barnaby's genius utilities.
                GenerateUvCoordinates(polygons[2]);



                // retrieve the vertices of the bottom polygon.
                vertices = polygons[4].Vertices;

                // bottom.
                if (fillToBottom)
                {
                    // fill downwards to the bottom.
                    vertices[index0].Position = vertexPositions[bottomOuterStart + i];
                    vertices[index1].Position = vertexPositions[bottomOuterStart + i + 1];
                    vertices[index2].Position = vertexPositions[bottomInnerStart + i + 1];
                    vertices[index3].Position = vertexPositions[bottomInnerStart + i];
                }
                else
                {
                    // fill downwards to the step height.
                    vertices[index0].Position = vertexPositions[outerStart + (i * 2) + 1] - new Vector3(0.0f, stepHeight, 0.0f);
                    vertices[index1].Position = vertexPositions[outerStart + (i * 2) + 2] - new Vector3(0.0f, stepHeight, 0.0f);
                    vertices[index2].Position = vertexPositions[innerStart + (i * 2) + 2] - new Vector3(0.0f, stepHeight, 0.0f);
                    vertices[index3].Position = vertexPositions[innerStart + (i * 2) + 1] - new Vector3(0.0f, stepHeight, 0.0f);
                }
                if (slopedCeiling)
                {
                    // create a smooth ceiling slope.
                    vertices[index0].Position = vertexPositions[outerStart + (i * 2) + 1] - new Vector3(0.0f, stepHeight * 2.0f, 0.0f);
                    vertices[index3].Position = vertexPositions[innerStart + (i * 2) + 1] - new Vector3(0.0f, stepHeight * 2.0f, 0.0f);
                }

                // calculate a normal using a virtual plane.
                GenerateNormals(polygons[4]);

                // update uv coordinates to prevent distortions using barnaby's genius utilities.
                GenerateUvCoordinates(polygons[4]);



                // retrieve the vertices of the back polygon.
                vertices = polygons[0].Vertices;

                // back panel.
                if (fillToBottom)
                {
                    // fill downwards to the bottom.
                    vertices[index0].Position = vertexPositions[bottomOuterStart + i + 1];
                    vertices[index1].Position = vertexPositions[outerStart + (i * 2) + 2];
                    vertices[index2].Position = vertexPositions[innerStart + (i * 2) + 2];
                    vertices[index3].Position = vertexPositions[bottomInnerStart + i + 1];
                }
                else
                {
                    // fill downwards to the step height.
                    vertices[index0].Position = vertexPositions[outerStart + (i * 2) + 2] - new Vector3(0.0f, stepHeight, 0.0f);
                    vertices[index1].Position = vertexPositions[outerStart + (i * 2) + 2];
                    vertices[index2].Position = vertexPositions[innerStart + (i * 2) + 2];
                    vertices[index3].Position = vertexPositions[innerStart + (i * 2) + 2] - new Vector3(0.0f, stepHeight, 0.0f);
                }
                if (slopedFloor)
                {
                    // create a smooth floor slope.
                    vertices[index1].Position = vertexPositions[outerStart + (i * 2) + 2 + 1];
                    vertices[index2].Position = vertexPositions[innerStart + (i * 2) + 2 + 1];
                }

                // calculate a normal using a virtual plane.
                GenerateNormals(polygons[0]);

                // update uv coordinates to prevent distortions using barnaby's genius utilities.
                GenerateUvCoordinates(polygons[0]);



                generatedBrushes[i].Invalidate(true);
                csgBounds.Encapsulate(generatedBrushes[i].GetBounds());
            }

            // apply the generated csg bounds.
            localBounds = csgBounds;
            m_LastKnownExtents = localBounds.extents;
            m_LastKnownPosition = transform.localPosition;
            // update the generated name in the hierarchy.
            UpdateGeneratedHierarchyName();
        }

        /// <summary>
        /// Generates the UV coordinates for a <see cref="Polygon"/> automatically.
        /// </summary>
        /// <param name="polygon">The polygon to be updated.</param>
        private void GenerateUvCoordinates(Polygon polygon)
        {
            foreach (Vertex vertex in polygon.Vertices)
                vertex.UV = GeometryHelper.GetUVForPosition(polygon, vertex.Position);
        }

        private void GenerateNormals(Polygon polygon)
        {
            Plane plane = new Plane(polygon.Vertices[1].Position, polygon.Vertices[2].Position, polygon.Vertices[3].Position);
            foreach (Vertex vertex in polygon.Vertices)
                vertex.Normal = plane.normal;
        }
    }
}

#endif