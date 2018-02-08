using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

#if UNITY_EDITOR || RUNTIME_CSG

namespace Sabresaurus.SabreCSG
{
    /// <summary>
    /// The unique SabreCSG T-Junction removal algorithm.
    /// <para>Created by Henry de Jongh.</para>
    /// </summary>
    public static class TJunctions
    {
        /// <summary>
        /// An edge of a polygon. See <see cref="SabreCSG.Edge"/>.
        /// </summary>
        private class TEdge
        {
            public Edge Edge;
            public Polygon Parent;
        }

        /// <summary>
        /// Represents a T-Junction. This is a vertex that lies upon but is not attached to an edge.
        /// </summary>
        private class TJunction
        {
            public Vertex Vertex;
            public TEdge DisconnectedEdge;
        }

        // todo: this is a useful function, maybe move this to an appropriate utility class.

        /// <summary>
        /// Determines whether a vertex is located on the specified edge.
        /// </summary>
        /// <param name="vertex">The vertex to check.</param>
        /// <param name="edge">The edge to chck.</param>
        /// <returns><c>true</c> if the vertex lies on the edge; otherwise, <c>false</c>.</returns>
        public static bool IsVertexOnEdge(Vertex vertex, Edge edge)
        {
            float x = vertex.Position.x;
            float y = vertex.Position.y;
            float z = vertex.Position.z;
            float x1 = edge.Vertex1.Position.x;
            float y1 = edge.Vertex1.Position.y;
            float z1 = edge.Vertex1.Position.z;
            float x2 = edge.Vertex2.Position.x;
            float y2 = edge.Vertex2.Position.y;
            float z2 = edge.Vertex2.Position.z;

            float AB = Mathf.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1) + (z2 - z1) * (z2 - z1));
            float AP = Mathf.Sqrt((x - x1) * (x - x1) + (y - y1) * (y - y1) + (z - z1) * (z - z1));
            float PB = Mathf.Sqrt((x2 - x) * (x2 - x) + (y2 - y) * (y2 - y) + (z2 - z) * (z2 - z));
            if (AB.EqualsWithEpsilon(AP + PB))
                return true;
            return false;
        }

        /// <summary>
        /// Fixes the T-Junctions (vertices that lie upon but are not attached to an edge).
        /// </summary>
        /// <param name="allGroupedPolygons">All grouped polygons from <see cref="CSGFactory"/>.</param>
        public static void FixTJunctions(Dictionary<int, List<Polygon>> allGroupedPolygons)
        {
            // todo: this algorithm was scuffed together after a toilet session. I bet you can optimize this.
            // if this comment is still here, it means you can do it. please! contribute! :)

            int totalTJunctions = 0;
            int giveup = 0;
            bool done = false;
            while (!done)
            {
                List<TEdge> edges = new List<TEdge>();
                List<Vertex> vertices = new List<Vertex>();

                foreach (var groupedPolygon in allGroupedPolygons)
                {
                    foreach (Polygon polygon in groupedPolygon.Value)
                    {
                        // build a collection of all edges and vertices.
                        foreach (Edge edge in polygon.GetEdges())
                        {
                            edges.Add(new TEdge { Edge = edge, Parent = polygon });
                            vertices.Add(edge.Vertex1);
                        }
                    }
                }

                // get list of unique vertices.
                List<Vertex> unique = new List<Vertex>();
                List<TJunction> tjunctions = new List<TJunction>();
                Polygon.VertexComparerEpsilon vertexComparerEpsilon = new Polygon.VertexComparerEpsilon();

                foreach (TEdge edge in edges)
                {
                    foreach (Vertex vertex in vertices)
                    {
                        // this vertex can not be part of the edge.
                        if (vertex.Position.EqualsWithEpsilon(edge.Edge.Vertex1.Position) || vertex.Position.EqualsWithEpsilon(edge.Edge.Vertex2.Position)) continue;

                        if (IsVertexOnEdge(vertex, edge.Edge))
                        {
                            if (!unique.Contains(vertex, vertexComparerEpsilon))
                            {
                                //Debug.Log("Yes Vertex on edge!");
                                unique.Add(vertex);
                                tjunctions.Add(new TJunction { Vertex = vertex, DisconnectedEdge = edge });
                                totalTJunctions++;
                            }
                        }
                    }
                }

                // I found the T-Junctions, fix em!
                foreach (TJunction tjunction in tjunctions)
                {
                    // split the edge that the vertex is on top of (but not connected to).
                    SplitPolygonAtEdge(tjunction.DisconnectedEdge.Parent, tjunction.DisconnectedEdge.Edge, tjunction.Vertex.Position);
                }

                done = tjunctions.Count == 0;

                giveup++;
                if (giveup > 256)
                {
                    Debug.LogError("(SabreCSG) FixTJunctions: Too many T-Junctions! :'(");
                    done = true;
                }
            }

            if (totalTJunctions != 0)
                Debug.Log("(SabreCSG) FixTJunctions: Removed " + totalTJunctions.ToString() + " T-Junctions after " + giveup.ToString() + " iteration(s).");
        }

        /// <summary>
        /// Adds a vertex to polygon at the center of the supplied edge, used by the Vertex tool's Split Edge button
        /// </summary>
        /// <returns><c>true</c>, if the edge was matched in the polygon and a vertex was added, <c>false</c> otherwise.</returns>
        /// <param name="polygon">Source polygon to add a vertex to.</param>
        /// <param name="edge">Edge to match and at a vertex to</param>
        public static bool SplitPolygonAtEdge(Polygon polygon, Edge edge, Vector3 futurePosition)
        {
            List<Vertex> vertices = new List<Vertex>(polygon.Vertices);
            for (int i = 0; i < polygon.Vertices.Length; i++)
            {
                Vector3 position1 = polygon.Vertices[i].Position;
                Vector3 position2 = polygon.Vertices[(i + 1) % polygon.Vertices.Length].Position;

                if ((edge.Vertex1.Position.EqualsWithEpsilon(position1) && edge.Vertex2.Position.EqualsWithEpsilon(position2))
                    || (edge.Vertex1.Position.EqualsWithEpsilon(position2) && edge.Vertex2.Position.EqualsWithEpsilon(position1)))
                {
                    Vertex begin = polygon.Vertices[i];
                    Vertex end = polygon.Vertices[(i + 1) % polygon.Vertices.Length];

                    // shoutouts to don stroganotti, excellent math.
                    float originalLength = (edge.Vertex1.Position - edge.Vertex2.Position).magnitude;
                    float newlength = (edge.Vertex1.Position - futurePosition).magnitude;
                    float interpolant = newlength / originalLength;

                    vertices.Insert(i + 1, Vertex.Lerp(begin, end, interpolant));
                    break;
                }
            }

            if (vertices.Count == polygon.Vertices.Length)
            {
                // Could not add vertex to adjacent polygon
                return false;
            }

            polygon.SetVertices(vertices.ToArray());

            return true;
        }
    }
}

#endif