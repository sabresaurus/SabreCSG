#if UNITY_EDITOR

using UnityEngine;
using System.Collections;
using UnityEditor;

namespace Sabresaurus.SabreCSG
{
    public static class SabreGraphics
    {
        // Screen position right at the front (note can't use 1, because even though OSX accepts it Windows doesn't)
        public const float FRONT_Z_DEPTH = 0.99f;

        public static void DrawBillboardQuad(Vector3 screenPosition, int width, int height, bool specifiedPoints = true)
        {
#if UNITY_5_4_OR_NEWER
            if (specifiedPoints)
            {
                // Convert from points to pixels
                float scale = EditorGUIUtility.pixelsPerPoint;
                width = Mathf.RoundToInt(scale * width);
                height = Mathf.RoundToInt(scale * height);
            }
#endif

            screenPosition.z = FRONT_Z_DEPTH;

            GL.TexCoord2(0, 1); // TL
            GL.Vertex(screenPosition + new Vector3(-width / 2, -height / 2, 0));
            GL.TexCoord2(1, 1); // TR
            GL.Vertex(screenPosition + new Vector3(width / 2, -height / 2, 0));
            GL.TexCoord2(1, 0); // BR
            GL.Vertex(screenPosition + new Vector3(width / 2, height / 2, 0));
            GL.TexCoord2(0, 0); // BL
            GL.Vertex(screenPosition + new Vector3(-width / 2, height / 2, 0));
        }

        public static void DrawScreenLine(Vector3 screenPosition1, Vector3 screenPosition2)
        {
            screenPosition1.z = FRONT_Z_DEPTH;
            GL.Vertex(screenPosition1);

            screenPosition2.z = FRONT_Z_DEPTH;
            GL.Vertex(screenPosition2);
        }

        public static void DrawScreenLineDashed(Vector3 screenPosition1, Vector3 screenPosition2)
        {
            screenPosition1.z = FRONT_Z_DEPTH;
            GL.TexCoord2(0, 0);
            GL.Vertex(screenPosition1);

            screenPosition2.z = FRONT_Z_DEPTH;
            GL.TexCoord2(Vector3.Distance(screenPosition1, screenPosition2) / 50f, 0);
            GL.Vertex(screenPosition2);
        }

        public static void DrawScreenRectFill(Rect rect)
        {
            Vector3 topLeft = new Vector3(rect.xMin, rect.yMin, FRONT_Z_DEPTH);
            Vector3 topRight = new Vector3(rect.xMax, rect.yMin, FRONT_Z_DEPTH);
            Vector3 bottomLeft = new Vector3(rect.xMin, rect.yMax, FRONT_Z_DEPTH);
            Vector3 bottomRight = new Vector3(rect.xMax, rect.yMax, FRONT_Z_DEPTH);

            GL.Vertex(topLeft);
            GL.Vertex(topRight);
            GL.Vertex(bottomRight);
            GL.Vertex(bottomLeft);

            GL.Vertex(bottomLeft);
            GL.Vertex(bottomRight);
            GL.Vertex(topRight);
            GL.Vertex(topLeft);
        }

        public static void DrawScreenRectOuter(Rect rect)
        {
            Vector3 topLeft = new Vector3(rect.xMin, rect.yMin, FRONT_Z_DEPTH);
            Vector3 topRight = new Vector3(rect.xMax, rect.yMin, FRONT_Z_DEPTH);
            Vector3 bottomLeft = new Vector3(rect.xMin, rect.yMax, FRONT_Z_DEPTH);
            Vector3 bottomRight = new Vector3(rect.xMax, rect.yMax, FRONT_Z_DEPTH);

            GL.Vertex(topLeft);
            GL.Vertex(topRight);

            GL.Vertex(bottomLeft);
            GL.Vertex(bottomRight);

            GL.Vertex(topLeft);
            GL.Vertex(bottomLeft);

            GL.Vertex(bottomRight);
            GL.Vertex(topRight);
        }

