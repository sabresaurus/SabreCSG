#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System;
using System.Linq;

namespace Sabresaurus.SabreCSG
{
    public static class Toolbar
    {
        public const int BOTTOM_TOOLBAR_HEIGHT = 20;
		public const int PRIMITIVE_MENU_WIDTH = 200;
		public const int PRIMITIVE_MENU_HEIGHT = 70;
		public const int VIEW_MENU_WIDTH = 220;
		public const int VIEW_MENU_HEIGHT = 244;

		public static int bottomToolbarHeight;

        static CSGModel csgModel;

		static string warningMessage = "Concave brushes detected";

		public static bool primitiveMenuShowing = false;
		public static bool viewMenuShowing = false;

		public static Rect viewMenuRect;
		public static Rect primitiveMenuRect;

		// If the viewport is too squashed to show everything on one line
		static bool showToolbarOnTwoLines = false;
		const int SQUASH_WIDTH = 450;

		// Rectangles used for GenericMenu dropdowns
		static Rect gridRect;

        public static CSGModel CSGModel
        {
            get
            {
                return csgModel;
            }
            set
            {
                csgModel = value;
            }
        }

		public static string WarningMessage {
			get {
				return warningMessage;
			}
			set {
				warningMessage = value;
			}
		}

		public static void OnSceneGUI (SceneView sceneView, Event e)
		{
			if (e.type == EventType.Repaint || e.type == EventType.Layout)
			{
				OnRepaint(sceneView, e);
			}
		}

		private static void OnRepaint(SceneView sceneView, Event e)
        {
			showToolbarOnTwoLines = sceneView.position.width < SQUASH_WIDTH;

			bottomToolbarHeight = BOTTOM_TOOLBAR_HEIGHT;
			if (showToolbarOnTwoLines) {
				bottomToolbarHeight *= 2;
			}

            Rect rectangle = new Rect(0, sceneView.position.height - bottomToolbarHeight, sceneView.position.width, bottomToolbarHeight);

            GUIStyle style = new GUIStyle(EditorStyles.toolbar);

            style.fixedHeight = bottomToolbarHeight;
			GUILayout.Window(140003, rectangle, OnBottomToolbarGUI, "", style);

			if (primitiveMenuShowing) {
				style = new GUIStyle(EditorStyles.toolbar);
				primitiveMenuRect = new Rect(
					0, 
					(sceneView.position.height - bottomToolbarHeight) - PRIMITIVE_MENU_HEIGHT, 
					PRIMITIVE_MENU_WIDTH,
					PRIMITIVE_MENU_HEIGHT
				);

				style.fixedHeight = PRIMITIVE_MENU_HEIGHT;
				GUILayout.Window(140006, primitiveMenuRect, OnPrimitiveMenuGUI, "", style);
			}

			if (viewMenuShowing) {
				style = new GUIStyle(EditorStyles.toolbar);
				viewMenuRect = new Rect(
					sceneView.position.width - VIEW_MENU_WIDTH, 
					(sceneView.position.height - bottomToolbarHeight) - VIEW_MENU_HEIGHT, 
					VIEW_MENU_WIDTH,
					VIEW_MENU_HEIGHT
				);

				style.fixedHeight = VIEW_MENU_HEIGHT;
				GUILayout.Window(140012, viewMenuRect, OnViewMenuGUI, "", style);
			}

			style = new GUIStyle(EditorStyles.toolbar);

			style.normal.background = SabreCSGResources.ClearTexture;
			rectangle = new Rect(0, 20, 320, 50);
			GUILayout.Window(140004, rectangle, OnTopToolbarGUI, "", style);

			if(!string.IsNullOrEmpty(warningMessage))
			{				
				style.fixedHeight = 70;
				rectangle = new Rect(0, sceneView.position.height - bottomToolbarHeight - style.fixedHeight, sceneView.position.width, style.fixedHeight);
				GUILayout.Window(140005, rectangle, OnWarningToolbar, "", style);
			}
            
        }

