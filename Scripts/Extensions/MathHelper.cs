using UnityEngine;

namespace Sabresaurus.SabreCSG
{
	public static class MathHelper
	{
        /// <summary>
        /// Since floating-point math is imprecise we use a smaller value of 0.00001 (1e-5f) to check
        /// for equality of two floats instead of absolute zero.
        /// </summary>
        public const float EPSILON_5 = 1e-5f;
        /// <summary>
        /// Since floating-point math is imprecise we use a smaller value of 0.0001 (1e-4f).
        /// </summary>
        public const float EPSILON_4 = 1e-4f;
        /// <summary>
        /// Since floating-point math is imprecise we use a smaller value of 0.001 (1e-3f).
        /// </summary>
        public const float EPSILON_3 = 1e-3f;
        /// <summary>
        /// Since floating-point math is imprecise we use a smaller value of 0.01 (1e-2f).
        /// </summary>
        public const float EPSILON_2 = 1e-2f;
        /// <summary>
        /// Since floating-point math is imprecise we use a smaller value of 0.1 (1e-1f).
        /// </summary>
        public const float EPSILON_1 = 1e-1f;
        /// <summary>
        /// Since floating-point math is imprecise we use a smaller value of 0.003.
        /// </summary>
        public const float EPSILON_3_3 = 0.003f;

        public static int GetSideThick(Plane plane, Vector3 point)
        {
            float dot = Vector3.Dot(plane.normal, point) + plane.distance;

            if (dot > 0.02f)
            {
                return 1;
            }
            else if (dot < -0.02f)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }

        public static Vector2 Vector2Cross(Vector2 vector)
        {
            return new Vector2(vector.y, -vector.x);
        }

        public static float InverseLerpNoClamp(float from, float to, float value)
	    {
	        if (from < to)
	        {
	            value -= from;
	            value /= to - from;
	            return value;
	        }
	        else
	        {
	            return 1f - (value - to) / (from - to);
	        }
	    }

	    public static Vector3 VectorInDirection(Vector3 sourceVector, Vector3 direction)
	    {
	        return direction * Vector3.Dot(sourceVector, direction);
	    }

	    public static Vector3 ClosestPointOnPlane(Vector3 point, Plane plane)
	    {
	        float signedDistance = plane.GetDistanceToPoint(point);

	        return point - plane.normal * signedDistance;
	    }

	    // From http://answers.unity3d.com/questions/344630/how-would-i-find-the-closest-vector3-point-to-a-gi.html
	    public static float DistanceToRay(Vector3 X0, Ray ray)
	    {
	        Vector3 X1 = ray.origin; // get the definition of a line from the ray
	        Vector3 X2 = ray.origin + ray.direction;
	        Vector3 X0X1 = (X0 - X1);
	        Vector3 X0X2 = (X0 - X2);

	        return (Vector3.Cross(X0X1, X0X2).magnitude / (X1 - X2).magnitude); // magic
	    }

	    // From: http://wiki.unity3d.com/index.php/3d_Math_functions
	    // Two non-parallel lines which may or may not touch each other have a point on each line which are closest
	    // to each other. This function finds those two points. If the lines are not parallel, the function 
	    // outputs true, otherwise false.
	    public static bool ClosestPointsOnTwoLines(out Vector3 closestPointLine1, out Vector3 closestPointLine2, Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2)
	    {
	        closestPointLine1 = Vector3.zero;
	        closestPointLine2 = Vector3.zero;

	        float a = Vector3.Dot(lineVec1, lineVec1);
	        float b = Vector3.Dot(lineVec1, lineVec2);
	        float e = Vector3.Dot(lineVec2, lineVec2);

	        float d = (a * e) - (b * b);

	        //lines are not parallel
	        if (d != 0.0f)
	        {
	            Vector3 r = linePoint1 - linePoint2;
	            float c = Vector3.Dot(lineVec1, r);
	            float f = Vector3.Dot(lineVec2, r);

	            float s = (b * f - c * e) / d;
	            float t = (a * f - c * b) / d;

	            closestPointLine1 = linePoint1 + lineVec1 * Mathf.Clamp01(s);
				closestPointLine2 = linePoint2 + lineVec2 * Mathf.Clamp01(t);

	            return true;
	        }
	        else
	        {
	            return false;
	        }
	    }

