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
	public class WorldTransformData
	{
		public Vector3 Position = Vector3.zero;
		public Quaternion Rotation = Quaternion.identity;
		public Vector3 LossyScale = Vector3.one;
		public Transform Parent;
		public int SiblingIndex;

		public WorldTransformData (Transform sourceTransform)
		{
			this.Position = sourceTransform.position;
			this.Rotation = sourceTransform.rotation;
			this.LossyScale = sourceTransform.lossyScale;
			this.Parent = sourceTransform.parent;
			this.SiblingIndex = sourceTransform.GetSiblingIndex();
		}

		public bool SetFromTransform(Transform sourceTransform, bool ignoreSiblingChange = false)
		{
			bool changed = false;

			if(this.Position != sourceTransform.position)
			{
				this.Position = sourceTransform.position;
				changed = true;
			}

			if(this.Rotation != sourceTransform.rotation)
			{
				this.Rotation = sourceTransform.rotation;
				changed = true;
			}

			if(this.LossyScale != sourceTransform.lossyScale)
			{
				this.LossyScale = sourceTransform.lossyScale;
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