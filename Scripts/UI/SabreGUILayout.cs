#if UNITY_EDITOR

using UnityEngine;
using System.Collections;
using UnityEditor;
using System;

namespace Sabresaurus.SabreCSG
{
	public static class SabreGUILayout
	{
		static Texture2D textFieldTexture1;
		static Texture2D textFieldTexture2;

		static int cachedIndentLevel;

		public static bool Button(string title, GUIStyle style = null)
	    {
			return GUILayout.Button(title, style ?? EditorStyles.miniButton);
	    }

		public static bool Button(Texture image, GUIStyle style = null)
		{
			return GUILayout.Button(image, style ?? EditorStyles.miniButton);
		}

		public static bool ColorButton(Color color)
		{
			bool buttonPressed = GUILayout.Button(new GUIContent());
			if (Event.current.type == EventType.Repaint)
			{
				Rect rect = GUILayoutUtility.GetLastRect();
				// Inset the color rect
				rect = rect.ExpandFromCenter(new Vector2(-4,-4));
				GUI.color = color;
				GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill);
				GUI.color = Color.white;
			}
			return buttonPressed;
		}

	    public static bool Toggle(bool value, string title, params GUILayoutOption[] options)
	    {
	        value = GUILayout.Toggle(value, title, EditorStyles.toolbarButton, options);
	        return value;
	    }

		public static bool ToggleMixed(bool[] values, string title, params GUILayoutOption[] options)
		{
			bool value = false;
			bool conflicted = false;
			if(values.Length > 1)
			{
				value = false;
				conflicted = true;
			}
			else
			{
				value = values[0];
				conflicted = false;
			}

			if(conflicted)
			{
				GUI.color = new Color(.8f,.8f,.8f);
			}

			value = GUILayout.Toggle(value, title, EditorStyles.toolbarButton, options);

			GUI.color = Color.white;

			return value;
		}

		public static bool ToggleMixed(Rect rect, bool value, bool conflicted, string title)
		{
			if(conflicted)
			{
				GUI.color = new Color(.8f,.8f,.8f);
			}

			value = GUI.Toggle(rect, value, title, EditorStyles.toolbarButton);

			GUI.color = Color.white;

			return value;
		}

