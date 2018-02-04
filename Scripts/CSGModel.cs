#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using System.Linq;
using System.Reflection;
using UnityEditor.Callbacks;

namespace Sabresaurus.SabreCSG
{
    [ExecuteInEditMode]
	public class CSGModel : CSGModelBase
	{
#if UNITY_EDITOR
		[SerializeField,HideInInspector]
		bool firstRun = true;

		const int MODEL_VERSION = 1;

		// Warning disabled as this field is not in use yet, but helps future proof some cases
#pragma warning disable 414 
		[SerializeField,HideInInspector]
		int modelVersion = 0;
#pragma warning restore 414
		
		[SerializeField,HideInInspector]
		bool autoRebuild = false;

		bool editMode = false;
        static CSGModel editModeModel = null;

		bool mouseIsDragging = false;
		bool mouseIsHeld = false;
		DateTime mouseReleaseTime = DateTime.MinValue;

		// Tools
		Tool activeTool = null;

		// Used to track what objects have been previously clicked on, so that the user can cycle click through objects
		// on the same (or similar) ray cast
		List<GameObject> previousHits = new List<GameObject>();
		List<GameObject> lastHitSet = new List<GameObject>();

		// Marked as serialized to persist through recompiles, used by draw tool
		[SerializeField,HideInInspector]
		Brush lastSelectedBrush = null;

		float currentFrameTimestamp = 0;
		float currentFrameDelta = 0;

		static bool anyCSGModelsInEditMode = false;

		static UnityEngine.Object[] deferredSelection = null;

		public float CurrentFrameDelta {
			get {
				return currentFrameDelta;
			}
		}

		Dictionary<MainMode, Tool> tools = new Dictionary<MainMode, Tool>()
		{
			{ MainMode.Resize, new ResizeEditor() },
			{ MainMode.Vertex, new VertexEditor() },
			{ MainMode.Face, new SurfaceEditor() },
			{ MainMode.Clip, new ClipEditor() },
			{ MainMode.Draw, new DrawEditor() },
		};

        Dictionary<OverrideMode, Tool> overrideTools = new Dictionary<OverrideMode, Tool>()
        {
            { OverrideMode.TransformModel, new TransformModelEditor() },
            //{ OverrideMode.Clip, new ClipEditor() },
            //{ OverrideMode.Draw, new DrawEditor() },
        };

        public bool MouseIsDragging 
		{
			get 
			{
				return mouseIsDragging;
			}
		}

		public bool MouseIsHeld 
		{
			get 
			{
				return mouseIsHeld;
			}
		}

		public bool MouseIsHeldOrRecent
		{
			get 
			{
				return mouseIsHeld || (MouseReleaseDuration < 0.22f);
			}
		}

		public double MouseReleaseDuration
		{
			get 
			{
				return (DateTime.UtcNow - mouseReleaseTime).TotalSeconds;
			}
		}

		public Brush LastSelectedBrush
		{
			get 
			{
				return lastSelectedBrush;
			}
		}

		public bool AutoRebuild {
			get {
				return autoRebuild;
			}
			set {
				autoRebuild = value;
			}
		}

		protected override void Start ()
		{
			UpdateUtility.RunCleanup();

			base.Start ();

			if(firstRun)
			{
				// Make sure editing is turned on
				EditMode = true;

                brushes = new List<Brush>(transform.GetComponentsInChildren<Brush>(false));

                // Only create a brush if the model doesn't already contain one
                if(brushes.Count == 0)
                {
                    // Create the default brush
                    GameObject newBrushObject = CreateBrush(PrimitiveBrushType.Cube, new Vector3(0, 1, 0));
                    // Set the selection to the new object
                    Selection.activeGameObject = newBrushObject;
                }

                firstRun = false;
				EditorHelper.SetDirty(this);
			}

			// Make sure we are correctly tracking whether any CSG Models are actually in edit mode
			bool wereAnyInEditMode = anyCSGModelsInEditMode;
			anyCSGModelsInEditMode = false;
			CSGModel[] csgModels = FindObjectsOfType<CSGModel>();

			// Loop through all the CSG Models in the scene and check if any are in edit mode
			for (int i = 0; i < csgModels.Length; i++) 
			{
				if(csgModels[i] != this
					&& csgModels[i].EditMode)
				{
					anyCSGModelsInEditMode = true;
				}
			}

			// If the status of whether any Models are in edit mode has changed, make sure all the brushes update their 
			// visibility. For example we moved from editing a CSG Model to opening another scene where no CSG Model is
			// in edit mode. wereAnyInEditMode would be true and anyCSGModelsInEditMode would now be false
			if(anyCSGModelsInEditMode != wereAnyInEditMode)
			{
                UpdateAllBrushesVisibility();
			}

			if(modelVersion < MODEL_VERSION)
			{
				// Upgrading or a new model, so grab all the brushes in case it's an upgrade
				brushes = new List<Brush>(transform.GetComponentsInChildren<Brush>(false));

				// Make sure all brushes have a valid brush cache and need rebuilding
				for (int i = 0; i < brushes.Count; i++) 
				{
					if(brushes[i] != null)
					{
						brushes[i].RecachePolygons(true);
					}
				}

				// Force all brushes to recalculate intersections
				for (int i = 0; i < brushes.Count; i++) 
				{
					if(brushes[i] != null)
					{
						brushes[i].RecalculateIntersections(brushes, false);
					}
				}

				// Finally now that the potential upgrade is complete, track that the model is the correct version now
				modelVersion = MODEL_VERSION;
			}
		}

        public override void Build(bool forceRebuild, bool buildInBackground)
        {
            // Build can take place if meshes are not saved to the DB, or if the scene is saved
            if(!buildSettings.SaveMeshesAsAssets || EnsureUntitledSceneHasBeenSaved("Scene must be saved for SaveMeshesAsAssets to work"))
            {
                base.Build(forceRebuild, buildInBackground);
            }
        }

        private bool EnsureUntitledSceneHasBeenSaved(string message)
        {
#if UNITY_5_6_OR_NEWER || UNITY_5_6
            return UnityEditor.SceneManagement.EditorSceneManager.EnsureUntitledSceneHasBeenSaved(message);
           
#elif UNITY_5_3_OR_NEWER
            if(string.IsNullOrEmpty(SceneManager.GetActiveScene().path)) // Scene not saved
            {
                // Ask the user to save
                UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                    
                // Check that the scene was saved
                if(!string.IsNullOrEmpty(SceneManager.GetActiveScene().path))
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
                return true;
            }
#else
            if(string.IsNullOrEmpty(EditorApplication.currentScene))
            {
                // Ask the user to save
                EditorApplication.SaveCurrentSceneIfUserWantsTo();

                // Check that the scene was saved
                if(!string.IsNullOrEmpty(EditorApplication.currentScene))
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
                return true;
            }
#endif
        }

		public override void OnBuildComplete ()
		{
			base.OnBuildComplete ();

			EditorUtility.ClearProgressBar();

            UpdateAllBrushesVisibility();

			EditorHelper.SetDirty(this);
			SetContextDirty();
		}

