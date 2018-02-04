#if UNITY_EDITOR || RUNTIME_CSG
using System;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
    public class Edge
    {
        public const float EPSILON = 1e-5f;
		
		Vertex vertex1;
		Vertex vertex2;

		public Vertex Vertex1
        {
            get
            {
                return this.vertex1;
            }
        }

		public Vertex Vertex2
        {
            get
            {
                return this.vertex2;
            }
        }

		public Vector3 GetCenterPoint()
		{
			return (vertex1.Position + vertex2.Position) * 0.5f;
		}

        // TODO: Should track entire Vertex here? May be necessary for edge collapse where positions align but UV's don't. Currently unsure if that will be a problem.		
		public Edge(Vertex vertex1, Vertex vertex2)
        {
            this.vertex1 = vertex1;
            this.vertex2 = vertex2;
        }

        public bool Matches(Edge otherEdge)
        {
            if (vertex1.Position.EqualsWithEpsilon(otherEdge.vertex1.Position) 
				&& vertex2.Position.EqualsWithEpsilon(otherEdge.Vertex2.Position))
            {
                return true;
            } // Check if the edge is the other way around
            else if (vertex1.Position.EqualsWithEpsilon(otherEdge.vertex2.Position) 
				&& vertex2.Position.EqualsWithEpsilon(otherEdge.Vertex1.Position))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

		public bool Intersects(Edge otherEdge)
		{
			Vector3 vector1 = vertex2.Position - vertex1.Position;
			Vector3 vector2 = otherEdge.Vertex2.Position - otherEdge.Vertex1.Position;

			float dot = (Vector3.Dot(vector1.normalized, vector2.normalized));

			bool parallel =  Mathf.Abs(dot) > 1 - EPSILON;

            // Edges not parallel, they can't be collinear
            if (!parallel)
                return false;

			// TODO: Hacky way of finding out if they are on the same line because we know two points must be the same
			bool matchedPoint = false;

            Vector3 delta1 = Vector3.zero; // Delta from the matched point on this edge to the other point on this edge
            Vector3 delta2 = Vector3.zero; // Delta from the matched point on otherEdge to the other point on otherEdge

            if (vertex1.Position.EqualsWithEpsilon(otherEdge.Vertex1.Position))
			{
				matchedPoint = true;
                delta1 = vertex2.Position - vertex1.Position;
                delta2 = otherEdge.Vertex2.Position - otherEdge.Vertex1.Position;
            }
			else if (vertex2.Position.EqualsWithEpsilon(otherEdge.Vertex2.Position))
			{
				matchedPoint = true;
                delta1 = vertex1.Position - vertex2.Position;
                delta2 = otherEdge.Vertex1.Position - otherEdge.Vertex2.Position;
            }
			else if (vertex1.Position.EqualsWithEpsilon(otherEdge.Vertex2.Position))
			{
				matchedPoint = true;
                delta1 = vertex2.Position - vertex1.Position;
                delta2 = otherEdge.Vertex1.Position - otherEdge.Vertex2.Position;
            }
			else if (vertex2.Position.EqualsWithEpsilon(otherEdge.Vertex1.Position))
			{
				matchedPoint = true;
                delta1 = vertex1.Position - vertex2.Position;
                delta2 = otherEdge.Vertex2.Position - otherEdge.Vertex1.Position;
            }

            // No points matched, assume not collinear
            if (!matchedPoint)
                return false;

            // If the two edges actually share a portion then the vectors on the edges from the matched points will be in opposite directions
            bool actuallySharePortion = (Vector3.Dot(delta1, delta2) > 0);

			return actuallySharePortion;
		}

        /// <summary>
        /// Is this edge collinear with the other edge?
        /// </summary>
        /// <param name="otherEdge">Other edge.</param>
        public bool Collinear(Edge otherEdge)
        {
            Vector3 vector1 = vertex2.Position - vertex1.Position;
            Vector3 vector2 = otherEdge.Vertex2.Position - otherEdge.Vertex1.Position;

            float dot = (Vector3.Dot(vector1.normalized, vector2.normalized));

            //bool parallel = Mathf.Abs(dot) > 1-Plane.EPSILON;
            bool parallel = dot > 1 - EPSILON;

            // TODO: Hacky way of finding out if they are on the same line because we know two points must be the same
            bool matchedPoint = false;

            if (vertex1.Position.EqualsWithEpsilon(otherEdge.Vertex1.Position))
            {
                matchedPoint = true;
            }
            else if (vertex2.Position.EqualsWithEpsilon(otherEdge.Vertex2.Position))
            {
                matchedPoint = true;
            }
            else if (vertex1.Position.EqualsWithEpsilon(otherEdge.Vertex2.Position))
            {
                matchedPoint = true;
            }
            else if (vertex2.Position.EqualsWithEpsilon(otherEdge.Vertex1.Position))
            {
                matchedPoint = true;
            }
            return parallel && matchedPoint;
        }

		public override string ToString ()
		{
			return string.Format (string.Format("[Edge] V1: {0} V2: {1}", vertex1.Position, vertex2.Position));
		}

        #region Static Methods

        /// <summary>
        /// Returns the normalized interpolant between point1 and point2 where the edge they represent intersects with the
        /// supplied plane.
        /// </summary>
        public static float IntersectsPlane(UnityEngine.Plane plane, Vector3 point1, Vector3 point2)
        {
			// TODO: The plane might need flipping here
            float interpolant = (-plane.normal.x * point1.x - plane.normal.y * point1.y - plane.normal.z * point1.z - plane.distance)
                / (-plane.normal.x * (point1.x - point2.x) - plane.normal.y * (point1.y - point2.y) - plane.normal.z * (point1.z - point2.z));

// DISABLED: Should find a way of making this work with the new Assert support in Unity
//			DebugHelper.Assert((interpolant >= 0 && interpolant <= 1), "Edge Interpolant outside (0,1) range");

            return interpolant;
        }
        #endregion
    }
}
#endif