		// From: http://wiki.unity3d.com/index.php/3d_Math_functions
		//This function finds out on which side of a line segment the point is located.
		//The point is assumed to be on a line created by linePoint1 and linePoint2. If the point is not on
		//the line segment, project it on the line using ProjectPointOnLine() first.
		//Returns 0 if point is on the line segment.
		//Returns 1 if point is outside of the line segment and located on the side of linePoint1.
		//Returns 2 if point is outside of the line segment and located on the side of linePoint2.
		public static int PointOnWhichSideOfLineSegment(Vector3 linePoint1, Vector3 linePoint2, Vector3 point){

			Vector3 lineVec = linePoint2 - linePoint1;
			Vector3 pointVec = point - linePoint1;

			float dot = Vector3.Dot(pointVec, lineVec);

			//point is on side of linePoint2, compared to linePoint1
			if(dot > 0){

				//point is on the line segment
				if(pointVec.magnitude <= lineVec.magnitude){

					return 0;
				}

				//point is not on the line segment and it is on the side of linePoint2
				else{

					return 2;
				}
			}

			//Point is not on side of linePoint2, compared to linePoint1.
			//Point is not on the line segment and it is on the side of linePoint1.
			else{

				return 1;
			}
		}
		
		// From: http://wiki.unity3d.com/index.php/3d_Math_functions
		//This function returns a point which is a projection from a point to a line.
		//The line is regarded infinite. If the line is finite, use ProjectPointOnLineSegment() instead.
		public static Vector3 ProjectPointOnLine(Vector3 linePoint, Vector3 lineVec, Vector3 point){		

			//get vector from point on line to point in space
			Vector3 linePointToPoint = point - linePoint;

			float t = Vector3.Dot(linePointToPoint, lineVec);

			return linePoint + lineVec * t;
		}
		
		// From: http://wiki.unity3d.com/index.php/3d_Math_functions
		//This function returns a point which is a projection from a point to a line segment.
		//If the projected point lies outside of the line segment, the projected point will 
		//be clamped to the appropriate line edge.
		//If the line is infinite instead of a segment, use ProjectPointOnLine() instead.
		public static Vector3 ProjectPointOnLineSegment(Vector3 linePoint1, Vector3 linePoint2, Vector3 point){

			Vector3 vector = linePoint2 - linePoint1;

			Vector3 projectedPoint = ProjectPointOnLine(linePoint1, vector.normalized, point);

			int side = PointOnWhichSideOfLineSegment(linePoint1, linePoint2, projectedPoint);

			//The projected point is on the line segment
			if(side == 0){

				return projectedPoint;
			}

			if(side == 1){

				return linePoint1;
			}

			if(side == 2){

				return linePoint2;
			}

			//output is invalid
			return Vector3.zero;
		}

		public static Vector3 ClosestPointOnLine(Ray ray, Vector3 lineStart, Vector3 lineEnd)
		{
			Vector3 rayStart = ray.origin;
			Vector3 rayDirection = ray.direction * 10000;

			// Outputs
			Vector3 closestPointLine1;
			Vector3 closestPointLine2;
			
			MathHelper.ClosestPointsOnTwoLines(out closestPointLine1, out closestPointLine2, rayStart, rayDirection, lineStart, lineEnd - lineStart);

			// Only interested in the closest point on the line (lineStart -> lineEnd), not the ray
			return closestPointLine1;
		}



	    public static float RoundFloat(float value, float gridScale)
	    {
	        float reciprocal = 1f / gridScale;
	        return gridScale * Mathf.Round(reciprocal * value);
		}
		
		public static Vector3 RoundVector3(Vector3 vector)
		{
			vector.x = Mathf.Round(vector.x);
			vector.y = Mathf.Round(vector.y);
			vector.z = Mathf.Round(vector.z);
			return vector;
		}
		
