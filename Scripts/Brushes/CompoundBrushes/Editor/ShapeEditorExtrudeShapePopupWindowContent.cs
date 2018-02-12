#if UNITY_EDITOR || RUNTIME_CSG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
    public class ShapeEditorExtrudeShapePopupWindowContent : PopupWindowContent
    {
        public float height = 1.0f;

        public override Vector2 GetWindowSize()
        {
            return new Vector2(200, 150);
        }

        public override void OnGUI(Rect rect)
        {
            GUILayout.Label("Extrude Shape", EditorStyles.boldLabel);
            height = EditorGUILayout.FloatField("Height", height);
        }
    }
}

#endif