using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
    public static class Extensions
    {
        public static Vector3 Abs(this Vector3 a)
        {
            return new Vector3(Mathf.Abs(a.x), Mathf.Abs(a.y), Mathf.Abs(a.z));
        }

        public static Vector3 Multiply(this Vector3 a, Vector3 b)
        {
            return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
        }

        public static Vector3 Divide(this Vector3 a, Vector3 b)
        {
            return new Vector3(a.x / b.x, a.y / b.y, a.z / b.z);
        }

        public static Vector3 SetAxis(this Vector3 vector, int axis, float newValue)
        {
            if (axis < 0 || axis > 2)
            {
                throw new ArgumentOutOfRangeException("Axis must be 0, 1 or 2");
            }

            vector[axis] = newValue;
            return vector;
        }

        /// <summary>
        /// Counts the number of components that are not equal to zero
        /// </summary>
        /// <returns>Number of components that are not zero.</returns>
        /// <param name="vector">Vector.</param>
        public static int GetSetAxisCount(this Vector3 vector)
        {
            int count = 0;
            for (int i = 0; i < 3; i++)
            {
                if (Mathf.Abs(vector[i]) > 1e-3f)
                {
                    count++;
                }
            }
            return count++;
        }

        public static Vector2 Multiply(this Vector2 a, Vector2 b)
        {
            return new Vector2(a.x * b.x, a.y * b.y);
        }

        public static Vector2 Divide(this Vector2 a, Vector2 b)
        {
            return new Vector2(a.x / b.x, a.y / b.y);
        }

        public static bool HasComponent<T>(this MonoBehaviour behaviour) where T : Component
        {
            return (behaviour.GetComponent<T>() != null);
        }

        public static bool HasComponent<T>(this GameObject gameObject) where T : Component
        {
            return (gameObject.GetComponent<T>() != null);
        }

        public static T AddOrGetComponent<T>(this MonoBehaviour behaviour) where T : Component
        {
            T component = behaviour.GetComponent<T>();
            if (component != null)
            {
                return component;
            }
            else
            {
                return behaviour.gameObject.AddComponent<T>();
            }
        }

        public static T AddOrGetComponent<T>(this GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component != null)
            {
                return component;
            }
            else
            {
                return gameObject.AddComponent<T>();
            }
        }

        public static Vector2 Rotate(this Vector2 vector, float angle)
        {
            angle *= Mathf.Deg2Rad;
            return new Vector2(vector.x * Mathf.Cos(angle) - vector.y * Mathf.Sin(angle),
                                vector.x * Mathf.Sin(angle) + vector.y * Mathf.Cos(angle));
        }

        //		public static GameObject Duplicate(this GameObject sourceObject)
        //		{
        //			GameObject duplicate = GameObject.Instantiate(sourceObject) as GameObject;
        //			duplicate.transform.parent = sourceObject.transform.parent;
        //			duplicate.name = sourceObject.name;
        //			return duplicate;
        //		}

        public static float GetSmallestExtent(this Bounds bounds)
        {
            if (bounds.extents.x < bounds.extents.y && bounds.extents.x < bounds.extents.z)
            {
                return bounds.extents.x;
            }
            else if (bounds.extents.y < bounds.extents.x && bounds.extents.y < bounds.extents.z)
            {
                return bounds.extents.y;
            }
            else
            {
                return bounds.extents.z;
            }
        }

        public static float GetLargestExtent(this Bounds bounds)
        {
            if (bounds.extents.x > bounds.extents.y && bounds.extents.x > bounds.extents.z)
            {
                return bounds.extents.x;
            }
            else if (bounds.extents.y > bounds.extents.x && bounds.extents.y > bounds.extents.z)
            {
                return bounds.extents.y;
            }
            else
            {
                return bounds.extents.z;
            }
        }

        public static bool Equals(this Color32 color, Color32 other)
        {
            if (color.r == other.r && color.g == other.g && color.b == other.b && color.a == other.a)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool NotEquals(this Color32 color, Color32 other)
        {
            if (color.r != other.r || color.g != other.g || color.b != other.b || color.a != other.a)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static Transform AddChild(this Transform parentTransform, string name)
        {
            GameObject newObject = new GameObject(name);
            newObject.transform.parent = parentTransform;
            newObject.transform.localPosition = Vector3.zero;
            newObject.transform.localRotation = Quaternion.identity;
            newObject.transform.localScale = Vector3.one;
            return newObject.transform;
        }

        public static void DestroyChildrenImmediate(this Transform parentTransform)
        {
            int childCount = parentTransform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                var go = parentTransform.GetChild(0).gameObject;
#if UNITY_EDITOR && UNITY_2018_3_OR_NEWER
                if (UnityEditor.PrefabUtility.IsPartOfPrefabInstance(go))
                {
                    GameObject.DestroyImmediate(UnityEditor.PrefabUtility.GetCorrespondingObjectFromOriginalSource(go), true);
                }
                else
#endif
                {
                    GameObject.DestroyImmediate(go);
                }

                //			GameObject.DestroyImmediate(parentTransform.GetChild(i).gameObject);
            }
        }

        public static bool IsParentOf(this Transform thisTransform, Transform otherTransform)
        {
            Transform parentTransform = otherTransform.parent;

            // Walk up the other transform's parents until we match this transform or hit null
            while (parentTransform != null)
            {
                if (parentTransform == thisTransform)
                {
                    return true;
                }

                parentTransform = parentTransform.parent;
            }

            // Reached the top and didn't match. This transform is not a parent of the other transform
            return false;
        }

        public static void ForceRefreshSharedMesh(this MeshCollider meshCollider)
        {
            Mesh sharedMesh = meshCollider.sharedMesh;
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = sharedMesh;
        }

        public static void ForceRefreshSharedMesh(this MeshFilter meshFilter)
        {
            Mesh sharedMesh = meshFilter.sharedMesh;
            Vector3[] vertices = sharedMesh.vertices;
            sharedMesh.vertices = vertices;
        }

        public static string ToStringLong(this Vector3 source)
        {
            return string.Format("{0},{1},{2}", source.x, source.y, source.z);
        }

        public static string ToStringLong(this Plane source)
        {
            return string.Format("{0}, {1}, {2} : {3}", source.normal.x, source.normal.y, source.normal.z, source.distance);
        }

        public static string ToStringWithSuffix(this int number, string suffixSingular, string suffixPlural)
        {
            if (number == 1)
            {
                return number + suffixSingular;
            }
            else
            {
                return number + suffixPlural;
            }
        }

        public static bool ContentsEquals<T>(this T[] array1, T[] array2)
        {
            // If array references are identical, it's the same object, must be equal
            if (ReferenceEquals(array1, array2))
            {
                return true;
            }

            // Null arrays will always be considered not equal, even if both are null
            if (array1 == null || array2 == null)
            {
                return false;
            }
            // If the arrays have different length's they're obviously not equal
            if (array1.Length != array2.Length)
            {
                return false;
            }

            // Walk through and compare each element in the two arrays, if any don't match, return false
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;

            for (int i = 0; i < array1.Length; i++)
            {
                if (!comparer.Equals(array1[i], array2[i]))
                {
                    return false;
                }
            }

            // Array contents are equal!
            return true;
        }

        public static bool ContentsEquals<T>(this List<T> list1, List<T> list2)
        {
            // If array references are identical, it's the same object, must be equal
            if (ReferenceEquals(list1, list2))
            {
                return true;
            }

            // Null arrays will always be considered not equal, even if both are null
            if (list1 == null || list2 == null)
            {
                return false;
            }
            // If the arrays have different length's they're obviously not equal
            if (list1.Count != list2.Count)
            {
                return false;
            }

            // Walk through and compare each element in the two arrays, if any don't match, return false
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;

            for (int i = 0; i < list1.Count; i++)
            {
                if (!comparer.Equals(list1[i], list2[i]))
                {
                    return false;
                }
            }

            // Array contents are equal!
            return true;
        }

        public static int[] GetTrianglesSafe(this Mesh mesh)
        {
            // Unfortunately in Unity 5.1 accessing .triangles on an empty mesh throws an error
            if (mesh.vertexCount == 0)
            {
                return new int[0];
            }
            else
            {
                return mesh.triangles;
            }
        }

#if !UNITY_2017_1_OR_NEWER
        // Before Unity 2017 there wasn't a built in Flip() method
        public static Plane Flip(this Plane sourcePlane)
        {
            sourcePlane.normal = -sourcePlane.normal;
            sourcePlane.distance = -sourcePlane.distance;
            return sourcePlane;
        }
#endif

        public static bool EqualsWithEpsilon(this float a, float b)
        {
            return Mathf.Abs(a - b) < MathHelper.EPSILON_5;
        }

        /// <summary>
        /// Determines whether two vector's are equal, allowing for floating point differences with an Epsilon value taken into account in per component comparisons
        /// </summary>
        public static bool EqualsWithEpsilon(this Vector3 a, Vector3 b)
        {
            return Mathf.Abs(a.x - b.x) < MathHelper.EPSILON_5 && Mathf.Abs(a.y - b.y) < MathHelper.EPSILON_5 && Mathf.Abs(a.z - b.z) < MathHelper.EPSILON_5;
        }

        public static bool EqualsWithEpsilonLower(this Vector3 a, Vector3 b)
        {
            return Mathf.Abs(a.x - b.x) < MathHelper.EPSILON_4 && Mathf.Abs(a.y - b.y) < MathHelper.EPSILON_4 && Mathf.Abs(a.z - b.z) < MathHelper.EPSILON_4;
        }

        public static bool EqualsWithEpsilonLower3(this Vector3 a, Vector3 b)
        {
            return Mathf.Abs(a.x - b.x) < MathHelper.EPSILON_2 && Mathf.Abs(a.y - b.y) < MathHelper.EPSILON_2 && Mathf.Abs(a.z - b.z) < MathHelper.EPSILON_2;
        }

        public static Rect ExpandFromCenter(this Rect rect, Vector2 expansion)
        {
            rect.size += expansion;
            rect.center -= expansion / 2f;
            return rect;
        }

        internal static bool Contains(this Bounds bounds1, Bounds bounds2)
        {
            if (bounds1.Contains(bounds2.min) && bounds1.Contains(bounds2.max))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        internal static bool IntersectsApproximate(this Bounds bounds1, Bounds bounds2)
        {
            //		return bounds1.min.x-EPSILON <= bounds2.max.x && bounds1.max.x+EPSILON >= bounds2.min.x && bounds1.min.y-EPSILON <= bounds2.max.y && bounds1.max.y+EPSILON >= bounds2.min.y && bounds1.min.z-EPSILON <= bounds2.max.z && bounds1.max.z+EPSILON >= bounds2.min.z;
            return bounds1.min.x - MathHelper.EPSILON_3 <= bounds2.max.x
                && bounds1.max.x + MathHelper.EPSILON_3 >= bounds2.min.x
                && bounds1.min.y - MathHelper.EPSILON_3 <= bounds2.max.y
                && bounds1.max.y + MathHelper.EPSILON_3 >= bounds2.min.y
                && bounds1.min.z - MathHelper.EPSILON_3 <= bounds2.max.z
                && bounds1.max.z + MathHelper.EPSILON_3 >= bounds2.min.z;
        }

        // If the second bounds has a coplanar side then it is considered not contained
        internal static bool ContainsWithin(this Bounds bounds1, Bounds bounds2)
        {
            return (bounds2.min.x > bounds1.min.x
                && bounds2.min.y > bounds1.min.y
                && bounds2.min.z > bounds1.min.z
                && bounds2.max.x < bounds1.max.x
                && bounds2.max.y < bounds1.max.y
                && bounds2.max.z < bounds1.max.z);
        }

        internal static bool ContainsApproximate(this Bounds bounds1, Vector3 point)
        {
            return (point.x > bounds1.min.x - MathHelper.EPSILON_3
                && point.y > bounds1.min.y - MathHelper.EPSILON_3
                && point.z > bounds1.min.z - MathHelper.EPSILON_3
                && point.x < bounds1.max.x + MathHelper.EPSILON_3
                && point.y < bounds1.max.y + MathHelper.EPSILON_3
                && point.z < bounds1.max.z + MathHelper.EPSILON_3);
        }

        /// <summary>
        /// Creates a beautiful string that matches the numbers shown in the resize tool.
        /// Used in the hierarchy.
        /// </summary>
        /// <param name="bounds">The bounds 'this' reference.</param>
        /// <returns>The beautiful string.</returns>
        internal static string ToGeneratedHierarchyString(this Bounds bounds)
        {
            string x = MathHelper.RoundFloat(bounds.size.x, 0.0001f).ToString(CultureInfo.InvariantCulture);
            string y = MathHelper.RoundFloat(bounds.size.y, 0.0001f).ToString(CultureInfo.InvariantCulture);
            string z = MathHelper.RoundFloat(bounds.size.z, 0.0001f).ToString(CultureInfo.InvariantCulture);
            return x + " x " + y + " x " + z;
        }

        /// <summary>
        /// Determines whether the layer mask contains the specified layer.
        /// </summary>
        /// <param name="layerMask">The layer mask.</param>
        /// <param name="layer">The layer to check for.</param>
        /// <returns><c>true</c> if the layer mask contains the specified layer; otherwise, <c>false</c>.</returns>
        internal static bool Contains(this LayerMask layerMask, int layer)
        {
            return layerMask == (layerMask | (1 << layer));
        }

        /// <summary>
        /// Clamps the R, G, B, A color channels to the 0 to 1 range.
        /// </summary>
        /// <param name="self">The 'this' reference for the extension method.</param>
        /// <returns>The resulting color.</returns>
        public static Color Clamp01(this Color self)
        {
            return new Color(Mathf.Clamp01(self.r), Mathf.Clamp01(self.g), Mathf.Clamp01(self.b), Mathf.Clamp01(self.a));
        }

        /// <summary>
        /// Looks at the color information in each channel and selects the base or blend
        /// color—whichever is darker—as the result color. Pixels lighter than the blend color are
        /// replaced, and pixels darker than the blend color do not change.
        /// </summary>
        /// <param name="self">The 'this' reference for the extension method.</param>
        /// <param name="blend">The blend color.</param>
        /// <returns>The resulting color.</returns>
        public static Color Darken(this Color self, Color blend)
        {
            return new Color(Mathf.Min(self.r, blend.r), Mathf.Min(self.g, blend.g), Mathf.Min(self.b, blend.b), self.a);
        }

        /// <summary>
        /// Looks at the color information in each channel and multiplies the base color by the blend
        /// color. The result color is always a darker color. Multiplying any color with black
        /// produces black. Multiplying any color with white leaves the color unchanged. When you’re
        /// painting with a color other than black or white, successive strokes with a painting tool
        /// produce progressively darker colors. The effect is similar to drawing on the image with
        /// multiple marking pens.
        /// </summary>
        /// <param name="self">The 'this' reference for the extension method.</param>
        /// <param name="blend">The blend color.</param>
        /// <returns>The resulting color.</returns>
        public static Color Multiply(this Color self, Color blend)
        {
            return new Color(self.r * blend.r, self.g * blend.g, self.b * blend.b, self.a);
        }

        /// <summary>
        /// Looks at the color information in each channel and darkens the base color to reflect the
        /// blend color by increasing the contrast between the two. Blending with white produces no change.
        /// </summary>
        /// <param name="self">The 'this' reference for the extension method.</param>
        /// <param name="blend">The blend color.</param>
        /// <returns>The resulting color.</returns>
        public static Color ColorBurn(this Color self, Color blend)
        {
            Color a = new Color(1 - self.r, 1 - self.g, 1 - self.b);
            Color b = new Color(a.r / blend.r, a.g / blend.g, a.b / blend.b);
            return new Color(1 - b.r, 1 - b.g, 1 - b.b, self.a);
        }

        /// <summary>
        /// Looks at the color information in each channel and darkens the base color to reflect the
        /// blend color by decreasing the brightness. Blending with white produces no change.
        /// </summary>
        /// <param name="self">The 'this' reference for the extension method.</param>
        /// <param name="blend">The blend color.</param>
        /// <returns>The resulting color.</returns>
        public static Color LinearBurn(this Color self, Color blend)
        {
            return new Color(self.r + blend.r - 1, self.g + blend.g - 1, self.b + blend.b - 1, self.a);
        }

        /// <summary>
        /// Looks at the color information in each channel and selects the base or blend
        /// color—whichever is lighter—as the result color. Pixels darker than the blend color are
        /// replaced, and pixels lighter than the blend color do not change.
        /// </summary>
        /// <param name="self">The 'this' reference for the extension method.</param>
        /// <param name="blend">The blend color.</param>
        /// <returns>The resulting color.</returns>
        public static Color Lighten(this Color self, Color blend)
        {
            return new Color(Mathf.Max(self.r, blend.r), Mathf.Max(self.g, blend.g), Mathf.Max(self.b, blend.b), self.a);
        }

        /// <summary>
        /// Looks at each channel’s color information and multiplies the inverse of the blend and
        /// base colors. The result color is always a lighter color. Screening with black leaves the
        /// color unchanged. Screening with white produces white. The effect is similar to projecting
        /// multiple photographic slides on top of each other.
        /// </summary>
        /// <param name="self">The 'this' reference for the extension method.</param>
        /// <param name="blend">The blend color.</param>
        /// <returns>The resulting color.</returns>
        public static Color Screen(this Color self, Color blend)
        {
            Color a = new Color(1 - self.r, 1 - self.g, 1 - self.b);
            Color b = new Color(1 - blend.r, 1 - blend.g, 1 - blend.b);
            Color c = new Color(a.r * b.r, a.g * b.g, a.b * a.b);
            return new Color(1 - c.r, 1 - c.g, 1 - c.b, self.a);
        }

        /// <summary>
        /// Looks at the color information in each channel and brightens the base color to reflect
        /// the blend color by decreasing contrast between the two. Blending with black produces no change.
        /// </summary>
        /// <param name="self">The 'this' reference for the extension method.</param>
        /// <param name="blend">The blend color.</param>
        /// <returns>The resulting color.</returns>
        public static Color ColorDodge(this Color self, Color blend)
        {
            Color a = new Color(1 - blend.r, 1 - blend.g, 1 - blend.b);
            return new Color(self.r / a.r, self.g / a.g, self.b / a.b, self.a);
        }

        /// <summary>
        /// Looks at the color information in each channel and brightens the base color to reflect
        /// the blend color by increasing the brightness. Blending with black produces no change.
        /// </summary>
        /// <param name="self">The 'this' reference for the extension method.</param>
        /// <param name="blend">The blend color.</param>
        /// <returns>The resulting color.</returns>
        public static Color LinearDodge(this Color self, Color blend)
        {
            return new Color(self.r + blend.r, self.g + blend.g, self.b + blend.b, self.a);
        }
    }
}