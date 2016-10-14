#if UNITY_EDITOR || RUNTIME_CSG
using System.Collections.Generic;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
	public interface IDeepCopyable<T>
	{
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
