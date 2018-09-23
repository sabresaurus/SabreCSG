// MIT License
// 
// Copyright (c) 2017 Sabresaurus
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
//  The above copyright notice and this permission notice shall be included in all
//  copies or substantial portions of the Software.
// 
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//  SOFTWARE.

#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.IO;

namespace Sabresaurus.Radial
{
    [InitializeOnLoad]
    public static class RadialMenu
    {
        // Change this to change they key to be pressed to activate the radial
        // See http://unity3d.com/support/documentation/ScriptReference/MenuItem.html for shortcut format
        private const string ACTIVATE_KEY = "j";

        // Used for offseting mouse position
        private const int TOOLBAR_HEIGHT = 37;

        // Screen position right at the front (note can't use 1, because even though OSX accepts it Windows doesn't)
        private const float FRONT_Z_DEPTH = 0.99f;

		// Whether the radial is being displayed
		private static bool isActive = false;

        private static Material circleMaterial = null;
        private static Material circleOutlineMaterial = null;


        static RadialMenu()
        {
            RefreshListeners();
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        static void OnReloadedScripts()
        {
            RefreshListeners();
        }

		public static bool IsActive 
        {
			get 
            {
				return isActive;
			}
		}

        private static void RefreshListeners()
        {
            // Make sure our listeners are removed so we don't add them again out of order
            SceneView.onSceneGUIDelegate -= OnEarlySceneGUI;
            SceneView.onSceneGUIDelegate -= OnLateSceneGUI;

            // Grab all the remaining listeners
            Delegate[] subscribers = new Delegate[0];

            if(SceneView.onSceneGUIDelegate != null)
            {
                subscribers = SceneView.onSceneGUIDelegate.GetInvocationList();
            }

            // Remove all the listeners - this should result in zero listeners
            foreach (Delegate subscriber in subscribers)
            {
                SceneView.onSceneGUIDelegate -= (SceneView.OnSceneFunc)subscriber;
            }

            SceneView.onSceneGUIDelegate += OnEarlySceneGUI;

            foreach (Delegate subscriber in subscribers)
            {
                SceneView.onSceneGUIDelegate += (SceneView.OnSceneFunc)subscriber;
            }

            SceneView.onSceneGUIDelegate += OnLateSceneGUI;
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
        private static void OnEarlySceneGUI(SceneView sceneView)
		{
            Event e = Event.current;

            if(isActive)
            {

				if (e.type == EventType.MouseDown || e.type == EventType.MouseUp || e.type == EventType.MouseDrag || e.type == EventType.MouseMove)
				{
					OnMouseAction(sceneView);
				}
			}

            if (EventsMatch(e, Event.KeyboardEvent(ACTIVATE_KEY)))
            {
                if (e.type == EventType.KeyUp)
                {
                    isActive = !isActive;
                    RefreshListeners();

                    SceneView.RepaintAll();
                    e.Use();
                }
                else if(e.type == EventType.KeyDown)
                {
                    e.Use();
                }
            }
		}

		/// <summary>
		/// Primarily final drawing, called late in the OnSceneGUI call
		/// </summary>
        private static void OnLateSceneGUI(SceneView sceneView)
		{
			// Only draw if menu is active and this is the right scene view
			if(isActive && SceneView.lastActiveSceneView == sceneView)
			{
				Event e = Event.current;

				if (e.type == EventType.Repaint)
				{
					OnRepaint(sceneView);
				}
			}
		}


		private static void OnMouseAction(SceneView sceneView)
		{
			Event e = Event.current;

            if(e.type == EventType.MouseDown) // They have pressed the mouse
			{
                if(e.button == 0)
                {
    				Vector2 centerPosition = new Vector2(Screen.width / 2, Screen.height / 2);

    				// Find which circle sector they released in (or -1 if in center)
    				int highlightIndex = GetIndex(centerPosition);

    				if(highlightIndex == 0)
    				{
    					IsoAlignSceneView(Vector3.down);
    				}
    				else if(highlightIndex == 1) // Align to nearest axis
    				{
    					IsoAlignSceneViewToNearest();
    				}
    				else if(highlightIndex == 2)
    				{
    					IsoAlignSceneView(Vector3.left);
    				}
    				else if(highlightIndex == 3)
    				{
    					IsoAlignSceneView(Vector3.back);
    				}
    				else if(highlightIndex == 4)
    				{
    					IsoAlignSceneView(Vector3.up);
    				}
    				else if(highlightIndex == 5) // Iso/Perspective toggle
    				{
    					sceneView.orthographic = !sceneView.orthographic;
    				}
    				else if(highlightIndex == 6)
    				{
    					IsoAlignSceneView(Vector3.right);
    				}
    				else if(highlightIndex == 7)
    				{
    					IsoAlignSceneView(Vector3.forward);
    				}
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
			Vector2 mousePosition = ConvertMousePointPosition(Event.current.mousePosition);

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
			float angleDelta = (Mathf.PI * 2f) / (float)angleCount;

			// Radial center position
			Vector2 centerPosition = new Vector2(Screen.width / 2, Screen.height / 2);

			// Sector being highlighted
			int highlightIndex = GetIndex(centerPosition);

			GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel);
			labelStyle.alignment = TextAnchor.MiddleCenter;

			// Draw normal circles
			GetCircleMaterial().SetPass(0);

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
				DrawBillboardQuad(position, 64, 64);
			}

			// Draw white dots from the center towards the highlighted circle
			if(highlightIndex != -1)
			{
				float angle = highlightIndex * angleDelta;

				GL.Color(Color.white);

				Vector3 position = centerPosition;
				DrawBillboardQuad(position, 8, 8);

				position = centerPosition + 20 * distanceScaler * new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
				DrawBillboardQuad(position, 16, 16);

				position = centerPosition + 50 * distanceScaler * new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
				DrawBillboardQuad(position, 32, 32);
			}
			else
			{
				// No highlighted option, just draw a white dot at the center
				GL.Color(Color.white);

				Vector3 position = centerPosition;
				DrawBillboardQuad(position, 16, 16);
			}

			GL.End();
			GL.PopMatrix();

			// Draw a white circle around the edge of the higlighted circle
			if(highlightIndex != -1)
			{
				GetCircleOutlineMaterial().SetPass(0);

				GL.PushMatrix();
				GL.LoadPixelMatrix();

				GL.Begin(GL.QUADS);

				float angle = highlightIndex * angleDelta;

				GL.Color(Color.white);

				Vector3 position = centerPosition + distance * new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
				DrawBillboardQuad(position, 64, 64);

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

        /// <summary>
        /// Aligns the scene view to look in a certain direction in iso mode 
        /// </summary>
        public static void IsoAlignSceneView(Vector3 direction)
        {
            SceneView sceneView = SceneView.lastActiveSceneView;

            SceneView.lastActiveSceneView.LookAt(sceneView.pivot, Quaternion.LookRotation(direction));

            // Mark the camera as iso (orthographic)
            sceneView.orthographic = true;
        }

        /// <summary>
        /// Aligns the scene view to the nearest axis in iso mode
        /// </summary>
        public static void IsoAlignSceneViewToNearest()
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            Vector3 cameraForward = sceneView.camera.transform.forward;
            Vector3 newForward = Vector3.up;
            float bestDot = -1;

            Vector3 testDirection;
            float dot;
            // Find out of the six axis directions the closest direction to the camera
            for (int i = 0; i < 3; i++) 
            {
                testDirection = Vector3.zero;
                testDirection[i] = 1;
                dot = Vector3.Dot(testDirection, cameraForward);
                if(dot > bestDot)
                {
                    bestDot = dot;
                    newForward = testDirection;
                }

                testDirection[i] = -1;
                dot = Vector3.Dot(testDirection, cameraForward);
                if(dot > bestDot)
                {
                    bestDot = dot;
                    newForward = testDirection;
                }
            }
            IsoAlignSceneView(newForward);
        }

        /// <summary>
        /// Converts the mouse position from event space to screen space, by default it assumes input in points and output in pixels
        /// </summary>
        /// <returns>The mouse position in points.</returns>
        /// <param name="sourceMousePosition">Source mouse position.</param>
        /// <param name="convertPointsToPixels">If set to <c>true</c> convert points to pixels.</param>
        public static Vector2 ConvertMousePointPosition(Vector2 sourceMousePosition, bool convertPointsToPixels = true)
        {
#if UNITY_5_4_OR_NEWER
            if(convertPointsToPixels)
            {
                sourceMousePosition = EditorGUIUtility.PointsToPixels(sourceMousePosition);
                // Flip the direction of Y and remove the Scene View top toolbar's height
                sourceMousePosition.y = Screen.height - sourceMousePosition.y - (TOOLBAR_HEIGHT * EditorGUIUtility.pixelsPerPoint);
            }
            else
            {
                // Flip the direction of Y and remove the Scene View top toolbar's height
                float screenHeightPoints = (Screen.height / EditorGUIUtility.pixelsPerPoint);
                sourceMousePosition.y = screenHeightPoints - sourceMousePosition.y - (TOOLBAR_HEIGHT);
            }

#else
            // Flip the direction of Y and remove the Scene View top toolbar's height
            sourceMousePosition.y = Screen.height - sourceMousePosition.y - TOOLBAR_HEIGHT;
#endif
            return sourceMousePosition;
        }

        public static void DrawBillboardQuad(Vector3 screenPosition, int width, int height, bool specifiedPoints = true)
        {
#if UNITY_5_4_OR_NEWER
            if(specifiedPoints)
            {
                // Convert from points to pixels
                float scale = EditorGUIUtility.pixelsPerPoint;
                width = Mathf.RoundToInt(scale * width);
                height = Mathf.RoundToInt(scale * height);
            }
#endif

            screenPosition.z = FRONT_Z_DEPTH;

            GL.TexCoord2(0, 0); // BL
            GL.Vertex(screenPosition + new Vector3(-width / 2, height / 2, 0));
            GL.TexCoord2(1, 0); // BR
            GL.Vertex(screenPosition + new Vector3(width / 2, height / 2, 0));
            GL.TexCoord2(1, 1); // TR
            GL.Vertex(screenPosition + new Vector3(width / 2, -height / 2, 0));
            GL.TexCoord2(0, 1); // TL
            GL.Vertex(screenPosition + new Vector3(-width / 2, -height / 2, 0));
        }

        public static string GetBasePath()
        {
            // Find all the scripts with RadialMenu in their name
            string[] guids = AssetDatabase.FindAssets("RadialMenu t:Script");

            foreach (string guid in guids) 
            {
                // Find the path of the file
                string path = AssetDatabase.GUIDToAssetPath(guid);

                string suffix = "RadialMenu.cs";
                // If it is the target file, i.e. RadialMenu.cs not RadialMenuSomething
                if(path.EndsWith(suffix))
                {
                    // Remove the suffix, to get for example Assets/RadialMenu
                    path = path.Remove(path.Length - suffix.Length, suffix.Length);

                    return path;
                }
            }

            // None matched
            return string.Empty;
        }

        private static Material GetCircleMaterial()
        {
            if (circleMaterial == null)
            {
                Shader shader = Shader.Find("Particles/Alpha Blended");
                circleMaterial = new Material(shader);
                circleMaterial.hideFlags = HideFlags.HideAndDontSave;
                circleMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
                circleMaterial.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(GetBasePath(), "Circle.png"));
            }
            return circleMaterial;
        }

        private static Material GetCircleOutlineMaterial()
        {
            if (circleOutlineMaterial == null)
            {
                Shader shader = Shader.Find("Particles/Alpha Blended");
                circleOutlineMaterial = new Material(shader);
                circleOutlineMaterial.hideFlags = HideFlags.HideAndDontSave;
                circleOutlineMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
                circleOutlineMaterial.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(GetBasePath(), "CircleOutline.png"));
            }
            return circleOutlineMaterial;
        }

        private static bool EventsMatch(Event event1, Event event2)
        {
            EventModifiers modifiers1 = event1.modifiers;
            EventModifiers modifiers2 = event2.modifiers;

            // Ignore capslock from either modifier
            modifiers1 &= (~EventModifiers.CapsLock);
            modifiers2 &= (~EventModifiers.CapsLock);

            // If key code and modifier match
            if(event1.keyCode == event2.keyCode
                && (modifiers1 == modifiers2))
            {
                return true;
            }

            return false;
        }
	}
}
#endif