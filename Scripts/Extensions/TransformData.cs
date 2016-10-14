using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
	/// <summary>
	/// Container for information usually held by a Transform
	/// </summary>
	[Serializable]
	public class TransformData
	{
		public Vector3 LocalPosition = Vector3.zero;
		public Quaternion LocalRotation = Quaternion.identity;
		public Vector3 LocalScale = Vector3.one;
		public Transform Parent;
		public int SiblingIndex;

		public TransformData (Transform sourceTransform)
		{
			this.LocalPosition = sourceTransform.localPosition;
			this.LocalRotation = sourceTransform.localRotation;
			this.LocalScale = sourceTransform.localScale;
			this.Parent = sourceTransform.parent;
			this.SiblingIndex = sourceTransform.GetSiblingIndex();
		}

		public bool SetFromTransform(Transform sourceTransform, bool ignoreSiblingChange = false)
		{
			bool changed = false;

			if(this.LocalPosition != sourceTransform.localPosition)
			{
				this.LocalPosition = sourceTransform.localPosition;
				changed = true;
			}

			if(this.LocalRotation != sourceTransform.localRotation)
			{
				this.LocalRotation = sourceTransform.localRotation;
				changed = true;
			}

			if(this.LocalScale != sourceTransform.localScale)
			{
				this.LocalScale = sourceTransform.localScale;
				changed = true;
			}

			if(this.Parent != sourceTransform.parent)
			{
				this.Parent = sourceTransform.parent;
				changed = true;
			}

			if(this.SiblingIndex != sourceTransform.GetSiblingIndex() 
				&& !ignoreSiblingChange)
			{
				this.SiblingIndex = sourceTransform.GetSiblingIndex();
				changed = true;
			}

			return changed;
		}
	}
}