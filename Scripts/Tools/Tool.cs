#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace Sabresaurus.SabreCSG
{
    public abstract class Tool
    {
        protected CSGModel csgModel;

        // Used so we can reset some of the tool if the selected brush changes
		protected BrushBase primaryTargetBrushBase;
        protected PrimitiveBrush primaryTargetBrush;
		protected Transform primaryTargetBrushTransform;

		protected BrushBase[] targetBrushBases;
		protected PrimitiveBrush[] targetBrushes;
		protected Transform[] targetBrushTransforms;

		bool panInProgress = false;

		public CSGModel CSGModel {
			get {
				return csgModel;
			}
			set {
				csgModel = value;
			}
		}

		public void SetSelection(GameObject selectedGameObject, GameObject[] selectedGameObjects)
		{
			// Find the selected brush bases
			List<BrushBase> brushBases = new List<BrushBase>();

			for (int i = 0; i < Selection.gameObjects.Length; i++) 
			{
				BrushBase matchedBrushBase = Selection.gameObjects[i].GetComponent<BrushBase>();

				// If we've selected a brush base that isn't a prefab in the project
				if(matchedBrushBase != null
#if UNITY_2018_2_OR_NEWER
						&& !(PrefabUtility.GetCorrespondingObjectFromSource(matchedBrushBase.gameObject) == null
#else
						&& !(PrefabUtility.GetPrefabParent(matchedBrushBase.gameObject) == null 
#endif
#if !UNITY_2018_3_OR_NEWER
							&& PrefabUtility.GetPrefabObject(matchedBrushBase.transform) != null))
#else
						&& PrefabUtility.GetPrefabInstanceHandle(selectedGameObject.transform) != null))
#endif
				{
					brushBases.Add(matchedBrushBase);
				}
			}

			// Also find an array of brushes (brush bases that aren't primitive brushes will have a null entry)
			PrimitiveBrush[] primitiveBrushes = brushBases.Select(item => item.GetComponent<PrimitiveBrush>()).ToArray();

            // Pick out the first brush base and primitive brush (or null if none)
            BrushBase newPrimaryBrushBase = null;
            PrimitiveBrush newPrimaryBrush = null;

			// Make sure it's not null and that it isn't a prefab in the project
			if(selectedGameObject != null
#if UNITY_2018_2_OR_NEWER
				&& !(PrefabUtility.GetCorrespondingObjectFromSource(selectedGameObject) == null
#else
                && !(PrefabUtility.GetPrefabParent(selectedGameObject) == null
#endif
#if !UNITY_2018_3_OR_NEWER
						&& PrefabUtility.GetPrefabObject(selectedGameObject.transform) != null))
#else
						&& PrefabUtility.GetPrefabInstanceHandle(selectedGameObject.transform) != null))
#endif
			{
				newPrimaryBrushBase = selectedGameObject.GetComponent<BrushBase>();// brushBases.FirstOrDefault();
				newPrimaryBrush = selectedGameObject.GetComponent<PrimitiveBrush>();// primitiveBrushes.Where(item => item != null).FirstOrDefault();
			}

			// If the primary brush base has changed
			if (primaryTargetBrushBase != newPrimaryBrushBase
				|| (primaryTargetBrushBase == null && newPrimaryBrushBase != null)) // Special case for undo where references are equal but one is null
			{
				primaryTargetBrushBase = newPrimaryBrushBase;

				if(newPrimaryBrushBase != null)
				{
					primaryTargetBrushTransform = newPrimaryBrushBase.transform;
				}
				else
				{
					primaryTargetBrushTransform = null;
				}

				ResetTool();
			}

			BrushBase[] brushBasesArray = brushBases.ToArray();
			primaryTargetBrush = newPrimaryBrush;

			if(!targetBrushBases.ContentsEquals(brushBasesArray))
			{
				OnSelectionChanged();
				targetBrushBases = brushBasesArray;
				targetBrushes = primitiveBrushes;
				targetBrushTransforms = new Transform[brushBasesArray.Length];
				for (int i = 0; i < brushBasesArray.Length; i++) 
				{
					if(brushBasesArray[i] != null)
					{
						targetBrushTransforms[i] = brushBasesArray[i].transform;
					}
					else
					{
						targetBrushTransforms[i] = null;
					}
				}
			}
		}


		// Calculate the bounds for all selected brushes, respecting the current pivotRotation mode to produce 
		// bounds aligned to the first selected brush in Local mode, or bounds aligned to the absolute grid in Global
		// mode.
		public Bounds GetBounds()
		{
			Bounds bounds;

			if(Tools.pivotRotation == PivotRotation.Local)
			{
				bounds = primaryTargetBrushBase.GetBounds();

				for (int i = 0; i < targetBrushBases.Length; i++) 
				{
					if(targetBrushBases[i] != primaryTargetBrushBase)
					{
                        bounds.Encapsulate(targetBrushBases[i].GetBoundsLocalTo(primaryTargetBrushTransform));
                    }
				}
			}
			else // Absolute/Global
			{
				bounds = primaryTargetBrushBase.GetBoundsTransformed();
				for (int i = 0; i < targetBrushBases.Length; i++) 
				{
					if(targetBrushBases[i] != primaryTargetBrushBase)
					{
						bounds.Encapsulate(targetBrushBases[i].GetBoundsTransformed());
					}
				}
			}

			return bounds;
		}

		// Takes into account pivotRotation and the way Tool.GetBounds() works with absolute vs local modes
		public Vector3 TransformPoint(Vector3 point)
		{
			if(Tools.pivotRotation == PivotRotation.Local)
			{
				return primaryTargetBrushTransform.TransformPoint(point);	
			}
			else
			{
				return point;
			}
		}

		// Takes into account pivotRotation and the way Tool.GetBounds() works with absolute vs local modes
		public Vector3 InverseTransformDirection(Vector3 direction)
		{
			if(Tools.pivotRotation == PivotRotation.Local)
			{
				return primaryTargetBrushTransform.InverseTransformDirection(direction);	
			}
			else
			{
				return direction;
			}
		}

		// Takes into account pivotRotation and the way Tool.GetBounds() works with absolute vs local modes
		public Vector3 TransformDirection(Vector3 direction)
		{
			if(Tools.pivotRotation == PivotRotation.Local)
			{
				return primaryTargetBrushTransform.TransformDirection(direction);	
			}
			else
			{
				return direction;
			}
		}

		protected bool CameraPanInProgress
		{
			get
			{
				if(Tools.viewTool == ViewTool.Orbit)
				{
					return true;
				}
				else if(Tools.viewTool == ViewTool.FPS)
				{
					return true;
				}
				else if(Tools.viewTool == ViewTool.Zoom)
				{
					return true;
				}
				else if(Tools.viewTool == ViewTool.Pan)
				{
					// Unity will often report panning in progress even when it's not, so use a separate check
					return panInProgress;
				}
				else
				{
					return false;
				}
			}
		}

        public virtual void OnSceneGUI(SceneView sceneView, Event e)
		{
			if(e.type == EventType.MouseDown)
			{
				// You can use ctrl-alt and left drag on PC for pan. On OSX it's cmd-alt and left drag
				// Unfortunately even when you're not panning Unity will usually default to saying the viewTool is
				// still pan, so it's necessary to do a more substantial event driven check to see if panning is actually
				// in progress
#if UNITY_EDITOR_OSX
				if(e.command && e.alt)
				{
					panInProgress = true;
				}
#else
				if(e.control && e.alt)
				{
					panInProgress = true;
				}
#endif
			}
			else if(e.type == EventType.MouseUp)
			{
				panInProgress = false;
			}
		}

		// Called when the selected objects has changed
		public virtual void OnSelectionChanged() {}

		// Fired by the CSG Model on the active tool when Unity triggers Undo.undoRedoPerformed
		public virtual void OnUndoRedoPerformed() {}

		// Called when the selected brush changes
		public abstract void ResetTool();

		public abstract void Deactivated();

		public virtual bool BrushesHandleDrawing
		{
			get
			{
				return true;
			}
		}

		public virtual bool PreventBrushSelection
		{
			get
			{
				return false;
			}
		}

		public virtual bool PreventDragDrop
		{
			get
			{
				return true;
			}
		}
    }
}
#endif
