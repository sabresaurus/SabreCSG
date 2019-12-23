#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace Sabresaurus.SabreCSG
{
	public class VertexEditor : Tool
	{
		const float EDGE_SCREEN_TOLERANCE = 15f;

		List<Edge> selectedEdges = new List<Edge>();
		Dictionary<Vertex, Brush> selectedVertices = new Dictionary<Vertex, Brush>();

		bool moveInProgress = false; 

		bool isMarqueeSelection = false; // Whether the user is (or could be) dragging a marquee box
        bool marqueeCancelled = false;

        Vector2 marqueeStart;
		Vector2 marqueeEnd;

		bool pivotNeedsReset = false;

		Dictionary<Vertex, Vector3> startPositions = new Dictionary<Vertex, Vector3>();

		// Configured by the user
		float weldTolerance = 0.1f;
		float scale = 1f;

        float chamferDistance = 0.1f;
        int chamferIterations = 3;

        Vertex movingVertex;

        private bool inverseSnapSelectionToCurrentGridLogic = false;

        void ClearSelection()
		{
			selectedEdges.Clear();
			selectedVertices.Clear();
		}

		void RemoveDisjointedVertices()
		{
			List<Vertex> verticesToRemove = new List<Vertex>();

			// Calculate what selected vertices no longer exist in their brush
			foreach (KeyValuePair<Vertex, Brush> selectedVertex in selectedVertices) 
			{
				Polygon[] polygons = selectedVertex.Value.GetPolygons();

				bool vertexPresent = false;
				// Check if the vertex is actually in the brush
				for (int i = 0; i < polygons.Length; i++) 
				{
					if(System.Array.IndexOf(polygons[i].Vertices, selectedVertex.Key) != -1)
					{
						// Found the vertex, break out the loop
						vertexPresent = true;
						break;
					}
				}

				if(!vertexPresent)
				{
					// Vertex wasn't there, so let's remove from selection
					verticesToRemove.Add(selectedVertex.Key);
				}
			}

			// Now actually remove the vertices in a separate loop (can't do this while iterating over the dictionary)
			for (int i = 0; i < verticesToRemove.Count; i++) 
			{
				selectedVertices.Remove(verticesToRemove[i]);
			}
		}

		public override void OnUndoRedoPerformed ()
		{
			base.OnUndoRedoPerformed ();

			// Undo/redo may mean that some selected vertices no longer exist in brushes, so strip them out to stop errors
			RemoveDisjointedVertices();
		}

		List<PrimitiveBrush> AutoWeld()
		{
            // Track the brushes that welding has changed
            List<PrimitiveBrush> changedBrushes = new List<PrimitiveBrush>();

			// Automatically weld any vertices that have been brought too close together
			if(primaryTargetBrush != null && selectedVertices.Count > 0)
			{
				float autoWeldTolerance = 0.001f;

				Dictionary<Brush, List<Vertex>> refinedSelections = new Dictionary<Brush, List<Vertex>>();

				foreach (PrimitiveBrush brush in targetBrushes) 
				{
					refinedSelections.Add(brush, SelectedVerticesOfBrush(brush));
				}

				bool selectionCleared = false;

				foreach (PrimitiveBrush brush in targetBrushes) 
				{
                    Polygon[] sourcePolygons = brush.GetPolygons();
                    // Make a copy so that we can differentiate newPolygons from the original, since welding updates affected polygons in place
                    Polygon[] sourcePolygonsCopy = sourcePolygons.DeepCopy();

					List<Vertex> allVertices = new List<Vertex>();
					for (int i = 0; i < sourcePolygonsCopy.Length; i++) 
					{
						allVertices.AddRange(sourcePolygonsCopy[i].Vertices);
					}

					Polygon[] newPolygons = VertexUtility.WeldNearbyVertices(autoWeldTolerance, sourcePolygonsCopy, allVertices);

                    bool hasChanged = false;

                    if(newPolygons.Length != sourcePolygons.Length)
                    {
                        hasChanged = true;
                    }

                    if(!hasChanged)
                    {
                        for (int i = 0; i < sourcePolygons.Length; i++)
                        {
                            if(sourcePolygons[i].Vertices.Length != newPolygons[i].Vertices.Length)
                            {
                                hasChanged = true;
                                break;
                            }
                        }
                    }

					if(hasChanged)
					{
						Undo.RecordObject(brush.transform, "Auto Weld Vertices");
						Undo.RecordObject(brush, "Auto Weld Vertices");

						if(!selectionCleared)
						{
							ClearSelection();
							selectionCleared = true;
						}
						brush.SetPolygons(newPolygons);

						SelectVertices(brush, newPolygons, refinedSelections[brush]);

                        // Brush has changed so mark it to be returned
                        changedBrushes.Add(brush);
                    }
				}
			}
            // Return the brushes that welding has changed
            return changedBrushes;
        }

		public void ScaleSelectedVertices(float scalar)
		{	
			Vector3 scalarCenter = GetSelectedCenter();

			// So we know which polygons need to have their normals recalculated
			List<Polygon> affectedPolygons = new List<Polygon>();

			foreach (PrimitiveBrush brush in targetBrushes) 
			{
				Polygon[] polygons = brush.GetPolygons();

				for (int i = 0; i < polygons.Length; i++) 
				{
					Polygon polygon = polygons[i];

					int vertexCount = polygon.Vertices.Length;

					Vector3[] newPositions = new Vector3[vertexCount];
					Vector2[] newUV = new Vector2[vertexCount];

					for (int j = 0; j < vertexCount; j++) 
					{
						newPositions[j] = polygon.Vertices[j].Position;
						newUV[j] = polygon.Vertices[j].UV;
					}

					bool polygonAffected = false;
					for (int j = 0; j < vertexCount; j++) 
					{
						Vertex vertex = polygon.Vertices[j];
						if(selectedVertices.ContainsKey(vertex))
						{
							Vector3 newPosition = vertex.Position;
							newPosition = brush.transform.TransformPoint(newPosition);
							newPosition -= scalarCenter;
							newPosition *= scalar;
							newPosition += scalarCenter;

							newPosition = brush.transform.InverseTransformPoint(newPosition);

							newPositions[j] = newPosition;

							newUV[j] = GeometryHelper.GetUVForPosition(polygon, newPosition);

							polygonAffected = true;
						}
					}

					if(polygonAffected)
					{
						affectedPolygons.Add(polygon);
					}

					// Apply all the changes to the polygon
					for (int j = 0; j < vertexCount; j++) 
					{
						Vertex vertex = polygon.Vertices[j];
						vertex.Position = newPositions[j];
						vertex.UV = newUV[j];
					}

					polygon.CalculatePlane();
				}
			}

			if(affectedPolygons.Count > 0)
			{
				for (int i = 0; i < affectedPolygons.Count; i++) 
				{
					affectedPolygons[i].ResetVertexNormals();
				}

				foreach (PrimitiveBrush brush in targetBrushes) 
				{
					brush.Invalidate(true);

					brush.BreakTypeRelation();
				}
			}
		}

		public void TranslateSelectedVertices(Vector3 worldDelta)
		{	
			foreach (PrimitiveBrush brush in targetBrushes) 
			{
				bool anyAffected = false;

				Polygon[] polygons = brush.GetPolygons();
				Vector3 localDelta = brush.transform.InverseTransformDirection(worldDelta);

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

					bool polygonAffected = false;

					for (int j = 0; j < vertexCount; j++) 
					{
						Vertex vertex = polygon.Vertices[j];
						if(selectedVertices.ContainsKey(vertex))
						{
							Vector3 startPosition = startPositions[vertex];
							Vector3 newPosition = vertex.Position + localDelta;

							Vector3 accumulatedDelta = newPosition - startPosition;

							if(CurrentSettings.PositionSnappingEnabled)
							{
								float snapDistance = CurrentSettings.PositionSnapDistance;
								//							newPosition = targetBrush.transform.TransformPoint(newPosition);
								accumulatedDelta = MathHelper.RoundVector3(accumulatedDelta, snapDistance);
								//							newPosition = targetBrush.transform.InverseTransformPoint(newPosition);
							}

							if(accumulatedDelta != Vector3.zero)
							{
								newPosition = startPosition + accumulatedDelta;

								newPositions[j] = newPosition;

								newUV[j] = GeometryHelper.GetUVForPosition(polygon, newPosition);

								polygonAffected = true;
								anyAffected = true;
							}
						}
					}

					// Apply all the changes to the polygon
					for (int j = 0; j < vertexCount; j++) 
					{
						Vertex vertex = polygon.Vertices[j];
						vertex.Position = newPositions[j];
						vertex.UV = newUV[j];
					}

					if(polygonAffected)
					{
						// Polygon geometry has changed, inform the polygon that it needs to recalculate its cached plane
						polygons[i].CalculatePlane();

						Vector3 newPlaneNormal = polygons[i].Plane.normal;

						// Find the rotation from the original polygon plane to the new polygon plane
						Quaternion normalRotation = Quaternion.FromToRotation(previousPlaneNormal, newPlaneNormal);

						// Update the affected normals so they are rotated by the rotational difference of the polygon from translation
						for (int j = 0; j < vertexCount; j++) 
						{
							Vertex vertex = polygon.Vertices[j];
							vertex.Normal = normalRotation * vertex.Normal;
						}
					}
				}

				if(anyAffected) // If any polygons have changed
				{
					// Mark the polygons and brush as having changed
					brush.Invalidate(true);

					// Assume that the brush no longer resembles it's base shape, this has false positives but that's not a big issue
					brush.BreakTypeRelation();
				}
			}

			if (CurrentSettings.PositionSnappingEnabled && (CurrentSettings.AlwaysSnapToCurrentGrid != inverseSnapSelectionToCurrentGridLogic)) {
				SnapSelectedVertices(true);
			}
		}

		public void SnapSelectedVertices(bool isAbsoluteGrid)
		{
			// So we know which polygons need to have their normals recalculated
			List<Polygon> affectedPolygons = new List<Polygon>();

			foreach (PrimitiveBrush brush in targetBrushes) 
			{
				Polygon[] polygons = brush.GetPolygons();

				for (int i = 0; i < polygons.Length; i++) 
				{
					Polygon polygon = polygons[i];
					
					int vertexCount = polygon.Vertices.Length;
					
					Vector3[] newPositions = new Vector3[vertexCount];
					Vector2[] newUV = new Vector2[vertexCount];
					
					for (int j = 0; j < vertexCount; j++) 
					{
						newPositions[j] = polygon.Vertices[j].Position;
						newUV[j] = polygon.Vertices[j].UV;
					}

					bool polygonAffected = false;
					for (int j = 0; j < vertexCount; j++) 
					{
						Vertex vertex = polygon.Vertices[j];
						if(selectedVertices.ContainsKey(vertex))
						{
							Vector3 newPosition = vertex.Position;
							
							float snapDistance = CurrentSettings.PositionSnapDistance;
							if(isAbsoluteGrid)
							{
								newPosition = brush.transform.TransformPoint(newPosition);
							}
							newPosition = MathHelper.RoundVector3(newPosition, snapDistance);
							if(isAbsoluteGrid)
							{
								newPosition = brush.transform.InverseTransformPoint(newPosition);
							}
							
							newPositions[j] = newPosition;

							newUV[j] = GeometryHelper.GetUVForPosition(polygon, newPosition);

							polygonAffected = true;
						}
					}

					if(polygonAffected)
					{
						affectedPolygons.Add(polygon);
					}
					
					// Apply all the changes to the polygon
					for (int j = 0; j < vertexCount; j++) 
					{
						Vertex vertex = polygon.Vertices[j];
						vertex.Position = newPositions[j];
						vertex.UV = newUV[j];
					}

					polygon.CalculatePlane();
				}
			}

			if(affectedPolygons.Count > 0)
			{
				for (int i = 0; i < affectedPolygons.Count; i++) 
				{
					affectedPolygons[i].ResetVertexNormals();
				}

				foreach (PrimitiveBrush brush in targetBrushes) 
				{
					brush.Invalidate(true);

					brush.BreakTypeRelation();
				}
			}
		}

		public bool AnySelected
		{
			get
			{
				if(selectedVertices.Count > 0 || selectedEdges.Count > 0)
				{
					return true;
				}
				else
				{
					return false;
				}
			}
		}
		
		// Used so that the gizmo for moving the points is positioned at the average between the selection
		public Vector3 GetSelectedCenter()
		{
			Vector3 average = Vector3.zero;
			int numberFound = 0;

			foreach (KeyValuePair<Vertex, Brush> selectedVertex in selectedVertices) 
			{
				Vector3 worldPosition = selectedVertex.Value.transform.TransformPoint(selectedVertex.Key.Position);
				average += worldPosition;
				numberFound++;
			}
			
			if(numberFound > 0)
			{
				return average / numberFound;
			}
			else
			{
				return Vector3.zero;
			}
		}

		public override void ResetTool ()
		{
			ClearSelection();
		}

		public override void OnSceneGUI (UnityEditor.SceneView sceneView, Event e)
		{
			base.OnSceneGUI(sceneView, e); // Allow the base logic to calculate first

            if (e.type == EventType.MouseUp || e.rawType == EventType.MouseUp)
            {
                moveInProgress = false;
            }

            if (e.type == EventType.KeyDown || e.type == EventType.KeyUp)
            {
                OnKeyAction(sceneView, e);
            }

            if (primaryTargetBrush != null && AnySelected)
			{
				if(startPositions.Count == 0)
				{
					foreach (KeyValuePair<Vertex, Brush> selectedVertex in selectedVertices) 
					{
						startPositions.Add(selectedVertex.Key, selectedVertex.Key.Position);
					}				
				}

				// Make the handle respect the Unity Editor's Local/World orientation mode
				Quaternion handleDirection = Quaternion.identity;
				if(Tools.pivotRotation == PivotRotation.Local)
				{
					handleDirection = primaryTargetBrush.transform.rotation;
				}
				
				// Grab a source point and convert from local space to world.
                // This is the emergency fall-back solution when no vertex is found.
				Vector3 sourceWorldPosition = GetSelectedCenter();


				if(e.type == EventType.MouseUp)
				{
					Undo.RecordObjects(targetBrushTransforms, "Moved Vertices");
					Undo.RecordObjects(targetBrushes, "Moved Vertices");

					List<PrimitiveBrush> changedBrushes = AutoWeld();

                    // Only invalidate the brushes that have actually changed
					foreach (PrimitiveBrush brush in changedBrushes) 
					{
						brush.Invalidate(true);

						brush.BreakTypeRelation();
					}
				}

				EditorGUI.BeginChangeCheck();

                // If not moving a vertex yet:
                if (!moveInProgress)
                {
                    // Find a selected vertex close to the mouse cursor.
                    Vector3 vpos;
                    Brush currentBrush;
                    if (FindClosestSelectedVertexAtMousePosition(out vpos, out movingVertex))
                        if (selectedVertices.TryGetValue(movingVertex, out currentBrush))
                            sourceWorldPosition = currentBrush.transform.TransformPoint(movingVertex.Position);
                }
                else
                {
                    // Move the last selected vertex.
                    Brush currentBrush;
                    if (selectedVertices.TryGetValue(movingVertex, out currentBrush))
                        sourceWorldPosition = currentBrush.transform.TransformPoint(movingVertex.Position);
                }

				// Display a handle and allow the user to determine a new position in world space
				Vector3 newWorldPosition = Handles.PositionHandle(sourceWorldPosition, handleDirection);

				if(EditorGUI.EndChangeCheck())
				{
					Undo.RecordObjects(targetBrushTransforms, "Moved Vertices");
					Undo.RecordObjects(targetBrushes, "Moved Vertices");
					
					Vector3 deltaWorld = newWorldPosition - sourceWorldPosition;

					//				if(deltaLocal.sqrMagnitude > 0)
					//				{
					TranslateSelectedVertices(deltaWorld);
					isMarqueeSelection = false;
					moveInProgress = true;
					foreach (PrimitiveBrush brush in targetBrushes) 
					{
						EditorUtility.SetDirty (brush);
					}
					e.Use();
					// Shouldn't reset the pivot while the vertices are being manipulated, so make sure the pivot
					// is set to get reset at next opportunity
					pivotNeedsReset = true;
				}
				else
				{
					// The user is no longer moving a handle
					if(pivotNeedsReset)
					{
						// Pivot needs to be reset, so reset it!
						foreach (PrimitiveBrush brush in targetBrushes) 
						{
							brush.ResetPivot();	
						}

						pivotNeedsReset = false;
					}

					startPositions.Clear();
				}
			}

			if(primaryTargetBrush != null)
			{
				
				if (e.type == EventType.MouseDown) 
				{
					OnMouseDown(sceneView, e);
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

            //			if(e.type == EventType.Repaint)
            {
				OnRepaint(sceneView, e);
			}
		}

        void SelectEdges(Polygon polygon, IDictionary<Vertex, Brush> selectedVertices)
        {
            for (int j = 0; j < polygon.Vertices.Length; j++)
            {
                Vertex vertex1 = polygon.Vertices[j];
                Vertex vertex2 = polygon.Vertices[(j + 1) % polygon.Vertices.Length];
                if (selectedVertices.ContainsKey(vertex1) && selectedVertices.ContainsKey(vertex2))
                    selectedEdges.Add(new Edge(vertex1, vertex2));
            }
        }

        void SelectEdges(Brush brush, Polygon[] polygons, Edge newEdge)
		{
			// Can only select a valid edge, if it's not valid early out
			if(newEdge == null || newEdge.Vertex1 == null || newEdge.Vertex2 == null)
			{
				return;
			}
			// Select the new edge
			selectedEdges.Add(newEdge);

			for (int i = 0; i < polygons.Length; i++) 
			{
				Polygon polygon = polygons[i];

				for (int j = 0; j < polygon.Vertices.Length; j++) 
				{
					Vertex vertex = polygon.Vertices[j];

					if(newEdge.Vertex1.Position.EqualsWithEpsilon(vertex.Position)
						|| newEdge.Vertex2.Position.EqualsWithEpsilon(vertex.Position))
					{
						if(!selectedVertices.ContainsKey(vertex))
						{
							selectedVertices.Add(vertex, brush);
						}
					}

				}
			}
		}

		void SelectVertices(Brush brush, Polygon[] polygons, List<Vertex> newSelectedVertices)
		{
			for (int i = 0; i < polygons.Length; i++) 
			{
				Polygon polygon = polygons[i];

				for (int j = 0; j < polygon.Vertices.Length; j++) 
				{
					Vertex vertex = polygon.Vertices[j];

					for (int k = 0; k < newSelectedVertices.Count; k++) 
					{
						if(newSelectedVertices[k].Position == vertex.Position)
						{
							if(!selectedVertices.ContainsKey(vertex))
							{
								selectedVertices.Add(vertex, brush);
							}
							break;
						}
					}
				}
			}
		}

		void OnToolbarGUI(int windowID)
		{
			GUILayout.Label("Vertex", SabreGUILayout.GetTitleStyle());

			// Button should only be enabled if there are any vertices selected
			GUI.enabled = selectedVertices.Count > 0;

			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button("Connect", EditorStyles.miniButton))
			{
				if(selectedVertices != null)
				{
					// Cache selection
					Dictionary<Brush, List<Vertex>> refinedSelections = new Dictionary<Brush, List<Vertex>>();

					foreach (PrimitiveBrush brush in targetBrushes) 
					{
						refinedSelections.Add(brush, SelectedVerticesOfBrush(brush));
					}

					ClearSelection();

					foreach (PrimitiveBrush brush in targetBrushes) 
					{
						Undo.RecordObject(brush.transform, "Connect Vertices");
						Undo.RecordObject(brush, "Connect Vertices");

						List<Edge> newEdges;

//						Polygon[] newPolygons = VertexUtility.ConnectVertices(brush.GetPolygons(), refinedSelections[brush], out newEdge);
						Polygon[] newPolygons = VertexUtility.ConnectVertices(brush.GetPolygons(), refinedSelections[brush], out newEdges);
						
						if(newPolygons != null)
						{
							brush.SetPolygons(newPolygons);

							for (int i = 0; i < newEdges.Count; i++) 
							{
								SelectEdges(brush, newPolygons, newEdges[i]);
							}
						}							
					}
				}
			}

//			if (GUILayout.Button("Remove", EditorStyles.miniButton))
//			{
//				if(selectedVertices != null)
//				{
//					Dictionary<Brush, List<Vertex>> refinedSelections = new Dictionary<Brush, List<Vertex>>();
//
//					foreach (PrimitiveBrush brush in targetBrushes) 
//					{
//						refinedSelections.Add(brush, SelectedVerticesOfBrush(brush));
//					}
//
//					foreach (PrimitiveBrush brush in targetBrushes) 
//					{
//						Undo.RecordObject(brush.transform, "Remove Vertices");
//						Undo.RecordObject(brush, "Remove Vertices");
//
//						Polygon[] newPolygons = VertexUtility.RemoveVertices(brush.GetPolygons(), refinedSelections[brush]);
//						brush.SetPolygons(newPolygons);
//					}
//
//					ClearSelection();
//				}
//			}

			GUILayout.EndHorizontal();

			if (GUILayout.Button("Weld Selection To Mid-Point", EditorStyles.miniButton))
			{
				if(selectedVertices != null)
				{
					Dictionary<Brush, List<Vertex>> refinedSelections = new Dictionary<Brush, List<Vertex>>();

					foreach (PrimitiveBrush brush in targetBrushes) 
					{
						refinedSelections.Add(brush, SelectedVerticesOfBrush(brush));
					}

					ClearSelection();

					foreach (PrimitiveBrush brush in targetBrushes) 
					{
						Undo.RecordObject(brush.transform, "Weld Vertices");
						Undo.RecordObject(brush, "Weld Vertices");

						Polygon[] newPolygons = VertexUtility.WeldVerticesToCenter(brush.GetPolygons(), refinedSelections[brush]);
						
						if(newPolygons != null)
						{
							brush.SetPolygons(newPolygons);
						}

						SelectVertices(brush, newPolygons, refinedSelections[brush]);
					}
				}
			}

			EditorGUILayout.BeginHorizontal();


			GUI.SetNextControlName("weldToleranceField");
			weldTolerance = EditorGUILayout.FloatField(weldTolerance);

			bool keyboardEnter = Event.current.isKey 
				&& Event.current.keyCode == KeyCode.Return 
				&& Event.current.type == EventType.KeyUp 
				&& GUI.GetNameOfFocusedControl() == "weldToleranceField";

			if (GUILayout.Button("Weld with Tolerance", EditorStyles.miniButton) || keyboardEnter)
			{
				if(selectedVertices != null)
				{
					Dictionary<Brush, List<Vertex>> refinedSelections = new Dictionary<Brush, List<Vertex>>();

					foreach (PrimitiveBrush brush in targetBrushes) 
					{
						refinedSelections.Add(brush, SelectedVerticesOfBrush(brush));
					}

					ClearSelection();

					foreach (PrimitiveBrush brush in targetBrushes) 
					{
						Undo.RecordObject(brush.transform, "Weld Vertices");
						Undo.RecordObject(brush, "Weld Vertices");

						Polygon[] newPolygons = VertexUtility.WeldNearbyVertices(weldTolerance, brush.GetPolygons(), refinedSelections[brush]);

						if(newPolygons != null)
						{
							brush.SetPolygons(newPolygons);
						}

						SelectVertices(brush, newPolygons, refinedSelections[brush]);

					}
				}
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button("Global Snap", EditorStyles.miniButton))
			{
				foreach (PrimitiveBrush brush in targetBrushes) 
				{
					Undo.RecordObject(brush.transform, "Snap Vertices");
					Undo.RecordObject(brush, "Snap Vertices");
				}

				SnapSelectedVertices(true);
			}

			if (GUILayout.Button("Local Snap", EditorStyles.miniButton))
			{
				foreach (PrimitiveBrush brush in targetBrushes) 
				{
					Undo.RecordObject(brush.transform, "Snap Vertices");
					Undo.RecordObject(brush, "Snap Vertices");
				}

				SnapSelectedVertices(false);
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();

			GUI.SetNextControlName("scaleField");
			scale = EditorGUILayout.FloatField(scale);

			keyboardEnter = Event.current.isKey 
				&& Event.current.keyCode == KeyCode.Return 
				&& Event.current.type == EventType.KeyUp 
				&& GUI.GetNameOfFocusedControl() == "scaleField";

			if (GUILayout.Button("Scale", EditorStyles.miniButton) || keyboardEnter)
			{
				foreach (PrimitiveBrush brush in targetBrushes) 
				{
					Undo.RecordObject(brush.transform, "Scale Vertices");
					Undo.RecordObject(brush, "Scale Vertices");
				}

				ScaleSelectedVertices(scale);
			}
			EditorGUILayout.EndHorizontal();

			GUILayout.Label("Edge", SabreGUILayout.GetTitleStyle());

			GUI.enabled = selectedEdges.Count > 0;

			if (GUILayout.Button("Connect Mid-Points", EditorStyles.miniButton))
			{
				if(selectedEdges != null)
				{
					List<Edge> selectedEdgesCopy = new List<Edge>(selectedEdges);
					ClearSelection();
					foreach (PrimitiveBrush brush in targetBrushes) 
					{
						Undo.RecordObject(brush.transform, "Connect Mid-Points");
						Undo.RecordObject(brush, "Connect Mid-Points");

						Polygon[] newPolygons;
						List<Edge> newEdges;
						EdgeUtility.SplitPolygonsByEdges(brush.GetPolygons(), selectedEdgesCopy, out newPolygons, out newEdges);

						brush.SetPolygons(newPolygons);

						for (int i = 0; i < newEdges.Count; i++) 
						{
							SelectEdges(brush, newPolygons, newEdges[i]);
						}
					}
				}
			}

			GUILayout.BeginHorizontal();

			if (GUILayout.Button("Split", EditorStyles.miniButton))
			{
				if(selectedEdges != null)
				{
					List<KeyValuePair<Vertex, Brush>> newSelectedVertices = new List<KeyValuePair<Vertex, Brush>>();
					foreach (PrimitiveBrush brush in targetBrushes) 
					{
						Undo.RecordObject(brush.transform, "Split Edge");
						Undo.RecordObject(brush, "Split Edge");
						Polygon[] polygons = brush.GetPolygons();

						for (int j = 0; j < selectedEdges.Count; j++) 
						{
							// First check if this edge actually belongs to the brush
							Brush parentBrush = selectedVertices[selectedEdges[j].Vertex1];

							if(parentBrush == brush)
							{
								for (int i = 0; i < polygons.Length; i++) 
								{
									Vertex newVertex;
									if(EdgeUtility.SplitPolygonAtEdge(polygons[i], selectedEdges[j], out newVertex))
									{
										newSelectedVertices.Add(new KeyValuePair<Vertex, Brush>(newVertex, brush));
									}
								}
							}
						}

						brush.Invalidate(true);
					}

					ClearSelection();

					for (int i = 0; i < newSelectedVertices.Count; i++) 
					{
						Brush brush = newSelectedVertices[i].Value;
						Vertex vertex = newSelectedVertices[i].Key;

						SelectVertices(brush, brush.GetPolygons(), new List<Vertex>() { vertex } );
					}
				}
			}
		
			GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            SabreGUILayout.RightClickMiniButton("Chamfer", "Bevels or rounds sharp edges.",
                () => OnEdgeChamfer(false),
                () => OnEdgeChamfer(true)
            );

            GUILayout.EndHorizontal();
        }

        List<Vertex> SelectedVerticesOfBrush(Brush brush)
		{
			List<Vertex> refinedSelection = new List<Vertex>();

			foreach (KeyValuePair<Vertex, Brush> selectedVertexPair in selectedVertices) 
			{
				if(selectedVertexPair.Value == brush)
				{
					refinedSelection.Add(selectedVertexPair.Key);
				}
			}
			return refinedSelection;
		}

        private void OnKeyAction(SceneView sceneView, Event e)
        {
            if (KeyMappings.EventsMatch(e, Event.KeyboardEvent(KeyMappings.Instance.SnapSelectionToCurrentGrid)))
            {
                if (e.type == EventType.KeyDown)
                {
                    inverseSnapSelectionToCurrentGridLogic = true;
                }
                else
                {
                    inverseSnapSelectionToCurrentGridLogic = false;
                }
            }
        }

        public void OnRepaint (SceneView sceneView, Event e)
		{
			if(isMarqueeSelection && sceneView == SceneView.lastActiveSceneView)
			{
				SabreGraphics.DrawMarquee(marqueeStart, marqueeEnd);
			}

			if(primaryTargetBrush != null)
			{
				DrawVertices(sceneView, e);
			}

            // Draw UI specific to this editor
#if UNITY_2019_3_OR_NEWER
            Rect rectangle = new Rect(0, 50, 185, 195);
#else
            Rect rectangle = new Rect(0, 50, 175, 180);
#endif
            GUIStyle toolbar = new GUIStyle(EditorStyles.toolbar);
			toolbar.normal.background = SabreCSGResources.ClearTexture;
			toolbar.fixedHeight = rectangle.height;
			GUILayout.Window(140002, rectangle, OnToolbarGUI, "", toolbar);
		}

		void OnMouseDown (SceneView sceneView, Event e)
		{
			isMarqueeSelection = false;
			moveInProgress = false;

			marqueeStart = e.mousePosition;

            if (EditorHelper.IsMousePositionInInvalidRects(e.mousePosition))
            {
                marqueeCancelled = true;
            }
            else
            {
                marqueeCancelled = false;
            }
        }

		void OnMouseDrag (SceneView sceneView, Event e)
		{
			if(!CameraPanInProgress)
			{
				if(!moveInProgress && e.button == 0)
				{
                    if (!marqueeCancelled)
                    {
                        marqueeEnd = e.mousePosition;
                        isMarqueeSelection = true;
                        sceneView.Repaint();
                    }
				}
			}
		}

		// Select any vertices
		void OnMouseUp (SceneView sceneView, Event e)
		{
			if(e.button == 0 && !CameraPanInProgress)
			{
				Transform sceneViewTransform = sceneView.camera.transform;
				Vector3 sceneViewPosition = sceneViewTransform.position;
				if(moveInProgress)
				{

				}
				else
				{
					if(isMarqueeSelection) // Marquee vertex selection
					{
						selectedEdges.Clear();

						isMarqueeSelection = false;
						
						marqueeEnd = e.mousePosition;

						foreach(PrimitiveBrush brush in targetBrushes)
						{
							Polygon[] polygons = brush.GetPolygons();

							for (int i = 0; i < polygons.Length; i++) 
							{
								Polygon polygon = polygons[i];
								
								for (int j = 0; j < polygon.Vertices.Length; j++) 
								{
									Vertex vertex = polygon.Vertices[j];
									
									Vector3 worldPosition = brush.transform.TransformPoint(vertex.Position);
									Vector3 screenPoint = sceneView.camera.WorldToScreenPoint(worldPosition);
									
									// Point is contained within marquee box
									if(SabreMouse.MarqueeContainsPoint(marqueeStart, marqueeEnd, screenPoint))
									{
										if(EnumHelper.IsFlagSet(e.modifiers, EventModifiers.Control))
										{
											// Only when holding control should a deselection occur from a valid point
											selectedVertices.Remove(vertex);
										}
										else
										{
											// Point was in marquee (and ctrl wasn't held) so select it!
											if(!selectedVertices.ContainsKey(vertex))
											{
												selectedVertices.Add(vertex, brush);
											}
										}
									}
									else if(!EnumHelper.IsFlagSet(e.modifiers, EventModifiers.Control) 
									        && !EnumHelper.IsFlagSet(e.modifiers, EventModifiers.Shift))
									{
										selectedVertices.Remove(vertex);
									}
                                }
                                SelectEdges(polygon, selectedVertices);
                            }
                        }
                        SceneView.RepaintAll();
					}
					else if (!EditorHelper.IsMousePositionInInvalidRects(e.mousePosition) && !marqueeCancelled) // Clicking style vertex selection
                    {
						Vector2 mousePosition = e.mousePosition;

						bool clickedAnyPoints = false;
//						Vertex closestVertexFound = null;
						Vector3 closestVertexWorldPosition = Vector3.zero;
						float closestDistanceSquare = float.PositiveInfinity;

						foreach (PrimitiveBrush brush in targetBrushes) 
						{
							Polygon[] polygons = brush.GetPolygons();
							for (int i = 0; i < polygons.Length; i++) 
							{
								Polygon polygon = polygons[i];

								for (int j = 0; j < polygon.Vertices.Length; j++) 
								{
									Vertex vertex = polygon.Vertices[j];

									Vector3 worldPosition = brush.transform.TransformPoint(vertex.Position);

									float vertexDistanceSquare = (sceneViewPosition - worldPosition).sqrMagnitude;

									if(EditorHelper.InClickZone(mousePosition, worldPosition) && vertexDistanceSquare < closestDistanceSquare)
									{
//										closestVertexFound = vertex;
										closestVertexWorldPosition = worldPosition;
										clickedAnyPoints = true;
										closestDistanceSquare = vertexDistanceSquare;
									}
								}
							}
						}

						if(!EnumHelper.IsFlagSet(e.modifiers, EventModifiers.Control) && !EnumHelper.IsFlagSet(e.modifiers, EventModifiers.Shift))
						{
							ClearSelection();
						}

						foreach (PrimitiveBrush brush in targetBrushes) 
						{
							Polygon[] polygons = brush.GetPolygons();
							for (int i = 0; i < polygons.Length; i++) 
							{
								Polygon polygon = polygons[i];
								
								for (int j = 0; j < polygon.Vertices.Length; j++) 
								{
									Vertex vertex = polygon.Vertices[j];
									Vector3 worldPosition = brush.transform.TransformPoint(vertex.Position);
									if(clickedAnyPoints && worldPosition == closestVertexWorldPosition)
									{
										if(EnumHelper.IsFlagSet(e.modifiers, EventModifiers.Control))
										{
											if(!selectedVertices.ContainsKey(vertex))
											{
												selectedVertices.Add(vertex, brush);
											}
											else
											{
												selectedVertices.Remove(vertex);
											}
										}
										else
										{
											if(!selectedVertices.ContainsKey(vertex))
											{
												selectedVertices.Add(vertex, brush);
											}
										}
									}
									else if(!EnumHelper.IsFlagSet(e.modifiers, EventModifiers.Control) 
									        && !EnumHelper.IsFlagSet(e.modifiers, EventModifiers.Shift))
									{
										selectedVertices.Remove(vertex);
									}
								}
                                SelectEdges(polygon, selectedVertices);
                            }
						}


						if(!clickedAnyPoints) // Couldn't click any directly, next try to click an edge
						{
							Edge selectedEdge = null;
							Vector3 selectedEdgeWorldPosition1 = Vector3.zero;
							Vector3 selectedEdgeWorldPosition2 = Vector3.zero;
							// Used to track the closest edge clicked, so if we could click through several edges with
							// one click, then we only count the closest
							float closestFound = float.PositiveInfinity;

							foreach (PrimitiveBrush brush in targetBrushes) 
							{
								Polygon[] polygons = brush.GetPolygons();
								for (int i = 0; i < polygons.Length; i++) 
								{
									Polygon polygon = polygons[i];
									for (int j = 0; j < polygon.Vertices.Length; j++) 
									{
										Vector3 worldPoint1 = brush.transform.TransformPoint(polygon.Vertices[j].Position);
										Vector3 worldPoint2 = brush.transform.TransformPoint(polygon.Vertices[(j+1) % polygon.Vertices.Length].Position);

										// Distance from the mid point of the edge to the camera
										float squareDistance = (Vector3.Lerp(worldPoint1,worldPoint2,0.5f) - Camera.current.transform.position).sqrMagnitude;

										float screenDistance = HandleUtility.DistanceToLine(worldPoint1, worldPoint2);
										if(screenDistance < EDGE_SCREEN_TOLERANCE && squareDistance < closestFound)
										{
											selectedEdgeWorldPosition1 = worldPoint1;
											selectedEdgeWorldPosition2 = worldPoint2;
											selectedEdge = new Edge(polygon.Vertices[j], polygon.Vertices[(j+1) % polygon.Vertices.Length]);

											closestFound = squareDistance;
										}
									}
								}
							}

							List<Vertex> newSelectedVertices = new List<Vertex>();

							if(selectedEdge != null)
							{
								newSelectedVertices.Add(selectedEdge.Vertex1);
								newSelectedVertices.Add(selectedEdge.Vertex2);

								selectedEdges.Add(selectedEdge);

								foreach (PrimitiveBrush brush in targetBrushes) 
								{
									Polygon[] polygons = brush.GetPolygons();

									for (int i = 0; i < polygons.Length; i++) 
									{
										Polygon polygon = polygons[i];

										for (int j = 0; j < polygon.Vertices.Length; j++) 
										{
											Vertex vertex = polygon.Vertices[j];

											Vector3 worldPosition = brush.transform.TransformPoint(vertex.Position);
											if(worldPosition == selectedEdgeWorldPosition1
												|| worldPosition == selectedEdgeWorldPosition2)
											{
												if(!selectedVertices.ContainsKey(vertex))
												{
													selectedVertices.Add(vertex, brush);
												}
											}
										}
									}
								}
							}
						}
					}
					moveInProgress = false;

					
					// Repaint all scene views to show the selection change
					SceneView.RepaintAll();
				}

				if(selectedVertices.Count > 0)
				{
					e.Use();
				}
			}
		}

		void DrawVertices(SceneView sceneView, Event e)
		{
			Camera sceneViewCamera = sceneView.camera;

			SabreCSGResources.GetVertexMaterial().SetPass (0);
			GL.PushMatrix();
			GL.LoadPixelMatrix();

			GL.Begin(GL.QUADS);

			// Draw each handle, colouring it if it's selected
			foreach (PrimitiveBrush brush in targetBrushes) 
			{
				Polygon[] polygons = brush.GetPolygons();

				Vector3 target;

				for (int i = 0; i < polygons.Length; i++) 
				{
					for (int j = 0; j < polygons[i].Vertices.Length; j++) 
					{
						Vertex vertex = polygons[i].Vertices[j];

						if(selectedVertices.ContainsKey(vertex))
						{
							GL.Color(new Color32(0, 255, 128, 255));
						}
						else
						{
							GL.Color(Color.white);
						}

						target = sceneViewCamera.WorldToScreenPoint(brush.transform.TransformPoint(vertex.Position));
						if(target.z > 0)
						{
							// Make it pixel perfect
							target = MathHelper.RoundVector3(target);
							SabreGraphics.DrawBillboardQuad(target, 8, 8);
						}
					}
				}
			}

			GL.End();

			// Draw lines for selected edges
			SabreCSGResources.GetSelectedBrushMaterial().SetPass(0);

			GL.Begin(GL.LINES);
			GL.Color(Color.green);

			for (int edgeIndex = 0; edgeIndex < selectedEdges.Count; edgeIndex++) 
			{
				Edge edge = selectedEdges[edgeIndex];

				if(selectedVertices.ContainsKey(edge.Vertex1))
				{
					Brush brush = selectedVertices[edge.Vertex1];

					Vector3 target1 = sceneViewCamera.WorldToScreenPoint(brush.transform.TransformPoint(edge.Vertex1.Position));
					Vector3 target2 = sceneViewCamera.WorldToScreenPoint(brush.transform.TransformPoint(edge.Vertex2.Position));

					if(target1.z > 0 && target2.z > 0)
					{
						SabreGraphics.DrawScreenLine(target1, target2);
					}
				}
			}

			GL.End();

			GL.PopMatrix();
		}

        /// <summary>Finds a selected vertex at the current mouse position.</summary>
        /// <param name="closestVertexWorldPosition">The closest selected vertex world position.</param>
        /// <returns>True if a vertex was found else false.</returns>
        private bool FindClosestSelectedVertexAtMousePosition(out Vector3 closestVertexWorldPosition, out Vertex closestVertex)
        {
            // find a vertex close to the mouse cursor.
            Transform sceneViewTransform = SceneView.currentDrawingSceneView.camera.transform;
            Vector3 sceneViewPosition = sceneViewTransform.position;
            Vector2 mousePosition = Event.current.mousePosition;

            bool foundAnyPoints = false;
            closestVertex = null;
            closestVertexWorldPosition = Vector3.zero;
            float closestDistanceSquare = float.PositiveInfinity;

            foreach (PrimitiveBrush brush in selectedVertices.Values)
            {
                Polygon[] polygons = brush.GetPolygons();
                for (int i = 0; i < polygons.Length; i++)
                {
                    Polygon polygon = polygons[i];

                    for (int j = 0; j < polygon.Vertices.Length; j++)
                    {
                        Vertex vertex = polygon.Vertices[j];
                        if (!selectedVertices.ContainsKey(vertex)) continue;

                        Vector3 worldPosition = brush.transform.TransformPoint(vertex.Position);

                        float vertexDistanceSquare = (sceneViewPosition - worldPosition).sqrMagnitude;

                        if (EditorHelper.InClickZone(mousePosition, worldPosition) && vertexDistanceSquare < closestDistanceSquare)
                        {
                            closestVertex = vertex;
                            closestVertexWorldPosition = worldPosition;
                            foundAnyPoints = true;
                            closestDistanceSquare = vertexDistanceSquare;
                        }
                    }
                }
            }

            if (foundAnyPoints == false)
            {
                // None matched, next try finding the closest by distance
                Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                closestVertexWorldPosition = Vector3.zero;
                closestDistanceSquare = float.PositiveInfinity;

                foreach (PrimitiveBrush brush in selectedVertices.Values)
                {
                    Polygon[] polygons = brush.GetPolygons();
                    for (int i = 0; i < polygons.Length; i++)
                    {
                        Polygon polygon = polygons[i];

                        for (int j = 0; j < polygon.Vertices.Length; j++)
                        {
                            Vertex vertex = polygon.Vertices[j];
                            if (!selectedVertices.ContainsKey(vertex)) continue;

                            Vector3 vertexWorldPosition = brush.transform.TransformPoint(vertex.Position);

                            Vector3 closestPoint = MathHelper.ProjectPointOnLine(ray.origin, ray.direction, vertexWorldPosition);

                            float vertexDistanceSquare = (closestPoint - vertexWorldPosition).sqrMagnitude;

                            if (vertexDistanceSquare < closestDistanceSquare)
                            {
                                closestVertex = vertex;
                                closestVertexWorldPosition = vertexWorldPosition;
                                foundAnyPoints = true;
                                closestDistanceSquare = vertexDistanceSquare;
                            }
                        }
                    }
                }
            }

            return foundAnyPoints;
        }

        /// <summary>
        /// Called when the chamfer edge button is pressed.
        /// </summary>
        /// <param name="popup">If set to <c>true</c> displays the configuration popup.</param>
        private void OnEdgeChamfer(bool popup)
        {
            if (selectedEdges == null) return;

            if (popup)
            {
                // create a chamfer configuration popup window.
                ToolSettingsPopup.Create("Chamfer Settings", 205, (rect) => {
                    chamferDistance = EditorGUILayout.FloatField(new GUIContent("Distance", "The size of the chamfered curve."), chamferDistance);
                    if (chamferDistance < 0.0f) chamferDistance = 0.0f;
                    chamferIterations = EditorGUILayout.IntField(new GUIContent("Iterations", "The amount of iterations determines how detailed the chamfer is (e.g. 1 is a simple bevel)."), chamferIterations);
                    if (chamferIterations < 1) chamferIterations = 1;
                })
                .AddConfirmButton("Chamfer", () => OnEdgeChamfer(false))
                .SetWikiLink("Brush-Tools-Vertex#chamfer-edges")
                .Show();

                return;
            }

            List<KeyValuePair<Vertex, Brush>> newSelectedVertices = new List<KeyValuePair<Vertex, Brush>>();
            foreach (PrimitiveBrush brush in targetBrushes)
            {
                Undo.RecordObject(brush.transform, "Chamfer Edge");
                Undo.RecordObject(brush, "Chamfer Edge");
                Polygon[] polygons = brush.GetPolygons();

                for (int j = 0; j < selectedEdges.Count; j++)
                {
                    // First check if this edge actually belongs to the brush
                    Brush parentBrush = selectedVertices[selectedEdges[j].Vertex1];

                    if (parentBrush == brush)
                    {
                        List<Polygon> resultPolygons;
                        if (PolygonFactory.ChamferPolygons(new List<Polygon>(polygons), selectedEdges, chamferDistance, chamferIterations, out resultPolygons))
                        {
                            brush.SetPolygons(resultPolygons.ToArray());
                        }
                    }
                }

                brush.Invalidate(true);
            }

            ClearSelection();

            for (int i = 0; i < newSelectedVertices.Count; i++)
            {
                Brush brush = newSelectedVertices[i].Value;
                Vertex vertex = newSelectedVertices[i].Key;

                SelectVertices(brush, brush.GetPolygons(), new List<Vertex>() { vertex });
            }
        }

        public override void Deactivated ()
		{
			
		}
	}
}
#endif