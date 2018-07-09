#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace Sabresaurus.SabreCSG.MaterialPalette
{
	public class PreviewTile
	{
		public ThumbSize materialThumbSize = ThumbSize.Medium;
		public Material material = null;
		public Color32 labelFontColor = Color.yellow;
		public bool renderSpherePreview = false;
		public MaterialPaletteWindow parent;

		public void Draw( Rect lastRect )
		{
			Vector2 thumbSize = new Vector2( (int)materialThumbSize, (int)materialThumbSize );

			lastRect.width -= 4;
			lastRect.height -= 4;

			lastRect.x += 2;
			lastRect.y += 2;

			if( material.name == "Deleted Material" )
			{
				MPGUI.ContextButton( new GUIContent( "", material.name ),
					new Vector2( thumbSize.x, thumbSize.y ),
					parent.Load, parent.Load,
					Styles.MPAssetPreviewBackground( (int)thumbSize.x, (int)thumbSize.y ) );
			}
			else
			{
				MPGUI.ContextButton( new GUIContent( "", material.name ),
					new Vector2( thumbSize.x, thumbSize.y ),
					ApplyMaterial, DisplayLabelPopup,
					material, parent,
					Styles.MPAssetPreviewBackground( (int)thumbSize.x, (int)thumbSize.y ) );
			}

			// material preview
			RenderThumb( GUILayoutUtility.GetLastRect() );

			lastRect = GUILayoutUtility.GetLastRect();// get the thumb rect
			Rect labelRect = lastRect;

			labelRect.y += thumbSize.y - 16;

			// material label
			if( !( lastRect.Contains( Event.current.mousePosition ) ) )
			{
				switch( materialThumbSize )
				{
					case ThumbSize.Large:
						GUI.contentColor = labelFontColor;
						GUI.Label( labelRect, material.name, Styles.MPAssetPreviewLabel );
						GUI.contentColor = Color.white;
						break;

					case ThumbSize.Medium:
						GUI.contentColor = labelFontColor;
						GUI.Label( labelRect, material.name, Styles.MPAssetPreviewLabel );
						GUI.contentColor = Color.white;
						break;

					case ThumbSize.Small:
						break;
				}
			}

			// hightlight
			if( lastRect.Contains( Event.current.mousePosition ) )
			{
				RenderHightlightOutline( GUILayoutUtility.GetLastRect() );
			}
		}

		private void RenderHightlightOutline( Rect lastRect )
		{
			Material m = new Material( Shader.Find( "Unlit/Color" ) );
			m.color = new Color32( 255, 0, 255, 255 );

			m.SetPass( 0 );

			GL.PushMatrix();
			GL.LoadPixelMatrix();
			GL.Begin( GL.LINES );
			GL.Color( m.color );

			lastRect.width += lastRect.x;
			lastRect.height += lastRect.y;

			GL.Vertex3( lastRect.x, lastRect.y, 0 ); // left
			GL.Vertex3( lastRect.x, lastRect.height, 0 );

			GL.Vertex3( lastRect.width, lastRect.height, 0 ); // right
			GL.Vertex3( lastRect.width, lastRect.y, 0 );

			GL.Vertex3( lastRect.x, lastRect.yMin, 0 ); // top
			GL.Vertex3( lastRect.width, lastRect.yMin, 0 );

			GL.Vertex3( lastRect.x, lastRect.height, 0 );
			GL.Vertex3( lastRect.width, lastRect.height, 0 );

			GL.End();
			GL.PopMatrix();
		}

		private void RenderThumb( Rect rect )
		{
			if( material != null )
			{
				if( material.HasProperty( "_MainTex" ) && !renderSpherePreview )
				{
					if( material.GetTexture( "_MainTex" ) != null )
					{
						GUI.Label( rect, material.GetTexture( "_MainTex" ) );
					}
					else
					{
						if( material.HasProperty( "_Color" ) )
						{
							Color col = material.GetColor( "_Color" ).gamma;
							GUI.color = col;
						}
						else
						{
							GUI.Label( rect, Styles.MPNoTexture );
						}

						if( material.HasProperty( "_EmissionColor" ) )
						{
							Color col = material.GetColor( "_EmissionColor" ).gamma;
							if( col != Color.black )
							{
								GUI.color = col;
							}
							else
							{
								GUI.Label( rect, Styles.MPNoTexture );
							}
						}

						GUI.DrawTexture( rect, EditorGUIUtility.whiteTexture );
						GUI.color = Color.white;
					}
				}
				else if( renderSpherePreview )
				{
					GUI.DrawTexture( rect, GetMaterialThumb() ?? SabreCSGResources.ClearTexture );
				}
				else
				{
					GUI.Label( rect, Styles.MPNoTexture );
				}
			}
		}

		private string[] FindTags()
		{
			string[] guids = AssetDatabase.FindAssets( "t:Material " + material.name );
			string asset = AssetDatabase.GUIDToAssetPath( guids[0] );
			Material mat = (Material)AssetDatabase.LoadMainAssetAtPath( asset );

			return AssetDatabase.GetLabels( mat );
		}

		private Texture2D GetMaterialThumb()
		{
			if( !material.HasProperty( "_MainTex" ) )
			{
				return Styles.MPNoTexture;
			}

			return AssetPreview.GetAssetPreview( material );
		}

		private void ApplyMaterial( object mat )
		{
			CSGModel activeModel = CSGModel.GetActiveCSGModel();
			Material m = (Material)mat;

			if( activeModel != null )
			{
				SurfaceEditor se = (SurfaceEditor)activeModel.GetTool( MainMode.Face );
				se.SetSelectionMaterial( m );

				SceneView.lastActiveSceneView.ShowNotification( new GUIContent( "Applied Material: \n" + m.name ) );
			}
		}

		private void DisplayLabelPopup( object parent )
		{
			MPLabelEditorWindow mp = EditorWindow.GetWindow<MPLabelEditorWindow>( true, "Tag Editor - " + material.name );
			MaterialPaletteWindow mpw = (MaterialPaletteWindow)parent;

			mp.material = material;
			mp.parent = mpw;
			mp.existingMaterialLabels = FindTags();

			for( int i = 0; i < mp.existingMaterialLabels.Length; i++ )
			{
				mp.labelsToAdd.Add( mp.existingMaterialLabels[i] );
			}

			Rect popupLocation = new Rect( mpw.position.x - 400,
											mpw.position.y - ( ( mpw.position.height * 0.5f ) + 7 ),
											400, 300 );
			mp.ShowAsDropDown( popupLocation, new Vector2( 400, 300 ) );
		}
	}
}

#endif
