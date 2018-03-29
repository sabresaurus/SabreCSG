#if UNITY_EDITOR || RUNTIME_CSG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Sabresaurus.SabreCSG.ShapeEditor.Decomposition
{
    //From the Poly2Tri project by Mason Green: http://code.google.com/p/poly2tri/source/browse?repo=archive#hg/scala/src/org/poly2tri/seidel

    /// <summary>
    /// Convex decomposition algorithm based on Raimund Seidel's paper "A simple and fast incremental randomized
    /// algorithm for computing trapezoidal decompositions and for triangulating polygons"
    /// See also: "Computational Geometry", 3rd edition, by Mark de Berg et al, Chapter 6.2
    ///           "Computational Geometry in C", 2nd edition, by Joseph O'Rourke
    /// </summary>
    public static class SeidelDecomposer
    {
        /// <summary>
        /// Decompose the polygon into several smaller non-concave polygon.
        /// </summary>
        /// <param name="vertices">The polygon to decompose.</param>
        /// <param name="sheer">The sheer to use. If you get bad results, try using a higher value. The default value is 0.001</param>
        /// <returns>A list of triangles</returns>
        public static List<Vector2[]> ConvexPartition(Vector2[] vertices, float sheer)
        {
            List<Point> compatList = new List<Point>(vertices.Length);

            foreach (Vector2 vertex in vertices)
            {
                compatList.Add(new Point(vertex.x, vertex.y));
            }

            Triangulator t = new Triangulator(compatList, sheer);

            List<Vector2[]> list = new List<Vector2[]>();

            foreach (List<Point> triangle in t.Triangles)
            {
                List<Vector2> verts = new List<Vector2>(triangle.Count);

                foreach (Point point in triangle)
                {
                    verts.Add(new Vector2(point.X, point.Y));
                }

                list.Add(verts.ToArray());
            }

            return list;
        }

        /// <summary>
        /// Decompose the polygon into several smaller non-concave polygon.
        /// </summary>
        /// <param name="vertices">The polygon to decompose.</param>
        /// <param name="sheer">The sheer to use. If you get bad results, try using a higher value. The default value is 0.001</param>
        /// <returns>A list of trapezoids</returns>
        public static List<Vector2[]> ConvexPartitionTrapezoid(Vector2[] vertices, float sheer)
        {
            List<Point> compatList = new List<Point>(vertices.Length);

            foreach (Vector2 vertex in vertices)
            {
                compatList.Add(new Point(vertex.x, vertex.y));
            }

            Triangulator t = new Triangulator(compatList, sheer);

            List<Vector2[]> list = new List<Vector2[]>();

            foreach (Trapezoid trapezoid in t.Trapezoids)
            {
                List<Vector2> verts = new List<Vector2>();

                List<Point> points = trapezoid.Vertices();
                foreach (Point point in points)
                {
                    verts.Add(new Vector2(point.X, point.Y));
                }

                list.Add(verts.ToArray());
            }

            return list;
        }
    }

    internal class MonotoneMountain
    {
        private const float PiSlop = 3.1f;
        public List<List<Point>> Triangles;
        private HashSet<Point> _convexPoints;
        private Point _head;

        // Monotone mountain points
        private List<Point> _monoPoly;

        // Triangles that constitute the mountain

        // Used to track which side of the line we are on
        private bool _positive;
        private int _size;
        private Point _tail;

        // Almost Pi!

        public MonotoneMountain()
        {
            _size = 0;
            _tail = null;
            _head = null;
            _positive = false;
            _convexPoints = new HashSet<Point>();
            _monoPoly = new List<Point>();
            Triangles = new List<List<Point>>();
        }

        // Append a point to the list
        public void Add(Point point)
        {
            if (_size == 0)
            {
                _head = point;
                _size = 1;
            }
            else if (_size == 1)
            {
                // Keep repeat points out of the list
                _tail = point;
                _tail.Prev = _head;
                _head.Next = _tail;
                _size = 2;
            }
            else
            {
                // Keep repeat points out of the list
                _tail.Next = point;
                point.Prev = _tail;
                _tail = point;
                _size += 1;
            }
        }

        // Remove a point from the list
        public void Remove(Point point)
        {
            Point next = point.Next;
            Point prev = point.Prev;
            point.Prev.Next = next;
            point.Next.Prev = prev;
            _size -= 1;
        }

        // Partition a x-monotone mountain into triangles O(n)
        // See "Computational Geometry in C", 2nd edition, by Joseph O'Rourke, page 52
        public void Process()
        {
            // Establish the proper sign
            _positive = AngleSign();
            // create monotone polygon - for dubug purposes
            GenMonoPoly();

            // Initialize internal angles at each nonbase vertex
            // Link strictly convex vertices into a list, ignore reflex vertices
            Point p = _head.Next;
            while (p.Neq(_tail))
            {
                float a = Angle(p);
                // If the point is almost colinear with it's neighbor, remove it!
                if (a >= PiSlop || a <= -PiSlop || a == 0.0)
                    Remove(p);
                else if (IsConvex(p))
                    _convexPoints.Add(p);
                p = p.Next;
            }

            Triangulate();
        }

        private void Triangulate()
        {
            while (_convexPoints.Count != 0)
            {
                IEnumerator<Point> e = _convexPoints.GetEnumerator();
                e.MoveNext();
                Point ear = e.Current;

                _convexPoints.Remove(ear);
                Point a = ear.Prev;
                Point b = ear;
                Point c = ear.Next;
                List<Point> triangle = new List<Point>(3);
                triangle.Add(a);
                triangle.Add(b);
                triangle.Add(c);

                Triangles.Add(triangle);

                // Remove ear, update angles and convex list
                Remove(ear);
                if (Valid(a))
                    _convexPoints.Add(a);
                if (Valid(c))
                    _convexPoints.Add(c);
            }

            Debug.Assert(_size <= 3, "Triangulation bug, please report");
        }

        private bool Valid(Point p)
        {
            return p.Neq(_head) && p.Neq(_tail) && IsConvex(p);
        }

        // Create the monotone polygon
        private void GenMonoPoly()
        {
            Point p = _head;
            while (p != null)
            {
                _monoPoly.Add(p);
                p = p.Next;
            }
        }

        private float Angle(Point p)
        {
            Point a = (p.Next - p);
            Point b = (p.Prev - p);
            return (float)Math.Atan2(a.Cross(b), a.Dot(b));
        }

        private bool AngleSign()
        {
            Point a = (_head.Next - _head);
            Point b = (_tail - _head);
            return Math.Atan2(a.Cross(b), a.Dot(b)) >= 0;
        }

        // Determines if the inslide angle is convex or reflex
        private bool IsConvex(Point p)
        {
            if (_positive != (Angle(p) >= 0))
                return false;
            return true;
        }
    }

    // Node for a Directed Acyclic graph (DAG)
    internal abstract class Node
    {
        protected Node LeftChild;
        public List<Node> ParentList;
        protected Node RightChild;

        protected Node(Node left, Node right)
        {
            ParentList = new List<Node>();
            LeftChild = left;
            RightChild = right;

            if (left != null)
                left.ParentList.Add(this);
            if (right != null)
                right.ParentList.Add(this);
        }

        public abstract Sink Locate(Edge s);

        // Replace a node in the graph with this node
        // Make sure parent pointers are updated
        public void Replace(Node node)
        {
            foreach (Node parent in node.ParentList)
            {
                // Select the correct node to replace (left or right child)
                if (parent.LeftChild == node)
                    parent.LeftChild = this;
                else
                    parent.RightChild = this;
            }
            ParentList.AddRange(node.ParentList);
        }
    }

    // Directed Acyclic graph (DAG)
    // See "Computational Geometry", 3rd edition, by Mark de Berg et al, Chapter 6.2
    internal class QueryGraph
    {
        private Node _head;

        public QueryGraph(Node head)
        {
            _head = head;
        }

        private Trapezoid Locate(Edge edge)
        {
            return _head.Locate(edge).Trapezoid;
        }

        public List<Trapezoid> FollowEdge(Edge edge)
        {
            List<Trapezoid> trapezoids = new List<Trapezoid>();
            trapezoids.Add(Locate(edge));
            int j = 0;

            while (edge.Q.X > trapezoids[j].RightPoint.X)
            {
                if (edge.IsAbove(trapezoids[j].RightPoint))
                {
                    trapezoids.Add(trapezoids[j].UpperRight);
                }
                else
                {
                    trapezoids.Add(trapezoids[j].LowerRight);
                }
                j += 1;
            }
            return trapezoids;
        }

        private void Replace(Sink sink, Node node)
        {
            if (sink.ParentList.Count == 0)
                _head = node;
            else
                node.Replace(sink);
        }

        public void Case1(Sink sink, Edge edge, Trapezoid[] tList)
        {
            YNode yNode = new YNode(edge, Sink.Isink(tList[1]), Sink.Isink(tList[2]));
            XNode qNode = new XNode(edge.Q, yNode, Sink.Isink(tList[3]));
            XNode pNode = new XNode(edge.P, Sink.Isink(tList[0]), qNode);
            Replace(sink, pNode);
        }

        public void Case2(Sink sink, Edge edge, Trapezoid[] tList)
        {
            YNode yNode = new YNode(edge, Sink.Isink(tList[1]), Sink.Isink(tList[2]));
            XNode pNode = new XNode(edge.P, Sink.Isink(tList[0]), yNode);
            Replace(sink, pNode);
        }

        public void Case3(Sink sink, Edge edge, Trapezoid[] tList)
        {
            YNode yNode = new YNode(edge, Sink.Isink(tList[0]), Sink.Isink(tList[1]));
            Replace(sink, yNode);
        }

        public void Case4(Sink sink, Edge edge, Trapezoid[] tList)
        {
            YNode yNode = new YNode(edge, Sink.Isink(tList[0]), Sink.Isink(tList[1]));
            XNode qNode = new XNode(edge.Q, yNode, Sink.Isink(tList[2]));
            Replace(sink, qNode);
        }
    }

    internal class Sink : Node
    {
        public Trapezoid Trapezoid;

        private Sink(Trapezoid trapezoid)
            : base(null, null)
        {
            Trapezoid = trapezoid;
            trapezoid.Sink = this;
        }

        public static Sink Isink(Trapezoid trapezoid)
        {
            if (trapezoid.Sink == null)
                return new Sink(trapezoid);
            return trapezoid.Sink;
        }

        public override Sink Locate(Edge edge)
        {
            return this;
        }
    }

    // See "Computational Geometry", 3rd edition, by Mark de Berg et al, Chapter 6.2
    internal class TrapezoidalMap
    {
        // Trapezoid container
        public HashSet<Trapezoid> Map;

        // AABB margin

        // Bottom segment that spans multiple trapezoids
        private Edge _bCross;

        // Top segment that spans multiple trapezoids
        private Edge _cross;
        private float _margin;

        public TrapezoidalMap()
        {
            Map = new HashSet<Trapezoid>();
            _margin = 50.0f;
            _bCross = null;
            _cross = null;
        }

        public void Clear()
        {
            _bCross = null;
            _cross = null;
        }

        // Case 1: segment completely enclosed by trapezoid
        //         break trapezoid into 4 smaller trapezoids
        public Trapezoid[] Case1(Trapezoid t, Edge e)
        {
            Trapezoid[] trapezoids = new Trapezoid[4];
            trapezoids[0] = new Trapezoid(t.LeftPoint, e.P, t.Top, t.Bottom);
            trapezoids[1] = new Trapezoid(e.P, e.Q, t.Top, e);
            trapezoids[2] = new Trapezoid(e.P, e.Q, e, t.Bottom);
            trapezoids[3] = new Trapezoid(e.Q, t.RightPoint, t.Top, t.Bottom);

            trapezoids[0].UpdateLeft(t.UpperLeft, t.LowerLeft);
            trapezoids[1].UpdateLeftRight(trapezoids[0], null, trapezoids[3], null);
            trapezoids[2].UpdateLeftRight(null, trapezoids[0], null, trapezoids[3]);
            trapezoids[3].UpdateRight(t.UpperRight, t.LowerRight);

            return trapezoids;
        }

        // Case 2: Trapezoid contains point p, q lies outside
        //         break trapezoid into 3 smaller trapezoids
        public Trapezoid[] Case2(Trapezoid t, Edge e)
        {
            Point rp;
            if (e.Q.X == t.RightPoint.X)
                rp = e.Q;
            else
                rp = t.RightPoint;

            Trapezoid[] trapezoids = new Trapezoid[3];
            trapezoids[0] = new Trapezoid(t.LeftPoint, e.P, t.Top, t.Bottom);
            trapezoids[1] = new Trapezoid(e.P, rp, t.Top, e);
            trapezoids[2] = new Trapezoid(e.P, rp, e, t.Bottom);

            trapezoids[0].UpdateLeft(t.UpperLeft, t.LowerLeft);
            trapezoids[1].UpdateLeftRight(trapezoids[0], null, t.UpperRight, null);
            trapezoids[2].UpdateLeftRight(null, trapezoids[0], null, t.LowerRight);

            _bCross = t.Bottom;
            _cross = t.Top;

            e.Above = trapezoids[1];
            e.Below = trapezoids[2];

            return trapezoids;
        }

        // Case 3: Trapezoid is bisected
        public Trapezoid[] Case3(Trapezoid t, Edge e)
        {
            Point lp;
            if (e.P.X == t.LeftPoint.X)
                lp = e.P;
            else
                lp = t.LeftPoint;

            Point rp;
            if (e.Q.X == t.RightPoint.X)
                rp = e.Q;
            else
                rp = t.RightPoint;

            Trapezoid[] trapezoids = new Trapezoid[2];

            if (_cross == t.Top)
            {
                trapezoids[0] = t.UpperLeft;
                trapezoids[0].UpdateRight(t.UpperRight, null);
                trapezoids[0].RightPoint = rp;
            }
            else
            {
                trapezoids[0] = new Trapezoid(lp, rp, t.Top, e);
                trapezoids[0].UpdateLeftRight(t.UpperLeft, e.Above, t.UpperRight, null);
            }

            if (_bCross == t.Bottom)
            {
                trapezoids[1] = t.LowerLeft;
                trapezoids[1].UpdateRight(null, t.LowerRight);
                trapezoids[1].RightPoint = rp;
            }
            else
            {
                trapezoids[1] = new Trapezoid(lp, rp, e, t.Bottom);
                trapezoids[1].UpdateLeftRight(e.Below, t.LowerLeft, null, t.LowerRight);
            }

            _bCross = t.Bottom;
            _cross = t.Top;

            e.Above = trapezoids[0];
            e.Below = trapezoids[1];

            return trapezoids;
        }

        // Case 4: Trapezoid contains point q, p lies outside
        //         break trapezoid into 3 smaller trapezoids
        public Trapezoid[] Case4(Trapezoid t, Edge e)
        {
            Point lp;
            if (e.P.X == t.LeftPoint.X)
                lp = e.P;
            else
                lp = t.LeftPoint;

            Trapezoid[] trapezoids = new Trapezoid[3];

            if (_cross == t.Top)
            {
                trapezoids[0] = t.UpperLeft;
                trapezoids[0].RightPoint = e.Q;
            }
            else
            {
                trapezoids[0] = new Trapezoid(lp, e.Q, t.Top, e);
                trapezoids[0].UpdateLeft(t.UpperLeft, e.Above);
            }

            if (_bCross == t.Bottom)
            {
                trapezoids[1] = t.LowerLeft;
                trapezoids[1].RightPoint = e.Q;
            }
            else
            {
                trapezoids[1] = new Trapezoid(lp, e.Q, e, t.Bottom);
                trapezoids[1].UpdateLeft(e.Below, t.LowerLeft);
            }

            trapezoids[2] = new Trapezoid(e.Q, t.RightPoint, t.Top, t.Bottom);
            trapezoids[2].UpdateLeftRight(trapezoids[0], trapezoids[1], t.UpperRight, t.LowerRight);

            return trapezoids;
        }

        // Create an AABB around segments
        public Trapezoid BoundingBox(List<Edge> edges)
        {
            Point max = edges[0].P + _margin;
            Point min = edges[0].Q - _margin;

            foreach (Edge e in edges)
            {
                if (e.P.X > max.X) max = new Point(e.P.X + _margin, max.Y);
                if (e.P.Y > max.Y) max = new Point(max.X, e.P.Y + _margin);
                if (e.Q.X > max.X) max = new Point(e.Q.X + _margin, max.Y);
                if (e.Q.Y > max.Y) max = new Point(max.X, e.Q.Y + _margin);
                if (e.P.X < min.X) min = new Point(e.P.X - _margin, min.Y);
                if (e.P.Y < min.Y) min = new Point(min.X, e.P.Y - _margin);
                if (e.Q.X < min.X) min = new Point(e.Q.X - _margin, min.Y);
                if (e.Q.Y < min.Y) min = new Point(min.X, e.Q.Y - _margin);
            }

            Edge top = new Edge(new Point(min.X, max.Y), new Point(max.X, max.Y));
            Edge bottom = new Edge(new Point(min.X, min.Y), new Point(max.X, min.Y));
            Point left = bottom.P;
            Point right = top.Q;

            return new Trapezoid(left, right, top, bottom);
        }
    }

    internal class Point
    {
        // Pointers to next and previous points in Monontone Mountain
        public Point Next, Prev;
        public float X, Y;

        public Point(float x, float y)
        {
            X = x;
            Y = y;
            Next = null;
            Prev = null;
        }

        public static Point operator -(Point p1, Point p2)
        {
            return new Point(p1.X - p2.X, p1.Y - p2.Y);
        }

        public static Point operator +(Point p1, Point p2)
        {
            return new Point(p1.X + p2.X, p1.Y + p2.Y);
        }

        public static Point operator -(Point p1, float f)
        {
            return new Point(p1.X - f, p1.Y - f);
        }

        public static Point operator +(Point p1, float f)
        {
            return new Point(p1.X + f, p1.Y + f);
        }

        public float Cross(Point p)
        {
            return X * p.Y - Y * p.X;
        }

        public float Dot(Point p)
        {
            return X * p.X + Y * p.Y;
        }

        public bool Neq(Point p)
        {
            return p.X != X || p.Y != Y;
        }

        public float Orient2D(Point pb, Point pc)
        {
            float acx = X - pc.X;
            float bcx = pb.X - pc.X;
            float acy = Y - pc.Y;
            float bcy = pb.Y - pc.Y;
            return acx * bcy - acy * bcx;
        }
    }

    internal class Edge
    {
        // Pointers used for building trapezoidal map
        public Trapezoid Above;
        public float B;
        public Trapezoid Below;

        // Equation of a line: y = m*x + b
        // Slope of the line (m)

        // Montone mountain points
        public HashSet<Point> MPoints;
        public Point P;
        public Point Q;
        public float Slope;

        // Y intercept

        public Edge(Point p, Point q)
        {
            P = p;
            Q = q;

            if (q.X - p.X != 0)
                Slope = (q.Y - p.Y) / (q.X - p.X);
            else
                Slope = 0;

            B = p.Y - (p.X * Slope);
            Above = null;
            Below = null;
            MPoints = new HashSet<Point>();
            MPoints.Add(p);
            MPoints.Add(q);
        }

        public bool IsAbove(Point point)
        {
            return P.Orient2D(Q, point) < 0;
        }

        public bool IsBelow(Point point)
        {
            return P.Orient2D(Q, point) > 0;
        }

        public void AddMpoint(Point point)
        {
            foreach (Point mp in MPoints)
                if (!mp.Neq(point))
                    return;

            MPoints.Add(point);
        }
    }

    internal class Trapezoid
    {
        public Edge Bottom;
        public bool Inside;
        public Point LeftPoint;

        // Neighbor pointers
        public Trapezoid LowerLeft;
        public Trapezoid LowerRight;

        public Point RightPoint;
        public Sink Sink;

        public Edge Top;
        public Trapezoid UpperLeft;
        public Trapezoid UpperRight;

        public Trapezoid(Point leftPoint, Point rightPoint, Edge top, Edge bottom)
        {
            LeftPoint = leftPoint;
            RightPoint = rightPoint;
            Top = top;
            Bottom = bottom;
            UpperLeft = null;
            UpperRight = null;
            LowerLeft = null;
            LowerRight = null;
            Inside = true;
            Sink = null;
        }

        // Update neighbors to the left
        public void UpdateLeft(Trapezoid ul, Trapezoid ll)
        {
            UpperLeft = ul;
            if (ul != null) ul.UpperRight = this;
            LowerLeft = ll;
            if (ll != null) ll.LowerRight = this;
        }

        // Update neighbors to the right
        public void UpdateRight(Trapezoid ur, Trapezoid lr)
        {
            UpperRight = ur;
            if (ur != null) ur.UpperLeft = this;
            LowerRight = lr;
            if (lr != null) lr.LowerLeft = this;
        }

        // Update neighbors on both sides
        public void UpdateLeftRight(Trapezoid ul, Trapezoid ll, Trapezoid ur, Trapezoid lr)
        {
            UpperLeft = ul;
            if (ul != null) ul.UpperRight = this;
            LowerLeft = ll;
            if (ll != null) ll.LowerRight = this;
            UpperRight = ur;
            if (ur != null) ur.UpperLeft = this;
            LowerRight = lr;
            if (lr != null) lr.LowerLeft = this;
        }

        // Recursively trim outside neighbors
        public void TrimNeighbors()
        {
            if (Inside)
            {
                Inside = false;
                if (UpperLeft != null) UpperLeft.TrimNeighbors();
                if (LowerLeft != null) LowerLeft.TrimNeighbors();
                if (UpperRight != null) UpperRight.TrimNeighbors();
                if (LowerRight != null) LowerRight.TrimNeighbors();
            }
        }

        // Determines if this point lies inside the trapezoid
        public bool Contains(Point point)
        {
            return (point.X > LeftPoint.X && point.X < RightPoint.X && Top.IsAbove(point) && Bottom.IsBelow(point));
        }

        public List<Point> Vertices()
        {
            List<Point> verts = new List<Point>(4);
            verts.Add(LineIntersect(Top, LeftPoint.X));
            verts.Add(LineIntersect(Bottom, LeftPoint.X));
            verts.Add(LineIntersect(Bottom, RightPoint.X));
            verts.Add(LineIntersect(Top, RightPoint.X));
            return verts;
        }

        private Point LineIntersect(Edge edge, float x)
        {
            float y = edge.Slope * x + edge.B;
            return new Point(x, y);
        }

        // Add points to monotone mountain
        public void AddPoints()
        {
            if (LeftPoint != Bottom.P)
            {
                Bottom.AddMpoint(LeftPoint);
            }
            if (RightPoint != Bottom.Q)
            {
                Bottom.AddMpoint(RightPoint);
            }
            if (LeftPoint != Top.P)
            {
                Top.AddMpoint(LeftPoint);
            }
            if (RightPoint != Top.Q)
            {
                Top.AddMpoint(RightPoint);
            }
        }
    }

    internal class XNode : Node
    {
        private Point _point;

        public XNode(Point point, Node lChild, Node rChild)
            : base(lChild, rChild)
        {
            _point = point;
        }

        public override Sink Locate(Edge edge)
        {
            if (edge.P.X >= _point.X)
                // Move to the right in the graph
                return RightChild.Locate(edge);
            // Move to the left in the graph
            return LeftChild.Locate(edge);
        }
    }

    internal class YNode : Node
    {
        private Edge _edge;

        public YNode(Edge edge, Node lChild, Node rChild)
            : base(lChild, rChild)
        {
            _edge = edge;
        }

        public override Sink Locate(Edge edge)
        {
            if (_edge.IsAbove(edge.P))
                // Move down the graph
                return RightChild.Locate(edge);

            if (_edge.IsBelow(edge.P))
                // Move up the graph
                return LeftChild.Locate(edge);

            // s and segment share the same endpoint, p
            if (edge.Slope < _edge.Slope)
                // Move down the graph
                return RightChild.Locate(edge);

            // Move up the graph
            return LeftChild.Locate(edge);
        }
    }

    internal class Triangulator
    {
        // Trapezoid decomposition list
        public List<Trapezoid> Trapezoids;
        public List<List<Point>> Triangles;

        // Initialize trapezoidal map and query structure
        private Trapezoid _boundingBox;
        private List<Edge> _edgeList;
        private QueryGraph _queryGraph;
        private float _sheer = 0.001f;
        private TrapezoidalMap _trapezoidalMap;
        private List<MonotoneMountain> _xMonoPoly;

        public Triangulator(List<Point> polyLine, float sheer)
        {
            _sheer = sheer;
            Triangles = new List<List<Point>>();
            Trapezoids = new List<Trapezoid>();
            _xMonoPoly = new List<MonotoneMountain>();
            _edgeList = InitEdges(polyLine);
            _trapezoidalMap = new TrapezoidalMap();
            _boundingBox = _trapezoidalMap.BoundingBox(_edgeList);
            _queryGraph = new QueryGraph(Sink.Isink(_boundingBox));

            Process();
        }

        // Build the trapezoidal map and query graph
        private void Process()
        {
            foreach (Edge edge in _edgeList)
            {
                List<Trapezoid> traps = _queryGraph.FollowEdge(edge);

                // Remove trapezoids from trapezoidal Map
                foreach (Trapezoid t in traps)
                {
                    _trapezoidalMap.Map.Remove(t);

                    bool cp = t.Contains(edge.P);
                    bool cq = t.Contains(edge.Q);
                    Trapezoid[] tList;

                    if (cp && cq)
                    {
                        tList = _trapezoidalMap.Case1(t, edge);
                        _queryGraph.Case1(t.Sink, edge, tList);
                    }
                    else if (cp && !cq)
                    {
                        tList = _trapezoidalMap.Case2(t, edge);
                        _queryGraph.Case2(t.Sink, edge, tList);
                    }
                    else if (!cp && !cq)
                    {
                        tList = _trapezoidalMap.Case3(t, edge);
                        _queryGraph.Case3(t.Sink, edge, tList);
                    }
                    else
                    {
                        tList = _trapezoidalMap.Case4(t, edge);
                        _queryGraph.Case4(t.Sink, edge, tList);
                    }
                    // Add new trapezoids to map
                    foreach (Trapezoid y in tList)
                    {
                        _trapezoidalMap.Map.Add(y);
                    }
                }
                _trapezoidalMap.Clear();
            }

            // Mark outside trapezoids
            foreach (Trapezoid t in _trapezoidalMap.Map)
            {
                MarkOutside(t);
            }

            // Collect interior trapezoids
            foreach (Trapezoid t in _trapezoidalMap.Map)
            {
                if (t.Inside)
                {
                    Trapezoids.Add(t);
                    t.AddPoints();
                }
            }

            // Generate the triangles
            CreateMountains();
        }

        // Build a list of x-monotone mountains
        private void CreateMountains()
        {
            foreach (Edge edge in _edgeList)
            {
                if (edge.MPoints.Count > 2)
                {
                    MonotoneMountain mountain = new MonotoneMountain();

                    // Sorting is a perfromance hit. Literature says this can be accomplised in
                    // linear time, although I don't see a way around using traditional methods
                    // when using a randomized incremental algorithm

                    // Insertion sort is one of the fastest algorithms for sorting arrays containing 
                    // fewer than ten elements, or for lists that are already mostly sorted.

                    List<Point> points = new List<Point>(edge.MPoints);
                    points.Sort((p1, p2) => p1.X.CompareTo(p2.X));

                    foreach (Point p in points)
                        mountain.Add(p);

                    // Triangulate monotone mountain
                    mountain.Process();

                    // Extract the triangles into a single list
                    foreach (List<Point> t in mountain.Triangles)
                    {
                        Triangles.Add(t);
                    }

                    _xMonoPoly.Add(mountain);
                }
            }
        }

        // Mark the outside trapezoids surrounding the polygon
        private void MarkOutside(Trapezoid t)
        {
            if (t.Top == _boundingBox.Top || t.Bottom == _boundingBox.Bottom)
                t.TrimNeighbors();
        }

        // Create segments and connect end points; update edge event pointer
        private List<Edge> InitEdges(List<Point> points)
        {
            List<Edge> edges = new List<Edge>();

            for (int i = 0; i < points.Count - 1; i++)
            {
                edges.Add(new Edge(points[i], points[i + 1]));
            }
            edges.Add(new Edge(points[0], points[points.Count - 1]));
            return OrderSegments(edges);
        }

        private List<Edge> OrderSegments(List<Edge> edgeInput)
        {
            // Ignore vertical segments!
            List<Edge> edges = new List<Edge>();

            foreach (Edge e in edgeInput)
            {
                Point p = ShearTransform(e.P);
                Point q = ShearTransform(e.Q);

                // Point p must be to the left of point q
                if (p.X > q.X)
                {
                    edges.Add(new Edge(q, p));
                }
                else if (p.X < q.X)
                {
                    edges.Add(new Edge(p, q));
                }
            }

            // Randomized triangulation improves performance
            // See Seidel's paper, or O'Rourke's book, p. 57 
            Shuffle(edges);
            return edges;
        }

        private static void Shuffle<T>(IList<T> list)
        {
            System.Random rng = new System.Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        // Prevents any two distinct endpoints from lying on a common vertical line, and avoiding
        // the degenerate case. See Mark de Berg et al, Chapter 6.3
        private Point ShearTransform(Point point)
        {
            return new Point(point.X + _sheer * point.Y, point.Y);
        }
    }
}
#endif