        private static void OnTopToolbarGUI(int windowID)
        {
			EditorGUILayout.BeginHorizontal();
            MainMode currentMode = CurrentSettings.CurrentMode;
            if(CurrentSettings.OverrideMode != OverrideMode.None)
            {
                currentMode = (MainMode)(-1);
            }
            MainMode newMainMode = SabreGUILayout.DrawPartialEnumGrid(currentMode, CurrentSettings.enabledModes, GUILayout.Width(50));
            if(newMainMode != currentMode)
            {
                csgModel.SetCurrentMode(newMainMode);
            }

			/*
			bool isClipMode = (CurrentSettings.OverrideMode == OverrideMode.Clip);
			if(SabreGUILayout.Toggle(isClipMode, "Clip"))
			{
				csgModel.SetOverrideMode(OverrideMode.Clip);
			}
			else
			{
				if(isClipMode)
				{
					csgModel.ExitOverrideMode();
				}
			}

			bool isDrawMode = (CurrentSettings.OverrideMode == OverrideMode.Draw);

			if(SabreGUILayout.Toggle(isDrawMode, "Draw"))
			{
				csgModel.SetOverrideMode(OverrideMode.Draw);
			}
			else
			{
				if(isDrawMode)
				{
					csgModel.ExitOverrideMode();
				}
			}
			*/
			
			EditorGUILayout.EndHorizontal();
        }

		private static void OnWarningToolbar(int windowID)
		{
			GUIStyle style = SabreGUILayout.GetOverlayStyle();
			Vector2 size = style.CalcSize(new GUIContent(warningMessage));

			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			GUILayout.Box(warningMessage, style, GUILayout.Width(size.x));
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
		}

		static Vector3 GetPositionForNewBrush()
		{
			Vector3 newPosition = Vector3.zero;
			if(SceneView.lastActiveSceneView != null)
			{
				Transform cameraTransform = SceneView.lastActiveSceneView.camera.transform;
				Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

                // First of all try to cast against existing brushes
				List<PolygonRaycastHit> hits = csgModel.RaycastBrushesAll(ray, false);
				if(hits.Count > 0)
				{
					newPosition = hits[0].Point;
					// Back a unit, since the brush is around 2 units in each dimensions
					newPosition += hits[0].Normal * CurrentSettings.PositionSnapDistance;
					newPosition -= csgModel.GetComponent<Transform>().position;

					if(CurrentSettings.PositionSnappingEnabled)
					{
						float snapDistance = CurrentSettings.PositionSnapDistance;
						newPosition = MathHelper.RoundVector3(newPosition, snapDistance);
					}
				}
				else
				{
                    // Couldn't hit an existing brush, so try to cast against the grid
                    float hitDistance = 0;
                    Plane activePlane = GetActivePlane();
                    if(activePlane.Raycast(ray, out hitDistance))
                    {
                        newPosition = ray.GetPoint(hitDistance);
                        // Back a unit, since the brush is around 2 units in each dimensions
                        newPosition += activePlane.normal;
						newPosition -= csgModel.GetComponent<Transform>().position;

                        if (CurrentSettings.PositionSnappingEnabled)
                        {
                            float snapDistance = CurrentSettings.PositionSnapDistance;
                            newPosition = MathHelper.RoundVector3(newPosition, snapDistance);
                        }
                    }
                    else
                    {
                        // Couldn't hit the grid, probably because they're looking up at the sky, so fallback to the camera pivot point
                        newPosition = SceneView.lastActiveSceneView.pivot;
                        if (CurrentSettings.PositionSnappingEnabled)
                        {
                            float snapDistance = CurrentSettings.PositionSnapDistance;
                            newPosition = MathHelper.RoundVector3(newPosition, snapDistance);
                        }
                    }
				}
			}
			return newPosition;
		}

        static Plane GetActivePlane()
        {
            SceneView activeSceneView = SceneView.lastActiveSceneView;
            if (activeSceneView != null 
                && activeSceneView.camera != null 
                && activeSceneView.camera.orthographic 
                && EditorHelper.GetSceneViewCamera(activeSceneView.camera) != EditorHelper.SceneViewCamera.Other)
            {
                // Axis aligned iso view
                return new Plane() { normal = -SceneView.lastActiveSceneView.camera.transform.forward, distance = 0 };
            }
            else
            {
                // No plane override, so use ground plane
                return new Plane() { normal = Vector3.up, distance = 0 };
            }
        }

        static void CreatePrimitiveBrush(PrimitiveBrushType brushType)
		{
			Vector3 position = GetPositionForNewBrush();
			GameObject newBrushObject = csgModel.CreateBrush(
				brushType, 
				position,
				Vector3.one * CurrentSettings.PositionSnapDistance * 2f
			);

			// Set the selection to the new object
			Selection.activeGameObject = newBrushObject;

			Undo.RegisterCreatedObjectUndo(newBrushObject, "Create Brush");
		}

