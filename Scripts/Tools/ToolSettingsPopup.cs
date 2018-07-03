#if UNITY_EDITOR

using System;
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
        /// The height of the popup window.
        /// </summary>
        private float m_Height = 20.0f;

        /// <summary>
        /// The OnGUI action.
        /// </summary>
        private Action<Rect> m_OnGuiAction;

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
                m_OnGuiAction.Invoke(rect);
            }

            // automatically determine the popup height.
            if (scope.ScopeRect.height != 1)
                m_Height = scope.ScopeRect.height + 8;
        }

        /// <summary>
        /// Shows a tool settings popup window.
        /// </summary>
        /// <param name="title">The title of the popup window.</param>
        /// <param name="onGUI">The OnGUI contents of the popup window.</param>
        /// <returns>The popup window handle.</returns>
        public static ToolSettingsPopup Show(string title, Action<Rect> onGUI)
        {
            return Show(title, 300, onGUI);
        }

        /// <summary>
        /// Shows a tool settings popup window.
        /// </summary>
        /// <param name="title">The title of the popup window.</param>
        /// <param name="width">The width of the popup window.</param>
        /// <param name="onGUI">The OnGUI contents of the popup window.</param>
        /// <returns>The popup window handle.</returns>
        public static ToolSettingsPopup Show(string title, float width, Action<Rect> onGUI)
        {
            ToolSettingsPopup popup;
            PopupWindow.Show(new Rect(Event.current.mousePosition, new Vector2(0, 0)), popup = new ToolSettingsPopup(title, width, onGUI));
            return popup;
        }
    }
}

#endif