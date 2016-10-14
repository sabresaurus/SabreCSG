#if UNITY_EDITOR || RUNTIME_CSG
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Sabresaurus.SabreCSG
{
	/// <summary>
	/// Holds information on a Ray to Polygon series raycast result
	/// </summary>
	public struct PolygonRaycastHit
	{
		public Vector3 Point; // Point at which the ray hit the polygon
		public Vector3 Normal; // Surface normal of the hit polygon
		public float Distance; // Distance along the ray at which the hit occurred
		public GameObject GameObject; // Brush that the polygon exists on (or <c>null</c> if not relevant)
		public Polygon Polygon; // Hit polygon
	}

	/// <summary>
	/// Provides general helper methods for dealing with geometry
	/// </summary>
	public static class GeometryHelper
	{
        const float TEST_EPSILON = 0.003f;
        private const float CONVEX_EPSILON = 0.01f;

        const float EPSILON = 1e-5f; // Used to avoid floating point test issues, e.g. -0.000001 may be considered 0
        const float EPSILON_LOWER = 1e-4f;
        const float EPSILON_LOWER_2 = 1e-3f;


        /// <summary>
        /// Determines if a set of polygons represent a convex brush with planar polygons
        /// </summary>
        /// <returns><c>true</c> if is brush is convex and all polygons are planar; otherwise, <c>false</c>.</returns>
        /// <param name="polygons">Source polygons.</param>
        public static bool IsBrushConvex(Polygon[] polygons)
		{
			for (int n = 0; n < polygons.Length; n++) 
			{
				for (int k = 0; k < polygons[n].Vertices.Length; k++) 
				{
					// Test every vertex against every plane, if the vertex is front of the plane then the brush is concave
					for (int i = 0; i < polygons.Length; i++) 
					{
						Polygon polygon = polygons[i];
						for (int z = 2; z < polygon.Vertices.Length; z++) 
						{
							Plane polygonPlane = new Plane(polygon.Vertices[0].Position, 
								polygon.Vertices[z-1].Position, 
								polygon.Vertices[z].Position);


							float dot = Vector3.Dot(polygonPlane.normal, polygons[n].Vertices[k].Position) + polygonPlane.distance;

							if(dot > CONVEX_EPSILON)
							{
								return false;
							}
						}

						for (int z = 0; z < polygon.Vertices.Length; z++) 
						{
							Plane polygonPlane = new Plane(polygon.Vertices[z].Position, 
								polygon.Vertices[(z+1)%polygon.Vertices.Length].Position, 
								polygon.Vertices[(z+2)%polygon.Vertices.Length].Position);


							float dot = Vector3.Dot(polygonPlane.normal, polygons[n].Vertices[k].Position) + polygonPlane.distance;

							if(dot > CONVEX_EPSILON)
							{
								return false;
							}
						}
					}

				}
			}

			return true;
		}
        /// <summary>
        /// Raycasts a series of polygons, returning the hit polygons in order from the ray origin
        /// </summary>
        /// <returns>The sorted list of hit polygons.</returns>
        /// <param name="polygons">Source polygons to raycast against.</param>
        /// <param name="ray">Ray.</param>
        /// <param name="polygonSkin">Optional polygon skin that allows the polygon to be made slightly larger by displacing its vertices.</param>
        public static List<PolygonRaycastHit> RaycastPolygonsAll(List<Polygon> polygons, Ray ray, float polygonSkin = 0)
        {
            List<PolygonRaycastHit> hits = new List<PolygonRaycastHit>();
            if (polygons != null)
            {
                for (int i = 0; i < polygons.Count; i++)
                {
                    if (polygons[i].ExcludeFromFinal)
                    {
                        continue;
                    }

                    // Skip any polygons that are facing away from the ray
                    if (Vector3.Dot(polygons[i].Plane.normal, ray.direction) > 0)
                    {
                        continue;
                    }

                    if (GeometryHelper.RaycastPolygon(polygons[i], ray, polygonSkin))
                    {
                        // Get the real hit point by testing the ray against the polygon's plane
                        Plane plane = polygons[i].Plane;

                        float rayDistance;
                        plane.Raycast(ray, out rayDistance);
                        Vector3 hitPoint = ray.GetPoint(rayDistance);

                        hits.Add(new PolygonRaycastHit()
                        {
                            Distance = rayDistance,
                            Point = hitPoint,
                            Normal = polygons[i].Plane.normal,
                            GameObject = null,
                            Polygon = polygons[i]
                        });
                    }
                }
            }

            hits.Sort((x, y) => x.Distance.CompareTo(y.Distance));
            return hits;
        }

        /// <summary>
        /// Raycasts a series of polygons, returning the hit polygon or <c>null</c>
        /// </summary>
        /// <returns>The hit polygon or <c>null</c> if no polygon was hit.</returns>
        /// <param name="polygons">Source polygons to raycast against.</param>
        /// <param name="ray">Ray.</param>
        /// <param name="hitDistance">If a hit ocurred, this is how far along the ray the hit was.</param>
        /// <param name="polygonSkin">Optional polygon skin that allows the polygon to be made slightly larger by displacing its vertices.</param>
        public static Polygon RaycastPolygons(List<Polygon> polygons, Ray ray, out float hitDistance, float polygonSkin = 0)
		{
			Polygon closestPolygon = null;
			float closestSquareDistance = float.PositiveInfinity;
			hitDistance = 0;

			if(polygons != null)
			{
				// 
				for (int i = 0; i < polygons.Count; i++) 
				{
					if(polygons[i].ExcludeFromFinal)
					{
						continue;
					}

					// Skip any polygons that are facing away from the ray
					if(Vector3.Dot(polygons[i].Plane.normal, ray.direction) > 0)
					{
						continue;
					}

					if(GeometryHelper.RaycastPolygon(polygons[i], ray, polygonSkin))
					{
						// Get the real hit point by testing the ray against the polygon's plane
						Plane plane = polygons[i].Plane;

						float rayDistance;
						plane.Raycast(ray, out rayDistance);
						Vector3 hitPoint = ray.GetPoint(rayDistance);

						// Find the square distance from the camera to the hit point (squares used for speed)
						float squareDistance = (ray.origin - hitPoint).sqrMagnitude;
						// If the distance is closer than the previous closest polygon, use this one.
						if(squareDistance < closestSquareDistance)
						{
							closestPolygon = polygons[i];
							closestSquareDistance = squareDistance;
							hitDistance = rayDistance;
						}
					}
				}
			}

			return closestPolygon;
		}

		/// <summary>
		/// Raycasts the polygon.
		/// </summary>
		/// <returns><c>true</c>, if polygon was raycasted, <c>false</c> otherwise.</returns>
		/// <param name="polygon">Polygon.</param>
		/// <param name="ray">Ray.</param>
		/// <param name="polygonSkin">Polygon skin.</param>
		/// 
		/// <summary>
		/// Raycasts a polygons, returning <c>true</c> if a hit occurred; otherwise <c>false</c>
		/// </summary>
		/// <returns><c>true</c> if a hit occurred; otherwise <c>false</c></returns>
		/// <param name="polygons">Source polygon to raycast against.</param>
		/// <param name="ray">Ray.</param>
		/// <param name="hitDistance">If a hit ocurred, this is how far along the ray the hit was.</param>
		/// <param name="polygonSkin">Optional polygon skin that allows the polygon to be made slightly larger by displacing its vertices.</param>
		public static bool RaycastPolygon(Polygon polygon, Ray ray, float polygonSkin = 0)
		{
			// Note: This probably won't work if the ray and polygon are coplanar, but right now that's not a usecase
//			polygon.CalculatePlane();
			Plane plane = polygon.Plane;
			float distance = 0;

			// First of all find if and where the ray hit's the polygon's plane
			if(plane.Raycast(ray, out distance))
			{
				Vector3 hitPoint = ray.GetPoint(distance);

				// Now find out if the point on the polygon plane is behind each polygon edge
				for (int i = 0; i < polygon.Vertices.Length; i++) 
				{
					Vector3 point1 = polygon.Vertices[i].Position;
					Vector3 point2 = polygon.Vertices[(i+1)%polygon.Vertices.Length].Position;

					Vector3 edge = point2 - point1; // Direction from a vertex to the next
					Vector3 polygonNormal = plane.normal;

					// Cross product of the edge with the polygon's normal gives the edge's normal
					Vector3 edgeNormal = Vector3.Cross(edge, polygonNormal);

					Vector3 edgeCenter = (point1+point2) * 0.5f;

					if(polygonSkin != 0)
					{
						edgeCenter += edgeNormal.normalized * polygonSkin;
					}

					Vector3 pointToEdgeCentroid = edgeCenter - hitPoint;

					// If the point is outside an edge this will return a negative value
					if(Vector3.Dot(edgeNormal, pointToEdgeCentroid) < 0)
					{
						return false;
					}
				}

				return true;
			}
			else
			{
				return false;
			}
		}
		
		/// <summary>
		/// Determine if any vertices from a series of polygons are on opposite sides of a plane. This basically tests against a really thick plane to see if some of the points are on each side of the thick plane. This makes sure we only split if we definitely need to (protecting against issues related to splitting very small polygons breaking other code).
		/// </summary>
		/// <returns><c>true</c>, if intersection was found, <c>false</c> otherwise.</returns>
		/// <param name="polygons">Source Polygons.</param>
		/// <param name="testPlane">Test plane.</param>
		public static bool PolygonsIntersectPlane (List<Polygon> polygons, Plane testPlane)
		{
			int numberInFront = 0;
			int numberBehind = 0;

			float distanceInFront = 0f;
			float distanceBehind = 0f;

			for (int polygonIndex = 0; polygonIndex < polygons.Count; polygonIndex++) 
			{
				for (int vertexIndex = 0; vertexIndex < polygons[polygonIndex].Vertices.Length; vertexIndex++) 
				{
					Vector3 point = polygons[polygonIndex].Vertices[vertexIndex].Position;

					float distance = testPlane.GetDistanceToPoint(point);

					if (distance < -TEST_EPSILON)
					{
						numberInFront++;

						distanceInFront = Mathf.Min(distanceInFront, distance);
					}
					else if (distance > TEST_EPSILON)
					{
						numberBehind++;

						distanceBehind = Mathf.Max(distanceBehind, distance);
					}
				}
			}

			if(numberInFront > 0 && numberBehind > 0)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Finds the UV for a supplied position on a polygon, note this internally handles situations where vertices overlap or are colinear which the other version of this method does not
		/// </summary>
		/// <returns>The UV for the supplied position.</returns>
		/// <param name="polygon">Polygon.</param>
		/// <param name="newPosition">Position to find the UV for.</param>
		public static Vector2 GetUVForPosition(Polygon polygon, Vector3 newPosition)
		{
			int vertexIndex1 = 0;
			int vertexIndex2 = 0;
			int vertexIndex3 = 0;

			// Account for overlapping vertices
			for (int i = vertexIndex1+1; i < polygon.Vertices.Length; i++) 
			{
				if(!polygon.Vertices[i].Position.EqualsWithEpsilon(polygon.Vertices[vertexIndex1].Position))
				{
					vertexIndex2 = i;
					break;
				}
			}

			for (int i = vertexIndex2+1; i < polygon.Vertices.Length; i++) 
			{
				if(!polygon.Vertices[i].Position.EqualsWithEpsilon(polygon.Vertices[vertexIndex2].Position))
				{
					vertexIndex3 = i;
					break;
				}
			}

			// Now account for the fact that the picked three vertices might be collinear
			Vector3 pos1 = polygon.Vertices[vertexIndex1].Position;
			Vector3 pos2 = polygon.Vertices[vertexIndex2].Position;
			Vector3 pos3 = polygon.Vertices[vertexIndex3].Position;

			Plane plane = new Plane(pos1,pos2,pos3);
			if(plane.normal == Vector3.zero)
			{
				for (int i = 2; i < polygon.Vertices.Length; i++) 
				{
					vertexIndex3 = i;

					pos3 = polygon.Vertices[vertexIndex3].Position;

					Plane tempPlane = new Plane(pos1,pos2,pos3);

					if(tempPlane.normal != Vector3.zero)
					{
						break;
					}
				}
				plane = new Plane(pos1,pos2,pos3);
			}

			// Should now have a good set of positions, so continue

			Vector3 planePoint = MathHelper.ClosestPointOnPlane(newPosition, plane);

			Vector2 uv1 = polygon.Vertices[vertexIndex1].UV;
			Vector2 uv2 = polygon.Vertices[vertexIndex2].UV;
			Vector2 uv3 = polygon.Vertices[vertexIndex3].UV;

			// calculate vectors from point f to vertices p1, p2 and p3:
			Vector3 f1 = pos1-planePoint;
			Vector3 f2 = pos2-planePoint;
			Vector3 f3 = pos3-planePoint;

			// calculate the areas (parameters order is essential in this case):
			Vector3 va = Vector3.Cross(pos1-pos2, pos1-pos3); // main triangle cross product
			Vector3 va1 = Vector3.Cross(f2, f3); // p1's triangle cross product
			Vector3 va2 = Vector3.Cross(f3, f1); // p2's triangle cross product
			Vector3 va3 = Vector3.Cross(f1, f2); // p3's triangle cross product

			float a = va.magnitude; // main triangle area

			// calculate barycentric coordinates with sign:
			float a1 = va1.magnitude/a * Mathf.Sign(Vector3.Dot(va, va1));
			float a2 = va2.magnitude/a * Mathf.Sign(Vector3.Dot(va, va2));
			float a3 = va3.magnitude/a * Mathf.Sign(Vector3.Dot(va, va3));

			// find the uv corresponding to point f (uv1/uv2/uv3 are associated to p1/p2/p3):
			Vector2 uv = uv1 * a1 + uv2 * a2 + uv3 * a3;

			return uv;
		}

		/// <summary>
		/// Given three position/UVs that can be used to reliably describe the UV find the UV for the specified position. Note if you do not have three vertices that will definitely provide reliable results you should use the other overload of this method which takes a polygon.
		/// </summary>
		/// <returns>The UV for the supplied position.</returns>
		/// <param name="pos1">Pos 1.</param>
		/// <param name="pos2">Pos 2.</param>
		/// <param name="pos3">Pos 3.</param>
		/// <param name="uv1">UV 1 (corresponding to Pos1).</param>
		/// <param name="uv2">UV 2 (corresponding to Pos2).</param>
		/// <param name="uv3">UV 3 (corresponding to Pos3).</param>
		/// <param name="newPosition">Position to find the UV for.</param>
		public static Vector2 GetUVForPosition(Vector3 pos1, Vector3 pos2, Vector3 pos3, 
			Vector2 uv1, Vector2 uv2, Vector2 uv3, 
			Vector3 newPosition)
		{
			Plane plane = new Plane(pos1,pos2,pos3);
			Vector3 planePoint = MathHelper.ClosestPointOnPlane(newPosition, plane);

			// calculate vectors from point f to vertices p1, p2 and p3:
			Vector3 f1 = pos1-planePoint;
			Vector3 f2 = pos2-planePoint;
			Vector3 f3 = pos3-planePoint;

			// calculate the areas (parameters order is essential in this case):
			Vector3 va = Vector3.Cross(pos1-pos2, pos1-pos3); // main triangle cross product
			Vector3 va1 = Vector3.Cross(f2, f3); // p1's triangle cross product
			Vector3 va2 = Vector3.Cross(f3, f1); // p2's triangle cross product
			Vector3 va3 = Vector3.Cross(f1, f2); // p3's triangle cross product

			float a = va.magnitude; // main triangle area

			// calculate barycentric coordinates with sign:
			float a1 = va1.magnitude/a * Mathf.Sign(Vector3.Dot(va, va1));
			float a2 = va2.magnitude/a * Mathf.Sign(Vector3.Dot(va, va2));
			float a3 = va3.magnitude/a * Mathf.Sign(Vector3.Dot(va, va3));

			// find the uv corresponding to point f (uv1/uv2/uv3 are associated to p1/p2/p3):
			Vector2 uv = uv1 * a1 + uv2 * a2 + uv3 * a3;

			return uv;
		}

        // Used for testing if a polygon is within a polyhedron. The epsilon usage here gives the polygons a generous skin
        // so if the polygon is on the edge of where there'd be ambiguity the polyhedron spreads out slightly to encompass it
        internal static bool PolyhedronContainsPointEpsilon2(List<Polygon> polygons, Vector3 point)
        {
            for (int i = 0; i < polygons.Count; i++)
            {
                Plane plane = polygons[i].Plane;
                // Use positive epsilon
                float distance = Vector3.Dot(plane.normal, point) + plane.distance;
                if (distance > EPSILON_LOWER)
                {
                    return false;
                }
            }
            return true;
        }

        internal static bool PolyhedronContainsPointEpsilon3(List<Polygon> polygons, Vector3 point)
        {
            for (int i = 0; i < polygons.Count; i++)
            {
                Plane plane = polygons[i].Plane;
                // Use positive epsilon
                float distance = Vector3.Dot(plane.normal, point) + plane.distance;
                if (distance > EPSILON_LOWER_2)
                {
                    return false;
                }
            }
            return true;
        }

        internal static bool PolyhedronContainsPoint(List<Polygon> polygons, Vector3 point)//, bool thickPlanes)
        {
            for (int i = 0; i < polygons.Count; i++)
            {
                Plane plane = polygons[i].Plane;
                // No epsilon here, maybe needs a -EPSILON?
                //				if(Vector3.Dot (plane.normal, point) + plane.distance >= 0f)
                if (Vector3.Dot(plane.normal, point) + plane.distance > 0)
                {
                    return false;
                }
            }
            return true;
        }

        internal static bool PolyhedronContainsPolyhedron(Polygon[] polygons1, List<Polygon> polygons2)//, bool thickPlanes)
        {
            for (int i2 = 0; i2 < polygons2.Count; i2++)
            {
                for (int v = 0; v < polygons2[i2].Vertices.Length; v++)
                {
                    Vector3 point = polygons2[i2].Vertices[v].Position;

                    for (int i1 = 0; i1 < polygons1.Length; i1++)
                    {
                        Plane plane = polygons1[i1].Plane;
                        // No epsilon here, maybe needs a -EPSILON?
                        //				if(Vector3.Dot (plane.normal, point) + plane.distance >= 0f)
                        float distance = Vector3.Dot(plane.normal, point) + plane.distance;
                        if (distance > 0.003f) // EPSILON_LOWER_2)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        internal static bool PolyhedronContainsPolyhedron(Polygon[] polygons1, Polygon[] polygons2)//, bool thickPlanes)
        {
            for (int i2 = 0; i2 < polygons2.Length; i2++)
            {
                for (int v = 0; v < polygons2[i2].Vertices.Length; v++)
                {
                    Vector3 point = polygons2[i2].Vertices[v].Position;

                    for (int i1 = 0; i1 < polygons1.Length; i1++)
                    {
                        Plane plane = polygons1[i1].Plane;
                        // No epsilon here, maybe needs a -EPSILON?
                        //				if(Vector3.Dot (plane.normal, point) + plane.distance >= 0f)
                        float distance = Vector3.Dot(plane.normal, point) + plane.distance;
                        if (distance >= 0)// 0.003f) // EPSILON_LOWER_2)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        internal static bool PolyhedronContainsPoint(Polygon[] polygons, Vector3 point)//, bool thickPlanes)
        {
            for (int i = 0; i < polygons.Length; i++)
            {
                Plane plane = polygons[i].Plane;
                // No epsilon here
                //				if(Vector3.Dot (plane.normal, point) + plane.distance >= 0f)
                float dist = Vector3.Dot(plane.normal, point) + plane.distance;
                if (dist > EPSILON)
                {
                    return false;
                }
            }
            return true;
        }

        internal static float PolyhedronContainsPointDistance(Polygon[] polygons, Vector3 point)//, bool thickPlanes)
        {
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < polygons.Length; i++)
            {
                Plane plane = polygons[i].Plane;
                // No epsilon here
                //				if(Vector3.Dot (plane.normal, point) + plane.distance >= 0f)
                float distance = -(Vector3.Dot(plane.normal, point) + plane.distance);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                }
                if (bestDistance < -EPSILON)
                {
                    return -1;
                }
            }
            return bestDistance;
        }

        internal static bool PolyhedronContainsPointEpsilon1(Polygon[] polygons, Vector3 point)
        {
            for (int i = 0; i < polygons.Length; i++)
            {
                Plane plane = polygons[i].Plane;
                // Use negative epsilon
                if (Vector3.Dot(plane.normal, point) + plane.distance >= -EPSILON)
                {
                    return false;
                }
            }
            return true;
        }

        internal static bool PolyhedronContainsPointEpsilon1(List<Polygon> polygons, Vector3 point)
        {
            for (int i = 0; i < polygons.Count; i++)
            {
                Plane plane = polygons[i].Plane;
                // Use negative epsilon
                if (Vector3.Dot(plane.normal, point) + plane.distance >= -EPSILON)
                {
                    return false;
                }
            }
            return true;
        }

        internal static bool PolyhedronIntersectsLine(Polygon[] polygons, Vector3 lineStart, Vector3 lineEnd)
        {
            // Use two time values to represent the reduced line segment, at the start 0 represents lineStart and 1
            // represents lineEnd
            float timeStart = 0;
            float timeEnd = 1f;

            Vector3 lineDelta = lineEnd - lineStart;

            for (int i = 0; i < polygons.Length; i++)
            {
                Plane plane = polygons[i].Plane;

                // Find the relation between the line and this polygon
                // Essentially we test the direction the line is moving in against the polygon's normal to see whether
                // the line is enterring or exiting through the polygon.

                float dot = Vector3.Dot(plane.normal, lineDelta);

                // Find the intersection time between the plane and the position
                // Note this essentially substitutes the line segment equation into the plane equation then makes t the
                // subject

                // Not sure why we have to flip time intersection's sign, probably to do with the pecularity of the way
                // Unity's Plane works
                float timeIntersection = -(Vector3.Dot(plane.normal, lineStart) + plane.distance) / dot;

                if (dot < 0) // Directions are opposing, must be enterring through the polygon
                {
                    if (timeIntersection > timeStart)
                    {
                        timeStart = timeIntersection;
                    }
                }
                else if (dot > 0) // Directions are similar, must be exiting through the polygon
                {
                    if (timeIntersection < timeEnd)
                    {
                        timeEnd = timeIntersection;
                    }
                }
                else // (dot == 0) directions are perpendicular, line is tangential to the polygon
                {
                    // Rather than just ignoring the perpendicular case, we instead test either of the points on the line
                    // against the plane, if it's outside the plane then the line is outside the polyhedron!
                    if (Polygon.ComparePointToPlane(lineStart, polygons[i].Plane) == Polygon.PointPlaneRelation.Behind)
                    {
                        return false;
                    }
                }

                // No intersection has occurred
                if (timeEnd - timeStart <= 0)
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool PolyhedraIntersect(Polygon[] polygons1, Polygon[] polygons2)
        {
            // TODO: Remember that Polyhedron to Polyhedron intersection can be sped up by first testing the two AABBs against each other, and even by refining the Polyhedra intersection tests to the area of the two AABB's that intersect!

            // 1: Intersect each edge of each polyhedron against the entirity of the other polyhedron (line segment to polyhedron test)
            // 1.1: Test edges in polyhedron 1 against the entirity of polyhedron 2

            for (int polygonIndex = 0; polygonIndex < polygons1.Length; polygonIndex++)
            {
                int vertexCount = polygons1[polygonIndex].Vertices.Length;
                for (int vertexIndex = 0; vertexIndex < vertexCount; vertexIndex++)
                {
                    Vertex vertexStart = polygons1[polygonIndex].Vertices[vertexIndex];
                    Vertex vertexEnd = polygons1[polygonIndex].Vertices[(vertexIndex + 1) % vertexCount];

                    if (PolyhedronIntersectsLine(polygons2, vertexStart.Position, vertexEnd.Position))
                    {
                        return true;
                    }
                }
            }

            // 1.2: Test edges in polyhedron 2 against the entirity of polyhedron 1
            for (int polygonIndex = 0; polygonIndex < polygons2.Length; polygonIndex++)
            {
                int vertexCount = polygons2[polygonIndex].Vertices.Length;
                for (int vertexIndex = 0; vertexIndex < vertexCount; vertexIndex++)
                {
                    Vertex vertexStart = polygons2[polygonIndex].Vertices[vertexIndex];
                    Vertex vertexEnd = polygons2[polygonIndex].Vertices[(vertexIndex + 1) % vertexCount];

                    if (PolyhedronIntersectsLine(polygons1, vertexStart.Position, vertexEnd.Position))
                    {
                        return true;
                    }
                }
            }

            // 2: Test each polygon's centroid against the other polyhedron (point in polyhedron test)
            // 2.1: Test each centroid in polyhedron 1 against the entirity of polyhedron 2
            for (int polygonIndex = 0; polygonIndex < polygons1.Length; polygonIndex++)
            {
                Vector3 centroid = polygons1[polygonIndex].GetCenterPoint();
                // TODO: Should we use epsilon here?
                if (PolyhedronContainsPoint(polygons2, centroid))
                {
                    return true;
                }
            }

            // 2.2: Test each centroid in polyhedron 2 against the entirity of polyhedron 1
            for (int polygonIndex = 0; polygonIndex < polygons2.Length; polygonIndex++)
            {
                Vector3 centroid = polygons2[polygonIndex].GetCenterPoint();
                // TODO: Should we use epsilon here?
                if (PolyhedronContainsPoint(polygons1, centroid))
                {
                    return true;
                }
            }

            // TODO: I'm worried that there could be a case, for example
            // two cuboids that are coplanar except in two dimensions one is smaller than the other and is contained
            // There wouldn't necessarily be any edge intersections? Need to verify that step 2 doesn't have any 
            // epsilon issues and correctly determines the smaller cuboid to be contained by the first

            // 3: If none of these are found true, then can't be intersecting!
            return false;
        }

        internal static bool PolygonContainsPolygon(Polygon container, Polygon containee)
        {
            // TODO: Does this actually need to test with normal and flip, or can it just test flipped
            if (MathHelper.PlaneEqualsLooserWithFlip(container.Plane, containee.Plane))
            {
                Plane plane = container.Plane;
                for (int j = 0; j < container.Vertices.Length; j++)
                {
                    Vector3 point1 = container.Vertices[j].Position;
                    Vector3 point2 = container.Vertices[(j + 1) % container.Vertices.Length].Position;

                    Vector3 edge = point2 - point1; // Direction from a vertex to the next
                    Vector3 polygonNormal = plane.normal;

                    // Cross product of the edge with the polygon's normal gives the edge's normal
                    Vector3 edgeNormal = Vector3.Cross(edge, polygonNormal);

                    Vector3 edgeCenter = (point1 + point2) * 0.5f;

                    float polygonSkin = 0.03f;
                    if (polygonSkin != 0)
                    {
                        // Ouch, this is slow!
                        edgeCenter += edgeNormal.normalized * polygonSkin;
                    }

                    for (int i = 0; i < containee.Vertices.Length; i++)
                    {
                        Vector3 point = containee.Vertices[i].Position;
                        Vector3 pointToEdgeCentroid = edgeCenter - point;

                        // If the point is outside an edge this will return a negative value
                        if (Vector3.Dot(edgeNormal, pointToEdgeCentroid) < 0)
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
#endif