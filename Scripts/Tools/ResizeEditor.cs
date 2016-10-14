#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

namespace Sabresaurus.SabreCSG
{
    public class ResizeEditor : Tool
    {
		// Whether in our special resize mode, or using standard Unity position/rotation handles
		enum WidgetMode { Bounds, Translate, Rotate };

		// Used in Resize WidgetMode, for example to rotate using the resize handle
		enum ActiveMode { None, Resize, Translate, Rotate };

		ActiveMode currentMode = ActiveMode.None;
		WidgetMode widgetMode = WidgetMode.Bounds;

        ResizeHandlePair? selectedResizeHandlePair = null;
        int selectedResizePointIndex = -1; // -1 is unset, 0 is Point1, 1 is Point2

        ResizeHandlePair? hoveredResizeHandlePair = null;
        int hoveredResizePointIndex = -1; // -1 is unset, 0 is Point1, 1 is Point2

        Vector3 dotOffset3;

		float fullDeltaAngle = 0;
		float unroundedDeltaAngle = 0;

		Vector3 initialRotationDirection;

		Plane translationPlane;

		Vector3 originalPosition; // For duplicating when translating

		bool moveCancelled = false;

		bool duplicationOccured = false;

		Vector3 translationUnrounded = Vector3.zero;

		// WidgetMode - Translate
		Vector3 startPosition;

		string message; // The tip message being displayed e.g. the size or current delta angle

		// Used in OnWidgetRotation so that when you first interact with a rotation arc we get rid of any existing
		// delta. Ideally we wouldn't need to do this.
		Quaternion? initialRotationOffset = null; 

		bool isMarqueeSelection = false; // Whether the user is (or could be) dragging a marquee box
        bool marqueeCancelled = false;

		Vector2 marqueeStart;
		Vector2 marqueeEnd;

        ResizeHandlePair[] resizeHandlePairs = new ResizeHandlePair[]
		{
			// Edge Mid Points
			new ResizeHandlePair(new Vector3(0,1,1)),
			new ResizeHandlePair(new Vector3(0,-1,1)),
			new ResizeHandlePair(new Vector3(1,0,1)),
			new ResizeHandlePair(new Vector3(-1,0,1)),
			new ResizeHandlePair(new Vector3(1,1,0)),
			new ResizeHandlePair(new Vector3(-1,1,0)),
			
			// Face Mid Points
			new ResizeHandlePair(new Vector3(1,0,0)),
			new ResizeHandlePair(new Vector3(0,1,0)),
			new ResizeHandlePair(new Vector3(0,0,1)),
		};

        public override void OnSceneGUI(SceneView sceneView, Event e)
        {
			base.OnSceneGUI(sceneView, e); // Allow the base logic to calculate first

			if (e.type == EventType.KeyDown || e.type == EventType.KeyUp)
			{
				OnKeyAction(sceneView, e);
			}


			if(widgetMode == WidgetMode.Translate)
			{
				OnWidgetTranslation();
			}
			else if(widgetMode == WidgetMode.Rotate)
			{
				OnWidgetRotation();
			}

			if(e.button == 0 || e.button == 1)
			{
	            if (e.type == EventType.MouseDown)
	            {
	                OnMouseDown(sceneView, e);
	            }
				else if(e.type == EventType.MouseMove)
				{
					OnMouseMove(sceneView, e);
				}
	            else if (e.type == EventType.MouseDrag)
	            {
	                OnMouseDrag(sceneView, e);
	            }
				// If you mouse up on a different scene view to the one you started on it's surpressed as Ignore, when
				// doing marquee selection make sure to check the real type
				else if (e.type == EventType.MouseUp || (isMarqueeSelection && e.rawType == EventType.MouseUp))
	            {
	                OnMouseUp(sceneView, e);
	            }
			}

				
            if (e.type == EventType.Layout || e.type == EventType.Repaint)
            {
                OnRepaint(sceneView, e);
            }
        }

		void OnWidgetTranslation()
		{			
			if(primaryTargetBrushBase == null)
			{
				return;
			}

			// Make the handle respect the Unity Editor's Local/World orientation mode
			Quaternion handleDirection = Quaternion.identity;
			if(Tools.pivotRotation == PivotRotation.Local)
			{
				handleDirection = primaryTargetBrushTransform.rotation;
			}

			// Grab a source point and convert from local space to world
			Vector3 sourceWorldPosition = GetBrushesPivotPoint();//targetBrushTransform.position;

			EditorGUI.BeginChangeCheck();
			// Display a handle and allow the user to determine a new position in world space
			Vector3 newWorldPosition = Handles.PositionHandle(sourceWorldPosition, handleDirection);

			if(EditorGUI.EndChangeCheck())
			{
				Vector3 delta = newWorldPosition-sourceWorldPosition;

				Vector3 newPosition = primaryTargetBrushTransform.position + delta;

				Vector3 accumulatedDelta = newPosition - startPosition;

				if(CurrentSettings.PositionSnappingEnabled)
				{
					accumulatedDelta = InverseTransformDirection(accumulatedDelta);

					float snapDistance = CurrentSettings.PositionSnapDistance;

					accumulatedDelta = MathHelper.RoundVector3(accumulatedDelta, snapDistance);

					accumulatedDelta = TransformDirection(accumulatedDelta);
				}

				newPosition = startPosition + accumulatedDelta;

				Vector3 finalDelta = newPosition - primaryTargetBrushTransform.position;

				TranslateBrushes(finalDelta);

				Event.current.Use();
			}
			else
			{
				startPosition = primaryTargetBrushTransform.position;
			}
		}

		void OnWidgetRotation()
		{
			if(primaryTargetBrushBase == null)
			{
				return;
			}

			Vector3 worldPosition = GetBrushesPivotPoint();

			if (Event.current.type == EventType.MouseUp)
			{
				initialRotationOffset = null;
			}

			DrawRotationAxis(Color.red, Vector3.right, worldPosition);
			DrawRotationAxis(Color.green, Vector3.up, worldPosition);
			DrawRotationAxis(Color.blue, Vector3.forward, worldPosition);
		}

//		Quaternion compoundRotation = Quaternion.identity;

		void DrawRotationAxis(Color color, Vector3 axis, Vector3 worldPosition)
		{
//			EventType source = Event.current.rawType;
			// Make the handle respect the Unity Editor's Local/World orientation mode
//			Quaternion handleDirection = Quaternion.identity;
//			if(Tools.pivotRotation == PivotRotation.Local)
//			{
//				handleDirection = targetBrush.transform.rotation;
//			}

			// Grab a source point and convert from local space to world
			Vector3 sourceWorldPosition = worldPosition;

			EditorGUI.BeginChangeCheck();
			// Display a handle and allow the user to determine a new position in world space

//			Vector3 lastEulerAngles = handleDirection.eulerAngles;

			Handles.color = color;

			float snapValue = 0;
			if(CurrentSettings.AngleSnappingEnabled)
			{
				snapValue = CurrentSettings.AngleSnapDistance;
			}

			Quaternion sourceRotation = Quaternion.identity;// targetBrushTransform.rotation;
//			Quaternion sourceRotation = targetBrushTransform.rotation;

			Quaternion newRotation = Handles.Disc(sourceRotation, 
				sourceWorldPosition, 
				axis, 
				HandleUtility.GetHandleSize(sourceWorldPosition), 
				true, 
				snapValue);
			
			if(EditorGUI.EndChangeCheck())
			{
				Quaternion deltaRotation = Quaternion.Inverse(primaryTargetBrushTransform.rotation) * newRotation;
				if(!initialRotationOffset.HasValue)
				{
					initialRotationOffset = deltaRotation;
					return;
				}
				deltaRotation = Quaternion.Inverse(initialRotationOffset.Value) * deltaRotation;
//				Quaternion deltaRotation = newRotation;
//				Quaternion deltaRotation = newRotation * Quaternion.Inverse(targetBrushTransform.rotation);

//				Debug.Log(deltaRotation.eulerAngles);
				if(CurrentSettings.AngleSnappingEnabled)
				{
					Quaternion plusSnap = Quaternion.AngleAxis(CurrentSettings.AngleSnapDistance, axis);// * baseRotation;
					Quaternion zeroSnap = Quaternion.identity;// * baseRotation;
					Quaternion negativeSnap = Quaternion.AngleAxis(-CurrentSettings.AngleSnapDistance, axis);// * baseRotation;

					float angleZero = Quaternion.Angle(deltaRotation, zeroSnap);
					float anglePlus = Quaternion.Angle(deltaRotation, plusSnap);
					float angleNegative = Quaternion.Angle(deltaRotation, negativeSnap);

//					Debug.Log("A0 " + angleZero + ", A+ " + anglePlus + ", A- " + angleNegative);

					if(anglePlus < angleZero)
					{
						RotateBrushes(plusSnap, sourceWorldPosition);
					}
					else if(angleNegative < angleZero)
					{
						RotateBrushes(negativeSnap, sourceWorldPosition);
					}
				}
				else
				{
					RotateBrushes(deltaRotation, sourceWorldPosition);
				}

				Event.current.Use();
			}
		}

