#if UNITY_EDITOR

/* TODO:
 * Search
 */

using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Sabresaurus.SabreCSG.MaterialPalette
{
	public class MaterialPaletteWindow : EditorWindow
	{
		private static MaterialPaletteWindow window;
		private static Material[] mats;
		private static List<GUIContent> labels = new List<GUIContent>();
		private static string[] assetLabels = new string[]{ };
		private static string filter = string.Empty;
		private static MaterialThumbSize mts = MaterialThumbSize.Large;
		private Vector2 previewsScrollPos = Vector2.zero;
		private Vector2 labelsScrollPos = Vector2.zero;

		[MenuItem( "SabreCSG/Material Palette Window", priority = 0 )]
		private static void Init()
		{
			window = EditorWindow.GetWindow<MaterialPaletteWindow>( "Material Palette" );

			window.minSize = new Vector2( 548, 256 );
			window.maxSize = new Vector2( 548, 4096 );

			Load();

			window.Show();
		}

		[InitializeOnLoadMethod]
		private static void Load()
		{
			labels.Clear();
			assetLabels = new string[] { };

			mats = MaterialPaletteHelper.GetAllMaterials( filter );
			assetLabels = MaterialPaletteHelper.GetAssetLabels();

			foreach( Material m in mats )
			{
				labels.Add( new GUIContent( MaterialPaletteHelper.GetMaterialThumb( m ), m.name ) );
			}

			mts = (MaterialThumbSize)EditorPrefs.GetInt( "SabreCSG.MaterialPalette.ThumbSize", (int)MaterialThumbSize.Medium );
		}

		private void OnDestroy()
		{
			EditorPrefs.SetInt( "SabreCSG.MaterialPalette.ThumbSize", (int)mts );
		}

		private void OnGUI()
		{
			EditorGUI.BeginChangeCheck();

			GUILayout.BeginVertical();
			{
				// top toolbar
				DrawToolBar();

				// material grid
				DrawGrid();

				// bottom toolbar
				DrawBottomToolbar();
			}
			GUILayout.EndVertical();

			if( EditorGUI.EndChangeCheck() )
			{
				EditorPrefs.SetInt( "SabreCSG.MaterialPalette.ThumbSize", (int)mts );
			}

			Repaint(); // ensure things are drawn responsively
		}

		private void DrawBottomToolbar()
		{
			GUILayout.Space( 2 );

			GUILayout.BeginVertical( GUILayout.Height( 128 ), GUILayout.ExpandWidth( true ), GUILayout.ExpandHeight( false ) );
			{
				labelsScrollPos = GUILayout.BeginScrollView( labelsScrollPos, false, false );
				{
					for( int i = 0; i < assetLabels.Length; i++ )
					{
						GUILayout.Space( 2 );
						int columnIndex = i % 9;

						if( columnIndex == 0 )
						{
							GUILayout.BeginHorizontal();
							GUILayout.Space( 2 );
						}

						if( GUILayout.Button( assetLabels[i], "Tag TextField" ) )
						{
							filter = assetLabels[i];
							Load();
						}

						if( ( columnIndex == 8 ) || i == assetLabels.Length - 1 )
						{
							GUILayout.EndHorizontal();
						}
					}
				}
				GUILayout.EndScrollView();
			}
			GUILayout.EndVertical();
		}

		private void DrawToolBar()
		{
			GUILayout.BeginHorizontal( EditorStyles.toolbarButton, GUILayout.ExpandWidth( true ) );
			{
				mts = (MaterialThumbSize)EditorGUILayout.EnumPopup( new GUIContent( "", "Thumbnail Size" ), mts, EditorStyles.toolbarDropDown, GUILayout.Width( 100 ) );

				if( filter != string.Empty )
				{
					GUILayout.FlexibleSpace();

					GUI.contentColor = Color.red;

					GUILayout.Label( "Filtered: " + filter );

					GUI.contentColor = Color.white;
				}

				GUILayout.FlexibleSpace();

				if( filter != string.Empty )
				{
					if( GUILayout.Button( "Clear Filter", EditorStyles.toolbarButton ) )
					{
						filter = string.Empty;
						Load();
					}
				}

				if( GUILayout.Button( "Reload", EditorStyles.toolbarButton ) )
				{
					Load();
				}
			}
			GUILayout.EndHorizontal();
		}

		private void DrawGrid()
		{
			GUILayout.BeginVertical();
			{
				GUILayout.BeginHorizontal( "GameViewBackground", GUILayout.ExpandHeight( true ) );
				{
					//BeginWindows();
					//GUILayout.Window( 1000, new Rect( 0, 18, 200, position.height - 18 ), LeftToolbar, "", SabreCSGResources.MaterialPaletteLToolbarBG ).Overlaps( position );
					//EndWindows();

					GUILayout.Space( 2 );

					previewsScrollPos = GUILayout.BeginScrollView( previewsScrollPos, false, false );
					{
						for( int i = 0; i < labels.Count; i++ )
						{
							int numColumns = 4;
							switch( mts )
							{
								case MaterialThumbSize.Small:
									numColumns = 15;
									break;

								case MaterialThumbSize.Medium:
									numColumns = 8;
									break;

								case MaterialThumbSize.Large:
									numColumns = 4;
									break;
							}

							int columnIndex = i % numColumns;

							if( columnIndex == 0 )
							{
								GUILayout.BeginHorizontal();
								GUILayout.Space( 4 );
							}

							if( GUILayout.Button( labels[i], SabreCSGResources.MaterialPaletteAssetPreviewBackground( (int)mts, (int)mts ), GUILayout.Width( (int)mts ), GUILayout.Height( (int)mts ) ) )
							{
								MaterialPaletteHelper.ApplyMaterial( mats[i] );
							}

							//GUILayout.Space( 2 );

							if( ( columnIndex == numColumns - 1 || i == labels.Count - 1 ) )
							{
								GUILayout.EndHorizontal();
							}
						}

						//currentSelectedMaterial = GUILayout.SelectionGrid( currentSelectedMaterial, labels.ToArray(), 4, SabreCSGResources.MaterialPaletteAssetPreviewBackground, GUILayout.Width( position.width - 200 ) );
					}
					GUILayout.EndScrollView();
				}
				GUILayout.EndHorizontal();
			}
			GUILayout.EndVertical();
		}
	}
}

#endif
