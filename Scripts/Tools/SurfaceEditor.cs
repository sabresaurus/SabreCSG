#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Array = System.Array;

namespace Sabresaurus.SabreCSG
{
    public class SurfaceEditor : Tool
    {
		bool selectHelpersVisible = false;
		enum Mode { None, Translate, Rotate };
		enum AlignDirection { Top, Bottom, Left, Right, Center };

		Mode currentMode = Mode.None;
		bool undoRecorded = false; // Used so that when translating or rotating undo is only recorded at the start, not per frame

		List<Polygon> selectedSourcePolygons = new List<Polygon>();
		Dictionary<Polygon, Brush> matchedBrushes = new Dictionary<Polygon, Brush>();

		Polygon lastSelectedPolygon = null;
		// The primary polygon being interacted with, this is the polygon that the drag started on
		Polygon currentPolygon = null;
		float rotationDiameter = 0;

		// Used so that the MouseUp event knows that it was the end of a drag not the end of a click
		bool dragging = false;

		bool limitToSameMaterial = false;

		Vector3 lastWorldPoint;
		Vector3 currentWorldPoint;
		bool pointSet = false;

		// Used to preserve movement while snapping
		Vector2 totalDelta;
		Vector2 appliedDelta;

		// Scale
		string scaleAmount = "1";

		string uScaleString = null;
		string vScaleString = null;

		string uOffsetString = null;
		string vOffsetString = null;

		// Rotation on UI
		float rotationAmount = 0;

		// Used to preserve movement while snapping
		float fullDeltaAngle = 0;
		float unroundedDeltaAngle = 0;

		// Main UI rectangle for this tool's UI
		readonly Rect toolbarRect = new Rect(6, 40, 200, 226);

        // Used to track what polygons have been previously clicked on, so that the user can cycle click through objects
        // on the same (or similar) ray cast
        List<Polygon> previousHits = new List<Polygon>();
        List<Polygon> lastHitSet = new List<Polygon>();

        Rect alignButtonRect = new Rect(118,110,80,45);

		Material lastMaterial = null;
		Color lastColor = Color.white;

		bool copyMaterialHeld = false;

		VertexColorWindow vertexColorWindow = null;

		Rect ToolbarRect
		{
			get
			{
				Rect rect = new Rect(toolbarRect);
				if(selectHelpersVisible)
				{
					rect.height += 116;
				}
				return rect;
			}
		}

		public override void OnSceneGUI(SceneView sceneView, Event e)
        {
			base.OnSceneGUI(sceneView, e); // Allow the base logic to calculate first

			// GUI events
			OnRepaintGUI(sceneView, e);

			if (e.type == EventType.KeyDown || e.type == EventType.KeyUp)
			{
				OnKeyAction(sceneView, e);
			}
			else if (e.type == EventType.Repaint)
			{
				OnRepaint(sceneView, e);
			}
			else if(e.type == EventType.MouseMove)
			{
				SceneView.RepaintAll();
			}
			else if(e.type == EventType.MouseDrag)
			{
				OnMouseDrag(sceneView, e);
			}
			else if(e.type == EventType.MouseDown 
				&& !EditorHelper.IsMousePositionInInvalidRects(e.mousePosition))
			{
				OnMouseDown(sceneView, e);
			}
			else if(e.type == EventType.MouseUp
				&& !EditorHelper.IsMousePositionInInvalidRects(e.mousePosition))
			{
				OnMouseUp(sceneView, e);
			}
			else if(e.type == EventType.DragPerform)
			{
				OnDragPerform(sceneView, e);
			}
			else if(e.type == EventType.DragUpdated)
			{
				e.Use();
			}
        }
		
		void OnMouseDown (SceneView sceneView, Event e)
		{
			if(e.button != 0
				|| (SabreInput.AnyModifiersSet(e) && !(SabreInput.IsModifier(e, EventModifiers.Control) || SabreInput.IsModifier(e, EventModifiers.Shift)))
				|| CameraPanInProgress
				|| EditorHelper.IsMousePositionInIMGUIRect(e.mousePosition, ToolbarRect))
			{
				return;
			}


			if(copyMaterialHeld) // Copy material
			{
				if(!EditorHelper.IsMousePositionInIMGUIRect(e.mousePosition, ToolbarRect))
				{
					Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
					Polygon polygon = csgModel.RaycastBuiltPolygons(ray);

					if(polygon != null)
					{
						Polygon sourcePolygon = csgModel.GetSourcePolygon(polygon.UniqueIndex);

						if(sourcePolygon != null)
						{
							if(!matchedBrushes.ContainsKey(sourcePolygon))
							{
								matchedBrushes.Add(sourcePolygon, csgModel.FindBrushFromPolygon(sourcePolygon));
							}

							if(sourcePolygon.Material != lastMaterial)
							{
								ChangePolygonMaterial(sourcePolygon, lastMaterial);
							}
							ChangePolygonColor(sourcePolygon, lastColor);
						}
					}
				}
			}
			else // Set currentPolygon for drag operations
			{
				Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

                // Get all the polygons the ray hits
                List<Polygon> raycastHits = csgModel.RaycastBuiltPolygonsAll(ray).Select(hit => csgModel.GetSourcePolygon(hit.Polygon.UniqueIndex)).Where(item => item != null).ToList();
                Polygon sourcePolygon = null;

                // Walk through the hits from front to back and find if any of them are in the selection set
                for (int i = 0; i < raycastHits.Count; i++)
                {
                    if(selectedSourcePolygons.Contains(raycastHits[i]))
                    {
                        sourcePolygon = raycastHits[i];
                        break;
                    }
                }

                // None of the hit polygons are in the selection set, so just use the first hit polygon if it's available
                if(sourcePolygon == null && raycastHits.Count >= 1)
                {
                    sourcePolygon = raycastHits[0];
                }

                // If a polygon has been hit
				if(sourcePolygon != null)
                {
                    // Reset drag values
                    totalDelta = Vector2.zero;
                    appliedDelta = Vector2.zero;

                    // Set the current polygon from the hit polygon, if used in a drag operation the drag code will add it to selection if it's not already in there
                    currentPolygon = sourcePolygon;

                    Brush matchedBrush = csgModel.FindBrushFromPolygon(currentPolygon);
                    Transform brushTransform = matchedBrush.transform;

                    Vector3[] transformedPositions = new Vector3[currentPolygon.Vertices.Length];

                    for (int j = 0; j < currentPolygon.Vertices.Length; j++)
                    {
                        transformedPositions[j] = brushTransform.TransformPoint(currentPolygon.Vertices[j].Position);
                    }

                    // Calculate the diameter of the the bounding circle for the transformed polygon (polygon aligned), used for rotation mode
                    for (int j = 1; j < transformedPositions.Length; j++)
                    {
                        float distance = Vector3.Distance(transformedPositions[1], transformedPositions[0]);
                        if (distance > rotationDiameter)
                        {
                            rotationDiameter = distance;
                        }
                    }

                    dragging = false;

                    Vertex vertex1;
                    Vertex vertex2;
                    Vertex vertex3;
                    // Get the three non-colinear vertices which will give us a valid plane
                    SurfaceUtility.GetPrimaryPolygonDescribers(currentPolygon, out vertex1, out vertex2, out vertex3);
                    Plane plane = new Plane(brushTransform.TransformPoint(vertex1.Position),
                        brushTransform.TransformPoint(vertex2.Position),
                        brushTransform.TransformPoint(vertex3.Position));

                    float rayDistance;

                    if (plane.Raycast(ray, out rayDistance))
                    {
                        Vector3 worldPoint = ray.GetPoint(rayDistance);

                        lastWorldPoint = worldPoint;
                    }

                    // Set the appropiate mode based on whether the user wants to rotate or translate
                    if (e.control && !e.shift)
                    {
                        currentMode = Mode.Rotate;
                    }
                    else if (!e.control && !e.shift)
                    {
                        currentMode = Mode.Translate;
                    }
                    else
                    {
                        currentMode = Mode.None;
                    }

                    // Reset undo recorded state, so that we record an undo state at first valid opportunity
                    undoRecorded = false;

                    e.Use();
                }
            }
		}

		void OnMouseDrag(SceneView sceneView, Event e)
		{
			if(e.button != 0 || CameraPanInProgress)
			{
				return;
			}

			// Used so that the MouseUp event knows that it was the end of a drag not the end of a click
			dragging = true; 

			if (currentMode == Mode.Rotate)
			{
				OnMouseDragRotate(sceneView, e);
			}
			else if (currentMode == Mode.Translate)
			{
				OnMouseDragTranslate(sceneView, e);
			}
			else if(copyMaterialHeld) // Copy material
			{
				if(!EditorHelper.IsMousePositionInIMGUIRect(e.mousePosition, ToolbarRect))
				{
					Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
					Polygon polygon = csgModel.RaycastBuiltPolygons(ray);

					if(polygon != null)
					{
						Polygon sourcePolygon = csgModel.GetSourcePolygon(polygon.UniqueIndex);

						if(sourcePolygon != null)
						{
							if(!matchedBrushes.ContainsKey(sourcePolygon))
							{
								matchedBrushes.Add(sourcePolygon, csgModel.FindBrushFromPolygon(sourcePolygon));
							}

							if(sourcePolygon.Material != lastMaterial)
							{
								ChangePolygonMaterial(sourcePolygon, lastMaterial);
							}
							ChangePolygonColor(sourcePolygon, lastColor);
						}
					}
				}
			}
		}

        void EnsureCurrentPolygonSelected()
        {
            Event e = Event.current;

            if (currentPolygon != null && !selectedSourcePolygons.Contains(currentPolygon))
            {
                if(!e.shift && !e.control)
                {
                    ResetSelection();
                }

                selectedSourcePolygons.Add(currentPolygon);
                matchedBrushes.Add(currentPolygon, csgModel.FindBrushFromPolygon(currentPolygon));
                lastSelectedPolygon = currentPolygon;
            }
        }

		void OnMouseDragTranslate (SceneView sceneView, Event e)
		{
            EnsureCurrentPolygonSelected();

            if (currentPolygon != null && matchedBrushes.ContainsKey(currentPolygon))
			{
				Transform brushTransform = matchedBrushes[currentPolygon].transform;

				Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

				Vertex vertex1;
				Vertex vertex2;
				Vertex vertex3;
				// Get the three non-colinear vertices which will give us a valid plane
				SurfaceUtility.GetPrimaryPolygonDescribers(currentPolygon, out vertex1, out vertex2, out vertex3);
				Plane plane = new Plane(brushTransform.TransformPoint(vertex1.Position), 
					brushTransform.TransformPoint(vertex2.Position), 
					brushTransform.TransformPoint(vertex3.Position));

				float distance;

				if(plane.Raycast(ray, out distance))
				{
					Vector3 worldPoint = ray.GetPoint(distance);

					currentWorldPoint = worldPoint;
					Vector3 worldDelta = currentWorldPoint - lastWorldPoint;
					pointSet = true;

					UVOrientation worldOrientation = SurfaceUtility.GetNorthEastVectors(currentPolygon, brushTransform);

					Vector3 worldVectorNorth = worldOrientation.NorthVector;
					Vector3 worldVectorEast = worldOrientation.EastVector;

					// Calculate the change in UV from the world delta
					float uvNorthDelta = Vector3.Dot(worldVectorNorth, -worldDelta);
					float uvEastDelta = Vector3.Dot(worldVectorEast, -worldDelta);

					// Discount scale from the translation
					float uvNorthScale = 1f / worldOrientation.NorthScale;
					float uvEastScale = 1f / worldOrientation.EastScale;

					Vector2 uvDelta = new Vector2(uvEastDelta * uvEastScale, uvNorthDelta * uvNorthScale);

					if(CurrentSettings.PositionSnappingEnabled)
					{
						totalDelta += uvDelta;
						
						float snapDistance = CurrentSettings.PositionSnapDistance;
						
						Vector2 roundedTotal = new Vector2(MathHelper.RoundFloat(totalDelta.x, uvEastScale * snapDistance),
							MathHelper.RoundFloat(totalDelta.y, uvNorthScale * snapDistance));
						uvDelta = roundedTotal - appliedDelta;
						appliedDelta += uvDelta;// - totalDelta;
					}

					bool recordUndo = false;
					if(!undoRecorded && uvDelta != Vector2.zero)
					{
						recordUndo = true;
						undoRecorded = true;
					}

					TransformUVs(UVUtility.TranslateUV, new UVUtility.TransformData(uvDelta,0), recordUndo);

					lastWorldPoint = currentWorldPoint;

					e.Use ();
				}
			}
		}


