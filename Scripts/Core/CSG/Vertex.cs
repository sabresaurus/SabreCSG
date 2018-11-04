#if UNITY_EDITOR || RUNTIME_CSG

using System;
using System.Collections.Generic;
using UnityEngine;

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
        public Vector3 Position;

        /// <summary>
        /// The uv coordinates of the <see cref="Vertex"/>. Determines how a texture is mapped onto a
        /// <see cref="Polygon"/>.
        /// </summary>
        public Vector2 UV;

        /// <summary>
        /// The normal direction of the <see cref="Vertex"/>. Used in lighting calculations to
        /// determine the direction a <see cref="Polygon"/> is facing.
        /// </summary>
        public Vector3 Normal;

        /// <summary>
        /// The color of the <see cref="Vertex"/>. Shaders can use this color to tint the model.
        /// </summary>
        public Color32 Color = UnityEngine.Color.white;

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
            Position = position;
            UV = uv;
            Normal = normal;
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
            Position = position;
            UV = uv;
            Normal = normal;
            Color = color;
        }

        /// <summary>
        /// Flips the <see cref="Normal"/> direction.
        /// </summary>
        public void FlipNormal()
        {
            Normal = -Normal;
        }

        /// <summary>
        /// Linearly interpolates between two vertices.
        /// <para>
        /// Note that all of the vertex components (i.e. <see cref="Position"/>, <see cref="UV"/>,
        /// <see cref="Normal"/>, <see cref="Color"/>) will be lerped.
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
        /// <exception cref="System.ArgumentNullException">
        /// Thrown when <paramref name="a"/> or <paramref name="b"/> is null.
        /// </exception>
        public static Vertex Lerp(Vertex a, Vertex b, float t)
        {
#if SABRE_CSG_DEBUG
            if (a == null) throw new ArgumentNullException("a");
            if (b == null) throw new ArgumentNullException("b");
#endif
            return new Vertex()
            {
                Position = Vector3.Lerp(a.Position, b.Position, t),
                UV = Vector2.Lerp(a.UV, b.UV, t),
                Normal = Vector3.Lerp(a.Normal, b.Normal, t),
                Color = Color32.Lerp(a.Color, b.Color, t),
            };
        }

        /// <summary>
        /// Creates a deep copy of the <see cref="Vertex"/>. Returs a new instance of a <see
        /// cref="Vertex"/> with the same value as this instance.
        /// </summary>
        /// <returns>The newly created <see cref="Vertex"/> copy with the same values.</returns>
        public Vertex DeepCopy()
        {
            return new Vertex(Position, Normal, UV, Color);
        }

        /// <summary>
        /// Returns a <see cref="string"/> that represents this <see cref="Vertex"/>.
        /// </summary>
        /// <returns>A <see cref="string"/> that represents this <see cref="Vertex"/>.</returns>
        public override string ToString()
        {
            return string.Format(string.Format("[Vertex] Pos: {0},{1},{2}", Position.x, Position.y, Position.z));
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
                   EqualityComparer<Vector3>.Default.Equals(Position, other.Position) &&
                   EqualityComparer<Vector2>.Default.Equals(UV, other.UV) &&
                   EqualityComparer<Vector3>.Default.Equals(Normal, other.Normal) &&
                   EqualityComparer<Color32>.Default.Equals(Color, other.Color);
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
            return base.GetHashCode();
        }

        /// <summary>
        /// True if the specified vertices are equal; otherwise, false.
        /// </summary>
        /// <param name="lhs">The left hand side operator.</param>
        /// <param name="rhs">The right hand side operator.</param>
        /// <returns><c>true</c> if the specified vertices are equal; otherwise, <c>false</c>.</returns>
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

            return lhs.Position == rhs.Position && lhs.UV == rhs.UV && lhs.Normal == rhs.Normal && lhs.Color.Equals(rhs.Color);
        }

        /// <summary>
        /// True if the specified vertices not are equal; otherwise, false.
        /// </summary>
        /// <param name="lhs">The left hand side operator.</param>
        /// <param name="rhs">The right hand side operator.</param>
        /// <returns><c>true</c> if the specified vertices are not equal; otherwise, <c>false</c>.</returns>
        public static bool operator !=(Vertex lhs, Vertex rhs)
        {
            return !(lhs == rhs);
        }
    }
}

#endif