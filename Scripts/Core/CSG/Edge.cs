#if UNITY_EDITOR || RUNTIME_CSG

using System;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
    /// <summary>
    /// An edge describes a connection between two vertices (see <see cref="Vertex"/>).
    /// </summary>
    public class Edge
    {
        /// <summary>
        /// The first <see cref="Vertex"/> of the <see cref="Edge"/>.
        /// </summary>
        private Vertex vertex1;

        /// <summary>
        /// The last <see cref="Vertex"/> of the <see cref="Edge"/>.
        /// </summary>
        private Vertex vertex2;

        /// <summary>
        /// The first <see cref="Vertex"/> of the <see cref="Edge"/>.
        /// </summary>
        /// <value>The first vertex of the edge.</value>
        public Vertex Vertex1
        {
            get
            {
                return this.vertex1;
            }
        }

        /// <summary>
        /// The last <see cref="Vertex"/> of the <see cref="Edge"/>.
        /// </summary>
        /// <value>The last vertex of the edge.</value>
        public Vertex Vertex2
        {
            get
            {
                return this.vertex2;
            }
        }

        /// <summary>
        /// Gets the center point between the vertex positions of the edge.
        /// </summary>
        /// <returns>The center point of the edge.</returns>
        public Vector3 CenterPoint
        {
            get
            {
                return (vertex1.Position + vertex2.Position) * 0.5f;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Edge"/> class.
        /// </summary>
        /// <param name="vertex1">The first vertex of the edge.</param>
        /// <param name="vertex2">The last vertex of the edge.</param>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown when <paramref name="vertex1"/> or <paramref name="vertex2"/> is null.
        /// </exception>
        public Edge(Vertex vertex1, Vertex vertex2)
        {
#if SABRE_CSG_DEBUG
            if (vertex1 == null) throw new ArgumentNullException("vertex1");
            if (vertex2 == null) throw new ArgumentNullException("vertex2");
#endif
            this.vertex1 = vertex1;
            this.vertex2 = vertex2;
        }

        /// <summary>
        /// Determines whether this edge matches the specified edge.
        /// <para>
        /// Floating point inaccuracies are taken into account (see <see
        /// cref="Extensions.EqualsWithEpsilon(Vector3, Vector3)"/>).
        /// </para>
        /// </summary>
        /// <param name="other">The other edge to compare this edge to.</param>
        /// <returns><c>true</c> if this edge matches the other edge; otherwise, <c>false</c>.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown when <paramref name="other"/> is null.
        /// </exception>
        public bool Matches(Edge other)
        {
#if SABRE_CSG_DEBUG
            if (other == null) throw new ArgumentNullException("other");
#endif
            // check whether we approximately match the other edge:
            if (vertex1.Position.EqualsWithEpsilon(other.vertex1.Position)
                && vertex2.Position.EqualsWithEpsilon(other.Vertex2.Position))
                return true;

            // even if the vertices are swapped it would yield the same edge:
            if (vertex1.Position.EqualsWithEpsilon(other.vertex2.Position)
                && vertex2.Position.EqualsWithEpsilon(other.Vertex1.Position))
                return true;

            // the edge can be considered different.
            return false;
        }

        /// <summary>
        /// Determines whether the other edge is parallel to this edge. Two edges are considered
        /// parallel if they face the same direction, do not intersect or touch each other at any point.
        /// <para>Floating point inaccuracies are taken into account (see <see cref="EPSILON"/>).</para>
        /// </summary>
        /// <param name="other">The other edge to check for parallelity.</param>
        /// <returns><c>true</c> if both edges are parallel; otherwise, <c>false</c>.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown when <paramref name="other"/> is null.
        /// </exception>
        public bool Parallel(Edge other)
        {
#if SABRE_CSG_DEBUG
            if (other == null) throw new ArgumentNullException("other");
#endif
            Vector3 direction1 = vertex2.Position - vertex1.Position;
            Vector3 direction2 = other.Vertex2.Position - other.Vertex1.Position;

            float dot = Vector3.Dot(direction1.normalized, direction2.normalized);

            return Mathf.Abs(dot) > 1 - MathHelper.EPSILON_5;
        }

        /// <summary>
        /// Does the other edge intersect with this edge?
        /// <para>
        /// Floating point inaccuracies are taken into account (see <see
        /// cref="Extensions.EqualsWithEpsilon(Vector3, Vector3)"/>).
        /// </para>
        /// </summary>
        /// <param name="other">The other edge to check for intersection.</param>
        /// <returns><c>true</c> if both edges intersect; otherwise, <c>false</c>.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown when <paramref name="other"/> is null.
        /// </exception>
        public bool Intersects(Edge other)
        {
#if SABRE_CSG_DEBUG
            if (other == null) throw new ArgumentNullException("other");
#endif
            // early out: if the edges aren't parallel to each other, they can't be collinear.
            if (!Parallel(other))
                return false;

            // delta from the matched point on this edge to the other point on this edge.
            Vector3 delta1 = Vector3.zero;
            // delta from the matched point on other edge to the other point on other edge.
            Vector3 delta2 = Vector3.zero;

            // TODO: hacky way of finding out if they are on the same line because we know
            // two points must be the same once CSG geometry has been built. This should
            // be able to detect any intersection of edges.
            bool matchedPoint = false;

            if (vertex1.Position.EqualsWithEpsilon(other.Vertex1.Position))
            {
                matchedPoint = true;
                delta1 = vertex2.Position - vertex1.Position;
                delta2 = other.Vertex2.Position - other.Vertex1.Position;
            }
            else if (vertex2.Position.EqualsWithEpsilon(other.Vertex2.Position))
            {
                matchedPoint = true;
                delta1 = vertex1.Position - vertex2.Position;
                delta2 = other.Vertex1.Position - other.Vertex2.Position;
            }
            else if (vertex1.Position.EqualsWithEpsilon(other.Vertex2.Position))
            {
                matchedPoint = true;
                delta1 = vertex2.Position - vertex1.Position;
                delta2 = other.Vertex1.Position - other.Vertex2.Position;
            }
            else if (vertex2.Position.EqualsWithEpsilon(other.Vertex1.Position))
            {
                matchedPoint = true;
                delta1 = vertex1.Position - vertex2.Position;
                delta2 = other.Vertex2.Position - other.Vertex1.Position;
            }

            // if no points matched, assume it's not collinear:
            if (!matchedPoint)
                return false;

            // if the two edges actually share a portion then the vectors on the edges
            // from the matched points will be in opposite directions.
            bool actuallySharePortion = (Vector3.Dot(delta1, delta2) > 0);

            return actuallySharePortion;
        }

        /// <summary>
        /// Is this edge collinear with the other edge? Two edges are considered collinear if they
        /// lie on a single straight line through space (gaps between them make no difference).
        /// </summary>
        /// <param name="other">Other edge that may be collinear.</param>
        /// <returns><c>true</c> if both edges are collinear; otherwise, <c>false</c>.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown when <paramref name="other"/> is null.
        /// </exception>
        public bool Collinear(Edge other)
        {
#if SABRE_CSG_DEBUG
            if (other == null) throw new ArgumentNullException("other");
#endif
            return EdgeUtility.EdgeMatches(this, other);
        }

        /// <summary>
        /// Gets the normalized interpolant between the two vertices of this edge where it intersects
        /// with the supplied <paramref name="plane"/>.
        /// </summary>
        /// <param name="plane">The plane that intersects with the edge.</param>
        /// <returns>The normalized interpolant between the edge points where the plane intersects.</returns>
        public float GetPlaneIntersectionInterpolant(Plane plane)
        {
            return GetPlaneIntersectionInterpolant(plane, vertex1.Position, vertex2.Position);
        }

        /// <summary>
        /// Returns a <see cref="string"/> that represents this <see cref="Edge"/>.
        /// </summary>
        /// <returns>A <see cref="string"/> that represents this <see cref="Edge"/>.</returns>
        public override string ToString()
        {
            return string.Format(string.Format("[Edge] V1: {0} V2: {1}", vertex1.Position, vertex2.Position));
        }

        /// <summary>
        /// Gets the normalized interpolant between <paramref name="point1"/> and <paramref name="point2"/> where the edge they
        /// represent intersects with the supplied <paramref name="plane"/>.
        /// </summary>
        /// <param name="plane">The plane that intersects with the edge.</param>
        /// <param name="point1">The first point of the edge.</param>
        /// <param name="point2">The last point of the edge.</param>
        /// <returns>The normalized interpolant between the edge points where the plane intersects.</returns>
        public static float GetPlaneIntersectionInterpolant(Plane plane, Vector3 point1, Vector3 point2)
        {
            float interpolant = (-plane.normal.x * point1.x - plane.normal.y * point1.y - plane.normal.z * point1.z - plane.distance)
                / (-plane.normal.x * (point1.x - point2.x) - plane.normal.y * (point1.y - point2.y) - plane.normal.z * (point1.z - point2.z));

            return interpolant;
        }
    }
}

#endif