		void OnKeyAction(SceneView sceneView, Event e)
		{
			if(widgetMode == WidgetMode.Bounds)
			{
				if (primaryTargetBrushBase != null 
					&& KeyMappings.EventsMatch(e, Event.KeyboardEvent(KeyMappings.Instance.CancelCurrentOperation))) // Cancel move
				{
					if (e.type == EventType.KeyUp)
					{
						CancelMove();					
					}
					e.Use();
				}
			}

			if(!CameraPanInProgress)
			{
				if(KeyMappings.EventsMatch(e, EditorKeyMappings.GetToolViewMapping()))
				{
                    if (e.type == EventType.KeyDown && !csgModel.MouseIsHeldOrRecent)
					{
						widgetMode = WidgetMode.Bounds;
						SceneView.RepaintAll();
					}
				}
				else if(KeyMappings.EventsMatch(e, EditorKeyMappings.GetToolMoveMapping()))
				{
                    if (e.type == EventType.KeyDown && !csgModel.MouseIsHeldOrRecent)
                    {
                        widgetMode = WidgetMode.Translate;
						// Make sure we get rid of any active custom cursor
						SabreMouse.ResetCursor();
						SceneView.RepaintAll();
					}
				}
				else if(KeyMappings.EventsMatch(e, EditorKeyMappings.GetToolRotateMapping()))
				{
                    if (e.type == EventType.KeyDown && !csgModel.MouseIsHeldOrRecent)
                    {
                        widgetMode = WidgetMode.Rotate;
						// Make sure we get rid of any active custom cursor
						SabreMouse.ResetCursor();
						SceneView.RepaintAll();
					}
				}
			}
		}

		void OnMouseMove(SceneView sceneView, Event e)
		{
			if(CameraPanInProgress || primaryTargetBrushBase == null || widgetMode != WidgetMode.Bounds)
			{
				return;
			}

			bool foundAny = false;
			Vector3 worldPos1 = Vector3.zero;
			Vector3 worldPos2 = Vector3.zero;

			Vector2 mousePosition = e.mousePosition;
				
			bool isAxisAlignedCamera = (EditorHelper.GetSceneViewCamera(sceneView) != EditorHelper.SceneViewCamera.Other);
			Vector3 cameraDirection = sceneView.camera.transform.forward;
			
			Bounds bounds = GetBounds();

            //			VisualDebug.ClearAll();

            // Reset the hover states
            hoveredResizeHandlePair = null;
            hoveredResizePointIndex = -1;

            // Edges
            if (isAxisAlignedCamera && sceneView.camera.orthographic) // Can only select edges in iso axis aligned
			{
				for (int i = 0; i < resizeHandlePairs.Length; i++)
				{
					// Skip any that shouldn't be seen from this camera angle (axis aligned only)
					if (isAxisAlignedCamera && Mathf.Abs(Vector3.Dot(TransformDirection(resizeHandlePairs[i].point1), cameraDirection)) > 0.001f)
					{
						continue;
					}
					// Skip any corners
					if(resizeHandlePairs[i].point1.sqrMagnitude != 1)
					{
						continue;
					}

					Vector3 normalVector3 = Vector3.Cross(InverseTransformDirection(cameraDirection), resizeHandlePairs[i].point1);

					Vector3 worldPosExtent1 = TransformPoint(bounds.center + normalVector3.Multiply(bounds.extents) + resizeHandlePairs[i].point1.Multiply(bounds.extents));
					Vector3 worldPosExtent2 = TransformPoint(bounds.center - normalVector3.Multiply(bounds.extents) + resizeHandlePairs[i].point1.Multiply(bounds.extents));

                    float range = resizeHandlePairs[i].CalculateScreenRange(TransformPoint, 0, bounds);

                    if (EditorHelper.InClickRect(mousePosition, worldPosExtent1, worldPosExtent2, range))
					{
						foundAny = true;
						worldPos1 = TransformPoint(bounds.center + resizeHandlePairs[i].point2.Multiply(bounds.extents));
						worldPos2 = TransformPoint(bounds.center + resizeHandlePairs[i].point1.Multiply(bounds.extents));

                        hoveredResizeHandlePair = resizeHandlePairs[i];
                        hoveredResizePointIndex = 0;

                        e.Use();
					}

					worldPosExtent1 = TransformPoint(bounds.center + normalVector3.Multiply(bounds.extents) + resizeHandlePairs[i].point2.Multiply(bounds.extents));
					worldPosExtent2 = TransformPoint(bounds.center - normalVector3.Multiply(bounds.extents) + resizeHandlePairs[i].point2.Multiply(bounds.extents));

                    range = resizeHandlePairs[i].CalculateScreenRange(TransformPoint, 1, bounds);

                    if (EditorHelper.InClickRect(mousePosition, worldPosExtent1, worldPosExtent2, range))
					{
						foundAny = true;
						worldPos1 = TransformPoint(bounds.center + resizeHandlePairs[i].point1.Multiply(bounds.extents));
						worldPos2 = TransformPoint(bounds.center + resizeHandlePairs[i].point2.Multiply(bounds.extents));

                        hoveredResizeHandlePair = resizeHandlePairs[i];
                        hoveredResizePointIndex = 1;

                        e.Use();
					}
				}
			}
            
            // Handles
			for (int i = 0; i < resizeHandlePairs.Length; i++)
			{
				// Skip any that shouldn't be seen from this camera angle (axis aligned only)
				if (isAxisAlignedCamera && Mathf.Abs(Vector3.Dot(TransformDirection(resizeHandlePairs[i].point1), cameraDirection)) > 0.001f)
				{
					continue;
				}

                if (resizeHandlePairs[i].InClickZone(TransformPoint, mousePosition, 0, bounds))
				{
					foundAny = true;
					worldPos1 = TransformPoint(bounds.center + resizeHandlePairs[i].point2.Multiply(bounds.extents));
					worldPos2 = TransformPoint(bounds.center + resizeHandlePairs[i].point1.Multiply(bounds.extents));

                    hoveredResizeHandlePair = resizeHandlePairs[i];
                    hoveredResizePointIndex = 0;
                    e.Use();
				}

                if (resizeHandlePairs[i].InClickZone(TransformPoint, mousePosition, 1, bounds))
                {
					foundAny = true;
					worldPos1 = TransformPoint(bounds.center + resizeHandlePairs[i].point1.Multiply(bounds.extents));
					worldPos2 = TransformPoint(bounds.center + resizeHandlePairs[i].point2.Multiply(bounds.extents));

                    hoveredResizeHandlePair = resizeHandlePairs[i];
                    hoveredResizePointIndex = 1;
                    e.Use();
                }
			}
				
			if (foundAny)
			{
				if(EnumHelper.IsFlagSet(e.modifiers, EventModifiers.Control))
				{
					SabreMouse.SetCursor(MouseCursor.RotateArrow);
				}
				else
				{
					Vector2 screenPoint1 = Camera.current.WorldToScreenPoint(worldPos1);
					Vector2 screenPoint2 = Camera.current.WorldToScreenPoint(worldPos2);
					
					SabreMouse.SetCursorFromVector3(screenPoint2, screenPoint1);
				}

				SceneView.RepaintAll();
			}
			else
			{
				SabreMouse.SetCursor(MouseCursor.Arrow);
				SceneView.RepaintAll();
			}
		}

        void DetermineTranslationPlane (Event e)
		{
			Bounds bounds = GetBounds();

			// Determine which face of the bounds the user has clicked
			Polygon[] translationBoxCollider = BrushFactory.GenerateCube(); // Generates a unit cube

			// First of all rescale transform the unit cube so that it matches the bounds
			for (int i = 0; i < translationBoxCollider.Length; i++) 
			{
				for (int j = 0; j < translationBoxCollider[i].Vertices.Length; j++) 
				{
					Vector3 position = translationBoxCollider[i].Vertices[j].Position;
					position = position.Multiply(bounds.extents) + bounds.center;

					position = TransformPoint(position); // Also transform the positions if in local mode
					translationBoxCollider[i].Vertices[j].Position = position;
                }
				translationBoxCollider[i].CalculatePlane();
			}

			// Construct a ray at the mouse position
			Vector2 currentPosition = e.mousePosition;
			Ray currentRay = Camera.current.ScreenPointToRay(EditorHelper.ConvertMousePointPosition(currentPosition));

			// Raycast against the ray against the bounds polygons we just created
			float hitDistance;
			Polygon hitPolygon = GeometryHelper.RaycastPolygons(translationBoxCollider.ToList(), currentRay, out hitDistance, 0);

			if(hitPolygon != null) // We hit a polygon
			{
				currentMode = ActiveMode.Translate;
				// Use this polygon's plane as the active translation plane
				translationPlane = hitPolygon.Plane;
			}
			else // Didn't hit anything
			{
				currentMode = ActiveMode.None;
			}
		}

