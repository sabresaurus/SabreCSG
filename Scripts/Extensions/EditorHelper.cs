#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;

#if !(UNITY_5_0 || UNITY_5_1 || UNITY_5_2)
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

namespace Sabresaurus.SabreCSG
{
	public static class EditorHelper
	{
	    // Threshold for raycasting vertex clicks, in screen space (should match half the icon dimensions)
	    const float CLICK_THRESHOLD = 15;

	    // Used for offseting mouse position
	    const int TOOLBAR_HEIGHT = 37;

		public static bool HasDelegate (System.Delegate mainDelegate, System.Delegate targetListener)
		{
			if (mainDelegate != null)
			{
				if (mainDelegate.GetInvocationList().Contains(targetListener))
				{
					return true;
				}
				else
				{
					return false;
				}
			}
			else
			{
				return false;
			}
		}

	    public static bool SceneViewHasDelegate(SceneView.OnSceneFunc targetDelegate)
	    {
			return HasDelegate(SceneView.onSceneGUIDelegate, targetDelegate);
	    }

	    public enum SceneViewCamera { Top, Bottom, Left, Right, Front, Back, Other };

		public static SceneViewCamera GetSceneViewCamera(SceneView sceneView)
		{
			return GetSceneViewCamera(sceneView.camera);
		}
		public static SceneViewCamera GetSceneViewCamera(Camera camera)
	    {
	        Vector3 cameraForward = camera.transform.forward;

	        if (cameraForward == new Vector3(0, -1, 0))
	        {
	            return SceneViewCamera.Top;
	        }
	        else if (cameraForward == new Vector3(0, 1, 0))
	        {
	            return SceneViewCamera.Bottom;
	        }
	        else if (cameraForward == new Vector3(1, 0, 0))
	        {
	            return SceneViewCamera.Left;
	        }
	        else if (cameraForward == new Vector3(-1, 0, 0))
	        {
	            return SceneViewCamera.Right;
	        }
	        else if (cameraForward == new Vector3(0, 0, -1))
	        {
	            return SceneViewCamera.Front;
	        }
	        else if (cameraForward == new Vector3(0, 0, 1))
	        {
	            return SceneViewCamera.Back;
	        }
	        else
	        {
	            return SceneViewCamera.Other;
	        }
	    }

	    /// <summary>
	    /// Whether the mouse position is within the bounds of the axis snapping gizmo that appears in the top right or the bottom toolbar
	    /// </summary>
	    public static bool IsMousePositionInInvalidRects(Vector2 mousePosition)
	    {
			float scale = 1;

#if UNITY_5_4_OR_NEWER
			mousePosition = EditorGUIUtility.PointsToPixels(mousePosition);
			scale = EditorGUIUtility.pixelsPerPoint;
#endif

			if (mousePosition.x < Screen.width - 14 * scale 
				&& mousePosition.x > Screen.width - 89 * scale 
				&& mousePosition.y > 14 * scale 
				&& mousePosition.y < 105 * scale)
	        {
                // Mouse is near the scene alignment gizmo
	            return true;
	        }
            else if (mousePosition.y > Screen.height - Toolbar.BOTTOM_TOOLBAR_HEIGHT * scale - TOOLBAR_HEIGHT * scale)
            {
                // Mouse is over the bottom toolbar
                return true;
            }
            else
	        {
	            return false;
	        }
	    }

		public static Vector2 ConvertMousePointPosition(Vector2 sourceMousePosition, bool convertPointsToPixels = true)
	    {
#if UNITY_5_4_OR_NEWER
			if(convertPointsToPixels)
			{
				sourceMousePosition = EditorGUIUtility.PointsToPixels(sourceMousePosition);
                // Flip the direction of Y and remove the Scene View top toolbar's height
                sourceMousePosition.y = Screen.height - sourceMousePosition.y - (TOOLBAR_HEIGHT * EditorGUIUtility.pixelsPerPoint);
            }
            else
            {
                // Flip the direction of Y and remove the Scene View top toolbar's height
				float screenHeightPoints = (Screen.height / EditorGUIUtility.pixelsPerPoint);
				sourceMousePosition.y = screenHeightPoints - sourceMousePosition.y - (TOOLBAR_HEIGHT);
            }
			
#else
			// Flip the direction of Y and remove the Scene View top toolbar's height
			sourceMousePosition.y = Screen.height - sourceMousePosition.y - TOOLBAR_HEIGHT;
#endif
	        return sourceMousePosition;
	    }

