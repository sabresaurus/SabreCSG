#if UNITY_EDITOR

using UnityEngine;

namespace Sabresaurus.SabreCSG.MaterialPalette
{
	public static class Styles
	{
		public static GUIStyle MPMiniScrollbar
		{
			get
			{
				GUIStyle s = new GUIStyle();

				return s;
			}
		}

		public static GUIStyle MPAssetPreviewLabel
		{
			get
			{
				GUIStyle s = new GUIStyle( "TL Range Overlay" );
				s.normal.textColor = Color.white;
				s.contentOffset = new Vector2( 2, 0 );
				s.margin = new RectOffset( 0, 0, 1, 1 );
				s.padding = new RectOffset( 0, 0, 1, 1 );
				s.clipping = TextClipping.Clip;

				return s;
			}
		}

		public static GUIStyle MPAssetTagLabel
		{
			get
			{
				GUIStyle s = new GUIStyle( "WindowBackground" );
				s.normal.textColor = Color.white;
				s.onHover.textColor = Color.yellow;
				s.active.textColor = Color.grey;
				s.fixedHeight = 16;
				s.contentOffset = new Vector2( 2, 0 );
				s.margin = new RectOffset( 0, 0, 1, 1 );
				s.padding = new RectOffset( 0, 0, 1, 1 );

				return s;
			}
		}

		public static Texture2D MPNoTexture
		{
			get
			{
				return (Texture2D)SabreCSGResources.LoadObject( "Internal/MaterialPalette/MaterialPaletteNoTexture.tga" );
			}
		}

		public static GUIStyle MPScrollViewBackground
		{
			get
			{
#if UNITY_5
				GUIStyle s = new GUIStyle( "AnimationCurveEditorBackground" );
#else
				GUIStyle s = new GUIStyle("CurveEditorBackground");
#endif

				return s;
			}
		}

		public static GUIStyle MPAssetPreviewBackground( int width, int height )
		{
			GUIStyle s = new GUIStyle();
			s.normal.background = (Texture2D)SabreCSGResources.LoadObject( "Internal/MaterialPalette/MaterialPaletteAssetPreviewBG.tga" );
			s.fixedHeight = height;
			s.fixedWidth = width;
			s.border = new RectOffset( 1, 1, 1, 1 );
			s.padding = new RectOffset( 2, 2, 2, 2 );
			s.margin = new RectOffset( 4, 4, 4, 4 );

			return s;
		}
	}
}
#endif