		public void OnSceneGUI(SceneView sceneView)
		{
			if(Event.current.type == EventType.ExecuteCommand)
			{				
				if(Event.current.commandName == "Duplicate")
				{
					if(EditorHelper.DuplicateSelection())
					{
						Event.current.Use();
					}
				}
			}
			Event e = Event.current;

			if(!EditMode)
			{
				return;
			}

			RadialMenu.OnEarlySceneGUI(sceneView);

			// Frame rate tracking
			if(e.type == EventType.Repaint)
			{
				currentFrameDelta = Time.realtimeSinceStartup - currentFrameTimestamp;
				currentFrameTimestamp = Time.realtimeSinceStartup;
			}

			// Raw checks for tracking mouse events (use raw so that consumed events are not ignored)
			if (e.rawType == EventType.MouseDown)
			{
				mouseIsDragging = false;
				mouseIsHeld = true;

				if(e.button == 0 && GUIUtility.hotControl == 0 )
				{
					GUIUtility.keyboardControl = 0;
				}

			}
			else if (e.rawType == EventType.MouseDrag)
			{
				mouseIsDragging = true;
			}
			else if (e.rawType == EventType.MouseUp)
			{
				mouseIsHeld = false;
				mouseReleaseTime = DateTime.UtcNow;
			}

//			if (CurrentSettings.BrushesVisible)
			{
				// No idea what this line of code means, but it seems to stop normal mouse selection
				HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
			}

			if(EditMode && CurrentSettings.OverrideMode != OverrideMode.TransformModel)
			{
				// In CSG mode, prevent the normal tools, so that the user must use our tools instead
				Tools.current = UnityEditor.Tool.None;
			}

			int concaveBrushCount = 0;
			for (int i = 0; i < brushes.Count; i++) 
			{
				if(brushes[i] != null && !brushes[i].IsBrushConvex)
				{
					concaveBrushCount++;
				}
			}
			if(concaveBrushCount > 0)
			{
				Toolbar.WarningMessage = concaveBrushCount + " Concave Brush" + (concaveBrushCount > 1 ? "es" : "") + " Detected";
			}
			else
			{
				//				Toolbar.WarningMessage = "";
			}

			Toolbar.CSGModel = this;
			Toolbar.OnSceneGUI(sceneView, e);

			if (e.type == EventType.Repaint)// || e.type == EventType.Layout)
			{				
				if(CurrentSettings.ShowExcludedPolygons)
				{
					List<Polygon> allPolygons = BuildContext.VisualPolygons ?? new List<Polygon>();
					List<Polygon> excluded = new List<Polygon>();
					for (int i = 0; i < allPolygons.Count; i++) 
					{
						if(allPolygons[i].UserExcludeFromFinal)
						{
							excluded.Add(allPolygons[i]);
						}
					}
					SabreCSGResources.GetExcludedMaterial().SetPass(0);

					Mesh mesh = new Mesh();
					List<int> indices = new List<int>();
					BrushFactory.GenerateMeshFromPolygons(excluded.ToArray(), ref mesh, out indices);

					Graphics.DrawMeshNow(mesh, Vector3.zero, Quaternion.identity);

//					SabreGraphics.DrawPolygons(new Color(1,1,0,0.65f), new Color(1,0.8f,0,0.9f), excluded.ToArray());
				}

				if (activeTool == null || activeTool.BrushesHandleDrawing)
				{
					SabreCSGResources.GetSelectedBrushMaterial().SetPass(0);
					// Selection
					GL.Begin(GL.LINES);
					Color outlineColor = Color.blue;

					for (int brushIndex = 0; brushIndex < brushes.Count; brushIndex++) 
					{
						Brush brush = brushes[brushIndex];
						if(brush == null)
						{
							continue;
						}
						GameObject brushGameObject = brush.gameObject;

						if(!brushGameObject.activeInHierarchy)
						{
							continue;
						}

						if (Selection.Contains(brushGameObject))
						{
							if (brushes[brushIndex].IsNoCSG)
							{
								outlineColor = new Color(1f,0.6f,1.0f);
							}
							else
							{
								if (brushes[brushIndex].Mode == CSGMode.Add)
								{
									outlineColor = Color.cyan;
								}
								else
								{
									outlineColor = Color.yellow;
								}
							}
						}
						else if(CurrentSettings.BrushesVisible)
						{
							if (brushes[brushIndex].IsNoCSG)
							{
								outlineColor = new Color(0.8f,0.3f,1.0f);
							}
							else
							{
								if (brushes[brushIndex].Mode == CSGMode.Add)
								{
									outlineColor = Color.blue;
								}
								else
								{
									outlineColor = new Color32(255, 130, 0, 255);
								}
							}
						}
						else
						{
							continue;
						}

						GL.Color(outlineColor);

						Polygon[] polygons = brush.GetPolygons();
						Transform brushTransform = brush.transform;

						// Brush Outline
						for (int i = 0; i < polygons.Length; i++)
						{
							Polygon polygon = polygons[i];

							for (int j = 0; j < polygon.Vertices.Length; j++)
							{
								Vector3 position = brushTransform.TransformPoint(polygon.Vertices[j].Position);
								GL.Vertex(position);

								if (j < polygon.Vertices.Length - 1)
								{
									Vector3 position2 = brushTransform.TransformPoint(polygon.Vertices[j + 1].Position);
									GL.Vertex(position2);
								}
								else
								{
									Vector3 position2 = brushTransform.TransformPoint(polygon.Vertices[0].Position);
									GL.Vertex(position2);
								}
							}
						}
					}

					GL.End();

					for (int i = 0; i < brushes.Count; i++)
					{
						if (brushes[i] is PrimitiveBrush && brushes[i] != null && brushes[i].gameObject.activeInHierarchy)
						{
							((PrimitiveBrush)brushes[i]).OnRepaint(sceneView, e);
						}
					}
				}
			}

			if (e.type == EventType.Repaint)
			{
				Rect rect = new Rect(0, 0, Screen.width, Screen.height);
				EditorGUIUtility.AddCursorRect(rect, SabreMouse.ActiveCursor);
			}

            if(e.type == EventType.Layout) 
            {
                // Don't need to do this for every OnSceneGUI call, so limit it to Layout events
                if (activeTool == null
                    || activeTool != GetActiveTool())
                {
                    UpdateActiveTool();
                }
            }
			
			if(activeTool != null)
			{
				activeTool.CSGModel = this;
				activeTool.SetSelection(Selection.activeGameObject, Selection.gameObjects);
				activeTool.OnSceneGUI(sceneView, e);
			}

//			if(e.type == EventType.DragPerform)
//			{
//				Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
//
//				RaycastHit hit = new RaycastHit();
//
//				int layerMask = 1 << LayerMask.NameToLayer("CSGMesh");
//				// Invert the layer mask
//				layerMask = ~layerMask;
//
//				// Shift mode means only add what they click (clicking nothing does nothing)
//				if (Physics.Raycast(ray, out hit, float.PositiveInfinity, layerMask))
//				{
//										OnDragDrop(hit.collider.gameObject);
//				}
//			}

			if (e.type == EventType.MouseUp && !RadialMenu.IsActive)
			{
				OnMouseUp(sceneView, e);
				SabreMouse.ResetCursor();
			}
			else if (e.type == EventType.KeyDown || e.type == EventType.KeyUp)
			{
				OnKeyAction(sceneView, e);
			}

			if(CurrentSettings.OverrideFlyCamera)
			{
				LinearFPSCam.OnSceneGUI(sceneView);
			}

			RadialMenu.OnLateSceneGUI(sceneView);
		}