        void OnMouseDown(SceneView sceneView, Event e)
		{
			if(primaryTargetBrushBase != null && widgetMode == WidgetMode.Bounds)
			{
				duplicationOccured = false;
				moveCancelled = false;
				originalPosition = primaryTargetBrushTransform.position;
				fullDeltaAngle = 0;

	            if (CameraPanInProgress)
	            {
					currentMode = ActiveMode.None;
	            }
	            else
	            {
	                // Resize
	                dotOffset3 = Vector3.zero;

	                Vector2 mousePosition = e.mousePosition;

	                // Reset which resize pair is being selected
	                selectedResizeHandlePair = null;

	                bool isAxisAlignedCamera = (EditorHelper.GetSceneViewCamera(sceneView) != EditorHelper.SceneViewCamera.Other);
	                Vector3 cameraDirection = sceneView.camera.transform.forward;

	                Bounds bounds = GetBounds();

					bool handleClicked = false;

					// Edges
					if(isAxisAlignedCamera && sceneView.camera.orthographic)
					{
						for (int i = 0; i < resizeHandlePairs.Length; i++)
						{
							// Skip any that shouldn't be seen from this camera angle (axis aligned only)
							if (isAxisAlignedCamera && Mathf.Abs(Vector3.Dot(TransformDirection(resizeHandlePairs[i].point1), cameraDirection)) > 0.001f)
							{
								continue;
							}
							// Skip any corners
							if(resizeHandlePairs[i].point1.sqrMagnitude != 1)
							{
								continue;
							}



							Vector3 normalVector3 = Vector3.Cross(InverseTransformDirection(cameraDirection), resizeHandlePairs[i].point1);

							Vector3 worldPosExtent1 = TransformPoint(bounds.center + normalVector3.Multiply(bounds.extents) + resizeHandlePairs[i].point1.Multiply(bounds.extents));
							Vector3 worldPosExtent2 = TransformPoint(bounds.center - normalVector3.Multiply(bounds.extents) + resizeHandlePairs[i].point1.Multiply(bounds.extents));

                            float range = resizeHandlePairs[i].CalculateScreenRange(TransformPoint, 0, bounds);

                            if (EditorHelper.InClickRect(mousePosition, worldPosExtent1, worldPosExtent2, range))
							{
								selectedResizeHandlePair = resizeHandlePairs[i];
								selectedResizePointIndex = 0;

								handleClicked = true;
								e.Use();
							}

							worldPosExtent1 = TransformPoint(bounds.center + normalVector3.Multiply(bounds.extents) + resizeHandlePairs[i].point2.Multiply(bounds.extents));
							worldPosExtent2 = TransformPoint(bounds.center - normalVector3.Multiply(bounds.extents) + resizeHandlePairs[i].point2.Multiply(bounds.extents));

                            range = resizeHandlePairs[i].CalculateScreenRange(TransformPoint, 0, bounds);

                            if (EditorHelper.InClickRect(mousePosition, worldPosExtent1, worldPosExtent2, range))
							{
								selectedResizeHandlePair = resizeHandlePairs[i];
								selectedResizePointIndex = 1;

								handleClicked = true;
								e.Use();
							}
						}
					}

	                for (int i = 0; i < resizeHandlePairs.Length; i++)
	                {
	                    // Skip any that shouldn't be seen from this camera angle (axis aligned only)
	                    if (isAxisAlignedCamera && Mathf.Abs(Vector3.Dot(TransformDirection(resizeHandlePairs[i].point1), cameraDirection)) > 0.001f)
	                    {
	                        continue;
	                    }

                        if (resizeHandlePairs[i].InClickZone(TransformPoint, mousePosition, 0, bounds))
	                    {
	                        selectedResizeHandlePair = resizeHandlePairs[i];
	                        selectedResizePointIndex = 0;

							handleClicked = true;
							e.Use();
	                    }

                        if (resizeHandlePairs[i].InClickZone(TransformPoint, mousePosition, 1, bounds))
                        {
                            selectedResizeHandlePair = resizeHandlePairs[i];
	                        selectedResizePointIndex = 1;

							handleClicked = true;
							e.Use();
	                    }
	                }

					if (handleClicked && selectedResizeHandlePair.HasValue)
	                {
	                    Vector3 worldPosition1;
	                    Vector3 worldPosition2;

	                    if (selectedResizePointIndex == 1)
	                    {
	                        worldPosition1 = TransformPoint(bounds.center + selectedResizeHandlePair.Value.point1.Multiply(bounds.extents));
	                        worldPosition2 = TransformPoint(bounds.center + selectedResizeHandlePair.Value.point2.Multiply(bounds.extents));
	                    }
	                    else
	                    {
	                        worldPosition1 = TransformPoint(bounds.center + selectedResizeHandlePair.Value.point2.Multiply(bounds.extents));
	                        worldPosition2 = TransformPoint(bounds.center + selectedResizeHandlePair.Value.point1.Multiply(bounds.extents));
	                    }
	                    Vector2 screenPoint1 = Camera.current.WorldToScreenPoint(worldPosition1);
	                    Vector2 screenPoint2 = Camera.current.WorldToScreenPoint(worldPosition2);

	                    SabreMouse.SetCursorFromVector3(screenPoint2, screenPoint1);

						if(EnumHelper.IsFlagSet(e.modifiers, EventModifiers.Control) 
							&& selectedResizeHandlePair.Value.ResizeType == ResizeType.EdgeMid)
						{
							currentMode = ActiveMode.Rotate;
							Vector3 activeDirection;
							if (selectedResizePointIndex == 0)
							{
								activeDirection = selectedResizeHandlePair.Value.point1;
							}
							else
							{
								activeDirection = selectedResizeHandlePair.Value.point2;
							}

							message = "0";

							initialRotationDirection = TransformDirection(activeDirection.Multiply(GetBounds().extents));
						}
						else
						{
							currentMode = ActiveMode.Resize;
						}

	                    SceneView.RepaintAll();
	                }
					else
					{
						DetermineTranslationPlane(e);
					}
	            }
			}

			isMarqueeSelection = false;

			marqueeStart = e.mousePosition;

            if(EditorHelper.IsMousePositionInInvalidRects(e.mousePosition))
            {
                marqueeCancelled = true;
            }
            else
            {
                marqueeCancelled = false;
            }
        }

        void OnMouseDrag(SceneView sceneView, Event e)
        {			
			if(e.button != 0 || CameraPanInProgress) // Must be LMB
			{
				return;
			}

			if (currentMode == ActiveMode.Resize && primaryTargetBrushBase != null && widgetMode == WidgetMode.Bounds)
            {
				OnMouseDragResize(sceneView, e);
			}
			else if (currentMode == ActiveMode.Rotate && primaryTargetBrushBase != null && widgetMode == WidgetMode.Bounds)
			{
				OnMouseDragRotate(sceneView, e);
            }
			else if (currentMode == ActiveMode.Translate && !moveCancelled && Tools.current == UnityEditor.Tool.None && primaryTargetBrushBase != null && widgetMode == WidgetMode.Bounds)
            {
				OnMouseDragTranslate(sceneView, e);
            }
			else
			{
                if(!marqueeCancelled)
                {
                    marqueeEnd = e.mousePosition;
                    isMarqueeSelection = true;
                    sceneView.Repaint();
                }
            }

			e.Use();
        }

		void OnMouseDragResize(SceneView sceneView, Event e)
		{
			Rect pixelRect = sceneView.camera.pixelRect;

			// Resize a handle
			Vector2 currentPosition = e.mousePosition;

			// Clamp the current position to the screen rect. Otherwise we get some odd problems if you carry on 
			// resizing off screen.
			currentPosition.x = Mathf.Clamp(currentPosition.x, pixelRect.xMin, pixelRect.xMax);
			currentPosition.y = Mathf.Clamp(currentPosition.y, pixelRect.yMin, pixelRect.yMax);

			Vector2 lastPosition = currentPosition - e.delta;

			Ray lastRay = Camera.current.ScreenPointToRay(EditorHelper.ConvertMousePointPosition(lastPosition));

			Ray currentRay = Camera.current.ScreenPointToRay(EditorHelper.ConvertMousePointPosition(currentPosition));

//			Bounds bounds = GetBounds();
			
			Vector3 offset = primaryTargetBrushTransform.position;
//			if(Tools.pivotMode == PivotMode.Center && Tools.pivotRotation == PivotRotation.Global)
//			{
//				offset = GetBounds().center;
//			}

			Vector3 lineStart = offset + TransformDirection(selectedResizeHandlePair.Value.point1);
			Vector3 lineEnd = offset + TransformDirection(selectedResizeHandlePair.Value.point2);

			Vector3 lastPositionWorld = MathHelper.ClosestPointOnLine(lastRay, lineStart, lineEnd);
			Vector3 currentPositionWorld = MathHelper.ClosestPointOnLine(currentRay, lineStart, lineEnd);
			
			Vector3 direction;
			if (selectedResizePointIndex == 0)
			{
				direction = selectedResizeHandlePair.Value.point1;
			}
			else
			{
				direction = selectedResizeHandlePair.Value.point2;
			}
			
			// If shift is held, flip the direction so they're resizing the opposite side
			if(EnumHelper.IsFlagSet(e.modifiers, EventModifiers.Shift))
			{
				direction = -direction;
			}
			
			Vector3 deltaWorld = (currentPositionWorld - lastPositionWorld);
			// Rescaling logic deals with local space changes, convert to that space
			Vector3 deltaLocal = InverseTransformDirection(deltaWorld);
			
			Vector3 dot3 = Vector3.zero;
			if (direction.x != 0)
			{
				dot3.x = Vector3.Dot(deltaLocal, new Vector3(Mathf.Sign(direction.x), 0, 0));
			}
			if (direction.y != 0)
			{
				dot3.y = Vector3.Dot(deltaLocal, new Vector3(0, Mathf.Sign(direction.y), 0));
			}
			if (direction.z != 0)
			{
				dot3.z = Vector3.Dot(deltaLocal, new Vector3(0, 0, Mathf.Sign(direction.z)));
			}

			float snapDistance = CurrentSettings.PositionSnapDistance;
			
			if (CurrentSettings.PositionSnappingEnabled)
			{
				// Snapping's dot uses an offset to track deltas that would be lost otherwise due to snapping
				dot3 += dotOffset3;

				Vector3 roundedDot3 = MathHelper.RoundVector3(dot3, snapDistance);
				dotOffset3 = dot3 - roundedDot3;
				dot3 = roundedDot3;
			}
			
			if(EnumHelper.IsFlagSet(e.modifiers, EventModifiers.Control))
			{
				Undo.RecordObjects(targetBrushTransforms, "Move brush(es)");
				dot3 = TransformDirection(dot3);
				if(selectedResizePointIndex == 1)
				{
					dot3 = -dot3;
				}

				TranslateBrushes(dot3);
			}
			else
			{
				if(GetIsValidToResize())
				{
					RescaleBrush(direction, dot3);
				}
			}
		}

