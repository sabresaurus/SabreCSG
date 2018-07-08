#if UNITY_EDITOR

using System;

using UnityEngine;
using UnityEditor;

namespace Sabresaurus.SabreCSG.MaterialPalette
{
	/// <summary>
	/// Class handling custom GUILayout elements.
	/// </summary>
	public static class MPGUI
	{
		/// <summary>
		/// Creates a button with a right-click context option.
		/// </summary>
		/// <param name="text">The text to display, with optional tooltip.</param>
		/// <param name="size">The size of this button.</param>
		/// <param name="onLeftClick">The left-click delegate to execute.</param>
		/// <param name="onRightClick">The right-click delegate to execute.</param>
		/// <param name="style">Optional style to display the button as.</param>
		/// <returns></returns>
		public static bool ContextButton( GUIContent text, Vector2 size, Action onLeftClick, Action onRightClick, GUIStyle style = null )
		{
			GUIStyle s = style ?? "Button";

			if( onLeftClick != null && onRightClick != null )
			{
				if( GUILayout.Button( text, s, GUILayout.Width( size.x ), GUILayout.Height( size.y ), GUILayout.ExpandHeight( false ), GUILayout.ExpandWidth( false ) ) )
				{
					if( Event.current.button == 1 )
					{
						onRightClick();
						return false;
					}
					else
					{
						onLeftClick();
						return true;
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Creates a button with a right-click context option.
		/// </summary>
		/// <param name="text">The text to display, with optional tooltip.</param>
		/// <param name="size">The size of this button.</param>
		/// <param name="onLeftClick">The left-click delegate to execute.</param>
		/// <param name="onRightClick">The right-click delegate to execute.</param>
		/// <param name="leftClickParam">The object for onLeftClick to handle.</param>
		/// <param name="rightClickParam">The object for onRightClick to handle.</param>
		/// <param name="style">Optional style to display the button as.</param>
		/// <returns></returns>
		public static bool ContextButton( GUIContent text, Vector2 size,
			Action<object> onLeftClick, Action<object> onRightClick,
			object leftClickParam, object rightClickParam,
			GUIStyle style = null )
		{
			GUIStyle s = style ?? "Button";

			if( onLeftClick != null && onRightClick != null )
			{
				if( GUILayout.Button( text, s, GUILayout.Width( size.x ), GUILayout.Height( size.y ), GUILayout.ExpandHeight( false ), GUILayout.ExpandWidth( false ) ) )
				{
					if( Event.current.button == 1 )
					{
						onRightClick( rightClickParam );
						return false;
					}
					else
					{
						onLeftClick( leftClickParam );
						return true;
					}
				}
			}

			return false;
		}
	}
}

#endif
