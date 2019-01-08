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
		public const int BRUSH_MENU_WIDTH = 130;
		public const int BRUSH_MENU_HEIGHT = 122;
		public const int VIEW_MENU_WIDTH = 220;
		public const int VIEW_MENU_HEIGHT = 180;

		public static int bottomToolbarHeight;

		static BrushBase primaryBrush;
		static List<BrushBase> selectedBrushes;
		static string[] brushModeSettings = new string[] {
			"Add",
			"Subtract",
			"Volume",
			"NoCSG"
		};

		static string[] gridTypeSettings = new string[] {
			"Unity",
			"SabreCSG",
			"None"
		};

        static CSGModel csgModel;

		static string warningMessage = "Concave brushes detected";

		static bool primitiveMenuShowing = false;
		static bool viewMenuShowing = false;

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

			// Brush menu
			if (Selection.activeGameObject != null)
            {
				style = new GUIStyle(EditorStyles.toolbar);
				primaryBrush = Selection.activeGameObject.GetComponent<BrushBase>();
				selectedBrushes = new List<BrushBase>();
				for (int i = 0; i < Selection.gameObjects.Length; i++) 
				{
					BrushBase brush = Selection.gameObjects[i].GetComponent<BrushBase>();
					if (brush != null)
					{
						selectedBrushes.Add(brush);
					}
				}
                if (primaryBrush != null && primaryBrush.SupportsCsgOperations)
                {
					Rect brushMenuRect = new Rect(
						0, 
						(sceneView.position.height - bottomToolbarHeight) - BRUSH_MENU_HEIGHT, 
						BRUSH_MENU_WIDTH,
						BRUSH_MENU_HEIGHT
					);

					if (primitiveMenuShowing) {
						brushMenuRect.y -= PRIMITIVE_MENU_HEIGHT;
					}

					style.fixedWidth = BRUSH_MENU_WIDTH;
					style.fixedHeight = BRUSH_MENU_HEIGHT;
					GUILayout.Window(140008, brushMenuRect, OnBrushSettingsGUI, "", style);
				}
			}

			if (primitiveMenuShowing) {
				style = new GUIStyle(EditorStyles.toolbar);
				Rect primitiveMenuRect = new Rect(
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
				Rect viewMenuRect = new Rect(
					sceneView.position.width - VIEW_MENU_WIDTH, 
					(sceneView.position.height - bottomToolbarHeight) - VIEW_MENU_HEIGHT, 
					VIEW_MENU_WIDTH,
					VIEW_MENU_HEIGHT
				);

				style.fixedHeight = VIEW_MENU_HEIGHT;
				GUILayout.Window(140009, viewMenuRect, OnViewMenuGUI, "", style);
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
            MainMode newMainMode = SabreGUILayout.DrawEnumGrid(currentMode, GUILayout.Width(50));
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

		// Calculate the bounds for all selected brushes, respecting the current pivotRotation mode to produce 
		// bounds aligned to the first selected brush in Local mode, or bounds aligned to the absolute grid in Global
		// mode.
		static Bounds GetBounds()
		{
			Bounds bounds;

			if(Tools.pivotRotation == PivotRotation.Local)
			{
				bounds = primaryBrush.GetBounds();

				for (int i = 0; i < selectedBrushes.Count; i++) 
				{
					if(selectedBrushes[i] != primaryBrush)
					{
                        bounds.Encapsulate(selectedBrushes[i].GetBoundsLocalTo(primaryBrush.transform));
                    }
				}
			}
			else // Absolute/Global
			{
				bounds = primaryBrush.GetBoundsTransformed();
				for (int i = 0; i < selectedBrushes.Count; i++) 
				{
					if(selectedBrushes[i] != primaryBrush)
					{
						bounds.Encapsulate(selectedBrushes[i].GetBoundsTransformed());
					}
				}
			}

			return bounds;
		}

		private static Vector3 GetSelectedBrushesPivotPoint()
        {
            if (primaryBrush != null)
            {
                if (Tools.pivotMode == PivotMode.Center)
                {
                    Bounds bounds = GetBounds();
                    if (Tools.pivotRotation == PivotRotation.Global)
                    {
                        return bounds.center;
                    }
                    else
                    {
                        return primaryBrush.transform.TransformPoint(bounds.center);
                    }
                }
                else // Local mode
                {
                    // Just return the position of the primary selected brush
                    return primaryBrush.transform.position;
                }
            }
            else
            {
                return Vector3.zero;
            }
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
					newPosition += hits[0].Normal;
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
			GameObject newBrushObject = csgModel.CreateBrush(brushType, position);

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

			GUILayout.Space(4);
			GUIStyle labelStyle = SabreGUILayout.GetLabelStyle();

			GUILayout.BeginHorizontal();
			labelStyle.fontStyle = FontStyle.Bold;
			GUILayout.Label("Viewport Settings", labelStyle);
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Hide Brushes");
			GUILayout.FlexibleSpace();
			bool lastBrushesHidden = CurrentSettings.BrushesHidden;
            CurrentSettings.BrushesHidden = GUILayout.Toggle(CurrentSettings.BrushesHidden, "", GUILayout.Width(left_pad));
            if (CurrentSettings.BrushesHidden != lastBrushesHidden)
            {
                // Has changed
                CSGModel.UpdateAllBrushesVisibility();
                SceneView.RepaintAll();
            }
			GUILayout.EndHorizontal();


			GUILayout.BeginHorizontal();
			GUILayout.Label("Hide Meshes");
			GUILayout.FlexibleSpace();
			bool lastMeshHidden = CurrentSettings.MeshHidden;
			CurrentSettings.MeshHidden = GUILayout.Toggle(CurrentSettings.MeshHidden, "", GUILayout.Width(left_pad));
			if (CurrentSettings.MeshHidden != lastMeshHidden)
			{
				// Has changed
                CSGModel.UpdateAllBrushesVisibility();
				SceneView.RepaintAll();
			}
			GUILayout.EndHorizontal();

			GUILayout.Space(10);

			GUILayout.BeginHorizontal();
			GUILayout.Label("Grid Settings", labelStyle);
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Grid type");
			GUILayout.FlexibleSpace();

			GridMode lastMode = CurrentSettings.GridMode;
			CurrentSettings.GridMode = (GridMode) EditorGUILayout.EnumPopup(CurrentSettings.GridMode, GUILayout.Width(left_pad));
			if (CurrentSettings.GridMode != lastMode) {
				OnSelectedGridOption(CurrentSettings.GridMode);
			}
			GUILayout.EndHorizontal();

			// Projected grid
			GUILayout.BeginHorizontal();
			GUILayout.Label("Projected Grid");
			GUILayout.FlexibleSpace();
            bool lastProjectedGridEnabled = CurrentSettings.ProjectedGridEnabled;
            CurrentSettings.ProjectedGridEnabled = GUILayout.Toggle(CurrentSettings.ProjectedGridEnabled, "", GUILayout.Width(left_pad));
            if (CurrentSettings.ProjectedGridEnabled != lastProjectedGridEnabled)
            {
                SceneView.RepaintAll();
            }
            if (Event.current.type == EventType.Repaint)
			{
				gridRect = GUILayoutUtility.GetLastRect();
				gridRect.width = 100;
			}
			GUILayout.EndHorizontal();

			// Position snapping UI
			GUILayout.BeginHorizontal();
			GUILayout.Label("Grid snapping");
			GUILayout.FlexibleSpace();
			CurrentSettings.PositionSnappingEnabled = GUILayout.Toggle(CurrentSettings.PositionSnappingEnabled, "", GUILayout.Width(left_pad));
			GUILayout.EndHorizontal();

			// Position snapping UI
			GUILayout.BeginHorizontal();
			GUILayout.Label("Rotation snapping");
			GUILayout.FlexibleSpace();
			CurrentSettings.AngleSnappingEnabled = GUILayout.Toggle(CurrentSettings.AngleSnappingEnabled, "", GUILayout.Width(left_pad));
			GUILayout.EndHorizontal();

			// Rotation snapping UI
			GUILayout.BeginHorizontal();
			GUILayout.Label("Snap angle");
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

		static void OnBrushSettingsGUI(int windowID) {
			GUIStyle labelStyle = SabreGUILayout.GetLabelStyle();

			GUILayout.BeginHorizontal();
			labelStyle.fontStyle = FontStyle.Bold;
			GUILayout.Label("Brush Settings", labelStyle);
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();

			GUILayout.Label ("Mode");

			GUILayout.FlexibleSpace();

			string currentBrushMode = "";
			if (primaryBrush.IsNoCSG) {
				currentBrushMode = "NoCSG";
			} else {
				currentBrushMode = primaryBrush.Mode.ToString();
			}
			int currentModeIndex = Array.IndexOf(brushModeSettings, currentBrushMode);

			string brushMode = brushModeSettings[EditorGUILayout.Popup("", currentModeIndex, brushModeSettings, GUILayout.Width(60))]; 
			if(brushMode != currentBrushMode)
			{
				bool anyChanged = false;

				foreach (BrushBase brush in selectedBrushes) 
				{
					Undo.RecordObject(brush, "Change Brush To " + brushMode);

					switch (brushMode) {
						case "Add":
							brush.Mode = CSGMode.Add;
							brush.IsNoCSG = false;
							break;
						case "Subtract":
							brush.Mode = CSGMode.Subtract;
							brush.IsNoCSG = false;
							break;
						case "Volume":
							brush.Mode = CSGMode.Volume;
							brush.IsNoCSG = false;
							break;
						case "NoCSG":
							// Volume overrides NoCSG, so it must be changed if you select NoCSG
							if (brush.Mode == CSGMode.Volume) { 
								brush.Mode = CSGMode.Add;
							}
							brush.IsNoCSG = true;
							break;
					}
					anyChanged = true;
				}
				if(anyChanged)
				{
					// Need to update the icon for the csg mode in the hierarchy
					EditorApplication.RepaintHierarchyWindow();

					foreach (BrushBase b in selectedBrushes) 
					{
						b.Invalidate(true);
					}
				}
			}

			GUILayout.EndHorizontal();

			bool[] collisionStates = selectedBrushes.Select(item => item.HasCollision).Distinct().ToArray();
			bool hasCollision = (collisionStates.Length == 1) ? collisionStates[0] : false;

			GUIStyle toggleStyle = new GUIStyle(GUI.skin.toggle);

			// TODO: If the brushes are all volumes, the collision and visible checkboxes should be disabled

			// bool allVolumes = true;
			// foreach (BrushBase brush in selectedBrushes) 
			// {
			// 	if (brush.Mode != CSGMode.Volume) {
			// 		allVolumes = false;
			// 	}
			// }

			GUILayout.BeginHorizontal();
			GUILayout.Label("Collision");
			GUILayout.FlexibleSpace();
			bool newHasCollision = GUILayout.Toggle(hasCollision, "");
			GUILayout.EndHorizontal();

			if(newHasCollision != hasCollision)
			{
				foreach (BrushBase brush in selectedBrushes) 
				{
					Undo.RecordObject(brush, "Change Brush Collision Mode");
					brush.HasCollision = newHasCollision;
				}
				// Tell the brushes that they have changed and need to recalc intersections
				foreach (BrushBase brush in selectedBrushes) 
				{
					brush.Invalidate(true);
				}
			}

			bool[] visibleStates = selectedBrushes.Select(item => item.IsVisible).Distinct().ToArray();
			bool isVisible = (visibleStates.Length == 1) ? visibleStates[0] : false;

			GUILayout.BeginHorizontal();
			GUILayout.Label("Visible");
			GUILayout.FlexibleSpace();
			bool newIsVisible = GUILayout.Toggle(isVisible, "");
			GUILayout.EndHorizontal();

			if(newIsVisible != isVisible)
			{
				foreach (BrushBase brush in selectedBrushes) 
				{
					Undo.RecordObject(brush, "Change Brush Visible Mode");
					brush.IsVisible = newIsVisible;
				}
				// Tell the brushes that they have changed and need to recalc intersections
				foreach (BrushBase brush in selectedBrushes) 
				{
					brush.Invalidate(true);
				}
				if(newIsVisible == false)
				{
					csgModel.NotifyPolygonsRemoved();
				}
			}

			GUILayout.BeginHorizontal();

			if (GUILayout.Button("Flip X", EditorStyles.miniButton))
            {	
                Undo.RecordObjects(selectedBrushes.ToArray(), "Flip Polygons");

                bool localToPrimaryBrush = (Tools.pivotRotation == PivotRotation.Local);
                BrushUtility.Flip(primaryBrush, selectedBrushes.ToArray(), 0, localToPrimaryBrush, GetSelectedBrushesPivotPoint());
            }

			if (GUILayout.Button("Flip Y", EditorStyles.miniButton))
            {	
                Undo.RecordObjects(selectedBrushes.ToArray(), "Flip Polygons");

                bool localToPrimaryBrush = (Tools.pivotRotation == PivotRotation.Local);
                BrushUtility.Flip(primaryBrush, selectedBrushes.ToArray(), 1, localToPrimaryBrush, GetSelectedBrushesPivotPoint());
            }

			if (GUILayout.Button("Flip Z", EditorStyles.miniButton))
            {	
                Undo.RecordObjects(selectedBrushes.ToArray(), "Flip Polygons");

                bool localToPrimaryBrush = (Tools.pivotRotation == PivotRotation.Local);
                BrushUtility.Flip(primaryBrush, selectedBrushes.ToArray(), 2, localToPrimaryBrush, GetSelectedBrushesPivotPoint());
            }

			GUILayout.EndHorizontal();

			if (GUILayout.Button("Snap Center", EditorStyles.miniButton))
            {
                for (int i = 0; i < selectedBrushes.Count; i++)
                {
                    Undo.RecordObject(selectedBrushes[i].transform, "Snap Center");
                    Undo.RecordObject(selectedBrushes[i], "Snap Center");

                    Vector3 newPosition = selectedBrushes[i].transform.position;

                    float snapDistance = CurrentSettings.PositionSnapDistance;
                    newPosition = MathHelper.RoundVector3(newPosition, snapDistance);
                    selectedBrushes[i].transform.position = newPosition;
                    selectedBrushes[i].Invalidate(true);
                }
            }

			GUILayout.BeginHorizontal();
			labelStyle.fontStyle = FontStyle.Normal;
			labelStyle.fontSize = 9;
			GUILayout.FlexibleSpace();
			GUILayout.Label(selectedBrushes.Count + " selected", labelStyle);
			GUILayout.EndHorizontal();

		}

		static void OnPrimitiveMenuGUI(int windowID) {
			GUIStyle createBrushStyle = new GUIStyle(EditorStyles.toolbarButton);
			createBrushStyle.fixedHeight = 20;

			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			GUILayout.Label("Create primitive");
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
					menu.AddItem (
						new GUIContent (compoundBrushTypes[i].Name), 
						false, 
						() => {
							CreateCompoundBrush(compoundBrushTypes[i]);
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

			GUIStyle labelStyle = SabreGUILayout.GetLabelStyle();
			labelStyle.fontSize = 9;
			labelStyle.fixedHeight = 16;
			labelStyle.alignment = TextAnchor.MiddleCenter;

			GUILayout.Label("Grid size", labelStyle);

			CurrentSettings.PositionSnapDistance = EditorGUILayout.FloatField(CurrentSettings.PositionSnapDistance, GUILayout.Width(30));
			
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