		void OnMouseDragTranslate(SceneView sceneView, Event e)
		{
			if(!duplicationOccured && EnumHelper.IsFlagSet(e.modifiers, EventModifiers.Control))
			{
//				Vector3 newPosition = targetBrushTransform.position;
				primaryTargetBrushTransform.position = originalPosition;
				
				duplicationOccured = true;

                // Duplicate selection, the selection is set automatically
                EditorHelper.DuplicateSelection();
			}
			else
			{
				SabreMouse.SetCursor(MouseCursor.MoveArrow);
				
				// Drag brush position
				Vector2 lastPosition = e.mousePosition - e.delta;
				Vector2 currentPosition = e.mousePosition;

				Ray lastRay = Camera.current.ScreenPointToRay(EditorHelper.ConvertMousePointPosition(lastPosition));
				Ray currentRay = Camera.current.ScreenPointToRay(EditorHelper.ConvertMousePointPosition(currentPosition));

				float lastRayHit;
				float currentRayHit;
				if(translationPlane.Raycast(lastRay, out lastRayHit))
				{
					if(translationPlane.Raycast(currentRay, out currentRayHit))
					{
						// Find the world points where the rays hit the rotation plane
						Vector3 lastPositionWorld = lastRay.GetPoint(lastRayHit);
						Vector3 currentPositionWorld = currentRay.GetPoint(currentRayHit);

						Vector3 delta = (currentPositionWorld - lastPositionWorld);

						float snapDistance = CurrentSettings.PositionSnapDistance;
						
						Vector3 finalDelta;
						
						if (CurrentSettings.PositionSnappingEnabled)
						{
							delta += translationUnrounded;

							// Round the delta according to the pivot rotation mode
							Vector3 roundedDelta = TransformDirection(MathHelper.RoundVector3(InverseTransformDirection(delta), snapDistance));

							translationUnrounded = delta - roundedDelta;
							finalDelta = roundedDelta;
						}
						else
						{
							finalDelta = delta;
						}
							

						TranslateBrushes(finalDelta);
//						for (int i = 0; i < Selection.transforms.Length; i++) 
//						{
//							Undo.RecordObjects(Selection.transforms, "Move brush(es)");
//							Selection.transforms[i].position += finalDelta;
//
//						}
					}
				}
			}
		}

		Vector3 GetRotationAxis()
		{
			Vector3 rotationAxis;
			
			if(selectedResizeHandlePair.Value.point1.x == 0)
			{
				rotationAxis = new Vector3(1,0,0);
			}
			else if(selectedResizeHandlePair.Value.point1.y == 0)
			{
				rotationAxis = new Vector3(0,1,0);
			}
			else
			{
				rotationAxis = new Vector3(0,0,1);
			}
			
			return rotationAxis;
		}

		Vector3 GetRotationAxisTransformed()
		{
			return TransformDirection(GetRotationAxis());
		}

		Vector3 GetBrushesPivotPoint()
		{
			if(primaryTargetBrushTransform != null)
			{
				if(Tools.pivotMode == PivotMode.Center)
				{
                    Bounds bounds = GetBounds();
                    if (Tools.pivotRotation == PivotRotation.Global)
                    {
                        return bounds.center;
                    }
                    else
                    {
                        return primaryTargetBrushTransform.TransformPoint(bounds.center);
                    }
				}
				else // Local mode
				{
					// Just return the position of the primary selected brush
					return primaryTargetBrushTransform.position;
				}
			}
			else
			{
				return Vector3.zero;
			}
		}

		void OnMouseDragRotate(SceneView sceneView, Event e)
		{
			Bounds bounds = GetBounds();
			// Rotation
			Vector3 rotationAxis = GetRotationAxisTransformed();
			// Brush center point
			Vector3 centerWorld = TransformPoint(bounds.center);

			Vector2 lastPosition = e.mousePosition - e.delta;
			Vector2 currentPosition = e.mousePosition;
			
			Ray lastRay = Camera.current.ScreenPointToRay(EditorHelper.ConvertMousePointPosition(lastPosition));
			Ray currentRay = Camera.current.ScreenPointToRay(EditorHelper.ConvertMousePointPosition(currentPosition));

			// Calculate the plane rotation is occuring on (this plane shares the rotation axis normal and is coplanar
			// with the center point of the brush)
			Plane plane = new Plane(rotationAxis, centerWorld);

			float lastRayHit;
			float currentRayHit;
			if(plane.Raycast(lastRay, out lastRayHit))
			{
				if(plane.Raycast(currentRay, out currentRayHit))
				{
					// Find the world points where the rays hit the rotation plane
					Vector3 lastRayWorld = lastRay.GetPoint(lastRayHit);
					Vector3 currentRayWorld = currentRay.GetPoint(currentRayHit);

					// Find the rotation needed to transform the points on the plane into XY aligned plane
					Quaternion cancellingRotation = Quaternion.Inverse(Quaternion.LookRotation(rotationAxis));

					// Subtract the brush's center point so the points are relative to the center of the brush
					currentRayWorld -= centerWorld;
					lastRayWorld -= centerWorld;

					// Rotate the world points by the cancelling rotation to put them on XY plane
					currentRayWorld = cancellingRotation * currentRayWorld;
					lastRayWorld = cancellingRotation * lastRayWorld;

					// Because the points have been transformed into XY plane, we can just use atan2 to find the angles
					float angle1 = Mathf.Rad2Deg * Mathf.Atan2(currentRayWorld.x, currentRayWorld.y);
					float angle2 = Mathf.Rad2Deg * Mathf.Atan2(lastRayWorld.x, lastRayWorld.y);
					// Change in angle is simply the new angle minus the last
					float deltaAngle = angle2 - angle1;


					if(CurrentSettings.AngleSnappingEnabled)
					{
						deltaAngle += unroundedDeltaAngle;

						float roundedAngle = MathHelper.RoundFloat(deltaAngle, CurrentSettings.AngleSnapDistance);
						unroundedDeltaAngle = deltaAngle - roundedAngle;
						deltaAngle = roundedAngle;
					}
					fullDeltaAngle += deltaAngle;
                    message = fullDeltaAngle.ToString();

					Undo.RecordObjects(targetBrushTransforms, "Rotate brush(es)");

					for (int i = 0; i < targetBrushTransforms.Length; i++) 
					{
						targetBrushTransforms[i].RotateAround(centerWorld, rotationAxis, deltaAngle);
					}

					for (int brushIndex = 0; brushIndex < targetBrushBases.Length; brushIndex++) 
					{
						EditorHelper.SetDirty(targetBrushBases[brushIndex]);
						EditorHelper.SetDirty(targetBrushBases[brushIndex].transform);
						targetBrushBases[brushIndex].Invalidate(true);
					}
				}
			}

			SabreMouse.SetCursor(MouseCursor.RotateArrow);
		}
		
