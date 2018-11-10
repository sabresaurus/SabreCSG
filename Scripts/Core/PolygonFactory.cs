#if UNITY_EDITOR || RUNTIME_CSG
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Sabresaurus.SabreCSG
{
	internal static class PolygonFactory
	{
#region INTERNAL
		internal static bool SplitPolygonsByPlane(List<Polygon> polygons, // Source polygons that will be split
		                                        Plane splitPlane, 
		                                        bool excludeNewPolygons, // Whether new polygons should be marked as excludeFromBuild
		                                        out List<Polygon> polygonsFront, 
		                                        out List<Polygon> polygonsBack)
		{
			polygonsFront = new List<Polygon>();
			polygonsBack = new List<Polygon>();
			
			// First of all make sure splitting actually needs to occur (we'll get bad issues if
			// we try splitting geometry when we don't need to)
			if(!GeometryHelper.PolygonsIntersectPlane(polygons, splitPlane))
			{
				return false;
			}
			
			// These are the vertices that will be used in the new caps
			List<Vertex> newVertices = new List<Vertex>();
			
			for (int polygonIndex = 0; polygonIndex < polygons.Count; polygonIndex++) 
			{
				Polygon.PolygonPlaneRelation planeRelation = Polygon.TestPolygonAgainstPlane(polygons[polygonIndex], splitPlane);
				
				// Polygon has been found to span both sides of the plane, attempt to split into two pieces
				if(planeRelation == Polygon.PolygonPlaneRelation.Spanning)
				{
					Polygon frontPolygon;
					Polygon backPolygon;
					Vertex newVertex1;
					Vertex newVertex2;
					
					// Attempt to split the polygon
					if(Polygon.SplitPolygon(polygons[polygonIndex], out frontPolygon, out backPolygon, out newVertex1, out newVertex2, splitPlane))
					{
						// If the split algorithm was successful (produced two valid polygons) then add each polygon to 
						// their respective points and track the intersection points
						polygonsFront.Add(frontPolygon);
						polygonsBack.Add(backPolygon);
						
						newVertices.Add(newVertex1);
						newVertices.Add(newVertex2);
					}
					else
					{
						// Two valid polygons weren't generated, so use the valid one
						if(frontPolygon != null)
						{
							planeRelation = Polygon.PolygonPlaneRelation.InFront;
						}
						else if(backPolygon != null)
						{
							planeRelation = Polygon.PolygonPlaneRelation.Behind;
						}
						else
						{
							planeRelation = Polygon.PolygonPlaneRelation.InFront;

							Debug.LogError("Polygon splitting has resulted in two zero area polygons. This is unhandled.");
							//							Polygon.PolygonPlaneRelation secondplaneRelation = Polygon.TestPolygonAgainstPlane(polygons[polygonIndex], splitPlane);
						}
					}
				}
				
				// If the polygon is on one side of the plane or the other
				if(planeRelation != Polygon.PolygonPlaneRelation.Spanning)
				{
					// Make sure any points that are coplanar on non-straddling polygons are still used in polygon 
					// construction
					for (int vertexIndex = 0; vertexIndex < polygons[polygonIndex].Vertices.Length; vertexIndex++) 
					{
						if(Polygon.ComparePointToPlane(polygons[polygonIndex].Vertices[vertexIndex].Position, splitPlane) == Polygon.PointPlaneRelation.On)
//						if(Polygon.ComparePointToPlane2(polygons[polygonIndex].Vertices[vertexIndex].Position, splitPlane) == Polygon.PointPlaneRelation.On)
						{
							newVertices.Add(polygons[polygonIndex].Vertices[vertexIndex]);
						}
					}
					
					if(planeRelation == Polygon.PolygonPlaneRelation.Behind)
					{
						polygonsBack.Add(polygons[polygonIndex]);
					}
					else 
					{
						polygonsFront.Add(polygons[polygonIndex]);
					}
				}
			}
			
			// If any splits occured or coplanar vertices are found. (For example if you're splitting a sphere at the
			// equator then no polygons will be split but there will be a bunch of coplanar vertices!)
			if(newVertices.Count > 0
				&& polygonsBack.Count >= 3
				&& polygonsFront.Count >= 3)
			{
				// HACK: This code is awful, because we end up with lots of duplicate vertices
				List<Vector3> positions = new List<Vector3>(newVertices.Count);
				for (int i = 0; i < newVertices.Count; i++) 
				{
					positions.Add(newVertices[i].Position);
				}

                Polygon newPolygon = PolygonFactory.ConstructPolygon(positions, true);
				
				// Assuming it was possible to create a polygon
				if(newPolygon != null)
				{
					if(!MathHelper.PlaneEqualsLooser(newPolygon.Plane, splitPlane))
					{
						// Polygons are sometimes constructed facing the wrong way, possibly due to a winding order
						// mismatch. If the two normals are opposite, flip the new polygon
						if(Vector3.Dot(newPolygon.Plane.normal, splitPlane.normal) < -0.9f)
						{
							newPolygon.Flip();
						}
					}
					
					newPolygon.ExcludeFromFinal = excludeNewPolygons;
					
					polygonsFront.Add(newPolygon);
					
					newPolygon = newPolygon.DeepCopy();
					newPolygon.Flip();
					
					newPolygon.ExcludeFromFinal = excludeNewPolygons;
					
					
					if(newPolygon.Plane.normal == Vector3.zero)
					{
						Debug.LogError("Invalid Normal! Shouldn't be zero. This is unexpected since extraneous positions should have been removed!");
						//						Polygon fooNewPolygon = PolygonFactory.ConstructPolygon(positions, true);
					}
					
					polygonsBack.Add(newPolygon);
				}
				return true;
			}
			else
			{
				// It wasn't possible to create the polygon, for example the constructed polygon was too small
				// This could happen if you attempt to clip the tip off a long but thin brush, the plane-polyhedron test
				// would say they intersect but in reality the resulting polygon would be near zero area
				return false;
			}
		}

        internal static bool SplitCoplanarPolygonsByPlane(List<Polygon> polygons, // Source polygons that will be split
                                                Plane splitPlane,
                                                out List<Polygon> polygonsFront,
                                                out List<Polygon> polygonsBack)
        {
            polygonsFront = new List<Polygon>();
            polygonsBack = new List<Polygon>();

            for (int polygonIndex = 0; polygonIndex < polygons.Count; polygonIndex++)
            {
                Polygon.PolygonPlaneRelation planeRelation = Polygon.TestPolygonAgainstPlane(polygons[polygonIndex], splitPlane);

                // Polygon has been found to span both sides of the plane, attempt to split into two pieces
                if (planeRelation == Polygon.PolygonPlaneRelation.Spanning)
                {
                    Polygon frontPolygon;
                    Polygon backPolygon;
                    Vertex newVertex1;
                    Vertex newVertex2;

                    // Attempt to split the polygon
                    if (Polygon.SplitPolygon(polygons[polygonIndex], out frontPolygon, out backPolygon, out newVertex1, out newVertex2, splitPlane))
                    {
                        // If the split algorithm was successful (produced two valid polygons) then add each polygon to 
                        // their respective points and track the intersection points
                        polygonsFront.Add(frontPolygon);
                        polygonsBack.Add(backPolygon);
                    }
                    else
                    {
                        // Two valid polygons weren't generated, so use the valid one
                        if (frontPolygon != null)
                        {
                            planeRelation = Polygon.PolygonPlaneRelation.InFront;
                        }
                        else if (backPolygon != null)
                        {
                            planeRelation = Polygon.PolygonPlaneRelation.Behind;
                        }
                        else
                        {
                            planeRelation = Polygon.PolygonPlaneRelation.InFront;

                            Debug.LogError("Polygon splitting has resulted in two zero area polygons. This is unhandled.");
                            //							Polygon.PolygonPlaneRelation secondplaneRelation = Polygon.TestPolygonAgainstPlane(polygons[polygonIndex], splitPlane);
                        }
                    }
                }

                // If the polygon is on one side of the plane or the other
                if (planeRelation != Polygon.PolygonPlaneRelation.Spanning)
                {
                    if (planeRelation == Polygon.PolygonPlaneRelation.Behind)
                    {
                        polygonsBack.Add(polygons[polygonIndex]);
                    }
                    else
                    {
                        polygonsFront.Add(polygons[polygonIndex]);
                    }
                }
            }

            if (polygonsBack.Count >= 1
                && polygonsFront.Count >= 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        /// <summary>
        /// Constructs a polygon from an unordered coplanar set of positions
        /// </summary>
        /// <param name="sourcePositions"></param>
        /// <param name="removeExtraPositions"></param>
        /// <returns></returns>
		public static Polygon ConstructPolygon(List<Vector3> sourcePositions, bool removeExtraPositions)
		{
			List<Vector3> positions;
			
			if(removeExtraPositions)
			{
				positions = new List<Vector3>();
				for (int i = 0; i < sourcePositions.Count; i++) 
				{
					Vector3 sourcePosition = sourcePositions[i];
					bool contained = false;

					for (int j = 0; j < positions.Count; j++) 
					{
						if(positions[j].EqualsWithEpsilonLower(sourcePosition))
						{
							contained = true;
							break;
						}
					}

					if(!contained)
					{
						positions.Add(sourcePosition);
					}
				}
			}
			else
			{
				positions = sourcePositions;
			}



			
			// If positions is smaller than 3 then we can't construct a polygon. This could happen if you try to cut the
			// tip off a very, very thin brush. While the plane and the brushes would intersect, the actual
			// cross-sectional area is near zero and too small to create a valid polygon. In this case simply return
			// null to indicate polygon creation was impossible
			if(positions.Count < 3)
			{
				return null;
			}
			
			// Find center point, so we can sort the positions around it
			Vector3 center = positions[0];
			
			for (int i = 1; i < positions.Count; i++)
			{
				center += positions[i];
			}
			
			center *= 1f / positions.Count;
			
			if(positions.Count < 3)
			{
				Debug.LogError("Position count is below 3, this is probably unhandled");
			}
			
			// Find the plane
			UnityEngine.Plane plane = new UnityEngine.Plane(positions[0], positions[1], positions[2]);
			
			
			
			// Rotation to go from the polygon's plane to XY plane (for sorting)
			Quaternion cancellingRotation;

			if(plane.normal != Vector3.zero)
			{
				cancellingRotation = Quaternion.Inverse(Quaternion.LookRotation(plane.normal));
			}
			else
			{
				cancellingRotation = Quaternion.identity;
			}

			// Rotate the center point onto the plane too
			Vector3 rotatedCenter = cancellingRotation * center;

            // Sort the positions, passing the rotation to put the positions on XY plane and the rotated center point
            IComparer<Vector3> comparer = new SortVectorsClockwise(cancellingRotation, rotatedCenter);
			positions.Sort(comparer);

			// Create the vertices from the positions
			Vertex[] newPolygonVertices = new Vertex[positions.Count];
			for (int i = 0; i < positions.Count; i++)
			{
				newPolygonVertices[i] = new Vertex(positions[i], -plane.normal, (cancellingRotation * positions[i]) * 0.5f);
			}
			Polygon newPolygon = new Polygon(newPolygonVertices, null, false, false);
			
			if(newPolygon.Plane.normal == Vector3.zero)
			{
				Debug.LogError("Zero normal found, this leads to invalid polyhedron-point tests");
				return null;
				// hacky
				//				if(removeExtraPositions)
				//				{
				//					Polygon.Vector3ComparerEpsilon equalityComparer = new Polygon.Vector3ComparerEpsilon();
				//					List<Vector3> testFoo = newPolygonVertices.Select(item => item.Position).Distinct(equalityComparer).ToList();
				//				}
			}
			return newPolygon;
		}



        /// <summary>
        /// Refines a set of vertices to form a valid convex hull
        /// </summary>
        /// <param name="sourceVertices"></param>
        /// <param name="removeExtraPositions"></param>
        /// <param name="removeCollinearPoints"></param>
        /// <param name="constructConvexHull"></param>
        /// <returns></returns>
        public static List<Vertex> RefineVertices(Vector3 normal, List<Vertex> sourceVertices, bool removeExtraPositions, bool removeCollinearPoints, bool constructConvexHull)
        {
            List<Vertex> vertices;

            if (removeExtraPositions)
            {
                vertices = new List<Vertex>();
                for (int i = 0; i < sourceVertices.Count; i++)
                {
                    Vertex sourceVertex = sourceVertices[i];
                    bool contained = false;

                    for (int j = 0; j < vertices.Count; j++)
                    {
                        if (vertices[j].Position.EqualsWithEpsilonLower(sourceVertex.Position))
                        {
                            contained = true;
                            break;
                        }
                    }

                    if (!contained)
                    {
                        vertices.Add(sourceVertex);
                    }
                }
            }
            else
            {
                vertices = sourceVertices;
            }



            // If vertices is smaller than 3 then we can't construct a polygon. This could happen if you try to cut the
            // tip off a very, very thin brush. While the plane and the brushes would intersect, the actual
            // cross-sectional area is near zero and too small to create a valid polygon. In this case simply return
            // null to indicate polygon creation was impossible
            if (vertices.Count < 3)
            {
                return null;
            }

            // Find center point, so we can sort the positions around it
            Vector3 center = vertices[0].Position;

            for (int i = 1; i < vertices.Count; i++)
            {
                center += vertices[i].Position;
            }

            center *= 1f / vertices.Count;

            if (vertices.Count < 3)
            {
                Debug.LogError("Vertex count is below 3, this is probably unhandled");
            }

            // Rotation to go from the polygon's plane to XY plane (for sorting)
            Quaternion cancellingRotation;

            if (normal != Vector3.zero)
            {
                cancellingRotation = Quaternion.Inverse(Quaternion.LookRotation(normal));
            }
            else
            {
                cancellingRotation = Quaternion.identity;
            }

            // Rotate the center point onto the plane too
            Vector3 rotatedCenter = cancellingRotation * center;

            if (constructConvexHull)
            {
                vertices = CalculateConvexHull(vertices, cancellingRotation, rotatedCenter);
            }

            // Sort the positions, passing the rotation to put the positions on XY plane and the rotated center point
            IComparer<Vertex> comparer = new SortVerticesClockwise(cancellingRotation, rotatedCenter);
            vertices.Sort(comparer);

            if (removeCollinearPoints)
            {
                List<Vertex> positionsCopy = new List<Vertex>();

                Vector3 lastDirection = (vertices[0].Position - vertices[vertices.Count - 1].Position).normalized;
                for (int i = 0; i < vertices.Count; i++)
                {
                    Vector3 nextDirection = (vertices[(i + 1) % vertices.Count].Position - vertices[i].Position).normalized;

                    if (Vector3.Dot(lastDirection, nextDirection) < 1.0f)
                    {
                        positionsCopy.Add(vertices[i]);
                    }

                    lastDirection = nextDirection;
                }

                // TODO: Need to handle wrap point

                vertices = positionsCopy;
            }

            vertices.Reverse();
            return vertices;
        }


        public static float Cross(Vector3 origin, Vector3 pointA, Vector3 pointB)
        {
            return (pointA.x - origin.x) * (pointB.y - origin.y) - (pointA.y - origin.y) * (pointB.x - origin.x);
        }

        public static List<Vertex> CalculateConvexHull(List<Vertex> positions, Quaternion cancellingRotation, Vector3 rotatedCenter)
        {
            // Key is position, Value is the position transformed to the XY plane
            List<KeyValuePair<Vertex, Vector3>> positionsComplex = new List<KeyValuePair<Vertex, Vector3>>(positions.Count);

            for (int i = 0; i < positions.Count; i++)
            {
                positionsComplex.Add(new KeyValuePair<Vertex, Vector3>(positions[i], cancellingRotation * positions[i].Position));
            }

            // Sort from left to right, then from bottom to top for values with the same X
            IComparer<KeyValuePair<Vertex, Vector3>> comparer = new SortVerticesByXThenY();
            positionsComplex.Sort(comparer);

            int positionCount = positions.Count;
            int numberAdded = 0;
            KeyValuePair<Vertex, Vector3>[] hullArray = new KeyValuePair<Vertex, Vector3>[2 * positionCount];

            // Build lower hull
            for (int i = 0; i < positionCount; ++i)
            {
                while (numberAdded >= 2 && Cross(hullArray[numberAdded - 2].Value, hullArray[numberAdded - 1].Value, positionsComplex[i].Value) <= 0)
                {
                    numberAdded--;
                }
                    
                hullArray[numberAdded] = positionsComplex[i];
                numberAdded++;
            }

            // Build upper hull
            for (int i = positionCount - 2, t = numberAdded + 1; i >= 0; i--)
            {
                while (numberAdded >= t && Cross(hullArray[numberAdded - 2].Value, hullArray[numberAdded - 1].Value, positionsComplex[i].Value) <= 0)
                {
                    numberAdded--;
                }
                    
                hullArray[numberAdded] = positionsComplex[i];
                numberAdded++;
            }

            List<Vertex> newPositions = new List<Vertex>(numberAdded);

            for (int i = 0; i < numberAdded; i++)
            {
                newPositions.Add(hullArray[i].Key);
            }

            return RemoveExtraneous(newPositions);
        }

        static List<Vertex> RemoveExtraneous(List<Vertex> sourceVertices)
        {
            List<Vertex> vertices = new List<Vertex>();
            for (int i = 0; i < sourceVertices.Count; i++)
            {
                Vertex sourceVertex = sourceVertices[i];
                bool contained = false;

                for (int j = 0; j < vertices.Count; j++)
                {
                    if (vertices[j].Position.EqualsWithEpsilonLower(sourceVertex.Position))
                    {
                        contained = true;
                        break;
                    }
                }

                if (!contained)
                {
                    vertices.Add(sourceVertex);
                }
            }
            return vertices;
        }

        internal static bool ChamferPolygons(List<Polygon> polygons, List<Edge> edges, float distance, int iterations, out List<Polygon> resultPolygons)
        {
            // list of clipping planes.
            List<Plane> clippingPlanes = new List<Plane>();
            List<Material> clippingPlaneMaterials = new List<Material>();

            // iterate through all edges and calculate the clipping planes.
            for (int e = 0; e < edges.Count; e++)
            {
                Edge edge = edges[e];

                // find the two polygons connected to the edge.
                Polygon[] matchingPolygons = polygons.Where(p => Polygon.ContainsEdge(p, edge)).ToArray();
                if (matchingPolygons.Length != 2) { resultPolygons = null; return false; };

                // find the actual edges on the polygons (which helps determine their direction for the chamfer).
                Edge realEdge1;
                Polygon.FindEdge(matchingPolygons[0], edge, out realEdge1);
                Edge realEdge2;
                Polygon.FindEdge(matchingPolygons[1], edge, out realEdge2);

                // calculate clipping plane position:
                Vector3 v1 = realEdge1.Vertex1.Position - ChamferPolygons_GetNormal(realEdge1.Vertex1.Position, realEdge1.Vertex2.Position, matchingPolygons[0].GetCenterPoint()).normalized * distance;
                Vector3 v2 = realEdge1.Vertex2.Position - ChamferPolygons_GetNormal(realEdge1.Vertex1.Position, realEdge1.Vertex2.Position, matchingPolygons[0].GetCenterPoint()).normalized * distance;
                Vector3 v3 = realEdge2.Vertex2.Position - ChamferPolygons_GetNormal(realEdge2.Vertex1.Position, realEdge2.Vertex2.Position, matchingPolygons[1].GetCenterPoint()).normalized * distance;

                for (int i = 0; i < iterations; i++)
                {
                    float t = (1.0f / iterations);
                    Vector3 p1 = ShapeEditor.Bezier.GetPoint(v1, realEdge1.Vertex1.Position, v3, t * i);
                    Vector3 p2 = ShapeEditor.Bezier.GetPoint(v1, realEdge1.Vertex1.Position, v3, t * (i + 1));
                    clippingPlanes.Add(new Plane(
                        p1,
                        p2,
                        p1 + (v1 - v2).normalized
                    ));
                    // find the most likely material we should be using for this chamfer.
                    // an attempt is made to ignore the default material.
                    clippingPlaneMaterials.Add(matchingPolygons[0].Material != null ? matchingPolygons[0].Material : matchingPolygons[1].Material);
                }
            }

            // copy the input polygons.
            resultPolygons = polygons.DeepCopy();

            // clip the polygons.
            for (int i = 0; i < clippingPlanes.Count; i++)
            {
                List<Polygon> polygonsFront;
                List<Polygon> polygonsBack;
                if (SplitPolygonsByPlane(resultPolygons, clippingPlanes[i], false, out polygonsFront, out polygonsBack))
                    resultPolygons = polygonsFront;
                // assign the most likely material to the new polygons.
                for (int j = 0; j < resultPolygons.Count; j++)
                    if (resultPolygons[j].Plane.normal.EqualsWithEpsilonLower3(clippingPlanes[i].normal))
                        resultPolygons[j].Material = clippingPlaneMaterials[i];
            }

            return true;
        }

        // todo: this should probably be moved somewhere else...
        private static Vector3 ChamferPolygons_GetNormal(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 side1 = b - a;
            Vector3 side2 = c - a;
            return Vector3.Cross(side1, side2).normalized;
        }
        #endregion

        #region PRIVATE
        // Used to sort a collection of Vectors in a clockwise direction
        internal class SortVectorsClockwise : IComparer<Vector3>
		{
			Quaternion cancellingRotation; // Used to transform the positions from an arbitrary plane to the XY plane
			Vector3 rotatedCenter; // Transformed center point, used as the center point to find the angles around
			
			public SortVectorsClockwise(Quaternion cancellingRotation, Vector3 rotatedCenter)
			{
				this.cancellingRotation = cancellingRotation;
				this.rotatedCenter = rotatedCenter;
			}
			
			public int Compare(Vector3 position1, Vector3 position2)
			{
				// Rotate the positions and subtract the center, so they become vectors from the center point on the plane
				Vector3 vector1 = (cancellingRotation * position1) - rotatedCenter;
				Vector3 vector2 = (cancellingRotation * position2) - rotatedCenter;
				
				// Find the angle of each vector on the plane
				float angle1 = Mathf.Atan2(vector1.x, vector1.y);
				float angle2 = Mathf.Atan2(vector2.x, vector2.y);
				
				// Compare the angles
				return angle1.CompareTo(angle2);
			}
		}

        internal class SortVerticesClockwise : IComparer<Vertex>
        {
            Quaternion cancellingRotation; // Used to transform the positions from an arbitrary plane to the XY plane
            Vector3 rotatedCenter; // Transformed center point, used as the center point to find the angles around

            public SortVerticesClockwise(Quaternion cancellingRotation, Vector3 rotatedCenter)
            {
                this.cancellingRotation = cancellingRotation;
                this.rotatedCenter = rotatedCenter;
            }

            public int Compare(Vertex vertex1, Vertex vertex2)
            {
                // Rotate the positions and subtract the center, so they become vectors from the center point on the plane
                Vector3 vector1 = (cancellingRotation * vertex1.Position) - rotatedCenter;
                Vector3 vector2 = (cancellingRotation * vertex2.Position) - rotatedCenter;

                // Find the angle of each vector on the plane
                float angle1 = Mathf.Atan2(vector1.x, vector1.y);
                float angle2 = Mathf.Atan2(vector2.x, vector2.y);

                // Compare the angles
                return angle1.CompareTo(angle2);
            }
        }

        internal class SortVerticesByXThenY : IComparer<KeyValuePair<Vertex, Vector3>>
        {
            public int Compare(KeyValuePair<Vertex, Vector3> vertex1, KeyValuePair<Vertex, Vector3> vertex2)
            {
                Vector3 localPosition1 = vertex1.Value;
                Vector3 localPosition2 = vertex2.Value;

                if (localPosition1.x != localPosition2.x) // If x is difference compare by them
                {
                    return localPosition1.x.CompareTo(localPosition2.x);
                }
                else // x is the same so compare by y
                {
                    return localPosition1.y.CompareTo(localPosition2.y);
                }
            }
        }

		internal static void GenerateMeshFromPolygons(Polygon[] polygons, ref Mesh mesh)
		{
			if(mesh == null)
			{
				mesh = new Mesh();
			}
			mesh.Clear();
			//	        mesh = new Mesh();
			List<Vector3> vertices = new List<Vector3>();
			List<Vector3> normals = new List<Vector3>();
			List<Vector2> uvs = new List<Vector2>();
			List<Color32> colors = new List<Color32>();
			List<int> triangles = new List<int>();

			// Set up an indexer that tracks unique vertices, so that we reuse vertex data appropiately
			VertexList vertexList = new VertexList();

			// Iterate through every polygon and triangulate
			for (int i = 0; i < polygons.Length; i++)
			{
				Polygon polygon = polygons[i];
				List<int> indices = new List<int>();

				for (int j = 0; j < polygon.Vertices.Length; j++)
				{
					// Each vertex must know about its shared data for geometry tinting
					//polygon.Vertices[j].Shared = polygon.SharedBrushData;
					// If the vertex is already in the indexer, fetch the index otherwise add it and get the added index
					int index = vertexList.Add(polygon.Vertices[j]);
					// Put each vertex index in an array for use in the triangle generation
					indices.Add(index);
				}

				// Triangulate the n-sided polygon and allow vertex reuse by using indexed geometry
				for (int j = 2; j < indices.Count; j++)
				{
					triangles.Add(indices[0]);
					triangles.Add(indices[j - 1]);
					triangles.Add(indices[j]);
				}
			}

			// Create the relevant buffers from the vertex array
			for (int i = 0; i < vertexList.Vertices.Count; i++)
			{
				vertices.Add(vertexList.Vertices[i].Position);
				normals.Add(vertexList.Vertices[i].Normal);
				uvs.Add(vertexList.Vertices[i].UV);
				//	                colors.Add(((SharedBrushData)indexer.Vertices[i].Shared).BrushTintColor);
			}

			// Set the mesh buffers
			mesh.vertices = vertices.ToArray();
			mesh.normals = normals.ToArray();
			mesh.colors32 = colors.ToArray();
			mesh.uv = uvs.ToArray();
			mesh.triangles = triangles.ToArray();
		}

		internal static Polygon[] Triangulate(Polygon polygon)
		{
			int triangleCount = polygon.Vertices.Length - 2;
			Polygon[] triangles = new Polygon[triangleCount];

			// Calculate triangulation
			for (int j = 0; j < triangleCount; j++) 
			{
				triangles[j] = new Polygon(
					new Vertex[]
					{
						polygon.Vertices[0],
						polygon.Vertices[j+1],
						polygon.Vertices[j+2]
					},
					polygon.Material,
					true, 
					true);
			}

			return triangles;
		}

#endregion
	}
}
#endif