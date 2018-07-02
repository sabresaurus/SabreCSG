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
		private static string[] assetLabels = new string[]{ };
		private static string filter = string.Empty;
		private static bool onlyUsedTags = false;
		private static MaterialThumbSize mts = MaterialThumbSize.Large;
		private Vector2 previewsScrollPos = Vector2.zero;
		private Vector2 labelsScrollPos = Vector2.zero;

		[MenuItem( "SabreCSG/Material Palette Window", priority = 0 )]
		private static void Init()
		{
			window = EditorWindow.GetWindow<MaterialPaletteWindow>( true, "Material Palette" );

			window.minSize = new Vector2( 650, 256 );
			window.maxSize = new Vector2( 650, 4096 );

			EditorApplication.update += window.Repaint;

			Load();

			window.Show();
		}

		[InitializeOnLoadMethod]
		private static void Load()
		{
			// ensure all arrays and lists are clear, and upate them
			LoadAssetTags();

			mats = MaterialPaletteHelper.GetAllMaterials( filter );

			mts = (MaterialThumbSize)EditorPrefs.GetInt( "SabreCSG.MaterialPalette.ThumbSize", (int)MaterialThumbSize.Medium );
			onlyUsedTags = EditorPrefs.GetBool( "SabreCSG.MaterialPalette.OnlyUsedTags", false );

			Debug.Log( "Loaded: [" + mats.Length + "] materials, with tag filter [" + filter + "]" );
		}

		private static void LoadAssetTags()
		{
			assetLabels = new string[] { };
			assetLabels = MaterialPaletteHelper.GetAssetLabels( onlyUsedTags );
		}

		private void OnDestroy()
		{
			EditorPrefs.SetInt( "SabreCSG.MaterialPalette.ThumbSize", (int)mts );
			EditorPrefs.SetBool( "SabreCSG.MaterialPalette.OnlyUsedTags", onlyUsedTags );
			EditorApplication.update -= Repaint;
		}

		private void OnGUI()
		{
			EditorGUI.BeginChangeCheck();

			GUILayout.BeginVertical();
			{
				// top toolbar
				DrawToolBar();

				GUILayout.BeginHorizontal();
				{
					// material grid
					DrawGrid();

					// tag list
					DrawTagList();
				}
				GUILayout.EndHorizontal();

				// status bar
				GUILayout.BeginHorizontal( GUILayout.Height( 16 ) );
				{
					GUILayout.Space( 4 );
					GUILayout.Label( "Materials: " + mats.Length );

					GUILayout.FlexibleSpace();

					GUILayout.Label( new GUIContent( "Used Tags Only", "Show only asset tag labels used in the project." ) );

					bool oldUsedTags;
					onlyUsedTags = EditorGUILayout.Toggle( oldUsedTags = onlyUsedTags, GUILayout.Width( 16 ) );

					if( oldUsedTags != onlyUsedTags )
						LoadAssetTags();

					GUILayout.Space( 4 );
				}
				GUILayout.EndHorizontal();
			}
			GUILayout.EndVertical();

			// update any prefs when the GUI changes, to ensure things are saved.
			if( EditorGUI.EndChangeCheck() )
			{
				EditorPrefs.SetInt( "SabreCSG.MaterialPalette.ThumbSize", (int)mts );
				EditorPrefs.SetBool( "SabreCSG.MaterialPalette.OnlyUsedTags", onlyUsedTags );
			}
		}

		// tags list, etc.
		private void DrawTagList()
		{
			GUILayout.BeginVertical( GUILayout.Width( 98 ), GUILayout.ExpandWidth( true ), GUILayout.ExpandHeight( true ) );
			{
				string tagLabel = onlyUsedTags ? "Tags (Used Only)" : "Tags";

				GUILayout.Label( new GUIContent( tagLabel, "Project-defined tags assigned by the user when importing an asset." ), EditorStyles.miniLabel );

				labelsScrollPos = GUILayout.BeginScrollView( labelsScrollPos, "CurveEditorBackground" );
				{
					for( int i = 0; i < assetLabels.Length; i++ )
					{
						EditorGUI.indentLevel = 0;

						if( i % 2 != 0 ) // show odd background
						{
							GUI.backgroundColor = new Color32( 170, 170, 170, 255 );
						}
						else // show even background
						{
							GUI.backgroundColor = new Color32( 200, 200, 200, 255 );
						}

						if( GUILayout.Button( assetLabels[i], SabreCSGResources.MPAssetTagLabel, GUILayout.ExpandHeight( false ), GUILayout.ExpandWidth( true ) ) )
						{
							filter = assetLabels[i];
							Load();
						}
						GUI.backgroundColor = Color.white;
					}
				}
				GUILayout.EndScrollView();
			}
			GUILayout.EndVertical();
		}

		// top toolbar - refresh, thumbnail size, etc.
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

		// preview area - thumbnails, etc.
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
						for( int i = 0; i < mats.Length; i++ )
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

							if( GUILayout.Button( new GUIContent( "", mats[i].name ), SabreCSGResources.MPAssetPreviewBackground( (int)mts, (int)mts ), GUILayout.Width( (int)mts ), GUILayout.Height( (int)mts ) ) )
							{
								ApplyMaterial( mats[i] );
							}
							MaterialPaletteHelper.DrawPreviewThumb( mats[i], GUILayoutUtility.GetLastRect(), mts );
							//if( Event.current.type == EventType.Repaint )

							//GUILayout.Space( 2 );

							if( ( columnIndex == numColumns - 1 || i == mats.Length - 1 ) )
							{
								GUILayout.EndHorizontal();
							}
						}
					}
					GUILayout.EndScrollView();
				}
				GUILayout.EndHorizontal();
			}
			GUILayout.EndVertical();
		}

		/// <summary>
		/// Apply a material to the selected CSGModel face.
		/// </summary>
		/// <param name="mat"></param>
		private void ApplyMaterial( Material mat )
		{
			CSGModel activeModel = CSGModel.GetActiveCSGModel();

			if( activeModel != null )
			{
				SurfaceEditor se = (SurfaceEditor)activeModel.GetTool( MainMode.Face );
				se.SetSelectionMaterial( mat );

				SceneView.lastActiveSceneView.ShowNotification( new GUIContent( "Applied Material: \n" + mat.name ) );
			}
		}
	}
}

#endif
