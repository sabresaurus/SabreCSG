#if UNITY_EDITOR || RUNTIME_CSG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Sabresaurus.SabreCSG.ShapeEditor.Triangulator
{
    struct LineSegment
    {
        public Vertex A;
        public Vertex B;

        public LineSegment(Vertex a, Vertex b)
        {
            A = a;
            B = b;
        }

        public float? IntersectsWithRay(Vector2 origin, Vector2 direction)
        {
            float largestDistance = Mathf.Max(A.Position.x - origin.x, B.Position.x - origin.x) * 2f;
            LineSegment raySegment = new LineSegment(new Vertex(origin, 0), new Vertex(origin + (direction * largestDistance), 0));

            Vector2? intersection = FindIntersection(this, raySegment);
            float? value = null;

            if (intersection != null)
                value = Vector2.Distance(origin, intersection.Value);

            return value;
        }

        public static Vector2? FindIntersection(LineSegment a, LineSegment b)
        {
            float x1 = a.A.Position.x;
            float y1 = a.A.Position.y;
            float x2 = a.B.Position.x;
            float y2 = a.B.Position.y;
            float x3 = b.A.Position.x;
            float y3 = b.A.Position.y;
            float x4 = b.B.Position.x;
            float y4 = b.B.Position.y;

            float denom = (y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1);

            float uaNum = (x4 - x3) * (y1 - y3) - (y4 - y3) * (x1 - x3);
            float ubNum = (x2 - x1) * (y1 - y3) - (y2 - y1) * (x1 - x3);

            float ua = uaNum / denom;
            float ub = ubNum / denom;

            if (Mathf.Clamp(ua, 0f, 1f) != ua || Mathf.Clamp(ub, 0f, 1f) != ub)
                return null;

            return a.A.Position + (a.B.Position - a.A.Position) * ua;
        }
    }
}
#endif