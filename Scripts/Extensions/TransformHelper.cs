#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
	public static class TransformHelper
	{
		public static List<Transform> GetRootSelectionOnly(Transform[] sourceTransforms)
		{
			List<Transform> rootTransforms = new List<Transform>(sourceTransforms);

			for (int i = 0; i < rootTransforms.Count; i++) 
			{
				for (int j = 0; j < rootTransforms.Count; j++) 
				{
					if(rootTransforms[i] != rootTransforms[j])
					{
						if(rootTransforms[j].IsParentOf(rootTransforms[i]))
						{
							rootTransforms.RemoveAt(i);
							i--;
							break;
						}
					}
				}
			}

			return rootTransforms;
		}
			
		public static void GroupSelection()
		{
			if(Selection.activeTransform != null)
			{
				List<Transform> selectedTransforms = Selection.transforms.ToList();
				selectedTransforms.Sort((x,y) => x.GetSiblingIndex().CompareTo(y.GetSiblingIndex()));

				Transform rootTransform = Selection.activeTransform.parent;

				int earliestSiblingIndex = Selection.activeTransform.GetSiblingIndex();

				// Make sure we use the earliest sibling index for grouping, as they may select in reverse order up the hierarchy
				for (int i = 0; i < selectedTransforms.Count; i++) 
				{
					if(selectedTransforms[i].parent == rootTransform)
					{
						int siblingIndex = selectedTransforms[i].GetSiblingIndex();
						if(siblingIndex < earliestSiblingIndex)
						{
							earliestSiblingIndex = siblingIndex;
						}
					}
				}

				// Create group
				GameObject groupObject = new GameObject("Group");
				Undo.RegisterCreatedObjectUndo (groupObject, "Group");
				Undo.SetTransformParent(groupObject.transform, rootTransform, "Group");

				groupObject.transform.position = Selection.activeTransform.position;
				groupObject.transform.rotation = Selection.activeTransform.rotation;
				groupObject.transform.localScale = Selection.activeTransform.localScale;
				// Ensure correct sibling index

				groupObject.transform.SetSiblingIndex(earliestSiblingIndex);
				// Renachor
				for (int i = 0; i < selectedTransforms.Count; i++) 
				{
					Undo.SetTransformParent(selectedTransforms[i], groupObject.transform, "Group");
				}

				Selection.activeGameObject = groupObject;
				//						EditorApplication.RepaintHierarchyWindow();
				//						SceneView.RepaintAll();
			}
		}

		public static void UngroupSelection()
		{
			if(Selection.activeTransform != null && Selection.activeGameObject.GetComponents<MonoBehaviour>().Length == 0)
			{
				Transform rootTransform = Selection.activeTransform.parent;
				int siblingIndex = Selection.activeTransform.GetSiblingIndex();

				int childCount = Selection.activeTransform.childCount;
				UnityEngine.Object[] newSelection = new UnityEngine.Object[childCount];

				for (int i = 0; i < childCount; i++) 
				{
					Transform childTransform = Selection.activeTransform.GetChild(0);
					Undo.SetTransformParent(childTransform, rootTransform, "Ungroup");
					childTransform.SetSiblingIndex(siblingIndex+i);

					newSelection[i] = childTransform.gameObject;
				}
				Undo.DestroyObjectImmediate(Selection.activeGameObject);
				//				GameObject.DestroyImmediate(Selection.activeGameObject);
				Selection.objects = newSelection;
			}
		}

	}
}
#endif