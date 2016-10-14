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

					for (int j = i+1; j < edges.Count; j++) 
					{
//						Vector3 point = planes[j].normal * planes[j].distance;
						Vector3 point = edges[j].MidPoint;
						int side = MathHelper.GetSideThick(edges[i].Plane, point);
                        if (side == -1)
                        {
                            frontEdges.Add(edges[j]);
                        }
                        else if (side == 1)
                        {
                            backEdges.Add(edges[j]);
                        }
                        else
                        {
                            frontEdges.Add(edges[j]);
                            backEdges.Add(edges[j]);
                        }
                    }

					front = new TriangulationNode(polygonsFront, frontEdges);
					back = new TriangulationNode(polygonsBack, backEdges);

//					Debug.Log(planes[i].ToStringLong() + " " + polygonsFront.Count + " " + polygonsBack.Count);

					// Created some child nodes so early out
					return;
				}
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