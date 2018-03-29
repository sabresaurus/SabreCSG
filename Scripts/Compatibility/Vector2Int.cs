#if (UNITY_EDITOR || RUNTIME_CSG) && !UNITY_2017_2_OR_NEWER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityEngine
{
    /// <summary>
    /// A complete reimplementation of Vector2Int for older Unity versions.
    /// </summary>
    [System.Serializable]
    public struct Vector2Int
    {
        [SerializeField]
        public int x;

        [SerializeField]
        public int y;

        private static readonly Vector2Int s_Zero = new Vector2Int(0, 0);

        private static readonly Vector2Int s_One = new Vector2Int(1, 1);

        private static readonly Vector2Int s_Up = new Vector2Int(0, 1);

        private static readonly Vector2Int s_Down = new Vector2Int(0, -1);

        private static readonly Vector2Int s_Left = new Vector2Int(-1, 0);

        private static readonly Vector2Int s_Right = new Vector2Int(1, 0);

        public int this[int index]
        {
            get
            {
                int result;
                if (index != 0)
                {
                    if (index != 1)
                    {
                        throw new IndexOutOfRangeException(string.Format("Invalid Vector2Int index addressed: {0}!", index));
                    }
                    result = this.y;
                }
                else
                {
                    result = this.x;
                }
                return result;
            }
            set
            {
                if (index != 0)
                {
                    if (index != 1)
                    {
                        throw new IndexOutOfRangeException(string.Format("Invalid Vector2Int index addressed: {0}!", index));
                    }
                    this.y = value;
                }
                else
                {
                    this.x = value;
                }
            }
        }

        public float magnitude
        {
            get
            {
                return Mathf.Sqrt((float)(this.x * this.x + this.y * this.y));
            }
        }

        public int sqrMagnitude
        {
            get
            {
                return this.x * this.x + this.y * this.y;
            }
        }

        public static Vector2Int zero
        {
            get
            {
                return Vector2Int.s_Zero;
            }
        }

        public static Vector2Int one
        {
            get
            {
                return Vector2Int.s_One;
            }
        }

        public static Vector2Int up
        {
            get
            {
                return Vector2Int.s_Up;
            }
        }

        public static Vector2Int down
        {
            get
            {
                return Vector2Int.s_Down;
            }
        }

        public static Vector2Int left
        {
            get
            {
                return Vector2Int.s_Left;
            }
        }

        public static Vector2Int right
        {
            get
            {
                return Vector2Int.s_Right;
            }
        }

        public Vector2Int(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public void Set(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public static float Distance(Vector2Int a, Vector2Int b)
        {
            return (a - b).magnitude;
        }

        public static Vector2Int Min(Vector2Int lhs, Vector2Int rhs)
        {
            return new Vector2Int(Mathf.Min(lhs.x, rhs.x), Mathf.Min(lhs.y, rhs.y));
        }

        public static Vector2Int Max(Vector2Int lhs, Vector2Int rhs)
        {
            return new Vector2Int(Mathf.Max(lhs.x, rhs.x), Mathf.Max(lhs.y, rhs.y));
        }

        public static Vector2Int Scale(Vector2Int a, Vector2Int b)
        {
            return new Vector2Int(a.x * b.x, a.y * b.y);
        }

        public void Scale(Vector2Int scale)
        {
            this.x *= scale.x;
            this.y *= scale.y;
        }

        public void Clamp(Vector2Int min, Vector2Int max)
        {
            this.x = Math.Max(min.x, this.x);
            this.x = Math.Min(max.x, this.x);
            this.y = Math.Max(min.y, this.y);
            this.y = Math.Min(max.y, this.y);
        }

        public static implicit operator Vector2(Vector2Int v)
        {
            return new Vector2((float)v.x, (float)v.y);
        }

        public static Vector2Int FloorToInt(Vector2 v)
        {
            return new Vector2Int(Mathf.FloorToInt(v.x), Mathf.FloorToInt(v.y));
        }

        public static Vector2Int CeilToInt(Vector2 v)
        {
            return new Vector2Int(Mathf.CeilToInt(v.x), Mathf.CeilToInt(v.y));
        }

        public static Vector2Int RoundToInt(Vector2 v)
        {
            return new Vector2Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y));
        }

        public static Vector2Int operator +(Vector2Int a, Vector2Int b)
        {
            return new Vector2Int(a.x + b.x, a.y + b.y);
        }

        public static Vector2Int operator -(Vector2Int a, Vector2Int b)
        {
            return new Vector2Int(a.x - b.x, a.y - b.y);
        }

        public static Vector2Int operator *(Vector2Int a, Vector2Int b)
        {
            return new Vector2Int(a.x * b.x, a.y * b.y);
        }

        public static Vector2Int operator *(Vector2Int a, int b)
        {
            return new Vector2Int(a.x * b, a.y * b);
        }

        public static bool operator ==(Vector2Int lhs, Vector2Int rhs)
        {
            return lhs.x == rhs.x && lhs.y == rhs.y;
        }

        public static bool operator !=(Vector2Int lhs, Vector2Int rhs)
        {
            return !(lhs == rhs);
        }

        public override bool Equals(object other)
        {
            bool result;
            if (!(other is Vector2Int))
            {
                result = false;
            }
            else
            {
                Vector2Int vector2Int = (Vector2Int)other;
                result = (this.x.Equals(vector2Int.x) && this.y.Equals(vector2Int.y));
            }
            return result;
        }

        public override int GetHashCode()
        {
            return this.x.GetHashCode() ^ this.y.GetHashCode() << 2;
        }

        public override string ToString()
        {
            return String.Format("({0}, {1})", new object[]
            {
                this.x,
                this.y
            });
        }
    }
}
#endif