		public static Vector2 ConvertMousePixelPosition(Vector2 sourceMousePosition, bool convertPixelsToPoints = true)
		{
            sourceMousePosition = MathHelper.RoundVector2(sourceMousePosition);
#if UNITY_5_4_OR_NEWER
			if(convertPixelsToPoints)
			{
				sourceMousePosition = EditorGUIUtility.PixelsToPoints(sourceMousePosition);
			}
			// Flip the direction of Y and remove the Scene View top toolbar's height
			sourceMousePosition.y = (Screen.height / EditorGUIUtility.pixelsPerPoint) - sourceMousePosition.y - (TOOLBAR_HEIGHT);
#else
			// Flip the direction of Y and remove the Scene View top toolbar's height
			sourceMousePosition.y = Screen.height - sourceMousePosition.y - TOOLBAR_HEIGHT;
#endif
			return sourceMousePosition;
		}

        public static float ConvertScreenPixelsToPoints(float screenPixels)
        {
#if UNITY_5_4_OR_NEWER
            return screenPixels / EditorGUIUtility.pixelsPerPoint;
#else
			// Pre 5.4 assume that 1 pixel = 1 point
			return screenPixels;
#endif
        }

        public static Vector2 ConvertScreenPixelsToPoints(Vector2 screenPixels)
        {
#if UNITY_5_4_OR_NEWER
            return EditorGUIUtility.PixelsToPoints(screenPixels);
#else
			// Pre 5.4 assume that 1 pixel = 1 point
			return screenPixels;
#endif
        }

        public static bool IsMousePositionInIMGUIRect(Vector2 mousePosition, Rect rect)
		{
			// This works in point space, not pixel space
			mousePosition += new Vector2(0, EditorStyles.toolbar.fixedHeight);

			return rect.Contains(mousePosition);
		}

