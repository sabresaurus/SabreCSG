#if UNITY_EDITOR || RUNTIME_CSG
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;

namespace Sabresaurus.SabreCSG
{
	public enum CSGMode { Add, Subtract };
	public abstract class BrushBase : MonoBehaviour
	{
		[SerializeField]
		protected CSGMode mode = CSGMode.Add;

		[SerializeField,FormerlySerializedAs("isDetailBrush")]
		protected bool isNoCSG;

		[SerializeField]
		protected bool hasCollision = true;

		[SerializeField]
		protected bool isVisible = true;

		protected bool destroyed = false;

		public CSGMode Mode
		{
			get
			{
				return this.mode;
			}
			set
			{
				if (this.mode != value)
				{
					this.mode = value;

					Invalidate(true);
				}
			}
		}

		public bool Destroyed
		{
			get
			{
				return this.destroyed;
			}
		}

		public bool IsNoCSG
		{
			get
			{
				return isNoCSG;
			}
			set 
			{ 
				isNoCSG = value; 
			}
		}

		public bool IsVisible
		{
			get
			{
				return isVisible;
			}
			set
			{
				isVisible = value;
			}
		}

		public bool HasCollision
		{
			get
			{
				return hasCollision;
			}
			set
			{
				hasCollision = value;
			}
		}

		public virtual void Invalidate(bool polygonsChanged){}

		public abstract void UpdateVisibility();

		public abstract Bounds GetBounds();

		public abstract void SetBounds(Bounds newBounds);

		public abstract Bounds GetBoundsTransformed();

        public abstract Bounds GetBoundsLocalTo(Transform otherTransform);

        // Fired by the CSG Model on each brush it knows about when Unity triggers Undo.undoRedoPerformed
        public abstract void OnUndoRedoPerformed ();


		protected virtual void OnDestroy()
		{
			destroyed = true;
		}			
	}
}

#endif