		static void CreateCompoundBrush(object compoundBrushType)
		{
			// Make sure we're actually being asked to create a compound brush
			if(compoundBrushType != null 
				&& compoundBrushType is Type
				&& !typeof(CompoundBrush).IsAssignableFrom((Type)compoundBrushType))
			{
				throw new ArgumentException("Specified type must be derived from CompoundBrush");
			}

			Vector3 position = GetPositionForNewBrush();
			GameObject newBrushObject = csgModel.CreateCompoundBrush((Type) compoundBrushType, position);

			// Set the selection to the new object
			Selection.activeGameObject = newBrushObject;

			Undo.RegisterCreatedObjectUndo(newBrushObject, "Create Brush");
		}

		static void CreateCompoundBrush<T>() where T : CompoundBrush
		{
			Vector3 position = GetPositionForNewBrush();
			GameObject newBrushObject = csgModel.CreateCompoundBrush<T>(position);

			// Set the selection to the new object
			Selection.activeGameObject = newBrushObject;

			Undo.RegisterCreatedObjectUndo(newBrushObject, "Create Brush");
		}

		static void OnSelectedGridOption(object userData)
		{
			if(userData.GetType() == typeof(GridMode))
			{
				CurrentSettings.GridMode = (GridMode)userData;
				GridManager.UpdateGrid();
			}
		}

		static void OnViewMenuGUI(int windowID) {

			float left_pad = 90f;
			EditorGUIUtility.labelWidth = 118f;

			GUILayout.Space(4);

			GUILayout.BeginHorizontal();
			GUILayout.Label("Viewport Settings", EditorStyles.boldLabel);
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			bool lastBrushesHidden = CurrentSettings.BrushesHidden;
            CurrentSettings.BrushesHidden = EditorGUILayout.Toggle(
				new GUIContent("Hide Brushes", "Hotkey: "+KeyMappings.Instance.ToggleBrushesHidden),
				CurrentSettings.BrushesHidden
			);
            if (CurrentSettings.BrushesHidden != lastBrushesHidden)
            {
                // Has changed
                CSGModel.UpdateAllBrushesVisibility();
                SceneView.RepaintAll();
            }
			GUILayout.EndHorizontal();

			bool lastMeshHidden = CurrentSettings.MeshHidden;
			CurrentSettings.MeshHidden = EditorGUILayout.Toggle("Hide Meshes", CurrentSettings.MeshHidden);
			if (CurrentSettings.MeshHidden != lastMeshHidden)
			{
				// Has changed
                CSGModel.UpdateAllBrushesVisibility();
				SceneView.RepaintAll();
			}

			EditorGUI.BeginChangeCheck();
            CurrentSettings.ShowExcludedPolygons = EditorGUILayout.Toggle("Show excluded faces", CurrentSettings.ShowExcludedPolygons);
            if (EditorGUI.EndChangeCheck())
            {
                // What's shown in the SceneView has potentially changed, so force it to repaint
                SceneView.RepaintAll();
            }

			EditorGUI.BeginChangeCheck();
            CurrentSettings.ShowBrushBoundsGuideLines = EditorGUILayout.Toggle("Show brush guides", CurrentSettings.ShowBrushBoundsGuideLines);
            if (EditorGUI.EndChangeCheck())
            {
                // What's shown in the SceneView has potentially changed, so force it to repaint
                CSGModel.UpdateAllBrushesVisibility();
                SceneView.RepaintAll();
            }

			EditorGUI.BeginChangeCheck();
            CurrentSettings.ShowBrushesAsWireframes = EditorGUILayout.Toggle("Wireframe", CurrentSettings.ShowBrushesAsWireframes);
            if (EditorGUI.EndChangeCheck())
            {
                // What's shown in the SceneView has potentially changed, so force it to repaint
                CSGModel.UpdateAllBrushesVisibility();
                SceneView.RepaintAll();
            }

			GUILayout.Space(10);

			GUILayout.BeginHorizontal();
			GUILayout.Label("Grid Settings", EditorStyles.boldLabel);
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Grid type", EditorStyles.label);
			GUILayout.FlexibleSpace();

			GridMode lastMode = CurrentSettings.GridMode;
			CurrentSettings.GridMode = (GridMode) EditorGUILayout.EnumPopup(CurrentSettings.GridMode, GUILayout.Width(left_pad));
			if (CurrentSettings.GridMode != lastMode) {
				OnSelectedGridOption(CurrentSettings.GridMode);
			}
			GUILayout.EndHorizontal();

			// Projected grid
            bool lastProjectedGridEnabled = CurrentSettings.ProjectedGridEnabled;
            CurrentSettings.ProjectedGridEnabled = EditorGUILayout.Toggle(
				new GUIContent("Projected Grid", "Hotkey: "+KeyMappings.Instance.ToggleProjectedGrid.Replace("#", "Shift+")),
				CurrentSettings.ProjectedGridEnabled
			);
            if (CurrentSettings.ProjectedGridEnabled != lastProjectedGridEnabled)
            {
                SceneView.RepaintAll();
            }
            if (Event.current.type == EventType.Repaint)
			{
				gridRect = GUILayoutUtility.GetLastRect();
				gridRect.width = 100;
			}

			// Position snapping UI
			CurrentSettings.PositionSnappingEnabled = EditorGUILayout.Toggle(
				new GUIContent("Grid snapping", "Hotkey: "+KeyMappings.Instance.TogglePosSnapping.Replace("#", "Shift+")),
				CurrentSettings.PositionSnappingEnabled
			);

			// Position snapping UI
			CurrentSettings.AngleSnappingEnabled = EditorGUILayout.Toggle(
				new GUIContent("Rotation snapping", "Hotkey: "+KeyMappings.Instance.ToggleAngSnapping.Replace("#", "Shift+")),
				CurrentSettings.AngleSnappingEnabled
			);

			// Rotation snapping UI
			GUILayout.BeginHorizontal();
			GUILayout.Label(new GUIContent(
				"Rotation size",
				"Hotkeys: " + KeyMappings.Instance.DecreaseAngSnapping.Replace("#", "Shift+") + "  " + KeyMappings.Instance.IncreaseAngSnapping.Replace("#", "Shift+")
			), EditorStyles.label);
			GUILayout.FlexibleSpace();

			// CurrentSettings.AngleSnappingEnabled = SabreGUILayout.Toggle(CurrentSettings.AngleSnappingEnabled, "Ang Snapping");
			CurrentSettings.AngleSnapDistance = EditorGUILayout.FloatField(CurrentSettings.AngleSnapDistance, GUILayout.Width(50));

			if (SabreGUILayout.Button("-", EditorStyles.miniButtonLeft))
			{
				if(CurrentSettings.AngleSnapDistance > 15)
				{
					CurrentSettings.AngleSnapDistance -= 15;
				}
				else
				{
					CurrentSettings.AngleSnapDistance -= 5;
				}
			}
			if (SabreGUILayout.Button("+", EditorStyles.miniButtonRight))
			{
				if(CurrentSettings.AngleSnapDistance >= 15)
				{
					CurrentSettings.AngleSnapDistance += 15;
				}
				else
				{
					CurrentSettings.AngleSnapDistance += 5;
				}
			}
			GUILayout.EndHorizontal();
		}

