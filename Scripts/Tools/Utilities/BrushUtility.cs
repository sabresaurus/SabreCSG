#if UNITY_EDITOR || RUNTIME_CSG
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Sabresaurus.SabreCSG
{
    /// <summary>
    /// Provides utility methods for manipulating brushes
    /// </summary>
    public static class BrushUtility
    {
        [Obsolete("Rescale method has been renamed to Scale")]
        public static void Rescale(PrimitiveBrush brush, Vector3 rescaleValue)
        {
            Scale(brush, rescaleValue);
        }

        /// <summary>
        /// Scales the brush by a local Vector3 scale from its pivot
        /// </summary>
        /// <param name="brush">The brush to be rescaled</param>
        /// <param name="rescaleValue">Local scale to apply</param>
        public static void Scale(PrimitiveBrush brush, Vector3 scaleValue)
        {
            Polygon[] polygons = brush.GetPolygons();

            for (int i = 0; i < polygons.Length; i++)
            {
                Polygon polygon = polygons[i];

                polygons[i].CalculatePlane();
                Vector3 previousPlaneNormal = polygons[i].Plane.normal;

                int vertexCount = polygon.Vertices.Length;

                Vector3[] newPositions = new Vector3[vertexCount];
                Vector2[] newUV = new Vector2[vertexCount];

                for (int j = 0; j < vertexCount; j++)
                {
                    newPositions[j] = polygon.Vertices[j].Position;
                    newUV[j] = polygon.Vertices[j].UV;
                }

                for (int j = 0; j < vertexCount; j++)
                {
                    Vertex vertex = polygon.Vertices[j];

                    Vector3 newPosition = vertex.Position.Multiply(scaleValue);
                    newPositions[j] = newPosition;

                    newUV[j] = GeometryHelper.GetUVForPosition(polygon, newPosition);
                }

                // Apply all the changes to the polygon
                for (int j = 0; j < vertexCount; j++)
                {
                    Vertex vertex = polygon.Vertices[j];
                    vertex.Position = newPositions[j];
                    vertex.UV = newUV[j];
                }

                // Polygon geometry has changed, inform the polygon that it needs to recalculate its cached plane
                polygons[i].CalculatePlane();

                Vector3 newPlaneNormal = polygons[i].Plane.normal;

                // Find the rotation from the original polygon plane to the new polygon plane
                Quaternion normalRotation = Quaternion.FromToRotation(previousPlaneNormal, newPlaneNormal);

                // Rotate all the vertex normals by the new rotation
                for (int j = 0; j < vertexCount; j++)
                {
                    Vertex vertex = polygon.Vertices[j];
                    vertex.Normal = normalRotation * vertex.Normal;
                }
            }
#if UNITY_EDITOR
            EditorHelper.SetDirty(brush);
#endif
            brush.Invalidate(true);
        }

        /// <summary>
        /// Resizes the brush so that it's local bounds match the specified extents
        /// </summary>
        /// <param name="brush">The brush to be resized</param>
        /// <param name="rescaleValue">The extents to match</param>
        public static void Resize(PrimitiveBrush brush, Vector3 resizeValue)
        {
            Bounds bounds = brush.GetBounds();
            // Calculate the rescale vector required to change the bounds to the resize vector
            Vector3 rescaleVector3 = resizeValue.Divide(bounds.size);
            Scale(brush, rescaleVector3);
        }

        /// <summary>
        /// Flips brushes along the provided axis 
        /// </summary>
        /// <param name="primaryTargetBrush">The brush considered to be the pivot brush, for use when localToPrimaryBrush is true</param>
        /// <param name="targetBrushes">All brushes to be flipped</param>
        /// <param name="axisIndex">Index of the axis component to flip along, 0 = X, 1 = Y, 2 = Z</param>
        /// <param name="localToPrimaryBrush">Whether the axis to flip in is local to the primary brush's rotation, if false global orientation is used</param>
        /// <param name="flipCenter">The point in world space at which to flip the geometry around</param>
        public static void Flip(PrimitiveBrush primaryTargetBrush, PrimitiveBrush[] targetBrushes, int axisIndex, bool localToPrimaryBrush, Vector3 flipCenter)
        {
            foreach (PrimitiveBrush brush in targetBrushes)
            {
                Polygon[] polygons = brush.GetPolygons();

                for (int i = 0; i < polygons.Length; i++)
                {
                    for (int j = 0; j < polygons[i].Vertices.Length; j++)
                    {
                        Vertex vertex = polygons[i].Vertices[j];

                        Vector3 position = vertex.Position;
                        Vector3 normal = vertex.Normal;

                        if (localToPrimaryBrush)
                        {
                            // Rotate the position and normal to be relative to the primary brush's local space
                            position = primaryTargetBrush.transform.InverseTransformDirection(brush.transform.TransformDirection(position));
                            normal = primaryTargetBrush.transform.InverseTransformDirection(brush.transform.TransformDirection(normal));
                        }
                        else
                        {
                            // Rotate the position and normal to be relative to the global axis orientation
                            position = brush.transform.TransformDirection(position);
                            normal = brush.transform.TransformDirection(normal);
                        }

                        // Flip in relevant axis
                        position[axisIndex] = -position[axisIndex];
                        normal[axisIndex] = -normal[axisIndex];

                        if (localToPrimaryBrush)
                        {
                            // Rotate the position and normal from the primary brush's local space back to the brush's local space
                            position = brush.transform.InverseTransformDirection(primaryTargetBrush.transform.TransformDirection(position));
                            normal = brush.transform.InverseTransformDirection(primaryTargetBrush.transform.TransformDirection(normal));
                        }
                        else
                        {
                            // Rotate the position and normal from the global axis orientation back to the brush's original local space
                            position = brush.transform.InverseTransformDirection(position);
                            normal = brush.transform.InverseTransformDirection(normal);
                        }

                        // Set the vertex position and normal to their new values
                        vertex.Position = position;
                        vertex.Normal = normal;
                    }
                    // Because a flip has occurred we need to reverse the winding order
                    Array.Reverse(polygons[i].Vertices);
                    // Polygon plane has probably changed so it should now be recalculated
                    polygons[i].CalculatePlane();
                }

                if (targetBrushes.Length > 0) // Only need to move brushes if there's more than one
                {
                    // Calculate the difference between the brush position and the center of flipping
                    Vector3 deltaFromCenter = brush.transform.position - flipCenter;
                    if (localToPrimaryBrush)
                    {
                        // Rotate the delta so that it's in the primary brush's local space
                        deltaFromCenter = primaryTargetBrush.transform.InverseTransformDirection(deltaFromCenter);
                    }

                    // Negate the delta, so that the brush position will be flipped to the other side
                    deltaFromCenter[axisIndex] = -deltaFromCenter[axisIndex];

                    if (localToPrimaryBrush)
                    {
                        // Rotate the delta back to its original space
                        deltaFromCenter = primaryTargetBrush.transform.TransformDirection(deltaFromCenter);
                    }
                    // Set the brush's new position
                    brush.transform.position = flipCenter + deltaFromCenter;
                }
                // Notify the brush that it has changed
                brush.Invalidate(true);
            }
        }

        public static void SplitIntersecting(PrimitiveBrush[] brushes)
        {
            List<Brush> intersections = new List<Brush>();

            foreach (PrimitiveBrush brush in brushes)
            {
                intersections.AddRange(brush.BrushCache.IntersectingVisualBrushes);
            }

            foreach (PrimitiveBrush brush in brushes)
            {
                List<PrimitiveBrush> newBrushes = new List<PrimitiveBrush>();

                foreach (Brush intersectingBrush in intersections)
                {
                    PrimitiveBrush brushToClip = (PrimitiveBrush)intersectingBrush;

                    Polygon[] polygons = brush.GetPolygons();

                    // A brush may have several polygons that share a plane, find all the distinct polygon planes
                    List<Plane> distinctPlanes = new List<Plane>();

                    for (int polygonIndex = 0; polygonIndex < polygons.Length; polygonIndex++)
                    {
                        Polygon polygon = polygons[polygonIndex];
                        Vertex vertex1, vertex2, vertex3;
                        SurfaceUtility.GetPrimaryPolygonDescribers(polygon, out vertex1, out vertex2, out vertex3);

                        Vector3 position1 = vertex1.Position;
                        Vector3 position2 = vertex2.Position;
                        Vector3 position3 = vertex3.Position;

                        // Transform from local to brush to local to intersectingBrush
                        position1 = intersectingBrush.transform.InverseTransformPoint(brush.transform.TransformPoint(position1));
                        position2 = intersectingBrush.transform.InverseTransformPoint(brush.transform.TransformPoint(position2));
                        position3 = intersectingBrush.transform.InverseTransformPoint(brush.transform.TransformPoint(position3));

                        // Calculate plane in intersectingBrush's local space
                        Plane polygonPlane = new Plane(position1, position2, position3);

                        bool found = false;
                        // See if it already exists
                        for (int i = 0; i < distinctPlanes.Count; i++)
                        {
                            if (MathHelper.PlaneEqualsLooser(distinctPlanes[i], polygonPlane))
                            {
                                found = true;
                                break;
                            }
                        }

                        // Not added to an existing group, so add new
                        if (!found)
                        {
                            // Add a new group for the polygon
                            distinctPlanes.Add(polygonPlane);
                        }
                    }

                    foreach (Plane clipPlane in distinctPlanes)
                    {
#if UNITY_EDITOR
                        UnityEditor.Undo.RecordObject(brushToClip, "Split Intersecting Brushes");
                        UnityEditor.Undo.RecordObject(brushToClip.transform, "Split Intersecting Brushes");
#endif

                        GameObject newObject = ClipUtility.ApplyClipPlane(brushToClip, clipPlane, true, false);

                        if (newObject != null)
                        {
#if UNITY_EDITOR
                            UnityEditor.Undo.RegisterCreatedObjectUndo(newObject, "Split Intersecting Brushes");
#endif
                            newBrushes.Add(newObject.GetComponent<PrimitiveBrush>());
                        }
                    }

                    brushToClip.ResetPivot();                    
                }

                foreach (PrimitiveBrush newBrush in newBrushes)
                {
                    newBrush.ResetPivot();
                }

                intersections.AddRange(newBrushes.ConvertAll<Brush>(item => (Brush)item));
            }
        }
    }
}
#endif