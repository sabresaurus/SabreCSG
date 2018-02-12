#if UNITY_EDITOR || RUNTIME_CSG
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
    /// <summary>
    /// The 2D Shape Editor Window.
    /// </summary>
    /// <remarks>Inspired by Unreal Editor 1 (1998). Created by Henry de Jongh for SabreCSG.</remarks>
    /// <seealso cref="UnityEditor.EditorWindow"/>
    public class ShapeEditorBrushWindow : EditorWindow
    {
        /// <summary>
        /// The type of segment.
        /// </summary>
        public enum SegmentType
        {
            Linear,
            Bezier
        }

        /// <summary>
        /// Any object that can be selected in the 2D Shape Editor.
        /// </summary>
        public interface ISelectable
        {
            /// <summary>
            /// The position of the object on the grid.
            /// </summary>
            Vector2Int position { get; set; }
        }

        /// <summary>
        /// A 2D Shape Editor Shape.
        /// </summary>
        [Serializable]
        public class Shape
        {
            /// <summary>
            /// The segments of the shape.
            /// </summary>
            [SerializeField]
            public List<Segment> segments = new List<Segment>() {
                new Segment(-8, -8),
                new Segment( 8, -8),
                new Segment( 8,  8),
                new Segment(-8,  8),
            };

            /// <summary>
            /// The center pivot of the shape.
            /// </summary>
            public Pivot pivot = new Pivot();

            /// <summary>
            /// Calculates the pivot position so that it's centered on the shape.
            /// </summary>
            public void CalculatePivotPosition()
            {
                Vector2Int center = new Vector2Int();
                foreach (Segment segment in segments)
                    center += segment.position;
                pivot.position = new Vector2Int(center.x / segments.Count, center.y / segments.Count);
            }
        }

        /// <summary>
        /// A 2D Shape Editor Segment.
        /// </summary>
        [Serializable]
        public class Segment : ISelectable
        {
            /// <summary>
            /// The position of the segment on the grid.
            /// </summary>
            [SerializeField]
            private Vector2Int _position;

            /// <summary>
            /// The position of the segment on the grid.
            /// </summary>
            public Vector2Int position
            {
                get { return _position; }
                set { _position = value; }
            }

            /// <summary>
            /// The segment type.
            /// </summary>
            [SerializeField]
            public SegmentType type = SegmentType.Linear;

            /// <summary>
            /// The first bezier pivot (see <see cref="SegmentType.Bezier"/>).
            /// </summary>
            [SerializeField]
            public Pivot bezierPivot1 = new Pivot();

            /// <summary>
            /// The second bezier pivot (see <see cref="SegmentType.Bezier"/>).
            /// </summary>
            [SerializeField]
            public Pivot bezierPivot2 = new Pivot();

            [SerializeField]
            public int bezierDetail = 3;

            /// <summary>
            /// Initializes a new instance of the <see cref="Segment"/> class.
            /// </summary>
            /// <param name="x">The x-coordinate on the grid.</param>
            /// <param name="y">The y-coordinate on the grid.</param>
            public Segment(int x, int y)
            {
                this.position = new Vector2Int(x, y);
            }
        }

        /// <summary>
        /// A 2D Shape Editor Pivot.
        /// </summary>
        [Serializable]
        public class Pivot : ISelectable
        {
            /// <summary>
            /// The position of the pivot on the grid.
            /// </summary>
            [SerializeField]
            private Vector2Int _position;

            /// <summary>
            /// The position of the pivot on the grid.
            /// </summary>
            public Vector2Int position
            {
                get { return _position; }
                set { _position = value; }
            }
        }

        /// <summary>
        /// The shapes in the project.
        /// </summary>
        [SerializeField]
        private List<Shape> shapes = new List<Shape>()
        {
            new Shape()
        };

        /// <summary>
        /// The global pivot in the project.
        /// </summary>
        [SerializeField]
        private Pivot globalPivot = new Pivot();

        //private class FakeUndoObject : UnityEngine.Object
        //{
        //    // todo
        //}

        /// <summary>
        /// The viewport scroll position.
        /// </summary>
        private Vector2 viewportScroll = new Vector2(100.0f, 100.0f);

        /// <summary>
        /// The grid scale.
        /// </summary>
        private int gridScale = 16;

        /// <summary>
        /// The currently selected objects.
        /// </summary>
        private List<ISelectable> selectedObjects = new List<ISelectable>();

        /// <summary>
        /// The line material.
        /// </summary>
        private Material lineMaterial;

        /// <summary>
        /// The currently selected segments.
        /// </summary>
        private IEnumerable<Segment> selectedSegments
        {
            get
            {
                return selectedObjects.OfType<Segment>();
            }
        }

        /// <summary>
        /// Gets a value indicating whether the global pivot is selected.
        /// </summary>
        /// <value><c>true</c> if the global pivot is selected; otherwise, <c>false</c>.</value>
        private bool isGlobalPivotSelected
        {
            get
            {
                return IsObjectSelected(globalPivot);
            }
        }

        /// <summary>
        /// Determines whether the specified object is selected.
        /// </summary>
        /// <param name="obj">The object to check.</param>
        /// <returns><c>true</c> if the specified object is selected; otherwise, <c>false</c>.</returns>
        private bool IsObjectSelected(ISelectable obj)
        {
            return selectedObjects.Contains(obj);
        }

        /// <summary>
        /// Clears the selection of <typeparamref name="T" /> elements.
        /// </summary>
        /// <typeparam name="T">The type of element to be deselected.</typeparam>
        private void ClearSelectionOf<T>() where T : ISelectable
        {
            selectedObjects.RemoveAll((obj) => obj.GetType() == typeof(T));
        }

        /// <summary>
        /// Gets the shape that the segment belongs to.
        /// </summary>
        /// <param name="segment">The segment to search for.</param>
        /// <returns>The shape that the segment belongs to.</returns>
        private Shape GetShapeOfSegment(Segment segment)
        {
            return shapes.Where((shape) => shape.segments.Contains(segment)).FirstOrDefault();
        }

        /// <summary>
        /// Gets the previous segment.
        /// </summary>
        /// <param name="segment">The segment to find the previous segment for.</param>
        /// <returns>The previous segment (wraps around).</returns>
        private Segment GetPreviousSegment(Segment segment)
        {
            Shape parent = GetShapeOfSegment(segment);
            int index = parent.segments.IndexOf(segment);
            if (index - 1 < 0)
                return parent.segments[parent.segments.Count - 1];
            return parent.segments[index - 1];
        }

        /// <summary>
        /// Gets the next segment.
        /// </summary>
        /// <param name="segment">The segment to find the next segment for.</param>
        /// <returns>The next segment (wraps around).</returns>
        private Segment GetNextSegment(Segment segment)
        {
            Shape parent = GetShapeOfSegment(segment);
            int index = parent.segments.IndexOf(segment);
            if (index + 1 > parent.segments.Count - 1)
                return parent.segments[0];
            return parent.segments[index + 1];
        }

        /// <summary>
        /// Gets the segment at grid position.
        /// </summary>
        /// <param name="x">The x-coordinate on the grid.</param>
        /// <param name="y">The y-coordinate on the grid.</param>
        /// <returns>The segment if found else null.</returns>
        private ISelectable GetObjectAtGridPosition(Vector2Int position)
        {
            // the global pivot point has the highest selection priority.
            if (globalPivot.position == position)
                return globalPivot;
            // the bezier segment pivots have medium-high priority.
            foreach (Shape shape in shapes)
            {
                Segment segment = shape.segments.FirstOrDefault((s) => s.type == SegmentType.Bezier && s.bezierPivot1.position == position);
                if (segment != null)
                    return segment.bezierPivot1;
                segment = shape.segments.FirstOrDefault((s) => s.type == SegmentType.Bezier && s.bezierPivot2.position == position);
                if (segment != null)
                    return segment.bezierPivot2;
            }
            // the shape pivots have medium-low priority.
            foreach (Shape shape in shapes)
            {
                if (shape.pivot.position == position)
                    return shape.pivot;
            }
            // the segments have the lowest priority.
            foreach (Shape shape in shapes)
            {
                Segment segment = shape.segments.FirstOrDefault((s) => s.position == position);
                if (segment != null)
                    return segment;
            }
            // nothing was found.
            return null;
        }

        [MenuItem("Window/SabreCSG/2D Shape Editor")]
        public static void Init()
        {
            // get existing open window or if none, make a new one:
            ShapeEditorBrushWindow window = GetWindow<ShapeEditorBrushWindow>();
            window.Show();
            window.titleContent = new GUIContent("Shape Editor");
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.MouseDrag)
            {
                // move object around with the left mouse button.
                if (Event.current.button == 0)
                {
                    Vector2Int grid = ScreenPointToGrid(new Vector3(Event.current.mousePosition.x, Event.current.mousePosition.y));
                    if (GetViewportRect().Contains(Event.current.mousePosition))
                    {
                        // move the global pivot.
                        if (isGlobalPivotSelected)
                        {
                            globalPivot.position = grid;
                            this.Repaint();
                        }
                        // move an entire shape by its pivot.
                        foreach (Shape shape in shapes)
                        {
                            if (IsObjectSelected(shape.pivot))
                            {
                                Vector2Int delta = grid - shape.pivot.position;
                                shape.pivot.position = grid;
                                foreach (Segment segment in shape.segments)
                                {
                                    // move segment.
                                    segment.position += delta;
                                    // move bezier pivot handles.
                                    segment.bezierPivot1.position += delta;
                                    segment.bezierPivot2.position += delta;
                                }
                                this.Repaint();
                            }
                            else
                            {
                                // if not dragging a shape by its pivot, center it.
                                // this is not quite the right place to do it but it works well enough.
                                shape.CalculatePivotPosition();
                                this.Repaint();
                            }

                            // move the bezier curves of a segment.
                            foreach (Segment segment in shape.segments)
                            {
                                if (segment.type != SegmentType.Bezier) continue;
                                if (IsObjectSelected(segment.bezierPivot1))
                                    segment.bezierPivot1.position = grid;
                                if (IsObjectSelected(segment.bezierPivot2))
                                    segment.bezierPivot2.position = grid;
                                this.Repaint();
                            }
                        }
                        // move a segment by its pivot.
                        foreach (Segment segment in selectedSegments)
                        {
                            segment.position = grid;
                            this.Repaint();
                        }
                    }
                }

                // pan the viewport around with the right mouse button.
                if (Event.current.button == 1)
                {
                    viewportScroll += Event.current.delta;
                    this.Repaint();
                }
            }

            if (Event.current.type == EventType.MouseDown)
            {
                if (Event.current.button == 0 && GetViewportRect().Contains(Event.current.mousePosition))
                {
                    // if the user is not holding CTRL or SHIFT we clear the selected objects.
                    if ((Event.current.modifiers & EventModifiers.Control) == 0 && (Event.current.modifiers & EventModifiers.Shift) == 0)
                        selectedObjects.Clear();

                    // try finding an object under the mouse cursor.
                    Vector2Int grid = ScreenPointToGrid(new Vector3(Event.current.mousePosition.x, Event.current.mousePosition.y));
                    ISelectable found = GetObjectAtGridPosition(grid);
                    if (found != null && !selectedObjects.Contains(found))
                        // select the object.
                        selectedObjects.Add(found);
                    this.Repaint();
                }
            }

            // implement keyboard shortcuts.
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.Delete)
                {
                    OnDelete();
                    Event.current.Use();
                }
                if (Event.current.keyCode == KeyCode.I)
                {
                    OnSegmentInsert();
                    Event.current.Use();
                }
                if (Event.current.keyCode == KeyCode.L)
                {
                    OnSegmentLinear();
                    Event.current.Use();
                }
                if (Event.current.keyCode == KeyCode.B)
                {
                    OnSegmentBezier();
                    Event.current.Use();
                }
                if (Event.current.keyCode == KeyCode.D)
                {
                    OnSegmentBezierDetail();
                    Event.current.Use();
                }
                if (Event.current.keyCode == KeyCode.Plus || Event.current.keyCode == KeyCode.KeypadPlus)
                {
                    OnZoomIn();
                    Event.current.Use();
                }
                if (Event.current.keyCode == KeyCode.Minus || Event.current.keyCode == KeyCode.KeypadMinus)
                {
                    OnZoomOut();
                    Event.current.Use();
                }
                if (Event.current.keyCode == KeyCode.V)
                {
                    OnFlipVertically();
                    Event.current.Use();
                }
                if (Event.current.keyCode == KeyCode.H)
                {
                    OnFlipHorizontally();
                    Event.current.Use();
                }
                if (Event.current.keyCode == KeyCode.R && Event.current.modifiers != 0)
                {
                    OnRotate90Left();
                    Event.current.Use();
                }
                else if (Event.current.keyCode == KeyCode.R)
                {
                    OnRotate90Right();
                    Event.current.Use();
                }
            }

            //if(Event.current.type == EventType.ValidateCommand && Event.current.commandName == "UndoRedoPerformed")
            //{
            //    // we don't want to use the unity editor undo function.
            //    Debug.Log(Event.current.commandName);
            //    Event.current.Use();
            //}


            if (Event.current.type == EventType.Repaint)
            {
                GUI.color = Color.white;
                GUI.DrawTexture(GetViewportRect(), EditorGUIUtility.whiteTexture);
                EditorGUIUtility.AddCursorRect(GetViewportRect(), MouseCursor.MoveArrow);

                GL.Begin(GL.QUADS);
                GL.LoadIdentity();

                if (lineMaterial == null)
                {
                    var shader = Shader.Find("SabreCSG/Line");
                    lineMaterial = new Material(shader);
                }

                lineMaterial.SetPass(0);

                DrawGrid();

                // draw all of the segments:
                foreach (Shape shape in shapes)
                {
                    foreach (Segment segment in shape.segments)
                    {
                        GL.Color(new Color(0.502f, 0.502f, 0.502f));
                        Segment next = GetNextSegment(segment);
                        float thickness = 1.0f;
                        bool isBezier1Selected = false;
                        bool isBezier2Selected = false;
                        if (segment.type == SegmentType.Bezier)
                        {
                            isBezier1Selected = IsObjectSelected(segment.bezierPivot1);
                            isBezier2Selected = IsObjectSelected(segment.bezierPivot2);
                        }
                        if (IsObjectSelected(segment) || isBezier1Selected || isBezier2Selected)
                        {
                            thickness = 3.0f;
                        }
                        if (segment.type == SegmentType.Linear)
                        {
                            Vector2 p1 = GridPointToScreen(segment.position);
                            Vector2 p2 = GridPointToScreen(next.position);
                            GlDrawLine(thickness, p1.x, p1.y, p2.x, p2.y);
                        }
                        if (segment.type == SegmentType.Bezier)
                        {
                            Vector2 p1 = GridPointToScreen(segment.position);
                            Vector2 p2 = GridPointToScreen(next.position);
                            Vector2 p3 = GridPointToScreen(segment.bezierPivot1.position);
                            Vector2 p4 = GridPointToScreen(segment.bezierPivot2.position);
                            GlDrawBezier(thickness, p1, p3, p4, p2, segment.bezierDetail + 1);

                            // draw the lines towards the pivots of the bezier curve.
                            GL.Color(Color.blue);
                            GlDrawLine(isBezier1Selected ? 2.0f : 1.0f, p1.x, p1.y, p3.x, p3.y);
                            GlDrawLine(isBezier2Selected ? 2.0f : 1.0f, p2.x, p2.y, p4.x, p4.y);
                        }
                    }
                }

                GL.End();

                // draw the handles on the corners of the segments.
                Handles.color = Color.white;
                foreach (Shape shape in shapes)
                {
                    foreach (Segment segment in shape.segments)
                    {
                        // draw pivots of the segments.
                        Vector2 segmentScreenPosition = GridPointToScreen(segment.position);
                        Handles.DrawSolidRectangleWithOutline(new Rect(segmentScreenPosition.x - 4.0f, segmentScreenPosition.y - 4.0f, 8.0f, 8.0f), Color.white, IsObjectSelected(segment) ? Color.red : Color.black);

                        // draw bezier pivots for bezier segments.
                        if (segment.type == SegmentType.Bezier)
                        {
                            segmentScreenPosition = GridPointToScreen(segment.bezierPivot1.position);
                            Handles.DrawSolidRectangleWithOutline(new Rect(segmentScreenPosition.x - 4.0f, segmentScreenPosition.y - 4.0f, 8.0f, 8.0f), Color.white, IsObjectSelected(segment.bezierPivot1) ? Color.red : Color.blue);
                            segmentScreenPosition = GridPointToScreen(segment.bezierPivot2.position);
                            Handles.DrawSolidRectangleWithOutline(new Rect(segmentScreenPosition.x - 4.0f, segmentScreenPosition.y - 4.0f, 8.0f, 8.0f), Color.white, IsObjectSelected(segment.bezierPivot2) ? Color.red : Color.blue);
                        }
                    }

                    // draw the shape pivot point.
                    Vector2 centerScreenPosition = GridPointToScreen(shape.pivot.position);
                    Handles.DrawSolidRectangleWithOutline(new Rect(centerScreenPosition.x - 4.0f, centerScreenPosition.y - 4.0f, 8.0f, 8.0f), Color.white, IsObjectSelected(shape.pivot) ? Color.red : Color.magenta);
                }

                // draw the global pivot point.
                Vector2 pivotScreenPosition = GridPointToScreen(globalPivot.position);
                Handles.DrawSolidRectangleWithOutline(new Rect(pivotScreenPosition.x - 4.0f, pivotScreenPosition.y - 4.0f, 8.0f, 8.0f), Color.white, isGlobalPivotSelected ? Color.red : Color.green);
            }

            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUIStyle createBrushStyle = new GUIStyle(EditorStyles.toolbarButton);
            if (GUILayout.Button(SabreCSGResources.ShapeEditorNewTexture, createBrushStyle))
            {
                OnNew();
            }
            if (GUILayout.Button(SabreCSGResources.ShapeEditorOpenTexture, createBrushStyle))
            {
                OnOpen();
            }
            if (GUILayout.Button(SabreCSGResources.ShapeEditorSaveTexture, createBrushStyle))
            {
                OnSave();
            }

            if (GUILayout.Button(SabreCSGResources.ShapeEditorRotate90LeftTexture, createBrushStyle))
            {
                OnRotate90Left();
            }

            if (GUILayout.Button(SabreCSGResources.ShapeEditorRotate90RightTexture, createBrushStyle))
            {
                OnRotate90Right();
            }

            if (GUILayout.Button(SabreCSGResources.ShapeEditorFlipVerticallyTexture, createBrushStyle))
            {
                OnFlipVertically();
            }
            if (GUILayout.Button(SabreCSGResources.ShapeEditorFlipHorizontallyTexture, createBrushStyle))
            {
                OnFlipHorizontally();
            }

            if (GUILayout.Button(SabreCSGResources.ShapeEditorZoomInTexture, createBrushStyle))
            {
                OnZoomIn();
            }
            if (GUILayout.Button(SabreCSGResources.ShapeEditorZoomOutTexture, createBrushStyle))
            {
                OnZoomOut();
            }

            if (GUILayout.Button(SabreCSGResources.ShapeEditorShapeCreateTexture, createBrushStyle))
            {
                OnShapeCreate();
            }

            if (GUILayout.Button(SabreCSGResources.ShapeEditorSegmentInsertTexture, createBrushStyle))
            {
                OnSegmentInsert();
            }
            if (GUILayout.Button(SabreCSGResources.ShapeEditorDeleteTexture, createBrushStyle))
            {
                OnDelete();
            }

            if (GUILayout.Button(SabreCSGResources.ShapeEditorSegmentLinearTexture, createBrushStyle))
            {
                OnSegmentLinear();
            }
            if (GUILayout.Button(SabreCSGResources.ShapeEditorSegmentBezierTexture, createBrushStyle))
            {
                OnSegmentBezier();
            }
            if (GUILayout.Button(SabreCSGResources.ShapeEditorSegmentBezierDetailTexture, createBrushStyle))
            {
                OnSegmentBezierDetail();
            }

            GUI.enabled = (Selection.activeGameObject && Selection.activeGameObject.HasComponent<ShapeEditorBrush>());
            if (GUILayout.Button(SabreCSGResources.ShapeEditorCreatePolygonTexture, createBrushStyle))
            {
                OnCreatePolygon();
            }
            if (GUILayout.Button(SabreCSGResources.ShapeEditorExtrudeRevolveTexture, createBrushStyle))
            {
                OnExtrudeRevolve();
            }
            if (GUILayout.Button(SabreCSGResources.ShapeEditorExtrudeShapeTexture, createBrushStyle))
            {
                OnExtrudeShape();
            }
            if (GUILayout.Button(SabreCSGResources.ShapeEditorExtrudePointTexture, createBrushStyle))
            {
                OnExtrudePoint();
            }
            if (GUILayout.Button(SabreCSGResources.ShapeEditorExtrudeBevelTexture, createBrushStyle))
            {
                OnExtrudeBevel();
            }
            GUI.enabled = true;

            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Rotates the segments by an amount of degrees around a pivot position.
        /// </summary>
        /// <param name="degrees">The degrees to rotate the segments by.</param>
        /// <param name="pivot">The pivot to rotate around.</param>
        private void RotateSegments(float degrees, Vector2Int pivot)
        {
            foreach (Shape shape in shapes)
            {
                foreach (Segment segment in shape.segments)
                {
                    float s = Mathf.Sin(Mathf.Deg2Rad * degrees);
                    float c = Mathf.Cos(Mathf.Deg2Rad * degrees);

                    // translate point back to origin:
                    segment.position -= pivot;
                    // translate the bezier pivots:
                    segment.bezierPivot1.position -= pivot;
                    segment.bezierPivot2.position -= pivot;

                    // rotate point.
                    float x1new = segment.position.x * c - segment.position.y * s;
                    float y1new = segment.position.x * s + segment.position.y * c;
                    // rotate bezier pivot 1.
                    float x2new = segment.bezierPivot1.position.x * c - segment.bezierPivot1.position.y * s;
                    float y2new = segment.bezierPivot1.position.x * s + segment.bezierPivot1.position.y * c;
                    // rotate bezier pivot 2.
                    float x3new = segment.bezierPivot2.position.x * c - segment.bezierPivot2.position.y * s;
                    float y3new = segment.bezierPivot2.position.x * s + segment.bezierPivot2.position.y * c;

                    // translate point back:
                    segment.position = new Vector2Int(Mathf.RoundToInt(x1new + pivot.x), Mathf.RoundToInt(y1new + pivot.y));
                    // translate bezier pivots back:
                    segment.bezierPivot1.position = new Vector2Int(Mathf.RoundToInt(x2new + pivot.x), Mathf.RoundToInt(y2new + pivot.y));
                    segment.bezierPivot2.position = new Vector2Int(Mathf.RoundToInt(x3new + pivot.x), Mathf.RoundToInt(y3new + pivot.y));
                }

                // recalculate the pivot position of the shape.
                shape.CalculatePivotPosition();
            }
        }



        /// <summary>
        /// Called when the new button is pressed. Will reset the shape.
        /// </summary>
        private void OnNew()
        {
            if (EditorUtility.DisplayDialog("2D Shape Editor", "Are you sure you wish to create a new project?", "Yes", "No"))
            {
                // reset the shapes to the default cube.
                shapes = new List<Shape>() { new Shape() };
            }
        }

        /// <summary>
        /// Called when the open button is pressed. Will let the user open an existing shape.
        /// </summary>
        private void OnOpen()
        {
            EditorUtility.DisplayDialog("2D Shape Editor", "This functionality has not been implemented yet.", "Aww!");
        }

        /// <summary>
        /// Called when the save button is pressed. Will let the user save the shape.
        /// </summary>
        private void OnSave()
        {
            EditorUtility.DisplayDialog("2D Shape Editor", "This functionality has not been implemented yet.", "Aww!");
        }

        /// <summary>
        /// Called when the rotate 90 left button is pressed. Will rotate the shape left by 90 degrees around the pivot point.
        /// </summary>
        private void OnRotate90Left()
        {
            RotateSegments(-90, globalPivot.position);
        }

        /// <summary>
        /// Called when the rotate 90 right button is pressed. Will rotate the shape right by 90 degrees around the pivot point.
        /// </summary>
        private void OnRotate90Right()
        {
            RotateSegments(90, globalPivot.position);
        }

        /// <summary>
        /// Called when the flip vertically button is pressed. Will flip the shape vertically at the pivot point.
        /// </summary>
        private void OnFlipVertically()
        {
            foreach (Shape shape in shapes)
            {
                foreach (Segment segment in shape.segments)
                {
                    // flip segment.
                    segment.position = new Vector2Int(segment.position.x, -segment.position.y + (globalPivot.position.y * 2));
                    // flip bezier pivot handles.
                    segment.bezierPivot1.position = new Vector2Int(segment.bezierPivot1.position.x, -segment.bezierPivot1.position.y + (globalPivot.position.y * 2));
                    segment.bezierPivot2.position = new Vector2Int(segment.bezierPivot2.position.x, -segment.bezierPivot2.position.y + (globalPivot.position.y * 2));
                }

                // recalculate the pivot position of the shape.
                shape.CalculatePivotPosition();
            }
        }

        /// <summary>
        /// Called when the flip horizontally button is pressed. Will flip the shape horizontally at the pivot point.
        /// </summary>
        private void OnFlipHorizontally()
        {
            foreach (Shape shape in shapes)
            {
                foreach (Segment segment in shape.segments)
                {
                    // flip segment.
                    segment.position = new Vector2Int(-segment.position.x + (globalPivot.position.x * 2), segment.position.y);
                    // flip bezier pivot handles.
                    segment.bezierPivot1.position = new Vector2Int(-segment.bezierPivot1.position.x + (globalPivot.position.x * 2), segment.bezierPivot1.position.y);
                    segment.bezierPivot2.position = new Vector2Int(-segment.bezierPivot2.position.x + (globalPivot.position.x * 2), segment.bezierPivot2.position.y);
                }

                // recalculate the pivot position of the shape.
                shape.CalculatePivotPosition();
            }
        }

        /// <summary>
        /// Called when the zoom in button is pressed. Will zoom in the grid.
        /// </summary>
        private void OnZoomIn()
        {
            switch (gridScale)
            {
                case 2: gridScale = 4; break;
                case 4: gridScale = 8; break;
                case 8: gridScale = 16; break;
                case 16: gridScale = 32; break;
                case 32: gridScale = 64; break;
                case 64: gridScale = 64; break;
                default: gridScale = 16; break;
            }
        }

        /// <summary>
        /// Called when the zoom out button is pressed. Will zoom out the grid.
        /// </summary>
        private void OnZoomOut()
        {
            switch (gridScale)
            {
                case 2 : gridScale = 2 ; break;
                case 4 : gridScale = 2 ; break;
                case 8 : gridScale = 4 ; break;
                case 16: gridScale = 8 ; break;
                case 32: gridScale = 16; break;
                case 64: gridScale = 32; break;
                default: gridScale = 16; break;
            }
        }

        /// <summary>
        /// Called when the create shape button is pressed. Will add a new shape.
        /// </summary>
        private void OnShapeCreate()
        {
            shapes.Add(new Shape());
        }

        /// <summary>
        /// Called when the insert segment button is pressed. Will split all selected segments.
        /// </summary>
        private void OnSegmentInsert()
        {
            foreach (Segment segment in selectedSegments)
            {
                Segment next = GetNextSegment(segment);
                int distance = Mathf.RoundToInt(Vector2Int.Distance(segment.position, next.position));
                if (distance < 2) continue; // too short to split segment.
                // calculate split position.
                Vector2 split = Vector2.Lerp(segment.position, next.position, 0.5f);
                // insert new segment at split position.
                Shape parent = GetShapeOfSegment(segment);
                parent.segments.Insert(parent.segments.IndexOf(next), new Segment(Mathf.RoundToInt(split.x), Mathf.RoundToInt(split.y)));

                // recalculate the pivot position of the shape.
                parent.CalculatePivotPosition();
            }
        }

        /// <summary>
        /// Called when the delete button is pressed. Will delete all selected segments and shapes.
        /// </summary>
        private void OnDelete()
        {
            // prevent the user from deleting too much.
            foreach (Shape shape in shapes)
            {
                if (shape.segments.Count - selectedSegments.Count() < 3)
                {
                    EditorUtility.DisplayDialog("2D Shape Editor", "A polygon must have at least 3 segments!", "Okay");
                    return;
                }
            }
            // remove all selected segments.
            foreach (Segment segment in selectedSegments)
            {
                Shape parent = GetShapeOfSegment(segment);
                parent.segments.Remove(segment);

                // recalculate the pivot position of the shape.
                parent.CalculatePivotPosition();
            }
            // remove all selected shapes.
            foreach (Shape shape in shapes.ToArray()) // use .ToArray() to iterate a clone.
            {
                if (IsObjectSelected(shape.pivot))
                {
                    shapes.Remove(shape);
                    selectedObjects.Remove(shape.pivot);
                }
            }
            ClearSelectionOf<Segment>();
        }

        /// <summary>
        /// Called when the linear segment button is pressed. Will set all selected segments to be linear.
        /// </summary>
        private void OnSegmentLinear()
        {
            foreach (Shape shape in shapes)
            {
                foreach (Segment segment in shape.segments)
                {
                    if (segment.type == SegmentType.Linear) continue;

                    // the segment or any of its bezier pivots can be used to mark it as linear.
                    if (IsObjectSelected(segment) || IsObjectSelected(segment.bezierPivot1) || IsObjectSelected(segment.bezierPivot2))
                    {
                        segment.type = SegmentType.Linear;
                    }
                }
            }
        }

        /// <summary>
        /// Called when the linear segment button is pressed. Will set all selected segments to be bezier.
        /// </summary>
        private void OnSegmentBezier()
        {
            foreach (Segment segment in selectedSegments)
            {
                // don't affect existing bezier segments.
                if (segment.type == SegmentType.Bezier) continue;
                segment.type = SegmentType.Bezier;

                // calculate a user friendly initial position.
                Segment next = GetNextSegment(segment);
                // calculate split positions.
                Vector2 first = Vector2.Lerp(segment.position, next.position, 0.25f);
                Vector2 second = Vector2.Lerp(segment.position, next.position, 0.75f);
                // set the bezier pivots to two positions on the segment.
                segment.bezierPivot1.position = new Vector2Int(Mathf.RoundToInt(first.x), Mathf.RoundToInt(first.y));
                segment.bezierPivot2.position = new Vector2Int(Mathf.RoundToInt(second.x), Mathf.RoundToInt(second.y));
            }
        }

        /// <summary>
        /// Called when the segment detail menu is pressed. Will let the user pick any selected segment's bezier's detail level.
        /// </summary>
        private void OnSegmentBezierDetail()
        {
            // let the user choose the amount of bezier curve detail.
            ShowCenteredPopupWindowContent(new ShapeEditorBezierDetailPopupWindowContent((customDetail) => {
                foreach (Shape shape in shapes)
                {
                    foreach (Segment segment in shape.segments)
                    {
                        if (segment.type == SegmentType.Linear) continue;
                        // the segment or any of its bezier pivots can be used to select it.
                        if (IsObjectSelected(segment) || IsObjectSelected(segment.bezierPivot1) || IsObjectSelected(segment.bezierPivot2))
                            segment.bezierDetail = customDetail;
                    }
                }

                // show the changes.
                Repaint();
            }));
        }

        /// <summary>
        /// Called when the create polygon button is pressed.
        /// </summary>
        private void OnCreatePolygon()
        {
        }

        /// <summary>
        /// Called when the extrude revolved button is pressed.
        /// </summary>
        private void OnExtrudeRevolve()
        {
        }

        /// <summary>
        /// Called when the extrude shape button is pressed.
        /// </summary>
        private void OnExtrudeShape()
        {
            ShowCenteredPopupWindowContent(new ShapeEditorExtrudeShapePopupWindowContent());
        }

        /// <summary>
        /// Called when the extrude point button is pressed.
        /// </summary>
        private void OnExtrudePoint()
        {
        }

        /// <summary>
        /// Called when the extrude bevelled button is pressed.
        /// </summary>
        private void OnExtrudeBevel()
        {
        }

        private Rect GetViewportRect()
        {
            Rect viewportRect = Screen.safeArea;
            viewportRect.y += 17;
            viewportRect.height -= 40;
            return viewportRect;
        }
        
        private void GlDrawLine(float thickness, float x1, float y1, float x2, float y2)
        {
            var point1 = new Vector2(x1, y1);
            var point2 = new Vector2(x2, y2);

            Vector2 startPoint = Vector2.zero;
            Vector2 endPoint = Vector2.zero;

            var diffx = Mathf.Abs(point1.x - point2.x);
            var diffy = Mathf.Abs(point1.y - point2.y);

            if (diffx > diffy)
            {
                if (point1.x <= point2.x)
                {
                    startPoint = point1;
                    endPoint = point2;
                }
                else
                {
                    startPoint = point2;
                    endPoint = point1;
                }
            }
            else
            {
                if (point1.y <= point2.y)
                {
                    startPoint = point1;
                    endPoint = point2;
                }
                else
                {
                    startPoint = point2;
                    endPoint = point1;
                }
            }

            var angle = Mathf.Atan2(endPoint.y - startPoint.y, endPoint.x - startPoint.x);
            var perp = angle + Mathf.PI * 0.5f;

            var p1 = Vector3.zero;
            var p2 = Vector3.zero;
            var p3 = Vector3.zero;
            var p4 = Vector3.zero;

            var cosAngle = Mathf.Cos(angle);
            var cosPerp = Mathf.Cos(perp);
            var sinAngle = Mathf.Sin(angle);
            var sinPerp = Mathf.Sin(perp);

            var distance = Vector2.Distance(startPoint, endPoint);

            p1.x = startPoint.x - (thickness * 0.5f) * cosPerp;
            p1.y = startPoint.y - (thickness * 0.5f) * sinPerp;

            p2.x = startPoint.x + (thickness * 0.5f) * cosPerp;
            p2.y = startPoint.y + (thickness * 0.5f) * sinPerp;

            p3.x = p2.x + distance * cosAngle;
            p3.y = p2.y + distance * sinAngle;

            p4.x = p1.x + distance * cosAngle;
            p4.y = p1.y + distance * sinAngle;

            GL.Vertex3(p1.x, p1.y, 0);
            GL.Vertex3(p2.x, p2.y, 0);
            GL.Vertex3(p3.x, p3.y, 0);
            GL.Vertex3(p4.x, p4.y, 0);
        }

        private void GlDrawBezier(float thickness, Vector2 start, Vector2 p1, Vector2 p2, Vector2 end, int detail)
        {
            Vector3 lineStart = Bezier.GetPoint(start, p1, p2, end, 0f);
            for (int i = 1; i <= detail; i++)
            {
                Vector3 lineEnd = Bezier.GetPoint(start, p1, p2, end, i / (float)detail);
                GlDrawLine(thickness, lineStart.x, lineStart.y, lineEnd.x, lineEnd.y);
                lineStart = lineEnd;
            }
        }

        private void DrawGrid()
        {
            Rect viewportRect = GetViewportRect();
            viewportRect.width += (gridScale * 8.0f);
            viewportRect.height += (gridScale * 8.0f);

            float ix = (-gridScale * 8.0f) + viewportScroll.x % (gridScale * 8.0f);
            float iy = (-gridScale * 8.0f) + viewportScroll.y % (gridScale * 8.0f);

            for (int x = 0; x < viewportRect.width / gridScale; x++)
            {
                for (int y = 0; y < viewportRect.height / gridScale; y++)
                {

                    if (x % 8 == 0 || y % 8 == 7)
                    {
                        GL.Color(new Color(0.843f, 0.843f, 0.843f));
                    }
                    else
                    {
                        GL.Color(new Color(0.922f, 0.922f, 0.922f));
                    }
                    float x1 = ix + x; x1 = x1 < 0.0f ? 0.0f : x1;
                    float y1 = iy + (viewportRect.y + (y * gridScale)); y1 = y1 < viewportRect.y ? viewportRect.y : y1;
                    float x2 = ix + (x + viewportRect.width); x2 = x2 > viewportRect.width ? viewportRect.width : x2;
                    float y2 = iy + (viewportRect.y + (y * gridScale)); y2 = y2 < viewportRect.y ? viewportRect.y : y2;
                    GlDrawLine(1.0f, x1, y1, x2, y2);

                    x1 = ix + (x * gridScale); x1 = x1 < 0.0f ? 0.0f : x1;
                    y1 = iy + (viewportRect.y + y); y1 = y1 < viewportRect.y ? viewportRect.y : y1;
                    x2 = ix + (x * gridScale); x2 = x2 > viewportRect.width ? viewportRect.width : x2;
                    y2 = iy + (viewportRect.y + y + viewportRect.height); y2 = y2 < viewportRect.y ? viewportRect.y : y2;
                    GlDrawLine(1.0f, x1, y1, x2, y2);
                }
            }

            Vector2 center = GridPointToScreen(new Vector2Int(0, 0));
            GL.Color(new Color(0.882f, 0.882f, 0.882f));

            GlDrawLine(3.0f, 0.0f, center.y, viewportRect.width, center.y);

            GlDrawLine(3.0f, center.x, viewportRect.y, center.x, viewportRect.height);
        }

        /// <summary>
        /// Converts a point on the screen to the point on the grid.
        /// </summary>
        /// <param name="point">The point to convert.</param>
        /// <returns>The point on the grid.</returns>
        private Vector2Int ScreenPointToGrid(Vector2 point)
        {
            Vector3 result = (point / gridScale) - (viewportScroll / gridScale);
            return new Vector2Int(Mathf.FloorToInt(result.x + 0.5f), Mathf.FloorToInt(result.y + 0.5f));
        }

        /// <summary>
        /// Converts a point on the grid to the point on the screen.
        /// </summary>
        /// <param name="point">The point to convert.</param>
        /// <returns>The point on the screen.</returns>
        private Vector2 GridPointToScreen(Vector2Int point)
        {
            return (point * (int)gridScale) + viewportScroll;
        }

        /// <summary>
        /// Provides methods for calculating bezier splines.
        /// </summary>
        private static class Bezier
        {
            public static Vector3 GetPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
            {
                t = Mathf.Clamp01(t);
                float OneMinusT = 1f - t;
                return
                    OneMinusT * OneMinusT * OneMinusT * p0 +
                    3f * OneMinusT * OneMinusT * t * p1 +
                    3f * OneMinusT * t * t * p2 +
                    t * t * t * p3;
            }
        }

        /// <summary>
        /// Shows the a popup in the center of the editor window.
        /// </summary>
        /// <param name="popup">The popup to show.</param>
        private void ShowCenteredPopupWindowContent(PopupWindowContent popup)
        {
            Vector2 size = popup.GetWindowSize();
            PopupWindow.Show(new Rect((Screen.safeArea.width / 2.0f) - (size.x / 2.0f), (Screen.safeArea.height / 2.0f) - (size.y / 2.0f), 0, 0), popup);
        }

        /// <summary>
        /// Called when the editor selection changes.
        /// </summary>
        private void OnSelectionChange()
        {
            // we have to repaint in case the user selects a shape editor brush.
            Repaint();
        }
    }
}

#endif