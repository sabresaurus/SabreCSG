#if UNITY_EDITOR

using UnityEngine;
using System.Collections;
using UnityEditor;
using System;
using System.Reflection;

namespace Sabresaurus.SabreCSG
{
    public static class SabreGUILayout
    {
        private static Texture2D textFieldTexture1;
        private static Texture2D textFieldTexture2;

        private static int cachedIndentLevel;

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
                rect = rect.ExpandFromCenter(new Vector2(-4, -4));
                GUI.color = color;
                GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill);
                GUI.color = Color.white;
            }
            return buttonPressed;
        }

        public static bool ColorButtonLabel(Color color, string label, GUIStyle style)
        {
            bool buttonPressed = GUILayout.Button(label, style);
            if (Event.current.type == EventType.Repaint)
            {
                Rect colorRect = GUILayoutUtility.GetLastRect();
                // Inset the color rect
                colorRect = colorRect.ExpandFromCenter(new Vector2(-6, -6));
                colorRect.xMax = colorRect.xMin + colorRect.height;

                Rect borderRect = colorRect.ExpandFromCenter(new Vector2(2, 2));

                GUI.color = Color.black;
                GUI.DrawTexture(borderRect, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill);

                Color rgbColor = color;
                rgbColor.a = 1;
                GUI.color = rgbColor;

                GUI.DrawTexture(colorRect, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill);

                // Draw alpha at bottom
                colorRect.yMin = colorRect.yMax - 3;
                GUI.color = Color.Lerp(Color.black, Color.white, color.a);

                GUI.DrawTexture(colorRect, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill);
                GUI.color = Color.white;
            }
            return buttonPressed;
        }

        public static void RightClickMiniButton(string text, string tooltip, Action onLeftClicked, Action onRightClicked)
        {
            RightClickMiniButton(new GUIContent(text, tooltip), onLeftClicked, onRightClicked);
        }

        public static void RightClickMiniButton(GUIContent content, Action onLeftClicked, Action onRightClicked)
        {
            bool buttonPressed = GUILayout.Button(new GUIContent(content.text, content.tooltip + "\nRight click to configure..."), EditorStyles.miniButton);
            if (Event.current.type == EventType.Repaint)
            {
                Rect rect = GUILayoutUtility.GetLastRect();
                float offset = rect.width - 9;
                rect.height = 13;
                rect.width = 13;
                rect.y += 1;
                rect.x = offset;
                GUI.DrawTexture(rect, SabreCSGResources.MouseRightClickHintTexture);
            }
            // detect mouse right-click and invoke the callback.
            if (buttonPressed && Event.current.button == 1)
                onRightClicked.Invoke();
            // use the actual button bool to determine a button press and invoke the callback.
            else if (buttonPressed)
                onLeftClicked.Invoke();
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
            if (values.Length > 1)
            {
                value = false;
                conflicted = true;
            }
            else
            {
                value = values[0];
                conflicted = false;
            }

            if (conflicted)
            {
                GUI.color = new Color(.8f, .8f, .8f);
            }

            value = GUILayout.Toggle(value, title, EditorStyles.toolbarButton, options);

            GUI.color = Color.white;

            return value;
        }

        public static bool ToggleMixed(Rect rect, bool value, bool conflicted, string title)
        {
            if (conflicted)
            {
                GUI.color = new Color(.8f, .8f, .8f);
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
            if (EditorGUIUtility.isProSkin)
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
            style.border = new RectOffset(22, 22, 22, 22);
            style.normal.background = SabreCSGResources.DialogOverlayTexture;
#if UNITY_5_4_OR_NEWER
            style.normal.scaledBackgrounds = new Texture2D[] { SabreCSGResources.DialogOverlayRetinaTexture };
#endif
            style.fontSize = 20;
            style.padding = new RectOffset(15, 15, 15, 15);
            style.alignment = TextAnchor.MiddleCenter;
            style.normal.textColor = Color.red;
            return style;
        }

        public static GUIStyle GetTextFieldStyle1()
        {
            GUIStyle style = new GUIStyle(EditorStyles.textField);
            if (textFieldTexture1 == null)
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
            if (textFieldTexture2 == null)
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
            if (EditorGUIUtility.isProSkin)
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

        public static T? EnumPopupMixed<T>(string label, T[] selected, params GUILayoutOption[] options) where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException("EnumPopupMixed must be passed an enum");
            }

            string[] names = Enum.GetNames(typeof(T));

            int selectedIndex = (int)(object)selected[0];

            for (int i = 1; i < selected.Length; i++)
            {
                if (selectedIndex != (int)(object)selected[i])
                {
                    selectedIndex = names.Length;

                    break;
                }
            }

            bool mixedSelection = (selectedIndex == names.Length);
            // Mixed selection, add a name entry for "Mixed"
            if (mixedSelection)
            {
                Array.Resize(ref names, names.Length + 1);

                int mixedIndex = names.Length - 1;
                names[mixedIndex] = "Mixed";
            }

            int newIndex = EditorGUILayout.Popup(label, selectedIndex, names, options);

            if (mixedSelection && newIndex == names.Length - 1)
            {
                return null;
            }
            else
            {
                return (T)Enum.ToObject(typeof(T), newIndex);
            }
        }

        /// <summary>
        /// Displays a list of enum buttons.
        /// </summary>
        /// <param name="value">The active value enum.</param>
        /// <returns>The enum selected when the user clicks one of the buttons.</returns>
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

        /// <summary>
        /// Displays a list of enum buttons, using only the provided enums.
        /// </summary>
        /// <param name="value">The active value enum.</param>
        /// <param name="enabled">The visible enum values.</param>
        /// <returns>The enum selected when the user clicks one of the buttons.</returns>
        public static T DrawPartialEnumGrid<T>(T value, T[] enabled, params GUILayoutOption[] options) where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException("DrawEnumGrid must be passed an enum");
            }
            GUILayout.BeginHorizontal();

            T[] enabledList = enabled;
            T[] baseList = (T[]) Enum.GetValues(value.GetType());

            for (int i = 0; i < baseList.Length; i++)
            {
                if (Array.IndexOf(enabledList, baseList[i]) == -1) {
                    continue;
                }

                GUIStyle activeStyle;
                if (baseList.Length == 1) // Only one button
                {
                    activeStyle = EditorStyles.miniButton;
                }
                else if (i == 0) // Left-most button
                {
                    activeStyle = EditorStyles.miniButtonLeft;
                }
                else if (i == baseList.Length - 1) // Right-most button
                {
                    activeStyle = EditorStyles.miniButtonRight;
                }
                else // Normal mid button
                {
                    activeStyle = EditorStyles.miniButtonMid;
                }

                bool isActive = (Convert.ToInt32(value) == i);
                //				string displayName = StringHelper.ParseDisplayString(names[i]);
                string displayName = baseList[i].ToString();
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
            if (!sourceFloat.HasValue)
            {
                EditorGUI.showMixedValue = true;
                sourceFloat = 1;
            }

            GUI.SetNextControlName(controlName);

            string sourceString = floatString ?? sourceFloat.Value.ToString();

            string newUScaleString = EditorGUI.TextField(rect, sourceString, style);

            EditorGUI.showMixedValue = false;

            if (GUI.GetNameOfFocusedControl() == controlName)
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

            if (keyboardEnter)
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

        public static void DrawNameTag(Vector2 position, string name)
        {
            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.fixedHeight = 12;
            style.padding = new RectOffset(7, 18, 0, 0);
            style.margin = new RectOffset(0, 0, 0, 0);
            style.normal.background = SabreCSGResources.GroupHeaderTexture;
#if UNITY_5_4_OR_NEWER
            style.normal.scaledBackgrounds = new Texture2D[] { SabreCSGResources.GroupHeaderRetinaTexture };
#endif
            style.font = EditorStyles.miniFont;
            style.fontSize = 9;

            style.border = new RectOffset(10, 32, 1, 1);

            //GUI.DrawTexture(lastRect, EditorGUIUtility.whiteTexture);

            Rect rect = new Rect();
            rect.x = position.x + 1;
            rect.y = position.y + 1;
            rect.width = style.CalcSize(new GUIContent(name)).x + 2;
            rect.height = style.fixedHeight;
            GUI.Box(rect, name, style);
        }

        /// <summary>
        /// Displays a field for layer masks.
        /// </summary>
        /// <param name="label">The label to show.</param>
        /// <param name="mask">The current mask.</param>
        /// <returns>The new mask.</returns>
        public static int LayerMaskField(GUIContent label, int mask)
        {
            System.Collections.Generic.List<string> layerNames = new System.Collections.Generic.List<string>();
            for (int i = 0; i < 32; i++)
            {
                string layerName = UnityEditorInternal.InternalEditorUtility.GetLayerName(i);
                //if (!(layerName == string.Empty))
                //{
                layerNames.Add(layerName);
                //}
            }

            // this currently shows a lot of empty entries.
            // we should figure out which ones are empty, not display those and fix the layer mask accordingly.

            return EditorGUILayout.MaskField(label, mask, layerNames.ToArray());
        }

#if !UNITY_2018_1_OR_NEWER
        // things used by color field:

        private static Type colorField_ColorPickerWindowType;
        private static MethodInfo colorField_HasFocusMethod = null;
        private static PropertyInfo colorField_GetColorMethod = null;

        private static bool ColorField_ColorPickerHasFocus(EditorWindow colorPickerWindow)
        {
            if (colorField_HasFocusMethod == null)
                colorField_HasFocusMethod = typeof(EditorWindow).GetProperty("hasFocus", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).GetGetMethod(true);
            return (bool)colorField_HasFocusMethod.Invoke(colorPickerWindow, null);
        }

        private static Color ColorField_ColorPickerCurrentColor(Type colorPickerWindowType, EditorWindow colorPickerWindow)
        {
            if (colorField_GetColorMethod == null)
                colorField_GetColorMethod = colorPickerWindowType.GetProperty("color", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            return (Color)colorField_GetColorMethod.GetValue(null, null);
        }

        private static Color ColorField_FetchCurrentColor(Color value)
        {
            // fetch the actual color directly through reflection:
            try
            {
                // first find the color picker window type.
                if (colorField_ColorPickerWindowType == null)
                    colorField_ColorPickerWindowType = (typeof(EditorWindow).Assembly.GetType("UnityEditor.ColorPicker"));

                // try finding the current color picker window (should only ever be one).
                UnityEngine.Object[] colorPickerWindows = Resources.FindObjectsOfTypeAll(colorField_ColorPickerWindowType);
                if (colorPickerWindows.Length > 0)
                {
                    EditorWindow colorPickerWindow = (EditorWindow)colorPickerWindows[0];
                    // if the color picker currently has focus:
                    if (ColorField_ColorPickerHasFocus(colorPickerWindow))
                        // fetch the current color.
                        value = ColorField_ColorPickerCurrentColor(colorField_ColorPickerWindowType, colorPickerWindow);
                }
            }
            catch
            {
                // reflection could go wrong in other versions of Unity so we catch any exceptions.
            }

            return value;
        }
#endif

        /// <summary>
        /// Displays a color field and uses extensive reflection hacks to make sure it works inside GUILayout.Window callbacks.
        /// </summary>
        /// <param name="value">The currently selected color value.</param>
        /// <returns>The selected color.</returns>
        public static Color ColorField(Color value)
        {
            // display the broken color field widget and fetch the color manually:
#if !UNITY_2018_1_OR_NEWER
            return ColorField_FetchCurrentColor(EditorGUILayout.ColorField(value));
#else
            return EditorGUILayout.ColorField(value);
#endif
        }

        /// <summary>
        /// Displays a color field and uses extensive reflection hacks to make sure it works inside GUILayout.Window callbacks.
        /// </summary>
        /// <param name="value">The currently selected color value.</param>
        /// <returns>The selected color.</returns>
        public static Color ColorField(GUIContent label, Color value, bool showEyedropper, bool showAlpha, params GUILayoutOption[] options)
        {
            // display the broken color field widget and fetch the color manually:
#if !UNITY_2018_1_OR_NEWER
            return ColorField_FetchCurrentColor(EditorGUILayout.ColorField(label, value, showEyedropper, showAlpha, false, null, options));
#else
            return EditorGUILayout.ColorField(label, value, showEyedropper, showAlpha, false, options);
#endif
        }

        /// <summary>
        /// Make a text field for entering even integers (e.g. when scrolled around it goes 2 4 6 8 10 or 16 14 12 10).
        /// </summary>
        /// <param name="label">Label to display in front of the int field.</param>
        /// <param name="value">The value to edit.</param>
        /// <param name="options">Optional GUIStyle.</param>
        /// <returns>The value entered by the user.</returns>
        public static int EvenIntField(GUIContent label, int value, params GUILayoutOption[] options)
        {
            int previousValue = value;
            value = EditorGUILayout.IntField("Radius", value);
            if (value < 2)
            {
                value = 2;
            }
            else
            {
                if (previousValue < value && (value % 2) != 0)
                    value += 1;
                else if (previousValue > value && (value % 2) != 0)
                    value -= 1;
            }
            return value;
        }
    }
}

#endif