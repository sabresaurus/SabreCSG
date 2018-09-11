#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Sabresaurus.SabreCSG.MaterialPalette
{
	internal static class MPHelper
	{
		/// <summary>
		/// Gets labels from assets in the project.
		/// </summary>
		/// <param name="untagged">Untagged assets</param>
		/// <returns></returns>
		public static string[] GetAssetLabels( string filter, out List<Material> untagged, out List<Material> tagged )
		{
			List<string> assetLabels = new List<string>();
			tagged = new List<Material>();
			untagged = new List<Material>();
			Material[] mats = GetAllMaterials( filter );

			foreach( Material m in mats )
			{
				if( AssetDatabase.GetLabels( m ).Length < 1 )
					untagged.Add( m );
				else
					tagged.Add( m );

				foreach( string s in AssetDatabase.GetLabels( m ) )
				{
					assetLabels.Add( s );
				}
			}

			return assetLabels.Distinct().ToArray();
		}

		/// <summary>
		/// Gets all asset labels, including built-in ones.
		/// </summary>
		/// <returns></returns>
		public static string[] GetAllAssetLabels()
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

		/// <summary>
		/// Get all labels on a material asset.
		/// </summary>
		/// <param name="asset"></param>
		/// <returns></returns>
		public static string[] GetLabelsForMaterial( Material asset )
		{
			return AssetDatabase.GetLabels( GetMaterialAsset( asset ) );
		}

		/// <summary>
		/// Get the asset on disk for the given material.
		/// </summary>
		/// <param name="material"></param>
		/// <returns></returns>
		public static Material GetMaterialAsset( Material material )
		{
			string[] guids = AssetDatabase.FindAssets( "t:Material " + material.name );
			string path = AssetDatabase.GUIDToAssetPath( guids[0] );
			return (Material)AssetDatabase.LoadMainAssetAtPath( path );
		}

		/// <summary>
		/// Selects and pings a <see cref="Material"/> object in the project window if its asset exists.
		/// </summary>
		/// <param name="material">The <see cref="Material"/> to find in the project.</param>
		// note - this requires the type object due to the use of Action<object> used in MPGUI.ContextButton.
		public static void FindAndSelectMaterial( object material )
		{
			Material selected = MPHelper.GetMaterialAsset( (Material)material );
			Selection.activeObject = selected;
			EditorGUIUtility.PingObject( selected );
		}

		// TODO: Remove/deprecate this function.
		/// <summary>
		/// Gets all materials in the project, filtered by label,
		/// excluding editor, and generated font materials.
		/// </summary>
		/// <param name="filter">The asset label to use when filtering.</param>
		/// <returns></returns>
		public static Material[] GetAllMaterials( string filter )
		{
			List<Material> mats = new List<Material>();

			// possible room for improvement - multiple tag filters
			string[] guids = AssetDatabase.FindAssets( "t:Material l:" + filter );

			foreach( string guid in guids )
			{
				Material mat = AssetDatabase.LoadAssetAtPath<Material>( AssetDatabase.GUIDToAssetPath( guid ) );
				string path = AssetDatabase.GUIDToAssetPath( guid );

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
		/// Applies a material to the selected face of a CSGModel brush.
		/// </summary>
		/// <param name="mat">The <see cref="Material"/> to apply to the selected face.</param>
		// see note on FindAndSelectMaterial - line #86
		public static void ApplyMaterial( object mat )
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
	}
}

#endif