		static void OnPrimitiveMenuGUI(int windowID) {
			GUIStyle createBrushStyle = new GUIStyle(EditorStyles.toolbarButton);
			createBrushStyle.fixedHeight = 20;

			GUILayout.BeginHorizontal();
			GUILayout.Label("Create primitive", EditorStyles.boldLabel);
			GUILayout.FlexibleSpace();

			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();

            if (GUILayout.Button(SabreCSGResources.ButtonSphereTexture, createBrushStyle))
            {
                CreatePrimitiveBrush(PrimitiveBrushType.Sphere);
				primitiveMenuShowing = false;
            }

            if (GUILayout.Button(SabreCSGResources.ButtonIcoSphereTexture, createBrushStyle))
            {
                CreatePrimitiveBrush(PrimitiveBrushType.IcoSphere);
				primitiveMenuShowing = false;
            }

            if (GUILayout.Button(SabreCSGResources.ButtonStairsTexture, createBrushStyle))
            {
                CreateCompoundBrush<StairBrush>();
				primitiveMenuShowing = false;
            }
			
            if (GUILayout.Button(SabreCSGResources.ButtonCurvedStairsTexture, createBrushStyle))
            {
                CreateCompoundBrush<CurvedStairBrush>();
				primitiveMenuShowing = false;
            }

			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));

            if (GUILayout.Button(SabreCSGResources.ButtonCylinderTexture, createBrushStyle))
			{
				CreatePrimitiveBrush(PrimitiveBrushType.Cylinder);
				primitiveMenuShowing = false;
			}

