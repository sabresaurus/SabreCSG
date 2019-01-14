#if UNITY_EDITOR || RUNTIME_CSG

using System.Collections.Generic;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
	internal class TriangulationNode
	{
		TriangulationNode front = null;
		TriangulationNode back = null;

//		Plane? activePlane = null;

		// Leaf nodes have polygons
		List<Polygon> polygonsAtNode = new List<Polygon>();

		internal TriangulationNode(List<Polygon> polygonsToProcess, List<SimpleEdge> edges)
		{
            List<Polygon> LastPolygonsFront=null;
            List<Polygon> LastPolygonsBack = null;
            List<SimpleEdge> LastFrontEdges = null;
            List<SimpleEdge> LastBackEdges = null;
            bool hasFound = false;
            for (int i = 0; i < edges.Count; i++)
			{
				List<Polygon> polygonsFront;
				List<Polygon> polygonsBack;
                
                if (PolygonFactory.SplitCoplanarPolygonsByPlane(polygonsToProcess, edges[i].Plane, out polygonsFront, out polygonsBack))
				{
					// Split success
//					activePlane = planes[i];
    
					List<SimpleEdge> frontEdges = new List<SimpleEdge>();
					List<SimpleEdge> backEdges = new List<SimpleEdge>();
                    bool cutedge = false;
                    
					for (int j = 0; j < edges.Count; j++) 
					{
                        if (i == j) continue;
//						Vector3 point = planes[j].normal * planes[j].distance;
                        //when you cut through an edge, by checking midpoint you will put the edge in one side while it should be in both side
                        //So we should check both endpoint instead
						Vector3 point1 = edges[j].Position1;
                        Vector3 point2 = edges[j].Position2;
                        int side1 = MathHelper.GetSideThick(edges[i].Plane, point1);
                        int side2 = MathHelper.GetSideThick(edges[i].Plane, point2);
                        if (side1 * side2 >= 0)
                        {
                            if (side1 + side2 < 0)
                            {
                                frontEdges.Add(edges[j]);
                            }
                            else
                            {
                                backEdges.Add(edges[j]);
                            }
                        }
                        else
                        {
                            cutedge = true;
                            frontEdges.Add(edges[j]);
                            backEdges.Add(edges[j]);
                        }
                    }
                    //don't early out when we cut through an edge, we will find a better cut
                    if (cutedge) {
                        hasFound = true;
                        //Debug.DrawLine(edges[i].Position1, edges[i].Position2, Color.red, 300, false);
                        LastPolygonsFront = polygonsFront;
                        LastPolygonsBack = polygonsBack;
                        LastFrontEdges = frontEdges;
                        LastBackEdges =backEdges;
                        continue;
                    }
                    //if(hasFound) Debug.DrawLine(edges[i].Position1, edges[i].Position2, Color.green, 300, false);
                    //else Debug.DrawLine(edges[i].Position1, edges[i].Position2, Color.blue, 300, false);
                    front = new TriangulationNode(polygonsFront, frontEdges);
					back = new TriangulationNode(polygonsBack, backEdges);

//					Debug.Log(planes[i].ToStringLong() + " " + polygonsFront.Count + " " + polygonsBack.Count);

					// Created some child nodes so early out
					return;
				}
			}
            //Fallback if we haven't found a better one
            if (hasFound)
            {
                front = new TriangulationNode(LastPolygonsFront, LastFrontEdges);
                back = new TriangulationNode(LastPolygonsBack, LastBackEdges);
                return;
            }
			this.polygonsAtNode = polygonsToProcess;
		}

		public List<List<Polygon>> GetAggregated()
		{
			List<List<Polygon>> aggregate = new List<List<Polygon>>();
			if(polygonsAtNode.Count > 0)
			{
				aggregate.Add(polygonsAtNode);
			}

			if(front != null)
			{
				aggregate.AddRange(front.GetAggregated());
			}

			if(back != null)
			{
				aggregate.AddRange(back.GetAggregated());
			}

			return aggregate;
		}
	}
}
#endif