		void OnMouseDragRotate(SceneView sceneView, Event e)
		{
            EnsureCurrentPolygonSelected();

            if (currentPolygon != null && matchedBrushes.ContainsKey(currentPolygon))
			{
				Transform brushTransform = matchedBrushes[currentPolygon].transform;

				Vertex vertex1;
				Vertex vertex2;
				Vertex vertex3;
				// Get the three non-colinear vertices which will give us a valid plane
				SurfaceUtility.GetPrimaryPolygonDescribers(currentPolygon, out vertex1, out vertex2, out vertex3);
				Plane plane = new Plane(brushTransform.TransformPoint(vertex1.Position), 
					brushTransform.TransformPoint(vertex2.Position), 
					brushTransform.TransformPoint(vertex3.Position));

				// Rotation will be around this axis
				Vector3 rotationAxis = plane.normal;
				// Brush center point
				Vector3 centerWorld = brushTransform.TransformPoint(currentPolygon.GetCenterPoint());

				// Where the mouse was last frame
				Vector2 lastPosition = e.mousePosition - e.delta;
				// Where the mouse is this frame
				Vector2 currentPosition = e.mousePosition;
				
				Ray lastRay = Camera.current.ScreenPointToRay(EditorHelper.ConvertMousePointPosition(lastPosition));
				Ray currentRay = Camera.current.ScreenPointToRay(EditorHelper.ConvertMousePointPosition(currentPosition));

				float lastRayHit;
				float currentRayHit;

				// Find where in the last frame the mouse ray intersected with this polygon's plane
				if(plane.Raycast(lastRay, out lastRayHit))
				{
					// Find where in the current frame the mouse ray intersected with this polygon's plane
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
						
						// If snapping is enabled, apply snapping to the delta angle
						if(CurrentSettings.AngleSnappingEnabled)
						{
							deltaAngle += unroundedDeltaAngle;
							
							float roundedAngle = MathHelper.RoundFloat(deltaAngle, CurrentSettings.AngleSnapDistance);
							// Store the change in angle that hasn't been applied due to snapping
							unroundedDeltaAngle = deltaAngle - roundedAngle;
							// Snap out delta angle for the snapped delta
							deltaAngle = roundedAngle;
						}
						fullDeltaAngle += deltaAngle;
						
	//					Undo.RecordObject(targetBrushTransform, "Rotated brush(es)");

						bool recordUndo = false;
						if(!undoRecorded)
						{
							recordUndo = true;
							undoRecorded = true;
						}

						// Rotate the UV using the supplied angle
						RotateAroundCenter(deltaAngle, recordUndo);

						e.Use();
					}
				}
				
				SabreMouse.SetCursor(MouseCursor.RotateArrow);
			}
		}

