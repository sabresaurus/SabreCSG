#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Sabresaurus.SabreCSG
{
    public class NamedVerticalScope : GUILayout.VerticalScope
    {
        const int HEIGHT = 12;
        const int SPACING = 2;

        string name;

        public NamedVerticalScope(string name)
            : base(EditorStyles.helpBox)
        {
            this.name = name;
            GUILayout.Space(HEIGHT + SPACING);
        }

        protected override void CloseScope()
        {
            base.CloseScope();

            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.fixedHeight = HEIGHT;
            style.padding = new RectOffset(7, 18, 0, 0);
            style.margin = new RectOffset(0, 0, 0, 0);
            style.normal.background = SabreCSGResources.GroupHeaderTexture;
#if UNITY_5_4_OR_NEWER
			style.normal.scaledBackgrounds = new Texture2D[] { SabreCSGResources.GroupHeaderRetinaTexture };
#endif
            style.font = EditorStyles.miniFont;
            style.fontSize = 9;

            style.border = new RectOffset(10, 32, 1, 1);

            Rect lastRect = GUILayoutUtility.GetLastRect();
            //GUI.DrawTexture(lastRect, EditorGUIUtility.whiteTexture);

            lastRect.x += 1;
            lastRect.y += 1;
            lastRect.height = style.fixedHeight;
            lastRect.width = style.CalcSize(new GUIContent(name)).x;
            GUI.Box(lastRect, name, style);
        }
    }
}
#endif