        public static void DrawBox(Bounds bounds, Transform transform = null)
        {
            Vector3 center = bounds.center;

            // Calculate each of the transformed axis with their corresponding length
            Vector3 up = Vector3.up * bounds.extents.y;
            Vector3 right = Vector3.right * bounds.extents.x;
            Vector3 forward = Vector3.forward * bounds.extents.z;

            if (transform != null)
            {
                center = transform.TransformPoint(bounds.center);

                // Calculate each of the transformed axis with their corresponding length
                up = transform.TransformVector(Vector3.up) * bounds.extents.y;
                right = transform.TransformVector(Vector3.right) * bounds.extents.x;
                forward = transform.TransformVector(Vector3.forward) * bounds.extents.z;
            }

            // Verticals
            GL.Vertex(center - right - forward + up);
            GL.Vertex(center - right - forward - up);
            GL.Vertex(center - right + forward + up);
            GL.Vertex(center - right + forward - up);
            GL.Vertex(center + right - forward + up);
            GL.Vertex(center + right - forward - up);
            GL.Vertex(center + right + forward + up);
            GL.Vertex(center + right + forward - up);

            // Horizontal - forward/back
            GL.Vertex(center - right + forward - up);
            GL.Vertex(center - right - forward - up);
            GL.Vertex(center + right + forward - up);
            GL.Vertex(center + right - forward - up);
            GL.Vertex(center - right + forward + up);
            GL.Vertex(center - right - forward + up);
            GL.Vertex(center + right + forward + up);
            GL.Vertex(center + right - forward + up);

            // Horizontal - right/left
            GL.Vertex(center + right - forward - up);
            GL.Vertex(center - right - forward - up);
            GL.Vertex(center + right + forward - up);
            GL.Vertex(center - right + forward - up);
            GL.Vertex(center + right - forward + up);
            GL.Vertex(center - right - forward + up);
            GL.Vertex(center + right + forward + up);
            GL.Vertex(center - right + forward + up);
        }

        public static void DrawBoxGuideLines(Bounds bounds, float length, Transform transform = null)
        {
            Vector3 center = bounds.center;

            // Calculate each of the transformed axis with their corresponding length
            Vector3 up = Vector3.up * bounds.extents.y;
            Vector3 right = Vector3.right * bounds.extents.x;
            Vector3 forward = Vector3.forward * bounds.extents.z;

            Vector3 gup = Vector3.up * length;
            Vector3 gright = Vector3.right * length;
            Vector3 gforward = Vector3.forward * length;

            Color transparentr = new Color(1.0f, 0.0f, 0.0f, 0.35f);
            Color transparentg = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentb = new Color(0.0f, 0.0f, 1.0f, 0.35f);

            if (transform != null)
            {
                center = transform.TransformPoint(bounds.center);

                // Calculate each of the transformed axis with their corresponding length
                up = transform.TransformVector(Vector3.up) * bounds.extents.y;
                right = transform.TransformVector(Vector3.right) * bounds.extents.x;
                forward = transform.TransformVector(Vector3.forward) * bounds.extents.z;

                gup = transform.TransformVector(Vector3.up) * length;
                gright = transform.TransformVector(Vector3.right) * length;
                gforward = transform.TransformVector(Vector3.forward) * length;
            }

            // Vertical Guide Lines
            GL.Color(Color.green); GL.Vertex(center - right - forward + up); GL.Color(transparentg); GL.Vertex(center - right - forward + up + gup);
            GL.Color(Color.green); GL.Vertex(center - right - forward - up); GL.Color(transparentg); GL.Vertex(center - right - forward - up - gup);
            GL.Color(Color.green); GL.Vertex(center - right + forward + up); GL.Color(transparentg); GL.Vertex(center - right + forward + up + gup);
            GL.Color(Color.green); GL.Vertex(center - right + forward - up); GL.Color(transparentg); GL.Vertex(center - right + forward - up - gup);
            GL.Color(Color.green); GL.Vertex(center + right - forward + up); GL.Color(transparentg); GL.Vertex(center + right - forward + up + gup);
            GL.Color(Color.green); GL.Vertex(center + right - forward - up); GL.Color(transparentg); GL.Vertex(center + right - forward - up - gup);
            GL.Color(Color.green); GL.Vertex(center + right + forward + up); GL.Color(transparentg); GL.Vertex(center + right + forward + up + gup);
            GL.Color(Color.green); GL.Vertex(center + right + forward - up); GL.Color(transparentg); GL.Vertex(center + right + forward - up - gup);

            // Horizontal Guide Lines - forward/back
            GL.Color(Color.blue); GL.Vertex(center - right + forward - up); GL.Color(transparentb); GL.Vertex(center - right + forward - up + gforward);
            GL.Color(Color.blue); GL.Vertex(center - right - forward - up); GL.Color(transparentb); GL.Vertex(center - right - forward - up - gforward);
            GL.Color(Color.blue); GL.Vertex(center + right + forward - up); GL.Color(transparentb); GL.Vertex(center + right + forward - up + gforward);
            GL.Color(Color.blue); GL.Vertex(center + right - forward - up); GL.Color(transparentb); GL.Vertex(center + right - forward - up - gforward);
            GL.Color(Color.blue); GL.Vertex(center - right + forward + up); GL.Color(transparentb); GL.Vertex(center - right + forward + up + gforward);
            GL.Color(Color.blue); GL.Vertex(center - right - forward + up); GL.Color(transparentb); GL.Vertex(center - right - forward + up - gforward);
            GL.Color(Color.blue); GL.Vertex(center + right + forward + up); GL.Color(transparentb); GL.Vertex(center + right + forward + up + gforward);
            GL.Color(Color.blue); GL.Vertex(center + right - forward + up); GL.Color(transparentb); GL.Vertex(center + right - forward + up - gforward);

            // Horizontal Guide Lines - right/left
            GL.Color(Color.red); GL.Vertex(center + right - forward - up); GL.Color(transparentr); GL.Vertex(center + right - forward - up + gright);
            GL.Color(Color.red); GL.Vertex(center - right - forward - up); GL.Color(transparentr); GL.Vertex(center - right - forward - up - gright);
            GL.Color(Color.red); GL.Vertex(center + right + forward - up); GL.Color(transparentr); GL.Vertex(center + right + forward - up + gright);
            GL.Color(Color.red); GL.Vertex(center - right + forward - up); GL.Color(transparentr); GL.Vertex(center - right + forward - up - gright);
            GL.Color(Color.red); GL.Vertex(center + right - forward + up); GL.Color(transparentr); GL.Vertex(center + right - forward + up + gright);
            GL.Color(Color.red); GL.Vertex(center - right - forward + up); GL.Color(transparentr); GL.Vertex(center - right - forward + up - gright);
            GL.Color(Color.red); GL.Vertex(center + right + forward + up); GL.Color(transparentr); GL.Vertex(center + right + forward + up + gright);
            GL.Color(Color.red); GL.Vertex(center - right + forward + up); GL.Color(transparentr); GL.Vertex(center - right + forward + up - gright);
        }

