#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Sabresaurus.SabreCSG.MaterialPalette
{
	internal static class MaterialPaletteHelper
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
	}
}

#endif
