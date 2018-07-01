#if UNITY_EDITOR

using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace Sabresaurus.SabreCSG.MaterialPalette
{
	public class MaterialPaletteHelper
	{
		public static string[] GetAssetLabels()
		{
			BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod;
			var labels = (Dictionary<string, float>)typeof( AssetDatabase ).InvokeMember( "GetAllLabels", flags, null, null, null );

			List<string> labelText = new List<string>();

			foreach( string l in labels.Keys )
			{
				labelText.Add( l );
			}

			return labelText.Distinct().ToArray();
		}

		public static Texture2D GetMaterialThumb( Material mat )
		{
			if( !mat.HasProperty( "_MainTex" ) )
			{
				return SabreCSGResources.MaterialPaletteNoTexture;
			}

			return AssetPreview.GetAssetPreview( mat );
		}

		public static Material[] GetAllMaterials( string filter )
		{
			List<Material> mats = new List<Material>();
			string[] guids = AssetDatabase.FindAssets( "t:Material l:" + filter );
			foreach( string guid in guids )
			{
				Material mat = AssetDatabase.LoadAssetAtPath<Material>( AssetDatabase.GUIDToAssetPath( guid ) );

				string path = AssetDatabase.GUIDToAssetPath( guid );

				if( path.Contains( "SabreCSG/Internal" )
					|| path.Contains( "SabreCSG/Resources" )
					|| path.Contains( "SabreCSG/Materials" )
					|| path.Contains( "ReverbVolume/Data" )
					|| path.Contains( "UsableTriggerVolume/Data" ) )
					continue;

				if( mat.name == "Font Material" )
					continue;

				mats.Add( mat );
			}

			return mats.ToArray();
		}

		/// <summary>
		/// Apply a material to the selected CSGModel face.
		/// </summary>
		/// <param name="mat"></param>
		public static void ApplyMaterial( Material mat )
		{
			if( Selection.activeObject != null )
			{
				CSGModel activeModel = CSGModel.GetActiveCSGModel();

				if( activeModel != null )
				{
					SurfaceEditor se = (SurfaceEditor)activeModel.GetTool( MainMode.Face );
					se.SetSelectionMaterial( mat );
				}
			}
		}

		/// <summary>
		/// Get the 2D thumbnail of the supplied material.
		/// </summary>
		/// <param name="mat"></param>
		/// <param name="rect"></param>
		private static void GetPreviewThumb( Material mat, Rect rect )
		{
			GL.PushMatrix();
			GL.LoadPixelMatrix();

			mat.SetPass( 0 );

			GL.Begin( GL.QUADS );
			{
				rect.width += rect.x;
				rect.height += rect.y;

				GL.TexCoord( new Vector3( 0, 0, 0 ) );
				GL.Vertex3( rect.x, rect.y, 0 );

				GL.TexCoord( new Vector3( 0, 1, 0 ) );
				GL.Vertex3( rect.x, rect.height, 0 );

				GL.TexCoord( new Vector3( 1, 1, 0 ) );
				GL.Vertex3( rect.width, rect.height, 0 );

				GL.TexCoord( new Vector3( 1, 0, 0 ) );
				GL.Vertex3( rect.width, rect.y, 0 );
			}
			GL.End();
			GL.PopMatrix();

			GUILayout.Label( mat.name, "ChannelStripSendReturnBar" );
		}
	}
}

#endif
