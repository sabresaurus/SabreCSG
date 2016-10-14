using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;

namespace Sabresaurus.SabreCSG
{
	[CustomEditor(typeof(KeyMappings))]
	public class KeyMappingsInspector : Editor
	{
		public override void OnInspectorGUI()
		{
			GUILayout.Label("SabreCSG Key Mappings", SabreGUILayout.GetTitleStyle());

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Uses Unity shortcut format");
			GUIStyle style = new GUIStyle(GUI.skin.label);
			style.normal.textColor = Color.blue;
			style.fontStyle = FontStyle.Bold;
			if(GUILayout.Button("See format docs", style))
			{
				Application.OpenURL("http://unity3d.com/support/documentation/ScriptReference/MenuItem.html");
			}
			EditorGUILayout.EndHorizontal();

			DrawDefaultInspector();
		}
	}
}