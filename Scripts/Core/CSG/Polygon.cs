#if UNITY_EDITOR || RUNTIME_CSG

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
    /// <summary>
    /// A plane shape (two-dimensional) with 3 or more straight sides (see <see cref="Edge"/>) e.g.
    /// triangles, rectangles and pentagons.
    /// </summary>
    /// <seealso cref="Sabresaurus.SabreCSG.IDeepCopyable{Sabresaurus.SabreCSG.Polygon}"/>
    [Serializable]
    public class Polygon : IDeepCopyable<Polygon>
    {
        /// <summary>
        /// The vertices (see <see cref="Vertex"/>) that make up this polygonal shape.
        /// </summary>
        [SerializeField]
        private Vertex[] vertices;

        /// <summary>
        /// The Unity <see cref="UnityEngine.Material"/> applied to the surface of this polygon.
        /// </summary>
        [SerializeField]
        private Material material;

        /// <summary>
        /// Whether the user requested that this polygon be excluded from the final CSG build (i.e.
        /// not rendered, it does affect CSG operations).
        /// </summary>
        [SerializeField]
        private bool userExcludeFromFinal = false;

        /// <summary>
        /// When a polygon is split or cloned, this number is preserved inside of those new polygons
        /// so they can track where they originally came from.
        /// </summary>
        [SerializeField]
        private int uniqueIndex = -1;

        /// <summary>
        /// Whether this polygon is excluded from the final CSG build (i.e. not rendered, it does
        /// affect CSG operations).
        /// </summary>
        private bool excludeFromFinal = false;

        /// <summary>
        /// Used externally to speed up calculations by not calculating a plane for every operation
        /// (see <see cref="CalculatePlane"/>).
        /// <para>
        /// A plane that approximately resembles the polygon. Most useful for calculations involving
        /// the normal of the polygon.
        /// </para>
        /// </summary>
        private Plane? cachedPlane = null;

        /// <summary>
        /// Gets or sets the vertices (see <see cref="Vertex"/>) that make up this polygonal shape.
        /// <para>Setting this value will automatically call <see cref="CalculatePlane"/>.</para>
        /// </summary>
        /// <value>The vertices.</value>
        /// <exception cref="ArgumentNullException">Thrown when the value is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">A polygon must have at least 3 vertices.</exception>
        public Vertex[] Vertices
        {
            get
            {
                return vertices;
            }
            set
            {
#if SABRE_CSG_DEBUG
                if (value == null) throw new ArgumentNullException("Vertices");
                if (value.Length < 3) throw new ArgumentOutOfRangeException("A polygon must have at least 3 vertices.");
                // consideration: check array for null elements?
#endif
                vertices = value;

                // the vertices of the polygon have been modified, calculate a new plane accordingly.
                CalculatePlane();
            }
        }

        /// <summary>
        /// Gets or sets the Unity <see cref="UnityEngine.Material"/> applied to the surface of this polygon.
        /// </summary>
        public Material Material
        {
            get
            {
                return material;
            }
            set
            {
                material = value;
            }
        }

        /// <summary>
        /// Gets or sets whether the user requested that this polygon be excluded from the final CSG build (i.e.
        /// not rendered, it does affect CSG operations).
        /// </summary>
        public bool UserExcludeFromFinal
        {
            get
            {
                return userExcludeFromFinal;
            }
            set
            {
                userExcludeFromFinal = value;
            }
        }

        /// <summary>
        /// Gets or sets the unique index of the polygon. When a polygon is split or cloned, this
        /// number is preserved inside of those new polygons so they can track where they originally
        /// came from.
        /// </summary>
        public int UniqueIndex
        {
            get
            {
                return uniqueIndex;
            }
            set
            {
                uniqueIndex = value;
            }
        }

        /// <summary>
        /// Gets or sets a temporary value used to mark polygons that are only created during the
        /// build process. Usually you will want to keep this set to <c>false</c>.
        /// <para>
        /// For example if you subtract cube B from cube A, SabreCSG will split cube A into chunks by
        /// cube B. In order to maintain convex solid chunks additional polygons are created on those
        /// split planes so that we can still determine if a point is inside/outside the chunk (this
        /// property is not serialized, see <see cref="UserExcludeFromFinal"/>).
        /// </para>
        /// </summary>
        public bool ExcludeFromFinal
        {
            get
            {
                return excludeFromFinal;
            }
            set
            {
                excludeFromFinal = value;
            }
        }

        /// <summary>
        /// Gets a plane that approximately resembles the polygon. Most useful for calculations
        /// involving the normal of the polygon.
        /// <para>
        /// This plane is cached for performance reasons, you may have to call <see
        /// cref="CalculatePlane"/> if you modified the polygon.
        /// </para>
        /// </summary>
        /// <value>A plane that approximately resembles the polygon.</value>
        public Plane Plane
        {
            get
            {
                // if we never calculated a plane for this polygon before we do so now:
                if (!cachedPlane.HasValue)
                    CalculatePlane();

                // return the cached plane instead of calculating one every time.
                return cachedPlane.Value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Polygon"/> class.
        /// </summary>
        /// <param name="vertices">The vertices (see <see cref="Vertex"/>) that make up this polygonal shape.</param>
        /// <param name="material">The Unity <see cref="UnityEngine.Material"/> applied to the surface of this polygon.</param>
        /// <param name="isTemporary">If set to <c>true</c> excludes the polygon from the final CSG build, it's only temporarily created during the build process to determine whether a point is inside/outside of a convex chunk (usually you set this argument to <c>false</c>, also see <paramref name="userExcludeFromFinal"/>).</param>
        /// <param name="userExcludeFromFinal">If set to <c>true</c> the user requested that this polygon be excluded from the final CSG build (i.e. not rendered, it does affect CSG operations).</param>
        /// <param name="uniqueIndex">When a polygon is split or cloned, this number is preserved inside of those new polygons so they can track where they originally came from.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="vertices"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">A polygon must have at least 3 vertices.</exception>
        public Polygon(Vertex[] vertices, Material material, bool isTemporary, bool userExcludeFromFinal, int uniqueIndex = -1)
        {
#if SABRE_CSG_DEBUG
            if (vertices == null) throw new ArgumentNullException("vertices");
            if (vertices.Length < 3) throw new ArgumentOutOfRangeException("A polygon must have at least 3 vertices.");
            // consideration: check array for null elements?
#endif
            this.vertices = vertices;
            this.material = material;
            this.uniqueIndex = uniqueIndex;
            this.excludeFromFinal = isTemporary;
            this.userExcludeFromFinal = userExcludeFromFinal;

            // calculate the cached plane.
            CalculatePlane();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Polygon"/> class.
        /// </summary>
        /// <param name="vertices">The vertex positions of a <see cref="Vertex"/> that make up this polygonal shape. Normals and UVs will be zero (see <see cref="ResetVertexNormals"/> and <see cref="GenerateUvCoordinates"/> to generate them automatically).</param>
        /// <param name="material">The Unity <see cref="UnityEngine.Material"/> applied to the surface of this polygon.</param>
        /// <param name="isTemporary">If set to <c>true</c> excludes the polygon from the final CSG build, it's only temporarily created during the build process to determine whether a point is inside/outside of a convex chunk (usually you set this argument to <c>false</c>, also see <paramref name="userExcludeFromFinal"/>).</param>
        /// <param name="userExcludeFromFinal">If set to <c>true</c> the user requested that this polygon be excluded from the final CSG build (i.e. not rendered, it does affect CSG operations).</param>
        /// <param name="uniqueIndex">When a polygon is split or cloned, this number is preserved inside of those new polygons so they can track where they originally came from.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="vertices"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">A polygon must have at least 3 vertices.</exception>
		public Polygon(Vector3[] vertices, Material material, bool isTemporary, bool userExcludeFromFinal, int uniqueIndex = -1)
        {
#if SABRE_CSG_DEBUG
            if (vertices == null) throw new ArgumentNullException("vertices");
            if (vertices.Length < 3) throw new ArgumentOutOfRangeException("A polygon must have at least 3 vertices.");
            // consideration: check array for null elements?
#endif
            // create vertices from the vector3 array.
            this.vertices = new Vertex[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
                this.vertices[i] = new Vertex(vertices[i], Vector3.zero, Vector2.zero);

            this.material = material;
            this.uniqueIndex = uniqueIndex;
            this.excludeFromFinal = isTemporary;
            this.userExcludeFromFinal = userExcludeFromFinal;

            // calculate the cached plane.
            CalculatePlane();
        }

        /// <summary>
        /// Creates a deep copy of the <see cref="Polygon"/>. Returns a new instance of a <see
        /// cref="Polygon"/> with the same value as this instance.
        /// </summary>
        /// <returns>The newly created <see cref="Polygon"/> copy with the same values.</returns>
        public Polygon DeepCopy()
        {
            return new Polygon(vertices.DeepCopy(), material, excludeFromFinal, userExcludeFromFinal, uniqueIndex);
        }

        /// <summary>
        /// Calculates a plane that approximately resembles the polygon (see <see cref="Plane"/>).
        /// </summary>
        public void CalculatePlane()
        {
            cachedPlane = new Plane(vertices[0].Position, vertices[1].Position, vertices[2].Position);

            // hack: if the plane's normal is zero and there's more than 3 vertices,
            // try using alternative vertices to construct the plane.
            if (cachedPlane.Value.normal == Vector3.zero && vertices.Length > 3)
            {
                // we use the first two vertices.
                Vector3 pos1 = vertices[0].Position;
                Vector3 pos2 = vertices[1].Position;

                // iterate through the available vertices.
                for (int i = 3; i < vertices.Length; i++)
                {
                    // use this vertex to construct a new plane.
                    cachedPlane = new Plane(pos1, pos2, vertices[i].Position);
                    // stop once we found a valid normal.
                    if (cachedPlane.Value.normal != Vector3.zero)
                        return;
                }
#if SABRE_CSG_DEBUG
                // if the normal is still zero we have a problem.
                if (cachedPlane.Value.normal == Vector3.zero)
                    Debug.LogError("(SabreCSG) Polygon.CalculatePlane: Invalid Normal! Shouldn't be zero. Vertices:\n" + string.Join(", ", Array.ConvertAll(vertices, v => v.ToString())));
#endif
            }
        }

        /// <summary>
        /// Gets a copy of the polygon that faces in the opposite direction.
        /// </summary>
        /// <value>A copy of the polygon that faces in the opposite direction.</value>
        public Polygon Flipped
        {
            get
            {
                // create a copy of this polygon.
                Polygon polygon = DeepCopy();
                // flip the polygon face.
                polygon.Flip();
                // return the flipped copy.
                return polygon;
            }
        }

        /// <summary>
        /// Flips this polygon by reversing the winding order and normals of the vertices, as well as
        /// the <see cref="Plane"/> (see <see cref="Flipped"/> to get a non-destructive copy).
        /// </summary>
        public void Flip()
        {
            // reverse winding order of the vertices.
            Array.Reverse(vertices);

            // flip the normal of each vertex:
            for (int i = 0; i < vertices.Length; i++)
                vertices[i].Normal *= -1;

            // flip the cached plane.
#if UNITY_2017_1_OR_NEWER
            // unity 2017 introduces a built-in plane flipped property.
            cachedPlane = cachedPlane.Value.flipped;
#else
			cachedPlane = cachedPlane.Value.Flip();
#endif
        }

        /// <summary>
        /// Resets the vertex normals to the normal of the <see cref="Plane"/> so they all face outside.
        /// <para>You may have to call <see cref="CalculatePlane"/> first if you modified the polygon.</para>
        /// </summary>
        public void ResetVertexNormals()
        {
            // iterate through all vertices:
            for (int i = 0; i < vertices.Length; i++)
                // set the vertex normal to the plane's normal.
                vertices[i].Normal = Plane.normal;
        }

        /// <summary>
        /// Gets the bounds that encapsulate this polygon.
        /// </summary>
        /// <returns>The bounds that encapsulate this polygon.</returns>
        public Bounds GetBounds()
        {
            Bounds polygonBounds = new Bounds(vertices[0].Position, Vector3.zero);

            // grow the bounds to include all vertex positions.
            for (int j = 1; j < vertices.Length; j++)
                polygonBounds.Encapsulate(vertices[j].Position);

            return polygonBounds;
        }

        /// <summary>
        /// Gets the edges that represent the shape of this polygon.
        /// </summary>
        /// <returns>The edges that represent the shape of this polygon.</returns>
        public Edge[] GetEdges()
        {
            Edge[] edges = new Edge[vertices.Length];

            // iterate through all vertices:
            for (int vertexIndex1 = 0; vertexIndex1 < vertices.Length; vertexIndex1++)
            {
                // the last vertex will connect back to the first vertex to make a full circle.
                int vertexIndex2 = ((vertexIndex1 + 1) >= vertices.Length ? 0 : vertexIndex1 + 1);
                // create an edge between the current vertex and the next vertex.
                edges[vertexIndex1] = new Edge(vertices[vertexIndex1], vertices[vertexIndex2]);
            }

            return edges;
        }

        /// <summary>
        /// Gets the center point between the vertex positions of the polygon.
        /// </summary>
        /// <returns>The center point of the polygon.</returns>
        public Vector3 GetCenterPoint()
        {
            // start with the first vertex position.
            Vector3 center = vertices[0].Position;

            // add the position of all vertices to the total:
            for (int i = 1; i < vertices.Length; i++)
                center += vertices[i].Position;

            // return the average position.
            return center / vertices.Length;
        }

        /// <summary>
        /// Gets the center UV coordinates of the polygon.
        /// </summary>
        /// <returns>The center UV coordinates of the polygon</returns>
        public Vector3 GetCenterUV()
        {
            // start with the first uv position.
            Vector2 centerUV = vertices[0].UV;

            // add the uv coordinates of all vertices to the total:
            for (int i = 1; i < vertices.Length; i++)
                centerUV += vertices[i].UV;

            // normalize the average uv coordinates into 0-1 range.
            centerUV *= 1f / vertices.Length;
            return centerUV;
        }

        /// <summary>
        /// Gets the surface area of the polygon.
        /// </summary>
        /// <returns>The surface area of the polygon.</returns>
        public float GetArea()
        {
            // todo: document this calculation.
            Vector3 normal = Vector3.Normalize(Vector3.Cross(vertices[1].Position - vertices[0].Position, vertices[2].Position - vertices[0].Position));
            Quaternion cancellingRotation = Quaternion.Inverse(Quaternion.LookRotation(normal));

            float totalArea = 0;

            int j = vertices.Length - 1;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 positionI = cancellingRotation * vertices[i].Position;
                Vector3 positionJ = cancellingRotation * vertices[j].Position;
                totalArea += (positionJ.x + positionI.x) * (positionJ.y - positionI.y);

                j = i;
            }
            return -totalArea * 0.5f;
        }

        /// <summary>
        /// Sets the vertex color of all vertices that make up this polygon.
        /// </summary>
        /// <param name="newColor">The new vertex color to apply.</param>
        public void SetColor(Color32 newColor)
        {
            // iterate through all vertices and assign a new color:
            for (int i = 0; i < vertices.Length; i++)
                vertices[i].Color = newColor;
        }

        /// <summary>
        /// WARNING: This method is misleading and doesn't create proper tangents - Do Not Use!
        /// <para>
        /// This should probably use an algorithm like this:
        /// https://forum.unity.com/threads/how-to-calculate-mesh-tangents.38984/ !
        /// </para>
        /// </summary>
        /// <returns>Wrong Value.</returns>
        public Vector3 GetTangent()
        {
            return (vertices[1].Position - vertices[0].Position).normalized;
        }

        /// <summary>
        /// Loops through the vertices of the polygon and removes all vertices that share the same
        /// position so that only unique vertices remain. If less than 3 unique vertices remain this
        /// method has no effect and will return false.
        /// <para>
        /// Floating point inaccuracies are taken into account (see <see
        /// cref="Extensions.EqualsWithEpsilon(Vector3, Vector3)"/>).
        /// </para>
        /// </summary>
        /// <returns><c>true</c> if there were at least 3 vertices; otherwise, <c>false</c>.</returns>
        public bool TryRemoveExtraneousVertices()
        {
            // early out: there's only 3 vertices in the polygon.
            if (vertices.Length == 3) return true;

            // create a list of unique vertices with a large enough initial capacity.
            List<Vertex> uniqueVertices = new List<Vertex>(vertices.Length)
            {
                // add the first vertex immediately.
                vertices[0]
            };

            // iterate through all vertices:
            for (int i = 1; i < vertices.Length; i++)
            {
                bool alreadyContained = false;

                // iterate through all unique vertices we determined so far:
                for (int j = 0; j < uniqueVertices.Count; j++)
                {
                    // if this vertex position roughly matches a unique vertex, skip it.
                    if (vertices[i].Position.EqualsWithEpsilonLower(uniqueVertices[j].Position))
                    {
                        alreadyContained = true;
                        break;
                    }
                }

                // we found another unique vertex, add it to the collection.
                if (!alreadyContained)
                    uniqueVertices.Add(vertices[i]);
            }

            // we cannot have a polygon with less than 3 vertices.
            if (uniqueVertices.Count < 3)
                return false;

            // success, assign the new vertices.
            vertices = uniqueVertices.ToArray();
            CalculatePlane();
            return true;
        }

        /// <summary>
        /// Generates the UV coordinates for this polygon automatically. This works similarly to the
        /// "AutoUV" button in the surface editor. This method may throw warnings in the console if
        /// the normal of the polygon is zero.
        /// <para>You may have to call <see cref="CalculatePlane"/> first if you modified the polygon.</para>
        /// </summary>
        public void GenerateUvCoordinates()
        {
            // stolen code from the surface editor "AutoUV".
            Quaternion cancellingRotation = Quaternion.Inverse(Quaternion.LookRotation(-Plane.normal));
            // sets the uv at each point to the position on the plane.
            for (int i = 0; i < vertices.Length; i++)
                vertices[i].UV = (cancellingRotation * vertices[i].Position) * 0.5f;
        }

        #region Comparator Classes

        /// <summary>
        /// An implementation of <see cref="IEqualityComparer{T}"/> that checks whether two <see
        /// cref="Vector3"/> can be considered equal.
        /// <para>Floating point inaccuracies are taken into account (see <see cref="MathHelper.EPSILON_3"/>).</para>
        /// </summary>
        /// <seealso cref="System.Collections.Generic.IEqualityComparer{UnityEngine.Vector3}"/>
        public class Vector3ComparerEpsilon : IEqualityComparer<Vector3>
        {
            /// <summary>
            /// Checks whether two <see cref="Vector3"/> can be considered equal.
            /// </summary>
            /// <param name="a">The first <see cref="Vector3"/>.</param>
            /// <param name="b">The second <see cref="Vector3"/>.</param>
            /// <returns><c>true</c> if the two <see cref="Vector3"/> can be considered equal; otherwise, <c>false</c>.</returns>
            public bool Equals(Vector3 a, Vector3 b)
            {
                return Mathf.Abs(a.x - b.x) < MathHelper.EPSILON_3
                    && Mathf.Abs(a.y - b.y) < MathHelper.EPSILON_3
                    && Mathf.Abs(a.z - b.z) < MathHelper.EPSILON_3;
            }

            /// <summary>
            /// Returns a hash code for this instance.
            /// </summary>
            /// <param name="obj">The object to hash.</param>
            /// <returns>
            /// A hash code for this instance, suitable for use in hashing algorithms and data structures
            /// like a hash table.
            /// </returns>
            public int GetHashCode(Vector3 obj)
            {
                // The similarity or difference between two positions can only be calculated if both are supplied
                // when Distinct is called GetHashCode is used to determine which values are in collision first
                // therefore we return the same hash code for all values to ensure all comparisons must use
                // our Equals method to properly determine which values are actually considered equal
                return 1;
            }
        }

        /// <summary>
        /// An implementation of <see cref="IEqualityComparer{T}"/> that checks whether two <see
        /// cref="Vertex"/> can be considered equal by their position alone.
        /// <para>
        /// Floating point inaccuracies are taken into account (see <see
        /// cref="Extensions.EqualsWithEpsilon(UnityEngine.Vector3, UnityEngine.Vector3)"/>).
        /// </para>
        /// </summary>
        /// <seealso cref="System.Collections.Generic.IEqualityComparer{Sabresaurus.SabreCSG.Vertex}"/>
        public class VertexComparerEpsilon : IEqualityComparer<Vertex> // should be renamed to VertexPositionComparerEpsilon
        {
            /// <summary>
            /// Checks whether two <see cref="Vertex"/> can be considered equal by their position.
            /// </summary>
            /// <param name="a">The first <see cref="Vertex"/>.</param>
            /// <param name="b">The second <see cref="Vertex"/>.</param>
            /// <returns><c>true</c> if the two <see cref="Vertex"/> can be considered equal; otherwise, <c>false</c>.</returns>
            public bool Equals(Vertex a, Vertex b)
            {
                return a.Position.EqualsWithEpsilon(b.Position);
            }

            /// <summary>
            /// Returns a hash code for this instance.
            /// </summary>
            /// <param name="obj">The object to hash.</param>
            /// <returns>
            /// A hash code for this instance, suitable for use in hashing algorithms and data structures
            /// like a hash table.
            /// </returns>
            public int GetHashCode(Vertex obj)
            {
                // The similarity or difference between two positions can only be calculated if both are supplied
                // when Distinct is called GetHashCode is used to determine which values are in collision first
                // therefore we return the same hash code for all values to ensure all comparisons must use
                // our Equals method to properly determine which values are actually considered equal
                return 1;
            }
        }

        /// <summary>
        /// An implementation of <see cref="IEqualityComparer{T}"/> that checks whether two <see
        /// cref="Polygon"/> can be considered equal by their unique index.
        /// </summary>
        /// <seealso cref="System.Collections.Generic.IEqualityComparer{Sabresaurus.SabreCSG.Polygon}"/>
        public class PolygonUIDComparer : IEqualityComparer<Polygon>
        {
            /// <summary>
            /// Checks whether two <see cref="Polygon"/> have the same unique index.
            /// </summary>
            /// <param name="a">The first <see cref="Polygon"/>.</param>
            /// <param name="b">The second <see cref="Polygon"/>.</param>
            /// <returns><c>true</c> if the two <see cref="Polygon"/> have the same unique index; otherwise, <c>false</c>.</returns>
            public bool Equals(Polygon a, Polygon b)
            {
                return a.UniqueIndex == b.UniqueIndex;
            }

            /// <summary>
            /// Returns a hash code for this instance.
            /// </summary>
            /// <param name="obj">The object to hash.</param>
            /// <returns>
            /// A hash code for this instance, suitable for use in hashing algorithms and data structures
            /// like a hash table.
            /// </returns>
            public int GetHashCode(Polygon obj)
            {
                return base.GetHashCode();
            }
        }

        #endregion Comparator Classes

        #region Static Methods

        public enum PolygonPlaneRelation { InFront, Behind, Spanning, Coplanar };

        public static PolygonPlaneRelation TestPolygonAgainstPlane(Polygon polygon, UnityEngine.Plane testPlane)
        {
            if (polygon.Plane.normal == testPlane.normal && polygon.Plane.distance == testPlane.distance)
            {
                return PolygonPlaneRelation.Coplanar;
            }

            // Count the number of vertices in front and behind the clip plane
            int verticesInFront = 0;
            int verticesBehind = 0;

            for (int i = 0; i < polygon.Vertices.Length; i++)
            {
                float distance = testPlane.GetDistanceToPoint(polygon.Vertices[i].Position);
                if (distance < -MathHelper.EPSILON_3) // Is the point in front of the plane (with thickness)
                {
                    verticesInFront++;
                }
                else if (distance > MathHelper.EPSILON_3) // Is the point behind the plane (with thickness)
                {
                    verticesBehind++;
                }
            }

            if (verticesInFront > 0 && verticesBehind > 0) // If some are in front and some are behind, then the poly spans
            {
                return PolygonPlaneRelation.Spanning;
            }
            else if (verticesInFront > 0) // Only in front, so entire polygon is in front
            {
                return PolygonPlaneRelation.InFront;
            }
            else if (verticesBehind > 0)  // Only behind, so entire polygon is behind
            {
                return PolygonPlaneRelation.Behind;
            }
            else // No points in front or behind the plane, so assume coplanar
            {
                return PolygonPlaneRelation.Coplanar;
            }
        }

        public static bool SplitPolygon(Polygon polygon, out Polygon frontPolygon, out Polygon backPolygon, out Vertex newVertex1, out Vertex newVertex2, UnityEngine.Plane clipPlane)
        {
            newVertex1 = null;
            newVertex2 = null;

            List<Vertex> frontVertices = new List<Vertex>();
            List<Vertex> backVertices = new List<Vertex>();

            for (int i = 0; i < polygon.vertices.Length; i++)
            {
                int previousIndex = i - 1;
                if (previousIndex < 0)
                {
                    previousIndex = polygon.vertices.Length - 1;
                }

                Vertex currentVertex = polygon.vertices[i];
                Vertex previousVertex = polygon.vertices[previousIndex];

                PointPlaneRelation currentRelation = ComparePointToPlane(currentVertex.Position, clipPlane);
                PointPlaneRelation previousRelation = ComparePointToPlane(previousVertex.Position, clipPlane);

                if (previousRelation == PointPlaneRelation.InFront && currentRelation == PointPlaneRelation.InFront)
                {
                    // Front add current
                    frontVertices.Add(currentVertex);
                }
                else if (previousRelation == PointPlaneRelation.Behind && currentRelation == PointPlaneRelation.InFront)
                {
                    float interpolant = Edge.GetPlaneIntersectionInterpolant(clipPlane, previousVertex.Position, currentVertex.Position);
                    Vertex intersection = Vertex.Lerp(previousVertex, currentVertex, interpolant);

                    // Front add intersection, add current
                    frontVertices.Add(intersection);
                    frontVertices.Add(currentVertex);

                    // Back add intersection
                    backVertices.Add(intersection.DeepCopy());

                    newVertex2 = intersection;
                }
                else if (previousRelation == PointPlaneRelation.InFront && currentRelation == PointPlaneRelation.Behind)
                {
                    // Reverse order here so that clipping remains consistent for either CW or CCW testing
                    float interpolant = Edge.GetPlaneIntersectionInterpolant(clipPlane, currentVertex.Position, previousVertex.Position);
                    Vertex intersection = Vertex.Lerp(currentVertex, previousVertex, interpolant);

                    // Front add intersection
                    frontVertices.Add(intersection);

                    // Back add intersection, current
                    backVertices.Add(intersection.DeepCopy());
                    backVertices.Add(currentVertex);

                    newVertex1 = intersection;
                }
                else if (previousRelation == PointPlaneRelation.Behind && currentRelation == PointPlaneRelation.Behind)
                {
                    // Back add current
                    backVertices.Add(currentVertex);
                }
                else if (currentRelation == PointPlaneRelation.On)
                {
                    // Front add current
                    frontVertices.Add(currentVertex);

                    // Back add current
                    backVertices.Add(currentVertex.DeepCopy());

                    if (previousRelation == PointPlaneRelation.InFront)
                    {
                        newVertex1 = currentVertex;
                    }
                    else if (previousRelation == PointPlaneRelation.Behind)
                    {
                        newVertex2 = currentVertex;
                    }
                    else
                    {
                        //						throw new System.Exception("Unhandled polygon configuration");
                    }
                }
                else if (currentRelation == PointPlaneRelation.Behind)
                {
                    backVertices.Add(currentVertex);
                }
                else if (currentRelation == PointPlaneRelation.InFront)
                {
                    frontVertices.Add(currentVertex);
                }
                else
                {
                    throw new System.Exception("Unhandled polygon configuration");
                }
            }
            //			Debug.Log("done");

            frontPolygon = new Polygon(frontVertices.ToArray(), polygon.Material, polygon.ExcludeFromFinal, polygon.UserExcludeFromFinal, polygon.uniqueIndex);
            backPolygon = new Polygon(backVertices.ToArray(), polygon.Material, polygon.ExcludeFromFinal, polygon.UserExcludeFromFinal, polygon.uniqueIndex);

            // Because of some floating point issues and some edge cases relating to splitting the tip of a very thin
            // polygon we can't reliable test that the polygon intersects a plane and will produce two valid pieces
            // so after splitting we need to do an additional test to check that each polygon is valid. If it isn't
            // then we mark that polygon as null and return false to indicate the split wasn't entirely successful

            bool splitNecessary = true;

            if (frontPolygon.vertices.Length < 3 || frontPolygon.Plane.normal == Vector3.zero)
            {
                frontPolygon = null;
                splitNecessary = false;
            }

            if (backPolygon.vertices.Length < 3 || backPolygon.Plane.normal == Vector3.zero)
            {
                backPolygon = null;
                splitNecessary = false;
            }

            return splitNecessary;
        }

        public static bool PlanePolygonIntersection(Polygon polygon, out Vector3 position1, out Vector3 position2, UnityEngine.Plane testPlane)
        {
            position1 = Vector3.zero;
            position2 = Vector3.zero;

            bool position1Set = false;
            bool position2Set = false;

            for (int i = 0; i < polygon.vertices.Length; i++)
            {
                int previousIndex = i - 1;
                if (previousIndex < 0)
                {
                    previousIndex = polygon.vertices.Length - 1;
                }

                Vertex currentVertex = polygon.vertices[i];
                Vertex previousVertex = polygon.vertices[previousIndex];

                PointPlaneRelation currentRelation = ComparePointToPlane(currentVertex.Position, testPlane);
                PointPlaneRelation previousRelation = ComparePointToPlane(previousVertex.Position, testPlane);

                if (previousRelation == PointPlaneRelation.InFront && currentRelation == PointPlaneRelation.InFront)
                {
                }
                else if (previousRelation == PointPlaneRelation.Behind && currentRelation == PointPlaneRelation.InFront)
                {
                    float interpolant = Edge.GetPlaneIntersectionInterpolant(testPlane, previousVertex.Position, currentVertex.Position);
                    position2 = Vector3.Lerp(previousVertex.Position, currentVertex.Position, interpolant);
                    position2Set = true;
                }
                else if (previousRelation == PointPlaneRelation.InFront && currentRelation == PointPlaneRelation.Behind)
                {
                    // Reverse order here so that clipping remains consistent for either CW or CCW testing
                    float interpolant = Edge.GetPlaneIntersectionInterpolant(testPlane, currentVertex.Position, previousVertex.Position);
                    position1 = Vector3.Lerp(currentVertex.Position, previousVertex.Position, interpolant);
                    position1Set = true;
                }
                else if (previousRelation == PointPlaneRelation.Behind && currentRelation == PointPlaneRelation.Behind)
                {
                }
                else if (currentRelation == PointPlaneRelation.On)
                {
                    if (previousRelation == PointPlaneRelation.InFront)
                    {
                        position1 = currentVertex.Position;
                        position1Set = true;
                    }
                    else if (previousRelation == PointPlaneRelation.Behind)
                    {
                        position2 = currentVertex.Position;
                        position2Set = true;
                    }
                    else
                    {
                        //						throw new System.Exception("Unhandled polygon configuration");
                    }
                }
                else if (currentRelation == PointPlaneRelation.Behind)
                {
                }
                else if (currentRelation == PointPlaneRelation.InFront)
                {
                }
                else
                {
                }
            }

            return position1Set && position2Set;
        }

        public enum PointPlaneRelation { InFront, Behind, On };

        public static PointPlaneRelation ComparePointToPlane2(Vector3 point, Plane plane)
        {
            float distance = plane.GetDistanceToPoint(point);
            if (distance < -MathHelper.EPSILON_5)
            {
                return PointPlaneRelation.InFront;
            }
            else if (distance > MathHelper.EPSILON_5)
            {
                return PointPlaneRelation.Behind;
            }
            else
            {
                return PointPlaneRelation.On;
            }
        }

        public static PointPlaneRelation ComparePointToPlane(Vector3 point, Plane plane)
        {
            float distance = plane.GetDistanceToPoint(point);
            if (distance < -MathHelper.EPSILON_3)
            {
                return PointPlaneRelation.InFront;
            }
            else if (distance > MathHelper.EPSILON_3)
            {
                return PointPlaneRelation.Behind;
            }
            else
            {
                return PointPlaneRelation.On;
            }
        }

        public static bool ContainsEdge(Polygon polygon, Edge candidateEdge)
        {
            // Check if any of the edges in the polygon match the candidate edge (including reversed order)
            for (int i = 0; i < polygon.Vertices.Length; i++)
            {
                Vector3 position1 = polygon.Vertices[i].Position;
                Vector3 position2 = polygon.Vertices[(i + 1) % polygon.Vertices.Length].Position;

                if ((candidateEdge.Vertex1.Position == position1 && candidateEdge.Vertex2.Position == position2)
                   || (candidateEdge.Vertex2.Position == position1 && candidateEdge.Vertex1.Position == position2))
                {
                    return true;
                }
            }
            // None found that matched
            return false;
        }

        internal static bool FindEdge(Polygon polygon, Edge candidateEdge, out Edge foundEdge)
        {
            // Check if any of the edges in the polygon match the candidate edge (including reversed order)
            for (int i = 0; i < polygon.Vertices.Length; i++)
            {
                Vector3 position1 = polygon.Vertices[i].Position;
                Vector3 position2 = polygon.Vertices[(i + 1) % polygon.Vertices.Length].Position;

                if ((candidateEdge.Vertex1.Position == position1 && candidateEdge.Vertex2.Position == position2)
                   || (candidateEdge.Vertex2.Position == position1 && candidateEdge.Vertex1.Position == position2))
                {
                    foundEdge = new Edge(polygon.Vertices[i], polygon.Vertices[(i + 1) % polygon.Vertices.Length]);
                    return true;
                }
            }
            // None found that matched
            foundEdge = null;
            return false;
        }

        #endregion Static Methods

        #region Obsolete Methods

        // Loops through the vertices and removes any that share a position with any others so that
        // only uniquely positioned vertices remain
        [Obsolete("Please use the new TryRemoveExtraneousVertices method as it prevents generating a polygon with less than 3 vertices.", false)]
        public void RemoveExtraneousVertices()
        {
            List<Vertex> newVertices = new List<Vertex>();
            newVertices.Add(vertices[0]);

            for (int i = 1; i < vertices.Length; i++)
            {
                bool alreadyContained = false;

                for (int j = 0; j < newVertices.Count; j++)
                {
                    if (vertices[i].Position.EqualsWithEpsilonLower(newVertices[j].Position))
                    {
                        alreadyContained = true;
                        break;
                    }
                }

                if (!alreadyContained)
                {
                    newVertices.Add(vertices[i]);
                }
            }

            vertices = newVertices.ToArray();
            if (vertices.Length > 2)
            {
                CalculatePlane();
            }
        }

        [Obsolete("Please assign the Vertices property instead of calling this method.", false)]
        public void SetVertices(Vertex[] vertices)
        {
            this.vertices = vertices;
            CalculatePlane();
        }

        #endregion Obsolete Methods
    }
}

#endif