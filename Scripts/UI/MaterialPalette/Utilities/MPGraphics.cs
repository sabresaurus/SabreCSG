#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace Sabresaurus.SabreCSG.MaterialPalette
{
	public class MPGraphics
	{
		/// <summary>
		/// Renders a colored outline around a <see cref="UnityEngine.Rect"/> using the <see cref="UnityEngine.GL"/> class.
		/// </summary>
		/// <param name="lastRect"></param>
		public static void RenderHightlightOutline( Rect lastRect, Color color )
		{
			Material m = new Material( Shader.Find( "Unlit/Color" ) );
			m.color = color;

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

		public static void RenderThumb( Rect rect, Material material )
		{
			if( material != null )
			{
				GUI.DrawTexture( rect, AssetPreview.GetAssetPreview( material ) ?? SabreCSGResources.ClearTexture );
			}
		}
	}
}

#endif
