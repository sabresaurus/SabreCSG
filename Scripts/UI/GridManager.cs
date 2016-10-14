#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Reflection;

namespace Sabresaurus.SabreCSG
{
	[InitializeOnLoad]
	public static class GridManager
	{
		static GridManager()
		{
			
		}

		[UnityEditor.Callbacks.DidReloadScripts]
		static void OnReloadedScripts()
		{
			UpdateGrid();
		}

		public static void UpdateGrid()
		{
			CSGModel[] csgModels = GameObject.FindObjectsOfType<CSGModel>();

			bool active = false;
			for (int i = 0; i < csgModels.Length; i++) 
			{
				if(csgModels[i].EditMode)
				{
					active = true;
					break;
				}
			}

			if(active)
			{
				SetGridMode(CurrentSettings.GridMode);
			}
			else
			{
				SetGridMode(GridMode.Unity);
			}
		}

		static void SetGridMode(GridMode gridMode)
		{
			if(gridMode == GridMode.SabreCSG)
			{
				CSGGrid.Activate();
			}
			else
			{
				CSGGrid.Deactivate();
			}

			if(gridMode == GridMode.Unity)
			{
				ShowOrHideUnityGrid(true);
			}
			else
			{
				ShowOrHideUnityGrid(false);
			}
		}

		public static void ShowOrHideUnityGrid(bool gridVisible)
		{
			// This code uses reflection to access and set the internal AnnotationUtility.showGrid property. 
			// As a result the internal structure could change, so the entire thing is wrapped in a try-catch
			try
			{
				Assembly unityEditorAssembly = Assembly.GetAssembly(typeof(Editor));
				if(unityEditorAssembly != null)
				{
					System.Type type = unityEditorAssembly.GetType("UnityEditor.AnnotationUtility");
					if(type != null)
					{
						PropertyInfo property = type.GetProperty("showGrid", BindingFlags.Static | BindingFlags.NonPublic);
						if(property != null)
						{
							property.GetSetMethod(true).Invoke(null, new object[] { gridVisible } );
						}
					}
				}
			}
			catch
			{
				// Failed, suppress any errors and just do nothing
			}
		}
	}
}
#endif