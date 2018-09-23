#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Sabresaurus.SabreCSG
{
    public class PaintEditor : Tool, IVertexColorEditable
    {
        enum Mode { PaintRGB, PaintAlpha };

        Mode currentMode = Mode.PaintRGB;

//        Vector3 downPoint; // The 3D point the mouse was over at the start of the mouse click
        Vector3 hoverPointWorld; // The 3D point the mouse is hovering over

        // Main UI rectangle for this tool's UI
        readonly Rect toolbarRect = new Rect(0, 40, 230, 146);

        VertexColorWindow vertexColorWindow = null;


        float radius = 1f;

        // RGB Color Mode
        Color rgbColor = Color.white;
        float rgbStrength = 0.06f;


        // Alpha Mode
        float alphaStrength = 0.3f;
        float alphaColor = 1f;

        // Common
        bool restrictToPoly = false;

        Polygon initialPolygon = null; // The polygon that the mouse down event occurred on
        Polygon activePolygon = null; // The polygon currently overriding the grid plane
        //PrimitiveBrush activeBrush = null; // The polygon currently overriding the grid plane

        // Defer expensive operations from drag time to mouse up
        List<Brush> brushesBeingEdited = new List<Brush>(); 

        public override Rect ToolbarRect
        {
            get
            {
                return new Rect(toolbarRect);
            }
        }

        public override void ResetTool()
        {
        }

        public override void OnSceneGUI(SceneView sceneView, Event e)
        {
            base.OnSceneGUI(sceneView, e); // Allow the base logic to calculate first

            if(e.button == 0
                && !EditorHelper.IsMousePositionInInvalidRects(e.mousePosition)
                && !CameraPanInProgress)
            {
                if (e.type == EventType.MouseDown) 
                {
                    OnMouseDown(sceneView, e);
                }
                else if (e.type == EventType.MouseDrag) 
                {
                    OnMouseDrag(sceneView, e);
                }
                else if (e.type == EventType.MouseMove) 
                {
                    OnMouseMove(sceneView, e);
                }
                // If you mouse up on a different scene view to the one you started on it's surpressed as Ignore, so
                // make sure to check the real type
                else if (e.type == EventType.MouseUp || e.rawType == EventType.MouseUp)
                {
                    OnMouseUp(sceneView, e);
                }
            }

            if (e.type == EventType.KeyDown || e.type == EventType.KeyUp)
            {
                OnKeyAction(sceneView, e);
            }

            if (e.type == EventType.Layout || e.type == EventType.Repaint)
            {
                OnRepaint(sceneView, e);
            }
        }

        void CalculateHitBuiltPolygon(Vector2 currentPosition)
        {
            activePolygon = null;

            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

            Polygon polygon = csgModel.RaycastBuiltPolygons(ray);

            if(polygon != null)
            {
                float rayDistance;

                if (polygon.Plane.Raycast(ray, out rayDistance))
                {
                    hoverPointWorld = ray.GetPoint(rayDistance);

                    PolygonRaycastHit hit = new PolygonRaycastHit()
                    {
                        Point = hoverPointWorld,
                        Normal = polygon.Plane.normal,
                        Distance = rayDistance,
                        GameObject = null,
                        Polygon = polygon,
                    };

                    activePolygon = hit.Polygon;
                    hoverPointWorld = hit.Point;
                }
            }
        }

        void OnMouseDown(SceneView sceneView, Event e)
        {
            CalculateHitBuiltPolygon(e.mousePosition);

            initialPolygon = activePolygon;

            SceneView.RepaintAll();
        }



        public bool Coplanar(Transform brushTransform, Polygon localBrushPolygon, Polygon worldPolygon)
        {
            Plane worldPolygonPlane = worldPolygon.Plane;

            Plane localBrushPlane = localBrushPolygon.Plane;
            Vector3 pointOnPlane = -localBrushPlane.normal * localBrushPlane.distance;
            Vector3 pointOnPlaneWorld = brushTransform.TransformPoint(pointOnPlane);
            Plane worldBrushPlane = new Plane(brushTransform.TransformDirection(localBrushPlane.normal), pointOnPlaneWorld);

            if(MathHelper.PlaneEqualsLooser(worldPolygonPlane, worldBrushPlane))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        void OnMouseDrag(SceneView sceneView, Event e)
        {
            CalculateHitBuiltPolygon(e.mousePosition);

            if(activePolygon != null)
            {
                if(initialPolygon == null)
                {
                    // They may have started the drag over empty space, so fill in the initial polygon with the first valid one
                    initialPolygon = activePolygon;
                }
                List<Brush> brushes = csgModel.GetBrushes();
                foreach (Brush brush in brushes)
                {
                    bool anyChanged = false;
                    foreach (Polygon brushPolygon in brush.GetPolygons())
                    {
                        if(restrictToPoly == false || Coplanar(brush.transform, brushPolygon, initialPolygon))
                        {
                            anyChanged |= ChangePolygonColor(brush, brushPolygon);
                        }
                    }    

                    if(anyChanged)
                    {
                        if(!brushesBeingEdited.Contains(brush))
                            brushesBeingEdited.Add(brush);                        
                    }
                }

            }
            SceneView.RepaintAll();
        }

        private bool ChangePolygonColor(Brush brush, Polygon polygon)
        {
            bool anyChanged = false;

            Vector3 hoverPointLocal = brush.transform.InverseTransformPoint(hoverPointWorld);

            Undo.RecordObject(brush, "Paint");
            csgModel.UndoRecordContext("Paint");

            for (int j = 0; j < polygon.Vertices.Length; j++) 
            {
                float squareDistance = (polygon.Vertices[j].Position - hoverPointLocal).sqrMagnitude;

                if(squareDistance <= (radius * radius))
                {
                    float distance = Mathf.Sqrt(squareDistance);
                    polygon.Vertices[j].Color = PaintColor(polygon.Vertices[j].Color, distance / radius);
                    anyChanged = true;
                }
            }

            PolygonEntry entry = csgModel.GetVisualPolygonEntry(polygon.UniqueIndex);
            if(entry != null)
            {
                if(entry.BuiltMesh != null)
                {
                    Undo.RecordObject(entry.BuiltMesh, "Change Vertex Color");

                    Vector3[] meshVertices = entry.BuiltMesh.vertices;
                    Color[] meshColors = entry.BuiltMesh.colors;
                    Color[] colors = entry.Colors;

                    for (int vertexIndex = 0; vertexIndex < entry.Positions.Length; vertexIndex++) 
                    {
                        float squareDistance = (meshVertices[entry.BuiltVertexOffset + vertexIndex] - hoverPointWorld).sqrMagnitude;

                        if(squareDistance <= (radius * radius))
                        {                        
                            float distance = Mathf.Sqrt(squareDistance);
                            colors[vertexIndex] = PaintColor(colors[vertexIndex], distance / radius);
                            meshColors[entry.BuiltVertexOffset + vertexIndex] = PaintColor(meshColors[entry.BuiltVertexOffset + vertexIndex], distance / radius);
                            anyChanged = true;
                        }
                    }

                    if(anyChanged)
                    {
                        entry.Colors = colors;
                        entry.BuiltMesh.colors = meshColors;

                        EditorHelper.SetDirty(entry.BuiltMesh);
                    }
                }
            }

            return anyChanged;
        }

        Color PaintColor(Color sourceColor, float paintAmount)
        {
            if(currentMode == Mode.PaintRGB)
            {
                Color newColor = Color.Lerp(sourceColor, rgbColor, rgbStrength * paintAmount);
                newColor.a = sourceColor.a;
                return newColor;
            }
            else // Paint Alpha
            {
                Color newColor = sourceColor;
                newColor.a = Mathf.Lerp(sourceColor.a, alphaColor, alphaStrength * paintAmount);
                return newColor;
            }
        }

        void OnMouseMove(SceneView sceneView, Event e)
        {
            CalculateHitBuiltPolygon(e.mousePosition);

            SceneView.RepaintAll();
        }

        void OnMouseUp(SceneView sceneView, Event e)
        {           
            SceneView.RepaintAll();

            // Deferred expensive operation
            foreach (var brush in brushesBeingEdited)
            {
                brush.RecachePolygons(false);
            }

            brushesBeingEdited.Clear();
        }

        void OnKeyAction(SceneView sceneView, Event e)
        {
//            if (KeyMappings.EventsMatch(e, Event.KeyboardEvent(KeyMappings.Instance.CancelCurrentOperation)))
//            {
//                if (e.type == EventType.KeyDown)
//                {
//                    if(drawMode != DrawMode.None || hitPoints.Count > 0)
//                    {
//                        // Drawing is in progress so cancel it
//                        ResetTool();
//                    }
//                    else
//                    {
//                        // No draw in progress, so user wants to cancel out of draw mode
//                        csgModel.ExitOverrideMode();
//                    }
//                }
//                e.Use();
//            }
        }

        void OnRepaint(SceneView sceneView, Event e)
        {
            if(vertexColorWindow != null)
            {
                vertexColorWindow.Repaint();
            }

            if(activePolygon != null && activePolygon.Vertices.Length >= 3)
            {
                Camera sceneViewCamera = sceneView.camera;

                SabreCSGResources.GetVertexMaterial().SetPass (0);
                GL.PushMatrix();
                GL.LoadPixelMatrix();

                GL.Begin(GL.QUADS);

                GL.Color(Color.white);

                Vector3 target = sceneViewCamera.WorldToScreenPoint(hoverPointWorld);

                if(target.z > 0)
                {
                    // Make it pixel perfect
                    target = MathHelper.RoundVector3(target);
                    SabreGraphics.DrawBillboardQuad(target, 8, 8);
                }

                GL.End();
                GL.PopMatrix();

                Handles.DrawWireArc(hoverPointWorld, activePolygon.Plane.normal, activePolygon.GetTangent(), 360f, radius);
            }
        }

        public override void OnToolbarGUI(int windowID)
        {
            GUISkin inspectorSkin = SabreGUILayout.GetInspectorSkin();

//            GUILayout.Label("Vertex", SabreGUILayout.GetTitleStyle());

            if(currentMode == Mode.PaintRGB)
            {
                if(SabreGUILayout.ColorButtonLabel(rgbColor, "Color", inspectorSkin.button))
                {
                    vertexColorWindow = VertexColorWindow.CreateAndShow(csgModel, this);
                }
            }
            else if(currentMode == Mode.PaintAlpha)
            {
                alphaColor = EditorGUILayout.Slider("Alpha Color", alphaColor, 0f, 1f);
                alphaColor = GUILayout.HorizontalSlider(alphaColor, 0f, 1f);
            }

            restrictToPoly = SabreGUILayout.Toggle(restrictToPoly, "Restrict To Poly");
            currentMode = SabreGUILayout.DrawEnumGrid(currentMode);

            GUI.skin = inspectorSkin;
            radius = EditorGUILayout.Slider("Radius", radius, 0.5f, 5f);
            radius = GUILayout.HorizontalSlider(radius, 0.5f, 5f);


            if(currentMode == Mode.PaintRGB)
            {
                rgbStrength = EditorGUILayout.Slider("RGB Strength", rgbStrength, 0f, 1f);
                rgbStrength = GUILayout.HorizontalSlider(rgbStrength, 0f, 1f);
            }
            else if(currentMode == Mode.PaintAlpha)
            {
                alphaStrength = EditorGUILayout.Slider("Alpha Strength", alphaStrength, 0f, 1f);
                alphaStrength = GUILayout.HorizontalSlider(alphaStrength, 0f, 1f);
            }
        }


        public override void Deactivated ()
        {

        }

        public override bool PreventBrushSelection 
        {
            get 
            {
                // Some special logic for clicking brushes
                return true;
            }
        }

        public override bool BrushesHandleDrawing
        {
            get
            {
                return false;
            }
        }

        public void SetSelectionColor(Color color)
        {
            this.rgbColor = color;
            SceneView.RepaintAll();
        }

        public Color GetColor()
        {
            return rgbColor;
        }
    }
}
#endif