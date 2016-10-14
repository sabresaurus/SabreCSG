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

		CSGModel csgModel;
		SurfaceEditor surfaceEditor;

		public static VertexColorWindow CreateAndShow(CSGModel csgModel, SurfaceEditor surfaceEditor)
		{
			VertexColorWindow window = EditorWindow.GetWindow<VertexColorWindow>(true, "Vertex Colors", true);
			window.surfaceEditor = surfaceEditor;
			window.csgModel = csgModel;

			// By setting both sizes to the same, even the resize cursor hover is automatically disabled
			window.minSize = WINDOW_SIZE;
			window.maxSize = WINDOW_SIZE;

			window.Show();

			return window;
		}

		void OnGUI()
		{
			if(surfaceEditor == null || csgModel == null)
			{
				// Link to face tool has been lost, so attempt to reacquire
				CSGModel[] csgModels = FindObjectsOfType<CSGModel>();

				// Build the first csg model that is currently being edited
				for (int i = 0; i < csgModels.Length; i++) 
				{
					if(csgModels[i].EditMode)
					{
						csgModel = csgModels[i];
						surfaceEditor = csgModels[i].GetTool(MainMode.Face) as SurfaceEditor;
						break;
					}
				}

				// If it's still null
				if(surfaceEditor == null || csgModel == null)
				{
					GUILayout.Label("No active CSG Model");
					return;
				}
			}

			GUILayout.Label("Set Vertex Colors", SabreGUILayout.GetTitleStyle());

			Color sourceColor = surfaceEditor.GetColor();

			Color newColor = EditorGUILayout.ColorField(sourceColor);

			if(newColor != sourceColor)
			{
				surfaceEditor.SetSelectionColor(newColor);
			}

			// Preset color buttons
			GUILayout.BeginHorizontal();
			for (int i = 0; i < PRESET_COLORS.Length; i++) 
			{
				if(SabreGUILayout.ColorButton(PRESET_COLORS[i]))
				{
					surfaceEditor.SetSelectionColor(PRESET_COLORS[i]);
				}	
			}
			GUILayout.EndHorizontal();
		}
	}
}
#endif