using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using System.IO;
using System.Reflection;

namespace Sabresaurus.SabreCSG
{
	public class UtilityShortcuts : MonoBehaviour
	{
		[MenuItem("GameObject/Create CSG", false, 30)]
		static void CreateNewCSGObject()
		{
			// Create objects to hold the CSG Model and Work Brush (with associated scripts attached)
			GameObject rootGameObject = new GameObject("CSGModel", typeof(CSGModel));
			
			Undo.RegisterCreatedObjectUndo (rootGameObject, "Create New CSG Model");
			
			// Set the user's selection to the new CSG Model, so that they can start working with it
			Selection.activeGameObject = rootGameObject;

			CurrentSettings.CurrentMode = MainMode.Resize;

			// The automatic lightmapping conflicts when dealing with small brush counts, so default to user baking
			// The user can change this back to Auto if they want, but generally that'll only be an issue when they've
			// got a few brushes.
			Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;
		}
		
		[MenuItem("Edit/Rebuild CSG " + KeyMappings.Rebuild, false, 100)]
		static void Rebuild()
		{
            CSGModel activeCSGModel = CSGModel.GetActiveCSGModel();

            if(activeCSGModel != null)
            {
                activeCSGModel.Build(false, false);
            }
		}

		[MenuItem("Edit/Group", false, 99)]
		public static void Group()
		{
			TransformHelper.GroupSelection();
		}

		[MenuItem("Edit/Ungroup", false, 99)]
		public static void Ungroup()
		{
			TransformHelper.UngroupSelection();
		}

        [MenuItem("Edit/Export All Brushes To OBJ", false, 100)]
        static void ExportAllToObj()
        {
            CSGModel activeCSGModel = CSGModel.GetActiveCSGModel();

            if (activeCSGModel != null)
            {
                activeCSGModel.ExportOBJ(false);
            }
        }

        [MenuItem("Edit/Export Selected To OBJ", false, 100)]
        static void ExportSelectedToObj()
        {
            CSGModel activeCSGModel = CSGModel.GetActiveCSGModel();

            if (activeCSGModel != null)
            {
                activeCSGModel.ExportOBJ(true);
            }
        }

        [MenuItem("Edit/Reset Scene Camera")]
		static void ResetSceneViewCamera()
		{
			// Sometimes have issues with the camera locking up, resetting both current tool and the view tool seems
			// to fix the issue. Generally this seems to be due to not consuming events correctly
			Tools.viewTool = ViewTool.None;
			Tools.current = UnityEditor.Tool.None;
		}

		[MenuItem("Window/CSG 4 Split")]
		static void CSG4Split()
		{
			string layoutsPath = Path.Combine(InternalEditorUtility.unityPreferencesFolder, "Layouts");
			string filePath = Path.Combine(layoutsPath, "4 Split.wlt");

			EditorUtility.LoadWindowLayout(filePath);

			for (int i = 0; i < 4; i++) 
			{
				SceneView sceneView = ((SceneView)SceneView.sceneViews[i]);
				if(EditorHelper.GetSceneViewCamera(sceneView) == EditorHelper.SceneViewCamera.Other)
				{
					sceneView.orthographic = false;
					sceneView.m_SceneLighting = true;
				}
				else
				{
					sceneView.orthographic = true;
					sceneView.m_SceneLighting = false;
					SceneView.SceneViewState state = GetSceneViewState(sceneView);
					state.Toggle(false);
				}
			}
			SceneView.RepaintAll();
		}

		private static SceneView.SceneViewState GetSceneViewState(SceneView sceneView)
		{
			// Unfortunately the SceneViewState (e.g. show fog, show skybox) is not exposed, so we need to reflect it
			BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;
			FieldInfo fieldInfo = typeof(SceneView).GetField("m_SceneViewState", bindingFlags);
			return fieldInfo.GetValue(sceneView) as SceneView.SceneViewState;
		}
	}
}