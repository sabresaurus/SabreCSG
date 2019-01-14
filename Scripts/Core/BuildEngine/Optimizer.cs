#if UNITY_EDITOR || RUNTIME_CSG

using System.Collections.Generic;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
	internal class SimpleEdge
	{
		public Vector3 Position1;
		public Vector3 Position2;

		public Vector3 MidPoint; // Used by the BSP Tree

		public Plane Plane = new Plane();

		public int Count = 1;

		public SimpleEdge(Vector3 position1, Vector3 position2, Vector3 normal, int startingCount = 1)
		{
			this.Position1 = position1;
			this.Position2 = position2;
            this.Count = startingCount;

			// Cache the mid point of the two positions
			this.MidPoint = (position1 + position2) * 0.5f;

			// Cache the plane that this edge forms perpendicular with the polygon it came from
			Plane = new Plane(position1, position2, position1 + normal);
		}

		public bool Matches(Vector3 otherPosition1, Vector3 otherPosition2)
		{
			if (Position1.EqualsWithEpsilonLower3(otherPosition1)
				&& Position2.EqualsWithEpsilonLower3(otherPosition2))
			{
				return true;
			} // Check if the edge is the other way around
			else if (Position1.EqualsWithEpsilonLower3(otherPosition2)
				&& Position2.EqualsWithEpsilonLower3(otherPosition1))
			{
				return true;
			}
			else
			{
				return false;
			}
		}

        public bool CollinearIntersects(Vector3 otherPosition1, Vector3 otherPosition2)
        {
            // TODO Cache this
            Vector3 tangent = (Position2 - Position1).normalized;
            Vector3 otherTangent = (otherPosition2 - otherPosition1).normalized;

            float dot = Vector3.Dot(tangent, otherTangent);
            if(dot > 0.99f
                || dot < -0.99f)
            {
                // It is parallel
                float upDot = Vector3.Dot(tangent, Vector3.up);
                Vector3 normal;
                if (Mathf.Abs(upDot) > 0.9f)
                {
                    normal = Vector3.Cross(Vector3.forward, tangent).normalized;
                }
                else
                {
                    normal = Vector3.Cross(Vector3.up, tangent).normalized;
                }

                Vector3 binormal = Vector3.Cross(normal, tangent);

                float dotNormal = Vector3.Dot(Position1, normal);
                float dotBinormal = Vector3.Dot(Position1, binormal);

                float otherDotNormal = Vector3.Dot(otherPosition1, normal);
                float otherDotBinormal = Vector3.Dot(otherPosition1, binormal);

                if(Mathf.Abs(dotNormal - otherDotNormal) < 0.1f
                    && Mathf.Abs(dotBinormal - otherDotBinormal) < 0.1f)
                {
                    // It is collinear
                    float t1 = Vector3.Dot(tangent, Position1);
                    float t2 = Vector3.Dot(tangent, Position2);

                    float ot1;
                    float ot2;

                    if(dot > 0)
                    {
                        ot1 = Vector3.Dot(tangent, otherPosition1);
                        ot2 = Vector3.Dot(tangent, otherPosition2);
                    }
                    else
                    {
                        ot1 = Vector3.Dot(tangent, otherPosition2);
                        ot2 = Vector3.Dot(tangent, otherPosition1);
                    }

                    if (ot1 < t2 - 0.01f
                        && ot2 > t1 + 0.01f)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    // Not collinear
                    return false;
                }
            }
            else
            {
                // Not parallel
                return false;
            }
        }

    }

    internal static class Optimizer
    {
		/// <summary>
		/// Given a set of coplanar polygons, this method uses the perimeter and calculates the convex hulls. Concavities and holes are supported and in general this method has been observed to produce mathematically optimal or close to. Applying a heuristic to the BSP tree phase could reduce excessive splitting, but from observation the current method is probably good enough. 
		/// </summary>
		/// <returns>A set of polygons that describe the convex hulls.</returns>
		/// <param name="polygons">Coplanar set of polygons.</param>
		internal static List<Polygon> CalculateConvexHulls(List<Polygon> polygons)
        {
            // Convex hull of 1 polygon is guaranteed to be itself, so early out. Note that care must be taken as the reference is the same as the input
            //if(polygons.Count == 1)
            //{
            //    return polygons;
            //}

            //Coplanar test. If failed, abort optimization;
            for (int i = 0; i < polygons.Count; i++)
            {
                for (int j = i + 1; j < polygons.Count; j++)
                {
                    if (!MathHelper.PlaneEqualsLooser(polygons[i].Plane, polygons[j].Plane))
                    {
                        return polygons;
                    }
                }
            }
            // Cache the polygon's normal so that we don't have to calculate it for the new polygons
            Vector3 normal = polygons[0].Plane.normal;

            // TODO: Generate islands

			// Create a list of edges from the source polygons and track how many times each edge occurs
            List<SimpleEdge> edges = new List<SimpleEdge>();

            for (int i = 0; i < polygons.Count; i++)
            {
                Polygon polygon = polygons[i];
                //if (!polygons[0].UserExcludeFromFinal)
                //    VisualDebug.AddPolygon(polygon, Color.blue);
                Edge[] polygonEdges = polygon.GetEdges();

                for (int j = 0; j < polygonEdges.Length; j++)
                {
                    Vector3 position1 = polygonEdges[j].Vertex1.Position;
                    Vector3 position2 = polygonEdges[j].Vertex2.Position;
                    //int foundIndex = edges.FindIndex(item => item.Matches(position1, position2));
                    List<SimpleEdge> found = edges.FindAll(item => item.CollinearIntersects(position1, position2));

                    if(found.Count > 0)
                    {
                        for (int k = 0; k < found.Count; k++)
                        {
                            found[k].Count++;
                        }
                        edges.Add(new SimpleEdge(position1, position2, polygons[0].Plane.normal, 2));
                    }
                    else
                    {
						edges.Add(new SimpleEdge(position1, position2, polygons[0].Plane.normal));
                    }
                }
            }

			// Construct a list of perimeter edges by extracting the edges that only occur once
			List<SimpleEdge> splitEdges = new List<SimpleEdge>();
            for (int i = 0; i < edges.Count; i++)
            {
                if(edges[i].Count == 1)// && (edges[i].Position2 - edges[i].Position1).sqrMagnitude > Mathf.Pow(0.01f, 2))
                {
					splitEdges.Add(edges[i]);
                    //VisualDebug.AddLine(edges[i].Position1, edges[i].Position2, Color.green, 8);
                }
                //else
                //{
                //    if (!polygons[0].UserExcludeFromFinal && i == 838)
                //        VisualDebug.AddLine(edges[i].Position1, edges[i].Position2, Color.red, 8);
                //}
            }

			// Using a BSP Tree subdivide the source polygons until they are classified into convex hulls. At this stage each convex hull may contain multiple polygons
            List<Polygon> unsortedPolygons = new List<Polygon>(polygons);
			TriangulationNode rootNode = new TriangulationNode(unsortedPolygons, splitEdges);

			// Pull out all the convex hull polygon sets from the BSP Tree
			List<List<Polygon>> convexHulls = rootNode.GetAggregated();

			//string message = "Convex hulls found: " + convexHulls.Count + " { ";

			List<Polygon> outputPolygons = new List<Polygon>(convexHulls.Count);

            // Walk through each set of polygons that maps to a convex hull
            for (int i = 0; i < convexHulls.Count; i++)
            {
                //float hue = i / 6f;
                //float sat = 1 - 0.6f * Mathf.Floor(i / 6f);
                //Color color = Color.HSVToRGB(hue, sat, 1);


                // Aggregate all the vertices from all the polygons classified in this convex hull
                List<Vertex> allVertices = new List<Vertex>();
                for (int j = 0; j < convexHulls[i].Count; j++)
                {

                    //if (!polygons[0].UserExcludeFromFinal)
                    //{
                    //    VisualDebug.AddLinePolygon(convexHulls[i][j].Vertices, Color.red);
                    //}

                    // TODO: Should this be a deep copy?
                    //                    allPositions.AddRange(convexHulls[i][j].Vertices);
                    allVertices.AddRange(convexHulls[i][j].Vertices.DeepCopy());
                }

                //if(!polygons[0].UserExcludeFromFinal && i == 15)
                //{
                //    VisualDebug.AddLinePolygon(allVertices, color);
                //}

                if(allVertices.Count < 3)
                {
                    continue;
                }
                    

                // Refine the source vertices into the minimum needed to form a convex hull, stripping out cospatial, collinear and interior vertices
                List <Vertex> newVertices = PolygonFactory.RefineVertices(normal, allVertices, true, true, true);
				// Now that the vertices of the convex hull have been refined, create a new polygon from them using the source polygon as a template
                Polygon polygon = new Polygon(newVertices.ToArray(), polygons[0].Material, polygons[0].ExcludeFromFinal, polygons[0].UserExcludeFromFinal, polygons[0].UniqueIndex);

                // TODO: Shouldn't need to do this
                //polygon.Flip();
                outputPolygons.Add(polygon);

				//Vertex[] vertices2 = polygon.Vertices;
				//message += vertices2.Length + ", ";
				//for (int k = 0; k < vertices2.Length; k++)
				//{
				//	VisualDebug.AddPoint(vertices2[k].Position, color, 0.1f);
				//}
				//VisualDebug.AddPolygon(polygon, color);
            }

			//message += " }";
			//Debug.Log(message);

			return outputPolygons;
        }


    }
}
#endif