		void OnMouseUp(SceneView sceneView, Event e)
		{
			if (mouseIsDragging 
				|| (activeTool != null && activeTool.PreventBrushSelection)
				|| EditorHelper.IsMousePositionInInvalidRects(e.mousePosition))
			{
                SceneView.RepaintAll();
				return;
			}

			// Left click - select
			if (e.button == 0)
			{
				Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
				List<PolygonRaycastHit> hits = RaycastBrushesAll(ray, true);
				List<Brush> hitBrushes = hits.Select(hit => hit.GameObject.GetComponent<Brush>()).ToList();
                List<BrushBase> hitBases = ExtractBrushBases(hitBrushes);
                List<GameObject> hitObjects = hitBases.Select(hit => hit.gameObject).ToList();

                GameObject selectedObject = null;

				if(hitObjects.Count == 0) // Didn't hit anything, blank the selection
				{
					previousHits.Clear();
					lastHitSet.Clear();
				}
				else if(hitObjects.Count == 1) // Only hit one thing, no ambiguity, this is what is selected
				{
					selectedObject = hitObjects[0];
					previousHits.Clear();
					lastHitSet.Clear();
				}
				else
				{
					if(!hitObjects.ContentsEquals(lastHitSet))
					{
						selectedObject = hitObjects[0];
						previousHits.Clear();
						lastHitSet = hitObjects;
					}
					else
					{
						// First try and select anything other than what has been previously hit
						for (int i = 0; i < hitObjects.Count; i++) 
						{
							if(!previousHits.Contains(hitObjects[i]))
							{
								selectedObject = hitObjects[i];
								break;
							}
						}

						// Only found previously hit objects
						if(selectedObject == null)
						{
							// Walk backwards to find the oldest previous hit that has been hit by this ray
							for (int i = previousHits.Count-1; i >= 0 && selectedObject == null; i--) 
							{
								for (int j = 0; j < hitObjects.Count; j++) 
								{
									if(hitObjects[j] == previousHits[i])
									{
										selectedObject = previousHits[i];
										break;
									}
								}
							}
						}
					}
				}

                if (selectedObject != null)
                {
                    previousHits.Remove(selectedObject);
                    // Most recent hit
                    previousHits.Insert(0, selectedObject);
                }

                if (EnumHelper.IsFlagSet(e.modifiers, EventModifiers.Shift)
					|| EnumHelper.IsFlagSet(e.modifiers, EventModifiers.Control)
					|| EnumHelper.IsFlagSet(e.modifiers, EventModifiers.Command))
				{
					List<UnityEngine.Object> objects = new List<UnityEngine.Object>(Selection.objects);

					if (objects.Contains(selectedObject))
					{
						objects.Remove(selectedObject);
					}
					else
					{
						objects.Add(selectedObject);
					}
					Selection.objects = objects.ToArray();
				}
				else
				{
					Selection.activeGameObject = selectedObject;
				}
				
				e.Use();
			}
		}

		/// <summary>
		/// Subscribes to both KeyDown and KeyUp events from the SceneView delegate. This allows us to easily store key
		/// events in one place and mark them as used as necessary (for example to prevent error sounds on key down)
		/// </summary>
		void OnKeyAction(SceneView sceneView, Event e)
		{
			OnGenericKeyAction(sceneView, e);
		}