		void OnMouseUp (SceneView sceneView, Event e)
		{
            // Normal selection mode
            if (e.button == 0 && !CameraPanInProgress 
				&& (!SabreInput.AnyModifiersSet(e) || SabreInput.IsModifier(e, EventModifiers.Control) || SabreInput.IsModifier(e, EventModifiers.Shift))
				&& !copyMaterialHeld)
			{ 
				currentMode = Mode.None;
				undoRecorded = false;
				pointSet = false;
				SabreMouse.ResetCursor();

				if(!dragging && !EditorHelper.IsMousePositionInIMGUIRect(e.mousePosition, ToolbarRect))
				{
					Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

                    // Get all the polygons that the ray hits, sorted from front to back
                    List<Polygon> raycastHits = csgModel.RaycastBuiltPolygonsAll(ray).Select(hit => csgModel.GetSourcePolygon(hit.Polygon.UniqueIndex)).Where(item => item != null).ToList();

                    // If no modifiers are held, reset selection
                    if (!e.shift && !e.control)
					{
						currentPolygon = null;
						ResetSelection();
						e.Use();
					}

                    // Find the hit polygon
                    Polygon selectedPolygon = null;

                    if (raycastHits.Count == 0) // Didn't hit anything, blank the selection
                    {
                        previousHits.Clear();
                        lastHitSet.Clear();
                    }
                    else if (raycastHits.Count == 1 // Only hit one thing, no ambiguity, this is what is selected
                        || e.shift || e.control) // User is trying to multiselect, let's not make life difficult for them by only accepting the nearest polygon
                    {
                        selectedPolygon = raycastHits[0];
                        previousHits.Clear();
                        lastHitSet.Clear();
                    }
                    else
                    {
                        if (!raycastHits.ContentsEquals(lastHitSet)) // If the hit polygons have changed, cycle click is not valid, default to first hit
                        {
                            selectedPolygon = raycastHits[0];
                            previousHits.Clear();
                            lastHitSet = raycastHits;
                        }
                        else
                        {
                            // First try and select anything other than what has been previously hit
                            for (int i = 0; i < raycastHits.Count; i++)
                            {
                                if (!previousHits.Contains(raycastHits[i]))
                                {
                                    selectedPolygon = raycastHits[i];
                                    break;
                                }
                            }

                            // Only found previously hit objects
                            if (selectedPolygon == null)
                            {
                                // Walk backwards to find the oldest previous hit that has been hit by this ray
                                for (int i = previousHits.Count - 1; i >= 0 && selectedPolygon == null; i--)
                                {
                                    for (int j = 0; j < raycastHits.Count; j++)
                                    {
                                        if (raycastHits[j] == previousHits[i])
                                        {
                                            selectedPolygon = previousHits[i];
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    
                    if (selectedPolygon != null)
                    {
                        // If a valid polygon has been selected, make sure it's the most recent in the history
                        previousHits.Remove(selectedPolygon);
                        // Most recent hit
                        previousHits.Insert(0, selectedPolygon);
                  
                        // If holding control or shift, the action counts as selection toggle (if already selected it's removed)
                        if (EnumHelper.IsFlagSet(e.modifiers, EventModifiers.Shift)
                        || EnumHelper.IsFlagSet(e.modifiers, EventModifiers.Control))
                        {
                            if (selectedSourcePolygons.Contains(selectedPolygon))
                            {
                                selectedSourcePolygons.Remove(selectedPolygon);
                                matchedBrushes.Remove(selectedPolygon);
                            }
                            else
                            {
                                selectedSourcePolygons.Add(selectedPolygon);
                                lastSelectedPolygon = selectedPolygon;
                                matchedBrushes.Add(selectedPolygon, csgModel.FindBrushFromPolygon(selectedPolygon));
                            }
                        }
                        else // No modifier pressed, add the polygon to selection
                        {
                            selectedSourcePolygons.Add(selectedPolygon);
                            lastSelectedPolygon = selectedPolygon;
                            matchedBrushes.Add(selectedPolygon, csgModel.FindBrushFromPolygon(selectedPolygon));
                        }
                    }

					// No faces selected, so make sure no objects are selected too
					if(selectedSourcePolygons.Count == 0)
					{
						Selection.activeGameObject = null;
					}
				}

				dragging = false;
			}
			else if(e.button == 0 && SabreInput.IsModifier(e, EventModifiers.Shift | EventModifiers.Control)) // Follow last face
			{
				if(!EditorHelper.IsMousePositionInIMGUIRect(e.mousePosition, ToolbarRect))
				{
					Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
					Polygon polygon = csgModel.RaycastBuiltPolygons(ray);

					if(polygon != null)
					{
						Polygon targetPolygon = csgModel.GetSourcePolygon(polygon.UniqueIndex);

						if(targetPolygon != null && targetPolygon != lastSelectedPolygon)
						{
							matchedBrushes.Add(targetPolygon, csgModel.FindBrushFromPolygon(targetPolygon));

							FollowLastFace(targetPolygon);
						}
					}
				}
			}
 			
		}

		void FollowLastFace(Polygon sourceTargetPolygon)
		{
			// Use UVs on lastSelectedPolygon as a template for targetPolygon
			Polygon[] sourceBuiltPolygons = csgModel.BuiltPolygonsByIndex(lastSelectedPolygon.UniqueIndex);
			Polygon[] targetBuiltPolygons = csgModel.BuiltPolygonsByIndex(sourceTargetPolygon.UniqueIndex);

			// Walk through all the built polygons from the last face and the newly selected face
			for (int sourceIndex = 0; sourceIndex < sourceBuiltPolygons.Length; sourceIndex++) 
			{
				for (int targetIndex = 0; targetIndex < targetBuiltPolygons.Length; targetIndex++) 
				{
					Edge matchedEdge1;
					Edge matchedEdge2;

					// Find if any of the two sets of built polygons match
					if(EdgeUtility.FindSharedEdge(sourceBuiltPolygons[sourceIndex], targetBuiltPolygons[targetIndex], out matchedEdge1, out matchedEdge2))
					{
						Polygon chosenTemplatePolygon = sourceBuiltPolygons[sourceIndex];
						Polygon chosenTargetPolygon = targetBuiltPolygons[targetIndex];

						Brush brush = csgModel.FindBrushFromPolygon(sourceTargetPolygon);

						Vector3 edge1Vector = (matchedEdge1.Vertex2.Position - matchedEdge1.Vertex1.Position).normalized;
						Vector3 sourcePosition = matchedEdge1.Vertex1.Position - Vector3.Cross(edge1Vector, chosenTemplatePolygon.Plane.normal);
//						VisualDebug.AddPoint(sourcePosition);
						Vector2 sourceUV = GeometryHelper.GetUVForPosition(chosenTemplatePolygon, sourcePosition);

						Vector3 targetPosition1;
						Vector3 targetPosition2;
						Vector3 targetPosition3;
						Vector2 targetUV1;
						Vector2 targetUV2;
						Vector2 targetUV3;

						if(chosenTemplatePolygon.Vertices.Length == 3 && chosenTargetPolygon.Vertices.Length == 3
							&& MathHelper.PlaneEqualsLooser(chosenTemplatePolygon.Plane, chosenTargetPolygon.Plane))
						{
							// Special logic for handling two coplanar triangles
							targetPosition1 = chosenTargetPolygon.Vertices[0].Position;
							targetPosition2 = chosenTargetPolygon.Vertices[1].Position;
							targetPosition3 = chosenTargetPolygon.Vertices[2].Position;


							targetUV1 = chosenTemplatePolygon.Vertices[0].UV;
							targetUV2 = chosenTemplatePolygon.Vertices[1].UV;
							targetUV3 = chosenTemplatePolygon.Vertices[2].UV;

							Vector2 uvDelta = targetUV1-targetUV2;
							targetUV1 += uvDelta;
							targetUV2 += uvDelta;
							targetUV3 += uvDelta;
						}
						else
						{
							targetPosition1 = matchedEdge1.Vertex1.Position;
							targetPosition2 = matchedEdge1.Vertex1.Position + (matchedEdge1.Vertex2.Position-matchedEdge1.Vertex1.Position);
							targetPosition3 = matchedEdge1.Vertex1.Position - Vector3.Cross(edge1Vector, chosenTargetPolygon.Plane.normal);


							targetUV1 = matchedEdge1.Vertex1.UV;
							targetUV2 = matchedEdge1.Vertex2.UV;
							targetUV3 = sourceUV;
						}
						bool flipY = false;

						float angleBetweenFaces = Vector3.Angle(chosenTemplatePolygon.Plane.normal, chosenTargetPolygon.Plane.normal);

						// Flip Y if there's been a 90 degree angle change
						if(angleBetweenFaces >= 89.99f)
						{
							if(matchedEdge1.Vertex1.UV.y.EqualsWithEpsilon(1)
								&& matchedEdge1.Vertex2.UV.y.EqualsWithEpsilon(1))
							{
								flipY = true;
							}
						}

						// Update the source polygons
						for (int i = 0; i < sourceTargetPolygon.Vertices.Length; i++) 
						{
							Vector3 inputPosition = sourceTargetPolygon.Vertices[i].Position;
							inputPosition = brush.transform.TransformPoint(inputPosition);
							Vector2 newUV = GeometryHelper.GetUVForPosition(targetPosition1,
								targetPosition2,
								targetPosition3,
								targetUV1,
								targetUV2,
								targetUV3,
								inputPosition);
							if(flipY)
							{
								newUV.y = 1 - newUV.y;
							}
							sourceTargetPolygon.Vertices[i].UV = newUV;
						}

						// Update the cached built polygons, so other operations like Align still work
						for (int builtPolygonIndex = 0; builtPolygonIndex < targetBuiltPolygons.Length; builtPolygonIndex++) 
						{
							Polygon builtPolygon = targetBuiltPolygons[builtPolygonIndex];
							for (int vertexIndex = 0; vertexIndex < builtPolygon.Vertices.Length; vertexIndex++) 
							{
								Vector3 position = builtPolygon.Vertices[vertexIndex].Position;

								Vector2 newUV = GeometryHelper.GetUVForPosition(targetPosition1,
									targetPosition2,
									targetPosition3,
									targetUV1,
									targetUV2,
									targetUV3,
									position);
								if(flipY)
								{
									newUV.y = 1 - newUV.y;
								}
								builtPolygon.Vertices[vertexIndex].UV = newUV;
							}
						}

						// Update the actual built mesh 
						PolygonEntry entry = csgModel.GetVisualPolygonEntry(sourceTargetPolygon.UniqueIndex);

						if(PolygonEntry.IsValidAndBuilt(entry))
						{
							Vector3[] vertices = entry.BuiltMesh.vertices;
							Vector2[] meshUVs = entry.BuiltMesh.uv;
							Vector2[] uvs = entry.UV;

							for (int i = 0; i < entry.Positions.Length; i++) 
							{
								Vector3 position = vertices[entry.BuiltVertexOffset + i];

								Vector2 newUV = GeometryHelper.GetUVForPosition(targetPosition1,
									targetPosition2,
									targetPosition3,
									targetUV1,
									targetUV2,
									targetUV3,
									position);
								if(flipY)
								{
									newUV.y = 1 - newUV.y;
								}

								uvs[i] = newUV;
								meshUVs[entry.BuiltVertexOffset + i] = newUV;
							}
							entry.UV = uvs;
							entry.BuiltMesh.uv = meshUVs;
							EditorHelper.SetDirty(entry.BuiltMesh);
						}

						ChangePolygonMaterial(sourceTargetPolygon, lastMaterial);
						ChangePolygonColor(sourceTargetPolygon, lastColor);
						// Set the last selected polygon to the one we just processed
						lastSelectedPolygon = sourceTargetPolygon;
						// Update the selection to this polygon
						selectedSourcePolygons.Clear();
						selectedSourcePolygons.Add(sourceTargetPolygon);
						matchedBrushes.Clear();
						matchedBrushes.Add(sourceTargetPolygon, brush);

						// Inform the brush cache that the polygons have changed, but that a rebuild isn't necessary
						brush.RecachePolygons(false);

						// All done, no need to test any more polygons, just return out
						return;
					}
				}
			}
		}

		float FindAngle(Vertex vertex1, Vertex vertex2, Vertex vertex3, Plane polygonPlane)
		{
			Vector3 vector1 = vertex1.Position - vertex2.Position;
			Vector3 vector2 = vertex1.Position - vertex3.Position;
			Quaternion cancellingRotation = Quaternion.Inverse(Quaternion.LookRotation(polygonPlane.normal));
			vector1 = cancellingRotation * vector1;
			vector2 = cancellingRotation * vector2;

			float angle1 = -Mathf.Atan2(vector1.x, vector1.y) * Mathf.Rad2Deg;
			float angle2 = -Mathf.Atan2(vector2.x, vector2.y) * Mathf.Rad2Deg;
			return angle1 - angle2;
		}

		void OnRepaint (SceneView sceneView, Event e)
		{
			if(vertexColorWindow != null)
			{
				vertexColorWindow.Repaint();
			}

			// Start drawing using the relevant material
			SabreCSGResources.GetSelectedBrushMaterial().SetPass(0);

			Polygon[] allPolygons;

			// Highlight the polygon the mouse is over unless they are moving the camera, or hovering over UI
			if(currentMode == Mode.None
				&& !CameraPanInProgress
				&& !EditorHelper.IsMousePositionInIMGUIRect(e.mousePosition, ToolbarRect)
				&& (!SabreInput.AnyModifiersSet(e) || SabreInput.IsModifier(e, EventModifiers.Control) || SabreInput.IsModifier(e, EventModifiers.Shift)  || SabreInput.IsModifier(e, EventModifiers.Control | EventModifiers.Shift)))
			{
				Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
				Polygon polygon = csgModel.RaycastBuiltPolygons(ray);

				// Hovered polygon
				if(polygon != null)
				{
					allPolygons = csgModel.BuiltPolygonsByIndex(polygon.UniqueIndex);
					SabreGraphics.DrawPolygons(new Color(0,1,0,0.15f), new Color(0,1,0,0.5f), allPolygons);	
				}
			}


			SabreCSGResources.GetSelectedBrushDashedMaterial().SetPass(0);
			// Draw each of the selcted polygons
			for (int i = 0; i < selectedSourcePolygons.Count; i++) 
			{
				if(selectedSourcePolygons[i] != null)
				{
					allPolygons = csgModel.BuiltPolygonsByIndex(selectedSourcePolygons[i].UniqueIndex);
					SabreGraphics.DrawPolygonsNoOutline(new Color(0,1,0,0.2f), allPolygons);
					SabreGraphics.DrawPolygonsOutlineDashed(Color.green, allPolygons);
				}
			}

			// Draw the rotation gizmo
			if(currentMode == Mode.Rotate 
				&& currentPolygon != null
				&& matchedBrushes.ContainsKey(currentPolygon))
			{
				SabreCSGResources.GetSelectedBrushMaterial().SetPass(0);

				Brush brush = matchedBrushes[currentPolygon];
				Transform brushTransform = brush.transform;

				// Polygons are stored in local space, transform the polygon normal into world space
				Vector3 normal = brushTransform.TransformDirection(currentPolygon.Plane.normal);

				Vector3 worldCenterPoint = brushTransform.TransformPoint(currentPolygon.GetCenterPoint());
				// Offset the gizmo so it's very slightly above the polygon, to avoid depth fighting
				if(brush.Mode == CSGMode.Add)
				{
					worldCenterPoint += normal * 0.02f;
				}
				else
				{
					worldCenterPoint -= normal * 0.02f;
				}

				float radius = rotationDiameter * .5f;

				Vector3 initialRotationDirection = 5 * (brushTransform.TransformPoint(currentPolygon.Vertices[1].Position) - brushTransform.TransformPoint(currentPolygon.Vertices[1].Position)).normalized;

				// Draw the actual rotation gizmo
				SabreGraphics.DrawRotationCircle(worldCenterPoint, normal, radius, initialRotationDirection);
			}

			// If the mouse is down draw a point where the mouse is interacting in world space
			if(e.button == 0 && pointSet)
			{
				Camera sceneViewCamera = sceneView.camera;

				SabreCSGResources.GetVertexMaterial().SetPass (0);
				GL.PushMatrix();
				GL.LoadPixelMatrix();
				
				GL.Begin(GL.QUADS);
				Vector3 target = sceneViewCamera.WorldToScreenPoint(currentWorldPoint);
				if(target.z > 0)
				{
					// Make it pixel perfect
					target = MathHelper.RoundVector3(target);
					SabreGraphics.DrawBillboardQuad(target, 8, 8);
				}
				GL.End();
				GL.PopMatrix();
			}
		}

		void OnDragPerform (SceneView sceneView, Event e)
		{
			if(DragAndDrop.objectReferences.Length == 1 && DragAndDrop.objectReferences[0] is Material)
			{
//				if(selectedSourcePolygons.Count > 0)
				{
					Material material = (Material)DragAndDrop.objectReferences[0];

					for (int i = 0; i < selectedSourcePolygons.Count; i++) 
					{
						if(selectedSourcePolygons[i] != null)
						{
							ChangePolygonMaterial(selectedSourcePolygons[i], material);
						}
					}
					DragAndDrop.AcceptDrag();
				}

				e.Use();
			}
		}

        void OnRepaintGUI(SceneView sceneView, Event e)
        {
            // Draw UI specific to this editor
			GUIStyle toolbar = new GUIStyle(EditorStyles.toolbar);

			// Set the background tint
			if(EditorGUIUtility.isProSkin)
			{
				toolbar.normal.background = SabreCSGResources.HalfBlackTexture;
			}
			else
			{
				toolbar.normal.background = SabreCSGResources.HalfWhiteTexture;
			}
			// Set the style height to match the rectangle (so it stretches instead of tiling)
			toolbar.fixedHeight = ToolbarRect.height;
			// Draw the actual GUI via a Window
			GUILayout.Window(140009, ToolbarRect, OnToolbarGUI, "", toolbar);
        }

		void OnKeyAction(SceneView sceneView, Event e)
		{
			if (KeyMappings.EventsMatch(e, Event.KeyboardEvent(KeyMappings.Instance.CopyMaterial)))
			{
				if(e.type == EventType.KeyDown)
				{
					copyMaterialHeld = true;
					e.Use();
				}
				else if (e.type == EventType.KeyUp)
				{
					copyMaterialHeld = false;
					e.Use();
				}
			}
		}

		void TransformUVs (UVUtility.UVTransformation transformationMethod, UVUtility.TransformData transformData, bool recordUndo)
		{
			for (int polygonIndex = 0; polygonIndex < selectedSourcePolygons.Count; polygonIndex++) 
			{
				Polygon polygon = selectedSourcePolygons[polygonIndex];

				TransformUVs(polygon, transformationMethod, transformData, recordUndo);
			}
		}

		void TransformUVs (Polygon polygon, 
			UVUtility.UVTransformation transformationMethod, 
			UVUtility.TransformData transformData,
			bool recordUndo)
		{
			Brush brush = matchedBrushes[polygon];
			if(recordUndo)
			{
				Undo.RecordObject(brush, "Transform UVs");
				csgModel.UndoRecordContext("Transform UVs");
			}

			// Update the source polygon, so rebuilding is correct
			for (int vertexIndex = 0; vertexIndex < polygon.Vertices.Length; vertexIndex++) 
			{
				Vertex vertex = polygon.Vertices[vertexIndex];
				vertex.UV = transformationMethod(vertex.UV, transformData);
//				polygon.Vertices[vertexIndex].UV = transformationMethod(polygon.Vertices[vertexIndex].UV, transformData);
			}

			// Update the built polygons in case we need to use them for something else
			Polygon[] builtPolygons = csgModel.BuiltPolygonsByIndex(polygon.UniqueIndex);
			for (int polygonIndex = 0; polygonIndex < builtPolygons.Length; polygonIndex++) 
			{
				Polygon builtPolygon = builtPolygons[polygonIndex];
				for (int vertexIndex = 0; vertexIndex < builtPolygon.Vertices.Length; vertexIndex++) 
				{
					builtPolygon.Vertices[vertexIndex].UV = transformationMethod(builtPolygon.Vertices[vertexIndex].UV, transformData);
				}
			}


			// Update the actual built mesh
			PolygonEntry entry = csgModel.GetVisualPolygonEntry(polygon.UniqueIndex);
			if(PolygonEntry.IsValidAndBuilt(entry))
			{
				if(recordUndo)
				{
					Undo.RecordObject(entry.BuiltMesh, "Transform UVs");
				}


				Vector2[] meshUVs = entry.BuiltMesh.uv;
				Vector2[] uvs = entry.UV;
				for (int vertexIndex = 0; vertexIndex < entry.Positions.Length; vertexIndex++) 
				{
					Vector2 newUV = transformationMethod(uvs[vertexIndex], transformData);
					uvs[vertexIndex] = newUV;
					meshUVs[entry.BuiltVertexOffset + vertexIndex] = newUV;
				}
				entry.UV = uvs;
				entry.BuiltMesh.uv = meshUVs;
				EditorHelper.SetDirty(entry.BuiltMesh);
			}

			// Inform the brush cache that the polygons have changed, but that a rebuild isn't necessary
			brush.RecachePolygons(false);
			EditorHelper.SetDirty(brush as PrimitiveBrush);
		}

		void RotateAroundCenter (float rotationAmount, bool recordUndo)
		{
			for (int polygonIndex = 0; polygonIndex < selectedSourcePolygons.Count; polygonIndex++) 
			{
				Polygon polygon = selectedSourcePolygons[polygonIndex];
				
				Vector2 centerUV = polygon.GetCenterUV();
				
				Vector2 uvDelta1 = (polygon.Vertices[2].UV - polygon.Vertices[1].UV).normalized;
				// Find the UV delta along an adjacent edge too (so we can detect flipping)
				Vector2 uvDelta2 = (polygon.Vertices[0].UV - polygon.Vertices[1].UV).normalized;
				
				Vector3 uvNormal = Vector3.Cross(uvDelta1,uvDelta2).normalized;
				
				if(uvNormal.z < 0)
				{
					TransformUVs(polygon, UVUtility.RotateUV, new UVUtility.TransformData(centerUV,rotationAmount), recordUndo);
				}
				else // Flip the delta angle for flipped UVs
				{
					TransformUVs(polygon, UVUtility.RotateUV, new UVUtility.TransformData(centerUV,-rotationAmount), recordUndo);
				}
			}
		}

		public override void OnUndoRedoPerformed ()
		{
			base.OnUndoRedoPerformed ();

			List<Polygon> visualPolygons = csgModel.VisualPolygons;
			for (int i = 0; i < visualPolygons.Count; i++) 
			{
				PolygonEntry entry = csgModel.GetVisualPolygonEntry(visualPolygons[i].UniqueIndex);
				if(PolygonEntry.IsValidAndBuilt(entry))
				{
					Vector2[] meshUVs = entry.BuiltMesh.uv;
					Vector2[] uvs = entry.UV;
					for (int vertexIndex = 0; vertexIndex < entry.Positions.Length; vertexIndex++) 
					{
						meshUVs[entry.BuiltVertexOffset + vertexIndex] = uvs[vertexIndex];
					}
					entry.BuiltMesh.uv = meshUVs;
					EditorHelper.SetDirty(entry.BuiltMesh);
				}
			}
		}

		void DrawMaterialBox()
		{
			bool materialConflict = false;
			Material material = null;

			selectedSourcePolygons.RemoveAll(item => item == null);

			if(selectedSourcePolygons.Count > 0)
			{
				// Set the material to the first polygon's
				material = selectedSourcePolygons[0].Material;
				// Continue through the rest of the polygons determining if there is a conflict

				for (int i = 1; i < selectedSourcePolygons.Count; i++) 
				{
					// Different materials found
					if(selectedSourcePolygons[i].Material != material)
					{
						materialConflict = true;
					}
				}
				lastMaterial = material;
				lastColor = selectedSourcePolygons[0].Vertices[0].Color;
			}

			if(material == null)
			{
				material = CSGModel.GetDefaultMaterial();
			}

//			GUILayout.Label("Mat", SabreGUILayout.GetForeStyle());
			GUILayout.Space(2);
			GUILayout.BeginHorizontal();
			Texture2D texture = AssetPreview.GetAssetPreview(material);
			if(AssetPreview.IsLoadingAssetPreview(material.GetInstanceID()))
			{
				// Not loaded yet, so tell the scene view it needs to attempt to paint again
				SceneView.RepaintAll();
			}

			Texture secondaryTexture = null;
			if(material.HasProperty("_MainTex"))
			{
				secondaryTexture = material.GetTexture("_MainTex"); // equivalent to .mainTexture
			}

			if(secondaryTexture == null)
			{
				// Couldn't find a main texture, so use the first found texture instead
				int propertyCount = ShaderUtil.GetPropertyCount(material.shader);
				for (int i = 0; i < propertyCount; i++) 
				{
					if(ShaderUtil.GetPropertyType(material.shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
					{
						Texture newTexture = material.GetTexture(ShaderUtil.GetPropertyName(material.shader, i));
						if(newTexture != null)
						{
							// Ignore normal maps
							string assetPath = AssetDatabase.GetAssetPath(newTexture);
							TextureImporter importer = TextureImporter.GetAtPath(assetPath) as TextureImporter;

#if UNITY_5_5_OR_NEWER
                            // Unity 5.5 refactored the TextureImporter, requiring slightly different logic
                            TextureImporterSettings importerSettings = new TextureImporterSettings();
                            importer.ReadTextureSettings(importerSettings);
                            bool isNormalMap = importerSettings.textureType == TextureImporterType.NormalMap;
#else
                            // Pre Unity 5.5 way of checking if a texture is a normal map
                            bool isNormalMap = importer.normalmap;
#endif

							if(!isNormalMap)
							{
								secondaryTexture = newTexture;
								break;
							}
						}
					}
				}
			}
			int width = 64;
			GUIStyle style = new GUIStyle(GUI.skin.box);
			style.padding = new RectOffset(0,0,0,0);
			style.margin = new RectOffset(0,0,0,0);
			GUILayout.Box(texture, style, GUILayout.Width(width), GUILayout.Height(width));

//			GUILayout.Box(secondaryTexture, style, GUILayout.Width(width), GUILayout.Height(width));

			// Draw again using GL and a custom shader that ignores alpha, only draws texture RGB
			if (Event.current.type == EventType.Repaint)
			{
				Rect rect = GUILayoutUtility.GetLastRect();
				rect.position += new Vector2(rect.width, 0);
				Material drawMaterial = SabreCSGResources.GetPreviewMaterial();
				drawMaterial.mainTexture = secondaryTexture;

				if(PlayerSettings.colorSpace == ColorSpace.Linear)
				{
					drawMaterial.SetFloat("_IsLinear", 1);
				}
				else
				{
					drawMaterial.SetFloat("_IsLinear", 0);
				}
				drawMaterial.SetPass(0);

				GL.PushMatrix();

				GL.LoadIdentity();
				GL.MultMatrix(GUI.matrix);
				GL.Begin(GL.QUADS);
				GL.Color(Color.white);
				Vector2 position = rect.center;
				SabreGraphics.DrawBillboardQuad(position, (int)rect.width, (int)rect.height, false);
				GL.End();
				GL.PopMatrix();
			}


			GUILayout.EndHorizontal();

			GUILayout.Space(1);
			Material newMaterial = null;

			if(materialConflict)
			{
				material = null;

				EditorGUI.showMixedValue = true;
				newMaterial = EditorGUILayout.ObjectField(material, typeof(Material), false, GUILayout.Width(105)) as Material;
				EditorGUI.showMixedValue = false;
			}
			else
			{
				newMaterial = EditorGUILayout.ObjectField(material, typeof(Material), false, GUILayout.Width(105)) as Material;
			}


			Rect materialFieldRect = GUILayoutUtility.GetLastRect();
			Rect buttonRect = new Rect(materialFieldRect);
			buttonRect.xMin = buttonRect.xMax - 15;
			buttonRect.xMax += 25;

			if(GUI.Button(buttonRect, "Set", EditorStyles.miniButton))
			{
				int controlID = GUIUtility.GetControlID(FocusType.Passive);
//				int controlID = GUIUtility.hotControl;
				EditorGUIUtility.ShowObjectPicker<Material>(material, false, string.Empty, controlID);
			}

			if(Event.current.type == EventType.ExecuteCommand)
			{
				if(Event.current.commandName == "ObjectSelectorUpdated")
				{
					newMaterial = EditorGUIUtility.GetObjectPickerObject() as Material;
				}
//				Debug.Log("ExecuteCommand: " + Event.current.commandName);
			}

			materialFieldRect.center += ToolbarRect.min;
			materialFieldRect.center -= new Vector2(0,EditorStyles.toolbar.fixedHeight);

			if(newMaterial != material)
			{
				for (int i = 0; i < selectedSourcePolygons.Count; i++) 
				{
					ChangePolygonMaterial(selectedSourcePolygons[i], newMaterial);
				}
			}				
		}

		void DrawExcludeBox()
		{
			bool excludeConflict = false;
			bool excludeState = false;

			if(selectedSourcePolygons.Count > 0)
			{
				// Set the state to the first polygon's
				excludeState = selectedSourcePolygons[0].UserExcludeFromFinal;

				// Continue through the rest of the polygons determining if there is a conflict
				for (int i = 1; i < selectedSourcePolygons.Count; i++) 
				{
					// Different materials found
					if(selectedSourcePolygons[i].UserExcludeFromFinal != excludeState)
					{
						excludeConflict = true;
						excludeState = false;
					}
				}
			}

			GUILayout.BeginHorizontal(GUILayout.Width(50));
			Rect rect = new Rect(72, 48, 60, 15);
			bool newExcludeState = SabreGUILayout.ToggleMixed(rect, excludeState, excludeConflict, "Exclude");

			EditorGUI.showMixedValue = false; // Reset mixed state
			GUILayout.EndHorizontal();

			// Changed exclude state
			if(newExcludeState != excludeState)
			{
				// Loop through all the selected polygons and apply
				for (int i = 0; i < selectedSourcePolygons.Count; i++) 
				{
					selectedSourcePolygons[i].UserExcludeFromFinal = newExcludeState;

					if(newExcludeState)
					{
						UserExcludePolygon(selectedSourcePolygons[i]);
					}
					else
					{
						UserIncludePolygon(selectedSourcePolygons[i]);
					}

					EditorHelper.SetDirty(matchedBrushes[selectedSourcePolygons[i]]);
					// Tell the brush that the polygons have changed but that there's no need to rebuild
					matchedBrushes[selectedSourcePolygons[i]].RecachePolygons(false);
				}
			}
		}

		void DrawManualTextBoxes()
		{
			float? northScale = 1;
			float? eastScale = 1;

			for (int polygonIndex = 0; polygonIndex < selectedSourcePolygons.Count; polygonIndex++) 
			{
				Polygon polygon = selectedSourcePolygons[polygonIndex];

				Transform brushTransform = matchedBrushes[polygon].transform;

				UVOrientation worldOrientation = SurfaceUtility.GetNorthEastVectors(polygon, brushTransform);

				if(polygonIndex == 0)
				{
					northScale = worldOrientation.NorthScale;
					eastScale = worldOrientation.EastScale;
				}
				else
				{
					if(!northScale.HasValue || !northScale.Value.EqualsWithEpsilon(worldOrientation.NorthScale))
					{
						northScale = null;
					}

					if(!eastScale.HasValue || !eastScale.Value.EqualsWithEpsilon(worldOrientation.EastScale))
					{
						eastScale = null;
					}
				}
			}

			Pair<float?, float?> uvOffset = SurfaceUtility.GetUVOffset(selectedSourcePolygons);

			float? eastOffset = uvOffset.First;
			float? northOffset = uvOffset.Second;

			GUIStyle textFieldStyle1 = SabreGUILayout.GetTextFieldStyle1();
			GUIStyle textFieldStyle2 = SabreGUILayout.GetTextFieldStyle2();


			// East Scale (u scale)
			Rect rect = new Rect(138, 2, 60, 16);
			if(SabreGUILayout.DrawUVField(rect, eastScale, ref uScaleString, "uScaleField", textFieldStyle1))
			{
				float newEastScale;
				if(float.TryParse(uScaleString, out newEastScale) && newEastScale != 0)
				{					
					for (int polygonIndex = 0; polygonIndex < selectedSourcePolygons.Count; polygonIndex++) 
					{
						Polygon polygon = selectedSourcePolygons[polygonIndex];

						float originalEastScale = SurfaceUtility.GetNorthEastVectors(polygon, matchedBrushes[polygon].transform).EastScale;

						TransformUVs(polygon, UVUtility.ScaleUV, new UVUtility.TransformData(new Vector2(newEastScale/originalEastScale,1),0), false);
					}
				}
			}

			// North scale (v scale)
			rect = new Rect(138, 17, 60, 16);
			if(SabreGUILayout.DrawUVField(rect, northScale, ref vScaleString, "vScaleField", textFieldStyle1))
			{
				float newNorthScale;
				if(float.TryParse(vScaleString, out newNorthScale) && newNorthScale != 0)
				{					
					for (int polygonIndex = 0; polygonIndex < selectedSourcePolygons.Count; polygonIndex++) 
					{
						Polygon polygon = selectedSourcePolygons[polygonIndex];

						float originalNorthScale = SurfaceUtility.GetNorthEastVectors(polygon, matchedBrushes[polygon].transform).NorthScale;

						TransformUVs(polygon, UVUtility.ScaleUV, new UVUtility.TransformData(new Vector2(1, newNorthScale/originalNorthScale),0), true);
					}
				}
			}


			// North scale (v scale)
			rect = new Rect(138, 35, 60, 16);
			if(SabreGUILayout.DrawUVField(rect, eastOffset, ref uOffsetString, "uOffsetField", textFieldStyle2))
			{
				float newEastOffset;
				if(float.TryParse(uOffsetString, out newEastOffset))
				{					
					for (int polygonIndex = 0; polygonIndex < selectedSourcePolygons.Count; polygonIndex++) 
					{
						Polygon polygon = selectedSourcePolygons[polygonIndex];

						float originalEastOffset = SurfaceUtility.GetUVOffset(polygon).x;

						TransformUVs(polygon, UVUtility.TranslateUV, new UVUtility.TransformData(new Vector2(newEastOffset-originalEastOffset,0),0), true);
					}
				}
			}

			rect = new Rect(138, 50, 60, 16);
			if(SabreGUILayout.DrawUVField(rect, northOffset, ref vOffsetString, "vOffsetField", textFieldStyle2))
			{
				float newNorthOffset;
				if(float.TryParse(vOffsetString, out newNorthOffset))
				{					
					for (int polygonIndex = 0; polygonIndex < selectedSourcePolygons.Count; polygonIndex++) 
					{
						Polygon polygon = selectedSourcePolygons[polygonIndex];

						float originalNorthOffset = SurfaceUtility.GetUVOffset(polygon).y;

						TransformUVs(polygon, UVUtility.TranslateUV, new UVUtility.TransformData(new Vector2(0, newNorthOffset-originalNorthOffset),0), true);
					}
				}
			}
		}

		public Color GetColor()
		{
			if(selectedSourcePolygons.Count > 0)
			{
				return selectedSourcePolygons[0].Vertices[0].Color;
			}
			else
			{
				return Color.white;
			}
		}
				
        void OnToolbarGUI(int windowID)
        {
			// Allow the user to change the material on the selected polygons
			DrawMaterialBox();

			// Allow the user to change whether the selected polygons will be excluded from the final mesh
			DrawExcludeBox();

			GUISkin inspectorSkin = SabreGUILayout.GetInspectorSkin();

			Rect rect = new Rect(138, 68, 60, 18);

			if(GUI.Button(rect, "Color", inspectorSkin.button))
			{
				vertexColorWindow = VertexColorWindow.CreateAndShow(csgModel, this);
			}

			GUIStyle newStyle = new GUIStyle(EditorStyles.miniButton);
			newStyle.padding = new RectOffset(0,0,0,0);

			if(GUI.Button(new Rect(alignButtonRect.xMin,alignButtonRect.yMin,alignButtonRect.width,alignButtonRect.height/3), "▲", newStyle))
			{
				Align(AlignDirection.Top);
			}
			if(GUI.Button(new Rect(alignButtonRect.xMin,alignButtonRect.yMin+alignButtonRect.height/3,alignButtonRect.width/3,alignButtonRect.height/3), "◄", EditorStyles.miniButtonLeft))
			{
				Align(AlignDirection.Left);
			}
			if(GUI.Button(new Rect(alignButtonRect.xMin+alignButtonRect.width/3,alignButtonRect.yMin+alignButtonRect.height/3,alignButtonRect.width/3,alignButtonRect.height/3), "C", EditorStyles.miniButtonMid))
			{
				Align(AlignDirection.Center);
			}
			if(GUI.Button(new Rect(alignButtonRect.xMin+2*alignButtonRect.width/3,alignButtonRect.yMin+alignButtonRect.height/3,alignButtonRect.width/3,alignButtonRect.height/3), "►", EditorStyles.miniButtonRight))
			{
				Align(AlignDirection.Right);
			}
			if(GUI.Button(new Rect(alignButtonRect.xMin,alignButtonRect.yMin+2*alignButtonRect.height/3,alignButtonRect.width,alignButtonRect.height/3), "▼", newStyle))
			{
				Align(AlignDirection.Bottom);
			}

//			if(GUILayout.Button("Auto UV Local", EditorStyles.miniButton, GUILayout.Width(70)))
//			{
//				AutoUV(false);
//			}

			GUILayout.BeginHorizontal(GUILayout.Width(180));

			if(GUILayout.Button("Auto UV", EditorStyles.miniButton))
			{
				AutoUV(true);
			}

			if(GUILayout.Button("Auto Fit", EditorStyles.miniButton))
			{
				AutoFit();
			}

			if(GUILayout.Button("Extrude Brush", EditorStyles.miniButton))
			{
				if(selectedSourcePolygons.Count > 0)
				{
					ExtrudeBrushesFromSelection();
				}
			}

			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal(GUILayout.Width(110));

			GUILayout.Label("Flip", SabreGUILayout.GetTitleStyle(), GUILayout.Width(25));
			if (GUILayout.Button("X", EditorStyles.miniButtonLeft))
			{
				TransformUVs(UVUtility.FlipUVX, new UVUtility.TransformData(Vector2.zero,0), true);
			}
			if (GUILayout.Button("Y", EditorStyles.miniButtonMid))
			{
				TransformUVs(UVUtility.FlipUVY, new UVUtility.TransformData(Vector2.zero,0), true);
			}
			if (GUILayout.Button("XY", EditorStyles.miniButtonRight))
			{
				TransformUVs(UVUtility.FlipUVXY, new UVUtility.TransformData(Vector2.zero,0), true);
			}
			GUILayout.EndHorizontal();						
				
			GUILayout.BeginHorizontal(GUILayout.Width(110));

			GUILayout.Label("Planar", SabreGUILayout.GetTitleStyle(), GUILayout.Width(39));

			if(GUILayout.Button("X", EditorStyles.miniButtonLeft))
			{
				PlanarMap(Vector3.right);
			}

			if(GUILayout.Button("Y", EditorStyles.miniButtonMid))
			{
				PlanarMap(Vector3.up);
			}

			if(GUILayout.Button("Z", EditorStyles.miniButtonRight))
			{
				PlanarMap(Vector3.forward);
			}

			GUILayout.EndHorizontal();						

			GUILayout.BeginHorizontal(GUILayout.Width(110));

			if(GUILayout.Button("Flatten", EditorStyles.miniButton))
			{
				List<Brush> brushesToNotify = new List<Brush>();
				for (int polygonIndex = 0; polygonIndex < selectedSourcePolygons.Count; polygonIndex++) 
				{
					Polygon polygon = selectedSourcePolygons[polygonIndex];
					Brush brush = matchedBrushes[polygon];
					SurfaceUtility.FacetPolygon(polygon);

					if(!brushesToNotify.Contains(brush))
					{
						brushesToNotify.Add(brush);
					}
				}

				for (int i = 0; i < brushesToNotify.Count; i++) 
				{
					brushesToNotify[i].Invalidate(true);
				}
			}
			if(GUILayout.Button("Smooth", EditorStyles.miniButton))
			{
				List<Brush> brushesToNotify = new List<Brush>();
				for (int polygonIndex = 0; polygonIndex < selectedSourcePolygons.Count; polygonIndex++) 
				{
					Polygon polygon = selectedSourcePolygons[polygonIndex];
					Brush brush = matchedBrushes[polygon];
					SurfaceUtility.SmoothPolygon(polygon, brush.GetPolygons());

					if(!brushesToNotify.Contains(brush))
					{
						brushesToNotify.Add(brush);
					}
				}

				for (int i = 0; i < brushesToNotify.Count; i++) 
				{
					brushesToNotify[i].Invalidate(true);
				}
			}
			GUILayout.EndHorizontal();
//			GUILayout.Space(8);

			GUILayout.BeginHorizontal(GUILayout.Width(180));

			GUI.SetNextControlName("faceRotateField");
			rotationAmount = EditorGUILayout.FloatField(rotationAmount, GUILayout.Width(40));

			bool keyboardEnter = Event.current.isKey 
				&& Event.current.keyCode == KeyCode.Return 
				&& Event.current.type == EventType.KeyUp 
				&& GUI.GetNameOfFocusedControl() == "faceRotateField";

			if(GUILayout.Button("Rotate", EditorStyles.miniButton) || keyboardEnter)
			{
				RotateAroundCenter(rotationAmount, true);
			}

			if (SabreGUILayout.Button("-90", EditorStyles.miniButtonLeft))
			{
				RotateAroundCenter(-90, true);
			}
			if (SabreGUILayout.Button("+90", EditorStyles.miniButtonRight))
			{
				RotateAroundCenter(90, true);
			}

			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal(GUILayout.Width(180));

			GUI.SetNextControlName("faceScaleField");
			scaleAmount = EditorGUILayout.TextField(scaleAmount, GUILayout.Width(50));

			keyboardEnter = Event.current.isKey 
				&& Event.current.keyCode == KeyCode.Return 
				&& Event.current.type == EventType.KeyUp 
				&& GUI.GetNameOfFocusedControl() == "faceScaleField";

			if(GUILayout.Button("Scale", EditorStyles.miniButton) || keyboardEnter)
			{
				// Try to parse a Vector3 scale from the input string
				Vector2 rescaleVector2;
				if(StringHelper.TryParseScale(scaleAmount, out rescaleVector2))
				{
					// None of the scale components can be zero
					if(rescaleVector2.x != 0 && rescaleVector2.y != 0)
					{
						TransformUVs(UVUtility.ScaleUV, new UVUtility.TransformData(rescaleVector2,0), true);
					}
				}
			}

			if (SabreGUILayout.Button("/ 2", EditorStyles.miniButtonLeft))
			{
				TransformUVs(UVUtility.ScaleUV, new UVUtility.TransformData(new Vector2(.5f,.5f),0), true);
			}
			if (SabreGUILayout.Button("x 2", EditorStyles.miniButtonRight))
			{
				TransformUVs(UVUtility.ScaleUV, new UVUtility.TransformData(new Vector2(2f,2f),0), true);
			}
			
			GUILayout.EndHorizontal();

			DrawManualTextBoxes();


			selectHelpersVisible = EditorGUILayout.Foldout(selectHelpersVisible, "Selection Helpers");

			if(selectHelpersVisible)
			{
				GUILayout.BeginHorizontal(GUILayout.Width(180));

				if(GUILayout.Button("All", EditorStyles.miniButton))
				{
					SelectAll();
				}

				if(GUILayout.Button("None", EditorStyles.miniButton))
				{
					ResetSelection();
				}

				if(GUILayout.Button("Invert", EditorStyles.miniButton))
				{
					InvertSelection();
				}

				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal(GUILayout.Width(180));

				if(GUILayout.Button("Excluded", EditorStyles.miniButton))
				{
					SelectExcluded();
				}

				if(GUILayout.Button("Same Material", EditorStyles.miniButton))
				{
					SelectSameMaterial();
				}

				GUILayout.EndHorizontal();

				GUILayout.Label("Adjacent", SabreGUILayout.GetTitleStyle());

				GUILayout.BeginHorizontal(GUILayout.Width(180));

				if(GUILayout.Button("Walls", EditorStyles.miniButtonLeft))
				{
					SelectAdjacentWalls();
				}

				if(GUILayout.Button("Floors", EditorStyles.miniButtonMid))
				{
					SelectAdjacentFloors();
				}

				if(GUILayout.Button("Ceilings", EditorStyles.miniButtonMid))
				{
					SelectAdjacentCeilings();
				}

                if (GUILayout.Button("Coplanar", EditorStyles.miniButtonRight))
                {
                    SelectAdjacentCoplanar();
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();

                if (GUILayout.Button("All", EditorStyles.miniButton))
				{
					SelectAdjacentAll();
				}

                GUIStyle style = EditorStyles.toggle;
                limitToSameMaterial = GUILayout.Toggle(limitToSameMaterial, "Limit to same material", style);

                GUILayout.EndHorizontal();

				

				string label = selectedSourcePolygons.Count.ToStringWithSuffix(" selected face", " selected faces");
				GUILayout.Label(label, SabreGUILayout.GetForeStyle());
			}
        }

		public override void ResetTool()
		{
			
		}

		public override void OnSelectionChanged ()
		{
			base.OnSelectionChanged ();

			ResetSelection();

			// Walk through all the selection brushes and select their faces
			for (int i = 0; i < Selection.gameObjects.Length; i++) 
			{
				Brush brush = Selection.gameObjects[i].GetComponent<Brush>();
				if(brush != null)
				{
					Polygon[] polygons = brush.GetPolygons();

					for (int j = 0; j < polygons.Length; j++) 
					{
						bool built = (csgModel.BuiltPolygonsByIndex(polygons[j].UniqueIndex).Length > 0);
						if(built)
						{
							selectedSourcePolygons.Add(polygons[j]);

							matchedBrushes.Add(polygons[j], brush);
						}
					}
				}
			}
		}

		public override void Deactivated ()
		{
		}

		public override bool BrushesHandleDrawing 
		{
			get 
			{
				return false;
			}
		}

		public override bool PreventBrushSelection 
		{
			get 
			{
				// Can't select brushes in the scene view in the Face tool
				return true;
			}
		}

		void SelectAll()
		{
			// Set the selection to all the possible selectable polygons
			selectedSourcePolygons = csgModel.GetAllSourcePolygons();

			// Recalculate the matched brushes
			matchedBrushes.Clear();

			for (int i = 0; i < selectedSourcePolygons.Count; i++) 
			{
				matchedBrushes.Add(selectedSourcePolygons[i], csgModel.FindBrushFromPolygon(selectedSourcePolygons[i]));
			}
		}

		void ResetSelection()
		{
			// Set the selection and matches brushes to empty
			selectedSourcePolygons.Clear();
			matchedBrushes.Clear();
		}

		void InvertSelection()
		{
			// Construct a list of all polygons that are possible to select
			List<Polygon> newList = csgModel.GetAllSourcePolygons();

			// Remove from that list polygons that are already selected
			for (int i = 0; i < selectedSourcePolygons.Count; i++) 
			{
				newList.Remove(selectedSourcePolygons[i]);
			}

			// Update the selected list with the new inverted selection
			selectedSourcePolygons = newList;

			// Recalculate the matched brushes
			matchedBrushes.Clear();

			for (int i = 0; i < selectedSourcePolygons.Count; i++) 
			{
				matchedBrushes.Add(selectedSourcePolygons[i], csgModel.FindBrushFromPolygon(selectedSourcePolygons[i]));
			}
		}

		void SelectExcluded()
		{
			// Set the selection to all the possible selectable polygons
			List<Polygon> allPolygons = csgModel.GetAllSourcePolygons();

			ResetSelection();

			for (int i = 0; i < allPolygons.Count; i++) 
			{
				if(allPolygons[i].UserExcludeFromFinal)
				{
					selectedSourcePolygons.Add(allPolygons[i]);
					matchedBrushes.Add(allPolygons[i], csgModel.FindBrushFromPolygon(allPolygons[i]));
				}
			}
		}

		void SelectSameMaterial()
		{
			List<Material> searchMaterials = selectedSourcePolygons.Select(polygon => polygon.Material).Distinct().ToList();

			// Set the selection to all the possible selectable polygons
			List<Polygon> allPolygons = csgModel.GetAllSourcePolygons();

			ResetSelection();

			for (int i = 0; i < allPolygons.Count; i++) 
			{
				if(searchMaterials.Contains(allPolygons[i].Material))
				{
					selectedSourcePolygons.Add(allPolygons[i]);
					matchedBrushes.Add(allPolygons[i], csgModel.FindBrushFromPolygon(allPolygons[i]));
				}
			}
		}

		void SelectAdjacentWalls()
		{
            AdjacencyFilters.MatchMaterial filter = null;
            if (limitToSameMaterial)
            {
                // Distinct set of materials used by selected polygons
                Material[] sourceMaterials = selectedSourcePolygons.Select(polygon => polygon.Material).Distinct().ToArray();
                filter = new AdjacencyFilters.MatchMaterial(sourceMaterials);
            }

            List<int> polygonIDs = AdjacencyHelper.FindAdjacentWalls(csgModel.VisualPolygons, selectedSourcePolygons, filter);

			SetSelectionFromPolygonIDs(polygonIDs);
		}

		void SelectAdjacentFloors()
		{
            AdjacencyFilters.MatchMaterial filter = null;
            if (limitToSameMaterial)
            {
                // Distinct set of materials used by selected polygons
                Material[] sourceMaterials = selectedSourcePolygons.Select(polygon => polygon.Material).Distinct().ToArray();
                filter = new AdjacencyFilters.MatchMaterial(sourceMaterials);
            }

            List<int> polygonIDs = AdjacencyHelper.FindAdjacentFloors(csgModel.VisualPolygons, selectedSourcePolygons, filter);

			SetSelectionFromPolygonIDs(polygonIDs);
		}

		void SelectAdjacentCeilings()
		{
            AdjacencyFilters.MatchMaterial filter = null;
            if (limitToSameMaterial)
            {
                // Distinct set of materials used by selected polygons
                Material[] sourceMaterials = selectedSourcePolygons.Select(polygon => polygon.Material).Distinct().ToArray();
                filter = new AdjacencyFilters.MatchMaterial(sourceMaterials);
            }

            List<int> polygonIDs = AdjacencyHelper.FindAdjacentCeilings(csgModel.VisualPolygons, selectedSourcePolygons, filter);

			SetSelectionFromPolygonIDs(polygonIDs);
		}

        void SelectAdjacentCoplanar()
        {
            AdjacencyFilters.MatchMaterial filter = null;
            if (limitToSameMaterial)
            {
                // Distinct set of materials used by selected polygons
                Material[] sourceMaterials = selectedSourcePolygons.Select(polygon => polygon.Material).Distinct().ToArray();
                filter = new AdjacencyFilters.MatchMaterial(sourceMaterials);
            }

            List<int> polygonIDs = AdjacencyHelper.FindAdjacentCoplanar(csgModel.VisualPolygons, selectedSourcePolygons, filter);

            SetSelectionFromPolygonIDs(polygonIDs);
        }

        void SelectAdjacentAll()
		{
            AdjacencyFilters.MatchMaterial filter = null;
            if (limitToSameMaterial)
            {
                // Distinct set of materials used by selected polygons
                Material[] sourceMaterials = selectedSourcePolygons.Select(polygon => polygon.Material).Distinct().ToArray();
                filter = new AdjacencyFilters.MatchMaterial(sourceMaterials);
            }

            List<int> polygonIDs = AdjacencyHelper.FindAdjacentAll(csgModel.VisualPolygons, selectedSourcePolygons, filter);

			SetSelectionFromPolygonIDs(polygonIDs);
		}

		void SetSelectionFromPolygonIDs(List<int> polygonIDs)
		{
			ResetSelection();

			for (int i = 0; i < polygonIDs.Count; i++) 
			{
				Polygon sourcePolygon = csgModel.GetSourcePolygon(polygonIDs[i]);
				selectedSourcePolygons.Add(sourcePolygon);
				matchedBrushes.Add(sourcePolygon, csgModel.FindBrushFromPolygon(sourcePolygon));
			}
		}

		void ExtrudeBrushesFromSelection()
		{
			GameObject[] newObjects = new GameObject[selectedSourcePolygons.Count];
			for (int i = 0; i < selectedSourcePolygons.Count; i++) 
			{
				Quaternion rotation;
				Polygon[] polygons;
				SurfaceUtility.ExtrudePolygon(selectedSourcePolygons[i], 1, out polygons, out rotation);

				Brush sourceBrush = matchedBrushes[selectedSourcePolygons[i]];
				GameObject newObject = ((PrimitiveBrush)sourceBrush).Duplicate();

				newObject.transform.rotation = sourceBrush.transform.rotation * rotation;
				// Finally give the new brush the other set of polygons
				newObject.GetComponent<PrimitiveBrush>().SetPolygons(polygons, true);

				Undo.RegisterCreatedObjectUndo(newObject, "Extrude Brush");

				newObjects[i] = newObject;
			}

			csgModel.SetCurrentMode(MainMode.Resize);
			Selection.objects = newObjects;
		}

		void AutoFit()
		{
			for (int polygonIndex = 0; polygonIndex < selectedSourcePolygons.Count; polygonIndex++) 
			{
				Polygon polygon = selectedSourcePolygons[polygonIndex];


				PolygonEntry entry = csgModel.GetVisualPolygonEntry(polygon.UniqueIndex);
				if(PolygonEntry.IsValidAndBuilt(entry))
				{
					Brush brush = matchedBrushes[polygon];
					Undo.RecordObject(brush, "Auto Fit");
					csgModel.UndoRecordContext("Auto Fit");
					Undo.RecordObject(entry.BuiltMesh, "Auto Fit");
					Transform brushTransform = brush.transform;

					UVOrientation worldOrientation = SurfaceUtility.GetNorthEastVectors(polygon, brushTransform);

					Vector3 worldVectorNorth = worldOrientation.NorthVector;
					Vector3 worldVectorEast = worldOrientation.EastVector;

					Vector3 polygonCenterLocal = polygon.GetCenterPoint();
					Vector3 polygonCenterWorld = brushTransform.TransformPoint(polygonCenterLocal);

					// World vertices
					Polygon[] builtPolygons = csgModel.BuiltPolygonsByIndex(polygon.UniqueIndex);
					Vertex northernVertex = builtPolygons[0].Vertices[0];
					Vertex southernVertex = builtPolygons[0].Vertices[0];
					Vertex easternVertex = builtPolygons[0].Vertices[0];
					Vertex westernVertex = builtPolygons[0].Vertices[0];


					for (int builtIndex = 0; builtIndex < builtPolygons.Length; builtIndex++) 
					{
						for (int i = 0; i < builtPolygons[builtIndex].Vertices.Length; i++) 
						{
							Vertex testVertex = builtPolygons[builtIndex].Vertices[i];

							float dotCurrent = Vector3.Dot(northernVertex.Position-polygonCenterWorld, worldVectorNorth);
							float dotTest = Vector3.Dot(testVertex.Position-polygonCenterWorld, worldVectorNorth);
							if(dotTest > dotCurrent)
							{
								northernVertex = testVertex;
							}

							dotCurrent = Vector3.Dot(southernVertex.Position-polygonCenterWorld, -worldVectorNorth);
							dotTest = Vector3.Dot(testVertex.Position-polygonCenterWorld, -worldVectorNorth);
							if(dotTest > dotCurrent)
							{
								southernVertex = testVertex;
							}

							dotCurrent = Vector3.Dot(easternVertex.Position-polygonCenterWorld, worldVectorEast);
							dotTest = Vector3.Dot(testVertex.Position-polygonCenterWorld, worldVectorEast);
							if(dotTest > dotCurrent)
							{
								easternVertex = testVertex;
							}

							dotCurrent = Vector3.Dot(westernVertex.Position-polygonCenterWorld, -worldVectorEast);
							dotTest = Vector3.Dot(testVertex.Position-polygonCenterWorld, -worldVectorEast);
							if(dotTest > dotCurrent)
							{
								westernVertex = testVertex;
							}
						}
					}

					float northernDistance = Vector3.Dot(northernVertex.Position - polygonCenterWorld, worldVectorNorth);
					float southernDistance = Vector3.Dot(southernVertex.Position - polygonCenterWorld, worldVectorNorth);

					float easternDistance = Vector3.Dot(easternVertex.Position - polygonCenterWorld, worldVectorEast);
					float westernDistance = Vector3.Dot(westernVertex.Position - polygonCenterWorld, worldVectorEast);

					// Update the source polygons
					for (int i = 0; i < polygon.Vertices.Length; i++) 
					{
						Vector3 localPosition = polygon.Vertices[i].Position;
						Vector3 worldPosition = brushTransform.TransformPoint(localPosition);

						float thisNorthDistance = Vector3.Dot(worldPosition - polygonCenterWorld, worldVectorNorth);
						float thisEastDistance = Vector3.Dot(worldPosition - polygonCenterWorld, worldVectorEast);

						Vector2 uv = new Vector2(MathHelper.InverseLerpNoClamp(westernDistance, easternDistance, thisEastDistance),
							MathHelper.InverseLerpNoClamp(southernDistance, northernDistance, thisNorthDistance));
						
						polygon.Vertices[i].UV = uv;
					}

					// Update the built polygons in case we need to use them for something else
					for (int builtPolygonIndex = 0; builtPolygonIndex < builtPolygons.Length; builtPolygonIndex++) 
					{
						Polygon builtPolygon = builtPolygons[builtPolygonIndex];
						for (int vertexIndex = 0; vertexIndex < builtPolygon.Vertices.Length; vertexIndex++) 
						{
							Vector3 worldPosition = builtPolygon.Vertices[vertexIndex].Position;

							float thisNorthDistance = Vector3.Dot(worldPosition - polygonCenterWorld, worldVectorNorth);
							float thisEastDistance = Vector3.Dot(worldPosition - polygonCenterWorld, worldVectorEast);

							Vector2 uv = new Vector2(MathHelper.InverseLerpNoClamp(westernDistance, easternDistance, thisEastDistance),
								MathHelper.InverseLerpNoClamp(southernDistance, northernDistance, thisNorthDistance));

							builtPolygon.Vertices[vertexIndex].UV = uv;
						}
					}


					Vector3[] vertices = entry.BuiltMesh.vertices;

					Vector2[] meshUVs = entry.BuiltMesh.uv;
					Vector2[] uvs = entry.UV;

					for (int vertexIndex = 0; vertexIndex < entry.Positions.Length; vertexIndex++) 
					{
						Vector3 worldPosition = vertices[entry.BuiltVertexOffset + vertexIndex];

						float thisNorthDistance = Vector3.Dot(worldPosition - polygonCenterWorld, worldVectorNorth);
						float thisEastDistance = Vector3.Dot(worldPosition - polygonCenterWorld, worldVectorEast);

						Vector2 uv = new Vector2(MathHelper.InverseLerpNoClamp(westernDistance, easternDistance, thisEastDistance),
							MathHelper.InverseLerpNoClamp(southernDistance, northernDistance, thisNorthDistance));

						uvs[vertexIndex] = uv;
						meshUVs[entry.BuiltVertexOffset + vertexIndex] = uv;
					}
					entry.UV = uvs;
					entry.BuiltMesh.uv = meshUVs;

					EditorHelper.SetDirty(entry.BuiltMesh);


					// Inform the brush cache that the polygons have changed, but that a rebuild isn't necessary
					brush.RecachePolygons(false);
				}
			}
		}

		void AutoUV(bool useWorldSpace)
		{
			for (int polygonIndex = 0; polygonIndex < selectedSourcePolygons.Count; polygonIndex++) 
			{
				Polygon polygon = selectedSourcePolygons[polygonIndex];

				Brush brush = matchedBrushes[polygon];
				Undo.RecordObject(brush, "Auto UV");
				csgModel.UndoRecordContext("Auto UV");
				Transform brushTransform = brush.transform;

				Vector3 planeNormal = polygon.Plane.normal;
				if(useWorldSpace)
				{
					planeNormal = brushTransform.TransformDirection(planeNormal);
				}

				Quaternion cancellingRotation = Quaternion.Inverse(Quaternion.LookRotation(-planeNormal));

				// Sets the UV at each point to the position on the plane
				for (int i = 0; i < polygon.Vertices.Length; i++) 
				{
					Vector3 position = polygon.Vertices[i].Position;
					if(useWorldSpace)
					{
						position = brushTransform.TransformPoint(position);
					}

					Vector2 uv = (cancellingRotation * position) * 0.5f;
					polygon.Vertices[i].UV = uv;
				}


				Polygon[] builtPolygons = csgModel.BuiltPolygonsByIndex(polygon.UniqueIndex);

				for (int builtPolygonIndex = 0; builtPolygonIndex < builtPolygons.Length; builtPolygonIndex++) 
				{
					Polygon builtPolygon = builtPolygons[builtPolygonIndex];
					for (int vertexIndex = 0; vertexIndex < builtPolygon.Vertices.Length; vertexIndex++) 
					{
						Vector3 position = builtPolygon.Vertices[vertexIndex].Position;

						if(!useWorldSpace)
						{
							position = brushTransform.InverseTransformPoint(position);
						}

						Vector2 uv = (cancellingRotation * position) * 0.5f;
						builtPolygon.Vertices[vertexIndex].UV = uv;
					}
				}

				// Update the actual built mesh 

				PolygonEntry entry = csgModel.GetVisualPolygonEntry(polygon.UniqueIndex);
				if(PolygonEntry.IsValidAndBuilt(entry))
				{
					Undo.RecordObject(entry.BuiltMesh, "Auto UV");
					Vector3[] vertices = entry.BuiltMesh.vertices;

					Vector2[] meshUVs = entry.BuiltMesh.uv;
					Vector2[] uvs = entry.UV;

					for (int vertexIndex = 0; vertexIndex < entry.Positions.Length; vertexIndex++) 
					{
						Vector3 position = vertices[entry.BuiltVertexOffset + vertexIndex];
						if(!useWorldSpace)
						{
							position = brushTransform.InverseTransformPoint(position);
						}

						Vector2 uv = (cancellingRotation * position) * 0.5f;

						uvs[vertexIndex] = uv;
						meshUVs[entry.BuiltVertexOffset + vertexIndex] = uv;
					}
					entry.UV = uvs;
					entry.BuiltMesh.uv = meshUVs;

					EditorHelper.SetDirty(entry.BuiltMesh);
				}

				// Inform the brush cache that the polygons have changed, but that a rebuild isn't necessary
				brush.RecachePolygons(false);
			}
		}

		void PlanarMap(Vector3 planarDirection)
		{
			for (int polygonIndex = 0; polygonIndex < selectedSourcePolygons.Count; polygonIndex++) 
			{
				Polygon polygon = selectedSourcePolygons[polygonIndex];

				Brush brush = matchedBrushes[polygon];
				Undo.RecordObject(brush, "Planar Map");
				csgModel.UndoRecordContext("Planar Map");
				Transform brushTransform = brush.transform;

				Quaternion cancellingRotation;
				if(planarDirection == Vector3.up)
				{
					cancellingRotation = Quaternion.Inverse(Quaternion.LookRotation(-planarDirection, Vector3.right));
				}
				else
				{
					cancellingRotation = Quaternion.Inverse(Quaternion.LookRotation(planarDirection));
				}

				Vector3 planeNormal = polygon.Plane.normal;
				planeNormal = brushTransform.TransformDirection(planeNormal);

				// Skip if the polygon and planar directorions are perpendicular
				if(Mathf.Abs(Vector3.Dot(planeNormal, planarDirection)) <= 0.01f)
				{
					return;
				}

				// Sets the UV at each point to the position on the plane
				for (int i = 0; i < polygon.Vertices.Length; i++) 
				{
					Vector3 position = polygon.Vertices[i].Position;
					position = brushTransform.TransformPoint(position);

					Vector2 uv = (cancellingRotation * position) * 0.5f;
					polygon.Vertices[i].UV = uv;
				}


				Polygon[] builtPolygons = csgModel.BuiltPolygonsByIndex(polygon.UniqueIndex);

				for (int builtPolygonIndex = 0; builtPolygonIndex < builtPolygons.Length; builtPolygonIndex++) 
				{
					Polygon builtPolygon = builtPolygons[builtPolygonIndex];
					for (int vertexIndex = 0; vertexIndex < builtPolygon.Vertices.Length; vertexIndex++) 
					{
						Vector3 position = builtPolygon.Vertices[vertexIndex].Position;

						Vector2 uv = (cancellingRotation * position) * 0.5f;
						builtPolygon.Vertices[vertexIndex].UV = uv;
					}
				}

				// Update the actual built mesh 

				PolygonEntry entry = csgModel.GetVisualPolygonEntry(polygon.UniqueIndex);
				if(PolygonEntry.IsValidAndBuilt(entry))
				{
					Undo.RecordObject(entry.BuiltMesh, "Planar Map");
					Vector3[] vertices = entry.BuiltMesh.vertices;

					Vector2[] meshUVs = entry.BuiltMesh.uv;
					Vector2[] uvs = entry.UV;

					for (int vertexIndex = 0; vertexIndex < entry.Positions.Length; vertexIndex++) 
					{
						Vector3 position = vertices[entry.BuiltVertexOffset + vertexIndex];

						Vector2 uv = (cancellingRotation * position) * 0.5f;

						uvs[vertexIndex] = uv;
						meshUVs[entry.BuiltVertexOffset + vertexIndex] = uv;
					}
					entry.UV = uvs;
					entry.BuiltMesh.uv = meshUVs;

					EditorHelper.SetDirty(entry.BuiltMesh);
				}

				// Inform the brush cache that the polygons have changed, but that a rebuild isn't necessary
				brush.RecachePolygons(false);
			}
		}


		void Align(AlignDirection direction)
		{
			for (int polygonIndex = 0; polygonIndex < selectedSourcePolygons.Count; polygonIndex++) 
			{
				Polygon polygon = selectedSourcePolygons[polygonIndex];

				Transform brushTransform = matchedBrushes[polygon].transform;

				UVOrientation worldOrientation = SurfaceUtility.GetNorthEastVectors(polygon, brushTransform);

				Vector3 worldVectorNorth = worldOrientation.NorthVector;
				Vector3 worldVectorEast = worldOrientation.EastVector;

				Vector3 polygonCenterLocal = polygon.GetCenterPoint();
				Vector3 polygonCenterWorld = brushTransform.TransformPoint(polygonCenterLocal);

				// World vertices
				Polygon[] builtPolygons = csgModel.BuiltPolygonsByIndex(polygon.UniqueIndex);
				Vertex northernVertex = builtPolygons[0].Vertices[0];
				Vertex southernVertex = builtPolygons[0].Vertices[0];
				Vertex easternVertex = builtPolygons[0].Vertices[0];
				Vertex westernVertex = builtPolygons[0].Vertices[0];


				for (int builtIndex = 0; builtIndex < builtPolygons.Length; builtIndex++) 
				{
					for (int i = 0; i < builtPolygons[builtIndex].Vertices.Length; i++) 
					{
						Vertex testVertex = builtPolygons[builtIndex].Vertices[i];

						float dotCurrent = Vector3.Dot(northernVertex.Position-polygonCenterWorld, worldVectorNorth);
						float dotTest = Vector3.Dot(testVertex.Position-polygonCenterWorld, worldVectorNorth);
						if(dotTest > dotCurrent)
						{
							northernVertex = testVertex;
						}

						dotCurrent = Vector3.Dot(southernVertex.Position-polygonCenterWorld, -worldVectorNorth);
						dotTest = Vector3.Dot(testVertex.Position-polygonCenterWorld, -worldVectorNorth);
						if(dotTest > dotCurrent)
						{
							southernVertex = testVertex;
						}

						dotCurrent = Vector3.Dot(easternVertex.Position-polygonCenterWorld, worldVectorEast);
						dotTest = Vector3.Dot(testVertex.Position-polygonCenterWorld, worldVectorEast);
						if(dotTest > dotCurrent)
						{
							easternVertex = testVertex;
						}

						dotCurrent = Vector3.Dot(westernVertex.Position-polygonCenterWorld, -worldVectorEast);
						dotTest = Vector3.Dot(testVertex.Position-polygonCenterWorld, -worldVectorEast);
						if(dotTest > dotCurrent)
						{
							westernVertex = testVertex;
						}
					}
				}

				Vector2 offset = new Vector2(0,0);

				if(direction == AlignDirection.Top)
				{
					offset.y = 1-northernVertex.UV.y;
				}
				else if(direction == AlignDirection.Bottom)
				{
					offset.y = 0-southernVertex.UV.y;
				}
				else if(direction == AlignDirection.Left)
				{
					offset.x = 0-westernVertex.UV.x;
				}
				else if(direction == AlignDirection.Right)
				{
					offset.x = 1-easternVertex.UV.x;
				}
				else if(direction == AlignDirection.Center)
				{
					offset.x = (0-westernVertex.UV.x + 1-easternVertex.UV.x) * .5f;
					offset.y = (1-northernVertex.UV.y + 0-southernVertex.UV.y) * .5f;
				}

				TransformUVs(polygon, UVUtility.TranslateUV, new UVUtility.TransformData(offset,0), true);
			}
		}

		void UserExcludePolygon(Polygon sourcePolygon)
		{
			Polygon[] builtRenderPolygons = csgModel.BuiltPolygonsByIndex(sourcePolygon.UniqueIndex);

			foreach (Polygon polygon in builtRenderPolygons) 
			{
				polygon.UserExcludeFromFinal = true;
			}

//			Polygon[] builtCollisionPolygons = csgModel.BuiltCollisionPolygonsByIndex(sourcePolygon.UniqueIndex);
//
//			foreach (Polygon polygon in builtCollisionPolygons) 
//			{
//				polygon.UserExcludeFromFinal = true;
//			}

			RemoveAndUpdateMesh(csgModel.GetVisualPolygonEntry(sourcePolygon.UniqueIndex), sourcePolygon);
			RemoveAndUpdateMesh(csgModel.GetCollisionPolygonEntry(sourcePolygon.UniqueIndex), sourcePolygon);

			// Mesh colliders need to be refreshed now that their collision meshes have changed
			csgModel.RefreshMeshGroup();

			csgModel.SetContextDirty();
		}


		void UserIncludePolygon(Polygon sourcePolygon)
		{
			Polygon[] builtRenderPolygons = csgModel.BuiltPolygonsByIndex(sourcePolygon.UniqueIndex);

			foreach (Polygon polygon in builtRenderPolygons) 
			{
				polygon.UserExcludeFromFinal = false;
			}

			AddAndUpdateMesh(csgModel.GetVisualPolygonEntry(sourcePolygon.UniqueIndex), sourcePolygon, true);
			AddAndUpdateMesh(csgModel.GetCollisionPolygonEntry(sourcePolygon.UniqueIndex), sourcePolygon, false);

			// Mesh colliders need to be refreshed now that their collision meshes have changed
			csgModel.RefreshMeshGroup();

			csgModel.SetContextDirty();
		}

		public void AddAndUpdateMesh(PolygonEntry entry, Polygon sourcePolygon, bool isVisual)
		{
			if(PolygonEntry.IsValid(entry))
			{
				int verticesToAdd = entry.Positions.Length;
				int trianglesToAdd = entry.Triangles.Length;
				Mesh newMesh;

				if(isVisual)
				{
					Material material = entry.Material;
					if(material == null)
					{
						material = csgModel.GetDefaultMaterial();
					}
					newMesh = csgModel.GetMeshForMaterial(material, verticesToAdd);
				}
				else
				{
					newMesh = csgModel.GetMeshForCollision(verticesToAdd);
				}

				// Unfortunately in Unity 5.1 accessing .triangles on an empty mesh throws an error
				int[] destTriangles = newMesh.GetTrianglesSafe();

				Vector3[] destVertices = newMesh.vertices;
				Vector2[] destUV = newMesh.uv;
				Vector3[] destNormals = newMesh.normals;
				Color[] destColors = newMesh.colors;

				int destVertexCount = destVertices.Length;
				int destTriangleCount = destTriangles.Length;

				Array.Resize(ref destVertices, destVertexCount + verticesToAdd);
				Array.Resize(ref destUV, destVertexCount + verticesToAdd);
				Array.Resize(ref destNormals, destVertexCount + verticesToAdd);
				Array.Resize(ref destColors, destVertexCount + verticesToAdd);

				for (int i = 0; i < entry.Positions.Length; i++) 
				{
					destVertices[destVertexCount + i] = entry.Positions[i];
					destUV[destVertexCount + i] = entry.UV[i];
					destNormals[destVertexCount + i] = entry.Normals[i];
					destColors[destVertexCount + i] = entry.Colors[i];
				}

				Array.Resize(ref destTriangles, destTriangleCount + trianglesToAdd);

				for (int i = 0; i < entry.Triangles.Length; i++) 
				{
					destTriangles[destTriangleCount + i] = entry.Triangles[i] + destVertexCount;
				}

				newMesh.vertices = destVertices;
				newMesh.uv = destUV;
				newMesh.normals = destNormals;
				newMesh.triangles = destTriangles;
				newMesh.colors = destColors;

				// If the mesh already has tangents
				if(csgModel.LastBuildHadTangents)
				{
					newMesh.GenerateTangents();
				}

				entry.BuiltMesh = newMesh;
				entry.BuiltVertexOffset = destVertexCount;
				entry.BuiltTriangleOffset = destTriangleCount;
			}
		}

		public void RemoveAndUpdateMesh(PolygonEntry entry, Polygon sourcePolygon)
		{
			if(!PolygonEntry.IsValidAndBuilt(entry))
			{
				// This polygon hasn't actually been built
				return;
			}

			int[] triangles  = entry.BuiltMesh.triangles;

            // Turn the triangles into degenerates
			for (int i = 0; i < entry.Triangles.Length; i++) 
			{
				triangles[entry.BuiltTriangleOffset + i] = 0;
			}

            bool areAllDegenerate = true;
            for (int i = 0; i < triangles.Length; i++)
            {
                if(triangles[i] != 0)
                {
                    areAllDegenerate = false;
                }
            }

            // PhysX will throw an error if we make all the polygons degenerate, so in that case simply change the triangles array to empty
            if(areAllDegenerate)
            {
                triangles = new int[0];
            }

			entry.BuiltMesh.triangles = triangles;
		}

		private void ChangePolygonMaterial(Polygon polygon, Material destinationMaterial)
		{
			Material defaultMaterial = CSGModel.GetDefaultMaterial();

			// Only attempt to transfer the polygon if it's to a different material!
			if(polygon.Material == destinationMaterial
				|| (polygon.Material == null && destinationMaterial == defaultMaterial)
				|| (polygon.Material == defaultMaterial && destinationMaterial == null))
			{
				return;
			}
			PolygonEntry entry = csgModel.GetVisualPolygonEntry(polygon.UniqueIndex);

			// Repoint the polygon's material, so it will change with rebuilds
			polygon.Material = destinationMaterial;
			
			if(!PolygonEntry.IsValidAndBuilt(entry))
			{
				// This polygon hasn't actually been built
				return;
			}

			Mesh originalMesh = entry.BuiltMesh;

			int verticesToAdd = entry.Positions.Length;
			int trianglesToAdd = entry.Triangles.Length;
			
			int[] sourceTriangles = originalMesh.triangles;
			Vector3[] sourceVertices = originalMesh.vertices;
			Vector2[] sourceUV = originalMesh.uv;
			Vector2[] sourceUV2 = originalMesh.uv2;
			Color[] sourceColors = originalMesh.colors;
			Vector3[] sourceNormals = originalMesh.normals;
			Vector4[] sourceTangents = originalMesh.tangents;
			
			Mesh newMesh = csgModel.GetMeshForMaterial(destinationMaterial, verticesToAdd);

			// Unfortunately in Unity 5.1 accessing .triangles on an empty mesh throws an error
			int[] destTriangles = newMesh.GetTrianglesSafe();

			Vector3[] destVertices = newMesh.vertices;
			Vector2[] destUV = newMesh.uv;
			Vector2[] destUV2 = newMesh.uv2;
			Color[] destColors = newMesh.colors;
			Vector3[] destNormals = newMesh.normals;
			Vector4[] destTangents = newMesh.tangents;
			
			int destVertexCount = destVertices.Length;
			int destTriangleCount = destTriangles.Length;
			
			
			Array.Resize(ref destVertices, destVertexCount + verticesToAdd);
			Array.Resize(ref destUV, destVertexCount + verticesToAdd);
			Array.Resize(ref destUV2, destVertexCount + verticesToAdd);
			Array.Resize(ref destColors, destVertexCount + verticesToAdd);
			Array.Resize(ref destNormals, destVertexCount + verticesToAdd);
			Array.Resize(ref destTangents, destVertexCount + verticesToAdd);

			for (int i = 0; i < entry.Positions.Length; i++) 
			{
				destVertices[destVertexCount + i] = sourceVertices[entry.BuiltVertexOffset + i];
				destUV[destVertexCount + i] = sourceUV[entry.BuiltVertexOffset + i];
				destNormals[destVertexCount + i] = sourceNormals[entry.BuiltVertexOffset + i];
			}

			// If the source mesh had lightmap UVs to copy
			if(sourceUV2.Length > 0)
			{
				for (int i = 0; i < entry.Positions.Length; i++) 
				{
					destUV2[destVertexCount + i] = sourceUV2[entry.BuiltVertexOffset + i];
				}
			}

			// If the source mesh had tangents to copy
			if(sourceTangents.Length > 0)
			{
				for (int i = 0; i < entry.Positions.Length; i++) 
				{
					destTangents[destVertexCount + i] = sourceTangents[entry.BuiltVertexOffset + i];
				}
			}

			// If the source mesh had colors to copy
			if(sourceColors.Length > 0)
			{
				for (int i = 0; i < entry.Positions.Length; i++) 
				{
					destColors[destVertexCount + i] = sourceColors[entry.BuiltVertexOffset + i];
				}
			}

			Array.Resize(ref destTriangles, destTriangleCount + trianglesToAdd);

			for (int i = 0; i < entry.Triangles.Length; i++) 
			{
				destTriangles[destTriangleCount + i] = sourceTriangles[entry.BuiltTriangleOffset + i] - entry.BuiltVertexOffset + destVertexCount;
			}

			for (int i = 0; i < entry.Triangles.Length; i++) 
			{
				sourceTriangles[entry.BuiltTriangleOffset + i] = 0;
			}

			newMesh.vertices = destVertices;
			newMesh.uv = destUV;
			newMesh.uv2 = destUV2;
			newMesh.colors = destColors;
			newMesh.normals = destNormals;
			newMesh.tangents = destTangents;
			newMesh.triangles = destTriangles;

			originalMesh.triangles = sourceTriangles;

			entry.Material = destinationMaterial;
			entry.BuiltMesh = newMesh;
			entry.BuiltTriangleOffset = destTriangleCount;
			entry.BuiltVertexOffset = destVertexCount;

			matchedBrushes[polygon].RecachePolygons(false);

			for (int i = 0; i < matchedBrushes[polygon].BrushCache.BuiltVisualPolygons.Count; i++) 
			{
				if(matchedBrushes[polygon].BrushCache.BuiltVisualPolygons[i].UniqueIndex == polygon.UniqueIndex)
				{
					matchedBrushes[polygon].BrushCache.BuiltVisualPolygons[i].Material = destinationMaterial;
				}
			}

			Polygon[] builtPolygons = csgModel.BuiltPolygonsByIndex(polygon.UniqueIndex);
			for (int i = 0; i < builtPolygons.Length; i++) 
			{
				builtPolygons[i].Material = destinationMaterial;
			}
			csgModel.SetContextDirty();
		}

		private void ChangePolygonColor(Polygon polygon, Color color)
		{
			for (int j = 0; j < polygon.Vertices.Length; j++) 
			{
				polygon.Vertices[j].Color = color;

				PolygonEntry entry = csgModel.GetVisualPolygonEntry(polygon.UniqueIndex);
				if(entry != null)
				{
					if(entry.BuiltMesh != null)
					{
						Undo.RecordObject(entry.BuiltMesh, "Change Vertex Color");
					
						Color[] meshColors = entry.BuiltMesh.colors;
						Color[] colors = entry.Colors;

						for (int vertexIndex = 0; vertexIndex < entry.Positions.Length; vertexIndex++) 
						{
							colors[vertexIndex] = color;
							meshColors[entry.BuiltVertexOffset + vertexIndex] = color;
						}
						entry.Colors = colors;
						entry.BuiltMesh.colors = meshColors;

						EditorHelper.SetDirty(entry.BuiltMesh);
					}
				}

				// Inform the brush cache that the polygons have changed, but that a rebuild isn't necessary
				matchedBrushes[polygon].RecachePolygons(false);
			}
		}

		public void SetSelectionColor(Color color)
		{
			for (int i = 0; i < selectedSourcePolygons.Count; i++) 
			{
				Polygon polygon = selectedSourcePolygons[i];
				ChangePolygonColor(polygon, color);
			}
		}

		public void SetSelectionMaterial(Material material)
		{
			for (int i = 0; i < selectedSourcePolygons.Count; i++) 
			{
				Polygon polygon = selectedSourcePolygons[i];
				ChangePolygonMaterial(polygon, material);
			}
		}
    }
}
#endif