		public static Vector3 RoundVector3(Vector3 vector, float gridScale)
		{
			// By dividing the source value by the scale, rounding it, then rescaling it, we calculate the rounding
			float reciprocal = 1f / gridScale;
			vector.x = gridScale * Mathf.Round(reciprocal * vector.x);
			vector.y = gridScale * Mathf.Round(reciprocal * vector.y);
			vector.z = gridScale * Mathf.Round(reciprocal * vector.z);
			return vector;
		}
		
		public static Vector2 RoundVector2(Vector3 vector)
		{
			vector.x = Mathf.Round(vector.x);
			vector.y = Mathf.Round(vector.y);
			return vector;
		}
		
		public static Vector2 RoundVector2(Vector2 vector, float gridScale)
		{
			// By dividing the source value by the scale, rounding it, then rescaling it, we calculate the rounding
			float reciprocal = 1f / gridScale;
			vector.x = gridScale * Mathf.Round(reciprocal * vector.x);
			vector.y = gridScale * Mathf.Round(reciprocal * vector.y);
			return vector;
		}

	    public static Vector3 VectorAbs(Vector3 vector)
	    {
	        vector.x = Mathf.Abs(vector.x);
	        vector.y = Mathf.Abs(vector.y);
	        vector.z = Mathf.Abs(vector.z);
	        return vector;
	    }

	    public static int Wrap(int i, int range)
	    {
	        if (i < 0)
	        {
	            i = range - 1;
	        }
	        if (i >= range)
	        {
	            i = 0;
	        }
	        return i;
	    }

		public static float Wrap(float i, float range)
		{
			if (i < 0)
			{
				i = range - 1;
			}
			if (i >= range)
			{
				i = 0;
			}
			return i;
		}


		public static float WrapAngle(float angle)
		{
			while(angle > 180)
			{
				angle -= 360;
			}
			while(angle <= -180)
			{
				angle += 360;
			}
			return angle;
		}

		public static bool PlaneEqualsLooser(Plane plane1, Plane plane2)
		{
			if(
				Mathf.Abs(plane1.distance - plane2.distance) < EPSILON_4
                && Mathf.Abs(plane1.normal.x - plane2.normal.x) < EPSILON_4
                && Mathf.Abs(plane1.normal.y - plane2.normal.y) < EPSILON_4
                && Mathf.Abs(plane1.normal.z - plane2.normal.z) < EPSILON_4)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

        public static bool PlaneEqualsLooserWithFlip(Plane plane1, Plane plane2)
        {
            if (
                Mathf.Abs(plane1.distance - plane2.distance) <= 0.08f
                && Mathf.Abs(plane1.normal.x - plane2.normal.x) < 0.006f
                && Mathf.Abs(plane1.normal.y - plane2.normal.y) < 0.006f
                && Mathf.Abs(plane1.normal.z - plane2.normal.z) < 0.006f)
            {
                return true;
            }
            else if (
                Mathf.Abs(-plane1.distance - plane2.distance) <= 0.08f
                && Mathf.Abs(-plane1.normal.x - plane2.normal.x) < 0.006f
                && Mathf.Abs(-plane1.normal.y - plane2.normal.y) < 0.006f
                && Mathf.Abs(-plane1.normal.z - plane2.normal.z) < 0.006f)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool PlaneEquals(Plane plane1, Plane plane2)
        {
            if (plane1.distance.EqualsWithEpsilon(plane2.distance) && plane1.normal.EqualsWithEpsilon(plane2.normal))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool IsVectorInteger(Vector3 vector)
		{
			if(vector.x % 1f != 0
				|| vector.y % 1f != 0
				|| vector.z % 1f != 0)
			{
				return false;
			}
			else
			{
				return true;
			}
		}

		public static bool IsVectorOnGrid(Vector3 position, Vector3 mask, float gridScale)
		{
			if(mask.x != 0)
			{
				if(position.x % gridScale != 0)
				{
					return false;
				}
			}

			if(mask.y != 0)
			{
				if(position.y % gridScale != 0)
				{
					return false;
				}
			}

			if(mask.z != 0)
			{
				if(position.z % gridScale != 0)
				{
					return false;
				}
			}

			return true;
		}
	}
}