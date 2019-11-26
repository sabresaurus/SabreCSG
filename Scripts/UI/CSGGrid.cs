#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace Sabresaurus.SabreCSG
{
	public static class CSGGrid
	{
		const int WIDTH = 100;

		// Used for granular fading
		const float MIN_VISIBLE = 20;
		const float MAX_VISIBLE = 40;
		const float MAJOR_LINE_DISTANCE = 200;
		const float MINOR_LINE_DISTANCE = 50;

        public static void Activate()
		{
            // resubscribe to the scene GUI updates.
#if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
#else
            SceneView.onSceneGUIDelegate -= OnSceneGUI;
            SceneView.onSceneGUIDelegate += OnSceneGUI;
#endif

            CSGModel[] csgModels = Object.FindObjectsOfType<CSGModel>();

			// Make sure the grid draws behind the CSG Model drawing. This requires us to ask the CSG Model to reregister
			for (int i = 0; i < csgModels.Length; i++) 
			{
				if(csgModels[i].EditMode)
				{
					csgModels[i].RebindToOnSceneGUI();
					break;
				}
			}
		}

		public static void Deactivate()
		{
#if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui -= OnSceneGUI;
#else
            SceneView.onSceneGUIDelegate -= OnSceneGUI;
#endif
        }

        static void OnSceneGUI(SceneView sceneView)
		{
			Event e = Event.current;

			if (e.type == EventType.Repaint)
			{
				CSGGrid.Draw();
			}
		}

		static Color ColorForVector(Vector3 direction)
		{
			Color color = Color.white;
			if(Vector3.Dot(direction.Abs(), Vector3.right) > 0.99f)
			{
				color = Color.red;
			}
			else if(Vector3.Dot(direction.Abs(), Vector3.up) > 0.99f)
			{
				color = Color.green;
			}
			else if(Vector3.Dot(direction.Abs(), Vector3.forward) > 0.99f)
			{
				color = Color.blue;
			}
			color.r += 0.1f;
			color.g += 0.1f;
			color.b += 0.1f;
			color.a = 0.8f;
			return color;
		}

		static List<Vector3> GetGridMinMax(Camera camera)
		{
			// These points must be shown on the grid
			List<Vector3> pointsInGrid = new List<Vector3>();

			Plane groundPlane = new Plane(Vector3.up, 0);
			// BL is 0,0, TR is 1,1

			float distance;
			Ray ray;

			// Walk up the screen until we hit the ground plane
			for (float y = 0; y < 1; y+= 0.03f) 
			{
				ray = camera.ViewportPointToRay(new Vector3(0,y,0));
				if(groundPlane.Raycast(ray, out distance))
				{
					Vector3 hitPoint = ray.GetPoint(distance);
					pointsInGrid.Add(hitPoint);
					break;
				}
			}

			// Walk up the screen until we hit the ground plane
			for (float y = 0; y < 1; y+= 0.03f) 
			{
				ray = camera.ViewportPointToRay(new Vector3(1,y,0));
				if(groundPlane.Raycast(ray, out distance))
				{
					Vector3 hitPoint = ray.GetPoint(distance);
					pointsInGrid.Add(hitPoint);
					break;
				}
			}

			// Walk down the screen until we hit the ground plane
			for (float y = 1; y > 0; y-= 0.03f) 
			{
				ray = camera.ViewportPointToRay(new Vector3(0,y,0));
				if(groundPlane.Raycast(ray, out distance))
				{
					Vector3 hitPoint = ray.GetPoint(distance);
					pointsInGrid.Add(hitPoint);
					break;
				}
			}

			// Walk down the screen until we hit the ground plane
			for (float y = 1; y > 0; y-= 0.03f) 
			{
				ray = camera.ViewportPointToRay(new Vector3(1,y,0));
				if(groundPlane.Raycast(ray, out distance))
				{
					Vector3 hitPoint = ray.GetPoint(distance);
					pointsInGrid.Add(hitPoint);
					break;
				}
			}

			return pointsInGrid;
		}

		static float CalculateWorldUnitScreenSize(Camera camera, float worldUnits)
		{
			EditorHelper.SceneViewCamera cameraOrientation = EditorHelper.GetSceneViewCamera(camera);

			bool orthographicAxisAligned = camera.orthographic && cameraOrientation != EditorHelper.SceneViewCamera.Other;
			Transform transform = camera.transform;

			if(orthographicAxisAligned)
			{
				float cameraHeight = camera.orthographicSize * 2;
				float cameraWidth = camera.aspect * cameraHeight;

				return worldUnits * (float)Screen.width / cameraWidth;
			}
			else
			{
				float distanceFromGrid = Mathf.Abs(transform.position.y);

				if(distanceFromGrid < 1)
				{
					distanceFromGrid = 1;
				}

				Vector3 worldCenter = transform.position + transform.forward * distanceFromGrid;

				Vector3 rightVector = transform.right;

				Vector3 worldPoint1 = worldCenter - rightVector * worldUnits * 0.5f;
				Vector3 worldPoint2 = worldCenter + rightVector * worldUnits * 0.5f;

				Vector3 screenPoint1 = camera.WorldToScreenPoint(worldPoint1);
				Vector3 screenPoint2 = camera.WorldToScreenPoint(worldPoint2);

				return screenPoint2.x - screenPoint1.x;
			}
		}

		private static void Draw()
		{
			SceneView sceneView = SceneView.currentDrawingSceneView;

			// If the user has turned on hiding of perspectve grid then hide it if perspective
			if(!sceneView.orthographic && CurrentSettings.HideGridInPerspective)
			{
				return;
			}

			// Cache additional references
			Camera camera = sceneView.camera;
			Transform transform = camera.transform;

			float fullSnapDistance = CurrentSettings.PositionSnapDistance;
			float snapDistance = fullSnapDistance;

			// Calculate various camera positions and its orientation
			EditorHelper.SceneViewCamera cameraOrientation = EditorHelper.GetSceneViewCamera(sceneView);

			// True if the camera is both orthographic (iso) and axis-aligned
			bool orthographicAxisAligned = sceneView.orthographic && cameraOrientation != EditorHelper.SceneViewCamera.Other;

			Vector3 cameraPosition = transform.position;

			Vector3 roundedCameraPosition = MathHelper.RoundVector3(cameraPosition, snapDistance);

			// Calculate the world vectors to use for the X and Y grid axes
			Vector3 xDirection = orthographicAxisAligned ? transform.right : Vector3.right;
			Vector3 yDirection = orthographicAxisAligned ? transform.up : Vector3.forward;

			// Now calculate a depth offset to help with dealing with objects far from the origin
			Vector3 depthOffset = Vector3.zero;
			if(orthographicAxisAligned)
			{
				float depthFudge = 1;

				depthOffset = transform.forward * (camera.nearClipPlane + depthFudge + Vector3.Dot(cameraPosition, transform.forward));

//				Debug.Log(camera.nearClipPlane + " to " + camera.farClipPlane);
//				Debug.Log(depthOffset);
			}
			else
			{
				depthOffset = Vector3.zero;
			}

			// Line colors
			Color normalLine = new Color32(200,200,200,128);
			Color smallestLine = new Color32(50,50,50,128);

			Color xAxisColor = ColorForVector(xDirection);
			Color yAxisColor = ColorForVector(yDirection);


			// How many lines to draw in each axis
			int xCount;
			int yCount;

			// Center point of the grid in each axis
			int xMid;
			int yMid;


			bool scaledUp = false;

			// Width of one world unit in screen pixels
			float gridWidthPixels = CalculateWorldUnitScreenSize(camera, 1);

			if(snapDistance >= 8)
			{
				gridWidthPixels = CalculateWorldUnitScreenSize(camera, snapDistance);
			}

			if(gridWidthPixels < MIN_VISIBLE)
			{
				if(snapDistance > 8)
				{
					snapDistance = Mathf.Pow(Mathf.Floor(Mathf.Log(snapDistance, 8)), 8);
					gridWidthPixels = CalculateWorldUnitScreenSize(camera, snapDistance);
				}

				while(gridWidthPixels < MIN_VISIBLE)
				{
					if(snapDistance < 8)
					{
						snapDistance = 8;
					}
					else
					{
						snapDistance *= 8;
					}
					scaledUp = true;
					gridWidthPixels = CalculateWorldUnitScreenSize(camera, snapDistance);
				}
			}

			// Alpha of the granular lines, generally based on distance
			float minorLineAlphaMultiplier = Mathf.InverseLerp(MIN_VISIBLE, MAX_VISIBLE, gridWidthPixels);

			float sanityScalar = Mathf.Max(1, snapDistance) * Mathf.Lerp(2,1, minorLineAlphaMultiplier);

			if(orthographicAxisAligned)
			{
				float cameraHeight = camera.orthographicSize * 2;
				float cameraWidth = camera.aspect * cameraHeight;

				xCount = Mathf.CeilToInt(cameraWidth / snapDistance);
				yCount = Mathf.CeilToInt(cameraHeight / snapDistance);

				xMid = (int)(Vector3.Dot(roundedCameraPosition, xDirection) / snapDistance);
				yMid = (int)(Vector3.Dot(roundedCameraPosition, yDirection) / snapDistance);
			}
			else
			{
				List<Vector3> pointsInGrid = GetGridMinMax(camera);

				Vector3 pivotPointOnGrid = sceneView.pivot;
				pivotPointOnGrid.y = 0;
				pointsInGrid.Add(pivotPointOnGrid);

				Vector3 min = pointsInGrid[0];
				Vector3 max = pointsInGrid[0];
				// Calculate min,max
				for (int i = 1; i < pointsInGrid.Count; i++) 
				{
					min.x = Mathf.Min(pointsInGrid[i].x, min.x);
					min.z = Mathf.Min(pointsInGrid[i].z, min.z);

					max.x = Mathf.Max(pointsInGrid[i].x, max.x);
					max.z = Mathf.Max(pointsInGrid[i].z, max.z);
				}

				// Ensure the grid isn't crazily big
				min.x = Mathf.Max(min.x, pivotPointOnGrid.x - MAJOR_LINE_DISTANCE * sanityScalar);
				min.z = Mathf.Max(min.z, pivotPointOnGrid.z - MAJOR_LINE_DISTANCE * sanityScalar);

				max.x = Mathf.Min(max.x, pivotPointOnGrid.x + MAJOR_LINE_DISTANCE * sanityScalar);
				max.z = Mathf.Min(max.z, pivotPointOnGrid.z + MAJOR_LINE_DISTANCE * sanityScalar);

				// Now calculate grid line count
				xCount = Mathf.CeilToInt((max.x - min.x) / snapDistance);
				yCount = Mathf.CeilToInt((max.z - min.z) / snapDistance);

				xMid = Mathf.RoundToInt((min.x + max.x) / 2f  / snapDistance);
				yMid = Mathf.RoundToInt((min.z + max.z) / 2f  / snapDistance);
			}

			// Pad out the edge case
			xCount += 2;
			yCount += 2;

			int xStart = xMid - xCount/2;
			int yStart = yMid - yCount/2;

			int xEnd = xMid + xCount/2;
			int yEnd = yMid + yCount/2;


			Vector3 cameraPositionOnPlane = new Vector3(transform.position.x, 0, transform.position.z);

			// Start rendering
			SabreCSGResources.GetSelectedBrushMaterial().SetPass(0);
			GL.Begin(GL.LINES);

			int lineDistributonWorld = 8;

			while(lineDistributonWorld <= snapDistance)
			{
				lineDistributonWorld *= 8;
			}

			// Calculate repetition count of the distribution lines
			int lineDistributionCount;

			if(snapDistance < lineDistributonWorld)
			{
				lineDistributionCount = (int)(lineDistributonWorld / snapDistance);
			}
			else
			{
				lineDistributionCount = 1;
			}

			int gridJump = 8;

			// Ensure we don't split grid lines at too high a density when drawing as that would kill performance
			if(snapDistance < 1)
			{
				gridJump = Mathf.RoundToInt(gridJump / snapDistance);
			}

			for (int y = yStart; y <= yEnd; y ++) 
			{
				Color sourceColor = normalLine;
				bool majorLine = true;
				if(y == 0)
				{
					sourceColor = xAxisColor;
				}
				else if(y % lineDistributionCount != 0)
				{
					majorLine = false;
					if(!scaledUp)
					{
						sourceColor = smallestLine;
					}

					sourceColor.a *= minorLineAlphaMultiplier * 0.6f;
				}

				float activeDistance = sanityScalar * (majorLine ? MAJOR_LINE_DISTANCE : MINOR_LINE_DISTANCE);

				for (int x = xStart; x < xEnd; x+= gridJump) 
				{
					Color color = sourceColor;

					Vector3 yOffset = y * yDirection * snapDistance;

					Vector3 center = depthOffset + yOffset;

					Vector3 testPoint = center + xDirection * (x+gridJump/2f) * snapDistance;
					testPoint.y = 0;
					float squareDistance = (testPoint - cameraPositionOnPlane).sqrMagnitude;

					if(squareDistance > (activeDistance * activeDistance))
					{
						continue;
					}

					float distance = Mathf.Sqrt(squareDistance);

					color.a *= Mathf.InverseLerp(activeDistance, activeDistance/2f, distance);

					GL.Color(color);

					GL.Vertex(center + xDirection * (x+0) * snapDistance); 
					GL.Vertex(center + xDirection * (x+gridJump) * snapDistance);
				}
			}

			for (int x = xStart; x <= xEnd; x++) 
			{
				Color sourceColor = normalLine;
				bool majorLine = true;
				if(x == 0)
				{
					sourceColor = yAxisColor;
				}
				else if(x % lineDistributionCount != 0)
				{
					majorLine = false;
					if(!scaledUp)
					{
						sourceColor = smallestLine;
					}

					sourceColor.a *= minorLineAlphaMultiplier * 0.6f;
				}

				float activeDistance = sanityScalar * (majorLine ? MAJOR_LINE_DISTANCE : MINOR_LINE_DISTANCE);

				for (int y = yStart; y < yEnd; y+=gridJump) 
				{
					Color color = sourceColor;

					Vector3 xOffset = x * xDirection * snapDistance;

					Vector3 center = depthOffset + xOffset;

					Vector3 testPoint = center + yDirection * (y+gridJump/2f) * snapDistance;
					testPoint.y = 0;
					float squareDistance = (testPoint - cameraPositionOnPlane).sqrMagnitude;

					if(squareDistance > (activeDistance * activeDistance))
					{
						continue;
					}

					float distance = Mathf.Sqrt(squareDistance);

					color.a *= Mathf.InverseLerp(activeDistance, activeDistance * 0.5f, distance);


					GL.Color(color);

					GL.Vertex(center + yDirection * (y+0) * snapDistance); 
					GL.Vertex(center + yDirection * (y+gridJump) * snapDistance);
				}
			}

			GL.End();
		}
	}
}
#endif