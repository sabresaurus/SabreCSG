#if UNITY_EDITOR || RUNTIME_CSG

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
	/// <summary>
	/// Volumes are custom programmable triggers in the shape of primitive brushes.
	/// </summary>
	[System.Serializable]
	public class Volume : ScriptableObject
	{
		/// <summary>
		/// Gets the brush preview material shown in the editor.
		/// </summary>
		/// <returns>The volume material.</returns>
		public virtual Material BrushPreviewMaterial
		{
			get
			{
#if RUNTIME_CSG && !UNITY_EDITOR
                return null;
#else
				return SabreCSGResources.GetVolumeMaterial();
#endif
			}
		}

		/// <summary>
		/// Searches the main C# assembly for volume types that can be instantiated.
		/// </summary>
		/// <returns>All matched volume types.</returns>
		public static List<Type> FindAllInAssembly()
		{
			List<Type> matchedTypes = new List<Type>();

			Assembly[] allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach( Assembly assembly in allAssemblies )
			{
				// walk through all the types in the main assembly
				if( assembly.FullName.StartsWith( "Assembly-CSharp" ) )
				{
					Type[] types = assembly.GetTypes();

					for( int i = 0; i < types.Length; i++ )
					{
						// if the type inherits from volume and is not abstract
						if( !types[i].IsAbstract && types[i].IsSubclassOf( typeof( Volume ) ) )
						{
							// valid volume type found!
							matchedTypes.Add( types[i] );
						}
					}
				}
			}

			return matchedTypes;
		}

		/// <summary>
		/// Called when the inspector GUI is drawn in the editor.
		/// </summary>
		/// <returns>True if a property changed or else false.</returns>
		public virtual bool OnInspectorGUI()
		{
#if UNITY_EDITOR
			GUILayout.BeginVertical( "Box" );
			{
				UnityEditor.EditorGUILayout.LabelField( "Volume Options", UnityEditor.EditorStyles.boldLabel );
				UnityEditor.EditorGUI.indentLevel = 1;

				GUILayout.BeginVertical();
				{
				}
				GUILayout.EndVertical();

				UnityEditor.EditorGUI.indentLevel = 0;
			}
			GUILayout.EndVertical();

			if( ChangeCheck() == true )
			{
				return true;
			}
#endif
			return false;
		}

#if UNITY_EDITOR

		/// <summary>
		/// Called when the volume is created in the editor.
		/// </summary>
		/// <param name="volume">The generated volume game object.</param>
		public virtual void OnCreateVolume( GameObject volume )
		{
		}

		/// <summary>
		/// Detects if values have changed. Used in OnInspectorGUI to check for changes. Add your own change check logic here.
		/// </summary>
		/// <returns></returns>
		protected virtual bool ChangeCheck()
		{
			return false;
		}

#endif
	}
}

#endif
