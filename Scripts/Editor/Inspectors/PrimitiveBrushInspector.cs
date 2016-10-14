using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
	[CanEditMultipleObjects]
    [CustomEditor(typeof(PrimitiveBrush))]
	public class PrimitiveBrushInspector : BrushBaseInspector
    {
		string scaleString = "1";
		string resizeString = "1";

		Mesh sourceMesh = null;

		SerializedProperty prismSideCountProp;
		SerializedProperty cylinderSideCountProp;
		SerializedProperty sphereSideCountProp;
		SerializedProperty icoSphereIterationCountProp;

		float shellDistance = 0;

		protected override void OnEnable ()
		{
			base.OnEnable ();
			// Setup the SerializedProperties.
			prismSideCountProp = serializedObject.FindProperty ("prismSideCount");
			cylinderSideCountProp = serializedObject.FindProperty ("cylinderSideCount");
			sphereSideCountProp = serializedObject.FindProperty ("sphereSideCount");
			icoSphereIterationCountProp = serializedObject.FindProperty ("icoSphereIterationCount");
		}

		private void ChangeBrushesToType(PrimitiveBrushType newType)
		{
			Undo.RecordObjects(targets, "Change Brush Type");
			PrimitiveBrush[] brushes = BrushTargets.Cast<PrimitiveBrush>().ToArray();
			foreach (PrimitiveBrush brush in brushes) 
			{
				Bounds localBounds = brush.GetBounds();
				brush.BrushType = newType;
				brush.ResetPolygons();

				if(localBounds.size != new Vector3(2, 2, 2))
				{
					BrushUtility.Resize(brush, localBounds.size);
				}
				else
				{
					brush.Invalidate(true);
				}
			}
		}

		private void ResetPolygonsKeepScale()
		{
			PrimitiveBrush[] brushes = BrushTargets.Cast<PrimitiveBrush>().ToArray();
			foreach (PrimitiveBrush brush in brushes) 
			{
				Bounds localBounds = brush.GetBounds();
				brush.ResetPolygons();

				if(localBounds.size != new Vector3(2, 2, 2))
				{
					BrushUtility.Resize(brush, localBounds.size);
				}
				else
				{
					brush.Invalidate(true);
				}
			}
		}

		private void ResetBounds()
		{
			// Reset the bounds to a 2,2,2 cube
			Undo.RecordObjects(targets, "Reset Bounds");
			PrimitiveBrush[] brushes = BrushTargets.Cast<PrimitiveBrush>().ToArray();
			foreach (PrimitiveBrush brush in brushes) 
			{
				BrushUtility.Resize(brush, new Vector3(2, 2, 2));
			}
		}

        void DrawBrushButton(PrimitiveBrushType brushType, PrimitiveBrushType? activeType, GUIStyle brushButtonStyle, GUIStyle labelStyle, int width, int height, bool shortMode)
        {
            GUI.enabled = !activeType.HasValue || activeType.Value != brushType;
            if (GUILayout.Button(new GUIContent(" ", SabreCSGResources.GetButtonTexture(brushType)), brushButtonStyle, GUILayout.Width(width), GUILayout.Height(height)))
            {
                ChangeBrushesToType(brushType);
            }

            Rect lastRect = GUILayoutUtility.GetLastRect();

            string name = brushType.ToString();
            if (brushType == PrimitiveBrushType.IcoSphere)
                name = "Ico";

            if (shortMode && brushType == PrimitiveBrushType.Cylinder)
            {
                name = "Cyl";
            }
            
            GUI.Label(lastRect, name, labelStyle);
        }

        public override void OnInspectorGUI()
        {
			float drawableWidth = EditorGUIUtility.currentViewWidth;
			drawableWidth -= 42; // Take some off for scroll bars and padding

			PrimitiveBrushType[] selectedTypes = BrushTargets.Select(item => ((PrimitiveBrush)item).BrushType).ToArray();

			PrimitiveBrushType? activeType = (selectedTypes.Length == 1) ? (PrimitiveBrushType?)selectedTypes[0] : null;

            using (new NamedVerticalScope("Type"))
            {
                GUILayout.BeginHorizontal();

                float areaWidth = drawableWidth - 18;
                int buttonWidth = Mathf.RoundToInt(areaWidth / 5f);
                int stretchButtonWidth = Mathf.RoundToInt(areaWidth - buttonWidth * 4); // To ensure a justified alignment one button must be stretched slightly
                int buttonHeight = 50;

                GUIStyle brushButtonStyle = new GUIStyle(GUI.skin.button);
                brushButtonStyle.imagePosition = ImagePosition.ImageAbove;
                brushButtonStyle.fontSize = 10;

                GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
                labelStyle.alignment = TextAnchor.LowerCenter;
                labelStyle.fontSize = brushButtonStyle.fontSize;

                bool shortMode = (areaWidth < 260); // Whether certain words need to be abbreviated to fit in the box

                DrawBrushButton(PrimitiveBrushType.Cube, activeType, brushButtonStyle, labelStyle, buttonWidth, buttonHeight, shortMode);
                DrawBrushButton(PrimitiveBrushType.Prism, activeType, brushButtonStyle, labelStyle, buttonWidth, buttonHeight, shortMode);
                DrawBrushButton(PrimitiveBrushType.Cylinder, activeType, brushButtonStyle, labelStyle, stretchButtonWidth, buttonHeight, shortMode);
                DrawBrushButton(PrimitiveBrushType.Sphere, activeType, brushButtonStyle, labelStyle, buttonWidth, buttonHeight, shortMode);
                DrawBrushButton(PrimitiveBrushType.IcoSphere, activeType, brushButtonStyle, labelStyle, buttonWidth, buttonHeight, shortMode);

                GUI.enabled = true; // Reset GUI enabled so that the next items aren't disabled
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();

                if (activeType.HasValue)
                {
                    GUILayout.Label("Active: " + selectedTypes[0]);
                }
                else
                {
                    GUILayout.Label("Active: Mixed");
                }

                if (activeType.HasValue)
                {
                    EditorGUIUtility.labelWidth = 60;
                    EditorGUIUtility.fieldWidth = 50;
                    EditorGUI.BeginChangeCheck();
                    if (activeType.Value == PrimitiveBrushType.Prism)
                    {
                        EditorGUILayout.PropertyField(prismSideCountProp, new GUIContent("Sides"));
                    }
                    else if (activeType.Value == PrimitiveBrushType.Cylinder)
                    {
                        EditorGUILayout.PropertyField(cylinderSideCountProp, new GUIContent("Sides"));
                    }
                    else if (activeType.Value == PrimitiveBrushType.Sphere)
                    {
                        EditorGUILayout.PropertyField(sphereSideCountProp, new GUIContent("Sides"));
                    }
                    else if (activeType.Value == PrimitiveBrushType.IcoSphere)
                    {
                        EditorGUILayout.PropertyField(icoSphereIterationCountProp, new GUIContent("Iterations"));
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        // One of the properties has changed
                        serializedObject.ApplyModifiedProperties();
                        ResetPolygonsKeepScale();
                    }
                }

                GUILayout.EndHorizontal();
            }

            using (new NamedVerticalScope("Size"))
            {
                if (GUILayout.Button(new GUIContent("Reset Bounds", "Resets the bounds of the brush to [2,2,2]")))
                {
                    ResetBounds();
                }

                GUILayout.BeginHorizontal();

                GUI.SetNextControlName("rescaleTextbox");

                scaleString = EditorGUILayout.TextField(scaleString);

                bool keyboardEnter = Event.current.isKey
                    && Event.current.keyCode == KeyCode.Return
                    && Event.current.type == EventType.KeyUp
                    && GUI.GetNameOfFocusedControl() == "rescaleTextbox";

                if (GUILayout.Button("Scale", GUILayout.MaxWidth(drawableWidth / 3f)) || keyboardEnter)
                {
                    // Try to parse a Vector3 scale from the input string
                    Vector3 scaleVector3;
                    if (StringHelper.TryParseScale(scaleString, out scaleVector3))
                    {
                        // None of the scale components can be zero
                        if (scaleVector3.x != 0 && scaleVector3.y != 0 && scaleVector3.z != 0)
                        {
                            // Rescale all the brushes
                            Undo.RecordObjects(targets, "Scale Polygons");
                            foreach (var thisBrush in targets)
                            {
                                BrushUtility.Scale((PrimitiveBrush)thisBrush, scaleVector3);
                            }
                        }
                    }
                }

                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();

                GUI.SetNextControlName("resizeTextbox");

                resizeString = EditorGUILayout.TextField(resizeString);

                keyboardEnter = Event.current.isKey
                    && Event.current.keyCode == KeyCode.Return
                    && Event.current.type == EventType.KeyUp
                    && GUI.GetNameOfFocusedControl() == "resizeTextbox";

                if (GUILayout.Button("Resize", GUILayout.MaxWidth(drawableWidth / 3f)) || keyboardEnter)
                {
                    // Try to parse a Vector3 scale from the input string
                    Vector3 resizeVector3;
                    if (StringHelper.TryParseScale(resizeString, out resizeVector3))
                    {
                        // None of the size components can be zero
                        if (resizeVector3.x != 0 && resizeVector3.y != 0 && resizeVector3.z != 0)
                        {
                            // Rescale all the brushes so that the local bounds is the same size as the resize vector
                            Undo.RecordObjects(targets, "Resize Polygons");
                            PrimitiveBrush[] brushes = BrushTargets.Cast<PrimitiveBrush>().ToArray();
                            foreach (PrimitiveBrush brush in brushes)
                            {
                                BrushUtility.Resize(brush, resizeVector3);
                            }
                        }
                    }
                }

                GUILayout.EndHorizontal();
            }

			using (new NamedVerticalScope("Rotation"))
			{

				GUILayout.Label("Align up direction", EditorStyles.boldLabel);
				GUILayout.BeginHorizontal();
				if(GUILayout.Button("X"))
				{
					AlignUpToAxis(new Vector3(1, 0, 0), false);
				}
				if(GUILayout.Button("Y"))
				{
					AlignUpToAxis(new Vector3(0, 1, 0), false);
				}
				if(GUILayout.Button("Z"))
				{
					AlignUpToAxis(new Vector3(0, 0, 1), false);
				}
				GUILayout.EndHorizontal();

				GUILayout.Label("Align up direction (keep positions)", EditorStyles.boldLabel);

				GUILayout.BeginHorizontal();
				if(GUILayout.Button("X"))
				{
					AlignUpToAxis(new Vector3(1, 0, 0), true);
				}
				if(GUILayout.Button("Y"))
				{
					AlignUpToAxis(new Vector3(0, 1, 0), true);
				}
				if(GUILayout.Button("Z"))
				{
					AlignUpToAxis(new Vector3(0, 0, 1), true);
				}
				GUILayout.EndHorizontal();
			}

            using (new NamedVerticalScope("Misc"))
            {
                // Import Row
                GUILayout.BeginHorizontal();
                sourceMesh = EditorGUILayout.ObjectField(sourceMesh, typeof(Mesh), false) as Mesh;

                if (GUILayout.Button("Import", GUILayout.MaxWidth(drawableWidth / 3f)))
                {
                    if (sourceMesh != null)
                    {
                        Undo.RecordObjects(targets, "Import Polygons From Mesh");

                        Polygon[] polygons = BrushFactory.GeneratePolygonsFromMesh(sourceMesh).ToArray();
                        bool convex = GeometryHelper.IsBrushConvex(polygons);
                        if (!convex)
                        {
                            Debug.LogError("Concavities detected in imported mesh. This may result in issues during CSG, please change the source geometry so that it is convex");
                        }
                        foreach (var thisBrush in targets)
                        {
                            ((PrimitiveBrush)thisBrush).SetPolygons(polygons, true);
                        }
                    }
                }

                GUILayout.EndHorizontal();

                // Shell Row
                GUILayout.BeginHorizontal();

                if (shellDistance == 0)
                {
                    shellDistance = CurrentSettings.PositionSnapDistance;
                }

                shellDistance = EditorGUILayout.FloatField("Distance", shellDistance);

                if (GUILayout.Button("Shell", GUILayout.MaxWidth(drawableWidth / 3f)))
                {
                    List<GameObject> newSelection = new List<GameObject>();
                    foreach (var thisBrush in targets)
                    {
                        GameObject newObject = ((PrimitiveBrush)thisBrush).Duplicate();
                        Polygon[] polygons = newObject.GetComponent<PrimitiveBrush>().GetPolygons();
                        VertexUtility.DisplacePolygons(polygons, -shellDistance);
                        Bounds newBounds = newObject.GetComponent<PrimitiveBrush>().GetBounds();
                        // Verify the new geometry
                        if (GeometryHelper.IsBrushConvex(polygons)
                            && newBounds.GetSmallestExtent() > 0)
                        {
                            Undo.RegisterCreatedObjectUndo(newObject, "Shell");
                            newSelection.Add(newObject);
                        }
                        else
                        {
                            // Produced a concave brush, delete it and pretend nothing happened
                            GameObject.DestroyImmediate(newObject);
                            Debug.LogWarning("Could not shell " + thisBrush.name + " as shelled geometry would not be valid. Try lowering the shell distance and attempt Shell again.");
                        }
                    }

                    if (newSelection.Count > 0)
                    {
                        Selection.objects = newSelection.ToArray();
                    }
                }

                GUILayout.EndHorizontal();

                // Split Intersecting Row
                if (GUILayout.Button("Split Intersecting Brushes"))
                {
                    // Chop up the intersecting brushes by the brush planes, ideally into as few new brushes as possible

                    PrimitiveBrush[] brushes = BrushTargets.Cast<PrimitiveBrush>().ToArray();

                    BrushUtility.SplitIntersecting(brushes);
                }


                //			BrushOrder brushOrder = BrushTarget.GetBrushOrder();
                //			string positionString = string.Join(",", brushOrder.Position.Select(item => item.ToString()).ToArray());
                //            GUILayout.Label(positionString, EditorStyles.boldLabel);

                //List<BrushCache> intersections = ((PrimitiveBrush)BrushTarget).BrushCache.IntersectingVisualBrushCaches;
                //GUILayout.Label("Intersecting brushes " + intersections.Count, EditorStyles.boldLabel);

                //for (int i = 0; i < intersections.Count; i++)
                //{
                //    GUILayout.Label(intersections[i].Mode.ToString(), EditorStyles.boldLabel);
                //}
            }

			base.OnInspectorGUI();
        }

        /// <summary>
        /// Rotates the selected brushes so that their transform.up matches the specified axis
        /// </summary>
        /// <param name="newUpAxis">Axis to match transform.up to</param>
        /// <param name="counterAlignment">If specified, when the brush is rotated the vertices will be counter rotated so they remain in their old positions and orientations</param>
		void AlignUpToAxis(Vector3 newUpAxis, bool counterAlignment)
		{
			PrimitiveBrush[] brushes = BrushTargets.Cast<PrimitiveBrush>().ToArray();
			foreach (PrimitiveBrush brush in brushes)
			{
                Vector3 perpendicularDirection = Vector3.up;
                if(Mathf.Abs(Vector3.Dot(perpendicularDirection, newUpAxis)) > 0.9f)
                {
                    perpendicularDirection = Vector3.back;
                }
                
                // Calculate the new rotation for the brush so that it's new transform.up is at the axis. Note the parameter order
                Quaternion rotation = Quaternion.LookRotation(-perpendicularDirection, newUpAxis);
                Quaternion inverseRotation = Quaternion.Inverse(rotation * Quaternion.Inverse(brush.transform.localRotation));

                //Debug.Log("Rotating from " + brush.transform.up + " to " + axis + ". Produces " + rotation.eulerAngles);

                // Apply the rotation
                brush.transform.localRotation = rotation;

                // If they want to realign the brush but keep original world positions
                if (counterAlignment)
				{
					Polygon[] polygons = brush.GetPolygons();

					for (int polygonIndex = 0; polygonIndex < polygons.Length; polygonIndex++) 
					{
						Vertex[] vertices = polygons[polygonIndex].Vertices;

						// Rotate positions and vertices so they remain in their original place
						for (int vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++) 
						{
							vertices[vertexIndex].Position = inverseRotation * vertices[vertexIndex].Position;
							vertices[vertexIndex].Normal = inverseRotation * vertices[vertexIndex].Normal;
						}
					}

					// Polygon vertices have changed, so recalculate the planes
					for (int polygonIndex = 0; polygonIndex < polygons.Length; polygonIndex++) 
					{
						polygons[polygonIndex].CalculatePlane();
					}
				}
			}
		}
    }
}