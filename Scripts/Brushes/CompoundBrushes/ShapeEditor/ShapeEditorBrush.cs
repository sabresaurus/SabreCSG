#if UNITY_EDITOR || RUNTIME_CSG
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Sabresaurus.SabreCSG.ShapeEditor
{
    /// <summary>
    /// The 2D Shape Editor Brush.
    /// </summary>
    /// <remarks>
    /// Inspired by Unreal Editor 1 (1998). Created by Henry de Jongh for SabreCSG. Special Thanks:
    /// Nickgravelyn for your triangulator https://github.com/nickgravelyn/Triangulator.
    /// </remarks>
    /// <seealso cref="Sabresaurus.SabreCSG.CompoundBrush"/>
    [ExecuteInEditMode]
    public class ShapeEditorBrush : CompoundBrush
    {
        /// <summary>
        /// The extrude modes to transform the 2D Shape Editor project into a 3D Brush.
        /// </summary>
        public enum ExtrudeMode
        {
            /// <summary>
            /// Creates a flat polygon in NoCSG mode.
            /// </summary>
            CreatePolygon,
            /// <summary>
            /// Revolves the shapes.
            /// </summary>
            ExtrudeRevolve,
            /// <summary>
            /// Extrudes the shapes.
            /// </summary>
            ExtrudeShape,
            /// <summary>
            /// Extrudes the shapes to a point.
            /// </summary>
            ExtrudePoint,
            /// <summary>
            /// Extrudes the shapes bevelled.
            /// </summary>
            ExtrudeBevel
        }

        /// <summary>The 2D Shape Editor Project (latest project used to build this brush).</summary>
        [SerializeField]
        Project project = JsonUtility.FromJson<Project>("{\"version\":1,\"shapes\":[{\"segments\":[{\"_position\":{\"x\":-16,\"y\":-2},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierPivot2\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierDetail\":3},{\"_position\":{\"x\":-12,\"y\":-5},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierPivot2\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierDetail\":3},{\"_position\":{\"x\":-12,\"y\":-7},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierPivot2\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierDetail\":3},{\"_position\":{\"x\":-18,\"y\":-7},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierPivot2\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierDetail\":3},{\"_position\":{\"x\":-8,\"y\":-13},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierPivot2\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierDetail\":3},{\"_position\":{\"x\":-4,\"y\":-13},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierPivot2\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierDetail\":3},{\"_position\":{\"x\":-4,\"y\":-10},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierPivot2\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierDetail\":3},{\"_position\":{\"x\":-1,\"y\":-13},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierPivot2\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierDetail\":3},{\"_position\":{\"x\":8,\"y\":-13},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierPivot2\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierDetail\":3},{\"_position\":{\"x\":11,\"y\":-10},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierPivot2\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierDetail\":3},{\"_position\":{\"x\":13,\"y\":-10},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierPivot2\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierDetail\":3},{\"_position\":{\"x\":16,\"y\":-6},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierPivot2\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierDetail\":3},{\"_position\":{\"x\":16,\"y\":4},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierPivot2\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierDetail\":3},{\"_position\":{\"x\":12,\"y\":-2},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierPivot2\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierDetail\":3},{\"_position\":{\"x\":11,\"y\":-5},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierPivot2\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierDetail\":3},{\"_position\":{\"x\":7,\"y\":-9},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierPivot2\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierDetail\":3},{\"_position\":{\"x\":0,\"y\":-10},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierPivot2\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierDetail\":3},{\"_position\":{\"x\":-1,\"y\":-5},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierPivot2\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierDetail\":3},{\"_position\":{\"x\":11,\"y\":-5},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierPivot2\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierDetail\":3},{\"_position\":{\"x\":12,\"y\":-2},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierPivot2\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierDetail\":3},{\"_position\":{\"x\":8,\"y\":-2},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierPivot2\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierDetail\":3},{\"_position\":{\"x\":0,\"y\":5},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierPivot2\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierDetail\":3},{\"_position\":{\"x\":0,\"y\":9},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierPivot2\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierDetail\":3},{\"_position\":{\"x\":7,\"y\":12},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierPivot2\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierDetail\":3},{\"_position\":{\"x\":14,\"y\":12},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierPivot2\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierDetail\":3},{\"_position\":{\"x\":12,\"y\":14},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierPivot2\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierDetail\":3},{\"_position\":{\"x\":-8,\"y\":14},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierPivot2\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierDetail\":3},{\"_position\":{\"x\":-16,\"y\":6},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierPivot2\":{\"_position\":{\"x\":0,\"y\":0}},\"bezierDetail\":3}],\"pivot\":{\"_position\":{\"x\":2,\"y\":-2}}}],\"globalPivot\":{\"_position\":{\"x\":0,\"y\":0}}}");

        /// <summary>
        /// The extrude mode (latest operation used to build this brush).
        /// </summary>
        [SerializeField]
        ExtrudeMode extrudeMode = ExtrudeMode.ExtrudeShape;

        /// <summary>
        /// The extrude height (set by latest operation used to build this brush).
        /// </summary>
        [SerializeField]
        float extrudeHeight = 1.0f;

        /// <summary>The last known extents of the compound brush to detect user resizing the bounds.</summary>
        private Vector3 m_LastKnownExtents;
        /// <summary>The last known position of the compound brush to prevent movement on resizing the bounds.</summary>
        private Vector3 m_LastKnownPosition;

        /// <summary>
        /// The last built polygons. Used to determine the <see cref="BrushCount"/> needed to build
        /// the compound brush.
        /// </summary>
        private List<Polygon> m_LastBuiltPolygons;

        /// <summary>
        /// The last built polygons determine the <see cref="BrushCount"/> needed to build the
        /// compound brush.
        /// </summary>
        private int m_DesiredBrushCount;

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
                // build the polygons from the project to determine the brush count.
                if (m_LastBuiltPolygons == null)
                    m_LastBuiltPolygons = BuildConvexPolygons();

                return m_DesiredBrushCount;
            }
        }

        public override void UpdateVisibility()
        {
        }

        public override void Invalidate(bool polygonsChanged)
        {
            // build the polygons from the project.
            if (m_LastBuiltPolygons == null)
                m_LastBuiltPolygons = BuildConvexPolygons();

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
            }                                                                 //
            ////////////////////////////////////////////////////////////////////

            // iterate through the brushes we received:
            int brushCount = BrushCount;

            // force nocsg when creating a flat polygon sheet as sabrecsg doesn't support it.
            if (extrudeMode == ExtrudeMode.CreatePolygon)
                this.IsNoCSG = true;

            Bounds csgBounds = new Bounds();
            for (int i = 0; i < brushCount; i++)
            {
                // copy our csg information to our child brushes.
                generatedBrushes[i].Mode = this.Mode;
                generatedBrushes[i].IsNoCSG = this.IsNoCSG;
                generatedBrushes[i].IsVisible = this.IsVisible;
                generatedBrushes[i].HasCollision = this.HasCollision;

                switch (extrudeMode)
                {
                    // generate a flat 2d polygon.
                    case ExtrudeMode.CreatePolygon:
                        GenerateNormals(m_LastBuiltPolygons[i]);
                        GenerateUvCoordinates(m_LastBuiltPolygons[i], false);
                        Polygon poly1 = m_LastBuiltPolygons[i].DeepCopy();
                        poly1.Flip();
                        generatedBrushes[i].SetPolygons(new Polygon[] { poly1 });
                        break;

                    case ExtrudeMode.ExtrudeRevolve:
                        break;

                    // generate a 3d cube-ish shape.
                    case ExtrudeMode.ExtrudeShape:
                        GenerateNormals(m_LastBuiltPolygons[i]);
                        Polygon[] outputPolygons;
                        Quaternion rot;
                        SurfaceUtility.ExtrudePolygon(m_LastBuiltPolygons[i], extrudeHeight, out outputPolygons, out rot);
                        foreach (Polygon poly in outputPolygons)
                            GenerateUvCoordinates(poly, false);
                        generatedBrushes[i].SetPolygons(outputPolygons);
                        break;

                    case ExtrudeMode.ExtrudePoint:
                        break;

                    case ExtrudeMode.ExtrudeBevel:
                        break;
                }

                generatedBrushes[i].Invalidate(true);
                csgBounds.Encapsulate(generatedBrushes[i].GetBounds());
            }

            // apply the generated csg bounds.
            localBounds = csgBounds;
            m_LastKnownExtents = localBounds.extents;
            m_LastKnownPosition = transform.localPosition;
        }

        /// <summary>
        /// Gets the next segment.
        /// </summary>
        /// <param name="segment">The segment to find the next segment for.</param>
        /// <returns>The next segment (wraps around).</returns>
        private Segment GetNextSegment(Shape parent, Segment segment)
        {
            int index = parent.segments.IndexOf(segment);
            if (index + 1 > parent.segments.Count - 1)
                return parent.segments[0];
            return parent.segments[index + 1];
        }

        /// <summary>
        /// Builds convex polygons out of the currently loaded 2D Shape Editor project.
        /// Note: Currently simply triangulates everything instead.
        /// </summary>
        /// <returns>A list of convex polygons.</returns>
        private List<Polygon> BuildConvexPolygons()
        {
            List<Polygon> polygons = new List<Polygon>();

            // for each shape in the project:
            foreach (Shape shape in project.shapes)
            {
                List<Vector2> vertices = new List<Vector2>();

                // iterate through all segments of the shape:
                foreach (Segment segment in shape.segments)
                {
                    // linear segment:
                    if (segment.type == SegmentType.Linear)
                    {
                        vertices.Add(new Vector2(segment.position.x, segment.position.y * -1.0f) / 8.0f);
                    }

                    // bezier segment:
                    else
                    {
                        foreach (Edge edge in GetBezierEdges(segment, GetNextSegment(shape, segment)))
                        {
                            vertices.Add(new Vector2(edge.Vertex1.Position.x, edge.Vertex1.Position.y * -1.0f) / 8.0f);
                        }
                    }
                }

                // create convex polygons:
                Vector2[] outputVertices;
                int[] indices;
                Triangulator.Triangulator.Triangulate(vertices.ToArray(), Triangulator.WindingOrder.CounterClockwise, out outputVertices, out indices);

                for (int i = 0; i < indices.Length; i += 3)
                {
                    Polygon polygon = new Polygon(new Vertex[] {
                        new Vertex(outputVertices[indices[i]], Vector3.zero, Vector3.zero),
                        new Vertex(outputVertices[indices[i+1]], Vector3.zero, Vector3.zero),
                        new Vertex(outputVertices[indices[i+2]], Vector3.zero, Vector3.zero)
                    }, null, false, false);
                    polygons.Add(polygon);
                }

                switch (extrudeMode)
                {
                    case ExtrudeMode.CreatePolygon:
                        // we make a brush for every polgon.
                        m_DesiredBrushCount = polygons.Count;
                        break;
                    case ExtrudeMode.ExtrudeRevolve:
                        break;
                    case ExtrudeMode.ExtrudeShape:
                        // every brush has 6 polgons (6 sides).
                        m_DesiredBrushCount = polygons.Count;// * 6;
                        break;
                    case ExtrudeMode.ExtrudePoint:
                        break;
                    case ExtrudeMode.ExtrudeBevel:
                        break;
                }
            }

            return polygons;
        }

        /// <summary>
        /// Generates the UV coordinates for a <see cref="Polygon"/> automatically.
        /// </summary>
        /// <param name="polygon">The polygon to be updated.</param>
        private void GenerateUvCoordinates(Polygon polygon, bool helper)
        {
            if (helper)
            {
                foreach (Vertex vertex in polygon.Vertices)
                    vertex.UV = GeometryHelper.GetUVForPosition(polygon, vertex.Position);
            }
            else
            {
                // stolen code from the surface editor "AutoUV".
                Vector3 planeNormal = polygon.Plane.normal;
                Quaternion cancellingRotation = Quaternion.Inverse(Quaternion.LookRotation(-planeNormal));
                // Sets the UV at each point to the position on the plane
                for (int i = 0; i < polygon.Vertices.Length; i++)
                {
                    Vector3 position = polygon.Vertices[i].Position;
                    Vector2 uv = (cancellingRotation * position) * 0.5f;
                    polygon.Vertices[i].UV = uv;
                }
            }
        }

        private void GenerateNormals(Polygon polygon)
        {
            Plane plane = new Plane(polygon.Vertices[0].Position, polygon.Vertices[1].Position, polygon.Vertices[2].Position);
            foreach (Vertex vertex in polygon.Vertices)
                vertex.Normal = plane.normal;
        }

        private static List<Edge> GetBezierEdges(Segment segment, Segment next)
        {
            List<Edge> edges = new List<Edge>();
            Vector3 lineStart = Bezier.GetPoint(new Vector3(segment.position.x, segment.position.y), new Vector3(segment.bezierPivot1.position.x, segment.bezierPivot1.position.y), new Vector3(segment.bezierPivot2.position.x, segment.bezierPivot2.position.y), new Vector3(next.position.x, next.position.y), 0f);
            for (int i = 1; i <= segment.bezierDetail + 1; i++)
            {
                Vector3 lineEnd = Bezier.GetPoint(new Vector3(segment.position.x, segment.position.y), new Vector3(segment.bezierPivot1.position.x, segment.bezierPivot1.position.y), new Vector3(segment.bezierPivot2.position.x, segment.bezierPivot2.position.y), new Vector3(next.position.x, next.position.y), i / (float)(segment.bezierDetail + 1));
                edges.Add(new Edge(new Vertex(new Vector3(lineStart.x, lineStart.y), Vector3.zero, Vector2.zero), new Vertex(new Vector3(lineEnd.x, lineEnd.y), Vector3.zero, Vector2.zero)));
                lineStart = lineEnd;
            }
            return edges;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////
        // PUBLIC API                                                                            //
        ///////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a copy of the embedded project that once upon a time created this brush.
        /// </summary>
        /// <returns>A copy of the embedded project.</returns>
        public Project GetEmbeddedProject()
        {
            // create a copy of the embedded project using JSON and return it.
            return project.Clone();
        }

        /// <summary>
        /// Creates a flat polygon in NoCSG mode. Also called by the 2D Shape Editor window.
        /// </summary>
        /// <param name="project">The project to be copied into the brush.</param>
        public void CreatePolygon(Project project)
        {
            // store a project copy inside of this brush.
            this.project = project.Clone();
            // store the extrude mode inside of this brush.
            extrudeMode = ExtrudeMode.CreatePolygon;
            // build the polygons out of the project.
            m_LastBuiltPolygons = BuildConvexPolygons();
            // build the brush.
            Invalidate(true);
        }

        /// <summary>
        /// Creates an extruded shape. Also called by the 2D Shape Editor window.
        /// </summary>
        /// <param name="project">The project to be copied into the brush.</param>
        /// <param name="height">The 3D height of the extruded shape.</param>
        public void ExtrudeShape(Project project, float height)
        {
            // store a project copy inside of this brush.
            this.project = project.Clone();
            // store the extrude mode inside of this brush.
            extrudeMode = ExtrudeMode.ExtrudeShape;
            extrudeHeight = height;
            // build the polygons out of the project.
            m_LastBuiltPolygons = BuildConvexPolygons();
            // build the brush.
            Invalidate(true);
        }
    }
}

#endif