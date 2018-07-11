#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
    public class NamedVerticalScope : GUILayout.VerticalScope
    {
        private const int HEIGHT = 12;
        private const int SPACING = 2;

        private string name;

        private Rect scopeRect;

        /// <summary>
        /// Gets the scope rectangle.
        /// </summary>
        /// <value>The scope rectangle.</value>
        public Rect ScopeRect { get { return scopeRect; } }

        /// <summary>
        /// Gets or sets a value indicating whether this named vertical scope is a popup window.
        /// </summary>
        /// <value><c>true</c> if this named vertical scope is a popup window; otherwise, <c>false</c>.</value>
        public bool IsPopupWindow { get; set; }

        /// <summary>
        /// Gets or sets the on close action.
        /// </summary>
        /// <value>The on close action.</value>
        public Action OnCloseAction { get; set; }

        /// <summary>
        /// Gets or sets the wiki link (either a shorthand like 'Tutorial-1' or a full link starting with 'http').
        /// </summary>
        /// <value>The link to the internet page.</value>
        public string WikiLink { get; set; }

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
            scopeRect = lastRect;
            //GUI.DrawTexture(lastRect, EditorGUIUtility.whiteTexture);

            lastRect.x += 1;
            lastRect.y += 1;
            lastRect.height = style.fixedHeight;
            lastRect.width = style.CalcSize(new GUIContent(name)).x;
            GUI.Box(lastRect, name, style);

            // show actions toolbar:
            Rect buttonRect = scopeRect;
            float offset = buttonRect.width - 2;
            buttonRect.width = 12;
            buttonRect.height = 12;
            buttonRect.y += 2;
            buttonRect.x = offset;
            buttonRect.x -= IsPopupWindow ? 10 : 0;

            // close button
            if (OnCloseAction != null)
            {
                if (GUI.Button(buttonRect, new GUIContent(SabreCSGResources.GroupHeaderButtonCloseTexture), GUIStyle.none))
                    OnCloseAction.Invoke();
                buttonRect.x -= 14;
            }

            // help button
            if (!string.IsNullOrEmpty(WikiLink))
            {
                if (GUI.Button(buttonRect, new GUIContent(SabreCSGResources.GroupHeaderButtonHelpTexture), GUIStyle.none))
                {
                    if (WikiLink.StartsWith("http"))
                        Application.OpenURL(WikiLink);
                    else
                        Application.OpenURL("https://github.com/sabresaurus/SabreCSG/wiki/" + WikiLink);
                }

                buttonRect.x -= 14;
            }
        }
    }
}

#endif