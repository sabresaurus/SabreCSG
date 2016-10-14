#if UNITY_EDITOR
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
    public enum ResizeType
    {
        Corner,
        EdgeMid,
        FaceMid
    };

    // Used by ResizeEditor to describe two handles (e.g. X axis resize handles)
    public struct ResizeHandlePair
    {
        public delegate Vector3 PointTransformer(Vector3 sourcePoint);

        public Vector3 point1;
        public Vector3 point2;
        ResizeType resizeType;

        public ResizeType ResizeType
        {
            get
            {
                return resizeType;
            }
        }

        public ResizeHandlePair(Vector3 point1)
        {
            this.point1 = point1;
            this.point2 = -1 * point1;

            if (point1.sqrMagnitude == 1)
            {
                resizeType = ResizeType.FaceMid;
            }
            else if (point1.sqrMagnitude == 2)
            {
                resizeType = ResizeType.EdgeMid;
            }
            else
            {
                resizeType = ResizeType.Corner;
            }
        }

        public Vector3 GetPoint(int pointIndex)
        {
            if (pointIndex == 0)
                return point1;
            else if (pointIndex == 1)
                return point2;
            else
                throw new System.IndexOutOfRangeException("Supplied point index should be 0 or 1");
        }

        public override bool Equals(object obj)
        {
            if (obj is ResizeHandlePair)
            {
                return this == (ResizeHandlePair)obj;
            }
            else
            {
                return false;
            }
        }

        public static bool operator ==(ResizeHandlePair lhs, ResizeHandlePair rhs)
        {
            return lhs.point1 == rhs.point1 && lhs.point2 == rhs.point2;
        }

        public static bool operator !=(ResizeHandlePair lhs, ResizeHandlePair rhs)
        {
            return lhs.point1 != rhs.point1 || lhs.point2 != rhs.point2;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public bool InClickZone(PointTransformer TransformPoint, Vector2 mousePosition, int pointIndex, Bounds bounds)
        {
            Vector3 worldPosition = TransformPoint(bounds.center + GetPoint(pointIndex).Multiply(bounds.extents));
            Vector3 targetScreenPosition = Camera.current.WorldToScreenPoint(worldPosition);

            float screenDistancePoints = CalculateScreenRange(TransformPoint, pointIndex, bounds);

            if (EditorHelper.InClickZone(mousePosition, targetScreenPosition, screenDistancePoints))
            {
                //Debug.Log(Mathf.Round(screenDistancePoints) + " " + Mathf.Round(screenBoundsSizePoints));

                return true;
            }
            else
            {
                return false;
            }
        }

        public float CalculateScreenRange(PointTransformer TransformPoint, int pointIndex, Bounds bounds)
        {
            float screenBoundsSizePoints = CalculateScreenSize(TransformPoint, pointIndex, bounds);

            // tolerance = (screenSize ^ 1.2) / 20, meaning 50 => 5.5, 80 => 9.6
            float screenDistancePoints = Mathf.Pow(screenBoundsSizePoints, 1.2f) / 20f;
            // Clamp to the 5 to 15 points range
            screenDistancePoints = Mathf.Clamp(screenDistancePoints, 5, 12);

            return screenDistancePoints;
        }

        float CalculateScreenSize(PointTransformer TransformPoint, int pointIndex, Bounds bounds)
        {
            float minDistancPoints = float.PositiveInfinity;

            Vector3 pairPoint = GetPoint(pointIndex);

            // Process each set component separately, this way we can calculate the min screen size of each active face
            for (int i = 0; i < 3; i++)
            {
                if (pairPoint[i] != 0)
                {
                    Vector3 sourceDirection = Vector3.zero;
                    sourceDirection[i] = pairPoint[i];

                    Vector3 extent1Positive = sourceDirection;
                    Vector3 extent1Negative = sourceDirection;
                    Vector3 extent2Positive = sourceDirection;
                    Vector3 extent2Negative = sourceDirection;

                    if (i == 0) // X already set, so set Y and Z
                    {
                        extent1Positive.y = 1;
                        extent1Negative.y = -1;
                        extent2Positive.z = 1;
                        extent2Negative.z = -1;
                    }
                    else if (i == 1) // Y already set, so set X and Z
                    {
                        extent1Positive.x = 1;
                        extent1Negative.x = -1;
                        extent2Positive.z = 1;
                        extent2Negative.z = -1;
                    }
                    else // Z already set, so set X and Y
                    {
                        extent1Positive.x = 1;
                        extent1Negative.x = -1;
                        extent2Positive.y = 1;
                        extent2Negative.y = -1;
                    }

                    Vector3 worldPosition1Positive = TransformPoint(bounds.center + extent1Positive.Multiply(bounds.extents));
                    Vector3 worldPosition1Negative = TransformPoint(bounds.center + extent1Negative.Multiply(bounds.extents));
                    Vector3 worldPosition2Positive = TransformPoint(bounds.center + extent2Positive.Multiply(bounds.extents));
                    Vector3 worldPosition2Negative = TransformPoint(bounds.center + extent2Negative.Multiply(bounds.extents));

                    //VisualDebug.AddPoints(worldPosition1Positive, worldPosition1Negative, worldPosition2Positive, worldPosition2Negative);

                    Vector3 screenPosition1Positive = Camera.current.WorldToScreenPoint(worldPosition1Positive);
                    Vector3 screenPosition1Negative = Camera.current.WorldToScreenPoint(worldPosition1Negative);
                    Vector3 screenPosition2Positive = Camera.current.WorldToScreenPoint(worldPosition2Positive);
                    Vector3 screenPosition2Negative = Camera.current.WorldToScreenPoint(worldPosition2Negative);

                    float distance1Points = EditorHelper.ConvertScreenPixelsToPoints(Vector2.Distance(screenPosition1Positive, screenPosition1Negative));
                    float distance2Points = EditorHelper.ConvertScreenPixelsToPoints(Vector2.Distance(screenPosition2Positive, screenPosition2Negative));

                    minDistancPoints = Mathf.Min(minDistancPoints, distance1Points);
                    minDistancPoints = Mathf.Min(minDistancPoints, distance2Points);
                }
            }
            return minDistancPoints;
        }

    }
}
#endif