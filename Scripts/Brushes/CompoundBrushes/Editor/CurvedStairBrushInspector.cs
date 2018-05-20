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
        SerializedProperty fillToBottom;
        SerializedProperty curvedWall;
        SerializedProperty slopedFloor;
        SerializedProperty slopedCeiling;

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
            fillToBottom = serializedObject.FindProperty("fillToBottom");
            curvedWall = serializedObject.FindProperty("curvedWall");
            slopedFloor = serializedObject.FindProperty("slopedFloor");
            slopedCeiling = serializedObject.FindProperty("slopedCeiling");
        }

        public override void DoInspectorGUI()
        {
            bool oldBool;

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

                // can only use additional height if fill to bottom is enabled.
                if (fillToBottom.boolValue)
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(addToFirstStep);
                    if (addToFirstStep.floatValue < 0.0f)
                        addToFirstStep.floatValue = 0.0f;
                    if (EditorGUI.EndChangeCheck())
                        ApplyAndInvalidate();
                }

                EditorGUILayout.Space();

                EditorGUILayout.BeginHorizontal();

                counterClockwise.boolValue = GUILayout.Toggle(oldBool = counterClockwise.boolValue, "Counter Clockwise", EditorStyles.toolbarButton);
                if (counterClockwise.boolValue != oldBool)
                    ApplyAndInvalidate();

                fillToBottom.boolValue = GUILayout.Toggle(oldBool = fillToBottom.boolValue, "Fill To Bottom", EditorStyles.toolbarButton);
                if (fillToBottom.boolValue != oldBool)
                    ApplyAndInvalidate();

                curvedWall.boolValue = GUILayout.Toggle(oldBool = curvedWall.boolValue, "Curved Wall", EditorStyles.toolbarButton);
                if (curvedWall.boolValue != oldBool)
                    ApplyAndInvalidate();

                EditorGUILayout.EndHorizontal();
            }

            // can only use nocsg slopes if build torus is disabled.
            if (!curvedWall.boolValue)
            {
                using (new NamedVerticalScope("Curved Stair: NoCSG Operators"))
                {
                    if (slopedFloor.boolValue || slopedCeiling.boolValue)
                    {
                        EditorGUILayout.HelpBox("The surface of the slope is non-planar. This means there are triangulated bumpy seams (look closely with your camera). NoCSG will be forced for this compound brush as the CSG engine cannot handle this shape properly.", MessageType.Warning);
                    }

                    EditorGUILayout.BeginHorizontal();

                    slopedFloor.boolValue = GUILayout.Toggle(oldBool = slopedFloor.boolValue, "Sloped Floor", EditorStyles.toolbarButton);
                    if (slopedFloor.boolValue != oldBool)
                        ApplyAndInvalidate();

                    // can only use ceiling slopes if it isn't filled to the bottom.
                    if (!fillToBottom.boolValue)
                    {
                        slopedCeiling.boolValue = GUILayout.Toggle(oldBool = slopedCeiling.boolValue, "Sloped Ceiling", EditorStyles.toolbarButton);
                        if (slopedCeiling.boolValue != oldBool)
                            ApplyAndInvalidate();
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            base.DoInspectorGUI();
        }

        void ApplyAndInvalidate()
        {
            serializedObject.ApplyModifiedProperties();
            System.Array.ForEach(BrushTargets, item => item.Invalidate(true));
        }
    }
}