		void OnMouseUp(SceneView sceneView, Event e)
		{
			duplicationOccured = false;
			moveCancelled = false;
			
			// Just let go of the mouse button, the resize operation has finished
			if (currentMode != ActiveMode.None && primaryTargetBrushBase != null && widgetMode == WidgetMode.Bounds)
			{
				selectedResizeHandlePair = null;
				currentMode = ActiveMode.None;

				if(csgModel.MouseIsDragging)
				{
					e.Use();
				}

				Undo.RecordObjects(targetBrushTransforms, "Rescale Brush");
				Undo.RecordObjects(targetBrushBases, "Rescale Brush");
			}
			else
			{
				if(isMarqueeSelection) // Marquee vertex selection
				{
					isMarqueeSelection = false;

					marqueeEnd = e.mousePosition;

					List<Brush> brushes = csgModel.GetBrushes();
					List<Object> highlightedBrushObjects = new List<Object>();
					for (int brushIndex = 0; brushIndex < brushes.Count; brushIndex++) 
					{
						if(brushes[brushIndex] != null)
						{
							Vector3 brushWorldPivot = brushes[brushIndex].transform.position;
							Vector3 screenPoint = sceneView.camera.WorldToScreenPoint(brushWorldPivot);

							if(SabreMouse.MarqueeContainsPoint(marqueeStart, marqueeEnd, screenPoint))
							{
								bool brushInsideMarquee = true;

								// Determine if all the brush's vertices are inside the maquee
								Polygon[] transformedPolygons = brushes[brushIndex].GenerateTransformedPolygons();
								for (int polygonIndex = 0; polygonIndex < transformedPolygons.Length && brushInsideMarquee; polygonIndex++) 
								{
									Vertex[] vertices = transformedPolygons[polygonIndex].Vertices;
									for (int vertexIndex = 0; vertexIndex < vertices.Length && brushInsideMarquee; vertexIndex++) 
									{
										screenPoint = sceneView.camera.WorldToScreenPoint(vertices[vertexIndex].Position);
										if(!SabreMouse.MarqueeContainsPoint(marqueeStart, marqueeEnd, screenPoint))
										{
											brushInsideMarquee = false;
										}
									}
								}

								if(brushInsideMarquee)
								{
									highlightedBrushObjects.Add(brushes[brushIndex].gameObject);
								}
							}
						}
					}

					if(!e.shift && !e.control)
					{
						Selection.objects = new Object[0];
					}

					List<Object> tempSelection = new List<Object>(Selection.objects);

					for (int i = 0; i < highlightedBrushObjects.Count; i++) 
					{
						if(e.control)
						{
							tempSelection.Remove(highlightedBrushObjects[i]);
						}
						else
						{
							if(!tempSelection.Contains(highlightedBrushObjects[i]))
							{
								tempSelection.Add(highlightedBrushObjects[i]);
							}
						}
					}

					Selection.objects = tempSelection.ToArray();
				}
			}

			if(primaryTargetBrushBase != null)
			{
				for (int i = 0; i < targetBrushes.Length; i++) 
				{
					if(targetBrushes[i] != null)
					{
						targetBrushes[i].ResetPivot();
					}
				}
			}
			
			SabreMouse.ResetCursor();
            SceneView.RepaintAll();
        }

        void CancelMove()
		{
			moveCancelled = true;
			primaryTargetBrushTransform.position = originalPosition;
			SabreMouse.ResetCursor();
		}
		
		public void OnRepaint(SceneView sceneView, Event e)
		{
			if(isMarqueeSelection && sceneView == SceneView.lastActiveSceneView)
			{
				SabreGraphics.DrawMarquee(marqueeStart, marqueeEnd);
			}

			if(primaryTargetBrushBase != null && widgetMode == WidgetMode.Bounds)
			{
				Bounds bounds = GetBounds();

	            // Selected brush green outline
				SabreCSGResources.GetSelectedBrushMaterial().SetPass(0);

	            // Selection
	            GL.Begin(GL.LINES);
	            GL.Color(Color.white);

				if(Tools.pivotRotation == PivotRotation.Global)
				{
					SabreGraphics.DrawBox(bounds);
				}
				else
				{
					SabreGraphics.DrawBox(bounds, primaryTargetBrushTransform);
				}

	            GL.End();


				if(currentMode == ActiveMode.Rotate && selectedResizeHandlePair.HasValue)
				{
					SabreCSGResources.GetSelectedBrushMaterial().SetPass(0);


					Vector3 extents = bounds.extents;

					// If rotation axis is (0,1,0) or (0,1,0), this produces (1,0,1)
					Vector3 mask = Vector3.one - MathHelper.VectorAbs(GetRotationAxis());

					// Discount any extents in the rotation axis and find the magnitude
					float largestExtent = extents.Multiply(mask).magnitude;// bounds.GetLargestExtent();
					Vector3 worldCenter = TransformPoint(bounds.center);
					SabreGraphics.DrawRotationCircle(worldCenter, GetRotationAxisTransformed(), largestExtent, initialRotationDirection);
				}
				DrawResizeHandles(sceneView, e);
			}

			GUIStyle style = new GUIStyle(EditorStyles.toolbar);
			style.normal.background = SabreCSGResources.ClearTexture;
			Rect rectangle = new Rect(0, 50, 300, 50);
			style.fixedHeight = rectangle.height;
			GUILayout.Window(140007, rectangle, OnTopToolbarGUI, "", style);
		}

		private void OnTopToolbarGUI(int windowID)
		{
			widgetMode = SabreGUILayout.DrawEnumGrid(widgetMode, GUILayout.Width(67));

			GUILayout.BeginHorizontal(GUILayout.Width(200));

			if (GUILayout.Button("Snap Center", EditorStyles.miniButton))
			{
				for (int i = 0; i < targetBrushBases.Length; i++) 
				{
					Undo.RecordObject(targetBrushBases[i].transform, "Snap Center");
					Undo.RecordObject(targetBrushBases[i], "Snap Center");

					Vector3 newPosition = targetBrushBases[i].transform.position;

					float snapDistance = CurrentSettings.PositionSnapDistance;
					newPosition = MathHelper.RoundVector3(newPosition, snapDistance);
					targetBrushBases[i].transform.position = newPosition;
					targetBrushBases[i].Invalidate(true);
				}
			}

			if (GUILayout.Button("Flip X", EditorStyles.miniButton))
			{
				Undo.RecordObjects(targetBrushes, "Flip Polygons");

                bool localToPrimaryBrush = (Tools.pivotRotation == PivotRotation.Local);
                BrushUtility.Flip(primaryTargetBrush, targetBrushes, 0, localToPrimaryBrush, GetBrushesPivotPoint());
			}

			if (GUILayout.Button("Flip Y", EditorStyles.miniButton))
			{
				Undo.RecordObjects(targetBrushes, "Flip Polygons");

                bool localToPrimaryBrush = (Tools.pivotRotation == PivotRotation.Local);
                BrushUtility.Flip(primaryTargetBrush, targetBrushes, 1, localToPrimaryBrush, GetBrushesPivotPoint());
            }

            if (GUILayout.Button("Flip Z", EditorStyles.miniButton))
			{
				Undo.RecordObjects(targetBrushes, "Flip Polygons");

                bool localToPrimaryBrush = (Tools.pivotRotation == PivotRotation.Local);
                BrushUtility.Flip(primaryTargetBrush, targetBrushes, 2, localToPrimaryBrush, GetBrushesPivotPoint());
            }

            GUILayout.EndHorizontal();
		}

