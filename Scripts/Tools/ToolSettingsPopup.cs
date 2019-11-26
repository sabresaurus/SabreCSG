#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
    /// <summary>
    /// A multi-purpose popup window used by the tools.
    /// </summary>
    /// <seealso cref="UnityEditor.PopupWindowContent"/>
    public class ToolSettingsPopup : PopupWindowContent
    {
        /// <summary>
        /// Gets or sets the title of the popup window.
        /// </summary>
        /// <value>The title of the popup window.</value>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the width of the popup window.
        /// </summary>
        /// <value>The width of the popup window.</value>
        public float Width { get; set; }

        /// <summary>
        /// Gets the height of the popup window.
        /// </summary>
        /// <value>The height of the popup window.</value>
        public float Height { get { return m_Height; } }

        /// <summary>
        /// Gets or sets the wiki link.
        /// </summary>
        /// <value>The wiki link.</value>
        public string WikiLink { get; set; }

        /// <summary>
        /// Whether an additional repaint has been done when the popup window is first shown. This is
        /// used to accurately calculate the height.
        /// </summary>
        private bool m_DidExtraRepaint = false;

        /// <summary>
        /// The height of the popup window.
        /// </summary>
        private float m_Height = 1.0f;

        /// <summary>
        /// The OnGUI action.
        /// </summary>
        private Action<Rect> m_OnGuiAction;

        /// <summary>
        /// The custom buttons to be shown.
        /// </summary>
        private Dictionary<string, Action> m_ConfirmButtons = new Dictionary<string, Action>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ToolSettingsPopup"/> class.
        /// </summary>
        private ToolSettingsPopup(string title, float width, Action<Rect> onGUI) : base()
        {
            Title = title;
            Width = width;
            m_OnGuiAction = onGUI;
        }

        /// <summary>
        /// Gets the size of the popup window.
        /// </summary>
        /// <returns>Gets the size of the popup window.</returns>
        public override Vector2 GetWindowSize()
        {
            return new Vector2(Width, m_Height);
        }

        /// <summary>
        /// Callback for drawing GUI controls for the popup window.
        /// </summary>
        /// <param name="rect">The rectangle to draw the GUI inside.</param>
        public override void OnGUI(Rect rect)
        {
            // use a named vertical scope for the title and to determine the required popup height.
            NamedVerticalScope scope;
            using (scope = new NamedVerticalScope(Title))
            {
                // configure the scope.
                scope.IsPopupWindow = true;
                scope.OnCloseAction = editorWindow.Close;
                scope.WikiLink = WikiLink;

                // call the custom gui action.
                m_OnGuiAction.Invoke(rect);

                // add any registered buttons.
                foreach (var customButton in m_ConfirmButtons)
                    if (GUILayout.Button(customButton.Key))
                    {
                        customButton.Value.Invoke();
                        editorWindow.Close();
                    }
            }

            // automatically determine the popup height.
            if (scope.ScopeRect.height != 1)
                m_Height = scope.ScopeRect.height + 8;

            // when the dialog is shown for the first time the height is wrong.
            // if the user has the mouse hovering over it then Unity will repaint and fix it.
            // to make sure it's always visible, we force an additional repaint here.
            if (!m_DidExtraRepaint)
            {
                editorWindow.Repaint();
                m_DidExtraRepaint = true;
            }
        }

        /// <summary>
        /// Registers a button to be shown to the user with an action called when it's pressed.
        /// </summary>
        /// <param name="button">The button name to be shown.</param>
        /// <param name="action">The action called when the button is clicked.</param>
        /// <returns>The <see cref="this"/> reference for chaining.</returns>
        public ToolSettingsPopup AddConfirmButton(string button, Action action)
        {
            // add the custom button.
            if (!m_ConfirmButtons.ContainsKey(button))
                m_ConfirmButtons.Add(button, action);
            return this;
        }

        /// <summary>
        /// Sets the wiki link (either a shorthand like 'Tutorial-1' or a full link starting with 'http').
        /// </summary>
        /// <param name="link">The link to the internet page.</param>
        /// <returns>The <see cref="this"/> reference for chaining.</returns>
        public ToolSettingsPopup SetWikiLink(string link)
        {
            // set the wiki link.
            WikiLink = link;
            return this;
        }

        /// <summary>
        /// Shows the tool settings popup window. Call this method last.
        /// </summary>
        /// <returns>The <see cref="this"/> reference for chaining.</returns>
        public ToolSettingsPopup Show()
        {
            PopupWindow.Show(new Rect(Event.current.mousePosition, new Vector2(0, 0)), this);
            return this;
        }

        /// <summary>
        /// Creates a tool settings popup window.
        /// </summary>
        /// <param name="title">The title of the popup window.</param>
        /// <param name="onGUI">The OnGUI contents of the popup window.</param>
        /// <returns>The popup window handle.</returns>
        public static ToolSettingsPopup Create(string title, Action<Rect> onGUI)
        {
            return Create(title, 300, onGUI);
        }

        /// <summary>
        /// Creates a tool settings popup window.
        /// </summary>
        /// <param name="title">The title of the popup window.</param>
        /// <param name="width">The width of the popup window.</param>
        /// <param name="onGUI">The OnGUI contents of the popup window.</param>
        /// <returns>The popup window handle.</returns>
        public static ToolSettingsPopup Create(string title, float width, Action<Rect> onGUI)
        {
            return new ToolSettingsPopup(title, width, onGUI);
        }
    }
}

#endif