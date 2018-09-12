#if UNITY_EDITOR

using System;

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

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
			bool isPressed = false;

			if( onLeftClick != null && onRightClick != null )
			{
				if( GUILayout.Button( text, s, GUILayout.Width( size.x ), GUILayout.Height( size.y ), GUILayout.ExpandHeight( false ), GUILayout.ExpandWidth( false ) ) )
				{
					if( Event.current.button == 1 )
					{
						onRightClick();
						isPressed = false;
					}
					onLeftClick();
					isPressed = true;
				}
			}

			return isPressed;
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
			bool isPressed = false;

			if( onLeftClick != null && onRightClick != null
				&& leftClickParam != null && rightClickParam != null )
			{
				if( GUILayout.Button( text, s, GUILayout.Width( size.x ), GUILayout.Height( size.y ), GUILayout.ExpandHeight( false ), GUILayout.ExpandWidth( false ) ) )
				{
					if( Event.current.button == 1 )
					{
						onRightClick( rightClickParam );
						isPressed = false;
					}
					else
					{
						onLeftClick( leftClickParam );
						isPressed = true;
					}
				}
			}

			return isPressed;
		}

		/// <summary>
		/// Creates a button with a middle, or alt + RMB, and right-click context option.
		/// </summary>
		/// <param name="text">The text to display, with optional tooltip.</param>
		/// <param name="size">The size of this button.</param>
		/// <param name="onLeftClick">The left-click delegate to execute.</param>
		/// <param name="onRightClick">The right-click delegate to execute.</param>
		/// <param name="onMiddleClick">The middle or alt + rmb click delegate to execute.</param>
		/// <param name="leftClickParam">The object for onLeftClick to handle.</param>
		/// <param name="rightClickParam">The object for onRightClick to handle.</param>
		/// <param name="middleClickParam">The object for onMiddleClick to handle.</param>
		/// <param name="style">Optional style to display the button as.</param>
		/// <returns></returns>
		public static bool ContextButton( GUIContent text, Vector2 size,
			Action<object> onLeftClick, Action<object> onRightClick, Action<object> onMiddleClick,
			object leftClickParam, object rightClickParam, object middleClickParam,
			GUIStyle style = null )
		{
			GUIStyle s = style ?? "Button";
			bool isPressed = false;

			if( onLeftClick != null && onRightClick != null && onMiddleClick != null
				&& leftClickParam != null && rightClickParam != null && middleClickParam != null )
			{
				if( GUILayout.Button( text, s, GUILayout.Width( size.x ), GUILayout.Height( size.y ), GUILayout.ExpandHeight( false ), GUILayout.ExpandWidth( false ) ) )
				{
					if( Event.current.button == 1 && !Event.current.alt )
					{
						onRightClick( rightClickParam );
						isPressed = false;
					}
					else if( Event.current.button == 2 || Event.current.button == 1 && Event.current.alt )
					{
						onMiddleClick( middleClickParam );
						isPressed = false;
					}
					else
					{
						onLeftClick( leftClickParam );
						isPressed = true;
					}
				}
			}

			return isPressed;
		}

		public static void BeginStatusBar( GUIStyle style )
		{
			if( style == null )
				style = "OL box";

			GUILayout.BeginHorizontal( style, GUILayout.Height( 20 ) );
		}

		public static void EndStatusBar( string textContent )
		{
			GUILayout.FlexibleSpace();
			GUILayout.Label( textContent );
			GUILayout.EndHorizontal();
		}

		public static void EndStatusBar( GUIContent textContent )
		{
			GUILayout.FlexibleSpace();
			GUILayout.Label( textContent );
			GUILayout.EndHorizontal();
		}

		public static void EndStatusBar( Texture icon, string tooltip )
		{
			GUILayout.FlexibleSpace();
			GUILayout.Label( new GUIContent( icon, tooltip ) );
			GUILayout.EndHorizontal();
		}

		//new GUIContent( textContent.image, textContent.tooltip )
	}
}

#endif
