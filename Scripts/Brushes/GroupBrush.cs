#if UNITY_EDITOR || RUNTIME_CSG
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

namespace Sabresaurus.SabreCSG
{
    /// <summary>
    /// Brush groups are created when the user wants to group together some brushes.
    /// </summary>
    [ExecuteInEditMode]
    public class GroupBrush : BrushBase
    {
        [SerializeField]
        protected Bounds localBounds = new Bounds(Vector3.zero, new Vector3(2, 2, 2));

        /// <summary>
        /// Gets a value indicating whether this brush supports CSG operations. Setting this to false
        /// will hide CSG brush related options in the editor.
        /// <para>For example a <see cref="GroupBrush"/> does not have any CSG operations.</para>
        /// </summary>
        /// <value><c>true</c> if this brush supports CSG operations; otherwise, <c>false</c>.</value>
        public override bool SupportsCsgOperations { get { return false; } }

        /// <summary>The last known extents of the compound brush to detect user resizing the bounds.</summary>
        private Vector3 m_LastKnownExtents;
        /// <summary>The last known position of the compound brush to prevent movement on resizing the bounds.</summary>
        private Vector3 m_LastKnownPosition;

        void Awake()
        {
            // get the last known extents and position (especially after scene changes).
            m_LastKnownExtents = localBounds.extents;
            m_LastKnownPosition = transform.localPosition;
        }

        public override Bounds GetBounds()
        {
            return localBounds;
        }

        public override Bounds GetBoundsLocalTo(Transform otherTransform)
        {
            Vector3[] points = new Vector3[8];

            // Calculate the positions of the bounds corners local to the other transform
            for (int i = 0; i < 8; i++)
            {
                Vector3 point = localBounds.center;
                Vector3 offset = localBounds.extents;

                if (i % 2 == 0)
                {
                    offset.x = -offset.x;
                }

                if (i % 4 < 2)
                {
                    offset.y = -offset.y;
                }

                if (i % 8 < 4)
                {
                    offset.z = -offset.z;
                }

                point += offset;

                // Transform to world space then to the other transform's local space
                point = otherTransform.InverseTransformPoint(transform.TransformPoint(point));
                points[i] = point;
            }

            // Construct the bounds, starting with the first bounds corner
            Bounds bounds = new Bounds(points[0], Vector3.zero);

            // Add the rest of the corners to the bounds
            for (int i = 1; i < 8; i++)
            {
                bounds.Encapsulate(points[i]);
            }

            return bounds;
        }

        public override Bounds GetBoundsTransformed()
        {
            Vector3[] points = new Vector3[8];

            // Calculate the world positions of the bounds corners
            for (int i = 0; i < 8; i++)
            {
                Vector3 point = localBounds.center;
                Vector3 offset = localBounds.extents;

                if (i % 2 == 0)
                {
                    offset.x = -offset.x;
                }

                if (i % 4 < 2)
                {
                    offset.y = -offset.y;
                }

                if (i % 8 < 4)
                {
                    offset.z = -offset.z;
                }

                point += offset;

                // Transform to world space
                point = transform.TransformPoint(point);
                points[i] = point;
            }

            // Construct the bounds, starting with the first bounds corner
            Bounds bounds = new Bounds(points[0], Vector3.zero);

            // Add the rest of the corners to the bounds
            for (int i = 1; i < 8; i++)
            {
                bounds.Encapsulate(points[i]);
            }

            return bounds;
        }

        public override void OnUndoRedoPerformed()
        {
        }

        public override void SetBounds(Bounds newBounds)
        {
            localBounds.center = Vector3.zero;
            localBounds.extents = newBounds.extents;

            transform.Translate(newBounds.center);
        }

        public override void UpdateVisibility()
        {
        }

        public override void Invalidate(bool polygonsChanged)
        {
            ////////////////////////////////////////////////////////////////////
            // a little hack to detect the user manually resizing the bounds. //
            // we use this to automatically add steps for barnaby.            //
            // it's probably good to build a more 'official' way to detect    //
            // user scaling events in compound brushes sometime.              //
            if (m_LastKnownExtents != localBounds.extents)                    //
            {                                                                 //
                // undo any position movement.                                //
                transform.localPosition = m_LastKnownPosition;                //
            }                                                                 //
            ////////////////////////////////////////////////////////////////////
            Bounds csgBounds = new Bounds();

            // nothing to do except copy csg information to our child brushes.
            foreach (Transform childTransform in transform)
            {
                BrushBase child = childTransform.GetComponent<BrushBase>();
                if (child == null) continue;

                // we do not override these properties in a group.
                // it wouldn't make much sense and break whatever the user grouped.
                //child.Mode = this.Mode;
                //child.IsNoCSG = this.IsNoCSG;
                //child.IsVisible = this.IsVisible;
                //child.HasCollision = this.HasCollision;
                child.Invalidate(polygonsChanged);
                csgBounds.Encapsulate(child.GetBoundsLocalTo(transform));
            }
            // apply the generated csg bounds.
            localBounds = csgBounds;
            m_LastKnownExtents = localBounds.extents;
            m_LastKnownPosition = transform.localPosition;
        }

        private void Update()
        {
            // encapsulate all of the child objects in our bounds.
            Bounds csgBounds = new Bounds();

            foreach (Transform childTransform in transform)
            {
                BrushBase child = childTransform.GetComponent<BrushBase>();
                if (child == null) continue;
                csgBounds.Encapsulate(child.GetBoundsLocalTo(transform));
            }
            // apply the generated csg bounds.
            localBounds = csgBounds;
        }
    }
}
#endif