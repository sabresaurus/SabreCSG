using UnityEngine;
using System.Collections;
using UnityEditor;

namespace Sabresaurus.SabreCSG
{
	[CustomPropertyDrawer (typeof(ExpandPropertiesAttribute))]
	class ExpandPropertiesDrawer : PropertyDrawer 
	{
		const int PADDING = 2;
		const int HEIGHT_PER_PROPERTY = 16;
		// Draw the property inside the given rect
		public override void OnGUI (Rect position, SerializedProperty property, GUIContent label) 
		{
			int oldIndentLevel = EditorGUI.indentLevel;
			EditorGUI.indentLevel++;
			// Using BeginProperty / EndProperty on the parent property means that
			// prefab override logic works on the entire property.
			EditorGUI.BeginProperty (position, label, property);

			position.yMax = position.yMin + HEIGHT_PER_PROPERTY;
			string basePath = property.propertyPath;
			while(property.NextVisible(true) 
				&& property.propertyPath.StartsWith(basePath + "."))
			{
				EditorGUI.PropertyField(position, property);
				position.y += HEIGHT_PER_PROPERTY + PADDING;
			}

			EditorGUI.EndProperty ();

			EditorGUI.indentLevel = oldIndentLevel;
			/*

			// Draw label
			position = EditorGUI.PrefixLabel (position, GUIUtility.GetControlID (FocusType.Passive), label);

			// Don't make child fields be indented
			var indent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;

			// Calculate rects
			Rect amountRect = new Rect (position.x, position.y, 30, position.height);
			Rect unitRect = new Rect (position.x+35, position.y, 50, position.height);
			Rect nameRect = new Rect (position.x+90, position.y, position.width-90, position.height);

			// Draw fields - passs GUIContent.none to each so they are drawn without labels
			EditorGUI.PropertyField (amountRect, property.FindPropertyRelative ("amount"), GUIContent.none);
			EditorGUI.PropertyField (unitRect, property.FindPropertyRelative ("unit"), GUIContent.none);
			EditorGUI.PropertyField (nameRect, property.FindPropertyRelative ("name"), GUIContent.none);

			// Set indent back to what it was
			EditorGUI.indentLevel = indent;

			EditorGUI.EndProperty ();
			*/
		}

		public override float GetPropertyHeight (SerializedProperty property, GUIContent label)
		{
			float totalHeight = 0;
			string basePath = property.propertyPath;

			int count = 0;
			while(property.NextVisible(true) 
				&& property.propertyPath.StartsWith(basePath + "."))
			{
				if(count > 0)
				{
					totalHeight += PADDING;
				}

				count++;
				totalHeight += base.GetPropertyHeight (property, new GUIContent(property.displayName));
			}

			return totalHeight;
		}
	}
}