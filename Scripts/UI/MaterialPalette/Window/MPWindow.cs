#if UNITY_EDITOR

// TODO: Filter

using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Sabresaurus.SabreCSG.MaterialPalette
{
	public class MPWindow : EditorWindow
	{
		private int MaterialCount
		{
			get
			{
				return untagged.Count + tagged.Count;
			}
		}

		private int CurrentCount
		{
			get
			{
				return current.Count;
			}
		}

		private int UntaggedCount
		{
			get
			{
				return untagged.Count;
			}
		}

		private int TaggedCount
		{
			get
			{
				return tagged.Count;
			}
		}

		private List<Material> All
		{
			get
			{
				List<Material> final = new List<Material>();
				final.AddRange( untagged );
				final.AddRange( tagged );

				return final;
			}
		}

		private static MPWindow window;
		private string[] labels = new string[]{ };
		private List<Material> untagged = new List<Material>();
		private List<Material> tagged = new List<Material>();
		private List<Material> current = new List<Material>();
		private List<Material> all = new List<Material>();
		private int previewSize = 64;
		private int tagListWidth = 128;
		private string filter = "";
		private bool tagListVisible = false;
		private Vector2 scrollPosGrid = Vector2.zero;
		private Vector2 labelsScrollPos;

		public void Load()
		{
			labels = MPHelper.GetAssetLabels( filter, out untagged, out tagged );
			current = untagged;
		}

		[MenuItem( "SabreCSG/Material Palette Window", priority = 0 )]
		private static void Init()
		{
			window = EditorWindow.GetWindow<MPWindow>( false, "Material Palette" );

			window.minSize = new Vector2( 280, 256 );
			window.maxSize = new Vector2( 4096, 4096 );
			window.Load();
			window.Show();
		}

		private void Update()
		{
			if( filter != string.Empty )
			{
			}
		}

		private void OnGUI()
		{
			GUILayout.BeginVertical( "GameViewBackground", GUILayout.Height( window.position.height ), GUILayout.ExpandHeight( true ) );
			{
				GUILayout.BeginHorizontal( GUILayout.ExpandHeight( true ) );
				{
					DrawGrid();
				}
				GUILayout.EndHorizontal();

				DrawStatusBar();
			}
			GUILayout.EndVertical();

			window.Repaint();
		}

		private void DrawGrid()
		{
			GUILayout.BeginVertical( GUILayout.Width( window.position.width - ( ( tagListVisible ) ? tagListWidth : 0 ) ), GUILayout.Height( window.position.height - 32 ) ); //Styles.MPScrollViewBackground );
			{
				scrollPosGrid = GUILayout.BeginScrollView( scrollPosGrid, false, false, GUILayout.Width( window.position.width - ( ( tagListVisible ) ? tagListWidth : 0 ) ) );
				{
					int numColumns = ( (int)window.position.width - ( ( tagListVisible ) ? tagListWidth : 0 ) ) / ( previewSize + 4 );

					for( int i = 0; i < current.Count; i++ )
					{
						int columnIndex = i % numColumns;

						if( columnIndex == 0 )
						{
							GUILayout.BeginHorizontal();
							GUILayout.Space( 4 );
						}

						if( current[i] != null )
						{
							PreviewTile pt = new PreviewTile();
							pt.parent = this;
							pt.material = current[i];
							pt.materialThumbSize = previewSize;
							pt.labelFontColor = new Color32( 255, 255, 255, 255 );
							pt.Draw( GUILayoutUtility.GetLastRect() );
						}
						else
						{
							PreviewTile pt = new PreviewTile();
							pt.parent = this;
							pt.material = new Material( Shader.Find( "Hidden/InternalErrorShader" ) );
							pt.material.name = "Deleted Material";
							pt.materialThumbSize = previewSize;
							pt.labelFontColor = new Color32( 255, 0, 0, 255 );
							pt.Draw( GUILayoutUtility.GetLastRect() );
						}

						if( ( columnIndex == numColumns - 1 || i == current.Count - 1 ) )
						{
							GUILayout.EndHorizontal();
						}
					}
				}
				GUILayout.EndScrollView();
			}
			GUILayout.EndVertical();

			if( tagListVisible )
			{
				GUILayout.BeginVertical( "Box", GUILayout.Width( tagListWidth ) );
				{
					GUILayout.Label( new GUIContent( "Tags", "Project-defined tags assigned by the user when importing an asset." ), EditorStyles.miniLabel );

					labelsScrollPos = GUILayout.BeginScrollView( labelsScrollPos, Styles.MPScrollViewBackground );
					{
						for( int i = 0; i < labels.Length; i++ )
						{
							EditorGUI.indentLevel = 0;

							if( i % 2 != 0 ) // show odd background
							{
								GUI.backgroundColor = new Color32( 150, 150, 150, 255 );
							}
							else // show even background
							{
								GUI.backgroundColor = new Color32( 200, 200, 200, 255 );
							}

							if( GUILayout.Button( labels[i], Styles.MPAssetTagLabel, GUILayout.ExpandHeight( false ), GUILayout.ExpandWidth( true ) ) )
							{
								filter = labels[i];
							}
							GUI.backgroundColor = Color.white;
						}
					}
					GUILayout.EndScrollView();
				}
				GUILayout.EndVertical();
			}
		}

		private void DrawStatusBar()
		{
			GUILayout.FlexibleSpace();
			StringBuilder sb = new StringBuilder();
			sb.Append( MaterialCount );
			sb.Append( " materials\n" );
			sb.Append( UntaggedCount );
			sb.Append( " untagged\n" );
			sb.Append( TaggedCount );
			sb.Append( " tagged\n" );
			sb.Append( CurrentCount );
			sb.Append( " filtered" );

			MPGUI.BeginStatusBar( "ProgressBarBack" );
			{
				previewSize = (int)GUILayout.HorizontalSlider( previewSize, 32, 128, GUILayout.Width( 100 ) );
				GUILayout.Label( previewSize.ToString(), GUILayout.Width( 24 ) );

				GUILayout.FlexibleSpace();
				tagListVisible = GUILayout.Toggle( tagListVisible, "", "OL Toggle" );
			}
			MPGUI.EndStatusBar( EditorGUIUtility.IconContent( "console.infoicon.sml" ).image, sb.ToString() );
		}
	}
}

#endif
