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
    /// Mark Bayazit for your implementation of Mark Keil's Algorithm https://mpen.ca/406/keil.
    /// </remarks>
    /// <seealso cref="Sabresaurus.SabreCSG.CompoundBrush"/>
    [ExecuteInEditMode]
    public class ShapeEditorBrush : CompoundBrush
    {
        public enum ExtrudeMode
        {
            ExtrudeShape
        }

        /// <summary>The 2D Shape Editor Project (latest project used to build this brush).</summary>
        [SerializeField]
        Project project = new Project();

        [SerializeField]
        ExtrudeMode extrudeMode = ExtrudeMode.ExtrudeShape;

        public void ExtrudeShape(Project project)
        {
            this.project = project;
            extrudeMode = ExtrudeMode.ExtrudeShape;

            m_LastBuiltPolygons = BuildConvexPolygons();

            Invalidate(true);
        }

        /// <summary>The last known extents of the compound brush to detect user resizing the bounds.</summary>
        private Vector3 m_LastKnownExtents;
        /// <summary>The last known position of the compound brush to prevent movement on resizing the bounds.</summary>
        private Vector3 m_LastKnownPosition;

        private List<Polygon> m_LastBuiltPolygons;

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
                if (m_LastBuiltPolygons == null)
                {
                    return 1;
                }
                else
                {
                    return m_LastBuiltPolygons.Count;
                }
            }
        }

        public override void UpdateVisibility()
        {
        }

        public override void Invalidate(bool polygonsChanged)
        {
            if (m_LastBuiltPolygons == null) return;

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

            Bounds csgBounds = new Bounds();
            for (int i = 0; i < brushCount; i++)
            {
                // copy our csg information to our child brushes.
                generatedBrushes[i].Mode = this.Mode;
                generatedBrushes[i].IsNoCSG = true;//this.IsNoCSG;
                generatedBrushes[i].IsVisible = this.IsVisible;
                generatedBrushes[i].HasCollision = this.HasCollision;

                // retrieve the polygons from the current cube brush.




                //List<Polygon> polygons = new List<Polygon>();//generatedBrushes[i].GetPolygons().ToList();

                ////Debug.Log("here");

                //// decompose concave shape from project into convex polygons.
                //for (int j = 0; i < project.shapes[i].segments.Count; i+=3)
                //{
                //    polygons.Add(new Polygon(
                //        new Vertex[] {
                //            new Vertex((Vector2)project.shapes[i].segments[j + 0].position, Vector3.up, Vector3.zero),
                //            new Vertex((Vector2)project.shapes[i].segments[j + 1].position, Vector3.up, Vector3.zero),
                //            new Vertex((Vector2)project.shapes[i].segments[j + 2].position, Vector3.up, Vector3.zero)
                //        },
                //        null, false, false
                //    ));
                //}
                //KeilPolygonDecomposition()

                generatedBrushes[i].SetPolygons(new Polygon[] { m_LastBuiltPolygons[i] });

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

        private List<Polygon> BuildConvexPolygons()
        {
            Polygon polygon = new Polygon(new Vertex[] { new Vertex(), new Vertex(), new Vertex() }, null, false, false);
            List<Vertex> vertices = new List<Vertex>();

            foreach (Segment segment in project.shapes[0].segments)
            {
                if (segment.type == SegmentType.Linear)
                {
                    vertices.Add(new Vertex((Vector2)segment.position, Vector3.zero, Vector2.zero));
                }
                else
                {
                    foreach(Edge edge in GetBezierEdges(segment, GetNextSegment(project.shapes[0], segment)))
                    {
                        vertices.Add(new Vertex(edge.Vertex1.Position, Vector3.zero, Vector2.zero));
                    }
                }
            }

            polygon.Vertices = vertices.ToArray();

            List<Edge> edges = KeilPolygonDecomposition(polygon);

            makeCCW(polygon);
            List<Polygon> polygons = new List<Polygon>();

            if (edges.Count == 0)
            {
                polygons.Add(polygon);
            }
            else
            {
                //List<Polygon> resultPolygons = new List<Polygon>();
                //SplitThem(resultPolygons, polygon.Vertices.ToList(), edges.ToList(), edges[0]);
                foreach (Edge edge in edges)
                {
                    Polygon frontPolygon;
                    Polygon backPolygon;
                    Vertex v1;
                    Vertex v2;
                    if (Polygon.SplitPolygon(polygon, out frontPolygon, out backPolygon, out v1, out v2, new Plane(edge.Vertex1.Position, edge.Vertex2.Position, Vector3.forward)))
                    {
                        polygons.Add(frontPolygon);
                        polygons.Add(backPolygon);
                    }
                    else
                    {
                        Debug.Log("Polygon.SplitPolygon Failure");
                    }
                }
            }

            Debug.Log(polygons.Count.ToString() + " x " + edges.Count.ToString());
            return polygons;
        }

        //private void SplitThem(List<Polygon> resultPolygons, List<Vertex> polygonVertices, List<Edge> keilEdges, Edge currentEdge)
        //{
        //    // follow the vertices left until we reach the other end of our edge.
        //    bool start = false;
        //    foreach (var v in polygonVertices)
        //    {
        //        if (v == currentEdge.Vertex1)
        //            start = true;
        //        if (v == currentEdge.Vertex2)
        //    }

        //}

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

        private static float Area(Vector2 a, Vector2 b, Vector2 c) {
            return (((b.x - a.x)*(c.y - a.y))-((c.x - a.x)*(b.y - a.y)));
        }

        private static bool left(Vector2 a, Vector2 b, Vector2 c) {
            return Area(a, b, c) > 0;
        }

        private static bool leftOn(Vector2 a, Vector2 b, Vector2 c) {
            return Area(a, b, c) >= 0;
        }

        private static bool right(Vector2 a, Vector2 b, Vector2 c) {
            return Area(a, b, c) < 0;
        }

        private static bool rightOn(Vector2 a, Vector2 b, Vector2 c) {
            return Area(a, b, c) <= 0;
        }

        private static bool collinear(Vector2 a, Vector2 b, Vector2 c) {
            return Area(a, b, c) == 0;
        }

        private static float sqdist(Vector2 a, Vector2 b) {
            float dx = b.x - a.x;
            float dy = b.y - a.y;
            return dx * dx + dy * dy;
        }

        private static Vector2 at(Polygon polygon, int i) {
            int s = polygon.Vertices.Length;
            return polygon.Vertices[i < 0 ? i % s + s : i % s].Position;
        }

        private static bool isReflex(Polygon polygon, int i) {
            return right(at(polygon, i - 1), at(polygon, i), at(polygon, i + 1));
        }

        private static bool eq(float a, float b) {
            return Mathf.Abs(a - b) <= 1e-5f;
        }

        private static Vector2 lineInt(Edge l1, Edge l2) {
            Vector2 i = new Vector2();
            float a1, b1, c1, a2, b2, c2, det;
            a1 = l1.Vertex2.Position.y - l1.Vertex1.Position.y;
            b1 = l1.Vertex1.Position.x - l1.Vertex2.Position.x;
            c1 = a1 * l1.Vertex1.Position.x + b1 * l1.Vertex1.Position.y;
            a2 = l2.Vertex2.Position.y - l2.Vertex1.Position.y;
            b2 = l2.Vertex1.Position.x - l2.Vertex2.Position.x;
            c2 = a2 * l2.Vertex1.Position.x + b2 * l2.Vertex1.Position.y;
            det = a1 * b2 - a2*b1;
            if (!eq(det, 0)) { // lines are not parallel
                i.x = (b2 * c1 - b1 * c2) / det;
                i.y = (a1 * c2 - a2 * c1) / det;
            }
            return i;
        }

        private static Edge Line(Vector2 a, Vector2 b)
        {
            return new Edge(new Vertex(a, Vector3.zero, Vector2.zero), new Vertex(b, Vector3.zero, Vector2.zero));
        }

        private static bool canSee(Polygon polygon, int a, int b) {
            Vector2 p;
            float dist;

            if (leftOn(at(polygon, a + 1), at(polygon, a), at(polygon, b)) && rightOn(at(polygon, a - 1), at(polygon, a), at(polygon, b))) {
                return false;
            }
            dist = sqdist(at(polygon, a), at(polygon, b));
            for (int i = 0; i < polygon.Vertices.Length; ++i) { // for each edge
                if ((i + 1) % polygon.Vertices.Length == a || i == a) // ignore incident edges
                    continue;
                if (leftOn(at(polygon, a), at(polygon, b), at(polygon, i + 1)) && rightOn(at(polygon, a), at(polygon, b), at(polygon, i))) { // if diag intersects an edge
                    p = lineInt(Line(at(polygon, a), at(polygon, b)), Line(at(polygon, i), at(polygon, i + 1)));
                    if (sqdist(at(polygon, a), p) < dist) { // if edge is blocking visibility to b
                        return false;
                    }
                }
            }

            return true;
        }

        private static Polygon copy(Polygon polygon, int i, int j) {
            Polygon p = new Polygon(new Vertex[] { new Vertex(), new Vertex(), new Vertex() }, null, false, false);
            if (i < j) {
                //p.v.insert(p.v.begin(), v.begin() + i, v.begin() + j + 1);
                List<Vertex> v = new List<Vertex>();//polygon.Vertices.ToList();
                v.InsertRange(0, polygon.Vertices.Skip(i).Take(j + 1));
                p.Vertices = v.ToArray();
            } else {
                //p.v.insert(p.v.begin(), v.begin() + i, v.end());
                //p.v.insert(p.v.end(), v.begin(), v.begin() + j + 1);
                List<Vertex> v = new List<Vertex>();
                v.InsertRange(0, polygon.Vertices.Skip(i));
                v.AddRange(polygon.Vertices.Take(j + 1));
                p.Vertices = v.ToArray();
            }
            return p;
        }

        private static void makeCCW(Polygon polygon)
        {
            int br = 0;

            // find bottom right point
            for (int i = 1; i < polygon.Vertices.Length; ++i)
            {
                if (polygon.Vertices[i].Position.y < polygon.Vertices[br].Position.y || (polygon.Vertices[i].Position.y == polygon.Vertices[br].Position.y && polygon.Vertices[i].Position.x > polygon.Vertices[br].Position.x))
                {
                    br = i;
                }
            }

            // reverse poly if clockwise
            if (!left(at(polygon, br - 1), at(polygon, br), at(polygon, br + 1)))
            {
                reverse(polygon);
            }
        }

        private static void reverse(Polygon polygon)
        {
            polygon.Vertices = polygon.Vertices.Reverse().ToArray();
        }

        private static List<Edge> KeilPolygonDecomposition(Polygon polygon)
        {
            List<Edge> min = new List<Edge>();
            List<Edge> tmp1 = new List<Edge>();
            List<Edge> tmp2 = new List<Edge>();
            int ndiags = int.MaxValue;

            for (int i = 0; i < polygon.Vertices.Length; ++i)
            {
                if (isReflex(polygon, i))
                {
                    for (int j = 0; j < polygon.Vertices.Length; ++j)
                    {
                        if (canSee(polygon, i, j))
                        {
                            tmp1 = KeilPolygonDecomposition(copy(polygon, i, j));
                            tmp2 = KeilPolygonDecomposition(copy(polygon, j, i));
                            //tmp1.insert(tmp1.end(), tmp2.begin(), tmp2.end());
                            tmp1.AddRange(tmp2);
                            if (tmp1.Count < ndiags)
                            {
                                min = tmp1;
                                ndiags = tmp1.Count;
                                min.Add(Line(at(polygon, i), at(polygon, j)));
                            }
                        }
                    }
                }
            }

            return min;
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
        /// Provides methods for calculating bezier splines.
        /// </summary>
        private static class Bezier
        {
            public static Vector3 GetPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
            {
                t = Mathf.Clamp01(t);
                float OneMinusT = 1f - t;
                return
                    OneMinusT * OneMinusT * OneMinusT * p0 +
                    3f * OneMinusT * OneMinusT * t * p1 +
                    3f * OneMinusT * t * t * p2 +
                    t * t * t * p3;
            }
        }
    }
}

#endif