        public static void DrawPlane(UnityEngine.Plane plane, Vector3 center, Color colorFront, Color colorBack, float size)
        {
            SabreCSGResources.GetPlaneMaterial().SetPass(0);

            GL.Begin(GL.QUADS);

            Vector3 normal = plane.normal.normalized;
            Vector3 tangent;

            if (normal == Vector3.up || normal == Vector3.down)
            {
                tangent = Vector3.Cross(normal, Vector3.forward).normalized;
            }
            else
            {
                tangent = Vector3.Cross(normal, Vector3.up).normalized;
            }

            Vector3 binormal = Quaternion.AngleAxis(90, normal) * tangent;

            //		GL.Color(colorFront);
            //		GL.Vertex(center + (normal * -plane.distance) - tangent * size - binormal * size);
            //		GL.Vertex(center + (normal * -plane.distance) + tangent * size - binormal * size);
            //		GL.Vertex(center + (normal * -plane.distance) + tangent * size + binormal * size);
            //		GL.Vertex(center + (normal * -plane.distance) - tangent * size + binormal * size);
            //
            //		GL.Color(colorBack);
            //		GL.Vertex(center + (normal * -plane.distance) - tangent * size + binormal * size);
            //		GL.Vertex(center + (normal * -plane.distance) + tangent * size + binormal * size);
            //		GL.Vertex(center + (normal * -plane.distance) + tangent * size - binormal * size);
            //		GL.Vertex(center + (normal * -plane.distance) - tangent * size - binormal * size);

            GL.Color(colorFront);
            GL.Vertex(center - tangent * size - binormal * size);
            GL.Vertex(center + tangent * size - binormal * size);
            GL.Vertex(center + tangent * size + binormal * size);
            GL.Vertex(center - tangent * size + binormal * size);

            GL.Color(colorBack);
            GL.Vertex(center - tangent * size + binormal * size);
            GL.Vertex(center + tangent * size + binormal * size);
            GL.Vertex(center + tangent * size - binormal * size);
            GL.Vertex(center - tangent * size - binormal * size);

            GL.End();

            GL.Begin(GL.LINES);

            GL.Color(Color.white);

            GL.Vertex(center - tangent * size + binormal * size);
            GL.Vertex(center + tangent * size + binormal * size);

            GL.Vertex(center + tangent * size + binormal * size);
            GL.Vertex(center + tangent * size - binormal * size);

            GL.Vertex(center + tangent * size - binormal * size);
            GL.Vertex(center - tangent * size - binormal * size);

            GL.Vertex(center - tangent * size - binormal * size);
            GL.Vertex(center - tangent * size + binormal * size);

            GL.Color(Color.green);

            Vector3 normalOffset = -normal * 0.01f;

            GL.Vertex(center + normalOffset - tangent * size + binormal * size);
            GL.Vertex(center + normalOffset + tangent * size + binormal * size);

            GL.Vertex(center + normalOffset + tangent * size + binormal * size);
            GL.Vertex(center + normalOffset + tangent * size - binormal * size);

            GL.Vertex(center + normalOffset + tangent * size - binormal * size);
            GL.Vertex(center + normalOffset - tangent * size - binormal * size);

            GL.Vertex(center + normalOffset - tangent * size - binormal * size);
            GL.Vertex(center + normalOffset - tangent * size + binormal * size);

            GL.Color(Color.red);

            normalOffset = normal * 0.01f;

            GL.Vertex(center + normalOffset - tangent * size + binormal * size);
            GL.Vertex(center + normalOffset + tangent * size + binormal * size);

            GL.Vertex(center + normalOffset + tangent * size + binormal * size);
            GL.Vertex(center + normalOffset + tangent * size - binormal * size);

            GL.Vertex(center + normalOffset + tangent * size - binormal * size);
            GL.Vertex(center + normalOffset - tangent * size - binormal * size);

            GL.Vertex(center + normalOffset - tangent * size - binormal * size);
            GL.Vertex(center + normalOffset - tangent * size + binormal * size);

            GL.End();
        }