		public void RescaleBrush(Vector3 direction, Vector3 translation)
        {			
			if(translation == Vector3.zero)
			{
				return;
			}

            //VisualDebug.ClearAll();

            // Scale the brush in the direction
            // e.g. if scaling a cuboid from left to right by scaleFactor 1.5 then the left face (verts) remains unchanged
            // but the right face with be an extra 50% away from the left face

            Bounds bounds = GetBounds();

			Vector3 negativeDirection = -1 * direction;
			Vector3 localPivotPoint = bounds.extents.Multiply(negativeDirection) + bounds.center;

			Vector3 size = bounds.size;

            Vector3 scaleFactor = new Vector3(1 + translation.x / size.x, 1 + translation.y / size.y, 1 + translation.z / size.z);

			Vector3 newDirection = direction.Multiply(scaleFactor);

			Vector3 scaleVector3 = MathHelper.VectorAbs(newDirection);

			if (Mathf.Abs(scaleVector3.x) <= 0.01f)
			{
				scaleVector3.x = 1;
			}
            if (Mathf.Abs(scaleVector3.y) <= 0.01f)
            {
				scaleVector3.y = 1;
			}
            if (Mathf.Abs(scaleVector3.z) <= 0.01f)
            {
				scaleVector3.z = 1;
			}

            if(scaleVector3 == Vector3.one)
            {
                // No scale to apply, so early out
                return;
            }

            Vector3 primaryPivot = primaryTargetBrushTransform.TransformPoint(bounds.center);

            for (int brushIndex = 0; brushIndex < targetBrushBases.Length; brushIndex++) 
			{
				Undo.RecordObject(targetBrushBases[brushIndex].transform, "Rescale Brush");
				Undo.RecordObject(targetBrushBases[brushIndex], "Rescale Brush");

				if(targetBrushes[brushIndex] != null) // PrimitiveBrush
				{
					Polygon[] polygons = targetBrushes[brushIndex].GetPolygons();
		
		            for (int i = 0; i < polygons.Length; i++)
		            {
		                Polygon polygon = polygons[i];

						polygon.CalculatePlane();
						Vector3 previousPlaneNormal = polygons[i].Plane.normal;

		                int vertexCount = polygon.Vertices.Length;

		                Vector3[] newPositions = new Vector3[vertexCount];
		                Vector2[] newUV = new Vector2[vertexCount];

		                for (int j = 0; j < vertexCount; j++)
		                {
		                    newPositions[j] = polygon.Vertices[j].Position;
		                    newUV[j] = polygon.Vertices[j].UV;
		                }

		                for (int j = 0; j < vertexCount; j++)
		                {
		                    Vertex vertex = polygon.Vertices[j];

		                    Vector3 newPosition = vertex.Position;

							// Start transform
							newPosition = targetBrushes[brushIndex].transform.TransformPoint(newPosition);

							if(Tools.pivotRotation == PivotRotation.Local)
							{
								newPosition = primaryTargetBrushTransform.InverseTransformPoint(newPosition);
							}

							newPosition -= localPivotPoint;

		                    // Scale in that direction
		                    newPosition = newPosition.Multiply(scaleVector3);

							newPosition += localPivotPoint;

							if(Tools.pivotRotation == PivotRotation.Local)
							{
								newPosition = primaryTargetBrushTransform.TransformPoint(newPosition);
							}

							newPosition = targetBrushes[brushIndex].transform.InverseTransformPoint(newPosition);

		                    newPositions[j] = newPosition;

		                    // Update UVs
		                    Vector3 p1 = polygon.Vertices[0].Position;
		                    Vector3 p2 = polygon.Vertices[1].Position;
		                    Vector3 p3 = polygon.Vertices[2].Position;

		                    UnityEngine.Plane plane = new UnityEngine.Plane(p1, p2, p3);
		                    Vector3 f = MathHelper.ClosestPointOnPlane(newPosition, plane);

		                    Vector2 uv1 = polygon.Vertices[0].UV;
		                    Vector2 uv2 = polygon.Vertices[1].UV;
		                    Vector2 uv3 = polygon.Vertices[2].UV;

		                    // calculate vectors from point f to vertices p1, p2 and p3:
		                    Vector3 f1 = p1 - f;
		                    Vector3 f2 = p2 - f;
		                    Vector3 f3 = p3 - f;

		                    // calculate the areas (parameters order is essential in this case):
		                    Vector3 va = Vector3.Cross(p1 - p2, p1 - p3); // main triangle cross product
		                    Vector3 va1 = Vector3.Cross(f2, f3); // p1's triangle cross product
		                    Vector3 va2 = Vector3.Cross(f3, f1); // p2's triangle cross product
		                    Vector3 va3 = Vector3.Cross(f1, f2); // p3's triangle cross product

		                    float a = va.magnitude; // main triangle area

		                    // calculate barycentric coordinates with sign:
		                    float a1 = va1.magnitude / a * Mathf.Sign(Vector3.Dot(va, va1));
		                    float a2 = va2.magnitude / a * Mathf.Sign(Vector3.Dot(va, va2));
		                    float a3 = va3.magnitude / a * Mathf.Sign(Vector3.Dot(va, va3));

		                    // find the uv corresponding to point f (uv1/uv2/uv3 are associated to p1/p2/p3):
		                    Vector2 uv = uv1 * a1 + uv2 * a2 + uv3 * a3;

		                    newUV[j] = uv;
		                }

		                // Apply all the changes to the polygon
		                for (int j = 0; j < vertexCount; j++)
		                {
		                    Vertex vertex = polygon.Vertices[j];
		                    vertex.Position = newPositions[j];
		                    vertex.UV = newUV[j];
		                }

						// Polygon geometry has changed, inform the polygon that it needs to recalculate its cached plane
						polygons[i].CalculatePlane();

						Vector3 newPlaneNormal = polygons[i].Plane.normal;

						// Find the rotation from the original polygon plane to the new polygon plane
						Quaternion normalRotation = Quaternion.FromToRotation(previousPlaneNormal, newPlaneNormal);

						// Rotate all the vertex normals by the new rotation
						for (int j = 0; j < vertexCount; j++) 
						{
							Vertex vertex = polygon.Vertices[j];
							vertex.Normal = normalRotation * vertex.Normal;
						}
		            }
				}
				else // Compound brush
				{
					if(Tools.pivotRotation == PivotRotation.Global)
					{
						if(targetBrushTransforms[brushIndex].forward.GetSetAxisCount() != 1
							|| targetBrushTransforms[brushIndex].up.GetSetAxisCount() != 1)
						{
							// Skip any brushes that are not axis aligned
							continue;
						}

						int found = 0;

						int globalAxis1 = 0;
						int globalAxis2 = 0;

						bool flipped1 = false;
						bool flipped2 = false;

						for (int i = 0; i < 3; i++) 
						{
							if(!direction[i].EqualsWithEpsilon(0))
							{
								if(found == 0)
								{
									globalAxis1 = i;
									if(direction[i] < 0)
									{
										flipped1 = true;
									}
								}
								else if(found == 1)
								{
									globalAxis2 = i;
									if(direction[i] < 0)
									{
										flipped2 = true;
									}
								}
								found++;
							}
						}

						if(found < 1 || found > 2) // Unexpected scenario, skip this brush
						{
							continue;
						}

						for (int axisIndex = 0; axisIndex < found; axisIndex++) 
						{
							bool flipped;
							int flippedSign;
							int globalAxis;

							if(axisIndex == 0)
							{
								globalAxis = globalAxis1;
								flipped = flipped1;
								flippedSign = flipped1 ? -1 : 1;
							}
							else
							{
								globalAxis = globalAxis2;
								flipped = flipped2;
								flippedSign = flipped2 ? -1 : 1;
							}

							Vector3 globalTempDirection = Vector3.zero.SetAxis(globalAxis, 1);
							Vector3 transformedDirection = targetBrushTransforms[brushIndex].TransformDirection(globalTempDirection);

							int localAxis = 0;
							for (int i = 0; i < 3; i++) 
							{
								if(!transformedDirection[i].EqualsWithEpsilon(0))
								{
									localAxis = i;
									break;
								}
							}

							Bounds newBounds = targetBrushBases[brushIndex].GetBounds();

							float offset = 0.5f * translation[globalAxis] * (newBounds.extents[localAxis] / bounds.extents[globalAxis]);
							if(flipped)
							{
								offset = -offset;
							}



							Vector3 globalPivot = bounds.center;
							Vector3 globalPivotOffset = Vector3.zero.SetAxis(globalAxis, -flippedSign * bounds.extents[globalAxis]);
							globalPivot += globalPivotOffset;
							//						VisualDebug.AddPoint(globalPivot, Color.red);

							Vector3 localPivot = newBounds.center + targetBrushTransforms[brushIndex].position;
							Vector3 localPivotOffset = Vector3.zero.SetAxis(globalAxis, -flippedSign * newBounds.extents[localAxis]);
							localPivot += localPivotOffset;
							//						VisualDebug.AddPoint(localPivot);

							offset += 0.5f * translation[globalAxis] * (localPivot[globalAxis] - globalPivot[globalAxis]) / bounds.extents[globalAxis];



							Vector3 offset3 = Vector3.zero.SetAxis(globalAxis, offset);
							targetBrushTransforms[brushIndex].position += offset3;

							// Apply new bounds size
							Vector3 extents = newBounds.extents;
							extents[localAxis] *= scaleVector3[globalAxis];
							newBounds.extents = extents;

							targetBrushBases[brushIndex].SetBounds(newBounds);
						}


					}
					else
					{
                        int found = 0;

                        int primaryAxis1 = 0;
                        int primaryAxis2 = 0;

                        bool flipped1 = false;
                        bool flipped2 = false;

                        bool flippedB1 = false;
                        bool flippedB2 = false;

                        for (int i = 0; i < 3; i++)
                        {
                            if (!direction[i].EqualsWithEpsilon(0))
                            {
                                if (found == 0)
                                {
                                    primaryAxis1 = i;
                                    if (direction[i] < 0)
                                    {
                                        flipped1 = true;
                                    }
                                }
                                else if (found == 1)
                                {
                                    primaryAxis2 = i;
                                    if (direction[i] < 0)
                                    {
                                        flipped2 = true;
                                    }
                                }
                                found++;
                            }
                        }

                        if (found < 1 || found > 2) // Unexpected scenario, skip this brush
                        {
                            continue;
                        }

                        int localAxis1 = 0;
                        int localAxis2 = 0;
                        if (primaryTargetBrushBase == targetBrushBases[brushIndex])
                        {
                            localAxis1 = primaryAxis1;
                            localAxis2 = primaryAxis2;
                        }
                        else
                        {
                            // Determine local axis
                            Vector3 primaryAxis1World = primaryTargetBrushTransform.TransformDirection(Vector3.zero.SetAxis(primaryAxis1, 1));
                            Vector3 primaryAxis1Local = targetBrushTransforms[brushIndex].InverseTransformDirection(primaryAxis1World);

                            for (int i = 0; i < 3; i++)
                            {
                                if(!primaryAxis1Local[i].EqualsWithEpsilon(0))
                                {
                                    if(primaryAxis1Local[i] < 0)
                                    {
                                        flippedB1 = true;
                                    }
                                    localAxis1 = i;
                                    break;
                                }
                            }

                            if(found > 1)
                            {
                                Vector3 primaryAxis2World = primaryTargetBrushTransform.TransformDirection(Vector3.zero.SetAxis(primaryAxis2, 1));
                                Vector3 primaryAxis2Local = targetBrushTransforms[brushIndex].InverseTransformDirection(primaryAxis2World);

                                for (int i = 0; i < 3; i++)
                                {
                                    if (!primaryAxis2Local[i].EqualsWithEpsilon(0))
                                    {
                                        if (primaryAxis1Local[i] < 0)
                                        {
                                            flippedB2 = true;
                                        }
                                        localAxis2 = i;
                                        break;
                                    }
                                }
                            }
                            //Debug.Log(primaryAxis1Local);
                        }
                        //Debug.Log(primaryAxis1 + " " + primaryAxis2 + " | " + localAxis1 + " " + localAxis2);

                        for (int i = 0; i < found; i++)
                        {
                            int primaryAxis;
                            int localAxis;
                            bool flipped;
                            bool flippedB;

                            if (i == 0)
                            {
                                primaryAxis = primaryAxis1;
                                localAxis = localAxis1;
                                flipped = flipped1;
                                flippedB = flippedB1;
                            }
                            else
                            {
                                primaryAxis = primaryAxis2;
                                localAxis = localAxis2;
                                flipped = flipped2;
                                flippedB = flippedB2;
                            }


                            Bounds newBounds = targetBrushBases[brushIndex].GetBounds();

                            // Offset from resizing
                            Vector3 translationInAxis = Vector3.zero.SetAxis(primaryAxis, translation[primaryAxis]);
                            Vector3 worldOffset = primaryTargetBrushTransform.TransformDirection(translationInAxis);
                            float sizeInBounds = newBounds.extents[localAxis] / bounds.extents[primaryAxis];
                            //Debug.Log(sizeInBounds);

                            Vector3 positionOffset;
                            if(flipped)
                            {
                                positionOffset = -sizeInBounds * 0.5f * worldOffset;
                            }
                            else
                            {
                                positionOffset = sizeInBounds * 0.5f * worldOffset;
                            }
                            

                            // Second offset
                            Vector3 primaryPivotCopy = primaryPivot;
                            //VisualDebug.AddPoint(primaryPivotCopy, Color.white, 0.1f);

                            Vector3 primaryPivotOffset = Vector3.zero.SetAxis(primaryAxis, bounds.extents[primaryAxis]);
                            primaryPivotOffset = primaryTargetBrushTransform.TransformDirection(primaryPivotOffset);
                            if (flipped)
                            {
                                primaryPivotCopy += primaryPivotOffset;
                            }
                            else
                            {
                                primaryPivotCopy -= primaryPivotOffset;
                            }

                            //VisualDebug.AddPoint(primaryPivotCopy, new Color(0,1,0,0.5f));




                            Vector3 localPivot = targetBrushTransforms[brushIndex].TransformPoint(newBounds.center);

                            //VisualDebug.AddPoint(localPivot, Color.yellow, 0.1f);


                            Vector3 localPivotOffset = Vector3.zero.SetAxis(localAxis, newBounds.extents[localAxis]);
                            localPivotOffset = targetBrushTransforms[brushIndex].TransformDirection(localPivotOffset);
                            if (flipped ^ flippedB)
                            {
                                localPivot += localPivotOffset;
                            }
                            else
                            {
                                localPivot -= localPivotOffset;
                            }

                            //VisualDebug.AddPoint(localPivot, Color.blue, 0.3f);

                            if(!localPivot.EqualsWithEpsilonLower(primaryPivotCopy))
                            {
                                Vector3 directionInAxis = Vector3.zero.SetAxis(primaryAxis, direction[primaryAxis]);
                                Vector3 worldDirection = primaryTargetBrushTransform.TransformDirection(directionInAxis);
                                Vector3 worldPositionOffset = scaleVector3[primaryAxis] * (localPivot - primaryPivotCopy) - (localPivot - primaryPivotCopy);
                                worldPositionOffset = worldDirection * Vector3.Dot(worldDirection, worldPositionOffset);
                                positionOffset += worldPositionOffset;

                            }

                            // Apply the position offsets after they've been calculated
                            targetBrushTransforms[brushIndex].position += positionOffset;

                            // Apply new bounds size
                            Vector3 extents = newBounds.extents;
                            extents[localAxis] *= scaleVector3[primaryAxis];
                            newBounds.extents = extents;

                            targetBrushBases[brushIndex].SetBounds(newBounds);
                        }
                    }
				}
				EditorHelper.SetDirty(targetBrushBases[brushIndex]);
				EditorHelper.SetDirty(targetBrushBases[brushIndex].transform);
				targetBrushBases[brushIndex].Invalidate(true);
			}
        }

