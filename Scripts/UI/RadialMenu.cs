#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
	public static class RadialMenu
	{
		// Whether the radial is being displayed
		static bool isActive = false;

		public static bool IsActive {
			get {
				return isActive;
			}
			set {
				isActive = value;
			}
		}

		static readonly Color[] colors = new Color[]
		{
			new Color32(186,255,0, 128),
			new Color32(128,128,128, 128),
			new Color32(255,48,0, 128),
			new Color32(0,180,255, 128),
			new Color32(186,255,0, 128),
			new Color32(128,128,128, 128),
			new Color32(255,48,0, 128),
			new Color32(0,180,255, 128),
		};

		static readonly string[] messages = new string[]
		{
			"Top",
			"Near",
			"Right",
			"Front",
			"Bottom",
			"Iso",
			"Left",
			"Back",
		};

		// Points distance, if the user lets releases the mouse within this distance of the radial center it cancels
		const float MIN_DISTANCE = 50;

		/// <summary>
		/// Primarily input operations, called early in the OnSceneGUI call
		/// </summary>
		public static void OnEarlySceneGUI(SceneView sceneView)
		{
			if(isActive)
			{
				Event e = Event.current;

				if (e.type == EventType.MouseDown || e.type == EventType.MouseUp || e.type == EventType.MouseDrag || e.type == EventType.MouseMove)
				{
					OnMouseAction(sceneView);
				}
			}
		}

		/// <summary>
		/// Primarily final drawing, called late in the OnSceneGUI call
		/// </summary>
		public static void OnLateSceneGUI(SceneView sceneView)
		{
			// Only draw if menu is active and this is the right scene view
			if(isActive && SceneView.lastActiveSceneView == sceneView)
			{
				Event e = Event.current;

				if (e.type == EventType.Repaint)
				{
					RadialMenu.OnRepaint(sceneView);
				}
			}
		}


		private static void OnMouseAction(SceneView sceneView)
		{
			Event e = Event.current;

			if(e.type == EventType.MouseUp) // They have released the mouse
			{
				Vector2 centerPosition = new Vector2(Screen.width/2, Screen.height/2);

				// Find which circle sector they released in (or -1 if in center)
				int highlightIndex = GetIndex(centerPosition);
				if(highlightIndex == 0)
				{
					EditorHelper.IsoAlignSceneView(Vector3.down);
				}
				else if(highlightIndex == 1) // Align to nearest axis
				{
					EditorHelper.IsoAlignSceneViewToNearest();
				}
				else if(highlightIndex == 2)
				{
					EditorHelper.IsoAlignSceneView(Vector3.left);
				}
				else if(highlightIndex == 3)
				{
					EditorHelper.IsoAlignSceneView(Vector3.back);
				}
				else if(highlightIndex == 4)
				{
					EditorHelper.IsoAlignSceneView(Vector3.up);
				}
				else if(highlightIndex == 5) // Iso/Perspective toggle
				{
					sceneView.orthographic = !sceneView.orthographic;
				}
				else if(highlightIndex == 6)
				{
					EditorHelper.IsoAlignSceneView(Vector3.right);
				}
				else if(highlightIndex == 7)
				{
					EditorHelper.IsoAlignSceneView(Vector3.forward);
				}

				// Operation is complete, hide the radial
				isActive = false;
			}

			e.Use();
		}

		/// <summary>
		/// Finds the sector the mouse is in (or -1 if it's within the center)
		/// </summary>
		/// <returns>Index of the sector, or -1 if it's within the center.</returns>
		/// <param name="centerPosition">Center position.</param>
		static int GetIndex(Vector2 radialCenterPosition)
		{
			// Number of sectors (or slices) the circle is cut into
			int angleCount = messages.Length;

			// Convert the mouse position to scene view space
			Vector2 mousePosition = EditorHelper.ConvertMousePointPosition(Event.current.mousePosition);

			int highlightIndex = -1; // Default to no sector matched (cancel)

			// If the mouse is far enough from the center to give a definitive result
			if(Vector2.Distance(mousePosition, radialCenterPosition) > MIN_DISTANCE)
			{
				Vector2 relativePosition = mousePosition - radialCenterPosition;
				float angle = Mathf.Atan2(relativePosition.x, relativePosition.y) * Mathf.Rad2Deg;
				// Make sure angle is in the 0 to 360 range, so we can easily quantize it
				if(angle < 0)
				{
					angle += 360;
				}
				highlightIndex = Mathf.RoundToInt(angle / (360f / angleCount));

				// Loop back if the index has overflowed (as the first sector contains both 1 degree and 359 degrees)
				if(highlightIndex >= angleCount)
				{
					highlightIndex -= angleCount;
				}
			}
			return highlightIndex;
		}

		private static void OnRepaint(SceneView sceneView)
		{
			float distanceScaler = 1f;

#if UNITY_5_4_OR_NEWER
			distanceScaler = EditorGUIUtility.pixelsPerPoint;
#endif
			// Distance in points from the radial center to the center of each option circle
			float distance = 100 * distanceScaler;

			// Number of sectors (or slices) the circle is cut into
			int angleCount = messages.Length;
			// Radians angle that each sector takes up
			float angleDelta = (Mathf.PI*2f) / (float)angleCount;

			// Radial center position
			Vector2 centerPosition = new Vector2(Screen.width/2, Screen.height/2);

			// Sector being highlighted
			int highlightIndex = GetIndex(centerPosition);

			GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel);
			labelStyle.alignment = TextAnchor.MiddleCenter;

			// Draw normal circles
			SabreCSGResources.GetCircleMaterial().SetPass(0);

			GL.PushMatrix();
			GL.LoadPixelMatrix();

			GL.Begin(GL.QUADS);

			for (int i = 0; i < angleCount; i++) 
			{
				float angle = i * angleDelta;
				Color color = colors[i];

				// Highlighted circle should be a bit more opaque
				if(i == highlightIndex)
				{
					color.a = 0.75f;
				}
				else
				{
					color.a = 0.5f;
				}
				GL.Color(color);

				Vector3 position = centerPosition + distance * new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
				SabreGraphics.DrawBillboardQuad(position, 64, 64);
			}

			// Draw white dots from the center towards the highlighted circle
			if(highlightIndex != -1)
			{
				float angle = highlightIndex * angleDelta;

				GL.Color(Color.white);

				Vector3 position = centerPosition;
				SabreGraphics.DrawBillboardQuad(position, 8, 8);

				position = centerPosition + 20 * distanceScaler * new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
				SabreGraphics.DrawBillboardQuad(position, 16, 16);

				position = centerPosition + 50 * distanceScaler * new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
				SabreGraphics.DrawBillboardQuad(position, 32, 32);
			}
			else
			{
				// No highlighted option, just draw a white dot at the center
				GL.Color(Color.white);

				Vector3 position = centerPosition;
				SabreGraphics.DrawBillboardQuad(position, 16, 16);
			}

			GL.End();
			GL.PopMatrix();

			// Draw a white circle around the edge of the higlighted circle
			if(highlightIndex != -1)
			{
				SabreCSGResources.GetCircleOutlineMaterial().SetPass(0);

				GL.PushMatrix();
				GL.LoadPixelMatrix();

				GL.Begin(GL.QUADS);

				float angle = highlightIndex * angleDelta;

				GL.Color(Color.white);

				Vector3 position = centerPosition + distance * new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
				SabreGraphics.DrawBillboardQuad(position, 64, 64);

				GL.End();
				GL.PopMatrix();
			}

			// Draw the text

			Vector3 screenOffset = new Vector3(0, 5, 0);

			for (int i = 0; i < angleCount; i++) 
			{
				string message = messages[i];
				// The Iso/Persp toggle should show the appropriate text based on active scene view
				if(message == "Iso" && sceneView.orthographic)
				{
					message = "Persp";
				}

				// Make the font size bigger for a highlighted option
				if(i == highlightIndex)
				{
					labelStyle.fontSize = 14;
				}
				else
				{
					labelStyle.fontSize = 12;
				}

				float angle = i * angleDelta;

				Vector3 position = centerPosition + distance * new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));

				// Offset the position slightly so that it better aligns with the drawn circle
				position += screenOffset * distanceScaler;

				// Need to manually offset the text to be center aligned for some reason
				Vector2 size = labelStyle.CalcSize(new GUIContent(message));
				position += new Vector3(-size.x * distanceScaler /4f , 0, 0);

				// Calculate the world position of the screen point just in front of the camera
				Ray ray = sceneView.camera.ScreenPointToRay(position);
				Vector3 world = ray.origin + ray.direction * 0.01f;

				// Draw using a world space Handle label
				Handles.Label(world, message, labelStyle);
			}
		}
	}
}
#endif