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
    [ExecuteInEditMode]
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

        protected string previousHierarchyName = "";

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

        /// <summary>
        /// Gets the beautiful name of the brush used in auto-generation of the hierarchy name.
        /// </summary>
        /// <value>The beautiful name of the brush.</value>
        public virtual string BeautifulBrushName
        {
            get
            {
                return "AppliedBrush";
            }
        }

        /// <summary>
        /// Gets an auto-generated name for use in the hierarchy.
        /// </summary>
        /// <value>An auto-generated name for use in the hierarchy.</value>
        public string GeneratedHierarchyName
        {
            get
            {
                return BeautifulBrushName + " (" + GetBounds().ToGeneratedHierarchyString() + ")";
            }
        }

        /// <summary>
        /// Updates the auto-generated name of the brush in the hierarchy. This must be called when
        /// the bounds of a brush change without a call to <see cref="Invalidate"/>. The name of the
        /// brush is not updated when the user changed it to something else manually. The only
        /// exception to that is when the user resets the name to an empty string.
        /// </summary>
        public void UpdateGeneratedHierarchyName()
        {
            // this may happen after the brush is duplicated.
            if (previousHierarchyName == "" && GeneratedHierarchyName == transform.name)
                previousHierarchyName = transform.name;

            if (transform.name == previousHierarchyName || transform.name == "")
                transform.name = previousHierarchyName = GeneratedHierarchyName;
        }

        /// <summary>
        /// Gets a value indicating whether this brush supports CSG operations. Setting this to false
        /// will hide CSG brush related options in the editor.
        /// <para>For example a <see cref="GroupBrush"/> does not have any CSG operations.</para>
        /// </summary>
        /// <value><c>true</c> if this brush supports CSG operations; otherwise, <c>false</c>.</value>
        public virtual bool SupportsCsgOperations { get { return true; } }

        public virtual void Invalidate(bool polygonsChanged)
        {
            // when a modification to a brush occured we update the auto-generated name.
            if (polygonsChanged)
                UpdateGeneratedHierarchyName();
        }

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

        protected virtual void Awake()
        {
            // if the brush name is equal to the auto-generated one,
            // we store the name so we can check for manual user changes.
            if (previousHierarchyName == "" && GeneratedHierarchyName == transform.name)
                previousHierarchyName = transform.name;
        }

        protected virtual void Update() { }
    }
}

#endif