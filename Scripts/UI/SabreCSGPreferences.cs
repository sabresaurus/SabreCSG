﻿#if UNITY_EDITOR

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
        private const string RUNTIME_CSG_DEFINE = "RUNTIME_CSG";
        private const string SABRE_CSG_DEBUG_DEFINE = "SABRE_CSG_DEBUG";
        private static readonly Vector2 WINDOW_SIZE = new Vector2(370, 400);

        //private static Event cachedEvent;

        public static void CreateAndShow()
        {
            // Unity API doens't allow us to bring up the preferences, so just create a window that will display it
            SabreCSGPreferences window = EditorWindow.GetWindow<SabreCSGPreferences>(true, "SabreCSG Preferences", true);

            // By setting both sizes to the same, even the resize cursor hover is automatically disabled
            window.minSize = WINDOW_SIZE;
            window.maxSize = WINDOW_SIZE;

            window.Show();
        }

        private void OnGUI()
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

            if (newHideGridInPerspective != CurrentSettings.HideGridInPerspective)
            {
                SceneView.RepaintAll();
                CurrentSettings.HideGridInPerspective = newHideGridInPerspective;
            }

            CurrentSettings.OverrideFlyCamera = GUILayout.Toggle(CurrentSettings.OverrideFlyCamera, "Linear fly camera");

            EditorGUI.BeginChangeCheck();
            CurrentSettings.ShowExcludedPolygons = GUILayout.Toggle(CurrentSettings.ShowExcludedPolygons, "Show excluded polygons");
            if (EditorGUI.EndChangeCheck())
            {
                // What's shown in the SceneView has potentially changed, so force it to repaint
                SceneView.RepaintAll();
            }

            EditorGUI.BeginChangeCheck();
            CurrentSettings.ShowBrushesAsWireframes = GUILayout.Toggle(CurrentSettings.ShowBrushesAsWireframes, "Show brushes as wireframes");
            if (EditorGUI.EndChangeCheck())
            {
                // What's shown in the SceneView has potentially changed, so force it to repaint
                CSGModel.UpdateAllBrushesVisibility();
                SceneView.RepaintAll();
            }

            EditorGUI.BeginChangeCheck();
            CurrentSettings.ShowBrushBoundsGuideLines = GUILayout.Toggle(CurrentSettings.ShowBrushBoundsGuideLines, "Show brush bounds guide lines");
            if (EditorGUI.EndChangeCheck())
            {
                // What's shown in the SceneView has potentially changed, so force it to repaint
                CSGModel.UpdateAllBrushesVisibility();
                SceneView.RepaintAll();
            }

            EditorGUILayout.Space();
            EditorGUI.indentLevel = 1;
            EditorGUILayout.LabelField("Developer Options", EditorStyles.boldLabel);
            EditorGUI.indentLevel = 0;
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            CurrentSettings.ShowHiddenGameObjectsInHierarchy = GUILayout.Toggle(CurrentSettings.ShowHiddenGameObjectsInHierarchy, "Show hidden game objects in hierarchy");
            if (EditorGUI.EndChangeCheck())
            {
                // What's shown in the SceneView has potentially changed, so force it to repaint
                CSGModel.RebuildAllVolumes();
                SceneView.RepaintAll();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Change key mappings"))
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

            if (enabled)
            {
                if (GUILayout.Button("Disable Runtime CSG (Experimental)"))
                {
                    definesSplit.Remove(RUNTIME_CSG_DEFINE);
                    defines = string.Join(";", definesSplit.ToArray());
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, defines);
                }
            }
            else
            {
                if (GUILayout.Button("Enable Runtime CSG (Experimental)"))
                {
                    if (!definesSplit.Contains(RUNTIME_CSG_DEFINE))
                    {
                        definesSplit.Add(RUNTIME_CSG_DEFINE);
                    }
                    defines = string.Join(";", definesSplit.ToArray());
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, defines);
                }
            }

            GUILayout.Space(20);
            GUILayout.Label("Debug mode executes additional code for verbose error checking. Used by SabreCSG developers.", style);
            buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
            definesSplit = defines.Split(';').ToList();
            enabled = definesSplit.Contains(SABRE_CSG_DEBUG_DEFINE);
            if (enabled)
            {
                if (GUILayout.Button("Disable Debug Mode (Recommended)"))
                {
                    definesSplit.Remove(SABRE_CSG_DEBUG_DEFINE);
                    defines = string.Join(";", definesSplit.ToArray());
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, defines);
                }
            }
            else
            {
                if (GUILayout.Button("Enable Debug Mode (Not Recommended)"))
                {
                    if (!definesSplit.Contains(SABRE_CSG_DEBUG_DEFINE))
                    {
                        definesSplit.Add(SABRE_CSG_DEBUG_DEFINE);
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