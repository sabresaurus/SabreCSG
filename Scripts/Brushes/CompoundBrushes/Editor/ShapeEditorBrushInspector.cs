#if UNITY_EDITOR || RUNTIME_CSG
using Sabresaurus.SabreCSG.ShapeEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ShapeEditorBrush), true)]
    public class ShapeEditorBrushInspector : CompoundBrushInspector
    {
        public override void OnInspectorGUI()
        {
            using (new NamedVerticalScope("Shape Editor Brush"))
            {
                if (GUILayout.Button("Show 2D Shape Editor"))
                {
                    ShapeEditorBrushWindow.Init();
                }
            }

            base.OnInspectorGUI();
        }
    }
}

#endif