		bool GetIsValidToResize()
		{
            // Check if any of the selected compound brushes are in invalid states
			for (int brushIndex = 0; brushIndex < targetBrushBases.Length; brushIndex++) 
			{
				if(targetBrushes[brushIndex] == null) // CompoundBrush
				{
                    Vector3 forward = targetBrushTransforms[brushIndex].forward;
                    Vector3 up = targetBrushTransforms[brushIndex].up;
                    
                    // Transform to local to the primary brush if necessary (local pivot mode)
                    forward = InverseTransformDirection(forward);
                    up = InverseTransformDirection(up);

                    if (forward.GetSetAxisCount() != 1
						|| up.GetSetAxisCount() != 1)
					{
						// Skip any brushes that are not axis aligned
						return false;
                    }
				}
			}

			return true;
		}

		void DrawResizeHandles(SceneView sceneView, Event e)
        {			
			bool isValidToResize = GetIsValidToResize();

            Camera sceneViewCamera = sceneView.camera;

            Bounds bounds = GetBounds();
			SabreCSGResources.GetGizmoMaterial().SetPass(0);

            GL.PushMatrix();
			GL.LoadPixelMatrix();

            GL.Begin(GL.QUADS);

            bool isAxisAlignedCamera = (EditorHelper.GetSceneViewCamera(sceneView) != EditorHelper.SceneViewCamera.Other);
            Vector3 cameraDirection = sceneViewCamera.transform.forward;

            // Draw all the handles
            for (int i = 0; i < resizeHandlePairs.Length; i++)
            {
                // Skip any that shouldn't be seen from this camera angle (axis aligned only)
                if (isAxisAlignedCamera && Mathf.Abs(Vector3.Dot(TransformDirection(resizeHandlePairs[i].point1), cameraDirection)) > 0.001f)
                {
                    continue;
                }

                if (selectedResizeHandlePair.HasValue)
                {
                    ResizeHandlePair selectedValue = selectedResizeHandlePair.Value;
                    if (resizeHandlePairs[i] == selectedValue)
                        continue;
                }

                if (hoveredResizeHandlePair.HasValue)
                {
                    ResizeHandlePair hoveredValue = hoveredResizeHandlePair.Value;
                    if (resizeHandlePairs[i] == hoveredValue)
                        continue;
                }

                Color color;
				if(!isValidToResize)
				{
					color = Color.grey;
				}
				else
				{
	                if (resizeHandlePairs[i].ResizeType == ResizeType.EdgeMid)
	                {
						color = Color.white;
	                }
	                else
	                {
						color = Color.yellow;
					}
				}

				// If this point faces away from the camera, then it should reduce the alpha
				color.a = 0.5f;
				
				Vector3 direction;
				Vector3 target;

				direction = TransformDirection(resizeHandlePairs[i].point1);

				int handleSize = 8;	
				if(isAxisAlignedCamera || Vector3.Dot(sceneViewCamera.transform.forward, direction) < 0)
				{
					color.a = 1f;
					handleSize = 8;
				}
				else
				{
					color.a = 0.5f;
					handleSize = 6;
				}
				GL.Color(color);

				target = sceneViewCamera.WorldToScreenPoint(TransformPoint(bounds.center + resizeHandlePairs[i].point1.Multiply(bounds.extents)));

				if (target.z > 0)
                {
                    // Make it pixel perfect
                    target = MathHelper.RoundVector3(target);
					SabreGraphics.DrawBillboardQuad(target, handleSize, handleSize);
				}

				direction = TransformDirection(resizeHandlePairs[i].point2);

				if(isAxisAlignedCamera || Vector3.Dot(sceneViewCamera.transform.forward, direction) < 0)
				{
					color.a = 1f;
					handleSize = 8;
				}
				else
				{
					color.a = 0.5f;
					handleSize = 6;
				}
				GL.Color(color);

				target = sceneViewCamera.WorldToScreenPoint(TransformPoint(bounds.center + resizeHandlePairs[i].point2.Multiply(bounds.extents)));

                if (target.z > 0)
                {
                    // Make it pixel perfect
                    target = MathHelper.RoundVector3(target);
					SabreGraphics.DrawBillboardQuad(target, handleSize, handleSize);
				}
			}

            GL.End();

            Vector2 screenPosition = new Vector2(Screen.width / 2, Screen.height / 2);


            // Draw the selected in green
            if (selectedResizeHandlePair.HasValue)
            {
				SabreCSGResources.GetSelectedBrushMaterial().SetPass(0);
                GL.Begin(GL.LINES);
                GL.Color(Color.green);

                Vector3 screenPosition1 = sceneViewCamera.WorldToScreenPoint(TransformPoint(bounds.center + selectedResizeHandlePair.Value.point1.Multiply(bounds.extents)));
                Vector3 screenPosition2 = sceneViewCamera.WorldToScreenPoint(TransformPoint(bounds.center + selectedResizeHandlePair.Value.point2.Multiply(bounds.extents)));
				SabreGraphics.DrawScreenLine(screenPosition1, screenPosition2);

                GL.End();

				SabreCSGResources.GetGizmoMaterial().SetPass(0);
                GL.Begin(GL.QUADS);
                GL.Color(Color.green);

                Vector3 target;
                int size = (selectedResizePointIndex == 0) ? 8 : 6;
                target = screenPosition1;
                // Make it pixel perfect
                target = MathHelper.RoundVector3(target);
                SabreGraphics.DrawBillboardQuad(target, size, size);


                size = (selectedResizePointIndex == 1) ? 8 : 6;
                target = screenPosition2;
                // Make it pixel perfect
                target = MathHelper.RoundVector3(target);
                SabreGraphics.DrawBillboardQuad(target, size, size);
               
                GL.End();

                screenPosition = Vector3.Lerp(screenPosition1, screenPosition2, 0.5f);
            }
            else if (hoveredResizeHandlePair.HasValue)
            {
                Color color = Color.green;
                
                
                SabreCSGResources.GetSelectedBrushDashedAlphaMaterial().SetPass(0);
                GL.Begin(GL.LINES);

                color.a = 0.5f;
                GL.Color(color);

                Vector3 screenPosition1 = sceneViewCamera.WorldToScreenPoint(TransformPoint(bounds.center + hoveredResizeHandlePair.Value.point1.Multiply(bounds.extents)));
                Vector3 screenPosition2 = sceneViewCamera.WorldToScreenPoint(TransformPoint(bounds.center + hoveredResizeHandlePair.Value.point2.Multiply(bounds.extents)));
                SabreGraphics.DrawScreenLineDashed(screenPosition1, screenPosition2);

                GL.End();

                SabreCSGResources.GetGizmoMaterial().SetPass(0);
                GL.Begin(GL.QUADS);

                if (hoveredResizeHandlePair.Value.ResizeType == ResizeType.EdgeMid)
                {
                    color = Color.white;
                }
                else
                {
                    color = Color.yellow;
                }

                color = Color.Lerp(color, Color.green, 0.7f);
                GL.Color(color);

                Vector3 target;
                int size = (hoveredResizePointIndex == 0) ? 8 : 6;
                target = screenPosition1;
                // Make it pixel perfect
                target = MathHelper.RoundVector3(target);
                SabreGraphics.DrawBillboardQuad(target, size, size);


                size = (hoveredResizePointIndex == 1) ? 8 : 6;
                target = screenPosition2;
                // Make it pixel perfect
                target = MathHelper.RoundVector3(target);
                SabreGraphics.DrawBillboardQuad(target, size, size);

                GL.End();

                screenPosition = Vector3.Lerp(screenPosition1, screenPosition2, 0.5f);
            }

            GL.PopMatrix();

//            GUI.backgroundColor = Color.red;

			if (selectedResizeHandlePair.HasValue)
			{
                // Draw the angle or bounds to let the user know about the current edit operation
	            GUIStyle style = new GUIStyle(EditorStyles.toolbar);
                if (currentMode != ActiveMode.Rotate)
                {
                    style.fixedHeight = 28 * 3;
                }
                else
                {
                    style.fixedHeight = 28;
                }
                style.normal.background = SabreCSGResources.ClearTexture;
                style.fontSize = 16;

                string messageToMeasure; // Construct a separate message that doesn't include rich text markup to accurately measure
				if(currentMode != ActiveMode.Rotate)
				{
                    messageToMeasure = "";
                    
                    Vector3 resizeDirection = selectedResizeHandlePair.Value.point1;
                    message = "";
                    // Construct the rich text message, tagging axes that are being edited as bold
                    for (int i = 0; i < 3; i++)
                    {
                        if(i > 0)
                        {
                            message += "\n";
                            messageToMeasure += "\n";
                        }

                        float sizeComponent = bounds.size[i];
                        sizeComponent = MathHelper.RoundFloat(sizeComponent, 0.0001f);
                        string formattedComponent = sizeComponent.ToString(CultureInfo.InvariantCulture);

                        messageToMeasure += formattedComponent;

                        if (resizeDirection[i] != 0)
                        {
                            message += "<b>" + formattedComponent + "</b>";
                        }
                        else
                        {
                            message += formattedComponent;
                        }
                    }
                }
				else
				{
                    // Rotation mode populates the message elsewhere
                    messageToMeasure = message;
                }

                // Position to draw the message at
                screenPosition = EditorHelper.ConvertMousePixelPosition(screenPosition, true);

                Vector2 calculatedSize = style.CalcSize(new GUIContent(messageToMeasure));
                float width = calculatedSize.x + 30;
                float height = calculatedSize.y;

                // Make the message centered around the position
                if (currentMode != ActiveMode.Rotate)
                {
                    screenPosition -= new Vector2(Mathf.Round(width / 2), Mathf.Round(height / 2) - 24);
                }
                else
                {
                    screenPosition -= new Vector2(Mathf.Round(width / 2), Mathf.Round(height / 2) - 16);
                }
                Rect rect = new Rect(screenPosition.x, screenPosition.y, width, height);
                // Draw the message
                GUILayout.Window(140008, rect, OnMessageWindow, "", style);
            }
        }