		private void OnGenericKeyAction(SceneView sceneView, Event e)
		{
			if (KeyMappings.EventsMatch(e, Event.KeyboardEvent(KeyMappings.Instance.ToggleMode))
				|| KeyMappings.EventsMatch(e, Event.KeyboardEvent(KeyMappings.Instance.ToggleModeBack)))
			{
				// Toggle mode - immediately (key down)
				if (e.type == EventType.KeyDown)
				{
					int currentModeInt = (int)CurrentSettings.CurrentMode;
					int count = Enum.GetNames(typeof(MainMode)).Length;

					if(KeyMappings.EventsMatch(e, Event.KeyboardEvent(KeyMappings.Instance.ToggleModeBack)))
					{
						currentModeInt--;
					}
					else
					{
						currentModeInt++;
					}

					if (currentModeInt >= count)
					{
						currentModeInt = 0;
					}
					else if (currentModeInt < 0)
					{
						currentModeInt = count - 1;
					}
						
					SetCurrentMode((MainMode)currentModeInt);

					SceneView.RepaintAll();
				}
				e.Use();
			}
			else if (!MouseIsHeld && KeyMappings.EventsMatch(e, Event.KeyboardEvent(KeyMappings.Instance.ActivateClipMode)))
			{
				// Activate mode - immediately (key down)
				if (e.type == EventType.KeyDown && !MouseIsHeldOrRecent)
				{
					SetCurrentMode(MainMode.Clip);
//					SetOverrideMode(OverrideMode.Clip);

					SceneView.RepaintAll();
				}
				e.Use();
			}
			else if (!MouseIsHeld && KeyMappings.EventsMatch(e, Event.KeyboardEvent(KeyMappings.Instance.ActivateDrawMode)))
			{
				// Activate mode - immediately (key down)
				if (e.type == EventType.KeyDown && !MouseIsHeldOrRecent)
				{
					SetCurrentMode(MainMode.Draw);
//					SetOverrideMode(OverrideMode.Draw);

					SceneView.RepaintAll();
				}
				e.Use();
			}
			else if (!MouseIsHeld && KeyMappings.EventsMatch(e, Event.KeyboardEvent(KeyMappings.Instance.ActivateFaceMode)))
			{
				// Activate mode - immediately (key down)
				if (e.type == EventType.KeyDown && !MouseIsHeldOrRecent)
				{
					SetCurrentMode(MainMode.Face);
//					SetOverrideMode(OverrideMode.Face);

					SceneView.RepaintAll();
				}
				e.Use();
			}
			else if (!MouseIsHeld && KeyMappings.EventsMatch(e, Event.KeyboardEvent(KeyMappings.Instance.ActivateVertexMode)))
			{
				// Activate mode - immediately (key down)
				if (e.type == EventType.KeyDown && !MouseIsHeldOrRecent)
				{
					SetCurrentMode(MainMode.Vertex);
//					SetOverrideMode(OverrideMode.Vertex);

					SceneView.RepaintAll();
				}
				e.Use();
			}
            else if (!MouseIsHeld && KeyMappings.EventsMatch(e, Event.KeyboardEvent(KeyMappings.Instance.ActivateResizeMode)))
            {
                if (e.type == EventType.KeyDown && !MouseIsHeldOrRecent)
                {
                    SetCurrentMode(MainMode.Resize);
                    SceneView.RepaintAll();
                }
                e.Use();
            }
			else if (KeyMappings.EventsMatch(e, Event.KeyboardEvent(KeyMappings.Instance.IncreasePosSnapping)) 
				&& !SabreGUIHelper.AnyControlFocussed)
			{
				if (e.type == EventType.KeyUp)
				{
					CurrentSettings.ChangePosSnapDistance(2f);
					SceneView.RepaintAll();
				}
				e.Use();
			}
			else if (KeyMappings.EventsMatch(e, Event.KeyboardEvent(KeyMappings.Instance.DecreasePosSnapping))
				&& !SabreGUIHelper.AnyControlFocussed)
			{
				if (e.type == EventType.KeyUp)
				{
					CurrentSettings.ChangePosSnapDistance(.5f);
					SceneView.RepaintAll();
				}
				e.Use();
			}
			else if (KeyMappings.EventsMatch(e, Event.KeyboardEvent(KeyMappings.Instance.TogglePosSnapping)) 
				&& !SabreGUIHelper.AnyControlFocussed)
			{
				if (e.type == EventType.KeyUp)
				{
					CurrentSettings.PositionSnappingEnabled = !CurrentSettings.PositionSnappingEnabled;
					SceneView.RepaintAll();
				}
				e.Use();
			}
			else if (KeyMappings.EventsMatch(e, Event.KeyboardEvent(KeyMappings.Instance.IncreaseAngSnapping)) 
				&& !SabreGUIHelper.AnyControlFocussed)
			{
				if (e.type == EventType.KeyUp)
				{
					if(CurrentSettings.AngleSnapDistance >= 15)
					{
						CurrentSettings.AngleSnapDistance += 15;
					}
					else
					{
						CurrentSettings.AngleSnapDistance += 5;
					}
					SceneView.RepaintAll();
				}
				e.Use();
			}
			else if (KeyMappings.EventsMatch(e, Event.KeyboardEvent(KeyMappings.Instance.DecreaseAngSnapping)) 
				&& !SabreGUIHelper.AnyControlFocussed)
			{
				if (e.type == EventType.KeyUp)
				{
					if(CurrentSettings.AngleSnapDistance > 15)
					{
						CurrentSettings.AngleSnapDistance -= 15;
					}
					else
					{
						CurrentSettings.AngleSnapDistance -= 5;
					}
					SceneView.RepaintAll();
				}
				e.Use();
			}
			else if (KeyMappings.EventsMatch(e, Event.KeyboardEvent(KeyMappings.Instance.ToggleAngSnapping)) 
				&& !SabreGUIHelper.AnyControlFocussed)
			{
				if (e.type == EventType.KeyUp)
				{
					CurrentSettings.AngleSnappingEnabled = !CurrentSettings.AngleSnappingEnabled;
					SceneView.RepaintAll();
				}
				e.Use();
			}
			else if (KeyMappings.EventsMatch(e, Event.KeyboardEvent(KeyMappings.Instance.ToggleBrushesHidden))
				&& !SabreGUIHelper.AnyControlFocussed)
			{
				if (e.type == EventType.KeyUp)
				{
					CurrentSettings.BrushesHidden = !CurrentSettings.BrushesHidden;
                    UpdateAllBrushesVisibility();
					SceneView.RepaintAll();
				}
				e.Use();
			}
			else if (KeyMappings.EventsMatch(e, Event.KeyboardEvent(KeyMappings.Instance.EnableRadialMenu))
				&& !SabreGUIHelper.AnyControlFocussed)
			{
				if (e.type == EventType.KeyUp)
				{
					RadialMenu.IsActive = true;
					SceneView.RepaintAll();
				}
				e.Use();
			}
			else if(!MouseIsHeld && (KeyMappings.EventsMatch(e, Event.KeyboardEvent(KeyMappings.Instance.ChangeBrushToAdditive))
				|| KeyMappings.EventsMatch(e, Event.KeyboardEvent(KeyMappings.Instance.ChangeBrushToAdditive2)))
			)
			{
				if (e.type == EventType.KeyDown && !MouseIsHeldOrRecent)
				{
					bool anyChanged = false;

					for (int i = 0; i < Selection.gameObjects.Length; i++) 
					{
						BrushBase brushBase = Selection.gameObjects[i].GetComponent<BrushBase>();
						if (brushBase != null)
						{
							Undo.RecordObject(brushBase, "Change Brush To Add");
							brushBase.Mode = CSGMode.Add;
							anyChanged = true;
						}
					}
					if(anyChanged)
					{
						// Need to update the icon for the csg mode in the hierarchy
						EditorApplication.RepaintHierarchyWindow();
						SceneView.RepaintAll();
					}
				}
				e.Use();
			}
			else if(!MouseIsHeld && (KeyMappings.EventsMatch(e, Event.KeyboardEvent(KeyMappings.Instance.ChangeBrushToSubtractive))
				|| KeyMappings.EventsMatch(e, Event.KeyboardEvent(KeyMappings.Instance.ChangeBrushToSubtractive2)))
			)
			{
				if (e.type == EventType.KeyDown && !MouseIsHeldOrRecent)
				{
					bool anyChanged = false;

					for (int i = 0; i < Selection.gameObjects.Length; i++) 
					{
						BrushBase brushBase = Selection.gameObjects[i].GetComponent<BrushBase>();
						if (brushBase != null)
						{
							Undo.RecordObject(brushBase, "Change Brush To Subtract");
							brushBase.Mode = CSGMode.Subtract;
							anyChanged = true;
						}
					}
					if(anyChanged)
					{
						// Need to update the icon for the csg mode in the hierarchy
						EditorApplication.RepaintHierarchyWindow();
						SceneView.RepaintAll();
					}
				}
				e.Use();
			}
			else if(!MouseIsHeld && (KeyMappings.EventsMatch(e, Event.KeyboardEvent(KeyMappings.Instance.Group))
					&& !SabreGUIHelper.AnyControlFocussed)
			)
			{
				if (e.type == EventType.KeyDown && !MouseIsHeldOrRecent)
				{
					TransformHelper.GroupSelection();
				}
				e.Use();
			}
			else if(!MouseIsHeld && (KeyMappings.EventsMatch(e, Event.KeyboardEvent(KeyMappings.Instance.Ungroup))
				&& !SabreGUIHelper.AnyControlFocussed)
			)
			{
				if (e.type == EventType.KeyDown && !MouseIsHeldOrRecent)
				{
					TransformHelper.UngroupSelection();
				}
				e.Use();
			}
		}


#if !(UNITY_5_0 || UNITY_5_1)
		void OnSelectionChanged()
		{
            bool onlyCSGModelsSelected = true; // Does not include brushes
			bool anyCSGObjectsSelected = false; // Includes brushes and models
			bool anyNonCSGSelected = false;

			List<CSGModel> foundModels = new List<CSGModel>();
			Dictionary<CSGModel, List<UnityEngine.Object>> selectedBrushes = new Dictionary<CSGModel, List<UnityEngine.Object>>();

			for (int i = 0; i < Selection.gameObjects.Length; i++) 
			{
				// Skip any selected prefabs in the project window
				if(PrefabUtility.GetPrefabParent(Selection.gameObjects[i]) == null 
					&& PrefabUtility.GetPrefabObject(Selection.gameObjects[i].transform) != null)
				{
					continue;
				}

				PrimitiveBrush primitiveBrush = Selection.gameObjects[i].GetComponent<PrimitiveBrush>();
				CSGModel csgModel = Selection.gameObjects[i].GetComponent<CSGModel>();

                if(csgModel == null)
                {
                    // Something other than a CSG Model has been selected
                    onlyCSGModelsSelected = false;
                }

				if(primitiveBrush != null)
				{
					csgModel = primitiveBrush.GetCSGModel() as CSGModel;

					if(csgModel != null)
					{
						if(!foundModels.Contains(csgModel))
						{
							foundModels.Add(csgModel);
							selectedBrushes[csgModel] = new List<UnityEngine.Object>();
						}

						selectedBrushes[csgModel].Add(primitiveBrush.gameObject);
					}
				}

				if(csgModel != null) // If a CSG Model is selected, or a brush has told us about its parent CSG Model
				{
					anyCSGObjectsSelected = true;

					if(!foundModels.Contains(csgModel))
					{
						foundModels.Add(csgModel);
						selectedBrushes[csgModel] = new List<UnityEngine.Object>();
					}
				}
				else // None found, try recursing up to find a CSG Model
				{
					CSGModel[] parentCSGModels = Selection.gameObjects[i].GetComponentsInParent<CSGModel>(true);
					if(parentCSGModels.Length > 0)
					{
						csgModel = parentCSGModels[0];

						if(Selection.gameObjects[i].GetComponent<MeshFilter>() != null
							|| Selection.gameObjects[i].GetComponent<MeshCollider>() != null)
						{
							anyNonCSGSelected = true;
						}
						else
						{
							anyCSGObjectsSelected = true;

							if(!foundModels.Contains(csgModel))
							{
								foundModels.Add(csgModel);
								selectedBrushes[csgModel] = new List<UnityEngine.Object>();
							}
						}
					}
					else
					{
						anyNonCSGSelected = true;
					}
				}
			}

			if(anyCSGObjectsSelected)
			{
				CSGModel activeModel = null;
				if(foundModels.Count == 1)
				{
					if(!foundModels[0].EditMode)
					{
						foundModels[0].EditMode = true;
					}
					activeModel = foundModels[0];
				}
				else
				{
					bool anyActive = false;

					for (int i = 0; i < foundModels.Count; i++) 
					{
						if(foundModels[i].EditMode)
						{
							anyActive = true;
							activeModel = foundModels[i];
							break;
						}
					}

					if(!anyActive)
					{
						foundModels[0].EditMode = true;
						activeModel = foundModels[0];
					}
				}

				if(anyNonCSGSelected && activeModel != null)
				{
					deferredSelection = selectedBrushes[activeModel].ToArray();
				}

                if (this != null && this.EditMode) // Current model can be destroyed
                {
                    if (onlyCSGModelsSelected)
                    {
                        // Only CSG model has been selected, so show special UI
                        SetOverrideMode(OverrideMode.TransformModel);
                    }
                    else
                    {
                        if (CurrentSettings.OverrideMode == OverrideMode.TransformModel)
                        {
                            SetOverrideMode(OverrideMode.None);
                        }
                    } 
                }
			}
			else if(anyNonCSGSelected)
			{
				EditMode = false;
			}


			if(EditMode)
			{
				// Walk backwards until we find the last selected brush
				for (int i = Selection.gameObjects.Length-1; i >= 0; i--)
				{
					Brush brush = Selection.gameObjects[i].GetComponent<Brush>();

					if(brush != null)
					{
						lastSelectedBrush = brush;
						break;
					}
				}
			}
		}
#endif

