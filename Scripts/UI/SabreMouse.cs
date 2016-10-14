#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using UnityEditor;

namespace Sabresaurus.SabreCSG
{
    public static class SabreMouse
    {
        static MouseCursor activeCursor = MouseCursor.Arrow;

        public static MouseCursor ActiveCursor
        {
            get { return activeCursor; }
        }

        public static void ResetCursor()
        {
            activeCursor = MouseCursor.Arrow;
        }

        public static void SetCursor(MouseCursor mouseCursor)
        {
            activeCursor = mouseCursor;
        }

        public static void SetCursorFromVector3(Vector2 currentPosition, Vector2 lastPosition)
        {
            Vector3 delta = currentPosition - lastPosition;
            float angle = Mathf.Rad2Deg * Mathf.Atan2(delta.y, delta.x);

            while (angle < 0)
            {
                angle += 180;
            }

            while (angle > 180)
            {
                angle -= 180;
            }

            if (angle >= 67.5f && angle < 112.5f)
            {
                activeCursor = MouseCursor.ResizeVertical;
            }
            else if (angle >= 112.5f && angle < 157.5f)
            {
                activeCursor = MouseCursor.ResizeUpLeft;
            }
            else if (angle >= 22.5f && angle < 67.5f)
            {
                activeCursor = MouseCursor.ResizeUpRight;
            }
            else
            {
                activeCursor = MouseCursor.ResizeHorizontal;
            }
        }

		public static bool MarqueeContainsPoint(Vector2 marqueeStart, Vector2 marqueeEnd, Vector3 screenPoint)
		{
			Vector2 point1 = EditorHelper.ConvertMousePointPosition(marqueeStart);
			Vector2 point2 = EditorHelper.ConvertMousePointPosition(marqueeEnd);

			float minX = Mathf.Min(point1.x, point2.x);
			float maxX = Mathf.Max(point1.x, point2.x);

			float minY = Mathf.Min(point1.y, point2.y);
			float maxY = Mathf.Max(point1.y, point2.y);

			if(screenPoint.z > 0 && 
				screenPoint.x > minX && screenPoint.x < maxX
				&& screenPoint.y > minY && screenPoint.y < maxY)
			{
				return true;
			}
			else
			{
				return false;
			}
		}
    }
}
#endif