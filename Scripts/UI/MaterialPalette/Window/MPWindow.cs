#if UNITY_EDITOR

using System.Collections.Generic;
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

		private static MPWindow window;
		private string[] labels = new string[]{ };
		private List<Material> untagged = new List<Material>();
		private List<Material> tagged = new List<Material>();
		private List<Material> current = new List<Material>();

		private int previewSize = 128;
		private string filter = "";
		private Vector2 scrollPosGrid = Vector2.zero;

		public void Load()
		{
			labels = MPHelper.GetAssetLabels( filter, out untagged, out tagged );
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
				scrollPosGrid = GUILayout.BeginScrollView( scrollPosGrid, false, false, GUILayout.ExpandHeight( true ) );
				{
					int numColumns = 4;

					if( previewSize <= 32 )
						numColumns = 15;
					if( previewSize <= 64 )
						numColumns = 8;
					if( previewSize <= 128 )
						numColumns = 4;

					if( previewSize > 128 )
						previewSize = 128;
					if( previewSize < 32 )
						previewSize = 32;

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
							pt.material = new Material( Shader.Find( "Standard" ) );
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
					if( GUILayout.Button( "-", EditorStyles.miniButtonLeft ) )
					{
						previewSize = previewSize / 2;
					}
					if( GUILayout.Button( "+", EditorStyles.miniButtonRight ) )
					{
						previewSize = previewSize * 2;
					}
					GUILayout.Label( "Preview Size [" + previewSize + "]", EditorStyles.miniLabel );
				}
				MPGUI.EndStatusBar( EditorGUIUtility.IconContent( "console.infoicon.sml" ).image, sb.ToString() );
			}
			GUILayout.EndVertical();
		}
	}
}

#endif