        /// <summary>
        /// Determines whether a mouse position is within distance of a screen point. Care must be taken with the inputs provided to this method - see the parameter explanations for details.
        /// </summary>
        /// <param name="mousePosition">As provided by Event.mousePosition (in points)</param>
        /// <param name="targetScreenPosition">As provided by Camera.WorldToScreenPoint() (in pixels)</param>
        /// <param name="screenDistancePoints">Screen distance (in points)</param>
        /// <returns>True if within the specified distance, False otherwise</returns>
        public static bool InClickZone(Vector2 mousePosition, Vector2 targetScreenPosition, float screenDistancePoints)
        {
            // Convert the mouse position to screen space, but leave in points
            mousePosition = ConvertMousePointPosition(mousePosition, false);
            // Convert screen position from pixels to points
            targetScreenPosition = EditorHelper.ConvertScreenPixelsToPoints(targetScreenPosition);

            float distance = Vector2.Distance(mousePosition, targetScreenPosition);

            if (distance <= screenDistancePoints)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool InClickZone(Vector2 mousePosition, Vector3 worldPosition)
	    {
	        Vector3 targetScreenPosition = Camera.current.WorldToScreenPoint(worldPosition);

	        if (targetScreenPosition.z < 0)
	        {
	            return false;
	        }

			float depthDistance = targetScreenPosition.z;

			// When z is 6 then click threshold is 15
			// when z is 20 then click threshold is 5
			float threshold = Mathf.Lerp(15, 5, Mathf.InverseLerp(6, 20, depthDistance));

            return InClickZone(mousePosition, targetScreenPosition, threshold);
        }

		public static bool InClickRect(Vector2 mousePosition, Vector3 worldPosition1, Vector3 worldPosition2, float range)
		{
			mousePosition = ConvertMousePointPosition(mousePosition, false);
			Vector3 targetScreenPosition1 = Camera.current.WorldToScreenPoint(worldPosition1);
			Vector3 targetScreenPosition2 = Camera.current.WorldToScreenPoint(worldPosition2);

			// Convert screen position from pixels to points
			targetScreenPosition1 = EditorHelper.ConvertScreenPixelsToPoints(targetScreenPosition1);
			targetScreenPosition2 = EditorHelper.ConvertScreenPixelsToPoints(targetScreenPosition2);

			if (targetScreenPosition1.z < 0)
			{
				return false;
			}

			Vector3 closestPoint = MathHelper.ProjectPointOnLineSegment(targetScreenPosition1, targetScreenPosition2, mousePosition);
			closestPoint.z = 0;

			if(Vector3.Distance(closestPoint, mousePosition) < range)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

	    public static Vector3 CalculateWorldPoint(SceneView sceneView, Vector3 screenPoint)
	    {
	        screenPoint = ConvertMousePointPosition(screenPoint);

	        return sceneView.camera.ScreenToWorldPoint(screenPoint);
	    }

//		public static string GetCurrentSceneGUID()
//		{
//			string currentScenePath = EditorApplication.currentScene;
//			if(!string.IsNullOrEmpty(currentScenePath))
//			{
//				return AssetDatabase.AssetPathToGUID(currentScenePath);
//			}
//			else
//			{
//				// Scene hasn't been saved
//				return null;
//			}
//		}

		public static void SetDirty(Object targetObject)
		{
			if(!Application.isPlaying)
			{
				EditorUtility.SetDirty(targetObject);

#if UNITY_5_0 || UNITY_5_1 || UNITY_5_2
				// As of Unity 5, SetDirty no longer marks the scene as dirty. Need to use the new call for that.
				EditorApplication.MarkSceneDirty();
#else // 5.3 and above introduce multiple scene management via EditorSceneManager
				Scene activeScene = EditorSceneManager.GetActiveScene();
				EditorSceneManager.MarkSceneDirty(activeScene);
#endif
			}
		}

		public static void IsoAlignSceneView(Vector3 direction)
		{
			SceneView sceneView = SceneView.lastActiveSceneView;

			SceneView.lastActiveSceneView.LookAt(sceneView.pivot, Quaternion.LookRotation(direction));

			// Mark the camera as iso (orthographic)
			sceneView.orthographic = true;
		}

		public static void IsoAlignSceneViewToNearest()
		{
			SceneView sceneView = SceneView.lastActiveSceneView;
			Vector3 cameraForward = sceneView.camera.transform.forward;
			Vector3 newForward = Vector3.up;
			float bestDot = -1;

			Vector3 testDirection;
			float dot;
			// Find out of the six axis directions the closest direction to the camera
			for (int i = 0; i < 3; i++) 
			{
				testDirection = Vector3.zero;
				testDirection[i] = 1;
				dot = Vector3.Dot(testDirection, cameraForward);
				if(dot > bestDot)
				{
					bestDot = dot;
					newForward = testDirection;
				}

				testDirection[i] = -1;
				dot = Vector3.Dot(testDirection, cameraForward);
				if(dot > bestDot)
				{
					bestDot = dot;
					newForward = testDirection;
				}
			}
			IsoAlignSceneView(newForward);
		}

		/// <summary>
		/// Overrides the built in selection duplication to maintain sibling order of the selection. But only if at least one of the selection is under a CSG Model.
		/// </summary>
		/// <returns><c>true</c>, if our custom duplication took place (and one of the selection was under a CSG Model), <c>false</c> otherwise in which case the duplication event should not be consumed so that Unity will duplicate for us.</returns>
		public static bool DuplicateSelection()
		{
			List<Transform> selectedTransforms = Selection.transforms.ToList();

			// Whether any of the selection objects are under a CSG Model
			bool shouldCustomDuplicationOccur = false; 

			for (int i = 0; i < selectedTransforms.Count; i++) 
			{
				if(selectedTransforms[i].GetComponentInParent<CSGModel>() != null)
				{
					shouldCustomDuplicationOccur = true;
				}
			}

			if(shouldCustomDuplicationOccur) // Some of the objects are under a CSG Model, so peform our special duplication override
			{
				// Sort the selected transforms in order of sibling index
				selectedTransforms.Sort((x,y) => x.GetSiblingIndex().CompareTo(y.GetSiblingIndex()));
				GameObject[] newObjects = new GameObject[Selection.gameObjects.Length];

				// Walk through each selected object in the correct order, duplicating them one by one
				for (int i = 0; i < selectedTransforms.Count; i++) 
				{
					// Temporarily set the selection to the single entry
					Selection.activeGameObject = selectedTransforms[i].gameObject;
                    // Seems to be a bug in Unity where we need to set the objects array too otherwise it won't be set straight away
                    Selection.objects = new Object[] { selectedTransforms[i].gameObject };

					// Duplicate the single entry
					Unsupported.DuplicateGameObjectsUsingPasteboard();
					// Cache the new entry, so when we're done we reselect all new objects
					newObjects[i] = Selection.activeGameObject;
				}
				// Finished duplicating, select all new objects
				Selection.objects = newObjects;
			}

			// Whether custom duplication took place and whether the Duplicate event should be consumed
			return shouldCustomDuplicationOccur;
		}

		public class TransformIndexComparer : IComparer
		{
			public int Compare(object x, object y)
			{
				return ((Transform) x).GetSiblingIndex().CompareTo(((Transform) y).GetSiblingIndex());
			}
		}
	}
}
#endif