        public static void DrawRotationCircle(Vector3 worldCenter, Vector3 normal, float radius, Vector3 initialRotationDirection)
        {
            Vector3 tangent;

            if (normal == Vector3.up || normal == Vector3.down)
            {
                tangent = Vector3.Cross(normal, Vector3.forward).normalized;
            }
            else
            {
                tangent = Vector3.Cross(normal, Vector3.up).normalized;
            }

            Vector3 binormal = Quaternion.AngleAxis(90, normal) * tangent;

            // Scale the tangent and binormal by the radius
            binormal *= radius;
            tangent *= radius;

            int count = 30;
            float deltaAngle = (2f * Mathf.PI) / count;

            GL.Begin(GL.TRIANGLES);
            GL.Color(new Color(1, 0, 1, 0.3f));

            for (int i = 0; i < count; i++)
            {
                GL.Vertex(worldCenter);
                GL.Vertex(worldCenter + tangent * Mathf.Sin(i * deltaAngle) + binormal * Mathf.Cos(i * deltaAngle));
                GL.Vertex(worldCenter + tangent * Mathf.Sin((i + 1) * deltaAngle) + binormal * Mathf.Cos((i + 1) * deltaAngle));
            }
            GL.End();

            GL.Begin(GL.LINES);
            GL.Color(Color.magenta);

            for (int i = 0; i < count; i++)
            {
                GL.Vertex(worldCenter + tangent * Mathf.Sin(i * deltaAngle) + binormal * Mathf.Cos(i * deltaAngle));
                GL.Vertex(worldCenter + tangent * Mathf.Sin((i + 1) * deltaAngle) + binormal * Mathf.Cos((i + 1) * deltaAngle));
            }
            GL.End();

            if (CurrentSettings.AngleSnappingEnabled)
            {
                Quaternion cancellingRotation = Quaternion.Inverse(Quaternion.LookRotation(normal));
                Vector3 planarInitialDirection = cancellingRotation * initialRotationDirection;
                float angleOffset = Mathf.Atan2(planarInitialDirection.x, planarInitialDirection.y);

                float angleSnapDistance = CurrentSettings.AngleSnapDistance;

                count = (int)(360f / angleSnapDistance);
                deltaAngle = (2f * Mathf.PI) / count;

                bool divisorOf90 = ((90 % angleSnapDistance) == 0);

                GL.Begin(GL.LINES);
                GL.Color(Color.white);

                for (int i = 0; i < count; i++)
                {
                    float totalDeltaAngleDeg = i * angleSnapDistance;
                    GL.Vertex(worldCenter + tangent * Mathf.Sin(angleOffset + i * deltaAngle) + binormal * Mathf.Cos(angleOffset + i * deltaAngle));
                    if (divisorOf90 && (totalDeltaAngleDeg % 90) == 0)
                    {
                        GL.Vertex(worldCenter + 0.7f * tangent * Mathf.Sin(angleOffset + i * deltaAngle) + 0.7f * binormal * Mathf.Cos(angleOffset + i * deltaAngle));
                    }
                    else
                    {
                        GL.Vertex(worldCenter + 0.9f * tangent * Mathf.Sin(angleOffset + i * deltaAngle) + 0.9f * binormal * Mathf.Cos(angleOffset + i * deltaAngle));
                    }
                }
                GL.End();

                // Draw a line showing the initial rotation angle, so the user can compare their current angle to it
                GL.Begin(GL.LINES);
                GL.Color(Color.yellow);

                GL.Vertex(worldCenter);
                GL.Vertex(worldCenter + initialRotationDirection);

                GL.End();
            }
        }

