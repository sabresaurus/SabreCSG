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
            GlobalPivotPosition,
            CreatePolygon,
            RevolveShape,
            ExtrudeShape,
            ExtrudePoint,
            ExtrudeBevel,
            GenerateCircle,
        }

        private PopupMode popupMode;

        public int bezierDetailLevel_Detail = 3;
        public float extrudeDepth = 1.0f;
        public float extrudeClipDepth = 0.5f;
        public Vector2 extrudeScale = Vector2.one;
        public int revolve360 = 8;
        public int revolveSteps = 4;
        public bool revolveSpiralSloped = false;
        public bool convexBrushes = true;
        public Vector2Int GlobalPivotPosition_Position;
        public int GenerateCircle_Radius = 2;

        private Action<ShapeEditorWindowPopup> onApply;

        public ShapeEditorWindowPopup(PopupMode popupMode, ShapeEditor.Project project, Action<ShapeEditorWindowPopup> onApply) : base()
        {
            this.popupMode = popupMode;

            // read the extrude settings from the project.
            extrudeDepth = project.extrudeDepth;
            extrudeClipDepth = project.extrudeClipDepth;
            extrudeScale = project.extrudeScale;
            revolve360 = project.revolve360;
            revolveSteps = project.revolveSteps;
            revolveSpiralSloped = project.revolveSpiralSloped;
            convexBrushes = project.convexBrushes;
            GlobalPivotPosition_Position = project.globalPivot.position;

            this.onApply = (self) =>
            {
                // store the extrude settings in the project.
                switch (popupMode)
                {
                    case PopupMode.CreatePolygon:
                        project.extrudeScale = extrudeScale;
                        project.convexBrushes = convexBrushes;
                        break;

                    case PopupMode.RevolveShape:
                        project.extrudeScale = extrudeScale;
                        project.convexBrushes = convexBrushes;
                        project.revolve360 = revolve360;
                        project.revolveSteps = revolveSteps;
                        project.revolveSpiralSloped = revolveSpiralSloped;
                        break;

                    case PopupMode.ExtrudeShape:
                        project.extrudeScale = extrudeScale;
                        project.convexBrushes = convexBrushes;
                        project.extrudeDepth = extrudeDepth;
                        break;

                    case PopupMode.ExtrudePoint:
                        project.extrudeScale = extrudeScale;
                        project.convexBrushes = convexBrushes;
                        project.extrudeDepth = extrudeDepth;
                        break;

                    case PopupMode.ExtrudeBevel:
                        project.extrudeScale = extrudeScale;
                        project.convexBrushes = convexBrushes;
                        project.extrudeDepth = extrudeDepth;
                        project.extrudeClipDepth = extrudeClipDepth;
                        break;
                }

                onApply(self);
                editorWindow.Close();
                EditorWindow.FocusWindowIfItsOpen<ShapeEditor.ShapeEditorWindow>();
            };
        }

        public override Vector2 GetWindowSize()
        {
            // + 18 for every element
            switch (popupMode)
            {
                case PopupMode.BezierDetailLevel:
                    return new Vector2(205, 140);

                case PopupMode.GlobalPivotPosition:
                    return new Vector2(300, 68);

                case PopupMode.GenerateCircle:
                    return new Vector2(300, 86);

                case PopupMode.CreatePolygon:
                    return new Vector2(300, 50 + 36);

                case PopupMode.RevolveShape:
                    return new Vector2(300, 104 + 36);

                case PopupMode.ExtrudeShape:
                    return new Vector2(300, 68 + 36);

                case PopupMode.ExtrudePoint:
                    return new Vector2(300, 68 + 36);

                case PopupMode.ExtrudeBevel:
                    return new Vector2(300, 86 + 36);

                default:
                    return new Vector2(300, 150);
            }
        }

        public override void OnGUI(Rect rect)
        {
            bool hasScale = true;
            bool hasConvexBrushes = true;
            string accept = "";
            switch (popupMode)
            {
                case PopupMode.BezierDetailLevel:
                    GUILayout.Label("Bezier Detail Level", EditorStyles.boldLabel);
                    hasScale = false;
                    hasConvexBrushes = false;
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

                case PopupMode.GlobalPivotPosition:
                    GUILayout.Label("Global Pivot Position", EditorStyles.boldLabel);
                    hasScale = false;
                    hasConvexBrushes = false;
                    accept = "Set Position";

#if !UNITY_2017_2_OR_NEWER
                    EditorGUIUtility.wideMode = true;
                    GlobalPivotPosition_Position = Vector2Int.FloorToInt(EditorGUILayout.Vector2Field("Position", GlobalPivotPosition_Position));
                    EditorGUIUtility.wideMode = false;
#else
                    EditorGUIUtility.wideMode = true;
                    GlobalPivotPosition_Position = EditorGUILayout.Vector2IntField("Position", GlobalPivotPosition_Position);
                    EditorGUIUtility.wideMode = false;
#endif
                    break;

                case PopupMode.GenerateCircle:
                    GUILayout.Label("Generate Circle", EditorStyles.boldLabel);
                    hasScale = false;
                    hasConvexBrushes = false;
                    accept = "Add Shape";

                    GenerateCircle_Radius = SabreGUILayout.EvenIntField(new GUIContent("Radius"), GenerateCircle_Radius);

                    bezierDetailLevel_Detail = EditorGUILayout.IntField("Detail", bezierDetailLevel_Detail);
                    if (bezierDetailLevel_Detail < 1) bezierDetailLevel_Detail = 1;
                    if (bezierDetailLevel_Detail > 999) bezierDetailLevel_Detail = 999;
                    break;

                case PopupMode.CreatePolygon:
                    GUILayout.Label("Create Polygon", EditorStyles.boldLabel);
                    accept = "Create";
                    break;

                case PopupMode.RevolveShape:
                    GUILayout.Label("Revolve Shape", EditorStyles.boldLabel);
                    accept = "Revolve";

                    revolve360 = EditorGUILayout.IntField("Per 360", revolve360);
                    if (revolve360 < 3) revolve360 = 3;
                    revolveSteps = EditorGUILayout.IntField("Steps", revolveSteps);
                    if (revolveSteps < 1) revolveSteps = 1;

                    revolveSpiralSloped = EditorGUILayout.Toggle("Sloped Spiral", revolveSpiralSloped);

                    // steps can't be more than 360.
                    if (revolveSteps > revolve360) revolveSteps = revolve360;
                    break;

                case PopupMode.ExtrudeShape:
                    GUILayout.Label("Extrude Shape", EditorStyles.boldLabel);
                    accept = "Extrude";

                    extrudeDepth = EditorGUILayout.FloatField("Depth", extrudeDepth);
                    if (extrudeDepth < 0.01f) extrudeDepth = 0.01f;
                    break;

                case PopupMode.ExtrudePoint:
                    GUILayout.Label("Extrude Point", EditorStyles.boldLabel);
                    accept = "Extrude";

                    extrudeDepth = EditorGUILayout.FloatField("Depth", extrudeDepth);
                    if (extrudeDepth < 0.01f) extrudeDepth = 0.01f;
                    break;

                case PopupMode.ExtrudeBevel:
                    GUILayout.Label("Extrude Bevel", EditorStyles.boldLabel);
                    accept = "Extrude";

                    extrudeDepth = EditorGUILayout.FloatField("Depth", extrudeDepth);
                    if (extrudeDepth < 0.01f) extrudeDepth = 0.01f;
                    extrudeClipDepth = EditorGUILayout.FloatField("Clip Depth", extrudeClipDepth);
                    if (extrudeClipDepth < 0.01f) extrudeClipDepth = 0.01f;
                    if (extrudeClipDepth > extrudeDepth) extrudeClipDepth = extrudeDepth;
                    break;
            }

            if (hasConvexBrushes)
            {
                convexBrushes = EditorGUILayout.Toggle("Convex Brushes", convexBrushes);
            }

            if (hasScale)
            {
                EditorGUIUtility.wideMode = true;
                extrudeScale = EditorGUILayout.Vector2Field("Scale", extrudeScale);
                EditorGUIUtility.wideMode = false;
                if (extrudeScale.x < 0.01f) extrudeScale.x = 0.01f;
                if (extrudeScale.y < 0.01f) extrudeScale.y = 0.01f;
            }

            if (GUILayout.Button(accept))
            {
                onApply(this);
            }
        }
    }
}

#endif