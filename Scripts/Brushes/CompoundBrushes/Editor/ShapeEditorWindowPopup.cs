#if UNITY_EDITOR || RUNTIME_CSG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
    /// <summary>
    /// A multi-purpose popup window used by the 2D Shape Editor.
    /// </summary>
    /// <seealso cref="UnityEditor.PopupWindowContent"/>
    public class ShapeEditorWindowPopup : PopupWindowContent
    {
        public enum PopupMode
        {
            BezierDetailLevel,
            ExtrudeShape,
            ExtrudePoint
        }

        private PopupMode popupMode;

        public int bezierDetailLevel_Detail = 3;
        public float extrudeShape_Height = 1.0f;
        public float extrudePoint_Height = 1.0f;

        private Action<ShapeEditorWindowPopup> onApply;

        public ShapeEditorWindowPopup(PopupMode popupMode, Action<ShapeEditorWindowPopup> onApply) : base()
        {
            this.popupMode = popupMode;
            this.onApply = (self) => { onApply(self); editorWindow.Close(); EditorWindow.FocusWindowIfItsOpen<ShapeEditor.ShapeEditorWindow>(); };
        }

        public override Vector2 GetWindowSize()
        {
            switch (popupMode)
            {
                case PopupMode.BezierDetailLevel:
                    return new Vector2(205, 140);
                case PopupMode.ExtrudeShape:
                    return new Vector2(205, 68);
                case PopupMode.ExtrudePoint:
                    return new Vector2(205, 68);
                default:
                    return new Vector2(200, 150);
            }
        }

        public override void OnGUI(Rect rect)
        {
            string accept = "";
            switch (popupMode)
            {
                case PopupMode.BezierDetailLevel:
                    GUILayout.Label("Bezier Detail Level", EditorStyles.boldLabel);
                    accept = "Apply";

                    GUILayout.BeginHorizontal(EditorStyles.toolbar);
                    if (GUILayout.Button("1", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { bezierDetailLevel_Detail = 1; onApply(this); }
                    if (GUILayout.Button("2", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { bezierDetailLevel_Detail = 2; onApply(this); }
                    if (GUILayout.Button("3", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { bezierDetailLevel_Detail = 3; onApply(this); }
                    if (GUILayout.Button("4", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { bezierDetailLevel_Detail = 4; onApply(this); }
                    if (GUILayout.Button("5", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { bezierDetailLevel_Detail = 5; onApply(this); }
                    if (GUILayout.Button("6", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { bezierDetailLevel_Detail = 6; onApply(this); }
                    if (GUILayout.Button("7", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { bezierDetailLevel_Detail = 7; onApply(this); }
                    if (GUILayout.Button("8", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { bezierDetailLevel_Detail = 8; onApply(this); }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(EditorStyles.toolbar);
                    if (GUILayout.Button("9", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { bezierDetailLevel_Detail = 9; onApply(this); }
                    if (GUILayout.Button("10", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { bezierDetailLevel_Detail = 10; onApply(this); }
                    if (GUILayout.Button("11", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { bezierDetailLevel_Detail = 11; onApply(this); }
                    if (GUILayout.Button("12", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { bezierDetailLevel_Detail = 12; onApply(this); }
                    if (GUILayout.Button("13", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { bezierDetailLevel_Detail = 13; onApply(this); }
                    if (GUILayout.Button("14", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { bezierDetailLevel_Detail = 14; onApply(this); }
                    if (GUILayout.Button("15", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { bezierDetailLevel_Detail = 15; onApply(this); }
                    if (GUILayout.Button("16", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { bezierDetailLevel_Detail = 16; onApply(this); }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(EditorStyles.toolbar);
                    if (GUILayout.Button("17", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { bezierDetailLevel_Detail = 17; onApply(this); }
                    if (GUILayout.Button("18", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { bezierDetailLevel_Detail = 18; onApply(this); }
                    if (GUILayout.Button("19", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { bezierDetailLevel_Detail = 19; onApply(this); }
                    if (GUILayout.Button("20", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { bezierDetailLevel_Detail = 20; onApply(this); }
                    if (GUILayout.Button("21", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { bezierDetailLevel_Detail = 21; onApply(this); }
                    if (GUILayout.Button("22", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { bezierDetailLevel_Detail = 22; onApply(this); }
                    if (GUILayout.Button("23", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { bezierDetailLevel_Detail = 23; onApply(this); }
                    if (GUILayout.Button("24", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { bezierDetailLevel_Detail = 24; onApply(this); }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(EditorStyles.toolbar);
                    if (GUILayout.Button("25", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { bezierDetailLevel_Detail = 25; onApply(this); }
                    if (GUILayout.Button("26", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { bezierDetailLevel_Detail = 26; onApply(this); }
                    if (GUILayout.Button("27", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { bezierDetailLevel_Detail = 27; onApply(this); }
                    if (GUILayout.Button("28", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { bezierDetailLevel_Detail = 28; onApply(this); }
                    if (GUILayout.Button("29", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { bezierDetailLevel_Detail = 29; onApply(this); }
                    if (GUILayout.Button("30", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { bezierDetailLevel_Detail = 30; onApply(this); }
                    if (GUILayout.Button("31", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { bezierDetailLevel_Detail = 31; onApply(this); }
                    if (GUILayout.Button("32", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { bezierDetailLevel_Detail = 32; onApply(this); }
                    GUILayout.EndHorizontal();

                    bezierDetailLevel_Detail = EditorGUILayout.IntField("Detail", bezierDetailLevel_Detail);
                    if (bezierDetailLevel_Detail < 1) bezierDetailLevel_Detail = 1;
                    if (bezierDetailLevel_Detail > 999) bezierDetailLevel_Detail = 999;
                    break;

                case PopupMode.ExtrudeShape:
                    GUILayout.Label("Extrude Shape", EditorStyles.boldLabel);
                    accept = "Extrude";

                    extrudeShape_Height = EditorGUILayout.FloatField("Depth", extrudeShape_Height);
                    if (extrudeShape_Height < 0.01f) extrudeShape_Height = 0.01f;
                    break;

                case PopupMode.ExtrudePoint:
                    GUILayout.Label("Extrude Point", EditorStyles.boldLabel);
                    accept = "Extrude";

                    extrudePoint_Height = EditorGUILayout.FloatField("Depth", extrudePoint_Height);
                    if (extrudePoint_Height < 0.01f) extrudePoint_Height = 0.01f;
                    break;
            }

            if (GUILayout.Button(accept))
            {
                onApply(this);
            }
        }
    }
}

#endif