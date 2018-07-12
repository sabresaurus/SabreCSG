#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
    public class DrawEditor : Tool
    {
        /// <summary>
        /// The polygon currently overriding the grid plane.
        /// </summary>
        private Polygon activePolygon = null;

        /// <summary>
        /// The polygon currently overriding the grid plane.
        /// </summary>
        private PrimitiveBrush activeBrush = null;

        /// <summary>
        /// The 3D point the mouse was over at the start of the mouse click.
        /// </summary>
        private Vector3 downPoint;

        /// <summary>
        /// The 3D point the mouse is hovering over.
        /// </summary>
        private Vector3 hoverPoint;

        /// <summary>
        /// The types of drawing operations.
        /// </summary>
        private enum DrawMode
        {
            /// <summary>
            /// Drawing is not active or has been cancelled.
            /// </summary>
            None,

            /// <summary>
            /// They've started drawing but we're not sure if it's a rectangle or point sequence.
            /// </summary>
            Ambiguous,

            /// <summary>
            /// They're drawing a rectangle through a click and drag.
            /// </summary>
            RectangleBase,

            /// <summary>
            /// They're defining a polygon by clicking a sequence of points.
            /// </summary>
            PolygonBase
        };

        /// <summary>
        /// The current draw mode.
        /// </summary>
        private DrawMode drawMode = DrawMode.None;

        /// <summary>
        /// The CSG mode of the brush currently being drawn.
        /// </summary>
        private CSGMode csgMode = CSGMode.Add;

        /// <summary>
        /// In 3D views after drawing the prism base, the height maps to the mouse.
        /// </summary>
        private bool selectingHeight = false;

        /// <summary>
        /// The height (or depth) of the prism being created.
        /// </summary>
        private float prismHeight = 0;

        /// <summary>
        /// Used to preserve height changes that have been snapped.
        /// </summary>
        private float unroundedPrismHeight = 0;

        /// <summary>
        /// The 3D points that they have clicked, in rectangle mode this is just the start and end point.
        /// </summary>
        private List<Vector3> hitPoints = new List<Vector3>();

        /// <summary>
        /// Whether the user has been holding shift throughout the entire drawing process.
        /// </summary>
        private bool startedSubtract = false;

        /// <summary>
        /// Double clicks occur in the mouse down event, so we need to then ignore next mouse up.
        /// </summary>
        private bool ignoreNextMouseUp = false;

        private bool Is3DView
        {
            get
            {
                if (!Camera.current.orthographic
                    || EditorHelper.GetSceneViewCamera(Camera.current) == EditorHelper.SceneViewCamera.Other)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public override void ResetTool()
        {
            // Set the selection to the last selected brush (usually the last drawn brush)
            if (CSGModel && CSGModel.LastSelectedBrush != null)
                Selection.activeGameObject = CSGModel.LastSelectedBrush.gameObject;
            else
                Selection.activeGameObject = null;
            drawMode = DrawMode.None;
            hitPoints.Clear();
            activePolygon = null;
            selectingHeight = false;
            prismHeight = 0;
            unroundedPrismHeight = 0;
        }

        public override void OnSceneGUI(SceneView sceneView, Event e)
        {
            base.OnSceneGUI(sceneView, e); // Allow the base logic to calculate first

            if (e.button == 0
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

        private Polygon GetActivePolygon()
        {
            if (Camera.current.orthographic && EditorHelper.GetSceneViewCamera(Camera.current) != EditorHelper.SceneViewCamera.Other)
            {
                // Axis aligned iso view
                return null;
            }
            else
            {
                return activePolygon;
            }
        }

        private Plane GetActivePlane()
        {
            if (Camera.current.orthographic && EditorHelper.GetSceneViewCamera(Camera.current) != EditorHelper.SceneViewCamera.Other)
            {
                // Axis aligned iso view
                return new Plane() { normal = -Camera.current.transform.forward, distance = 0 };
            }
            else
            {
                if (activePolygon != null)
                {
                    // A polygon plane is overriding the grid plane, so return that
                    return activePolygon.Plane;
                }
                else
                {
                    // No plane override, so use ground plane
                    return new Plane() { normal = Vector3.up, distance = 0 };
                }
            }
        }

        private PolygonRaycastHit? CalculateHitPolygon(Vector2 currentPosition)
        {
            if (EditorHelper.GetSceneViewCamera(SceneView.lastActiveSceneView) == EditorHelper.SceneViewCamera.Other)
            {
                // Convert the mouse position into a ray to intersect with a plane in the world
                Ray currentRay = Camera.current.ScreenPointToRay(EditorHelper.ConvertMousePointPosition(currentPosition));
                List<PolygonRaycastHit> hits = csgModel.RaycastBrushesAll(currentRay, false);
                if (hits.Count > 0)
                {
                    return hits[0];
                }
                else
                {
                    return null;
                }
            }
            else
            {
                // If the scene view is axis-aligned iso then we don't raycast polygons
                return null;
            }
        }

        private bool IsPrismBaseValid(List<Vector3> points)
        {
            Vector2 hitPointsSize = CalculateHitPointsSize(points);

            if (hitPointsSize.x > 0 && hitPointsSize.y > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private Vector2 CalculateHitPointsSize(List<Vector3> points)
        {
            Vector3 normal = GetActivePlane().normal;
            Vector3 tangent = Vector3.zero;

            for (int i = 0; i < points.Count; i++)
            {
                Vector3 delta = points[(i + 1) % points.Count] - points[i];
                if (delta.magnitude > 0.01f)
                {
                    tangent = delta.normalized;
                    break;
                }
            }

            if (tangent == Vector3.zero)
            {
                if (Vector3.Dot(normal.Abs(), Vector3.up) > 0.9f)
                {
                    tangent = Vector3.Cross(normal, Vector3.forward).normalized;
                }
                else
                {
                    tangent = Vector3.Cross(normal, Vector3.up).normalized;
                }
            }

            Vector3 binormal = Vector3.Cross(normal, tangent);

            float minX = Vector3.Dot(tangent, points[0]);
            float maxX = minX;

            float minY = Vector3.Dot(binormal, points[0]);
            float maxY = minY;

            for (int i = 1; i < points.Count; i++)
            {
                float testX = Vector3.Dot(tangent, points[i]);
                float testY = Vector3.Dot(binormal, points[i]);

                minX = Mathf.Min(minX, testX);
                maxX = Mathf.Max(maxX, testX);

                minY = Mathf.Min(minY, testY);
                maxY = Mathf.Max(maxY, testY);
            }

            return new Vector2(maxX - minX, maxY - minY);
        }

        private Vector3? GetHitPoint(Vector2 currentPosition)
        {
            // Conver the mouse position into a ray to intersect with a plane in the world
            Ray currentRay = Camera.current.ScreenPointToRay(EditorHelper.ConvertMousePointPosition(currentPosition));

            // Find the currently active plane
            Plane plane = GetActivePlane();

            // If we hit the plane, return the hit point, otherwise return null
            float distance;
            if (plane.Raycast(currentRay, out distance))
            {
                Vector3 hitPoint = currentRay.GetPoint(distance);

                if (CurrentSettings.PositionSnappingEnabled)
                {
                    Polygon activePolygon = GetActivePolygon();

                    if (activePolygon != null)
                    {
                        // Rotation to go from the polygon's plane to XY plane (for sorting)
                        Quaternion cancellingRotation = Quaternion.Inverse(Quaternion.LookRotation(plane.normal));
                        Quaternion restoringRotation = Quaternion.LookRotation(plane.normal);
                        hitPoint -= activePolygon.GetCenterPoint();
                        Vector3 localHitPoint = cancellingRotation * hitPoint;
                        // Round in local space
                        localHitPoint = MathHelper.RoundVector3(localHitPoint, CurrentSettings.PositionSnapDistance);
                        // Convert back to correct space
                        hitPoint = restoringRotation * localHitPoint;
                        hitPoint += activePolygon.GetCenterPoint();
                    }
                    else
                    {
                        hitPoint = MathHelper.RoundVector3(hitPoint, CurrentSettings.PositionSnapDistance);
                    }
                }
                return hitPoint;
            }
            else
            {
                return null;
            }
        }

        private void UpdateCSGMode(Event e)
        {
            // if the user is drawing against a polygon:
            if (activePolygon != null)
            {
                // drawing into the polygon makes us subtract.
                csgMode = (prismHeight < 0.0f) ? CSGMode.Subtract : CSGMode.Add;
                // the user can hold shift to invert the logic.
                if (e.shift)
                {
                    csgMode = (csgMode == CSGMode.Add) ? CSGMode.Subtract : CSGMode.Add;
                }
            }

            // if the user is drawing in the air:
            else
            {
                // the user can hold shift to invert the logic.
                csgMode = e.shift ? CSGMode.Subtract : CSGMode.Add;
            }
        }

        private void OnMouseDown(SceneView sceneView, Event e)
        {
            if (drawMode == DrawMode.None || drawMode == DrawMode.Ambiguous)
            {
                startedSubtract = e.shift;
            }

            UpdateCSGMode(e);

            if (selectingHeight)
                return;

            if (e.clickCount == 2)
            {
                // Double click, so finish the polygon
                if (drawMode == DrawMode.PolygonBase)
                {
                    if (Is3DView && !startedSubtract)
                    {
                        selectingHeight = true;
                        prismHeight = 0;
                        ignoreNextMouseUp = true;
                        SceneView.RepaintAll();
                    }
                    else
                    {
                        CreateBrush(hitPoints);

                        ResetTool();

                        sceneView.Repaint();
                    }
                }
            }
            else
            {
                if (drawMode == DrawMode.None)
                {
                    PolygonRaycastHit? hit = CalculateHitPolygon(e.mousePosition);
                    if (hit.HasValue)
                    {
                        activePolygon = hit.Value.Polygon;
                        activeBrush = hit.Value.GameObject.GetComponent<PrimitiveBrush>();
                    }
                    else
                    {
                        activePolygon = null;
                        activeBrush = null;
                    }

                    Vector3? hitPoint = GetHitPoint(e.mousePosition);
                    if (hitPoint.HasValue)
                    {
                        downPoint = hitPoint.Value;
                        hoverPoint = downPoint;
                    }

                    hitPoints.Clear();

                    if (hitPoint.HasValue)
                    {
                        drawMode = DrawMode.Ambiguous;
                    }
                }
                else
                {
                    Vector3? hitPoint = GetHitPoint(e.mousePosition);
                    if (hitPoint.HasValue)
                    {
                        downPoint = hitPoint.Value;
                        hoverPoint = downPoint;
                    }
                }
            }
        }

        private void OnMouseDrag(SceneView sceneView, Event e)
        {
            // if the user stopped holding shift we cancel the automatic height.
            if (!e.shift)
                startedSubtract = false;

            UpdateCSGMode(e);
            Vector3? hitPoint = GetHitPoint(e.mousePosition);

            if (hitPoint.HasValue)
            {
                if (drawMode == DrawMode.PolygonBase && !selectingHeight)
                {
                    hoverPoint = hitPoint.Value;
                    SceneView.RepaintAll();
                }

                if (drawMode == DrawMode.Ambiguous)
                {
                    hitPoints.Clear();
                    drawMode = DrawMode.RectangleBase;
                    hitPoints.Add(downPoint);
                }

                if (drawMode == DrawMode.Ambiguous || (drawMode == DrawMode.RectangleBase && !selectingHeight))
                {
                    if (hitPoints.Count < 2)
                    {
                        hitPoints.Add(hitPoint.Value);
                    }
                    else
                    {
                        hitPoints[1] = hitPoint.Value;
                    }

                    // We have something to draw so make sure the sceneview repaints
                    sceneView.Repaint();

                    drawMode = DrawMode.RectangleBase;
                }
            }
        }

        private void OnMouseMoveSelectHeight(SceneView sceneView, Event e)
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
            Vector3 direction = GetActivePlane().normal;

            Vector3 lineStart = hitPoints[0];
            Vector3 lineEnd = hitPoints[0] + direction;

            Vector3 lastPositionWorld = MathHelper.ClosestPointOnLine(lastRay, lineStart, lineEnd);
            Vector3 currentPositionWorld = MathHelper.ClosestPointOnLine(currentRay, lineStart, lineEnd);

            Vector3 deltaWorld = (currentPositionWorld - lastPositionWorld);

            float deltaScale = Vector3.Dot(direction, deltaWorld);
            float snapDistance = CurrentSettings.PositionSnapDistance;

            if (CurrentSettings.PositionSnappingEnabled)
            {
                deltaScale += unroundedPrismHeight;
                float roundedDeltaScale = MathHelper.RoundFloat(deltaScale, snapDistance);

                unroundedPrismHeight = deltaScale - roundedDeltaScale;
                deltaScale = roundedDeltaScale;
            }

            prismHeight += deltaScale;

            SceneView.RepaintAll();
        }

        private void OnMouseMove(SceneView sceneView, Event e)
        {
            // if the user stopped holding shift we cancel the automatic height.
            if (!e.shift)
                startedSubtract = false;

            UpdateCSGMode(e);

            if (selectingHeight)
            {
                // Mouse movement while selecting depth controls prism height
                OnMouseMoveSelectHeight(sceneView, e);
            }
            else
            {
                // Not currently drawing, so update the active polygon to whatever they are currently hovering over
                if (drawMode == DrawMode.None)
                {
                    PolygonRaycastHit? hit = CalculateHitPolygon(e.mousePosition);
                    if (hit.HasValue)
                    {
                        activePolygon = hit.Value.Polygon;
                        activeBrush = hit.Value.GameObject.GetComponent<PrimitiveBrush>();
                    }
                    else
                    {
                        activePolygon = null;
                        activeBrush = null;
                    }
                }

                // Find the snapped hover point based on any active polygon or the grid
                Vector3? hitPoint = GetHitPoint(e.mousePosition);
                if (hitPoint.HasValue)
                {
                    hoverPoint = hitPoint.Value;
                    SceneView.RepaintAll();
                }
            }
        }

        private void OnMouseUp(SceneView sceneView, Event e)
        {
            if (ignoreNextMouseUp)
            {
                // Double click just occurred, so don't process the up event
                ignoreNextMouseUp = false;
                return;
            }

            // if the user stopped holding shift we cancel the automatic height.
            if (!e.shift)
                startedSubtract = false;

            UpdateCSGMode(e);

            if (selectingHeight)
            {
                if (drawMode == DrawMode.RectangleBase)
                {
                    CreateBrush(GetRectanglePoints());
                }
                else
                {
                    CreateBrush(hitPoints);
                }
                selectingHeight = false;

                ResetTool();

                sceneView.Repaint();
            }
            else
            {
                if (drawMode == DrawMode.RectangleBase)
                {
                    if (Is3DView && !startedSubtract)
                    {
                        // Verify that it will form a valid prism by trying to create a polygon out the base
                        if (IsPrismBaseValid(GetRectanglePoints()))
                        {
                            selectingHeight = true;
                            prismHeight = 0;
                        }
                        else
                        {
                            // Will not form a valid prism so cancel the draw
                            ResetTool();

                            sceneView.Repaint();
                        }
                    }
                    else
                    {
                        CreateBrush(GetRectanglePoints());

                        ResetTool();

                        sceneView.Repaint();
                    }
                }
                else if (drawMode == DrawMode.Ambiguous || drawMode == DrawMode.PolygonBase)
                {
                    Vector3? hitPoint = GetHitPoint(e.mousePosition);

                    if (hitPoint.HasValue)
                    {
                        drawMode = DrawMode.PolygonBase;

                        hitPoints.Add(hitPoint.Value);

                        if (hitPoints.Count > 2 && hitPoint.Value.EqualsWithEpsilon(hitPoints[0]))
                        {
                            if (Is3DView && !startedSubtract)
                            {
                                // Verify that it will form a valid prism by trying to create a polygon out the base
                                if (IsPrismBaseValid(hitPoints))
                                {
                                    selectingHeight = true;
                                    prismHeight = 0;
                                }
                                else
                                {
                                    // Will not form a valid prism so cancel the draw
                                    ResetTool();

                                    sceneView.Repaint();
                                }
                            }
                            else
                            {
                                CreateBrush(hitPoints);

                                ResetTool();

                                sceneView.Repaint();
                            }
                        }
                    }
                }
            }
        }

        private void CreateBrush(List<Vector3> positions)
        {
            Polygon sourcePolygon = PolygonFactory.ConstructPolygon(positions, true);

            // Early out if it wasn't possible to create the polygon
            if (sourcePolygon == null)
            {
                return;
            }

            if (activePolygon != null)
            {
                for (int i = 0; i < sourcePolygon.Vertices.Length; i++)
                {
                    Vector2 newUV = GeometryHelper.GetUVForPosition(activePolygon, sourcePolygon.Vertices[i].Position);
                    sourcePolygon.Vertices[i].UV = newUV;
                }
            }

            Vector3 planeNormal = GetActivePlane().normal;

            //			Debug.Log(Vector3.Dot(sourcePolygon.Plane.normal, planeNormal));

            // Flip the polygon if the winding order is wrong
            if (Vector3.Dot(sourcePolygon.Plane.normal, planeNormal) < 0)
            {
                sourcePolygon.Flip();

                // Need to flip the UVs across the U (X) direction
                for (int i = 0; i < sourcePolygon.Vertices.Length; i++)
                {
                    Vector2 uv = sourcePolygon.Vertices[i].UV;
                    uv.x = 1 - uv.x;
                    sourcePolygon.Vertices[i].UV = uv;
                }
            }

            float extrusionDistance = 1;
            Vector3 positionOffset = Vector3.zero;

            if (selectingHeight)
            {
                extrusionDistance = prismHeight;
            }
            else
            {
                if (activePolygon != null && activeBrush != null)
                {
                    extrusionDistance = activeBrush.CalculateExtentsInAxis(planeNormal);
                }
                else
                {
                    Brush lastSelectedBrush = csgModel.LastSelectedBrush;
                    if (lastSelectedBrush != null)
                    {
                        Bounds lastSelectedBrushBounds = lastSelectedBrush.GetBoundsTransformed();

                        for (int i = 0; i < 3; i++)
                        {
                            if (!planeNormal[i].EqualsWithEpsilon(0))
                            {
                                if (lastSelectedBrushBounds.size[i] != 0)
                                {
                                    extrusionDistance = lastSelectedBrushBounds.size[i];

                                    if (planeNormal[i] > 0)
                                    {
                                        positionOffset[i] = lastSelectedBrushBounds.center[i] - lastSelectedBrushBounds.extents[i];
                                    }
                                    else
                                    {
                                        positionOffset[i] = lastSelectedBrushBounds.center[i] + lastSelectedBrushBounds.extents[i];
                                    }
                                }
                            }
                        }
                    }
                }

                // Subtractions should go through
                if (csgMode == CSGMode.Subtract)
                {
                    sourcePolygon.Flip();
                }
            }

            Quaternion rotation;
            Polygon[] polygons;
            SurfaceUtility.ExtrudePolygon(sourcePolygon, extrusionDistance, out polygons, out rotation);

            GameObject newObject = csgModel.CreateCustomBrush(polygons);

            PrimitiveBrush newBrush = newObject.GetComponent<PrimitiveBrush>();

            newObject.transform.rotation = rotation;
            newObject.transform.position += positionOffset;

            if (activePolygon != null
                && activePolygon.Material != csgModel.GetDefaultMaterial())
            {
                for (int i = 0; i < polygons.Length; i++)
                {
                    polygons[i].Material = activePolygon.Material;
                }
            }
            // Finally give the new brush the other set of polygons
            newBrush.SetPolygons(polygons, true);

            newBrush.Mode = csgMode;

            newBrush.ResetPivot();

            // Use this brush as the basis for drawing the next brush
            csgModel.SetLastSelectedBrush(newBrush);

            Undo.RegisterCreatedObjectUndo(newObject, "Draw Brush");
        }

        private void OnKeyAction(SceneView sceneView, Event e)
        {
            if (KeyMappings.EventsMatch(e, Event.KeyboardEvent(KeyMappings.Instance.CancelCurrentOperation)))
            {
                if (e.type == EventType.KeyDown)
                {
                    if (drawMode != DrawMode.None || hitPoints.Count > 0)
                    {
                        // Drawing is in progress so cancel it
                        ResetTool();
                    }
                    else
                    {
                        // No draw in progress, so user wants to cancel out of draw mode
                        csgModel.ExitOverrideMode();
                    }
                }
                e.Use();
            }
            else if (KeyMappings.EventsMatch(e, Event.KeyboardEvent(KeyMappings.Instance.Back))
                || KeyMappings.EventsMatch(e, Event.KeyboardEvent(KeyMappings.Instance.Delete), false, true))
            {
                if (e.type == EventType.KeyDown)
                {
                    if (drawMode == DrawMode.PolygonBase)
                    {
                        if (hitPoints.Count > 1)
                        {
                            // Remove the last point
                            hitPoints.RemoveAt(hitPoints.Count - 1);
                        }
                        else
                        {
                            ResetTool();
                        }
                    }
                    else
                    {
                        ResetTool();
                    }
                }
                e.Use();
            }
        }

        private List<Vector3> GetRectanglePoints()
        {
            Plane plane = GetActivePlane();
            Vector3 planeNormal = plane.normal; //MathHelper.VectorAbs(plane.normal);

            Vector3 axis1;
            Vector3 axis2;

            if (Vector3.Dot(planeNormal.Abs(), Vector3.up) > 0.99f)
            {
                axis1 = Vector3.forward;
                axis2 = Vector3.right;
            }
            else
            {
                axis1 = Vector3.up;
                axis2 = Vector3.Cross(Vector3.up, planeNormal).normalized;
            }

            Vector3 startPoint = hitPoints[0];
            Vector3 endPoint = hitPoints[1];

            List<Vector3> points = new List<Vector3>(4);

            Vector3 delta = endPoint - startPoint;

            Vector3 deltaAxis1 = Vector3.Dot(delta, axis1) * axis1;
            Vector3 deltaAxis2 = Vector3.Dot(delta, axis2) * axis2;

            points.Add(startPoint);
            points.Add(startPoint + deltaAxis1);
            points.Add(endPoint);
            points.Add(startPoint + deltaAxis2);

            return points;
        }

        private void OnRepaint(SceneView sceneView, Event e)
        {
            Camera sceneViewCamera = sceneView.camera;

            //			if(hoverPoint.HasValue)
            {
                // Draw the active polygon
                if (activePolygon != null)
                {
                    SabreCSGResources.GetSelectedBrushMaterial().SetPass(0);

                    SabreGraphics.DrawPolygons(new Color(1, 1, 0, 0.15f), new Color(1, 1, 0, 0.5f), activePolygon);
                }

                SabreCSGResources.GetVertexMaterial().SetPass(0);
                GL.PushMatrix();
                GL.LoadPixelMatrix();

                GL.Begin(GL.QUADS);

                if (activePolygon != null)
                {
                    GL.Color(Color.yellow);// new Color(0.75f,0.75f,0.75f,1));
                }
                else
                {
                    GL.Color(Color.white);
                }

                Vector3 target = sceneViewCamera.WorldToScreenPoint(hoverPoint);

                if (target.z > 0)
                {
                    // Make it pixel perfect
                    target = MathHelper.RoundVector3(target);
                    SabreGraphics.DrawBillboardQuad(target, 8, 8);
                }

                for (int i = 0; i < hitPoints.Count; i++)
                {
                    target = sceneViewCamera.WorldToScreenPoint(hitPoints[i]);
                    GL.Color(Color.blue);
                    if (target.z > 0)
                    {
                        // Make it pixel perfect
                        target = MathHelper.RoundVector3(target);
                        SabreGraphics.DrawBillboardQuad(target, 8, 8);
                    }
                }

                GL.End();
                GL.PopMatrix();
            }

            if (drawMode == DrawMode.RectangleBase || drawMode == DrawMode.PolygonBase)
            {
                List<Vector3> pointsToDraw = new List<Vector3>();
                if (drawMode == DrawMode.RectangleBase)
                {
                    pointsToDraw.AddRange(GetRectanglePoints());
                }
                else
                {
                    pointsToDraw.AddRange(hitPoints);
                    pointsToDraw.Insert(pointsToDraw.Count, hoverPoint);
                }

                SabreCSGResources.GetSelectedBrushMaterial().SetPass(0);

                //				GL.Begin(GL);
                //				GL.Color(new Color32(0, 181, 255, 100));
                //
                //				// Draw each point
                //				for (int i = 0; i < pointsToDraw.Count; i++)
                //				{
                //					GL.Vertex(pointsToDraw[i]);
                //				}
                //
                //				GL.End();

                // Marquee border
                GL.Begin(GL.LINES);

                if (csgMode == CSGMode.Add)
                {
                    GL.Color(Color.blue);
                }
                else if (csgMode == CSGMode.Subtract)
                {
                    GL.Color(Color.yellow);
                }

                // Draw lines between all the points
                for (int i = 0; i < pointsToDraw.Count; i++)
                {
                    // In polygon mode, if it's bigger than a line, draw the last edge in grey
                    if (i == pointsToDraw.Count - 1
                        && drawMode == DrawMode.PolygonBase
                        && pointsToDraw.Count > 2
                        && !selectingHeight)
                    {
                        GL.Color(Color.grey);
                    }
                    // Draw a line from one point to the next
                    GL.Vertex(pointsToDraw[i]);
                    GL.Vertex(pointsToDraw[(i + 1) % pointsToDraw.Count]);
                }

                if (selectingHeight)
                {
                    Vector3 offset = GetActivePlane().normal * prismHeight;

                    for (int i = 0; i < pointsToDraw.Count; i++)
                    {
                        // Draw a line from one point to the next
                        GL.Vertex(pointsToDraw[i]);
                        GL.Vertex(pointsToDraw[i] + offset);
                    }

                    GL.Color(Color.grey);
                    // Draw grid lines along the prism to indicate size
                    for (int heightLine = 1; heightLine < Mathf.Abs(prismHeight); heightLine++)
                    {
                        for (int i = 0; i < pointsToDraw.Count; i++)
                        {
                            Vector3 gridOffset = GetActivePlane().normal * heightLine * Mathf.Sign(prismHeight);
                            // Draw a line from one point to the next
                            GL.Vertex(pointsToDraw[i] + gridOffset);
                            GL.Vertex(pointsToDraw[(i + 1) % pointsToDraw.Count] + gridOffset);
                        }
                    }

                    GL.Color(Color.green);
                    for (int i = 0; i < pointsToDraw.Count; i++)
                    {
                        // Draw a line from one point to the next
                        GL.Vertex(pointsToDraw[i] + offset);
                        GL.Vertex(pointsToDraw[(i + 1) % pointsToDraw.Count] + offset);
                    }
                }

                GL.End();
            }

            //			Rect rectangle = new Rect(0, 50, 210, 130);
            //			GUIStyle toolbar = new GUIStyle(EditorStyles.toolbar);
            //			toolbar.normal.background = SabreCSGResources.ClearTexture;
            //			toolbar.fixedHeight = rectangle.height;
            //			GUILayout.Window(140010, rectangle, OnToolbarGUI, "",toolbar);
        }

        //		void OnToolbarGUI(int windowID)
        //		{
        //			raycastBrushes = EditorGUILayout.Toggle("Raycast Brushes", raycastBrushes);
        //		}

        public override void Deactivated()
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
    }
}

#endif