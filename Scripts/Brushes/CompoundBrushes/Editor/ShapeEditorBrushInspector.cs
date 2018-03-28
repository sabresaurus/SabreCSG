﻿#if UNITY_EDITOR || RUNTIME_CSG
using Sabresaurus.SabreCSG.ShapeEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
    [CustomEditor(typeof(ShapeEditorBrush), true)]
    public class ShapeEditorBrushInspector : CompoundBrushInspector
    {
        public override void DoInspectorGUI()
        {
            using (new NamedVerticalScope("Shape Editor Brush"))
            {
                GUILayout.BeginHorizontal(EditorStyles.toolbar);
                GUIStyle createBrushStyle = new GUIStyle(EditorStyles.toolbarButton);
                if (GUILayout.Button(new GUIContent(" Show Editor", SabreCSGResources.ButtonShapeEditorTexture, "Show 2D Shape Editor"), createBrushStyle))
                {
                    // display the 2d shape ditor.
                    ShapeEditorWindow.Init();
                }
                if (GUILayout.Button(new GUIContent(" Load Project", SabreCSGResources.ShapeEditorOpenTexture, "Load Embedded Project Into 2D Shape Editor"), createBrushStyle))
                {
                    if (EditorUtility.DisplayDialog("2D Shape Editor", "Are you sure you wish to load the embedded project?\r\nAny unsaved work in the 2D Shape Editor will be lost!", "Yes", "No"))
                    {
                        // display the 2d shape ditor.
                        ShapeEditorWindow window = ShapeEditorWindow.InitAndGetHandle();
                        // load a copy of the embedded project into the editor.
                        window.LoadProject(BrushTarget.GetComponent<ShapeEditorBrush>().GetEmbeddedProject());
                    }
                }
                GUILayout.EndHorizontal();
            }

            base.DoInspectorGUI();
        }
    }
}

#endif