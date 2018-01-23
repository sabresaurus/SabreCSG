using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(CurvedStairBrush), true)]
    public class CurvedStairBrushInspector : CompoundBrushInspector
    {
        SerializedProperty innerRadius;
        SerializedProperty stepHeight;
        SerializedProperty stepWidth;
        SerializedProperty angleOfCurve;
        SerializedProperty numSteps;
        SerializedProperty addToFirstStep;
        SerializedProperty counterClockwise;

        protected override void OnEnable()
        {
            base.OnEnable();
            // Setup the SerializedProperties.
            innerRadius = serializedObject.FindProperty("innerRadius");
            stepHeight = serializedObject.FindProperty("stepHeight");
            stepWidth = serializedObject.FindProperty("stepWidth");
            angleOfCurve = serializedObject.FindProperty("angleOfCurve");
            numSteps = serializedObject.FindProperty("numSteps");
            addToFirstStep = serializedObject.FindProperty("addToFirstStep");
            counterClockwise = serializedObject.FindProperty("counterClockwise");
        }

        public override void OnInspectorGUI()
        {
            using (new NamedVerticalScope("Curved Stair"))
            {
                EditorGUI.BeginChangeCheck();
                {
                    EditorGUILayout.PropertyField(innerRadius);
                    if (innerRadius.floatValue < 0.01f)
                        innerRadius.floatValue = 0.01f;
                    if (EditorGUI.EndChangeCheck())
                        ApplyAndInvalidate();
                }

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(stepHeight);
                if (stepHeight.floatValue < 0.001f)
                    stepHeight.floatValue = 0.001f;
                if (EditorGUI.EndChangeCheck())
                    ApplyAndInvalidate();

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(stepWidth);
                if (stepWidth.floatValue < 0.001f)
                    stepWidth.floatValue = 0.001f;
                if (EditorGUI.EndChangeCheck())
                    ApplyAndInvalidate();

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(angleOfCurve);
                if (angleOfCurve.floatValue < 1.0f)
                    angleOfCurve.floatValue = 1.0f;
                if (angleOfCurve.floatValue > 360.0f)
                    angleOfCurve.floatValue = 360.0f;
                if (EditorGUI.EndChangeCheck())
                    ApplyAndInvalidate();

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(numSteps);
                if (numSteps.intValue < 1)
                    numSteps.intValue = 1;
                if (EditorGUI.EndChangeCheck())
                    ApplyAndInvalidate();

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(addToFirstStep);
                if (addToFirstStep.floatValue < 0.0f)
                    addToFirstStep.floatValue = 0.0f;
                if (EditorGUI.EndChangeCheck())
                    ApplyAndInvalidate();

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(counterClockwise);
                if (EditorGUI.EndChangeCheck())
                    ApplyAndInvalidate();
            }

            base.OnInspectorGUI();
        }

        void ApplyAndInvalidate()
        {
            serializedObject.ApplyModifiedProperties();
            System.Array.ForEach(BrushTargets, item => item.Invalidate(true));
        }
    }
}