            if (GUILayout.Button(SabreCSGResources.ButtonConeTexture, createBrushStyle))
            {
                CreatePrimitiveBrush(PrimitiveBrushType.Cone);
				primitiveMenuShowing = false;
            }

            if (GUILayout.Button(SabreCSGResources.ButtonShapeEditorTexture, createBrushStyle))
            {
                CreateCompoundBrush<ShapeEditor.ShapeEditorBrush>();
				primitiveMenuShowing = false;
            }

			if (GUILayout.Button(SabreCSGResources.ButtonMoreTexture, createBrushStyle))
			{
				GenericMenu menu = new GenericMenu ();

				List<Type> compoundBrushTypes = CompoundBrush.FindAllInAssembly();
				for (int i = 0; i < compoundBrushTypes.Count; i++) 
				{
                    int j = i; // Closure causes "i" to be "2" in the lambda expression unless we assign it to a scoped variable.
					menu.AddItem (
						new GUIContent (compoundBrushTypes[i].Name), 
						false, 
						() => {
							CreateCompoundBrush(compoundBrushTypes[j]);
							primitiveMenuShowing = false;
						} 
					);
				}

                menu.AddSeparator("");
                
                menu.AddItem(
					new GUIContent("Add More?"), 
					false, 
					() => { 
						EditorUtility.DisplayDialog("SabreCSG - About Compound Brushes", "Any custom compound brushes in your project are automatically detected and added to this list. Simply inherit from 'Sabresaurus.SabreCSG.CompoundBrush'.", "Okay");
						primitiveMenuShowing = false;
					}
				);

				menu.DropDown(new Rect(60,createBrushStyle.fixedHeight, 100, createBrushStyle.fixedHeight));
			}

			GUILayout.EndHorizontal();
		}

        private static void OnBottomToolbarGUI(int windowID)
        {
            GUILayout.BeginHorizontal();

			GUIStyle createBrushStyle = new GUIStyle(EditorStyles.toolbarButton);
			createBrushStyle.fixedHeight = 20;
			if(GUILayout.Button(SabreCSGResources.ButtonCubeTexture, createBrushStyle))
			{
				CreatePrimitiveBrush(PrimitiveBrushType.Cube);
			}

			primitiveMenuShowing = GUILayout.Toggle(primitiveMenuShowing, primitiveMenuShowing?"▼": "▲", createBrushStyle);

#if DEBUG_SABRECSG_PERF
			// For debugging frame rate
			GUILayout.Label(((int)(1 / csgModel.CurrentFrameDelta)).ToString(), SabreGUILayout.GetLabelStyle());
#endif

            if (SabreGUILayout.Button("Rebuild", createBrushStyle))
            {
				csgModel.Build(false, false);
            }

			if (SabreGUILayout.Button("Force Rebuild", createBrushStyle))
			{
				csgModel.Build(true, false);
			}

			GUI.color = Color.white;

			csgModel.AutoRebuild = GUILayout.Toggle(csgModel.AutoRebuild, "Auto Rebuild", createBrushStyle);

			GUI.color = Color.white;
#if SABRE_CSG_DEBUG
            GUILayout.Label(csgModel.BuildMetrics.BuildMetaData.ToString(), SabreGUILayout.GetForeStyle(), GUILayout.Width(140));
#else
            EditorGUILayout.Space();
#endif
            
			if (showToolbarOnTwoLines) {
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal();
			}


			GUILayout.FlexibleSpace();

			GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
			labelStyle.fontSize = 9;
			labelStyle.fixedHeight = 16;
			labelStyle.alignment = TextAnchor.MiddleCenter;

			GUILayout.Label("Grid size", labelStyle);

			CurrentSettings.PositionSnapDistance = EditorGUILayout.FloatField(CurrentSettings.PositionSnapDistance, GUILayout.MaxWidth(70f),GUILayout.MinWidth(30f));
			
			if (SabreGUILayout.Button("-", EditorStyles.miniButtonLeft))
			{
				CurrentSettings.ChangePosSnapDistance(.5f);
			}
			if (SabreGUILayout.Button("+", EditorStyles.miniButtonRight))
			{
				CurrentSettings.ChangePosSnapDistance(2f);
			}

			viewMenuShowing = GUILayout.Toggle(viewMenuShowing, "Viewport settings", createBrushStyle);

            GUILayout.EndHorizontal();
        }
    }
}
#endif
