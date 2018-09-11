#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Sabresaurus.SabreCSG.MaterialPalette
{
	public class MPWindow : EditorWindow
	{
		private static MPWindow window;

		public void Load()
		{
		}

		[MenuItem( "SabreCSG/Material Palette Window", priority = 0 )]
		private static void Init()
		{
			window = EditorWindow.GetWindow<MPWindow>( true, "Material Palette" );

			window.minSize = new Vector2( 280, 256 );
			window.maxSize = new Vector2( 4096, 4096 );
			window.Load();
			window.Show();
		}

		private void OnDestroy()
		{
		}

		private void OnGUI()
		{
			GUILayout.BeginVertical( Styles.MPScrollViewBackground );
			{
				GUILayout.FlexibleSpace();
				MPGUI.BeginStatusBar( "Set up tooltip text", "ProgressBarBack" );
				{
					GUILayout.Label( "Preview Size", EditorStyles.miniLabel );
					GUILayout.Button( "-", EditorStyles.miniButtonLeft );
					GUILayout.Button( "+", EditorStyles.miniButtonRight );
				}
				MPGUI.EndStatusBar();
			}
			GUILayout.EndVertical();
		}
	}
}

#endif