		public void SetLastSelectedBrush(Brush brush)
		{
			lastSelectedBrush = brush;
		}

		void OnHierarchyItemGUI(int instanceID, Rect drawRect)
		{
			if(Event.current.type == EventType.ExecuteCommand)
			{				
				if(Event.current.commandName == "Duplicate")
				{
					if(EditorHelper.DuplicateSelection())
					{
						Event.current.Use();
					}
				}
			}

			GameObject gameObject = EditorUtility.InstanceIDToObject (instanceID) as GameObject;

			if(Event.current.type == EventType.DragPerform)
			{
				if(drawRect.Contains(Event.current.mousePosition))
				{
					if(gameObject != null)
					{
						OnDragDrop(gameObject);
					}
				}
			}


			if(gameObject != null)
			{
				BrushBase brushBase = gameObject.GetComponent<BrushBase>();
				if(brushBase != null)
				{
					drawRect.xMax -= 2;
					drawRect.xMin = drawRect.xMax - 16;
					drawRect.height = 16;

					Material iconMaterial = null;
					// Read only brushes have a greyscale icon
					if(brushBase is PrimitiveBrush)
					{
						if(((PrimitiveBrush)brushBase).IsReadOnly)
						{
							iconMaterial = SabreCSGResources.GetGreyscaleUIMaterial();
						}
					}
					if(brushBase.IsNoCSG)
					{
						Graphics.DrawTexture(drawRect, SabreCSGResources.NoCSGIconTexture, iconMaterial);
					}
					else if(brushBase.Mode == CSGMode.Add)
					{
						Graphics.DrawTexture(drawRect, SabreCSGResources.AddIconTexture, iconMaterial);
					}
					else
					{
						Graphics.DrawTexture(drawRect, SabreCSGResources.SubtractIconTexture, iconMaterial);
					}
				}


                if(EditMode)
                {
                    Event e = Event.current;

                    if (e.type == EventType.KeyDown || e.type == EventType.KeyUp)
                    {
                        OnGenericKeyAction(null, e);
                    }
                }
			}
		}

		int frameIndex;
		void OnEditorUpdate ()
		{
			if(deferredSelection != null)
			{
				Selection.objects = deferredSelection;
				deferredSelection = null;
				SceneView.RepaintAll();
				EditorApplication.RepaintHierarchyWindow();
			}

			if(EditMode)
			{
				frameIndex++;
				if(frameIndex > 1000)
				{
					frameIndex -= 1000;
				}

				if(AutoRebuild && gameObject.activeInHierarchy && this.enabled)
				{
//					if(frameIndex % 30 == 0)
					{
						Build(false, false);
					}
				}

				if(CurrentSettings.OverrideFlyCamera)
				{
					LinearFPSCam.OnUpdate();
				}
			}

			if(!Application.isPlaying)
			{
				bool buildOccurred = CSGFactory.Tick();
				if(buildOccurred)
				{
					OnBuildComplete();
				}
			}
		}

		protected override void Update()
		{
			if(editMode && !anyCSGModelsInEditMode)
			{
				anyCSGModelsInEditMode = true;
                UpdateAllBrushesVisibility();
			}
				
			base.Update();

			// Make sure the events we need to listen for are all bound (recompilation removes listeners, so it is
			// necessary to rebind dynamically)
			if(!EditorHelper.SceneViewHasDelegate(OnSceneGUI))
			{
				// Then resubscribe and repaint
				SceneView.onSceneGUIDelegate += OnSceneGUI;
				SceneView.RepaintAll();
			}

#if !(UNITY_5_0 || UNITY_5_1)
			if(!EditorHelper.HasDelegate(Selection.selectionChanged, (Action)OnSelectionChanged))
			{
				Selection.selectionChanged += OnSelectionChanged;
			}
#endif

			if(!EditorHelper.HasDelegate(EditorApplication.hierarchyWindowItemOnGUI, (EditorApplication.HierarchyWindowItemCallback)OnHierarchyItemGUI))
			{
				EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyItemGUI;
			}

			if (EditMode)
			{
				if(!EditorHelper.HasDelegate(EditorApplication.projectWindowItemOnGUI, (EditorApplication.ProjectWindowItemCallback)OnProjectItemGUI))
				{
					EditorApplication.projectWindowItemOnGUI += OnProjectItemGUI;
				}


				if(!EditorHelper.HasDelegate(EditorApplication.update, (EditorApplication.CallbackFunction)OnEditorUpdate))
				{
					EditorApplication.update += OnEditorUpdate;
				}

				if(!EditorHelper.HasDelegate(Undo.undoRedoPerformed, (Undo.UndoRedoCallback)OnUndoRedoPerformed))
				{
					Undo.undoRedoPerformed += OnUndoRedoPerformed;					
				}

				// Track whether all the brushes have been destroyed
				bool anyBrushes = false;

				if(brushes != null)
				{
					for (int i = 0; i < brushes.Count; i++) 
					{
						if(brushes[i] != null)
						{
							anyBrushes = true;
							break;
						}
					}
				}

				Toolbar.WarningMessage = "";
				Brush firstBrush = GetComponentInChildren<Brush>();
				if(firstBrush != null)
				{
					if(firstBrush.Mode == CSGMode.Subtract)
					{
						Toolbar.WarningMessage = "First brush must be additive";
					}
					//				anyBrushes = true;
				}

				// All the brushes have been destroyed so add a default cube brush
				if(!Application.isPlaying && !anyBrushes)
				{
					// Create the default brush
					//GameObject newBrushObject = CreateBrush(PrimitiveBrushType.Cube, new Vector3(0,1,0));
					//// Set the selection to the new object
					//Selection.activeGameObject = newBrushObject;
				}
			}
		}


