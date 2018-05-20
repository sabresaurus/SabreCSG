using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(TrimBrush), true)]
	public class TrimBrushInspector : CompoundBrushInspector
	{
		SerializedProperty trimSizeProp;

		protected override void OnEnable ()
		{
			base.OnEnable ();
			// Setup the SerializedProperties.
			trimSizeProp = serializedObject.FindProperty ("trimSize");
        }

		public override void DoInspectorGUI()
		{
			using (new NamedVerticalScope("TrimBrush"))
			{
				EditorGUI.BeginChangeCheck();
	            EditorGUILayout.PropertyField(trimSizeProp);
	            if (EditorGUI.EndChangeCheck())
	            {
	                ApplyAndInvalidate();
	            }

                EditorGUILayout.Space();
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
