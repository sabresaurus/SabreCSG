#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sabresaurus.SabreCSG
{
    public class VertexColorWindow : EditorWindow
	{
		[System.NonSerialized]
		static readonly Color[] PRESET_COLORS = new Color[]
		{
			new ColorHex("ffab00"),
			new ColorHex("ff1a00"),
			new ColorHex("ff00f9"),
			new ColorHex("00dbff"),
			new ColorHex("d0d0d0"),
			new ColorHex("7500ff"),
		};

		static readonly Vector2 WINDOW_SIZE = new Vector2(180,60);

        static Object defaultColorPresetLibrary = null;

		CSGModel csgModel;

        public static VertexColorWindow CreateAndShow(CSGModel csgModel, IVertexColorEditable targetEditor)
		{
			VertexColorWindow window = EditorWindow.GetWindow<VertexColorWindow>(true, "Vertex Colors", true);
			window.csgModel = csgModel;

			// By setting both sizes to the same, even the resize cursor hover is automatically disabled
			window.minSize = WINDOW_SIZE;
			//window.maxSize = WINDOW_SIZE;

			window.Show();

			return window;
		}

		void OnGUI()
		{
			if(csgModel == null)
			{
				// Link to face tool has been lost, so attempt to reacquire
				CSGModel[] csgModels = FindObjectsOfType<CSGModel>();

				// Build the first csg model that is currently being edited
				for (int i = 0; i < csgModels.Length; i++) 
				{
					if(csgModels[i].EditMode)
					{
						csgModel = csgModels[i];

						break;
					}
				}

				// If it's still null
				if(csgModel == null)
				{
					GUILayout.Label("No active CSG Model");
					return;
				}
			}

            IVertexColorEditable targetEditor = csgModel.ActiveTool as IVertexColorEditable;

            if(targetEditor == null)
            {
                GUIStyle style = GUI.skin.label;
                style.wordWrap = true;
                GUILayout.Label("Color window can't be used with active tool", style);
                return;
            }

			GUILayout.Label("Set Vertex Colors", SabreGUILayout.GetTitleStyle());

			Color sourceColor = targetEditor.GetColor();

			Color newColor = EditorGUILayout.ColorField(sourceColor);

			if(newColor != sourceColor)
			{
				targetEditor.SetSelectionColor(newColor);
			}

            Rect nameTagRect = GUILayoutUtility.GetRect(10, SabreGUILayout.NAME_TAG_HEIGHT);
            SabreGUILayout.DrawNameTag(nameTagRect.position, "Presets");

			// Preset color buttons
            GUILayout.BeginHorizontal();
            foreach (Color color in PRESET_COLORS)
            {
                if(SabreGUILayout.ColorButton(color))
				{
                    targetEditor.SetSelectionColor(color);
				}	
			}
			GUILayout.EndHorizontal();

            nameTagRect = GUILayoutUtility.GetRect(10, SabreGUILayout.NAME_TAG_HEIGHT);
            SabreGUILayout.DrawNameTag(nameTagRect.position, "Library");
            if(defaultColorPresetLibrary == null)
            {
                defaultColorPresetLibrary = AssetDatabase.LoadMainAssetAtPath(CSGModel.GetSabreCSGPath() + "DefaultColors.colors");
            }

            // Add in the color presets
            if(defaultColorPresetLibrary != null)
            {
                Color[] palette = PaintUtility.ExtractPalette(defaultColorPresetLibrary);
                GUILayout.BeginHorizontal();
                foreach (Color color in palette)
                {
                    if(SabreGUILayout.ColorButton(color))
                    {
                        targetEditor.SetSelectionColor(color);
                    }   
                }
                GUILayout.EndHorizontal();
            }
		}
	}
}
#endif