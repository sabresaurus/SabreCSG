#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace Sabresaurus.SabreCSG.MaterialPalette
{
	public class PreviewTile
	{
		public int materialThumbSize = (int)ThumbSize.Medium;
		public Material material = null;
		public Color32 labelFontColor = Color.yellow;
		public MPWindow parent;

		public void Draw( Rect lastRect )
		{
			// TODO: this is done a few times. Maybe create an operator to convert Vector2 to ThumbSize?
			Vector2 thumbSize = new Vector2( (int)materialThumbSize, (int)materialThumbSize );

			lastRect.width -= 4;
			lastRect.height -= 4;

			lastRect.x += 2;
			lastRect.y += 2;

			if( material.name == "Deleted Material" )
			{
				// TODO: might be a better way to do this
				MPGUI.ContextButton( new GUIContent( "", material.name ),
					new Vector2( thumbSize.x, thumbSize.y ),
					parent.Load, parent.Load,
					Styles.MPAssetPreviewBackground( (int)thumbSize.x, (int)thumbSize.y ) );
			}
			else
			{
				MPGUI.ContextButton( new GUIContent( "", material.name ),
					new Vector2( thumbSize.x, thumbSize.y ),
					MPHelper.ApplyMaterial, DisplayTagEditor, MPHelper.FindAndSelectMaterial,
					material, parent, material,
					Styles.MPAssetPreviewBackground( (int)thumbSize.x, (int)thumbSize.y ) );
			}

			// material preview
			MPGraphics.RenderThumb( GUILayoutUtility.GetLastRect(), material );

			// label rect + offset
			lastRect = GUILayoutUtility.GetLastRect();// get the thumb rect
			Rect labelRect = new Rect( lastRect.x, lastRect.y, lastRect.width, 16 );

			labelRect.y += thumbSize.y - 16;

			// material label
			if( !( lastRect.Contains( Event.current.mousePosition ) ) )
			{
				if( materialThumbSize <= 128 )
				{
					GUI.contentColor = labelFontColor;
					GUI.Label( labelRect, material.name, Styles.MPAssetPreviewLabel );
					GUI.contentColor = Color.white;
				}
				if( materialThumbSize <= 64 )
				{
					GUI.contentColor = labelFontColor;
					GUI.Label( labelRect, material.name, Styles.MPAssetPreviewLabel );
					GUI.contentColor = Color.white;
				}
				else
				{
				}
			}

			// hightlight
			if( lastRect.Contains( Event.current.mousePosition ) )
			{
				MPGraphics.RenderHightlightOutline( GUILayoutUtility.GetLastRect(), /*magenta*/ new Color32( 255, 0, 255, 255 ) );
			}
		}

		private void DisplayTagEditor( object parent )
		{
			MPTagEditorWindow mp = EditorWindow.GetWindow<MPTagEditorWindow>( true, "Tag Editor - " + material.name );
			MPWindow mpw = (MPWindow)parent;

			mp.material = material;
			mp.parent = mpw;
			mp.existingMaterialLabels = MPHelper.GetLabelsForMaterial( mp.material );

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
