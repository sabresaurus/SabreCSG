using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(StairBrush), true)]
	public class StairBrushInspector : CompoundBrushInspector
	{
		// Manual size of step
		SerializedProperty stepDepthProp;
		SerializedProperty stepHeightProp;
        SerializedProperty stepDepthSpacingProp;
        SerializedProperty stepHeightSpacingProp;

        // Whether the size of the step should be automatically calculated from the bounds and other step dimension
		SerializedProperty autoDepthProp;
        SerializedProperty autoHeightProp;

		// Whether the stairs align to the bottom edge or the top edge
		SerializedProperty leadFromTopProp;

		protected override void OnEnable ()
		{
			base.OnEnable ();
			// Setup the SerializedProperties.
			stepDepthProp = serializedObject.FindProperty ("stepDepth");
			stepHeightProp = serializedObject.FindProperty ("stepHeight");

            stepDepthSpacingProp = serializedObject.FindProperty("stepDepthSpacing");
            stepHeightSpacingProp = serializedObject.FindProperty("stepHeightSpacing");

			autoDepthProp = serializedObject.FindProperty ("autoDepth");
            autoHeightProp = serializedObject.FindProperty ("autoHeight");

			leadFromTopProp = serializedObject.FindProperty ("leadFromTop");
		}

		public override void OnInspectorGUI()
		{
            Rect rect = GUILayoutUtility.GetRect(new GUIContent(), GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.Height(130));

            DrawStairPreview(rect);

			using (new NamedVerticalScope("Stair"))
			{
				if(DisplayAutoSetting(stepDepthProp, autoDepthProp))
				{
	                ApplyAndInvalidate();
	            }

				EditorGUILayout.Space();

				if(DisplayAutoSetting(stepHeightProp, autoHeightProp))
				{
	                ApplyAndInvalidate();
				}

				EditorGUILayout.Space();

	            EditorGUI.BeginChangeCheck();
	            EditorGUILayout.PropertyField(stepDepthSpacingProp);
	            if (EditorGUI.EndChangeCheck())
	            {
	                ApplyAndInvalidate();
	            }

	            EditorGUI.BeginChangeCheck();
	            EditorGUILayout.PropertyField(stepHeightSpacingProp);
	            if(EditorGUI.EndChangeCheck())
	            {
	                ApplyAndInvalidate();
	            }

	            bool oldValue = leadFromTopProp.boolValue;
				bool newValue = GUILayout.Toggle(oldValue, "Lead From Top", EditorStyles.toolbarButton);
				if(newValue != oldValue)
				{
					leadFromTopProp.boolValue = newValue;
					serializedObject.ApplyModifiedProperties();
					System.Array.ForEach(BrushTargets, item => item.Invalidate(true));
				}
				EditorGUILayout.Space();
			}

			base.OnInspectorGUI();
		}

        private void DrawStairPreview(Rect rect)
        {
            // Calculate an appropriate world space to UI scale for the diagram
            float depthTotal = stepDepthProp.floatValue * 2 + stepDepthSpacingProp.floatValue;
            float heightTotal = stepHeightProp.floatValue * 2 + stepHeightSpacingProp.floatValue;
            float uiScale = Mathf.Min(150 / depthTotal, 60 / heightTotal);

            // Offset the center of the diagram to fit better
            Vector2 centerOffset = rect.center + new Vector2(0, 10);

            // Draw white background
            //Graphics.DrawTexture(rect, EditorGUIUtility.whiteTexture);

            // Step size and spacing in UI space
            Vector2 stepUISize = new Vector2(stepDepthProp.floatValue * uiScale, stepHeightProp.floatValue * uiScale);
            Vector2 stepUISpacing = new Vector2(stepDepthSpacingProp.floatValue * uiScale, stepHeightSpacingProp.floatValue * uiScale);
            
            // Tint boxes blue
            GUI.color = new Color(0, 0.7f, 1f);

            // Draw bottom left step
            Rect stepRect1 = new Rect(centerOffset.x, centerOffset.y, stepUISize.x, stepUISize.y);
            stepRect1.center += new Vector2(-stepUISize.x - stepUISpacing.x/2f, stepUISpacing.y / 2f);
            GUI.Box(stepRect1, new GUIContent());

            // Draw top right step
            Rect stepRect2 = new Rect(centerOffset.x, centerOffset.y, stepUISize.x, stepUISize.y);
            stepRect2.center += new Vector2(stepUISpacing.x / 2f, -stepUISize.y - stepUISpacing.y/2f);
            GUI.Box(stepRect2, new GUIContent());

            // Draw depth spacing lines          
            GUI.color = Color.grey;
            
            Rect drawRect = new Rect(stepRect1.xMax, stepRect2.yMin - 20, 1, 18);
            GUI.DrawTexture(drawRect, EditorGUIUtility.whiteTexture);

            drawRect = new Rect(stepRect2.xMin, stepRect2.yMin - 20, 1, 18);
            GUI.DrawTexture(drawRect, EditorGUIUtility.whiteTexture);

            // Draw height spacing lines
            drawRect = new Rect(stepRect1.xMin - 20, stepRect1.yMin, 18, 1);
            GUI.DrawTexture(drawRect, EditorGUIUtility.whiteTexture);

            drawRect = new Rect(stepRect1.xMin - 20, stepRect2.yMax, 18, 1);
            GUI.DrawTexture(drawRect, EditorGUIUtility.whiteTexture);

            // Draw text annotations
            SabreGUILayout.DrawAnchoredLabel(stepDepthProp.floatValue.ToString(), new Vector2(stepRect1.center.x, stepRect1.yMax), new Vector2(30, 20), TextAnchor.UpperCenter);
            SabreGUILayout.DrawAnchoredLabel(stepHeightProp.floatValue.ToString(), new Vector2(stepRect1.xMax, stepRect1.center.y), new Vector2(30, 20), TextAnchor.MiddleLeft);

            SabreGUILayout.DrawAnchoredLabel(stepDepthSpacingProp.floatValue.ToString(), new Vector2((stepRect1.xMax + stepRect2.xMin) / 2f, stepRect2.yMin - 20), new Vector2(30, 20), TextAnchor.LowerCenter);
            SabreGUILayout.DrawAnchoredLabel(stepHeightSpacingProp.floatValue.ToString(), new Vector2(stepRect1.xMin - 20, (stepRect1.yMin + stepRect2.yMax) / 2f), new Vector2(30, 20), TextAnchor.MiddleRight);

            // Reset GUI tints for rest of UI
            GUI.color = Color.white; 
        }

        void ApplyAndInvalidate()
        {
            serializedObject.ApplyModifiedProperties();
            System.Array.ForEach(BrushTargets, item => item.Invalidate(true));
        }

        private bool DisplayAutoSetting(SerializedProperty manualProp, SerializedProperty autoToggleProp)
		{
			bool changeOccurred = false;

			EditorGUILayout.BeginHorizontal();
			EditorGUI.BeginChangeCheck();
			float originalValue = manualProp.floatValue;
			EditorGUILayout.PropertyField(manualProp);

			if(EditorGUI.EndChangeCheck())
			{
				if(manualProp.floatValue <= 0)
				{
					manualProp.floatValue = originalValue;
				}
				changeOccurred = true;
			}

			bool oldValue = autoToggleProp.boolValue;

			bool newValue = GUILayout.Toggle(oldValue, "Auto", EditorStyles.toolbarButton, GUILayout.Width(50));

			if(newValue != oldValue)
			{
				autoToggleProp.boolValue = newValue;
				changeOccurred = true;
			}

			EditorGUILayout.EndHorizontal();

			return changeOccurred;
		}
	}
}