        public static void DrawPolygons(Color color, Transform transform, params Polygon[] polygons)
        {
            GL.Begin(GL.LINES);
            GL.Color(color);

            for (int j = 0; j < polygons.Length; j++)
            {
                Polygon polygon = polygons[j];
                for (int i = 0; i < polygon.Vertices.Length - 1; i++)
                {
                    GL.Vertex(transform.TransformPoint(polygon.Vertices[i].Position));
                    GL.Vertex(transform.TransformPoint(polygon.Vertices[i + 1].Position));
                }
                GL.Vertex(transform.TransformPoint(polygon.Vertices[polygon.Vertices.Length - 1].Position));
                GL.Vertex(transform.TransformPoint(polygon.Vertices[0].Position));
            }

            GL.End();

            GL.Begin(GL.TRIANGLES);
            color.a = 0.3f;
            GL.Color(color);

            for (int j = 0; j < polygons.Length; j++)
            {
                Polygon polygon = polygons[j];
                Vector3 position1 = polygon.Vertices[0].Position;

                for (int i = 1; i < polygon.Vertices.Length - 1; i++)
                {
                    GL.Vertex(transform.TransformPoint(position1));
                    GL.Vertex(transform.TransformPoint(polygon.Vertices[i].Position));
                    GL.Vertex(transform.TransformPoint(polygon.Vertices[i + 1].Position));
                }
            }
            GL.End();
        }

        public static void DrawPolygons(Color faceColor, Color outlineColor, params Polygon[] polygons)
        {
            GL.Begin(GL.LINES);
            GL.Color(outlineColor);

            for (int j = 0; j < polygons.Length; j++)
            {
                Vector3 offset = polygons[j].Plane.normal * 0.001f;

                Polygon polygon = polygons[j];
                for (int i = 0; i < polygon.Vertices.Length - 1; i++)
                {
                    GL.Vertex(polygon.Vertices[i].Position + offset);
                    GL.Vertex(polygon.Vertices[i + 1].Position + offset);
                }
                GL.Vertex(polygon.Vertices[polygon.Vertices.Length - 1].Position + offset);
                GL.Vertex(polygon.Vertices[0].Position + offset);
            }

            GL.End();

            GL.Begin(GL.TRIANGLES);
            GL.Color(faceColor);

            for (int j = 0; j < polygons.Length; j++)
            {
                Vector3 offset = polygons[j].Plane.normal * 0.001f;

                Polygon polygon = polygons[j];
                Vector3 position1 = polygon.Vertices[0].Position;

                for (int i = 1; i < polygon.Vertices.Length - 1; i++)
                {
                    GL.Vertex(position1 + offset);
                    GL.Vertex(polygon.Vertices[i].Position + offset);
                    GL.Vertex(polygon.Vertices[i + 1].Position + offset);
                }
            }
            GL.End();
        }

        public static void DrawPolygonsNoOutline(Color color, params Polygon[] polygons)
        {
            GL.Begin(GL.TRIANGLES);

            GL.Color(color);

            for (int j = 0; j < polygons.Length; j++)
            {
                Polygon polygon = polygons[j];
                Vector3 position1 = polygon.Vertices[0].Position;

                for (int i = 1; i < polygon.Vertices.Length - 1; i++)
                {
                    GL.Vertex(position1);
                    GL.Vertex(polygon.Vertices[i].Position);
                    GL.Vertex(polygon.Vertices[i + 1].Position);
                }
            }
            GL.End();
        }

