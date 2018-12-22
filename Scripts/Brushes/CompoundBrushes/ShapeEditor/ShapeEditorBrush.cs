#if UNITY_EDITOR || RUNTIME_CSG

using Sabresaurus.SabreCSG.ShapeEditor.Decomposition;
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
    /// Gabriel Ochsenhofer for your decomposition algorithms https://github.com/gabstv/Farseer-Unity3D.
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
            RevolveShape,

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
        private Project project = JsonUtility.FromJson<Project>("{\"version\":1,\"shapes\":[{\"segments\":[{\"_position\":{\"x\":-18,\"y\":-8},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierPivot2\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierDetail\":3},{\"_position\":{\"x\":-14,\"y\":-11},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierPivot2\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierDetail\":3},{\"_position\":{\"x\":-14,\"y\":-13},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierPivot2\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierDetail\":3},{\"_position\":{\"x\":-20,\"y\":-13},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierPivot2\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierDetail\":3},{\"_position\":{\"x\":-10,\"y\":-19},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierPivot2\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierDetail\":3},{\"_position\":{\"x\":-6,\"y\":-19},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierPivot2\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierDetail\":3},{\"_position\":{\"x\":-6,\"y\":-16},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierPivot2\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierDetail\":3},{\"_position\":{\"x\":-3,\"y\":-19},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierPivot2\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierDetail\":3},{\"_position\":{\"x\":6,\"y\":-19},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierPivot2\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierDetail\":3},{\"_position\":{\"x\":9,\"y\":-16},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierPivot2\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierDetail\":3},{\"_position\":{\"x\":11,\"y\":-16},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierPivot2\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierDetail\":3},{\"_position\":{\"x\":14,\"y\":-12},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierPivot2\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierDetail\":3},{\"_position\":{\"x\":14,\"y\":-2},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierPivot2\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierDetail\":3},{\"_position\":{\"x\":10,\"y\":-8},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierPivot2\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierDetail\":3},{\"_position\":{\"x\":9,\"y\":-11},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierPivot2\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierDetail\":3},{\"_position\":{\"x\":5,\"y\":-15},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierPivot2\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierDetail\":3},{\"_position\":{\"x\":-2,\"y\":-16},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierPivot2\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierDetail\":3},{\"_position\":{\"x\":-3,\"y\":-11},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierPivot2\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierDetail\":3},{\"_position\":{\"x\":9,\"y\":-11},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierPivot2\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierDetail\":3},{\"_position\":{\"x\":10,\"y\":-8},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierPivot2\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierDetail\":3},{\"_position\":{\"x\":6,\"y\":-8},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierPivot2\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierDetail\":3},{\"_position\":{\"x\":-2,\"y\":-1},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierPivot2\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierDetail\":3},{\"_position\":{\"x\":-2,\"y\":3},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierPivot2\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierDetail\":3},{\"_position\":{\"x\":5,\"y\":6},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierPivot2\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierDetail\":3},{\"_position\":{\"x\":12,\"y\":6},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierPivot2\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierDetail\":3},{\"_position\":{\"x\":10,\"y\":8},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierPivot2\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierDetail\":3},{\"_position\":{\"x\":-10,\"y\":8},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierPivot2\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierDetail\":3},{\"_position\":{\"x\":-18,\"y\":0},\"type\":0,\"bezierPivot1\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierPivot2\":{\"_position\":{\"x\":-2,\"y\":-6}},\"bezierDetail\":3}],\"pivot\":{\"_position\":{\"x\":0,\"y\":-8}}}],\"globalPivot\":{\"_position\":{\"x\":0,\"y\":0}},\"flipHorizontally\":false,\"flipVertically\":false,\"extrudeDepth\":1.0,\"extrudeClipDepth\":0.5,\"extrudeScale\":{\"x\":1.0,\"y\":1.0},\"revolve360\":8,\"revolveSteps\":4,\"revolveDistance\":1,\"revolveRadius\":1,\"revolveDirection\":true}");

        /// <summary>
        /// The extrude mode (latest operation used to build this brush).
        /// </summary>
        [SerializeField]
        private ExtrudeMode extrudeMode = ExtrudeMode.ExtrudeShape;

        /// <summary>
        /// The last built polygons determine the <see cref="BrushCount"/> needed to build the
        /// compound brush.
        /// </summary>
        [SerializeField]
        private int desiredBrushCount;

        /// <summary>
        /// Whether the geometry has been changed through one of the extrude methods.
        /// </summary>
        [SerializeField]
        private bool isDirty = true;

        /// <summary>
        /// Gets the beautiful name of the brush used in auto-generation of the hierarchy name.
        /// </summary>
        /// <value>The beautiful name of the brush.</value>
        public override string BeautifulBrushName
        {
            get
            {
                return "2D Shape Editor Brush";
            }
        }

        /// <summary>The last known extents of the compound brush to detect user resizing the bounds.</summary>
        private Vector3 m_LastKnownExtents;

        /// <summary>The last known position of the compound brush to prevent movement on resizing the bounds.</summary>
        private Vector3 m_LastKnownPosition;

        /// <summary>
        /// The last built polygons. Used to determine the <see cref="BrushCount"/> needed to build
        /// the compound brush.
        /// </summary>
        private List<Polygon> m_LastBuiltPolygons;

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
                // if the user desires a single concave brush we return 1.
                if (!project.convexBrushes)
                    return 1;

                // we already know the amount of brushes we need.
                if (!isDirty)
                    return desiredBrushCount;

                // build the polygons from the project to determine the brush count.
                if (m_LastBuiltPolygons == null)
                    m_LastBuiltPolygons = BuildConvexPolygons();

                return desiredBrushCount;
            }
        }

        public override void UpdateVisibility()
        {
        }

        public override void Invalidate(bool polygonsChanged)
        {
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
            Bounds csgBounds = new Bounds();

            // force nocsg when creating a flat polygon sheet as sabrecsg doesn't support it.
            if (extrudeMode == ExtrudeMode.CreatePolygon)
                this.IsNoCSG = true;

            // force nocsg when revolving with a sloped spiral as there are non-planar polygons.
            if (extrudeMode == ExtrudeMode.RevolveShape && project.revolveSpiralSloped && project.globalPivot.position.y != 0)
                this.IsNoCSG = true;

            // force nocsg when using concave brushes as sabrecsg doesn't support it.
            if (!project.convexBrushes)
                this.IsNoCSG = true;

            // nothing to do except copy csg information to our child brushes.
            if (!isDirty)
            {
                for (int i = 0; i < (project.convexBrushes ? desiredBrushCount : 1); i++)
                {
                    generatedBrushes[i].Mode = this.Mode;
                    generatedBrushes[i].IsNoCSG = this.IsNoCSG;
                    generatedBrushes[i].IsVisible = this.IsVisible;
                    generatedBrushes[i].HasCollision = this.HasCollision;
                    generatedBrushes[i].Invalidate(true);
                    csgBounds.Encapsulate(generatedBrushes[i].GetBounds());
                }
                // apply the generated csg bounds.
                localBounds = csgBounds;
                m_LastKnownExtents = localBounds.extents;
                m_LastKnownPosition = transform.localPosition;
                return;
            }

            base.Invalidate(polygonsChanged);
            isDirty = false;

            // build the polygons from the project.
            if (m_LastBuiltPolygons == null)
                m_LastBuiltPolygons = BuildConvexPolygons();

            // prepare a list of polygons for concave brushes.
            List<Polygon> concavePolygons = null;
            if (!project.convexBrushes)
                concavePolygons = new List<Polygon>();

            // iterate through the brushes we received:
            int brushCount = desiredBrushCount;

            // iterate through the brushes we received:
            for (int i = 0; i < brushCount; i++)
            {
                // copy our csg information to our child brushes.
                generatedBrushes[project.convexBrushes ? i : 0].Mode = this.Mode;
                generatedBrushes[project.convexBrushes ? i : 0].IsNoCSG = this.IsNoCSG;
                generatedBrushes[project.convexBrushes ? i : 0].IsVisible = this.IsVisible;
                generatedBrushes[project.convexBrushes ? i : 0].HasCollision = this.HasCollision;

                // local variables.
                Quaternion rot;
                Polygon[] outputPolygons;

                switch (extrudeMode)
                {
                    // generate a flat 2d polygon.
                    case ExtrudeMode.CreatePolygon:
                        GenerateNormals(m_LastBuiltPolygons[i]);
                        GenerateUvCoordinates(m_LastBuiltPolygons[i], false);
                        Polygon poly1 = m_LastBuiltPolygons[i].DeepCopy();
                        poly1.Flip();

                        if (project.convexBrushes)
                            generatedBrushes[i].SetPolygons(new Polygon[] { poly1 });
                        else
                            concavePolygons.Add(poly1);
                        break;

                    // generate 3d cube-ish shapes that revolve around the pivot and spirals up or down.
                    case ExtrudeMode.RevolveShape:
                        float spiralHeight = ((((project.globalPivot.position.y * project.extrudeScale.y) / 8.0f) * (i / m_LastBuiltPolygons.Count)) / project.revolve360) * (project.revolve360 / project.revolveSteps);
                        float spiralStep = ((((project.globalPivot.position.y * project.extrudeScale.y) / 8.0f)) / project.revolve360) * (project.revolve360 / project.revolveSteps);
                        int labpIndex = i % m_LastBuiltPolygons.Count;

                        Polygon poly2 = m_LastBuiltPolygons[labpIndex].DeepCopy();
                        poly2.Flip();
                        GenerateUvCoordinates(poly2, false);
                        foreach (Vertex v in poly2.Vertices)
                        {
                            float step = 360.0f / project.revolve360;
                            v.Position = new Vector3(0, -spiralHeight, 0) + RotatePointAroundPivot(v.Position, new Vector3(((project.revolveDistance / 8.0f) * project.extrudeScale.x) + ((project.revolveRadius * project.extrudeScale.x) / 8.0f), 0.0f, 0.0f), new Vector3(0.0f, ((i / m_LastBuiltPolygons.Count) * step), 0.0f));
                        }
                        GenerateNormals(poly2);

                        Polygon nextPoly = m_LastBuiltPolygons[labpIndex].DeepCopy();
                        nextPoly.Flip();
                        foreach (Vertex v in nextPoly.Vertices)
                        {
                            float step = 360.0f / project.revolve360;
                            v.Position = new Vector3(0, -spiralHeight - (project.revolveSpiralSloped ? spiralStep : 0), 0) + RotatePointAroundPivot(v.Position, new Vector3(((project.revolveDistance / 8.0f) * project.extrudeScale.x) + ((project.revolveRadius * project.extrudeScale.x) / 8.0f), 0.0f, 0.0f), new Vector3(0.0f, (((i / m_LastBuiltPolygons.Count) * step) + step), 0.0f));
                        }
                        List<Polygon> polygons = new List<Polygon>() { poly2 };
                        List<Vertex> backPolyVertices = new List<Vertex>();
                        Edge[] myEdges = poly2.GetEdges();
                        Edge[] nextEdges = nextPoly.GetEdges();
                        for (int j = 0; j < myEdges.Length; j++)
                        {
                            Edge myEdge = myEdges[j];
                            Edge nextEdge = nextEdges[j];

                            Polygon newPoly = new Polygon(new Vertex[] {
                                new Vertex(myEdge.Vertex1.Position, Vector3.zero, Vector2.zero),
                                new Vertex(nextEdge.Vertex1.Position, Vector3.zero, Vector2.zero),
                                new Vertex(nextEdge.Vertex2.Position, Vector3.zero, Vector2.zero),
                                new Vertex(myEdge.Vertex2.Position, Vector3.zero, Vector2.zero),
                            }, null, false, false);

                            backPolyVertices.Add(nextEdge.Vertex1);
                            GenerateNormals(newPoly);
                            if (newPoly.Plane.normal == Vector3.zero) continue; // discard single line, can happen in the center of the shape.
                            GenerateUvCoordinates(newPoly, false);
                            polygons.Add(newPoly);
                        }

                        Polygon backPoly = new Polygon(backPolyVertices.ToArray(), null, false, false);

                        backPoly.Flip();
                        GenerateNormals(backPoly);
                        GenerateUvCoordinates(backPoly, false);
                        polygons.Add(backPoly);

                        if (project.convexBrushes)
                            generatedBrushes[i].SetPolygons(polygons.ToArray());
                        else
                            concavePolygons.AddRange(polygons);
                        break;

                    // generate a 3d cube-ish shape.
                    case ExtrudeMode.ExtrudeShape:
                        GenerateNormals(m_LastBuiltPolygons[i]);
                        SurfaceUtility.ExtrudePolygon(m_LastBuiltPolygons[i], project.extrudeDepth, out outputPolygons, out rot);
                        foreach (Polygon poly in outputPolygons)
                            GenerateUvCoordinates(poly, false);
                        if (project.convexBrushes)
                            generatedBrushes[i].SetPolygons(outputPolygons);
                        else
                            concavePolygons.AddRange(outputPolygons);
                        break;

                    // generate a 3d cone-ish shape.
                    case ExtrudeMode.ExtrudePoint:
                        GenerateNormals(m_LastBuiltPolygons[i]);
                        ExtrudePolygonToPoint(m_LastBuiltPolygons[i], project.extrudeDepth, new Vector2((project.globalPivot.position.x * project.extrudeScale.x) / 8.0f, -(project.globalPivot.position.y * project.extrudeScale.y) / 8.0f), out outputPolygons, out rot);
                        foreach (Polygon poly in outputPolygons)
                            GenerateUvCoordinates(poly, false);
                        if (project.convexBrushes)
                            generatedBrushes[i].SetPolygons(outputPolygons);
                        else
                            concavePolygons.AddRange(outputPolygons);
                        break;

                    // generate a 3d trapezoid-ish shape.
                    case ExtrudeMode.ExtrudeBevel:
                        GenerateNormals(m_LastBuiltPolygons[i]);
                        ExtrudePolygonBevel(m_LastBuiltPolygons[i], project.extrudeDepth, project.extrudeClipDepth / project.extrudeDepth, new Vector2((project.globalPivot.position.x * project.extrudeScale.x) / 8.0f, -(project.globalPivot.position.y * project.extrudeScale.y) / 8.0f), out outputPolygons, out rot);
                        foreach (Polygon poly in outputPolygons)
                            GenerateUvCoordinates(poly, false);
                        if (project.convexBrushes)
                            generatedBrushes[i].SetPolygons(outputPolygons);
                        else
                            concavePolygons.AddRange(outputPolygons);
                        break;
                }

                // we invalidate every brush after hidden surface removal.
            }

            // we exclude hidden faces automatically.
            // this step will automatically optimize NoCSG output the same way additive brushes would have.
            // it also excludes a couple faces that CSG doesn't exclude due to floating point precision errors.
            // the latter is especially noticable with complex revolved shapes.

            // hidden surface removal for convex brushes.
            if (project.convexBrushes)
            {
                // compare each brush to another brush:
                for (int i = 0; i < brushCount; i++)
                {
                    for (int j = 0; j < brushCount; j++)
                    {
                        // can't check for hidden faces on the same brush.
                        if (i == j) continue;

                        // compare each polygon on brush i to each polygon on brush j:
                        foreach (Polygon pa in generatedBrushes[i].GetPolygons())
                        {
                            foreach (Polygon pb in generatedBrushes[j].GetPolygons())
                            {
                                // check they both have this polygon:
                                bool identical = true;
                                foreach (Vertex va in pa.Vertices)
                                {
                                    if (!pb.Vertices.Any(vb => vb.Position == va.Position))
                                    {
                                        identical = false;
                                        break;
                                    }
                                }
                                // identical polygons on both brushes means it can be excluded:
                                if (identical)
                                {
                                    pa.UserExcludeFromFinal = true;
                                    pb.UserExcludeFromFinal = true;
                                }
                            }
                        }
                    }

                    // invalidate every brush.
                    generatedBrushes[i].Invalidate(true);
                    csgBounds.Encapsulate(generatedBrushes[i].GetBounds());
                }
            }

            // hidden surface removal for a concave brush.
            else
            {
                List<Polygon> concavePolygonsCopy = concavePolygons.ToList();

                // compare each polygon and find duplicates:
                foreach (Polygon pa in concavePolygonsCopy)
                {
                    foreach (Polygon pb in concavePolygonsCopy)
                    {
                        // can't be the same polygon.
                        if (pa == pb) continue;

                        // check they both have this polygon:
                        bool identical = true;
                        foreach (Vertex va in pa.Vertices)
                        {
                            if (!pb.Vertices.Any(vb => vb.Position == va.Position))
                            {
                                identical = false;
                                break;
                            }
                        }
                        // identical polygons on both brushes means it can be excluded:
                        if (identical)
                        {
                            concavePolygons.Remove(pa);
                            concavePolygons.Remove(pb);
                        }
                    }
                }

                // invalidate the brush.
                generatedBrushes[0].SetPolygons(concavePolygons.ToArray());
                csgBounds.Encapsulate(generatedBrushes[0].GetBounds());
            }

            // apply the generated csg bounds.
            localBounds = csgBounds;
            m_LastKnownExtents = localBounds.extents;
            m_LastKnownPosition = transform.localPosition;
            // update the generated name in the hierarchy.
            UpdateGeneratedHierarchyName();
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
                        vertices.Add(new Vector2(segment.position.x * project.extrudeScale.x, segment.position.y * -1.0f * project.extrudeScale.y) / 8.0f);
                    }

                    // bezier segment:
                    else
                    {
                        foreach (Edge edge in GetBezierEdges(segment, GetNextSegment(shape, segment)))
                        {
                            vertices.Add(new Vector2(edge.Vertex1.Position.x * project.extrudeScale.x, edge.Vertex1.Position.y * -1.0f * project.extrudeScale.y) / 8.0f);
                        }
                    }
                }

                // in project v1 we use the horizontal and vertical flags to keep track of the correct winding order.
                Vector2[] inputVertices = vertices.ToArray();
                if (project.flipHorizontally && !project.flipVertically)
                    inputVertices = ReverseWindingOrder(inputVertices);
                if (!project.flipHorizontally && project.flipVertically)
                    inputVertices = ReverseWindingOrder(inputVertices);

                // create convex polygons:
                inputVertices = ReverseWindingOrder(inputVertices);
                List<Vector2[]> convexPolygonsVertices = BayazitDecomposer.ConvexPartition(inputVertices);

                foreach (Vector2[] polyVertices in convexPolygonsVertices)
                {
                    List<Vertex> vertexList = new List<Vertex>();
                    foreach (Vector2 vert in polyVertices)
                        vertexList.Add(new Vertex(vert, Vector3.zero, Vector2.zero));

                    polygons.Add(new Polygon(vertexList.ToArray(), null, false, false));
                }

                switch (extrudeMode)
                {
                    case ExtrudeMode.CreatePolygon:
                        // we make a brush for every polgon.
                        desiredBrushCount = polygons.Count;
                        break;

                    case ExtrudeMode.RevolveShape:
                        // we need another brush for every revolve step.
                        desiredBrushCount = polygons.Count * project.revolveSteps;
                        break;

                    case ExtrudeMode.ExtrudeShape:
                        // we make a brush for every polgon.
                        desiredBrushCount = polygons.Count;
                        break;

                    case ExtrudeMode.ExtrudePoint:
                        // we make a brush for every polgon.
                        desiredBrushCount = polygons.Count;
                        break;

                    case ExtrudeMode.ExtrudeBevel:
                        // we make a brush for every polgon.
                        desiredBrushCount = polygons.Count;
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

        /// <summary>
        /// Reverses the winding order for a set of vertices.
        /// </summary>
        /// <param name="vertices">The vertices of the polygon.</param>
        /// <returns>The new vertices for the polygon with the opposite winding order.</returns>
        private static Vector2[] ReverseWindingOrder(Vector2[] vertices)
        {
            Vector2[] newVerts = new Vector2[vertices.Length];
            newVerts[0] = vertices[0];
            for (int i = 1; i < newVerts.Length; i++)
                newVerts[i] = vertices[vertices.Length - i];
            return newVerts;
        }

        /// <summary>
        /// Creates a brush by extruding a supplied polygon by a specified extrusion distance and end it in a point.
        /// </summary>
        /// <param name="sourcePolygon">Source polygon, typically transformed into world space.</param>
        /// <param name="extrusionDistance">Extrusion distance, this is the height (or depth) of the created geometry perpendicular to the source polygon.</param>
        /// <param name="offset">The offset of the point.</param>
        /// <param name="outputPolygons">Output brush polygons.</param>
        /// <param name="rotation">The rotation to be supplied to the new brush transform.</param>
        private static void ExtrudePolygonToPoint(Polygon sourcePolygon, float extrusionDistance, Vector2 offset, out Polygon[] outputPolygons, out Quaternion rotation)
        {
            bool flipped = false;
            if (extrusionDistance < 0)
            {
                sourcePolygon.Flip();
                extrusionDistance = -extrusionDistance;
                flipped = true;
            }

            // Create base polygon
            Polygon basePolygon = sourcePolygon.DeepCopy();
            basePolygon.UniqueIndex = -1;

            rotation = Quaternion.LookRotation(basePolygon.Plane.normal);
            Quaternion cancellingRotation = Quaternion.Inverse(rotation);

            Vertex[] vertices = basePolygon.Vertices;

            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i].Position = cancellingRotation * vertices[i].Position;

                vertices[i].Normal = cancellingRotation * vertices[i].Normal;
            }

            basePolygon.Vertices = vertices;

            // Create the opposite polygon by duplicating the base polygon, offsetting and flipping
            Vector3 normal = basePolygon.Plane.normal;

            basePolygon.Flip();

            // Now create each of the brush side polygons
            Polygon[] brushSides = new Polygon[sourcePolygon.Vertices.Length];

            for (int i = 0; i < basePolygon.Vertices.Length; i++)
            {
                Vertex vertex1 = basePolygon.Vertices[i].DeepCopy();
                Vertex vertex2 = basePolygon.Vertices[(i + 1) % basePolygon.Vertices.Length].DeepCopy();

                // Create new UVs for the sides, otherwise we'll get distortion

                float sourceDistance = Vector3.Distance(vertex1.Position, vertex2.Position);
                float uvDistance = Vector2.Distance(vertex1.UV, vertex2.UV);

                float uvScale = sourceDistance / uvDistance;

                vertex1.UV = Vector2.zero;
                if (flipped)
                {
                    vertex2.UV = new Vector2(-sourceDistance / uvScale, 0);
                }
                else
                {
                    vertex2.UV = new Vector2(sourceDistance / uvScale, 0);
                }

                Vector2 uvDelta = vertex2.UV - vertex1.UV;

                Vector2 rotatedUVDelta = uvDelta.Rotate(90) * (extrusionDistance / sourceDistance);

                Vertex vertex3 = vertex1.DeepCopy();
                vertex3.Position += normal * extrusionDistance;
                vertex3.UV += rotatedUVDelta;

                Vertex vertex4 = vertex2.DeepCopy();
                vertex4.Position += normal * extrusionDistance;
                vertex4.UV += rotatedUVDelta;

                // end in a point.
                vertex3.Position.x = ((normal * extrusionDistance).x * 0.5f) + offset.x;
                vertex3.Position.y = ((normal * extrusionDistance).y * 0.5f) + offset.y;

                Vertex[] newVertices = new Vertex[] { vertex1, vertex2, vertex3 };

                brushSides[i] = new Polygon(newVertices, sourcePolygon.Material, false, false);
                brushSides[i].Flip();
                brushSides[i].ResetVertexNormals();
            }

            List<Polygon> polygons = new List<Polygon>();
            polygons.Add(basePolygon);
            //polygons.Add(oppositePolygon);
            polygons.AddRange(brushSides);

            outputPolygons = polygons.ToArray();
        }

        /// <summary>
        /// Creates a brush by extruding a supplied polygon by a specified extrusion distance towards
        /// a point and then capping it to build a trapezoid.
        /// </summary>
        /// <param name="sourcePolygon">Source polygon, typically transformed into world space.</param>
        /// <param name="extrusionDistance">
        /// Extrusion distance, this is the height (or depth) of the created geometry perpendicular
        /// to the source polygon.
        /// </param>
        /// <param name="clip">Where to clip the new polygon (0.0f - 1.0f).</param>
        /// <param name="offset">The offset of the point.</param>
        /// <param name="outputPolygons">Output brush polygons.</param>
        /// <param name="rotation">The rotation to be supplied to the new brush transform.</param>
        private static void ExtrudePolygonBevel(Polygon sourcePolygon, float extrusionDistance, float clip, Vector2 offset, out Polygon[] outputPolygons, out Quaternion rotation)
        {
            // cap the max extrusion distance.
            float originalExtrusionDistance = extrusionDistance;
            extrusionDistance = extrusionDistance * clip;

            bool flipped = false;
            if (extrusionDistance < 0)
            {
                sourcePolygon.Flip();
                extrusionDistance = -extrusionDistance;
                flipped = true;
            }

            // Create base polygon
            Polygon basePolygon = sourcePolygon.DeepCopy();
            basePolygon.UniqueIndex = -1;

            rotation = Quaternion.LookRotation(basePolygon.Plane.normal);
            Quaternion cancellingRotation = Quaternion.Inverse(rotation);

            Vertex[] vertices = basePolygon.Vertices;

            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i].Position = cancellingRotation * vertices[i].Position;

                vertices[i].Normal = cancellingRotation * vertices[i].Normal;
            }

            basePolygon.Vertices = vertices;

            // Create the opposite polygon by duplicating the base polygon, offsetting and flipping
            Vector3 normal = basePolygon.Plane.normal;
            Polygon oppositePolygon = basePolygon.DeepCopy();
            oppositePolygon.UniqueIndex = -1;

            basePolygon.Flip();

            vertices = oppositePolygon.Vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i].Position += normal * extrusionDistance;
                //vertices[i].Position.x *= Mathf.Lerp( (originalExtrusionDistance * clip);
                //vertices[i].Position.y *= clip;
                vertices[i].Position.x = Mathf.Lerp(vertices[i].Position.x, ((normal * originalExtrusionDistance).x * 0.5f) + offset.x, clip);
                vertices[i].Position.y = Mathf.Lerp(vertices[i].Position.y, ((normal * originalExtrusionDistance).y * 0.5f) + offset.y, clip);
                //				vertices[i].UV.x *= -1; // Flip UVs
            }
            oppositePolygon.Vertices = vertices;

            // Now create each of the brush side polygons
            Polygon[] brushSides = new Polygon[sourcePolygon.Vertices.Length];

            for (int i = 0; i < basePolygon.Vertices.Length; i++)
            {
                Vertex vertex1 = basePolygon.Vertices[i].DeepCopy();
                Vertex vertex2 = basePolygon.Vertices[(i + 1) % basePolygon.Vertices.Length].DeepCopy();

                // Create new UVs for the sides, otherwise we'll get distortion

                float sourceDistance = Vector3.Distance(vertex1.Position, vertex2.Position);
                float uvDistance = Vector2.Distance(vertex1.UV, vertex2.UV);

                float uvScale = sourceDistance / uvDistance;

                vertex1.UV = Vector2.zero;
                if (flipped)
                {
                    vertex2.UV = new Vector2(-sourceDistance / uvScale, 0);
                }
                else
                {
                    vertex2.UV = new Vector2(sourceDistance / uvScale, 0);
                }

                Vector2 uvDelta = vertex2.UV - vertex1.UV;

                Vector2 rotatedUVDelta = uvDelta.Rotate(90) * (extrusionDistance / sourceDistance);

                Vertex vertex3 = vertex1.DeepCopy();
                vertex3.Position += normal * extrusionDistance;
                vertex3.UV += rotatedUVDelta;

                Vertex vertex4 = vertex2.DeepCopy();
                vertex4.Position += normal * extrusionDistance;
                vertex4.UV += rotatedUVDelta;

                // end in a point.
                vertex3.Position.x = Mathf.Lerp(vertex3.Position.x, ((normal * originalExtrusionDistance).x * 0.5f) + offset.x, clip);
                vertex3.Position.y = Mathf.Lerp(vertex3.Position.y, ((normal * originalExtrusionDistance).y * 0.5f) + offset.y, clip);

                vertex4.Position.x = Mathf.Lerp(vertex4.Position.x, ((normal * originalExtrusionDistance).x * 0.5f) + offset.x, clip);
                vertex4.Position.y = Mathf.Lerp(vertex4.Position.y, ((normal * originalExtrusionDistance).y * 0.5f) + offset.y, clip);

                Vertex[] newVertices = new Vertex[] { vertex1, vertex2, vertex4, vertex3 };

                brushSides[i] = new Polygon(newVertices, sourcePolygon.Material, false, false);
                brushSides[i].Flip();
                brushSides[i].ResetVertexNormals();
            }

            List<Polygon> polygons = new List<Polygon>();
            polygons.Add(basePolygon);
            polygons.Add(oppositePolygon);
            polygons.AddRange(brushSides);

            outputPolygons = polygons.ToArray();
        }

        private Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles)
        {
            return Quaternion.Euler(angles) * (point - pivot) + pivot;
        }

        /// <summary>
        /// Flips a project horizontally. This code was stolen from the ShapeEditorWindow.
        /// </summary>
        private void FlipProjectHorizontally()
        {
            // store this flip inside of the project.
            project.flipHorizontally = !project.flipHorizontally;

            foreach (Shape shape in project.shapes)
            {
                foreach (Segment segment in shape.segments)
                {
                    // flip segment.
                    segment.position = new Vector2Int(-segment.position.x + (project.globalPivot.position.x * 2), segment.position.y);
                    // flip bezier pivot handles.
                    segment.bezierPivot1.position = new Vector2Int(-segment.bezierPivot1.position.x + (project.globalPivot.position.x * 2), segment.bezierPivot1.position.y);
                    segment.bezierPivot2.position = new Vector2Int(-segment.bezierPivot2.position.x + (project.globalPivot.position.x * 2), segment.bezierPivot2.position.y);
                }

                // recalculate the pivot position of the shape.
                shape.CalculatePivotPosition();
            }
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
            isDirty = true;
            Invalidate(true);
        }

        /// <summary>
        /// Creates an extruded shape that revolves around. Also called by the 2D Shape Editor window.
        /// </summary>
        /// <param name="project">The project to be copied into the brush.</param>
        public void RevolveShape(Project project)
        {
            // store a project copy inside of this brush.
            this.project = project.Clone();
            // flip project horizontally if revolving left.
            if (!this.project.revolveDirection) FlipProjectHorizontally();
            // store the extrude mode inside of this brush.
            extrudeMode = ExtrudeMode.RevolveShape;
            // build the polygons out of the project.
            m_LastBuiltPolygons = BuildConvexPolygons();
            // build the brush.
            isDirty = true;
            Invalidate(true);
            // un-flip project horizontally if revolving left.
            if (!this.project.revolveDirection) FlipProjectHorizontally();
        }

        /// <summary>
        /// Creates an extruded shape. Also called by the 2D Shape Editor window.
        /// </summary>
        /// <param name="project">The project to be copied into the brush.</param>
        public void ExtrudeShape(Project project)
        {
            // store a project copy inside of this brush.
            this.project = project.Clone();
            // store the extrude mode inside of this brush.
            extrudeMode = ExtrudeMode.ExtrudeShape;
            // build the polygons out of the project.
            m_LastBuiltPolygons = BuildConvexPolygons();
            // build the brush.
            isDirty = true;
            Invalidate(true);
        }

        /// <summary>
        /// Creates an extruded shape that ends in a point like a cone. Also called by the 2D Shape Editor window.
        /// </summary>
        /// <param name="project">The project to be copied into the brush.</param>
        public void ExtrudePoint(Project project)
        {
            // store a project copy inside of this brush.
            this.project = project.Clone();
            // store the extrude mode inside of this brush.
            extrudeMode = ExtrudeMode.ExtrudePoint;
            // build the polygons out of the project.
            m_LastBuiltPolygons = BuildConvexPolygons();
            // build the brush.
            isDirty = true;
            Invalidate(true);
        }

        /// <summary>
        /// Creates an extruded shape towards a point but is then capped causing a trapezoid. Also
        /// called by the 2D Shape Editor window.
        /// </summary>
        /// <param name="project">The project to be copied into the brush.</param>
        public void ExtrudeBevel(Project project)
        {
            // if the depth and clip depth are identical, extrude a point instead.
            if (project.extrudeDepth.EqualsWithEpsilon(project.extrudeClipDepth))
            {
                ExtrudePoint(project);
                return;
            }

            // store a project copy inside of this brush.
            this.project = project.Clone();
            // store the extrude mode inside of this brush.
            extrudeMode = ExtrudeMode.ExtrudeBevel;
            // build the polygons out of the project.
            m_LastBuiltPolygons = BuildConvexPolygons();
            // build the brush.
            isDirty = true;
            Invalidate(true);
        }
    }
}

#endif