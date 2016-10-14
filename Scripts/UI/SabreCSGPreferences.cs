#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sabresaurus.SabreCSG
{
	public class SabreCSGPreferences : EditorWindow
	{
		const string RUNTIME_CSG_DEFINE = "RUNTIME_CSG";
		static readonly Vector2 WINDOW_SIZE = new Vector2(370,360);

		static Event cachedEvent;

		public static void CreateAndShow()
		{
			// Unity API doens't allow us to bring up the preferences, so just create a window that will display it
			SabreCSGPreferences window = EditorWindow.GetWindow<SabreCSGPreferences>(true, "SabreCSG Preferences", true);

			// By setting both sizes to the same, even the resize cursor hover is automatically disabled
			window.minSize = WINDOW_SIZE;
			window.maxSize = WINDOW_SIZE;

			window.Show();
		}

		void OnGUI()
		{
			GUILayout.Label("SabreCSG Preferences", SabreGUILayout.GetTitleStyle(20));
			PreferencesGUI();

		}

		[PreferenceItem("SabreCSG")]
		public static void PreferencesGUI()
		{
			

//			Event.current.GetTypeForControl
//
//			if(Event.current.type == EventType.KeyDown)
//			{
//				cachedEvent = new Event(Event.current);
////				this.Repaint();
//			}
//
//			GUILayout.TextField("");
//
//			if(cachedEvent != null)
//			{
//				GUILayout.Label(cachedEvent.ToString());
//			}
//			else
//			{
//				GUILayout.Label("No event");
//			}



			GUILayout.Space(10);

			bool newHideGridInPerspective = GUILayout.Toggle(CurrentSettings.HideGridInPerspective, "Hide grid in perspective scene views");

			if(newHideGridInPerspective != CurrentSettings.HideGridInPerspective)
			{
				SceneView.RepaintAll();			
				CurrentSettings.HideGridInPerspective = newHideGridInPerspective;
			}


			CurrentSettings.OverrideFlyCamera = GUILayout.Toggle(CurrentSettings.OverrideFlyCamera, "Linear fly camera");

			EditorGUI.BeginChangeCheck();
			CurrentSettings.ShowExcludedPolygons = GUILayout.Toggle(CurrentSettings.ShowExcludedPolygons, "Show excluded polygons");
			if(EditorGUI.EndChangeCheck())
			{
				// What's shown in the SceneView has potentially changed, so force it to repaint
				SceneView.RepaintAll();
			}

			GUILayout.Space(10);

			if(GUILayout.Button("Change key mappings"))
			{
				Selection.activeObject = KeyMappings.Instance;
				// Show inspector
				EditorApplication.ExecuteMenuItem("Window/Inspector");
			}
//			CurrentSettings.ReducedHandleThreshold = GUILayout.Toggle(CurrentSettings.ReducedHandleThreshold, "Reduced handle threshold");

			GUILayout.Space(20);

			GUIStyle style = SabreGUILayout.GetForeStyle();
			style.wordWrap = true;
			GUILayout.Label("Runtime CSG is a new experimental feature which allows you to create, alter and build brushes at runtime in your built applications.", style);
			BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
			string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
			List<string> definesSplit = defines.Split(';').ToList();
			bool enabled = definesSplit.Contains(RUNTIME_CSG_DEFINE);

			if(enabled)
			{
				if(GUILayout.Button("Disable Runtime CSG (Experimental)"))
				{
					definesSplit.Remove(RUNTIME_CSG_DEFINE);
					defines = string.Join(";", definesSplit.ToArray());
					PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, defines);
				}
			}
			else
			{
				if(GUILayout.Button("Enable Runtime CSG (Experimental)"))
				{
					if(!definesSplit.Contains(RUNTIME_CSG_DEFINE))
					{
						definesSplit.Add(RUNTIME_CSG_DEFINE);
					}
					defines = string.Join(";", definesSplit.ToArray());
					PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, defines);
				}
			}


			GUILayout.FlexibleSpace();

			GUILayout.Label("SabreCSG Version " + CSGModel.VERSION_STRING, SabreGUILayout.GetForeStyle());
		}
	}
}
#endif