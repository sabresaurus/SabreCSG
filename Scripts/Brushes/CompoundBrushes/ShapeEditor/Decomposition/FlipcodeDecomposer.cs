#if UNITY_EDITOR || RUNTIME_CSG

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Sabresaurus.SabreCSG.ShapeEditor.Decomposition
{
    // Original code can be found here: http://www.flipcode.com/archives/Efficient_Polygon_Triangulation.shtml

    /// <summary>
    /// Triangulates a polygon into triangles.
    /// Doesn't handle holes.
    /// </summary>
    public static class FlipcodeDecomposer
    {
        public static float SettingsEpsilon = 0.0001f;

        private static Vector2 _tmpA;
        private static Vector2 _tmpB;
        private static Vector2 _tmpC;

        /// <summary>
        /// Check if the point P is inside the triangle defined by
        /// the points A, B, C
        /// </summary>
        /// <param name="a">The A point.</param>
        /// <param name="b">The B point.</param>
        /// <param name="c">The C point.</param>
        /// <param name="p">The point to be tested.</param>
        /// <returns>True if the point is inside the triangle</returns>
        private static bool InsideTriangle(ref Vector2 a, ref Vector2 b, ref Vector2 c, ref Vector2 p)
        {
            //A cross bp
            float abp = (c.x - b.x) * (p.y - b.y) - (c.y - b.y) * (p.x - b.x);

            //A cross ap
            float aap = (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);

            //b cross cp
            float bcp = (a.x - c.x) * (p.y - c.y) - (a.y - c.y) * (p.x - c.x);

            return ((abp >= 0.0f) && (bcp >= 0.0f) && (aap >= 0.0f));
        }

        /// <summary>
        /// Cut a the contour and add a triangle into V to describe the
        /// location of the cut
        /// </summary>
        /// <param name="contour">The list of points defining the polygon</param>
        /// <param name="u">The index of the first point</param>
        /// <param name="v">The index of the second point</param>
        /// <param name="w">The index of the third point</param>
        /// <param name="n">The number of elements in the array.</param>
        /// <param name="V">The array to populate with indicies of triangles.</param>
        /// <returns>True if a triangle was found</returns>
        private static bool Snip(List<Vector2> contour, int u, int v, int w, int n,
                                 int[] V)
        {
            if (SettingsEpsilon > Area(ref _tmpA, ref _tmpB, ref _tmpC))
            {
                return false;
            }

            for (int p = 0; p < n; p++)
            {
                if ((p == u) || (p == v) || (p == w))
                {
                    continue;
                }

                Vector2 point = contour[V[p]];

                if (InsideTriangle(ref _tmpA, ref _tmpB, ref _tmpC, ref point))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns a positive number if c is to the left of the line going from a to b.
        /// </summary>
        /// <returns>Positive number if point is left, negative if point is right,
        /// and 0 if points are collinear.</returns>
        private static float Area(ref Vector2 a, ref Vector2 b, ref Vector2 c)
        {
            return a.x * (b.y - c.y) + b.x * (c.y - a.y) + c.x * (a.y - b.y);
        }

        /// <summary>
        /// Decompose the polygon into triangles
        /// </summary>
        /// <param name="contour">The list of points describing the polygon</param>
        /// <returns></returns>
        public static List<List<Vector2>> ConvexPartition(List<Vector2> contour, bool isCounterClockWise)
        {
            int n = contour.Count;
            if (n < 3)
                return new List<List<Vector2>>();

            int[] V = new int[n];

            // We want a counter-clockwise polygon in V
            if (isCounterClockWise)
            {
                for (int v = 0; v < n; v++)
                    V[v] = v;
            }
            else
            {
                for (int v = 0; v < n; v++)
                    V[v] = (n - 1) - v;
            }

            int nv = n;

            // Remove nv-2 Vertices, creating 1 triangle every time
            int count = 2 * nv; /* error detection */

            List<List<Vector2>> result = new List<List<Vector2>>();

            for (int v = nv - 1; nv > 2;)
            {
                // If we loop, it is probably a non-simple polygon
                if (0 >= (count--))
                {
                    // Triangulate: ERROR - probable bad polygon!
                    return new List<List<Vector2>>();
                }

                // Three consecutive vertices in current polygon, <u,v,w>
                int u = v;
                if (nv <= u)
                    u = 0; // Previous
                v = u + 1;
                if (nv <= v)
                    v = 0; // New v
                int w = v + 1;
                if (nv <= w)
                    w = 0; // Next

                _tmpA = contour[V[u]];
                _tmpB = contour[V[v]];
                _tmpC = contour[V[w]];

                if (Snip(contour, u, v, w, nv, V))
                {
                    int s, t;

                    // Output Triangle
                    List<Vector2> triangle = new List<Vector2>(3);
                    triangle.Add(_tmpA);
                    triangle.Add(_tmpB);
                    triangle.Add(_tmpC);
                    result.Add(triangle);

                    // Remove v from remaining polygon
                    for (s = v, t = v + 1; t < nv; s++, t++)
                    {
                        V[s] = V[t];
                    }
                    nv--;

                    // Reset error detection counter
                    count = 2 * nv;
                }
            }

            return result;
        }
    }
}

#endif