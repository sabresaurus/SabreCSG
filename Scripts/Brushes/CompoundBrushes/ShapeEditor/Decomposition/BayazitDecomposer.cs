#if UNITY_EDITOR || RUNTIME_CSG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Sabresaurus.SabreCSG.ShapeEditor.Decomposition
{
    /// <summary>
    /// Convex decomposition algorithm created by Mark Bayazit (http://mnbayazit.com/)
    /// For more information about this algorithm, see http://mnbayazit.com/406/bayazit
    /// </summary>
    public static class BayazitDecomposer
    {
        public static float SettingsEpsilon = 0.0001f;
        public static int MaxPolygonVertices = 1024;

        private static Vector2 At(int i, Vector2[] vertices)
        {
            int s = vertices.Length;
            return vertices[i < 0 ? s - (-i % s) : i % s];
        }

        private static Vector2[] Copy(int i, int j, Vector2[] vertices)
        {
            List<Vector2> p = new List<Vector2>();
            while (j < i) j += vertices.Length;
            //p.reserve(j - i + 1);
            for (; i <= j; ++i)
            {
                p.Add(At(i, vertices));
            }
            return p.ToArray();
        }

        /// <summary>
        /// Decompose the polygon into several smaller non-concave polygon.
        /// If the polygon is already convex, it will return the original polygon, unless it is over Settings.MaxPolygonVertices.
        /// Precondition: Counter Clockwise polygon
        /// </summary>
        /// <param name="vertices"></param>
        /// <returns></returns>
        public static List<Vector2[]> ConvexPartition(Vector2[] vertices)
        {
            //We force it to CCW as it is a precondition in this algorithm.
            //vertices.ForceCounterClockWise();

            List<Vector2[]> list = new List<Vector2[]>();
            float d, lowerDist, upperDist;
            Vector2 p;
            Vector2 lowerInt = new Vector2();
            Vector2 upperInt = new Vector2(); // intersection points
            int lowerIndex = 0, upperIndex = 0;
            List<Vector2> lowerPoly, upperPoly;

            for (int i = 0; i < vertices.Length; ++i)
            {
                if (Reflex(i, vertices))
                {
                    lowerDist = upperDist = float.MaxValue; // std::numeric_limits<qreal>::max();
                    for (int j = 0; j < vertices.Length; ++j)
                    {
                        // if line intersects with an edge
                        if (Left(At(i - 1, vertices), At(i, vertices), At(j, vertices)) &&
                            RightOn(At(i - 1, vertices), At(i, vertices), At(j - 1, vertices)))
                        {
                            // find the point of intersection
                            p = LineTools.LineIntersect(At(i - 1, vertices), At(i, vertices), At(j, vertices),
                                                        At(j - 1, vertices));
                            if (Right(At(i + 1, vertices), At(i, vertices), p))
                            {
                                // make sure it's inside the poly
                                d = SquareDist(At(i, vertices), p);
                                if (d < lowerDist)
                                {
                                    // keep only the closest intersection
                                    lowerDist = d;
                                    lowerInt = p;
                                    lowerIndex = j;
                                }
                            }
                        }

                        if (Left(At(i + 1, vertices), At(i, vertices), At(j + 1, vertices)) &&
                            RightOn(At(i + 1, vertices), At(i, vertices), At(j, vertices)))
                        {
                            p = LineTools.LineIntersect(At(i + 1, vertices), At(i, vertices), At(j, vertices),
                                                        At(j + 1, vertices));
                            if (Left(At(i - 1, vertices), At(i, vertices), p))
                            {
                                d = SquareDist(At(i, vertices), p);
                                if (d < upperDist)
                                {
                                    upperDist = d;
                                    upperIndex = j;
                                    upperInt = p;
                                }
                            }
                        }
                    }

                    // if there are no vertices to connect to, choose a point in the middle
                    if (lowerIndex == (upperIndex + 1) % vertices.Length)
                    {
                        Vector2 sp = ((lowerInt + upperInt) / 2);

                        lowerPoly = Copy(i, upperIndex, vertices).ToList();
                        lowerPoly.Add(sp);
                        upperPoly = Copy(lowerIndex, i, vertices).ToList();
                        upperPoly.Add(sp);
                    }
                    else
                    {
                        double highestScore = 0, bestIndex = lowerIndex;
                        while (upperIndex < lowerIndex) upperIndex += vertices.Length;
                        for (int j = lowerIndex; j <= upperIndex; ++j)
                        {
                            if (CanSee(i, j, vertices))
                            {
                                double score = 1 / (SquareDist(At(i, vertices), At(j, vertices)) + 1);
                                if (Reflex(j, vertices))
                                {
                                    if (RightOn(At(j - 1, vertices), At(j, vertices), At(i, vertices)) &&
                                        LeftOn(At(j + 1, vertices), At(j, vertices), At(i, vertices)))
                                    {
                                        score += 3;
                                    }
                                    else
                                    {
                                        score += 2;
                                    }
                                }
                                else
                                {
                                    score += 1;
                                }
                                if (score > highestScore)
                                {
                                    bestIndex = j;
                                    highestScore = score;
                                }
                            }
                        }
                        lowerPoly = Copy(i, (int)bestIndex, vertices).ToList();
                        upperPoly = Copy((int)bestIndex, i, vertices).ToList();
                    }
                    list.AddRange(ConvexPartition(lowerPoly.ToArray()));
                    list.AddRange(ConvexPartition(upperPoly.ToArray()));
                    return list;
                }
            }

            // polygon is already convex
            if (vertices.Length > MaxPolygonVertices)
            {
                lowerPoly = Copy(0, vertices.Length / 2, vertices).ToList();
                upperPoly = Copy(vertices.Length / 2, 0, vertices).ToList();
                list.AddRange(ConvexPartition(lowerPoly.ToArray()));
                list.AddRange(ConvexPartition(upperPoly.ToArray()));
            }
            else
                list.Add(vertices);

            //The polygons are not guaranteed to be without collinear points. We remove
            //them to be sure.
            for (int i = 0; i < list.Count; i++)
            {
                list[i] = SimplifyTools.CollinearSimplify(list[i], 0);
            }

            //Remove empty vertice collections
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].Length == 0)
                    list.RemoveAt(i);
            }

            return list;
        }

        private static bool CanSee(int i, int j, Vector2[] vertices)
        {
            if (Reflex(i, vertices))
            {
                if (LeftOn(At(i, vertices), At(i - 1, vertices), At(j, vertices)) &&
                    RightOn(At(i, vertices), At(i + 1, vertices), At(j, vertices))) return false;
            }
            else
            {
                if (RightOn(At(i, vertices), At(i + 1, vertices), At(j, vertices)) ||
                    LeftOn(At(i, vertices), At(i - 1, vertices), At(j, vertices))) return false;
            }
            if (Reflex(j, vertices))
            {
                if (LeftOn(At(j, vertices), At(j - 1, vertices), At(i, vertices)) &&
                    RightOn(At(j, vertices), At(j + 1, vertices), At(i, vertices))) return false;
            }
            else
            {
                if (RightOn(At(j, vertices), At(j + 1, vertices), At(i, vertices)) ||
                    LeftOn(At(j, vertices), At(j - 1, vertices), At(i, vertices))) return false;
            }
            for (int k = 0; k < vertices.Length; ++k)
            {
                if ((k + 1) % vertices.Length == i || k == i || (k + 1) % vertices.Length == j || k == j)
                {
                    continue; // ignore incident edges
                }
                Vector2 intersectionPoint;
                if (LineTools.LineIntersect(At(i, vertices), At(j, vertices), At(k, vertices), At(k + 1, vertices), out intersectionPoint))
                {
                    return false;
                }
            }
            return true;
        }

        // precondition: ccw
        private static bool Reflex(int i, Vector2[] vertices)
        {
            return Right(i, vertices);
        }

        private static bool Right(int i, Vector2[] vertices)
        {
            return Right(At(i - 1, vertices), At(i, vertices), At(i + 1, vertices));
        }

        private static bool Left(Vector2 a, Vector2 b, Vector2 c)
        {
            return MathUtils.Area(ref a, ref b, ref c) > 0;
        }

        private static bool LeftOn(Vector2 a, Vector2 b, Vector2 c)
        {
            return MathUtils.Area(ref a, ref b, ref c) >= 0;
        }

        private static bool Right(Vector2 a, Vector2 b, Vector2 c)
        {
            return MathUtils.Area(ref a, ref b, ref c) < 0;
        }

        private static bool RightOn(Vector2 a, Vector2 b, Vector2 c)
        {
            return MathUtils.Area(ref a, ref b, ref c) <= 0;
        }

        private static float SquareDist(Vector2 a, Vector2 b)
        {
            float dx = b.x - a.x;
            float dy = b.y - a.y;
            return dx * dx + dy * dy;
        }

        private static class MathUtils
        {
            /// <summary>
            /// Returns a positive number if c is to the left of the line going from a to b.
            /// </summary>
            /// <returns>Positive number if point is left, negative if point is right, 
            /// and 0 if points are collinear.</returns>
            public static float Area(Vector2 a, Vector2 b, Vector2 c)
            {
                return Area(ref a, ref b, ref c);
            }

            /// <summary>
            /// Returns a positive number if c is to the left of the line going from a to b.
            /// </summary>
            /// <returns>Positive number if point is left, negative if point is right, 
            /// and 0 if points are collinear.</returns>
            public static float Area(ref Vector2 a, ref Vector2 b, ref Vector2 c)
            {
                return a.x * (b.y - c.y) + b.x * (c.y - a.y) + c.x * (a.y - b.y);
            }

            public static bool FloatEquals(float value1, float value2)
            {
                return Math.Abs(value1 - value2) <= SettingsEpsilon;
            }

            /// <summary>
            /// Determines if three vertices are collinear (ie. on a straight line)
            /// </summary>
            /// <param name="a">First vertex</param>
            /// <param name="b">Second vertex</param>
            /// <param name="c">Third vertex</param>
            /// <returns></returns>
            public static bool Collinear(ref Vector2 a, ref Vector2 b, ref Vector2 c)
            {
                return Collinear(ref a, ref b, ref c, 0);
            }

            public static bool Collinear(ref Vector2 a, ref Vector2 b, ref Vector2 c, float tolerance)
            {
                return FloatInRange(Area(ref a, ref b, ref c), -tolerance, tolerance);
            }

            /// <summary>
            /// Checks if a floating point Value is within a specified
            /// range of values (inclusive).
            /// </summary>
            /// <param name="value">The Value to check.</param>
            /// <param name="min">The minimum Value.</param>
            /// <param name="max">The maximum Value.</param>
            /// <returns>True if the Value is within the range specified,
            /// false otherwise.</returns>
            public static bool FloatInRange(float value, float min, float max)
            {
                return (value >= min && value <= max);
            }

            /// <summary>
            /// Gets the next index. Used for iterating all the edges with wrap-around.
            /// </summary>
            /// <param name="index">The current index</param>
            public static int NextIndex(Vector2[] vertices, int index)
            {
                return (index + 1 > vertices.Length - 1) ? 0 : index + 1;
            }

            /// <summary>
            /// Gets the previous index. Used for iterating all the edges with wrap-around.
            /// </summary>
            /// <param name="index">The current index</param>
            public static int PreviousIndex(Vector2[] vertices, int index)
            {
                return index - 1 < 0 ? vertices.Length - 1 : index - 1;
            }
        }

        private static class LineTools
        {
            //From Mark Bayazit's convex decomposition algorithm
            public static Vector2 LineIntersect(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2)
            {
                Vector2 i = Vector2.zero;
                float a1 = p2.y - p1.y;
                float b1 = p1.x - p2.x;
                float c1 = a1 * p1.x + b1 * p1.y;
                float a2 = q2.y - q1.y;
                float b2 = q1.x - q2.x;
                float c2 = a2 * q1.x + b2 * q1.y;
                float det = a1 * b2 - a2 * b1;

                if (!MathUtils.FloatEquals(det, 0))
                {
                    // lines are not parallel
                    i.x = (b2 * c1 - b1 * c2) / det;
                    i.y = (a1 * c2 - a2 * c1) / det;
                }
                return i;
            }

            /// <summary>
            /// This method detects if two line segments intersect,
            /// and, if so, the point of intersection. 
            /// Note: If two line segments are coincident, then 
            /// no intersection is detected (there are actually
            /// infinite intersection points).
            /// </summary>
            /// <param name="point1">The first point of the first line segment.</param>
            /// <param name="point2">The second point of the first line segment.</param>
            /// <param name="point3">The first point of the second line segment.</param>
            /// <param name="point4">The second point of the second line segment.</param>
            /// <param name="intersectionPoint">This is set to the intersection
            /// point if an intersection is detected.</param>
            /// <returns>True if an intersection is detected, false otherwise.</returns>
            public static bool LineIntersect(Vector2 point1, Vector2 point2, Vector2 point3, Vector2 point4,
                                             out Vector2 intersectionPoint)
            {
                return LineIntersect(ref point1, ref point2, ref point3, ref point4, true, true, out intersectionPoint);
            }

            /// <summary>
            /// This method detects if two line segments (or lines) intersect,
            /// and, if so, the point of intersection. Use the <paramref name="firstIsSegment"/> and
            /// <paramref name="secondIsSegment"/> parameters to set whether the intersection point
            /// must be on the first and second line segments. Setting these
            /// both to true means you are doing a line-segment to line-segment
            /// intersection. Setting one of them to true means you are doing a
            /// line to line-segment intersection test, and so on.
            /// Note: If two line segments are coincident, then 
            /// no intersection is detected (there are actually
            /// infinite intersection points).
            /// Author: Jeremy Bell
            /// </summary>
            /// <param name="point1">The first point of the first line segment.</param>
            /// <param name="point2">The second point of the first line segment.</param>
            /// <param name="point3">The first point of the second line segment.</param>
            /// <param name="point4">The second point of the second line segment.</param>
            /// <param name="intersectionPoint">This is set to the intersection
            /// point if an intersection is detected.</param>
            /// <param name="firstIsSegment">Set this to true to require that the 
            /// intersection point be on the first line segment.</param>
            /// <param name="secondIsSegment">Set this to true to require that the
            /// intersection point be on the second line segment.</param>
            /// <returns>True if an intersection is detected, false otherwise.</returns>
            public static bool LineIntersect(Vector2 point1, Vector2 point2, Vector2 point3, Vector2 point4,
                                             bool firstIsSegment,
                                             bool secondIsSegment, out Vector2 intersectionPoint)
            {
                return LineIntersect(ref point1, ref point2, ref point3, ref point4, firstIsSegment, secondIsSegment,
                                     out intersectionPoint);
            }

            /// <summary>
            /// This method detects if two line segments (or lines) intersect,
            /// and, if so, the point of intersection. Use the <paramref name="firstIsSegment"/> and
            /// <paramref name="secondIsSegment"/> parameters to set whether the intersection point
            /// must be on the first and second line segments. Setting these
            /// both to true means you are doing a line-segment to line-segment
            /// intersection. Setting one of them to true means you are doing a
            /// line to line-segment intersection test, and so on.
            /// Note: If two line segments are coincident, then 
            /// no intersection is detected (there are actually
            /// infinite intersection points).
            /// Author: Jeremy Bell
            /// </summary>
            /// <param name="point1">The first point of the first line segment.</param>
            /// <param name="point2">The second point of the first line segment.</param>
            /// <param name="point3">The first point of the second line segment.</param>
            /// <param name="point4">The second point of the second line segment.</param>
            /// <param name="point">This is set to the intersection
            /// point if an intersection is detected.</param>
            /// <param name="firstIsSegment">Set this to true to require that the 
            /// intersection point be on the first line segment.</param>
            /// <param name="secondIsSegment">Set this to true to require that the
            /// intersection point be on the second line segment.</param>
            /// <returns>True if an intersection is detected, false otherwise.</returns>
            public static bool LineIntersect(ref Vector2 point1, ref Vector2 point2, ref Vector2 point3, ref Vector2 point4,
                                             bool firstIsSegment, bool secondIsSegment,
                                             out Vector2 point)
            {
                point = new Vector2();

                // these are reused later.
                // each lettered sub-calculation is used twice, except
                // for b and d, which are used 3 times
                float a = point4.y - point3.y;
                float b = point2.x - point1.x;
                float c = point4.x - point3.x;
                float d = point2.y - point1.y;

                // denominator to solution of linear system
                float denom = (a * b) - (c * d);

                // if denominator is 0, then lines are parallel
                if (!(denom >= -SettingsEpsilon && denom <= SettingsEpsilon))
                {
                    float e = point1.y - point3.y;
                    float f = point1.x - point3.x;
                    float oneOverDenom = 1.0f / denom;

                    // numerator of first equation
                    float ua = (c * e) - (a * f);
                    ua *= oneOverDenom;

                    // check if intersection point of the two lines is on line segment 1
                    if (!firstIsSegment || ua >= 0.0f && ua <= 1.0f)
                    {
                        // numerator of second equation
                        float ub = (b * e) - (d * f);
                        ub *= oneOverDenom;

                        // check if intersection point of the two lines is on line segment 2
                        // means the line segments intersect, since we know it is on
                        // segment 1 as well.
                        if (!secondIsSegment || ub >= 0.0f && ub <= 1.0f)
                        {
                            // check if they are coincident (no collision in this case)
                            if (ua != 0f || ub != 0f)
                            {
                                //There is an intersection
                                point.x = point1.x + ua * b;
                                point.y = point1.y + ua * d;
                                return true;
                            }
                        }
                    }
                }

                return false;
            }
        }

        private static class SimplifyTools
        {
            /// <summary>
            /// Removes all collinear points on the polygon.
            /// </summary>
            /// <param name="vertices">The polygon that needs simplification.</param>
            /// <param name="collinearityTolerance">The collinearity tolerance.</param>
            /// <returns>A simplified polygon.</returns>
            public static Vector2[] CollinearSimplify(Vector2[] vertices, float collinearityTolerance)
            {
                //We can't simplify polygons under 3 vertices
                if (vertices.Length < 3)
                    return vertices;

                List<Vector2> simplified = new List<Vector2>();

                for (int i = 0; i < vertices.Length; i++)
                {
                    int prevId = MathUtils.PreviousIndex(vertices, i);
                    int nextId = MathUtils.NextIndex(vertices, i);

                    Vector2 prev = vertices[prevId];
                    Vector2 current = vertices[i];
                    Vector2 next = vertices[nextId];

                    //If they collinear, continue
                    if (MathUtils.Collinear(ref prev, ref current, ref next, collinearityTolerance))
                        continue;

                    simplified.Add(current);
                }

                return simplified.ToArray();
            }
        }
    }
}
#endif