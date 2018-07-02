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
		public static List<string> excludedMaterials = new List<string>();

		public static string[] GetAssetLabels( bool onlyUsed )
		{
			if( onlyUsed )
			{
				List<string> assetLabels = new List<string>();
				Material[] mats = GetAllMaterials( "" );

				foreach( Material m in mats )
				{
					foreach( string s in AssetDatabase.GetLabels( m ) )
					{
						assetLabels.Add( s );
					}
				}

				return assetLabels.Distinct().ToArray();
			}
			else
			{
				BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod;
				var labels = (Dictionary<string, float>)typeof( AssetDatabase ).InvokeMember( "GetAllLabels", flags, null, null, null );

				List<string> labelText = new List<string>();

				foreach( string l in labels.Keys )
				{
					labelText.Add( l );
				}

				return labelText.Distinct().ToArray(); // no duplicates
			}
		}

		public static Texture2D GetMaterialThumb( Material mat )
		{
			if( !mat.HasProperty( "_MainTex" ) )
			{
				return SabreCSGResources.MPNoTexture;
			}

			return AssetPreview.GetAssetPreview( mat );
		}

		public static Material[] GetAllMaterials( string filter )
		{
			List<Material> mats = new List<Material>();

			// possible room for improvement - multiple tag filters
			string[] guids = AssetDatabase.FindAssets( "t:Material l:" + filter );

			foreach( string guid in guids )
			{
				Material mat = AssetDatabase.LoadAssetAtPath<Material>( AssetDatabase.GUIDToAssetPath( guid ) );
				string path = AssetDatabase.GUIDToAssetPath( guid );

				// exclude any materials added from another script.
				foreach( string s in excludedMaterials )
				{
					if( path.Contains( s ) )
						continue;
				}

				// we always exclude builtin editor-only materials.
				if( path.Contains( "SabreCSG/Internal" )
					|| path.Contains( "SabreCSG/Resources" )
					|| path.Contains( "SabreCSG/Materials" )
					|| path.Contains( "ReverbVolume/Data" )
					|| path.Contains( "UsableTriggerVolume/Data" ) )
					continue;

				// exclude any materials created for fonts by unity editor
				if( mat.name == "Font Material" )
					continue;

				mats.Add( mat );
			}

			return mats.ToArray();
		}

		/// <summary>
		/// Get the 2D thumbnail of the supplied material.
		/// </summary>
		/// <param name="mat"></param>
		/// <param name="rect"></param>
		public static void DrawPreviewThumb( Material mat, Rect rect, MaterialThumbSize mts )
		{
			Color fontColor = Color.yellow;
			Rect labelRect = rect;
			labelRect.y += (int)mts - 16;

			rect.width -= 4;
			rect.height -= 4;

			rect.x += 2;
			rect.y += 2;

			if( mat != null )
			{
				if( mat.HasProperty( "_MainTex" ) )
				{
					if( mat.GetTexture( "_MainTex" ) != null )
					{
						GUI.Label( rect, mat.GetTexture( "_MainTex" ) );
					}
					else
					{
						if( mat.HasProperty( "_Color" ) )
						{
							Color col = mat.GetColor( "_Color" ).gamma;
							GUI.color = col;

							fontColor = new Color( 1 - col.r, 1 - col.g, 1 - col.b, 1.0f );
						}
						else
						{
							GUI.Label( rect, SabreCSGResources.MPNoTexture );
						}

						if( mat.HasProperty( "_EmissionColor" ) )
						{
							Color col = mat.GetColor( "_EmissionColor" ).gamma;
							if( col != Color.black )
							{
								GUI.color = col;
								fontColor = new Color( 1 - col.r, 1 - col.g, 1 - col.b, 1.0f );
							}
							else
							{
								GUI.Label( rect, SabreCSGResources.MPNoTexture );
							}
						}

						GUI.DrawTexture( rect, EditorGUIUtility.whiteTexture );
						GUI.color = Color.white;
					}
				}
				else
				{
					GUI.Label( rect, SabreCSGResources.MPNoTexture );
				}

				if( !( Event.current.type == EventType.Repaint
					&& rect.Contains( Event.current.mousePosition ) ) )
				{
					switch( mts )
					{
						case MaterialThumbSize.Large:
							GUI.contentColor = fontColor;
							GUI.Label( labelRect, mat.name, SabreCSGResources.MPAssetPreviewLabel );
							GUI.contentColor = Color.white;
							break;

						case MaterialThumbSize.Medium:
							GUI.contentColor = fontColor;
							GUI.Label( labelRect, mat.name, SabreCSGResources.MPAssetPreviewLabel );
							GUI.contentColor = Color.white;
							break;

						case MaterialThumbSize.Small:
							break;
					}
				}
			}

			/*
			Material m = new Material( Shader.Find( "Unlit/Texture" ) );

			if( mat.HasProperty( "_MainTex" ) )
				m.mainTexture = mat.mainTexture;
			else
				m.color = mat.color;

			m.SetPass( 0 );

			GL.PushMatrix();
			GL.LoadIdentity();

			GL.Begin( GL.QUADS );
			{
				rect.width += rect.x;
				rect.height += rect.y;

				//GL.TexCoord( new Vector3( 0, 0, 0 ) );
				GL.Vertex3( rect.x, rect.y, 0 );

				//GL.TexCoord( new Vector3( 0, 1, 0 ) );
				GL.Vertex3( rect.x, rect.height, 0 );

				//GL.TexCoord( new Vector3( 1, 1, 0 ) );
				GL.Vertex3( rect.width, rect.height, 0 );

				//GL.TexCoord( new Vector3( 1, 0, 0 ) );
				GL.Vertex3( rect.width, rect.y, 0 );
			}
			GL.End();
			GL.PopMatrix();
			*/
			//GUILayout.Label( mat.name, "ChannelStripSendReturnBar" );
		}
	}
}

#endif
