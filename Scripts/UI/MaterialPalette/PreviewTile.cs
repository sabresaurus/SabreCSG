#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace Sabresaurus.SabreCSG.MaterialPalette
{
	public class PreviewTile
	{
		public MaterialThumbSize materialThumbSize = MaterialThumbSize.Medium;
		public Material material = null;
		public Color32 labelFontColor = Color.yellow;
		public bool renderSpherePreview = false;

		public void Draw( Rect lastRect, MaterialPaletteWindow parent )
		{
			if( material == null )
			{
				Debug.LogError( "Material is null! Please assign a material using PreviewTile.material." );
				return;
			}

			Vector2 thumbSize = new Vector2( (int)materialThumbSize, (int)materialThumbSize );

			lastRect.width -= 4;
			lastRect.height -= 4;

			lastRect.x += 2;
			lastRect.y += 2;

			// assign material button
			if( GUILayout.Button( new GUIContent( "", material.name ),
				Styles.MPAssetPreviewBackground( (int)thumbSize.x, (int)thumbSize.y ),
				GUILayout.Width( (int)thumbSize.x ), GUILayout.Height( (int)thumbSize.y ) ) )
			{
				if( Event.current.button == 1 )
				{
					MaterialPaletteTagPopup mp = EditorWindow.GetWindow<MaterialPaletteTagPopup>( true, "Tag Editor - " + material.name );
					mp.material = material;
					mp.parent = parent;
					mp.existingMaterialLabels = FindTags();

					for( int i = 0; i < mp.existingMaterialLabels.Length; i++ )
					{
						mp.labelsToAdd.Add( mp.existingMaterialLabels[i] );
					}

					Rect popupLocation = new Rect( parent.position.x - 400,
													parent.position.y - ( ( parent.position.height * 0.5f ) + 7 ),
													400, 300 );
					mp.ShowAsDropDown( popupLocation, new Vector2( 400, 300 ) );
				}
				else
					ApplyMaterial( material );
			}

			// material preview
			RenderThumb( GUILayoutUtility.GetLastRect() );

			lastRect = GUILayoutUtility.GetLastRect();// get the thumb rect
			Rect labelRect = lastRect;

			labelRect.y += thumbSize.y - 16;

			// material label
			if( !( Event.current.type == EventType.Repaint
					&& lastRect.Contains( Event.current.mousePosition ) ) )
			{
				switch( materialThumbSize )
				{
					case MaterialThumbSize.Large:
						GUI.contentColor = labelFontColor;
						GUI.Label( labelRect, material.name, Styles.MPAssetPreviewLabel );
						GUI.contentColor = Color.white;
						break;

					case MaterialThumbSize.Medium:
						GUI.contentColor = labelFontColor;
						GUI.Label( labelRect, material.name, Styles.MPAssetPreviewLabel );
						GUI.contentColor = Color.white;
						break;

					case MaterialThumbSize.Small:
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
				GUI.DrawTexture( rect, GetMaterialThumb() );
			}
			else
			{
				GUI.Label( rect, Styles.MPNoTexture );
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