		void TranslateBrushes(Vector3 worldDelta)
		{
			List<Transform> rootTransforms = TransformHelper.GetRootSelectionOnly(targetBrushTransforms);
			Undo.RecordObjects(rootTransforms.ToArray(), "Move brush(es)");

			for (int i = 0; i < rootTransforms.Count; i++) 
			{
				targetBrushTransforms[i].position += worldDelta;
			}

			for (int brushIndex = 0; brushIndex < targetBrushBases.Length; brushIndex++) 
			{
				EditorHelper.SetDirty(targetBrushBases[brushIndex]);
				EditorHelper.SetDirty(targetBrushBases[brushIndex].transform);
				targetBrushBases[brushIndex].Invalidate(true);
			}
		}

		void RotateBrushes(Quaternion rotationDelta, Vector3 sourceWorldPosition)
		{
			List<Transform> rootTransforms = TransformHelper.GetRootSelectionOnly(targetBrushTransforms);
			Undo.RecordObjects(rootTransforms.ToArray(), "Rotate brush(es)");

			for (int i = 0; i < rootTransforms.Count; i++) 
			{
				targetBrushTransforms[i].rotation = rotationDelta * rootTransforms[i].rotation;

				Vector3 localPosition = rootTransforms[i].position - sourceWorldPosition;
				localPosition = rotationDelta * localPosition;
				targetBrushTransforms[i].position = localPosition + sourceWorldPosition;
			}	

			for (int brushIndex = 0; brushIndex < targetBrushes.Length; brushIndex++) 
			{
				EditorHelper.SetDirty(targetBrushBases[brushIndex]);
				EditorHelper.SetDirty(targetBrushBases[brushIndex].transform);
				targetBrushBases[brushIndex].Invalidate(true);
			}
		}

        private void OnMessageWindow(int id)
        {
            // Resize and rotate messages are drawn differently
            bool isResizeMessage = message.Contains("\n");

            // Background color of the message
            GUI.backgroundColor = new Color(0, 0.5f, 0, 1);

            GUIStyle boxStyle = new GUIStyle(GUI.skin.button);

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.margin = boxStyle.margin;
            labelStyle.padding = boxStyle.padding;
            labelStyle.fontSize = 16;
            labelStyle.richText = true;
            labelStyle.font = EditorStyles.miniBoldFont;
            if (isResizeMessage)
            {
                labelStyle.alignment = TextAnchor.MiddleLeft;
            }
            else
            {
                labelStyle.alignment = TextAnchor.MiddleCenter;
            }

            labelStyle.normal.textColor = Color.white;

            Rect rect = GUILayoutUtility.GetRect(new GUIContent(message), labelStyle, GUILayout.ExpandWidth(true));
            // Draw the background
            GUI.Box(rect, new GUIContent(), EditorStyles.helpBox);

            if(isResizeMessage)
            {
                rect.xMin += 10;
            }
            // Draw the message text
            GUI.Box(rect, message, labelStyle);

            if (isResizeMessage)
            {
                // In resize mode draw three coloured squares to indicate the axis (RGB = XYZ)
                SabreGUILayout.DrawOutlinedBox(new Rect(12, 12 + 0, 6, 6), Color.red);
                SabreGUILayout.DrawOutlinedBox(new Rect(12, 12 + 18, 6, 6), Color.green);
                SabreGUILayout.DrawOutlinedBox(new Rect(12, 12 + 36, 6, 6), Color.blue);
            }
        }

        public override void ResetTool ()
		{
			SabreMouse.ResetCursor();

			if(primaryTargetBrushTransform != null)
			{
				startPosition = primaryTargetBrushTransform.position;
			}
		}

		public override void Deactivated ()
		{
			// Make sure we get rid of any active custom cursor
			SabreMouse.ResetCursor();
		}
    }
}
#endif