        public static void DrawPolygonsOutline(Color color, params Polygon[] polygons)
        {
            Vector3 depthAdjust = -0.01f * SceneView.currentDrawingSceneView.camera.transform.forward;
            GL.Begin(GL.LINES);

            for (int j = 0; j < polygons.Length; j++)
            {
                Polygon polygon = polygons[j];

                GL.Color(color);

                for (int i = 0; i < polygon.Vertices.Length; i++)
                {
                    Vector3 currentPosition = polygon.Vertices[i].Position + depthAdjust;
                    Vector3 nextPosition = polygon.Vertices[(i + 1) % polygon.Vertices.Length].Position + depthAdjust;

                    GL.Vertex(currentPosition);
                    GL.Vertex(nextPosition);
                }
            }

            GL.End();
        }

        public static void DrawPolygonsOutlineDashed(Color color, params Polygon[] polygons)
        {
            Vector3 depthAdjust = -0.01f * SceneView.currentDrawingSceneView.camera.transform.forward;
            GL.Begin(GL.LINES);

            for (int j = 0; j < polygons.Length; j++)
            {
                Polygon polygon = polygons[j];

                GL.Color(color);

                for (int i = 0; i < polygon.Vertices.Length; i++)
                {
                    Vector3 currentPosition = polygon.Vertices[i].Position + depthAdjust;
                    Vector3 nextPosition = polygon.Vertices[(i + 1) % polygon.Vertices.Length].Position + depthAdjust;

                    GL.TexCoord2(0, 0);
                    GL.Vertex(currentPosition);
                    GL.TexCoord2(Vector3.Distance(nextPosition, currentPosition), 0);
                    GL.Vertex(nextPosition);
                }
            }

            GL.End();
        }

        public static void DrawMarquee(Vector2 marqueeStart, Vector2 marqueeEnd)
        {
            Vector2 point1 = EditorHelper.ConvertMousePointPosition(marqueeStart);
            Vector2 point2 = EditorHelper.ConvertMousePointPosition(marqueeEnd);

            Rect rect = new Rect(point1.x, point1.y, point2.x - point1.x, point2.y - point1.y);

            SabreCSGResources.GetSelectedBrushMaterial().SetPass(0);

            GL.PushMatrix();
            GL.LoadPixelMatrix();

            GL.Begin(GL.QUADS);
            GL.Color(new Color(0.2f, 0.3f, 0.8f, 0.3f));

            // Marquee fill (draw double sided)
            SabreGraphics.DrawScreenRectFill(rect);

            GL.End();

            // Marquee border
            GL.Begin(GL.LINES);
            GL.Color(Color.white);

            // Draw marquee box edges
            SabreGraphics.DrawScreenRectOuter(rect);

            GL.End();
            GL.PopMatrix();
        }

        //		public static void DrawThickLineTest(Vector3 testPoint1, Vector3 testPoint2, float width)
        //	    {
        //			Camera sceneViewCamera = UnityEditor.SceneView.currentDrawingSceneView.camera;
        //
        ////    		Vector3 screenPoint1 = sceneViewCamera.WorldToScreenPoint(testPoint1);
        ////    		Vector3 screenPoint2 = sceneViewCamera.WorldToScreenPoint(testPoint2);
        //
        ////    		Vector3 perpendicular = new Vector3(screenPoint2.x - screenPoint1.x, screenPoint1.y - screenPoint2.y, screenPoint1.z).normalized;
        //
        ////			Vector3 up = sceneViewCamera.transform.up;
        //			Vector3 forward = sceneViewCamera.transform.forward * 0.01f;
        //
        //			Vector3 cameraVector = sceneViewCamera.transform.position - (testPoint1 + testPoint2)/2f;
        //			Vector3 lineVector = testPoint2 - testPoint1;
        //
        //			Vector3 up = Vector3.Cross(cameraVector.normalized, lineVector.normalized);
        //
        ////			Vector3 up = Quaternion.LookRotation(sceneViewCamera.transform.forward, sceneViewCamera.transform.up) * Vector3.up;
        //
        //			GL.Color(Color.black);
        //			GL.Vertex(testPoint1 + up * width - forward);
        //			GL.Vertex(testPoint1 - up * width - forward);
        //			GL.Vertex(testPoint2 - up * width - forward);
        //			GL.Vertex(testPoint2 + up * width - forward);
        //
        //			GL.Color(Color.green);
        //			GL.Vertex(testPoint1 + up * width/2 - forward);
        //			GL.Vertex(testPoint1 - up * width/2 - forward);
        //			GL.Vertex(testPoint2 - up * width/2 - forward);
        //			GL.Vertex(testPoint2 + up * width/2 - forward);
        //	    }
    }
}

#endif