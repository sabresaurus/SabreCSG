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

        /// <summary>The height of each step.</summary>
        [SerializeField]
        float stepHeight = 0.0625f;

        /// <summary>The width of each step.</summary>
        [SerializeField]
        float stepWidth = 1.0f;

        /// <summary>The amount of curvature in degrees.</summary>
        [SerializeField]
        float angleOfCurve = 90.0f;

        /// <summary>The amount of steps on the staircase.</summary>
        [SerializeField]
        int numSteps = 4;

        /// <summary>An amount of height to add to the first stair step.</summary>
        [SerializeField]
        float addToFirstStep = 0.0f;

        /// <summary>Whether the stairs are mirrored counter-clockwise.</summary>
        [SerializeField]
        bool counterClockwise = false;

        /// <summary>The last known extents of the compound brush to detect user resizing the bounds.</summary>
        private Vector3 m_LastKnownExtents;
        /// <summary>The last known position of the compound brush to prevent movement on resizing the bounds.</summary>
        private Vector3 m_LastKnownPosition;

        void Awake()
        {
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
                    Invalidate(true); // recusion! <3                         //
                    return;                                                   //
                }                                                             //
                // user is trying to scale down.                              //
                if (localBounds.extents.y < m_LastKnownExtents.y)             //
                {                                                             //
                    numSteps -= 1;                                            //
                    if (numSteps < 1) numSteps = 1;                           //
                    m_LastKnownExtents = localBounds.extents;                 //
                    Invalidate(true); // recusion! <3                         //
                    return;                                                   //
                }                                                             //
            }                                                                 //
            ////////////////////////////////////////////////////////////////////

            // local variables
            List<Vector3> vertexPositions = new List<Vector3>();
            Plane plane;
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
                vertices[index0].Position = vertexPositions[outerStart + (i * 2) + 2];
                vertices[index1].Position = vertexPositions[outerStart + (i * 2) + 1];
                vertices[index2].Position = vertexPositions[innerStart + (i * 2) + 1];
                vertices[index3].Position = vertexPositions[innerStart + (i * 2) + 2];

                // update uv coordinates to prevent distortions using barnaby's genius utilities.
                vertices[index0].UV = GeometryHelper.GetUVForPosition(polygons[5], vertexPositions[outerStart + (i * 2) + 2]);
                vertices[index1].UV = GeometryHelper.GetUVForPosition(polygons[5], vertexPositions[outerStart + (i * 2) + 1]);
                vertices[index2].UV = GeometryHelper.GetUVForPosition(polygons[5], vertexPositions[innerStart + (i * 2) + 1]);
                vertices[index3].UV = GeometryHelper.GetUVForPosition(polygons[5], vertexPositions[innerStart + (i * 2) + 2]);



                // retrieve the vertices of the front polygon.
                vertices = polygons[3].Vertices;

                // step front.
                vertices[index0].Position = vertexPositions[outerStart + (i * 2) + 1];
                vertices[index1].Position = vertexPositions[bottomOuterStart + i];
                vertices[index2].Position = vertexPositions[bottomInnerStart + i];
                vertices[index3].Position = vertexPositions[innerStart + (i * 2) + 1];

                // calculate a normal using a virtual plane.
                plane = new Plane(vertices[index1].Position, vertices[index2].Position, vertices[index3].Position);
                vertices[index0].Normal = plane.normal;
                vertices[index1].Normal = plane.normal;
                vertices[index2].Normal = plane.normal;
                vertices[index3].Normal = plane.normal;



                // retrieve the vertices of the left polygon.
                vertices = polygons[1].Vertices;

                // inner curve.
                vertices[index0].Position = vertexPositions[bottomInnerStart + i + 1];
                vertices[index1].Position = vertexPositions[innerStart + (i * 2) + 2];
                vertices[index2].Position = vertexPositions[innerStart + (i * 2) + 1];
                vertices[index3].Position = vertexPositions[bottomInnerStart + i];

                // calculate a normal using a virtual plane.
                plane = new Plane(vertices[index1].Position, vertices[index2].Position, vertices[index3].Position);
                vertices[index0].Normal = plane.normal;
                vertices[index1].Normal = plane.normal;
                vertices[index2].Normal = plane.normal;
                vertices[index3].Normal = plane.normal;



                // retrieve the vertices of the right polygon.
                vertices = polygons[2].Vertices;

                // outer curve.
                vertices[index0].Position = vertexPositions[outerStart + (i * 2) + 2];
                vertices[index1].Position = vertexPositions[bottomOuterStart + i + 1];
                vertices[index2].Position = vertexPositions[bottomOuterStart + i];
                vertices[index3].Position = vertexPositions[outerStart + (i * 2) + 1];

                // calculate a normal using a virtual plane.
                plane = new Plane(vertices[index1].Position, vertices[index2].Position, vertices[index3].Position);
                vertices[index0].Normal = plane.normal;
                vertices[index1].Normal = plane.normal;
                vertices[index2].Normal = plane.normal;
                vertices[index3].Normal = plane.normal;



                // retrieve the vertices of the bottom polygon.
                vertices = polygons[4].Vertices;

                // bottom.
                vertices[index0].Position = vertexPositions[bottomOuterStart + i];
                vertices[index1].Position = vertexPositions[bottomOuterStart + i + 1];
                vertices[index2].Position = vertexPositions[bottomInnerStart + i + 1];
                vertices[index3].Position = vertexPositions[bottomInnerStart + i];

                // update uv coordinates to prevent distortions using barnaby's genius utilities.
                vertices[index0].UV = GeometryHelper.GetUVForPosition(polygons[4], vertexPositions[bottomOuterStart + i]);
                vertices[index1].UV = GeometryHelper.GetUVForPosition(polygons[4], vertexPositions[bottomOuterStart + i + 1]);
                vertices[index2].UV = GeometryHelper.GetUVForPosition(polygons[4], vertexPositions[bottomInnerStart + i + 1]);
                vertices[index3].UV = GeometryHelper.GetUVForPosition(polygons[4], vertexPositions[bottomInnerStart + i]);



                // retrieve the vertices of the back polygon.
                vertices = polygons[0].Vertices;

                // back panel.
                vertices[index0].Position = vertexPositions[bottomOuterStart + i + 1];
                vertices[index1].Position = vertexPositions[outerStart + (i * 2) + 2];
                vertices[index2].Position = vertexPositions[innerStart + (i * 2) + 2];
                vertices[index3].Position = vertexPositions[bottomInnerStart + i + 1];

                // calculate a normal using a virtual plane.
                plane = new Plane(vertices[index1].Position, vertices[index2].Position, vertices[index3].Position);
                vertices[index0].Normal = plane.normal;
                vertices[index1].Normal = plane.normal;
                vertices[index2].Normal = plane.normal;
                vertices[index3].Normal = plane.normal;

                generatedBrushes[i].Invalidate(true);
                csgBounds.Encapsulate(generatedBrushes[i].GetBounds());
            }

            // apply the generated csg bounds.
            localBounds = csgBounds;
            m_LastKnownExtents = localBounds.extents;
            m_LastKnownPosition = transform.localPosition;
        }
    }
}

#endif