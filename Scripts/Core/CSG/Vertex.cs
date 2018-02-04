#if UNITY_EDITOR || RUNTIME_CSG

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Sabresaurus.SabreCSG
{
    /// <summary>
    /// A vertex (plural vertices) describes the position of a point in 3D space. With additional
    /// attributes for lighting, texturing and coloring.
    /// </summary>
    /// <seealso cref="Sabresaurus.SabreCSG.IDeepCopyable{Sabresaurus.SabreCSG.Vertex}"/>
    /// <seealso cref="System.IEquatable{Sabresaurus.SabreCSG.Vertex}"/>
    [Serializable]
    public class Vertex : IDeepCopyable<Vertex>, IEquatable<Vertex>
    {
        /// <summary>The position of the <see cref="Vertex"/>.</summary>
        [FormerlySerializedAs("Position")]
        public Vector3 position;

        /// <summary>
        /// The uv coordinates of the <see cref="Vertex"/>. Determines how a texture is mapped onto a
        /// <see cref="Polygon"/>.
        /// </summary>
        [FormerlySerializedAs("UV")]
        public Vector2 uv;

        /// <summary>
        /// The normal direction of the <see cref="Vertex"/>. Used in lighting calculations to
        /// determine the direction a <see cref="Polygon"/> is facing.
        /// </summary>
        [FormerlySerializedAs("Normal")]
        public Vector3 normal;

        /// <summary>
        /// The color of the <see cref="Vertex"/>. Shaders can use this color to tint the model.
        /// </summary>
        [FormerlySerializedAs("Color")]
        public Color32 color = UnityEngine.Color.white;

        /// <summary>
        /// Initializes a new instance of the <see cref="Vertex"/> class.
        /// </summary>
        public Vertex() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Vertex"/> class.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="normal">The normal.</param>
        /// <param name="uv">The uv coordinates.</param>
        public Vertex(Vector3 position, Vector3 normal, Vector2 uv)
        {
            this.position = position;
            this.uv = uv;
            this.normal = normal;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Vertex"/> class.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="normal">The normal.</param>
        /// <param name="uv">The uv coordinates.</param>
        /// <param name="color">The tinting color.</param>
        public Vertex(Vector3 position, Vector3 normal, Vector2 uv, Color32 color)
        {
            this.position = position;
            this.uv = uv;
            this.normal = normal;
            this.color = color;
        }

        /// <summary>
        /// Flips the <see cref="normal"/> direction.
        /// </summary>
        public void FlipNormal()
        {
            normal = -normal;
        }

        /// <summary>
        /// Linearly interpolates between two vertices.
        /// <para>
        /// Note that all of the vertex components (i.e. <see cref="position"/>, <see cref="uv"/>,
        /// <see cref="normal"/>, <see cref="color"/>) will be lerped.
        /// </para>
        /// </summary>
        /// <param name="a">The initial <see cref="Vertex"/>.</param>
        /// <param name="b">The final <see cref="Vertex"/>.</param>
        /// <param name="t">
        /// When <paramref name="t"/> = 0 returns <paramref name="a"/>. When <paramref name="t"/>
        /// = 1 returns <paramref name="b"/>. When <paramref name="t"/> = 0.5 returns the vertex
        /// midway between <paramref name="a"/> and <paramref name="b"/>.
        /// </param>
        /// <returns>The new interpolated <see cref="Vertex"/>.</returns>
        public static Vertex Lerp(Vertex a, Vertex b, float t)
        {
            return new Vertex()
            {
                position = Vector3.Lerp(a.position, b.position, t),
                uv = Vector2.Lerp(a.uv, b.uv, t),
                normal = Vector3.Lerp(a.normal, b.normal, t),
                color = Color32.Lerp(a.color, b.color, t),
            };
        }

        /// <summary>
        /// Creates a deep copy of the <see cref="Vertex"/>. Returs a new instance of a <see
        /// cref="Vertex"/> with the same value as this instance.
        /// </summary>
        /// <returns>The newly created <see cref="Vertex"/> copy with the same values.</returns>
        public Vertex DeepCopy()
        {
            return new Vertex(position, normal, uv, color);
        }

        /// <summary>
        /// Returns a <see cref="string"/> that represents this <see cref="Vertex"/>.
        /// </summary>
        /// <returns>A <see cref="string"/> that represents this <see cref="Vertex"/>.</returns>
        public override string ToString()
        {
            return string.Format(string.Format("[Vertex] Pos: {0},{1},{2}", position.x, position.y, position.z));
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/>, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance;
        /// otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as Vertex);
        }

        /// <summary>
        /// Determines whether the specified <see cref="Vertex"/>, is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="Vertex"/> to compare with this instance.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="Vertex"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(Vertex other)
        {
            return other != null &&
                   EqualityComparer<Vector3>.Default.Equals(position, other.position) &&
                   EqualityComparer<Vector2>.Default.Equals(uv, other.uv) &&
                   EqualityComparer<Vector3>.Default.Equals(normal, other.normal) &&
                   EqualityComparer<Color32>.Default.Equals(color, other.color);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures
        /// like a hash table.
        /// </returns>
        public override int GetHashCode()
        {
            var hashCode = -414430010;
            hashCode = hashCode * -1521134295 + EqualityComparer<Vector3>.Default.GetHashCode(position);
            hashCode = hashCode * -1521134295 + EqualityComparer<Vector2>.Default.GetHashCode(uv);
            hashCode = hashCode * -1521134295 + EqualityComparer<Vector3>.Default.GetHashCode(normal);
            hashCode = hashCode * -1521134295 + EqualityComparer<Color32>.Default.GetHashCode(color);
            return hashCode;
        }

        public static bool operator ==(Vertex lhs, Vertex rhs)
        {
            if (ReferenceEquals(lhs, rhs))
            {
                return true;
            }
            else if (ReferenceEquals(lhs, null) || ReferenceEquals(rhs, null))
            {
                return false;
            }

            return lhs.position == rhs.position && lhs.uv == rhs.uv && lhs.normal == rhs.normal && lhs.color.Equals(rhs.color);
        }

        public static bool operator !=(Vertex lhs, Vertex rhs)
        {
            return !(lhs == rhs);
        }
    }
}

#endif