		public static GUISkin GetInspectorSkin()
		{
			return EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector);
		}

		public static GUIStyle GetLabelStyle()
		{
			GUIStyle style = new GUIStyle(GUI.skin.label);
			if(EditorGUIUtility.isProSkin)
			{
				style.normal.textColor = Color.white;
			}
			else
			{
				style.normal.textColor = Color.black;
			}
			return style;
		}

		public static GUIStyle GetOverlayStyle()
		{
			GUIStyle style = new GUIStyle(GUI.skin.box);
			style.border = new RectOffset(22,22,22,22);
			style.normal.background = SabreCSGResources.DialogOverlayTexture;
#if UNITY_5_4_OR_NEWER
			style.normal.scaledBackgrounds = new Texture2D[] { SabreCSGResources.DialogOverlayRetinaTexture };
#endif			
			style.fontSize = 20;
			style.padding = new RectOffset(15,15,15,15);
			style.alignment = TextAnchor.MiddleCenter;
			style.normal.textColor = Color.red;
			return style;
		}

		public static GUIStyle GetTextFieldStyle1()
		{
			GUIStyle style = new GUIStyle(EditorStyles.textField);
			if(textFieldTexture1 == null)
			{
				textFieldTexture1 = UnityEditor.AssetDatabase.LoadMainAssetAtPath(CSGModel.GetSabreCSGPath() + "Gizmos/TextField.png") as Texture2D;
			}
			style.normal.background = textFieldTexture1;
			style.normal.textColor = Color.white;
			style.focused.background = textFieldTexture1;
			style.focused.textColor = Color.white;
			return style;
		}

		public static GUIStyle GetTextFieldStyle2()
		{
			GUIStyle style = new GUIStyle(EditorStyles.textField);
			if(textFieldTexture2 == null)
			{
				textFieldTexture2 = UnityEditor.AssetDatabase.LoadMainAssetAtPath(CSGModel.GetSabreCSGPath() + "Gizmos/TextField2.png") as Texture2D;
			}
			style.normal.background = textFieldTexture2;
			style.normal.textColor = Color.black;
			style.focused.background = textFieldTexture2;
			style.focused.textColor = Color.black;
			return style;
		}

		public static Color GetForeColor()
		{
			if(EditorGUIUtility.isProSkin)
			{
				return Color.white;
			}
			else
			{
				return Color.black;
			}
		}

		public static GUIStyle GetForeStyle()
		{
			return FormatStyle(GetForeColor());
		}

		public static GUIStyle GetTitleStyle(int fontSize = 11)
		{
			return FormatStyle(GetForeColor(), fontSize, FontStyle.Bold, TextAnchor.MiddleLeft);
		}

	    public static GUIStyle FormatStyle(Color textColor)
	    {
	        GUIStyle style = new GUIStyle(GUI.skin.label);
	        style.normal.textColor = textColor;
	        return style;
	    }

	    public static GUIStyle FormatStyle(Color textColor, int fontSize)
	    {
	        GUIStyle style = new GUIStyle(GUI.skin.label);
	        style.normal.textColor = textColor;
	        style.fontSize = fontSize;
	        return style;
	    }

		public static GUIStyle FormatStyle(Color textColor, int fontSize, TextAnchor alignment)
		{
			GUIStyle style = new GUIStyle(GUI.skin.label);
			style.normal.textColor = textColor;
			style.alignment = alignment;
			style.fontSize = fontSize;
			return style;
		}

		public static GUIStyle FormatStyle(Color textColor, int fontSize, FontStyle fontStyle, TextAnchor alignment)
		{
			GUIStyle style = new GUIStyle(GUI.skin.label);
			style.normal.textColor = textColor;
			style.alignment = alignment;
			style.fontSize = fontSize;
			style.fontStyle = fontStyle;
			return style;
		}

		public static T? EnumPopupMixed<T> (string label, T[] selected, params GUILayoutOption[] options) where T : struct, IConvertible
		{
			if (!typeof(T).IsEnum)
			{
				throw new ArgumentException("EnumPopupMixed must be passed an enum");
			}

			string[] names = Enum.GetNames(typeof(T));

			int selectedIndex = (int)(object)selected[0];

			for (int i = 1; i < selected.Length; i++) 
			{
				if(selectedIndex != (int)(object)selected[i])
				{
					selectedIndex = names.Length;

					break;
				}
			}

			bool mixedSelection = (selectedIndex == names.Length);
			// Mixed selection, add a name entry for "Mixed"
			if(mixedSelection)
			{
				Array.Resize(ref names, names.Length+1);
				
				int mixedIndex = names.Length-1;
				names[mixedIndex] = "Mixed";
			}

			int newIndex = EditorGUILayout.Popup(label, selectedIndex, names, options);

			if(mixedSelection && newIndex == names.Length-1)
			{
				return null;
			}
			else
			{
				return (T)Enum.ToObject(typeof(T), newIndex);
			}
		}

	    public static T DrawEnumGrid<T>(T value, params GUILayoutOption[] options) where T : struct, IConvertible
	    {
	        if (!typeof(T).IsEnum)
	        {
	            throw new ArgumentException("DrawEnumGrid must be passed an enum");
	        }
	        GUILayout.BeginHorizontal();

	        string[] names = Enum.GetNames(value.GetType());
	        for (int i = 0; i < names.Length; i++)
	        {
	            GUIStyle activeStyle;
	            if (names.Length == 1) // Only one button
	            {
	                activeStyle = EditorStyles.miniButton;
	            }
	            else if (i == 0) // Left-most button
	            {
	                activeStyle = EditorStyles.miniButtonLeft;
	            }
	            else if (i == names.Length - 1) // Right-most button
	            {
	                activeStyle = EditorStyles.miniButtonRight;
	            }
	            else // Normal mid button
	            {
	                activeStyle = EditorStyles.miniButtonMid;
	            }

	            bool isActive = (Convert.ToInt32(value) == i);
//				string displayName = StringHelper.ParseDisplayString(names[i]);
				string displayName = names[i];
				if (GUILayout.Toggle(isActive, displayName, activeStyle, options))
	            {
	                value = (T)Enum.ToObject(typeof(T), i);
	            }
	        }

	        GUILayout.EndHorizontal();
	        return value;
	    }

		public static bool DrawUVField(Rect rect, float? sourceFloat, ref string floatString, string controlName, GUIStyle style)
		{
			if(!sourceFloat.HasValue)
			{
				EditorGUI.showMixedValue = true;
				sourceFloat = 1;
			}

			GUI.SetNextControlName(controlName);

			string sourceString = floatString ?? sourceFloat.Value.ToString();


			string newUScaleString = EditorGUI.TextField(rect, sourceString, style);

			EditorGUI.showMixedValue = false;

			if(GUI.GetNameOfFocusedControl() == controlName)
			{
				floatString = newUScaleString;
			}
			else
			{
				floatString = null;
			}

			bool keyboardEnter = Event.current.isKey 
				&& Event.current.keyCode == KeyCode.Return 
				&& Event.current.rawType == EventType.KeyUp 
				&& GUI.GetNameOfFocusedControl() == controlName;

			if(keyboardEnter)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public static void BeginIndentRegion()
		{
			cachedIndentLevel = EditorGUI.indentLevel;
		}

		public static void EndIndentRegion()
		{
			EditorGUI.indentLevel = cachedIndentLevel;
		}

        public static void DrawAnchoredLabel(string text, Vector2 anchoredPosition, Vector2 size, TextAnchor alignment)
        {
            // Calculate normalized device coordinates (-1 to +1)
            float xAnchor = ((int)alignment % 3) - 1; // -1 is left, 0 center, 1 is right
            float yAnchor = ((int)alignment / 3) - 1; // -1 is left, 0 center, 1 is right

            Vector2 realPosition = anchoredPosition + new Vector2((-xAnchor * 0.5f - 0.5f) * size.x, -(yAnchor * 0.5f + 0.5f) * size.y);

            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.alignment = alignment;
            GUI.Label(new Rect(realPosition.x, realPosition.y, size.x, size.y), text, style);
        }

        public static void DrawOutlinedBox(Rect rect, Color fillColor)
        {
            // Draw outer line in black
            GUI.color = Color.black;
            GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture);

            // Draw an inset rectangle in the specified colour
            rect.size -= new Vector2(2, 2);
            rect.center += new Vector2(1, 1);
            GUI.color = fillColor;
            GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture);
        }
    }
}
#endif