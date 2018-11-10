#if UNITY_EDITOR || RUNTIME_CSG
using System.Collections.Generic;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
    /// <summary>Supports deep copying, which creates a new instance of a class with the same value as an existing instance.</summary>
    /// <typeparam name="T">The class type.</typeparam>
    public interface IDeepCopyable<T>
    {
        /// <summary>
        /// Creates a deep copy of the <typeparamref name="T"/>. Returns a new instance of a
        /// <typeparamref name="T"/> with the same value as this instance.
        /// </summary>
        /// <returns>The newly created <typeparamref name="T"/> copy with the same values.</returns>
        T DeepCopy();
    }

    public static class DeepCopyableExtensions
	{
		public static T[] DeepCopy<T>(this T[] sourceArray) where T : IDeepCopyable<T>
		{
			T[] newArray = new T[sourceArray.Length];

			for (var i = 0; i < sourceArray.Length; i++)
				newArray[i] = sourceArray[i].DeepCopy();

			return newArray;
		}

		public static List<T> DeepCopy<T>(this List<T> sourceList) where T : IDeepCopyable<T>
		{
			List<T> newList = new List<T>(sourceList.Count);

			for (var i = 0; i < sourceList.Count; i++)
				newList.Add(sourceList[i].DeepCopy());

			return newList;
		}

		public static IEnumerable<T> Clone<T>(this IEnumerable<T> sourceItems) where T : IDeepCopyable<T>
		{
			foreach (var item in sourceItems)
				yield return item.DeepCopy();
		}
	}
}
#endif
