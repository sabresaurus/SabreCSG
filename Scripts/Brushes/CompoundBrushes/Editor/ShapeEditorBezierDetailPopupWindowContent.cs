#if UNITY_EDITOR || RUNTIME_CSG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
    public class ShapeEditorBezierDetailPopupWindowContent : PopupWindowContent
    {
        private int detail = 3;
        private Action<int> onApply;

        public ShapeEditorBezierDetailPopupWindowContent(Action<int> onApply) : base()
        {
            this.onApply = onApply;
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(205, 140);
        }

        public override void OnGUI(Rect rect)
        {
            GUILayout.Label("Bezier Detail Level", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("1", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { detail = 1; onApply(detail); editorWindow.Close(); }
            if (GUILayout.Button("2", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { detail = 2; onApply(detail); editorWindow.Close(); }
            if (GUILayout.Button("3", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { detail = 3; onApply(detail); editorWindow.Close(); }
            if (GUILayout.Button("4", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { detail = 4; onApply(detail); editorWindow.Close(); }
            if (GUILayout.Button("5", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { detail = 5; onApply(detail); editorWindow.Close(); }
            if (GUILayout.Button("6", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { detail = 6; onApply(detail); editorWindow.Close(); }
            if (GUILayout.Button("7", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { detail = 7; onApply(detail); editorWindow.Close(); }
            if (GUILayout.Button("8", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { detail = 8; onApply(detail); editorWindow.Close(); }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("9",  EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { detail = 9;  onApply(detail); editorWindow.Close(); }
            if (GUILayout.Button("10", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { detail = 10; onApply(detail); editorWindow.Close(); }
            if (GUILayout.Button("11", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { detail = 11; onApply(detail); editorWindow.Close(); }
            if (GUILayout.Button("12", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { detail = 12; onApply(detail); editorWindow.Close(); }
            if (GUILayout.Button("13", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { detail = 13; onApply(detail); editorWindow.Close(); }
            if (GUILayout.Button("14", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { detail = 14; onApply(detail); editorWindow.Close(); }
            if (GUILayout.Button("15", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { detail = 15; onApply(detail); editorWindow.Close(); }
            if (GUILayout.Button("16", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { detail = 16; onApply(detail); editorWindow.Close(); }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("17", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { detail = 17; onApply(detail); editorWindow.Close(); }
            if (GUILayout.Button("18", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { detail = 18; onApply(detail); editorWindow.Close(); }
            if (GUILayout.Button("19", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { detail = 19; onApply(detail); editorWindow.Close(); }
            if (GUILayout.Button("20", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { detail = 20; onApply(detail); editorWindow.Close(); }
            if (GUILayout.Button("21", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { detail = 21; onApply(detail); editorWindow.Close(); }
            if (GUILayout.Button("22", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { detail = 22; onApply(detail); editorWindow.Close(); }
            if (GUILayout.Button("23", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { detail = 23; onApply(detail); editorWindow.Close(); }
            if (GUILayout.Button("24", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { detail = 24; onApply(detail); editorWindow.Close(); }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("25", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { detail = 25; onApply(detail); editorWindow.Close(); }
            if (GUILayout.Button("26", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { detail = 26; onApply(detail); editorWindow.Close(); }
            if (GUILayout.Button("27", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { detail = 27; onApply(detail); editorWindow.Close(); }
            if (GUILayout.Button("28", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { detail = 28; onApply(detail); editorWindow.Close(); }
            if (GUILayout.Button("29", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { detail = 29; onApply(detail); editorWindow.Close(); }
            if (GUILayout.Button("30", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { detail = 30; onApply(detail); editorWindow.Close(); }
            if (GUILayout.Button("31", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { detail = 31; onApply(detail); editorWindow.Close(); }
            if (GUILayout.Button("32", EditorStyles.toolbarButton, GUILayout.MinWidth(24), GUILayout.MaxWidth(24))) { detail = 32; onApply(detail); editorWindow.Close(); }
            GUILayout.EndHorizontal();

            detail = EditorGUILayout.IntField("Detail", detail);
            if (detail < 1) detail = 1;
            if (detail > 999) detail = 999;
            if (GUILayout.Button("Apply"))
            {
                onApply(detail);
                editorWindow.Close();
            }
        }
    }
}

#endif