		void OnDestroy()
		{
			EditorApplication.update -= OnEditorUpdate;
			EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyItemGUI;
			EditorApplication.projectWindowItemOnGUI -= OnProjectItemGUI;
			SceneView.onSceneGUIDelegate -= OnSceneGUI;
			Undo.undoRedoPerformed -= OnUndoRedoPerformed;

			GridManager.UpdateGrid();
		}

		public void RebindToOnSceneGUI()
		{
			// Unbind the delegate, then rebind to ensure our method gets called last
			SceneView.onSceneGUIDelegate -= OnSceneGUI;
			SceneView.onSceneGUIDelegate += OnSceneGUI;
		}

		public void ExportOBJ (bool limitToSelection)
		{
            List<Polygon> polygonsToExport = new List<Polygon>();

            if (limitToSelection)
            {
                for (int i = 0; i < Selection.gameObjects.Length; i++)
                {
                    PrimitiveBrush brush = Selection.gameObjects[i].GetComponent<PrimitiveBrush>();
                    if (brush != null) // If there's actually a brush attached to the game object
                    {
                        polygonsToExport.AddRange(brush.BrushCache.BuiltVisualPolygons.DeepCopy());
                    }
                }
            }
            else
            {
                if (buildContext.VisualPolygons != null)
                {
                    polygonsToExport = buildContext.VisualPolygons.DeepCopy();
                }
            }

            if(polygonsToExport.Count > 0)
            {
                string path = EditorUtility.SaveFilePanel("Save Geometry As OBJ", "Assets", this.name + ".obj", "obj");
                if (!string.IsNullOrEmpty(path))
                {
                    OBJFactory.ExportToFile(path, transform, polygonsToExport, GetDefaultMaterial());
                    AssetDatabase.Refresh();
                }
            }
            else
            {
                // Nothing to export
                if (limitToSelection)
                {
                    EditorUtility.DisplayDialog("Nothing to export", "No brushes selected corresponding to built geometry", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Nothing to export", "No built geometry found to export", "OK");
                }
            }
        }

        public static string GetSabreCSGPath()
		{
			// Find all the scripts with CSGModel in their name
			string[] guids = AssetDatabase.FindAssets("CSGModel t:Script");

			foreach (string guid in guids) 
			{
				// Find the path of the file
				string path = AssetDatabase.GUIDToAssetPath(guid);

				string suffix = "Scripts/CSGModel.cs";
				// If it is the target file, i.e. CSGModel.cs not CSGModelInspector
				if(path.EndsWith(suffix))
				{
					// Remove the suffix, to get for example Assets/SabreCSG
					path = path.Remove(path.Length-suffix.Length, suffix.Length);

					return path;
				}
			}

			// None matched
			return string.Empty;
		}

		/// <summary>
		/// Marks the Build Context associated with this CSG Model as changed
		/// </summary>
		public void SetContextDirty()
		{
			EditorHelper.SetDirty(buildContextBehaviour);
		}

		public void UndoRecordContext(string name)
		{
			Undo.RecordObject(buildContextBehaviour, name);
		}

        Tool GetActiveTool()
        {
            if (CurrentSettings.OverrideMode != OverrideMode.None)
            {
                return overrideTools[CurrentSettings.OverrideMode];
            }
            else
            {
                return tools[CurrentSettings.CurrentMode];
            }
        }

		private void UpdateActiveTool()
		{
			Tool lastTool = activeTool;

            activeTool = GetActiveTool();

			if(activeTool != lastTool)
			{
				if(lastTool != null)
				{
					lastTool.Deactivated();
				}
				activeTool.ResetTool();
			}
            UpdateAllBrushesVisibility();
        }

		public Tool GetTool(MainMode mode)
		{
			return tools[mode];
		}

		public void SetCurrentMode(MainMode newMode)
		{
            if(CurrentSettings.OverrideMode == OverrideMode.TransformModel)
            {
                // If we're leaving TransformModel 
                bool onlyModelsSelected = true;
                for (int i = 0; i < Selection.gameObjects.Length; i++)
                {
                    if(Selection.gameObjects[i].GetComponent<CSGModel>() == null)
                    {
                        onlyModelsSelected = false;
                        break;
                    }
                }
                // Deselect if only CSG Models are selected
                if(onlyModelsSelected)
                {
                    Selection.activeGameObject = null;
                }
            }
			CurrentSettings.OverrideMode = OverrideMode.None;

			CurrentSettings.CurrentMode = newMode;

			UpdateActiveTool();			
		}

		public void SetOverrideMode(OverrideMode newMode)
		{
			CurrentSettings.OverrideMode = newMode;

			UpdateActiveTool();
		}

		public void ExitOverrideMode()
		{
			CurrentSettings.OverrideMode = OverrideMode.None;

			UpdateActiveTool();
		}

		public bool EditMode
		{
			get
			{
				return this.editMode;
			}
			set
			{
				// Has edit mode changed
				if (editMode != value)
				{
					editMode = value;

                    editModeModel = this;

                    CSGModel[] csgModels = Resources.FindObjectsOfTypeAll<CSGModel>();

					if (value == true) // Edit mode enabled
					{
						// If there are any other CSG Models in the scene, disabling their editing
						if(csgModels.Length > 1)
						{
							for (int i = 0; i < csgModels.Length; i++) 
							{
								if(csgModels[i] != this)
								{
									csgModels[i].EditMode = false;
								}
							}
						}

						// This CSG Model is now in edit mode, so we know for sure one is
						anyCSGModelsInEditMode = true;

						// Bind listeners
						EditorApplication.update += OnEditorUpdate;

						// Force the scene views to repaint (shows our own UI)
						SceneView.RepaintAll();

						//						if(Event.current != null)
						//						{
						//							Event.current.Use();
						//						}

						//                        SceneView.onSceneGUIDelegate += OnSceneGUI;


					}
					else // Edit mode disabled
					{
						// Unbind listeners
						EditorApplication.update -= OnEditorUpdate;

						//                        SceneView.onSceneGUIDelegate -= OnSceneGUI;

						// Force the scene views to repaint (hides our own UI)
						SceneView.RepaintAll();
						//                        HandleUtility.Repaint();

						// This CSG Model is no longer in edit mode, so find out if any are
						anyCSGModelsInEditMode = false;
						if(csgModels.Length > 1)
						{
							for (int i = 0; i < csgModels.Length; i++) 
							{
								if(csgModels[i] != this
									&& csgModels[i].EditMode)
								{
                                    editModeModel = csgModels[i];
									anyCSGModelsInEditMode = true;
								}
							}
						}
					}

                    UpdateAllBrushesVisibility();

					GridManager.UpdateGrid();
				}
			}
		}



		void OnProjectItemGUI (string guid, Rect selectionRect)
		{
			//										Debug.Log(Event.current.type.ToString());
			/*
			if (Event.current.type == EventType.MouseDrag)
			{
				if(selectionRect.Contains(Event.current.mousePosition))
				{
					//					Debug.Log(Event.current.type.ToString());
					string path = AssetDatabase.GUIDToAssetPath (guid);
					if(!string.IsNullOrEmpty(path))
					{
						DragAndDrop.PrepareStartDrag();
						DragAndDrop.paths = new string[] { path };

						DragAndDrop.StartDrag ("Dragging material");
						
						// Make sure no one else uses this event
						Event.current.Use();
					}
				}
			}
			*/
		}

		void OnDragDrop(GameObject gameObject)
		{
			//			PrimitiveBrush brush = gameObject.GetComponent<PrimitiveBrush>();
			//			
			//			if(brush != null)
			//			{
			//				if(DragAndDrop.objectReferences.Length == 1)
			//				{
			//					if(DragAndDrop.objectReferences[0] is Material)
			//					{
			//						brush.Material = (Material)DragAndDrop.objectReferences[0];
			//						DragAndDrop.AcceptDrag();
			//						Event.current.Use();
			//					}
			//				}
			//			}
		}

		private void OnUndoRedoPerformed()
		{
			// An undo or redo operation may restore a brush, so make sure we track all
			brushes = new List<Brush>(transform.GetComponentsInChildren<Brush>(false));

			BrushBase[] brushBases = transform.GetComponentsInChildren<BrushBase>(false);

			// Tell each brush base that an undo/redo has been performed so it can make sure the render mesh is updated
			for (int i = 0; i < brushBases.Length; i++) 
			{
				if(brushBases[i] != null)
				{
					brushBases[i].OnUndoRedoPerformed();
				}
			}

			activeTool.OnUndoRedoPerformed();

			// If the user undos or redos a face change then a shared mesh may be updated.
			// Unity won't automatically refresh the mesh filters that use a shared mesh, so we need to force refresh
			// all of them so they fetch the latest revision of the mesh
			Transform meshGroup = GetMeshGroupTransform();

			if(meshGroup != null)
			{
				MeshFilter[] meshFilters = meshGroup.GetComponentsInChildren<MeshFilter>();
				for (int i = 0; i < meshFilters.Length; i++) 
				{
					meshFilters[i].ForceRefreshSharedMesh();
				}
			}

			EditorApplication.RepaintHierarchyWindow();
		}

		public override bool AreBrushesVisible {
			get {
				if(!Application.isPlaying)
				{
                    return CurrentSettings.BrushesVisible
                        && anyCSGModelsInEditMode 
                        && editModeModel != null 
                        && (editModeModel.activeTool == null || editModeModel.activeTool.BrushesHandleDrawing);
				}
				return base.AreBrushesVisible;
			}
		}

        public static void UpdateAllBrushesVisibility()
        {
            CSGModel[] csgModels = Resources.FindObjectsOfTypeAll<CSGModel>();
            for (int i = 0; i < csgModels.Length; i++) 
            {
                Transform meshGroup = csgModels[i].transform.Find("MeshGroup");

                if(meshGroup != null)
                {
                    meshGroup.gameObject.SetActive(!CurrentSettings.MeshHidden);
                }

                List<Brush> brushes = csgModels[i].brushes;
                for (int j = 0; j < brushes.Count; j++)
                {
                    if (brushes[j] != null)
                    {
                        brushes[j].UpdateVisibility();
                    }
                }
            }
        }



		public override Material GetDefaultFallbackMaterial ()
		{
			if(!Application.isPlaying)
			{
				// To allow users to move the SabreCSG folder, we must base the material loading on the asset path
				return AssetDatabase.LoadMainAssetAtPath(GetSabreCSGPath() + "Resources/" + DEFAULT_FALLBACK_MATERIAL_PATH + ".mat") as Material;
			}
			else
			{
				return base.GetDefaultFallbackMaterial ();
			}
		}

		public override void OnBuildProgressChanged (float progress)
		{
			base.OnBuildProgressChanged (progress);

			EditorUtility.DisplayProgressBar("Building", "Building geometry from brushes", progress);
		}

		private void BreakMeshTest(Mesh mesh)
		{
			Vector3[] vertices = new Vector3[mesh.triangles.Length];
			Vector3[] normals = new Vector3[mesh.triangles.Length];
			Color32[] colors32 = new Color32[mesh.triangles.Length];
			Vector2[] uvs = new Vector2[mesh.triangles.Length];
			Vector4[] tangents = new Vector4[mesh.triangles.Length];
			int[] triangles = new int[mesh.triangles.Length];
			for (int i = 0; i < mesh.triangles.Length; i++) 
			{
				int oldIndex = mesh.triangles[i];
				vertices[i] = mesh.vertices[oldIndex];
				normals[i] = mesh.normals[oldIndex];
				colors32[i] = mesh.colors32[oldIndex];
				uvs[i] = mesh.uv[oldIndex];
				tangents[i] = mesh.tangents[oldIndex];
				triangles[i] = i;
			}
			mesh.vertices = vertices;
			mesh.normals = normals;
			mesh.colors32 = colors32;
			mesh.uv = uvs;
			mesh.tangents = tangents;
			mesh.triangles = triangles;
		}

		public override void OnFinalizeVisualMesh (GameObject newGameObject, Mesh mesh)
		{
			base.OnFinalizeVisualMesh (newGameObject, mesh);

			if (buildSettings.GenerateLightmapUVs)
			{
//				BreakMeshTest(mesh);

				// Create a copy of the mesh, which we can then unwrap
				Mesh meshCopy = new Mesh();

				meshCopy.vertices = mesh.vertices;
				meshCopy.normals = mesh.normals;
				meshCopy.colors32 = mesh.colors32;
				meshCopy.uv = mesh.uv;
				meshCopy.tangents = mesh.tangents;
				meshCopy.triangles = mesh.triangles;

				//				Vector2[] perTriangleUVs = UnityEditor.Unwrapping.GeneratePerTriangleUV(meshCopy);			

				UnityEditor.UnwrapParam unwrapParameters = new UnityEditor.UnwrapParam()
				{
					angleError = buildSettings.UnwrapAngleError,
					areaError = buildSettings.UnwrapAreaError,
					hardAngle = buildSettings.UnwrapHardAngle,
					packMargin = buildSettings.UnwrapPackMargin,
				};

				// Unwrap the mesh copy, note that this also changes the vertex buffer, which is why we do it on a copy
				UnityEditor.Unwrapping.GenerateSecondaryUVSet(meshCopy, unwrapParameters);

				// Now transfer the unwrapped UVs to the original mesh, since the two vertex counts differ
				Vector2[] uv2 = new Vector2[mesh.vertices.Length];

				if(meshCopy.triangles.Length == mesh.triangles.Length)
				{
//					VisualDebug.ClearAll();
//					for (int i = 0; i < meshCopy.triangles.Length/3; i++) 
//					{
//						int index1 = meshCopy.triangles[i*3 + 0];
//						int index2 = meshCopy.triangles[i*3 + 1];
//						int index3 = meshCopy.triangles[i*3 + 2];
////						if(index1 == index2 || index2 == index3 || index1 == index3)
////						{
////							Debug.LogError("Degen found");
////						}
//						VisualDebug.AddLinePolygon(new Vector3[]{
//							meshCopy.uv2[index1],
//							meshCopy.uv2[index2],
//							meshCopy.uv2[index3],
//						}, Color.white);
//					}

					int triangleCount = mesh.triangles.Length;
					for (int i = 0; i < triangleCount; i++) 
					{
						int originalIndex = mesh.triangles[i];
						int copyIndex = meshCopy.triangles[i];

//						if(mesh.vertices[originalIndex] != meshCopy.vertices[copyIndex])
//						{
//							Debug.LogError("Vertex mismatch found");
//						}
//
//						if(uv2[originalIndex] != Vector2.zero
//							&& uv2[originalIndex] != meshCopy.uv2[copyIndex])
//						{
//							Debug.Log("Overwriting " + uv2[originalIndex] + " with " + meshCopy.uv2[copyIndex]);
//						}
						uv2[originalIndex] = meshCopy.uv2[copyIndex];

					}


					mesh.uv2 = uv2;


//					for (int i = 0; i < mesh.triangles.Length/3; i++) 
//					{
//						int index1 = mesh.triangles[i*3 + 0];
//						int index2 = mesh.triangles[i*3 + 1];
//						int index3 = mesh.triangles[i*3 + 2];
////						if(index1 == index2 || index2 == index3 || index1 == index3)
////						{
////							Debug.LogError("Degen found");
////						}
//						VisualDebug.AddLinePolygon(new Vector3[]{
//							mesh.uv2[index1] + Vector2.right,
//							mesh.uv2[index2] + Vector2.right,
//							mesh.uv2[index3] + Vector2.right,
//						}, Color.blue);
//					}
//					for (int i = 0; i < uv2.Length/3; i++) 
//					{
//						VisualDebug.AddLinePolygon(new Vector3[]{
//							uv2[i*3 + 0] + Vector2.right,
//							uv2[i*3 + 1] + Vector2.right,
//							uv2[i*3 + 2] + Vector2.right,
//						}, Color.blue);
//					}
				}
				else
				{
					Debug.LogError("Unwrapped mesh triangle count mismatches source mesh");
				}

				//				GameObject.DestroyImmediate(meshCopy);
			}

			// Apply the model layer, tag and appropriate static flags 
			ApplyModelAttributes(newGameObject, true);
		}

		public override void OnFinalizeCollisionMesh (GameObject newGameObject, Mesh mesh)
		{
			base.OnFinalizeCollisionMesh (newGameObject, mesh);

			// Apply the model layer, tag and appropriate static flags 
			ApplyModelAttributes(newGameObject, false);
		}


		public Mesh GetMeshForMaterial(Material sourceMaterial, int fitVertices = 0)
		{
			if(materialMeshDictionary.Contains(sourceMaterial))
			{
				List<MaterialMeshDictionary.MeshObjectMapping> mappings = materialMeshDictionary[sourceMaterial];

				Mesh lastMesh = mappings[mappings.Count-1].Mesh;

				if(lastMesh.vertices.Length + fitVertices < MESH_VERTEX_LIMIT)
				{
					return lastMesh;
				}
			}

			Mesh mesh = new Mesh();

			materialMeshDictionary.Add(sourceMaterial, mesh, null);

			Material materialToApply = sourceMaterial;
			if(sourceMaterial == null)
			{
				materialToApply = GetDefaultMaterial();
			}

			GameObject newGameObject = CSGFactory.CreateMaterialMesh(this.transform, materialToApply, mesh);

			// Apply the model layer, tag and appropriate static flags 
			ApplyModelAttributes(newGameObject, false);

			return mesh;
		}

		public Mesh GetMeshForCollision(int fitVertices = 0)
		{
			Mesh lastMesh = collisionMeshDictionary[collisionMeshDictionary.Count-1];
			if(lastMesh.vertices.Length + fitVertices < MESH_VERTEX_LIMIT)
			{
				return lastMesh;
			}

			Mesh mesh = new Mesh();
			collisionMeshDictionary.Add(mesh);
			GameObject newGameObject = CSGFactory.CreateCollisionMesh(this.transform, mesh);

			// Apply the model layer, tag and appropriate static flags 
			ApplyModelAttributes(newGameObject, false);

			return mesh;
		}

		void ApplyModelAttributes(GameObject newGameObject, bool isVisual)
		{
			// Inherit any static flags from the CSG model
			UnityEditor.StaticEditorFlags rootStaticFlags = UnityEditor.GameObjectUtility.GetStaticEditorFlags(this.gameObject);
			UnityEditor.GameObjectUtility.SetStaticEditorFlags(newGameObject, rootStaticFlags);

			// Inherit the layer and tag from the CSG model
			newGameObject.layer = this.gameObject.layer;
			newGameObject.tag = this.gameObject.tag;

			// If the mesh is lightmap UV'd then make sure the object is lightmap static
			if(buildSettings.GenerateLightmapUVs)
			{
				UnityEditor.StaticEditorFlags staticFlags = UnityEditor.GameObjectUtility.GetStaticEditorFlags(newGameObject);
				staticFlags |= UnityEditor.StaticEditorFlags.LightmapStatic;
				UnityEditor.GameObjectUtility.SetStaticEditorFlags(newGameObject, staticFlags);
			}
		}

        public static CSGModel GetActiveCSGModel()
        {
            CSGModel[] csgModels = FindObjectsOfType<CSGModel>();

            // Return the first csg model that is currently being edited
            for (int i = 0; i < csgModels.Length; i++)
            {
                if (csgModels[i].EditMode)
                {
                    return csgModels[i];
                }
            }

            // None found
            return null;
        }

		static void CleanupForBuild(Transform csgModelTransform)
		{
			Transform meshGroup = csgModelTransform.Find("MeshGroup");
			if(meshGroup != null)
			{
				// Reanchor the meshes to the parent of the CSG Model
                meshGroup.SetParent(csgModelTransform.parent, true);
			}

			// Remove the CSG Model and its brushes
			DestroyImmediate (csgModelTransform.gameObject);	
		}

		[PostProcessScene(1)]
		public static void OnPostProcessScene()
		{
            CSGModel[] csgModels = Resources.FindObjectsOfTypeAll<CSGModel>();
			for (int i = 0; i < csgModels.Length; i++) 
			{
				CleanupForBuild(csgModels[i].transform);
			}
		}

#if UNITY_EDITOR && (UNITY_5_0 || UNITY_5_1)
		void OnDrawGizmosSelected()
		{
			// Ensure Edit Mode is on
			EditMode = true;
            editModeModel = this;
		}
#endif
#endif
	}
}
#endif