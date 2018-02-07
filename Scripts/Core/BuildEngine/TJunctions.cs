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
        /// A vertex of a polygon. See <see cref="SabreCSG.Vertex"/>.
        /// </summary>
        private class TVertex
        {
            public Vertex Vertex;
            public Polygon Parent;
        }

        /// <summary>
        /// Represents a T-Junction. This is a vertex that lies upon but is not attached to an edge.
        /// </summary>
        private class TJunction
        {
            public TVertex Vertex;
            public TEdge DisconnectedEdge;
        }

        /// <summary>
        /// Compares whether a TVertex is identical (position only) to another TVertex.
        /// </summary>
        /// <seealso cref="System.Collections.Generic.IEqualityComparer{Sabresaurus.SabreCSG.TJunctions.TVertex}"/>
        private class TVertexComparerEpsilon : IEqualityComparer<TVertex>
        {
            public bool Equals(TVertex x, TVertex y)
            {
                return x.Vertex.Position.EqualsWithEpsilon(y.Vertex.Position);
            }

            public int GetHashCode(TVertex obj)
            {
                // The similarity or difference between two positions can only be calculated if both are supplied
                // when Distinct is called GetHashCode is used to determine which values are in collision first
                // therefore we return the same hash code for all values to ensure all comparisons must use 
                // our Equals method to properly determine which values are actually considered equal
                return 1;
            }
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

            int giveup = 0;
            bool done = false;
            while (!done)
            {
                List<TEdge> edges = new List<TEdge>();
                List<TVertex> vertices = new List<TVertex>();

                foreach (var groupedPolygon in allGroupedPolygons)
                {
                    int pid = 0;
                    foreach (Polygon polygon in groupedPolygon.Value)
                    {
                        // build a collection of all edges.
                        foreach (Edge edge in polygon.GetEdges())
                        {
                            edges.Add(new TEdge { Edge = edge, Parent = polygon });
                        }

                        // build a collection of all vertices.
                        foreach (Vertex vertex in polygon.Vertices)
                        {
                            vertices.Add(new TVertex { Vertex = vertex, Parent = polygon });
                        }

                        pid++;
                    }
                }

                // get list of unique vertices.
                List<TVertex> unique = new List<TVertex>();
                List<TJunction> tjunctions = new List<TJunction>();
                TVertexComparerEpsilon vertexComparerEpsilon = new TVertexComparerEpsilon();

                foreach (TEdge edge in edges)
                {
                    foreach (TVertex vertex in vertices)
                    {
                        // this vertex can not be part of the edge.
                        if (vertex.Vertex.Position.EqualsWithEpsilon(edge.Edge.Vertex1.Position) || vertex.Vertex.Position.EqualsWithEpsilon(edge.Edge.Vertex2.Position)) continue;
                        // this vertex can not be part of the same polygon as the edge.
                        //if (vertex.Parent == edge.Parent) continue;

                        if (IsVertexOnEdge(vertex.Vertex, edge.Edge))
                        {
                            if (!unique.Contains(vertex, vertexComparerEpsilon))
                            {
                                //Debug.Log("Yes Vertex on edge!");
                                unique.Add(vertex);
                                tjunctions.Add(new TJunction { Vertex = vertex, DisconnectedEdge = edge });
                            }
                        }
                    }
                }

                // I found the T-Junctions, fix em!
                foreach (TJunction tjunction in tjunctions)
                {
                    // split the edge that the vertex is on top of (but not connected to).
                    Vertex vertex;
                    SplitPolygonAtEdge(tjunction.DisconnectedEdge.Parent, tjunction.DisconnectedEdge.Edge, tjunction.Vertex.Vertex.Position, out vertex);
                }

                done = tjunctions.Count == 0;

                giveup++;
                if (giveup > 256)
                {
                    Debug.LogError("(SabreCSG) FixTJunctions: Too many T-Junctions! :'(");
                    done = true;
                }
            }
        }

        /// <summary>
        /// Adds a vertex to polygon at the center of the supplied edge, used by the Vertex tool's Split Edge button
        /// </summary>
        /// <returns><c>true</c>, if the edge was matched in the polygon and a vertex was added, <c>false</c> otherwise.</returns>
        /// <param name="polygon">Source polygon to add a vertex to.</param>
        /// <param name="edge">Edge to match and at a vertex to</param>
        /// <param name="newVertex">New vertex if one was created (first check the method returned <c>true</c>)</param>
        public static bool SplitPolygonAtEdge(Polygon polygon, Edge edge, Vector3 futurePosition, out Vertex newVertex)
        {
            newVertex = null;

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

                    newVertex = Vertex.Lerp(begin, end, interpolant);
                    vertices.Insert(i + 1, newVertex);
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