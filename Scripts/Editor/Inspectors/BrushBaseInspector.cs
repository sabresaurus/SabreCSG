﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
	[CanEditMultipleObjects]
	public class BrushBaseInspector : Editor
	{
		protected virtual void OnEnable () 
		{
			// Setup the SerializedProperties.
		}

		protected BrushBase BrushTarget
		{
			get
			{
				return (BrushBase)target;
			}
		}

		protected BrushBase[] BrushTargets
		{
			get
			{
				return System.Array.ConvertAll(targets, item => (BrushBase)item);
			}
		}

        /// <summary>
        /// Whether to show the group editor in the inspector.
        /// </summary>
        protected virtual bool ShowGroupInspector { get { return true; } }

        /// <summary>
        /// Implement this function to make a custom inspector.
        /// </summary>
        public virtual void DoInspectorGUI()
        {

        }

        public sealed override void OnInspectorGUI()
		{
            // group editing:
            if (ShowGroupInspector)
            {
                using (new NamedVerticalScope("Group"))
                {
                    GUILayout.BeginHorizontal();

                    // find whether we are currently inside of a group:
                    GroupBrush group = null;
                    if (BrushTarget.transform.parent)
                        group = BrushTarget.transform.parent.GetComponent<GroupBrush>();

                    // we are in a group:
                    if (group != null)
                    {
                        if (GUILayout.Button("Select Group"))
                        {
                            // select the group.
                            Selection.objects = new Object[] { BrushTarget.transform.parent.gameObject };
                        }
                    }

                    if (GUILayout.Button("Create Group"))
                    {
                        // create a group.
                        TransformHelper.GroupSelection();
                    }

                    GUILayout.EndHorizontal();
                }
            }

            // custom inspector:
            DoInspectorGUI();


            // generic brush editing:
            using (new NamedVerticalScope("Order"))
            {
                List<BrushBase> orderedTargets = BrushTargets.ToList();
                orderedTargets.RemoveAll(item => (item == null));
                orderedTargets.Sort((x, y) => x.transform.GetSiblingIndex().CompareTo(y.transform.GetSiblingIndex()));

                if (GUILayout.Button("Set As First"))
                {
                    for (int i = 0; i < orderedTargets.Count; i++)
                    {
                        // REVERSED
                        BrushBase thisBrush = orderedTargets[orderedTargets.Count - 1 - i];

                        Undo.SetTransformParent(thisBrush.transform, thisBrush.transform.parent, "Change Order");
                        thisBrush.transform.SetAsFirstSibling();
                    }

                    // Force all the brushes to recalculate their intersections and get ready for rebuilding
                    for (int i = 0; i < orderedTargets.Count; i++)
                    {
                        if (orderedTargets[i] is PrimitiveBrush)
                        {
                            ((PrimitiveBrush)orderedTargets[i]).RecalculateIntersections();
                            ((PrimitiveBrush)orderedTargets[i]).BrushCache.SetUnbuilt();
                        }
                    }
                }

                if (GUILayout.Button("Send Earlier"))
                {
                    for (int i = 0; i < orderedTargets.Count; i++)
                    {
                        BrushBase thisBrush = orderedTargets[i];

                        int siblingIndex = thisBrush.transform.GetSiblingIndex();
                        if (siblingIndex > 0)
                        {
                            Undo.SetTransformParent(thisBrush.transform, thisBrush.transform.parent, "Change Order");
                            siblingIndex--;
                            thisBrush.transform.SetSiblingIndex(siblingIndex);
                        }
                    }

                    // Force all the brushes to recalculate their intersections and get ready for rebuilding
                    for (int i = 0; i < orderedTargets.Count; i++)
                    {
                        if (orderedTargets[i] is PrimitiveBrush)
                        {
                            ((PrimitiveBrush)orderedTargets[i]).RecalculateIntersections();
                            ((PrimitiveBrush)orderedTargets[i]).BrushCache.SetUnbuilt();
                        }
                    }
                }

                if (GUILayout.Button("Send Later"))
                {
                    for (int i = 0; i < orderedTargets.Count; i++)
                    {
                        // REVERSED
                        BrushBase thisBrush = orderedTargets[orderedTargets.Count - 1 - i];

                        int siblingIndex = thisBrush.transform.GetSiblingIndex();
                        Undo.SetTransformParent(thisBrush.transform, thisBrush.transform.parent, "Change Order");
                        siblingIndex++;
                        thisBrush.transform.SetSiblingIndex(siblingIndex);
                    }

                    // Force all the brushes to recalculate their intersections and get ready for rebuilding
                    for (int i = 0; i < orderedTargets.Count; i++)
                    {
                        if (orderedTargets[i] is PrimitiveBrush)
                        {
                            ((PrimitiveBrush)orderedTargets[i]).RecalculateIntersections();
                            ((PrimitiveBrush)orderedTargets[i]).BrushCache.SetUnbuilt();
                        }
                    }
                }

                if (GUILayout.Button("Set As Last"))
                {
                    for (int i = 0; i < orderedTargets.Count; i++)
                    {
                        BrushBase thisBrush = orderedTargets[i];

                        Undo.SetTransformParent(thisBrush.transform, thisBrush.transform.parent, "Change Order");
                        thisBrush.transform.SetAsLastSibling();
                    }

                    // Force all the brushes to recalculate their intersections and get ready for rebuilding
                    for (int i = 0; i < orderedTargets.Count; i++)
                    {
                        if (orderedTargets[i] is PrimitiveBrush)
                        {
                            ((PrimitiveBrush)orderedTargets[i]).RecalculateIntersections();
                            ((PrimitiveBrush)orderedTargets[i]).BrushCache.SetUnbuilt();
                        }
                    }
                }

                if (serializedObject.targetObject != null)
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }
	}
}