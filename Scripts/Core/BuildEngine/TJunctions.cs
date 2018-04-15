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
        /// Represents a T-Junction. This is a vertex that lies upon but is not attached to an edge.
        /// </summary>
        private class TJunction
        {
            public Vector3 Vertex;
            public Edge DisconnectedEdge;
            public Polygon Polygon;
        }

        // todo: this is a useful function, maybe move this to an appropriate utility class.

        /// <summary>
        /// Determines whether a vertex is located on the specified edge.
        /// </summary>
        /// <param name="vertex">The vertex to check.</param>
        /// <param name="edge">The edge to check.</param>
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
        /// Determines whether a vertex is located on the specified edge.
        /// </summary>
        /// <param name="vertex">The vertex to check.</param>
        /// <param name="edge">The edge to check.</param>
        /// <returns><c>true</c> if the vertex lies on the edge; otherwise, <c>false</c>.</returns>
        public static bool IsVertexOnEdge(Vector3 vertex, Edge edge)
        {
            float x = vertex.x;
            float y = vertex.y;
            float z = vertex.z;
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
            ///////////////////////////////////////////////////////////////
            ///////////////////////////////////////////////////////////////
            ///////////////////////////////////////////////////////////////
            ///////////////////////////////////////////////////////////////
            ///////////////////////////////////////////////////////////////
            ///////// UNUSED UNUSED UNUSED SEE MeshGroupManager ///////////
            ///////// UNUSED UNUSED UNUSED SEE MeshGroupManager ///////////
            ///////// UNUSED UNUSED UNUSED SEE MeshGroupManager ///////////
            ///////// UNUSED UNUSED UNUSED SEE MeshGroupManager ///////////
            ///////// UNUSED UNUSED UNUSED SEE MeshGroupManager ///////////
            ///////// UNUSED UNUSED UNUSED SEE MeshGroupManager ///////////
            ///////// UNUSED UNUSED UNUSED SEE MeshGroupManager ///////////
            ///////// UNUSED UNUSED UNUSED SEE MeshGroupManager ///////////
            ///////// UNUSED UNUSED UNUSED SEE MeshGroupManager ///////////
            ///////// UNUSED UNUSED UNUSED SEE MeshGroupManager ///////////
            ///////////////////////////////////////////////////////////////
            ///////////////////////////////////////////////////////////////
            ///////////////////////////////////////////////////////////////
            ///////////////////////////////////////////////////////////////

            List<Vector3> globalVertices = new List<Vector3>();

            // create a global list of all vertices.
            foreach (KeyValuePair<int, List<Polygon>> row in allGroupedPolygons)
                foreach (Polygon polygon in row.Value)
                    foreach (Vertex vertex in polygon.Vertices)
                        if (!globalVertices.Contains(vertex.Position))
                            globalVertices.Add(vertex.Position);

            int totalTJunctions = 0;
            int giveup = 0;
            bool done = false;
            while (!done)
            {
                List<TJunction> steiners = new List<TJunction>();

                foreach (KeyValuePair<int, List<Polygon>> row in allGroupedPolygons)
                    foreach (Polygon polygon in row.Value)
                    {
                        // find global vertices that are not part of this polygon.
                        IEnumerable<Vector3> otherWorldVertices = globalVertices.Where(p => !polygon.Vertices.Any(v => v.Position == p));
                        // find the remaining vertices that touch this polygon.
                        foreach (Edge edge in polygon.GetEdges())
                            foreach (Vector3 vertex in otherWorldVertices.Where(p => IsVertexOnEdge(p, edge)))
                                steiners.Add(new TJunction { Vertex = vertex, DisconnectedEdge = edge, Polygon = polygon });
                        // these neighbour polygons will result in T-Junctions.

                        // remove duplicate steiners so that we don't get steined to death with C# errors.
                        List<TJunction> antiSteiners = new List<TJunction>();
                        foreach (TJunction steiner in steiners)
                        {
                            bool found = false;
                            foreach (TJunction antiSteiner in antiSteiners)
                            {
                                if (antiSteiner.Vertex == steiner.Vertex)
                                {
                                    found = true;
                                    break;
                                }
                            }
                            if (!found)
                            {
                                antiSteiners.Add(steiner);
                            }
                        }
                        steiners = antiSteiners;

                        //Debug.Log(globalVertices.Count);
                        //Debug.Log(otherWorldVertices.Count());
                        //Debug.Log(steiners.Count());
                    }

                //Debug.Log("Yes Vertex on edge!");
                //unique.Add(vertex);
                //tjunctions.Add(new TJunction { Vertex = vertex, DisconnectedEdge = edge });

                // I found the T-Junctions, fix em!
                foreach (TJunction tjunction in steiners)
                {
                    // split the edge that the vertex is on top of (but not connected to).
                    int index = 0;
                    if (SplitPolygonAtEdge(tjunction.Polygon, tjunction.DisconnectedEdge, tjunction.Vertex, out index))
                    {
                        List<Vertex> oldTriangle = tjunction.Polygon.Vertices.ToList();
                        Vertex[] newTriangle = new Vertex[3];
                        if (index + 2 < tjunction.Polygon.Vertices.Length)
                        {
                            newTriangle[0] = tjunction.Polygon.Vertices[index + 0];
                            newTriangle[1] = tjunction.Polygon.Vertices[index + 1];
                            newTriangle[2] = tjunction.Polygon.Vertices[index + 2];

                            oldTriangle.Remove(tjunction.Polygon.Vertices[index + 1]);
                        }
                        else if (index - 2 >= 0)
                        {
                            newTriangle[2] = tjunction.Polygon.Vertices[index - 0];
                            newTriangle[1] = tjunction.Polygon.Vertices[index - 1];
                            newTriangle[0] = tjunction.Polygon.Vertices[index - 2];

                            oldTriangle.Remove(tjunction.Polygon.Vertices[index - 1]);
                        }
                        tjunction.Polygon.Vertices = oldTriangle.ToArray();

                        allGroupedPolygons[0].Add(new Polygon(newTriangle, tjunction.Polygon.Material, tjunction.Polygon.ExcludeFromFinal, tjunction.Polygon.UserExcludeFromFinal));
                    }
                    totalTJunctions++;

                    //globalVertices.Remove(tjunction.DisconnectedEdge.Vertex1.Position);
                    //globalVertices.Remove(tjunction.DisconnectedEdge.Vertex2.Position);
                }

                done = steiners.Count == 0;

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
        public static bool SplitPolygonAtEdge(Polygon polygon, Edge edge, Vector3 futurePosition, out int index)
        {
            index = 0;
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
                    index = i + 1;
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

        //public static void FixVertices(Dictionary<int, List<Polygon>> allGroupedPolygons)
        //{
        //    int totalMisalignments = 0;

        //    List<Vertex> vertices = new List<Vertex>();

        //    foreach (var groupedPolygon in allGroupedPolygons)
        //    {
        //        foreach (Polygon polygon in groupedPolygon.Value)
        //        {
        //            // build a collection of all edges and vertices.
        //            foreach (Vertex vertex in polygon.Vertices)
        //            {
        //                // check if a vertex in about the same position already exists.
        //                Vertex result = vertices.Find(v => v.Position.EqualsWithEpsilonLower3(vertex.Position));
        //                // if it didn't exist then add this vertex to our collection.
        //                if (result == null)
        //                    vertices.Add(vertex);
        //                // it exists so make sure the vertex is on exactly the same position.
        //                else
        //                {
        //                    if (vertex.Position != result.Position)
        //                    {
        //                        vertex.Position = result.Position;
        //                        totalMisalignments++;
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    if (totalMisalignments != 0)
        //        Debug.Log("(SabreCSG) FixTJunctions: Aligned " + totalMisalignments.ToString() + " vertices.");
        //}
    }
}

#endif