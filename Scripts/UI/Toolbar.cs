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
        public const int BOTTOM_TOOLBAR_HEIGHT = 40;

        static CSGModel csgModel;

		static string warningMessage = "Concave brushes detected";

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
            Rect rectangle = new Rect(0, sceneView.position.height - BOTTOM_TOOLBAR_HEIGHT, sceneView.position.width, BOTTOM_TOOLBAR_HEIGHT);

            GUIStyle style = new GUIStyle(EditorStyles.toolbar);

            style.fixedHeight = BOTTOM_TOOLBAR_HEIGHT;
			GUILayout.Window(140003, rectangle, OnBottomToolbarGUI, "", style);//, EditorStyles.textField);

			style = new GUIStyle(EditorStyles.toolbar);

			style.normal.background = SabreCSGResources.ClearTexture;
			rectangle = new Rect(0, 20, 320, 50);
			GUILayout.Window(140004, rectangle, OnTopToolbarGUI, "", style);

			if(!string.IsNullOrEmpty(warningMessage))
			{				
				style.fixedHeight = 70;
				rectangle = new Rect(0, sceneView.position.height - BOTTOM_TOOLBAR_HEIGHT - style.fixedHeight, sceneView.position.width, style.fixedHeight);
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
				&& compoundBrushType == typeof(Type) 
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

        private static void OnBottomToolbarGUI(int windowID)
        {
            GUILayout.BeginHorizontal();

            GUIStyle createBrushStyle = new GUIStyle(EditorStyles.toolbarButton);
			createBrushStyle.fixedHeight = 20;
			if(GUI.Button(new Rect(0,0, 30, createBrushStyle.fixedHeight), SabreCSGResources.ButtonCubeTexture, createBrushStyle))
			{
				CreatePrimitiveBrush(PrimitiveBrushType.Cube);
			}

			if(GUI.Button(new Rect(30,0, 30, createBrushStyle.fixedHeight), SabreCSGResources.ButtonPrismTexture, createBrushStyle))
			{
				CreatePrimitiveBrush(PrimitiveBrushType.Prism);
			}

			//if(GUI.Button(new Rect(60,0, 30, createBrushStyle.fixedHeight), "", createBrushStyle))
			//{
			//}

            if (GUI.Button(new Rect(60, 0, 30, createBrushStyle.fixedHeight), SabreCSGResources.ButtonStairsTexture, createBrushStyle))
            {
                CreateCompoundBrush<StairBrush>();
            }

            GUILayout.Space(92);
#if DEBUG_SABRECSG_PERF
			// For debugging frame rate
			GUILayout.Label(((int)(1 / csgModel.CurrentFrameDelta)).ToString(), SabreGUILayout.GetLabelStyle());
#endif

            if (SabreGUILayout.Button("Rebuild"))
            {
				csgModel.Build(false, false);
            }

			if (SabreGUILayout.Button("Force Rebuild"))
			{
				csgModel.Build(true, false);
			}

			GUI.color = Color.white;

			if(csgModel.AutoRebuild)
			{
				GUI.color = Color.green;
			}
			csgModel.AutoRebuild = SabreGUILayout.Toggle(csgModel.AutoRebuild, "Auto Rebuild");
			GUI.color = Color.white;

			GUILayout.Label(csgModel.BuildMetrics.BuildMetaData.ToString(), SabreGUILayout.GetForeStyle(), GUILayout.Width(140));

            bool lastBrushesHidden = CurrentSettings.BrushesHidden;
			if(lastBrushesHidden)
			{
				GUI.color = Color.red;
			}
            CurrentSettings.BrushesHidden = SabreGUILayout.Toggle(CurrentSettings.BrushesHidden, "Brushes Hidden");
            if (CurrentSettings.BrushesHidden != lastBrushesHidden)
            {
                // Has changed
                csgModel.UpdateBrushVisibility();
                SceneView.RepaintAll();
            }
			GUI.color = Color.white;


			bool lastMeshHidden = CurrentSettings.MeshHidden;
			if(lastMeshHidden)
			{
				GUI.color = Color.red;
			}
			CurrentSettings.MeshHidden = SabreGUILayout.Toggle(CurrentSettings.MeshHidden, "Mesh Hidden");
			if (CurrentSettings.MeshHidden != lastMeshHidden)
			{
				// Has changed
				csgModel.UpdateBrushVisibility();
				SceneView.RepaintAll();
			}

			GUI.color = Color.white;

			
			if(GUILayout.Button("Grid " + CurrentSettings.GridMode.ToString(), EditorStyles.toolbarDropDown, GUILayout.Width(90)))
			{
				GenericMenu menu = new GenericMenu ();
				
				string[] names = Enum.GetNames(typeof(GridMode));
				
				for (int i = 0; i < names.Length; i++) 
				{
					GridMode value = (GridMode)Enum.Parse(typeof(GridMode),names[i]);
					bool selected = false;
					if(CurrentSettings.GridMode == value)
					{
						selected = true;
					}
					menu.AddItem (new GUIContent (names[i]), selected, OnSelectedGridOption, value);
				}
				
				menu.DropDown(gridRect);
			}

			if (Event.current.type == EventType.Repaint)
			{
				gridRect = GUILayoutUtility.GetLastRect();
				gridRect.width = 100;
			}

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            // Line Two
            GUILayout.BeginHorizontal();

			if(GUI.Button(new Rect(0,createBrushStyle.fixedHeight, 30, createBrushStyle.fixedHeight), SabreCSGResources.ButtonCylinderTexture, createBrushStyle))
			{
				CreatePrimitiveBrush(PrimitiveBrushType.Cylinder);
			}

			if(GUI.Button(new Rect(30,createBrushStyle.fixedHeight, 30, createBrushStyle.fixedHeight), SabreCSGResources.ButtonSphereTexture, createBrushStyle))
			{
				CreatePrimitiveBrush(PrimitiveBrushType.Sphere);
			}

            //if (GUI.Button(new Rect(60, createBrushStyle.fixedHeight, 30, createBrushStyle.fixedHeight), "", createBrushStyle))
            //{
            //}

            if (GUI.Button(new Rect(60,createBrushStyle.fixedHeight, 30, createBrushStyle.fixedHeight), "...", createBrushStyle))
			{
				GenericMenu menu = new GenericMenu ();

				List<Type> compoundBrushTypes = CompoundBrush.FindAllInAssembly();
				for (int i = 0; i < compoundBrushTypes.Count; i++) 
				{
					menu.AddItem (new GUIContent (compoundBrushTypes[i].Name), false, CreateCompoundBrush, compoundBrushTypes[i]);
				}

				menu.DropDown(new Rect(60,createBrushStyle.fixedHeight, 100, createBrushStyle.fixedHeight));
			}

			GUILayout.Space(92);

			// Display brush count
			GUILayout.Label(csgModel.BrushCount.ToStringWithSuffix(" brush", " brushes"), SabreGUILayout.GetLabelStyle());
//			CurrentSettings.GridMode = (GridMode)EditorGUILayout.EnumPopup(CurrentSettings.GridMode, EditorStyles.toolbarPopup, GUILayout.Width(80));

            if (Selection.activeGameObject != null)
            {
				BrushBase primaryBrush = Selection.activeGameObject.GetComponent<BrushBase>();
				List<BrushBase> brushes = new List<BrushBase>();
				for (int i = 0; i < Selection.gameObjects.Length; i++) 
				{
					BrushBase brush = Selection.gameObjects[i].GetComponent<BrushBase>();
					if (brush != null)
					{
						brushes.Add(brush);
					}
				}
                if (primaryBrush != null)
                {
					CSGMode brushMode = (CSGMode)EditorGUILayout.EnumPopup(primaryBrush.Mode, EditorStyles.toolbarPopup, GUILayout.Width(80));
					if(brushMode != primaryBrush.Mode)
					{
						bool anyChanged = false;

						foreach (BrushBase brush in brushes) 
						{
							Undo.RecordObject(brush, "Change Brush To " + brushMode);
							brush.Mode = brushMode;
							anyChanged = true;
						}
						if(anyChanged)
						{
							// Need to update the icon for the csg mode in the hierarchy
							EditorApplication.RepaintHierarchyWindow();
						}
					}


					bool[] noCSGStates = brushes.Select(brush => brush.IsNoCSG).Distinct().ToArray();
					bool isNoCSG = (noCSGStates.Length == 1) ? noCSGStates[0] : false;

					bool newIsNoCSG = SabreGUILayout.ToggleMixed(noCSGStates, "NoCSG", GUILayout.Width(53));


					bool[] collisionStates = brushes.Select(item => item.HasCollision).Distinct().ToArray();
					bool hasCollision = (collisionStates.Length == 1) ? collisionStates[0] : false;

					bool newHasCollision = SabreGUILayout.ToggleMixed(collisionStates, "Collision", GUILayout.Width(53));


					bool[] visibleStates = brushes.Select(item => item.IsVisible).Distinct().ToArray();
					bool isVisible = (visibleStates.Length == 1) ? visibleStates[0] : false;

					bool newIsVisible = SabreGUILayout.ToggleMixed(visibleStates, "Visible", GUILayout.Width(53));

					if(newIsNoCSG != isNoCSG)
					{
						foreach (BrushBase brush in brushes) 
						{
							Undo.RecordObject(brush, "Change Brush NoCSG Mode");
							brush.IsNoCSG = newIsNoCSG;						
						}
						// Tell the brushes that they have changed and need to recalc intersections
						foreach (BrushBase brush in brushes) 
						{
							brush.Invalidate(true);
						}

						EditorApplication.RepaintHierarchyWindow();
					}
					if(newHasCollision != hasCollision)
					{
						foreach (BrushBase brush in brushes) 
						{
							Undo.RecordObject(brush, "Change Brush Collision Mode");
							brush.HasCollision = newHasCollision;
						}
						// Tell the brushes that they have changed and need to recalc intersections
						foreach (BrushBase brush in brushes) 
						{
							brush.Invalidate(true);
						}
					}
					if(newIsVisible != isVisible)
					{
						foreach (BrushBase brush in brushes) 
						{
							Undo.RecordObject(brush, "Change Brush Visible Mode");
							brush.IsVisible = newIsVisible;
						}
						// Tell the brushes that they have changed and need to recalc intersections
						foreach (BrushBase brush in brushes) 
						{
							brush.Invalidate(true);
						}
						if(newIsVisible == false)
						{
							csgModel.NotifyPolygonsRemoved();
						}
					}
                }
            }

			GUILayout.Space(10);

			// Position snapping UI
			CurrentSettings.PositionSnappingEnabled = SabreGUILayout.Toggle(CurrentSettings.PositionSnappingEnabled, "Pos Snapping");
			CurrentSettings.PositionSnapDistance = EditorGUILayout.FloatField(CurrentSettings.PositionSnapDistance, GUILayout.Width(50));
			
			if (SabreGUILayout.Button("-", EditorStyles.miniButtonLeft))
			{
				CurrentSettings.ChangePosSnapDistance(.5f);
			}
			if (SabreGUILayout.Button("+", EditorStyles.miniButtonRight))
			{
				CurrentSettings.ChangePosSnapDistance(2f);
			}

			// Rotation snapping UI
			CurrentSettings.AngleSnappingEnabled = SabreGUILayout.Toggle(CurrentSettings.AngleSnappingEnabled, "Ang Snapping");
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

			GUILayout.FlexibleSpace();

			if (SabreGUILayout.Button("Prefs"))
			{
				SabreCSGPreferences.CreateAndShow();
			}

			if (SabreGUILayout.Button("Disable"))
			{
				Selection.activeGameObject = null;
				csgModel.EditMode = false;
			}

            GUILayout.EndHorizontal();
        }
    }
}
#endif