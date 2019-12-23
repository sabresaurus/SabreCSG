#if UNITY_EDITOR

using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;

namespace Sabresaurus.SabreCSG
{
    public class PaintEditor : Tool
    {
        /// <summary>
        /// Constants for the color palette. These colors were recreated from Adobe Photoshop CC 2018 swatches.
        /// </summary>
        private static readonly Color[] colorPalette = new Color[]
        {
            new Color(1.000f, 0.000f, 0.000f),
            new Color(1.000f, 1.000f, 0.000f),
            new Color(0.000f, 1.000f, 0.000f),
            new Color(0.000f, 1.000f, 1.000f),
            new Color(0.000f, 0.000f, 1.000f),
            new Color(1.000f, 0.000f, 1.000f),
            new Color(1.000f, 1.000f, 1.000f),
            new Color(0.929f, 0.929f, 0.929f),
            new Color(0.898f, 0.898f, 0.898f),
            new Color(0.863f, 0.863f, 0.863f),
            new Color(0.824f, 0.824f, 0.824f),
            new Color(0.788f, 0.788f, 0.788f),
            new Color(0.749f, 0.749f, 0.749f),
            new Color(0.710f, 0.710f, 0.710f),
            new Color(0.667f, 0.667f, 0.667f),
            new Color(0.627f, 0.627f, 0.627f),
            new Color(0.890f, 0.024f, 0.075f),
            new Color(1.000f, 0.929f, 0.000f),
            new Color(0.000f, 0.588f, 0.251f),
            new Color(0.000f, 0.624f, 0.890f),
            new Color(0.192f, 0.153f, 0.514f),
            new Color(0.902f, 0.000f, 0.494f),
            new Color(0.584f, 0.584f, 0.584f),
            new Color(0.537f, 0.537f, 0.537f),
            new Color(0.486f, 0.486f, 0.486f),
            new Color(0.435f, 0.435f, 0.435f),
            new Color(0.384f, 0.384f, 0.384f),
            new Color(0.325f, 0.325f, 0.325f),
            new Color(0.263f, 0.263f, 0.263f),
            new Color(0.196f, 0.196f, 0.196f),
            new Color(0.106f, 0.106f, 0.106f),
            new Color(0.000f, 0.000f, 0.000f),
            new Color(0.953f, 0.600f, 0.482f),
            new Color(0.969f, 0.698f, 0.522f),
            new Color(0.988f, 0.796f, 0.557f),
            new Color(1.000f, 0.961f, 0.612f),
            new Color(0.812f, 0.878f, 0.608f),
            new Color(0.686f, 0.827f, 0.608f),
            new Color(0.561f, 0.784f, 0.604f),
            new Color(0.537f, 0.800f, 0.792f),
            new Color(0.514f, 0.816f, 0.961f),
            new Color(0.549f, 0.678f, 0.863f),
            new Color(0.561f, 0.600f, 0.804f),
            new Color(0.573f, 0.522f, 0.745f),
            new Color(0.671f, 0.549f, 0.753f),
            new Color(0.776f, 0.576f, 0.761f),
            new Color(0.949f, 0.624f, 0.773f),
            new Color(0.953f, 0.612f, 0.635f),
            new Color(0.925f, 0.392f, 0.275f),
            new Color(0.949f, 0.553f, 0.306f),
            new Color(0.976f, 0.698f, 0.337f),
            new Color(1.000f, 0.945f, 0.369f),
            new Color(0.718f, 0.820f, 0.404f),
            new Color(0.525f, 0.753f, 0.416f),
            new Color(0.247f, 0.686f, 0.424f),
            new Color(0.137f, 0.706f, 0.694f),
            new Color(0.000f, 0.725f, 0.933f),
            new Color(0.282f, 0.549f, 0.796f),
            new Color(0.349f, 0.447f, 0.714f),
            new Color(0.392f, 0.333f, 0.627f),
            new Color(0.549f, 0.353f, 0.631f),
            new Color(0.686f, 0.373f, 0.635f),
            new Color(0.925f, 0.412f, 0.639f),
            new Color(0.925f, 0.404f, 0.478f),
            new Color(0.890f, 0.024f, 0.075f),
            new Color(0.918f, 0.357f, 0.047f),
            new Color(0.953f, 0.573f, 0.000f),
            new Color(1.000f, 0.929f, 0.000f),
            new Color(0.588f, 0.757f, 0.118f),
            new Color(0.231f, 0.667f, 0.208f),
            new Color(0.000f, 0.588f, 0.251f),
            new Color(0.000f, 0.604f, 0.576f),
            new Color(0.000f, 0.624f, 0.890f),
            new Color(0.000f, 0.412f, 0.706f),
            new Color(0.000f, 0.286f, 0.604f),
            new Color(0.192f, 0.153f, 0.514f),
            new Color(0.400f, 0.141f, 0.514f),
            new Color(0.588f, 0.106f, 0.506f),
            new Color(0.902f, 0.000f, 0.494f),
            new Color(0.898f, 0.000f, 0.318f),
            new Color(0.612f, 0.063f, 0.024f),
            new Color(0.631f, 0.259f, 0.008f),
            new Color(0.655f, 0.408f, 0.000f),
            new Color(0.702f, 0.643f, 0.000f),
            new Color(0.408f, 0.533f, 0.086f),
            new Color(0.161f, 0.475f, 0.145f),
            new Color(0.000f, 0.420f, 0.176f),
            new Color(0.000f, 0.427f, 0.408f),
            new Color(0.000f, 0.435f, 0.620f),
            new Color(0.000f, 0.290f, 0.498f),
            new Color(0.012f, 0.196f, 0.427f),
            new Color(0.141f, 0.098f, 0.365f),
            new Color(0.290f, 0.082f, 0.361f),
            new Color(0.416f, 0.059f, 0.353f),
            new Color(0.627f, 0.000f, 0.341f),
            new Color(0.624f, 0.027f, 0.216f),
            new Color(0.467f, 0.059f, 0.000f),
            new Color(0.482f, 0.196f, 0.000f),
            new Color(0.498f, 0.310f, 0.000f),
            new Color(0.525f, 0.490f, 0.000f),
            new Color(0.306f, 0.408f, 0.059f),
            new Color(0.110f, 0.365f, 0.106f),
            new Color(0.000f, 0.325f, 0.129f),
            new Color(0.000f, 0.325f, 0.310f),
            new Color(0.000f, 0.333f, 0.471f),
            new Color(0.000f, 0.216f, 0.380f),
            new Color(0.016f, 0.141f, 0.325f),
            new Color(0.114f, 0.055f, 0.275f),
            new Color(0.224f, 0.043f, 0.275f),
            new Color(0.322f, 0.024f, 0.267f),
            new Color(0.482f, 0.000f, 0.255f),
            new Color(0.478f, 0.024f, 0.153f),
            new Color(0.808f, 0.737f, 0.647f),
            new Color(0.643f, 0.569f, 0.494f),
            new Color(0.494f, 0.420f, 0.365f),
            new Color(0.361f, 0.294f, 0.259f),
            new Color(0.239f, 0.196f, 0.176f),
            new Color(0.796f, 0.651f, 0.459f),
            new Color(0.686f, 0.525f, 0.333f),
            new Color(0.588f, 0.416f, 0.224f),
            new Color(0.498f, 0.318f, 0.133f),
            new Color(0.412f, 0.235f, 0.067f)
        };

        /// <summary>
        /// Constants for the red palette. These colors are a simple lerp from 1 to 0 over 32 steps.
        /// </summary>
        private static readonly Color[] redPalette = new Color[]
        {
            new Color(1.000f, 0.000f, 0.000f),
            new Color(0.968f, 0.000f, 0.000f),
            new Color(0.935f, 0.000f, 0.000f),
            new Color(0.903f, 0.000f, 0.000f),
            new Color(0.871f, 0.000f, 0.000f),
            new Color(0.839f, 0.000f, 0.000f),
            new Color(0.806f, 0.000f, 0.000f),
            new Color(0.774f, 0.000f, 0.000f),
            new Color(0.742f, 0.000f, 0.000f),
            new Color(0.710f, 0.000f, 0.000f),
            new Color(0.677f, 0.000f, 0.000f),
            new Color(0.645f, 0.000f, 0.000f),
            new Color(0.613f, 0.000f, 0.000f),
            new Color(0.581f, 0.000f, 0.000f),
            new Color(0.548f, 0.000f, 0.000f),
            new Color(0.516f, 0.000f, 0.000f),
            new Color(0.484f, 0.000f, 0.000f),
            new Color(0.452f, 0.000f, 0.000f),
            new Color(0.419f, 0.000f, 0.000f),
            new Color(0.387f, 0.000f, 0.000f),
            new Color(0.355f, 0.000f, 0.000f),
            new Color(0.323f, 0.000f, 0.000f),
            new Color(0.290f, 0.000f, 0.000f),
            new Color(0.258f, 0.000f, 0.000f),
            new Color(0.226f, 0.000f, 0.000f),
            new Color(0.194f, 0.000f, 0.000f),
            new Color(0.161f, 0.000f, 0.000f),
            new Color(0.129f, 0.000f, 0.000f),
            new Color(0.097f, 0.000f, 0.000f),
            new Color(0.065f, 0.000f, 0.000f),
            new Color(0.032f, 0.000f, 0.000f),
            new Color(0.000f, 0.000f, 0.000f)
        };

        /// <summary>
        /// Constants for the green palette. These colors are a simple lerp from 1 to 0 over 32 steps.
        /// </summary>
        private static readonly Color[] greenPalette = new Color[]
        {
            new Color(0.000f, 1.000f, 0.000f),
            new Color(0.000f, 0.968f, 0.000f),
            new Color(0.000f, 0.935f, 0.000f),
            new Color(0.000f, 0.903f, 0.000f),
            new Color(0.000f, 0.871f, 0.000f),
            new Color(0.000f, 0.839f, 0.000f),
            new Color(0.000f, 0.806f, 0.000f),
            new Color(0.000f, 0.774f, 0.000f),
            new Color(0.000f, 0.742f, 0.000f),
            new Color(0.000f, 0.710f, 0.000f),
            new Color(0.000f, 0.677f, 0.000f),
            new Color(0.000f, 0.645f, 0.000f),
            new Color(0.000f, 0.613f, 0.000f),
            new Color(0.000f, 0.581f, 0.000f),
            new Color(0.000f, 0.548f, 0.000f),
            new Color(0.000f, 0.516f, 0.000f),
            new Color(0.000f, 0.484f, 0.000f),
            new Color(0.000f, 0.452f, 0.000f),
            new Color(0.000f, 0.419f, 0.000f),
            new Color(0.000f, 0.387f, 0.000f),
            new Color(0.000f, 0.355f, 0.000f),
            new Color(0.000f, 0.323f, 0.000f),
            new Color(0.000f, 0.290f, 0.000f),
            new Color(0.000f, 0.258f, 0.000f),
            new Color(0.000f, 0.226f, 0.000f),
            new Color(0.000f, 0.194f, 0.000f),
            new Color(0.000f, 0.161f, 0.000f),
            new Color(0.000f, 0.129f, 0.000f),
            new Color(0.000f, 0.097f, 0.000f),
            new Color(0.000f, 0.065f, 0.000f),
            new Color(0.000f, 0.032f, 0.000f),
            new Color(0.000f, 0.000f, 0.000f)
        };

        /// <summary>
        /// Constants for the blue palette. These colors are a simple lerp from 1 to 0 over 32 steps.
        /// </summary>
        private static readonly Color[] bluePalette = new Color[]
        {
            new Color(0.000f, 0.000f, 1.000f),
            new Color(0.000f, 0.000f, 0.968f),
            new Color(0.000f, 0.000f, 0.935f),
            new Color(0.000f, 0.000f, 0.903f),
            new Color(0.000f, 0.000f, 0.871f),
            new Color(0.000f, 0.000f, 0.839f),
            new Color(0.000f, 0.000f, 0.806f),
            new Color(0.000f, 0.000f, 0.774f),
            new Color(0.000f, 0.000f, 0.742f),
            new Color(0.000f, 0.000f, 0.710f),
            new Color(0.000f, 0.000f, 0.677f),
            new Color(0.000f, 0.000f, 0.645f),
            new Color(0.000f, 0.000f, 0.613f),
            new Color(0.000f, 0.000f, 0.581f),
            new Color(0.000f, 0.000f, 0.548f),
            new Color(0.000f, 0.000f, 0.516f),
            new Color(0.000f, 0.000f, 0.484f),
            new Color(0.000f, 0.000f, 0.452f),
            new Color(0.000f, 0.000f, 0.419f),
            new Color(0.000f, 0.000f, 0.387f),
            new Color(0.000f, 0.000f, 0.355f),
            new Color(0.000f, 0.000f, 0.323f),
            new Color(0.000f, 0.000f, 0.290f),
            new Color(0.000f, 0.000f, 0.258f),
            new Color(0.000f, 0.000f, 0.226f),
            new Color(0.000f, 0.000f, 0.194f),
            new Color(0.000f, 0.000f, 0.161f),
            new Color(0.000f, 0.000f, 0.129f),
            new Color(0.000f, 0.000f, 0.097f),
            new Color(0.000f, 0.000f, 0.065f),
            new Color(0.000f, 0.000f, 0.032f),
            new Color(0.000f, 0.000f, 0.000f)
        };

        /// <summary>
        /// Constants for the alpha palette. These colors are a simple lerp from 1 to 0 over 32 steps.
        /// </summary>
        private static readonly Color[] alphaPalette = new Color[]
        {
            new Color(1.000f, 1.000f, 1.000f),
            new Color(0.968f, 0.968f, 0.968f),
            new Color(0.935f, 0.935f, 0.935f),
            new Color(0.903f, 0.903f, 0.903f),
            new Color(0.871f, 0.871f, 0.871f),
            new Color(0.839f, 0.839f, 0.839f),
            new Color(0.806f, 0.806f, 0.806f),
            new Color(0.774f, 0.774f, 0.774f),
            new Color(0.742f, 0.742f, 0.742f),
            new Color(0.710f, 0.710f, 0.710f),
            new Color(0.677f, 0.677f, 0.677f),
            new Color(0.645f, 0.645f, 0.645f),
            new Color(0.613f, 0.613f, 0.613f),
            new Color(0.581f, 0.581f, 0.581f),
            new Color(0.548f, 0.548f, 0.548f),
            new Color(0.516f, 0.516f, 0.516f),
            new Color(0.484f, 0.484f, 0.484f),
            new Color(0.452f, 0.452f, 0.452f),
            new Color(0.419f, 0.419f, 0.419f),
            new Color(0.387f, 0.387f, 0.387f),
            new Color(0.355f, 0.355f, 0.355f),
            new Color(0.323f, 0.323f, 0.323f),
            new Color(0.290f, 0.290f, 0.290f),
            new Color(0.258f, 0.258f, 0.258f),
            new Color(0.226f, 0.226f, 0.226f),
            new Color(0.194f, 0.194f, 0.194f),
            new Color(0.161f, 0.161f, 0.161f),
            new Color(0.129f, 0.129f, 0.129f),
            new Color(0.097f, 0.097f, 0.097f),
            new Color(0.065f, 0.065f, 0.065f),
            new Color(0.032f, 0.032f, 0.032f),
            new Color(0.000f, 0.000f, 0.000f)
        };

        /// <summary>
        /// The blending modes for the brush. These modes were inspired by Adobe Photoshop CC 2018 blending modes.
        /// </summary>
        private enum BlendingMode
        {
            Replace,
            Darken,
            Multiply,
            ColorBurn,
            LinearBurn,
            Lighten,
            Screen,
            ColorDodge,
            LinearDodge
        }

        /// <summary>
        /// The color drawing modes.
        /// </summary>
        private enum DrawingMode
        {
            /// <summary>
            /// The color mode affects the Red, Green and Blue channels.
            /// </summary>
            Color,

            /// <summary>
            /// The red mode affects only the Red channel.
            /// </summary>
            R,

            /// <summary>
            /// The green mode affects only the Green channel.
            /// </summary>
            G,

            /// <summary>
            /// The blue mode affects only the Blue channel.
            /// </summary>
            B,

            /// <summary>
            /// The alpha mode affects only the Alpha channel.
            /// </summary>
            Alpha
        };

        /// <summary>
        /// The current color drawing mode.
        /// </summary>
        private DrawingMode drawingMode = DrawingMode.Color;

        /// <summary>
        /// The world position of the point the mouse is hovering over.
        /// </summary>
        private Vector3 mouseHoverPoint;

        /// <summary>
        /// The toolbar rectangle for this tool's user interface.
        /// </summary>
        private readonly Rect toolbarRect = new Rect(6, 40, 203, 0);

        /// <summary>
        /// An additional height used when switching between color and alpha drawing modes.
        /// </summary>
        private int toolbarHeight = 0;

        /// <summary>
        /// The brush drawing radius.
        /// </summary>
        private float brushRadius = 1f;

        /// <summary>
        /// The color brush's current color.
        /// </summary>
        private Color colorBrushColor = Color.white;

        /// <summary>
        /// The color brush's current strength.
        /// </summary>
        private float colorBrushStrength = 0.06f;

        /// <summary>
        /// The color brush's blending mode.
        /// </summary>
        private BlendingMode colorBrushBlendingMode = BlendingMode.Replace;

        /// <summary>
        /// The red brush's current strength.
        /// </summary>
        private float redBrushStrength = 0.3f;

        /// <summary>
        /// The red brush's current color.
        /// </summary>
        private float redBrushColor = 1f;

        /// <summary>
        /// The green brush's current strength.
        /// </summary>
        private float greenBrushStrength = 0.3f;

        /// <summary>
        /// The green brush's current color.
        /// </summary>
        private float greenBrushColor = 1f;

        /// <summary>
        /// The blue brush's current strength.
        /// </summary>
        private float blueBrushStrength = 0.3f;

        /// <summary>
        /// The blue brush's current color.
        /// </summary>
        private float blueBrushColor = 1f;

        /// <summary>
        /// The alpha brush's current strength.
        /// </summary>
        private float alphaBrushStrength = 0.3f;

        /// <summary>
        /// The alpha brush's current color.
        /// </summary>
        private float alphaBrushColor = 1f;

        /// <summary>
        /// Whether to restrict the drawing to the initial face that was clicked upon.
        /// </summary>
        private bool restrictToFace = false;

        /// <summary>
        /// The initial polygon that the mouse down event occured on.
        /// </summary>
        private Polygon initialPolygon = null; // The polygon that the mouse down event occurred on

        /// <summary>
        /// The active polygon that the mouse is currently hovering over.
        /// </summary>
        private Polygon activePolygon = null;

        /// <summary>
        /// The brushes that have been edited by the user.
        /// </summary>
        private List<Brush> editedBrushes = new List<Brush>();

        /// <summary>
        /// Gets the actual toolbar rectangle, this changes depending on the drawing modes.
        /// </summary>
        /// <value>The actual toolbar rectangle.</value>
        private Rect actualToolbarRect
        {
            get
            {
                return new Rect(toolbarRect.x, toolbarRect.y, toolbarRect.width, toolbarRect.height + toolbarHeight);
            }
        }

        /// <summary>
        /// Called when the tool gets reset.
        /// </summary>
        public override void ResetTool()
        {
        }

        /// <summary>
        /// Called whenever the editor updates and there was an event on the scene GUI.
        /// </summary>
        /// <param name="sceneView">The scene view.</param>
        /// <param name="e">The event information.</param>
        public override void OnSceneGUI(SceneView sceneView, Event e)
        {
            // allow the base logic to calculate first.
            base.OnSceneGUI(sceneView, e);

            // we adjust the toolbar size depending on the drawing mode.
            switch (drawingMode)
            {
                case DrawingMode.Color:
#if UNITY_2019_3_OR_NEWER
                    toolbarHeight = 230;
#else
                    toolbarHeight = 217;
#endif
                    break;

                case DrawingMode.R:
                case DrawingMode.G:
                case DrawingMode.B:
                case DrawingMode.Alpha:
#if UNITY_2019_3_OR_NEWER
                    toolbarHeight = 138;
#else
                    toolbarHeight = 126;
#endif
                    break;
            }

            if (e.button == 0
                && !EditorHelper.IsMousePositionInInvalidRects(e.mousePosition)
                && !EditorHelper.IsMousePositionInIMGUIRect(e.mousePosition, actualToolbarRect)
                && !CameraPanInProgress)
            {
                if (e.type == EventType.MouseDown)
                {
                    OnMouseDown(sceneView, e);
                }
                else if (e.type == EventType.MouseDrag)
                {
                    OnMouseDrag(sceneView, e);
                }
                else if (e.type == EventType.MouseMove)
                {
                    OnMouseMove(sceneView, e);
                }
                // If you mouse up on a different scene view to the one you started on it's surpressed as Ignore, so
                // make sure to check the real type
                else if (e.type == EventType.MouseUp || e.rawType == EventType.MouseUp)
                {
                    OnMouseUp(sceneView, e);
                }
            }

            if (e.type == EventType.KeyDown || e.type == EventType.KeyUp)
            {
                OnKeyAction(sceneView, e);
            }

            if (e.type == EventType.Layout || e.type == EventType.Repaint)
            {
                OnRepaint(sceneView, e);
            }
        }

        private void CalculateHitBuiltPolygon(Vector2 currentPosition)
        {
            activePolygon = null;

            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

            Polygon polygon = csgModel.RaycastBuiltPolygons(ray);

            if (polygon != null)
            {
                float rayDistance;

                if (polygon.Plane.Raycast(ray, out rayDistance))
                {
                    mouseHoverPoint = ray.GetPoint(rayDistance);

                    PolygonRaycastHit hit = new PolygonRaycastHit()
                    {
                        Point = mouseHoverPoint,
                        Normal = polygon.Plane.normal,
                        Distance = rayDistance,
                        GameObject = null,
                        Polygon = polygon,
                    };

                    activePolygon = hit.Polygon;
                    mouseHoverPoint = hit.Point;
                }
            }
        }

        private void OnMouseDown(SceneView sceneView, Event e)
        {
            CalculateHitBuiltPolygon(e.mousePosition);

            initialPolygon = activePolygon;

            SceneView.RepaintAll();

            // when left-clicked immediately paint instead of waiting until the user moves.
            if (e.button == 0)
                OnMouseDrag(sceneView, e);
        }

        private bool Coplanar(Transform brushTransform, Polygon localBrushPolygon, Polygon worldPolygon)
        {
            Plane worldPolygonPlane = worldPolygon.Plane;

            Plane localBrushPlane = localBrushPolygon.Plane;
            Vector3 pointOnPlane = -localBrushPlane.normal * localBrushPlane.distance;
            Vector3 pointOnPlaneWorld = brushTransform.TransformPoint(pointOnPlane);
            Plane worldBrushPlane = new Plane(brushTransform.TransformDirection(localBrushPlane.normal), pointOnPlaneWorld);

            if (MathHelper.PlaneEqualsLooser(worldPolygonPlane, worldBrushPlane))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void OnMouseDrag(SceneView sceneView, Event e)
        {
            CalculateHitBuiltPolygon(e.mousePosition);

            if (activePolygon != null)
            {
                if (initialPolygon == null)
                {
                    // They may have started the drag over empty space, so fill in the initial polygon with the first valid one
                    initialPolygon = activePolygon;
                }
                List<Brush> brushes = csgModel.GetBrushes();
                foreach (Brush brush in brushes)
                {
                    bool anyChanged = false;
                    foreach (Polygon brushPolygon in brush.GetPolygons())
                    {
                        if (restrictToFace == false || Coplanar(brush.transform, brushPolygon, initialPolygon))
                        {
                            anyChanged |= ChangePolygonColor(brush, brushPolygon);
                        }
                    }

                    if (anyChanged)
                    {
                        if (!editedBrushes.Contains(brush))
                            editedBrushes.Add(brush);
                    }
                }
            }
            SceneView.RepaintAll();
        }

        private bool ChangePolygonColor(Brush brush, Polygon polygon)
        {
            bool anyChanged = false;

            Vector3 hoverPointLocal = brush.transform.InverseTransformPoint(mouseHoverPoint);

            Undo.RecordObject(brush, "Paint");
            csgModel.UndoRecordContext("Paint");

            for (int j = 0; j < polygon.Vertices.Length; j++)
            {
                float squareDistance = (polygon.Vertices[j].Position - hoverPointLocal).sqrMagnitude;

                if (squareDistance <= (brushRadius * brushRadius))
                {
                    float distance = Mathf.Sqrt(squareDistance);
                    polygon.Vertices[j].Color = PaintColor(polygon.Vertices[j].Color, distance / brushRadius);
                    anyChanged = true;
                }
            }

            PolygonEntry entry = csgModel.GetVisualPolygonEntry(polygon.UniqueIndex);
            if (entry != null)
            {
                if (entry.BuiltMesh != null)
                {
                    Undo.RecordObject(entry.BuiltMesh, "Change Vertex Color");

                    Vector3[] meshVertices = entry.BuiltMesh.vertices;
                    Color32[] meshColors = entry.BuiltMesh.colors32;
                    Color32[] colors = entry.Colors;

                    for (int vertexIndex = 0; vertexIndex < entry.Positions.Length; vertexIndex++)
                    {
                        float squareDistance = (meshVertices[entry.BuiltVertexOffset + vertexIndex] - mouseHoverPoint).sqrMagnitude;

                        if (squareDistance <= (brushRadius * brushRadius))
                        {
                            float distance = Mathf.Sqrt(squareDistance);
                            colors[vertexIndex] = PaintColor(colors[vertexIndex], distance / brushRadius);
                            meshColors[entry.BuiltVertexOffset + vertexIndex] = PaintColor(meshColors[entry.BuiltVertexOffset + vertexIndex], distance / brushRadius);
                            anyChanged = true;
                        }
                    }

                    if (anyChanged)
                    {
                        entry.Colors = colors;
                        entry.BuiltMesh.colors32 = meshColors;

                        EditorHelper.SetDirty(entry.BuiltMesh);
                    }
                }
            }

            return anyChanged;
        }

        private Color PaintColor(Color sourceColor, float paintAmount)
        {
            switch (drawingMode)
            {
                case DrawingMode.Color:
                    switch (colorBrushBlendingMode)
                    {
                        case BlendingMode.Replace:
                            {
                                Color color = Color.Lerp(sourceColor, colorBrushColor, colorBrushStrength * paintAmount).Clamp01();
                                color.a = sourceColor.a;
                                return color;
                            }
                        case BlendingMode.Darken:
                            {
                                Color color = Color.Lerp(sourceColor, sourceColor.Darken(colorBrushColor), colorBrushStrength * paintAmount).Clamp01();
                                color.a = sourceColor.a;
                                return color;
                            }
                        case BlendingMode.Multiply:
                            {
                                Color color = Color.Lerp(sourceColor, sourceColor.Multiply(colorBrushColor), colorBrushStrength * paintAmount).Clamp01();
                                color.a = sourceColor.a;
                                return color;
                            }
                        case BlendingMode.ColorBurn:
                            {
                                Color color = Color.Lerp(sourceColor, sourceColor.ColorBurn(colorBrushColor), colorBrushStrength * paintAmount).Clamp01();
                                color.a = sourceColor.a;
                                return color;
                            }
                        case BlendingMode.LinearBurn:
                            {
                                Color color = Color.Lerp(sourceColor, sourceColor.LinearBurn(colorBrushColor), colorBrushStrength * paintAmount).Clamp01();
                                color.a = sourceColor.a;
                                return color;
                            }
                        case BlendingMode.Lighten:
                            {
                                Color color = Color.Lerp(sourceColor, sourceColor.Lighten(colorBrushColor), colorBrushStrength * paintAmount).Clamp01();
                                color.a = sourceColor.a;
                                return color;
                            }
                        case BlendingMode.Screen:
                            {
                                Color color = Color.Lerp(sourceColor, sourceColor.Screen(colorBrushColor), colorBrushStrength * paintAmount).Clamp01();
                                color.a = sourceColor.a;
                                return color;
                            }
                        case BlendingMode.ColorDodge:
                            {
                                Color color = Color.Lerp(sourceColor, sourceColor.ColorDodge(colorBrushColor), colorBrushStrength * paintAmount).Clamp01();
                                color.a = sourceColor.a;
                                return color;
                            }
                        case BlendingMode.LinearDodge:
                            {
                                Color color = Color.Lerp(sourceColor, sourceColor.LinearDodge(colorBrushColor), colorBrushStrength * paintAmount).Clamp01();
                                color.a = sourceColor.a;
                                return color;
                            }
                    }

                    return sourceColor;

                case DrawingMode.R:
                    {
                        Color newColor = sourceColor;
                        newColor.r = Mathf.Lerp(sourceColor.r, redBrushColor, redBrushStrength * paintAmount);
                        return newColor;
                    }

                case DrawingMode.G:
                    {
                        Color newColor = sourceColor;
                        newColor.g = Mathf.Lerp(sourceColor.g, greenBrushColor, greenBrushStrength * paintAmount);
                        return newColor;
                    }

                case DrawingMode.B:
                    {
                        Color newColor = sourceColor;
                        newColor.b = Mathf.Lerp(sourceColor.b, blueBrushColor, blueBrushStrength * paintAmount);
                        return newColor;
                    }

                case DrawingMode.Alpha:
                    {
                        Color newColor = sourceColor;
                        newColor.a = Mathf.Lerp(sourceColor.a, alphaBrushColor, alphaBrushStrength * paintAmount);
                        return newColor;
                    }
            }
            return sourceColor;
        }

        private void OnMouseMove(SceneView sceneView, Event e)
        {
            CalculateHitBuiltPolygon(e.mousePosition);

            SceneView.RepaintAll();
        }

        private void OnMouseUp(SceneView sceneView, Event e)
        {
            SceneView.RepaintAll();

            // Deferred expensive operation
            foreach (var brush in editedBrushes)
            {
                brush.RecachePolygons(false);
            }

            editedBrushes.Clear();
        }

        private void OnKeyAction(SceneView sceneView, Event e)
        {
            //            if (KeyMappings.EventsMatch(e, Event.KeyboardEvent(KeyMappings.Instance.CancelCurrentOperation)))
            //            {
            //                if (e.type == EventType.KeyDown)
            //                {
            //                    if(drawMode != DrawMode.None || hitPoints.Count > 0)
            //                    {
            //                        // Drawing is in progress so cancel it
            //                        ResetTool();
            //                    }
            //                    else
            //                    {
            //                        // No draw in progress, so user wants to cancel out of draw mode
            //                        csgModel.ExitOverrideMode();
            //                    }
            //                }
            //                e.Use();
            //            }
        }

        private void OnRepaint(SceneView sceneView, Event e)
        {
            OnRepaintGUI(sceneView, e);

            if (activePolygon != null && activePolygon.Vertices.Length >= 3)
            {
                Camera sceneViewCamera = sceneView.camera;

                SabreCSGResources.GetVertexMaterial().SetPass(0);
                GL.PushMatrix();
                GL.LoadPixelMatrix();

                GL.Begin(GL.QUADS);

                GL.Color(Color.white);

                Vector3 target = sceneViewCamera.WorldToScreenPoint(mouseHoverPoint);

                if (target.z > 0)
                {
                    // Make it pixel perfect
                    target = MathHelper.RoundVector3(target);
                    SabreGraphics.DrawBillboardQuad(target, 8, 8);
                }

                GL.End();
                GL.PopMatrix();

                Handles.DrawWireArc(mouseHoverPoint, activePolygon.Plane.normal, activePolygon.GetTangent(), 360f, brushRadius);
            }
        }

        private void OnRepaintGUI(SceneView sceneView, Event e)
        {
            // Draw UI specific to this editor
            GUIStyle toolbar = new GUIStyle(EditorStyles.toolbar);

            // Set the background tint
            if (EditorGUIUtility.isProSkin)
            {
                toolbar.normal.background = SabreCSGResources.HalfBlackTexture;
            }
            else
            {
                toolbar.normal.background = SabreCSGResources.HalfWhiteTexture;
            }
            // Set the style height to match the rectangle (so it stretches instead of tiling)
            toolbar.fixedHeight = toolbarRect.height + toolbarHeight;
            // Draw the actual GUI via a Window
            GUILayout.Window(140010, actualToolbarRect, OnToolbarGUI, "", toolbar);
        }

        private void OnToolbarGUI(int windowID)
        {
            EditorGUILayout.Space();

            restrictToFace = SabreGUILayout.Toggle(restrictToFace, "Restrict To Face");
            drawingMode = SabreGUILayout.DrawEnumGrid(drawingMode);

            // radius slider
            EditorGUILayout.BeginHorizontal();
            GUI.color = Color.black;
            GUILayout.Label("Radius");
            GUI.color = Color.white;
            brushRadius = EditorGUILayout.Slider("", brushRadius, 0.1f, 5f, GUILayout.MaxWidth(128));
            EditorGUILayout.EndHorizontal();

            // opacity slider
            EditorGUILayout.BeginHorizontal();
            GUI.color = Color.black;
            GUILayout.Label("Opacity");
            GUI.color = Color.white;

            switch (drawingMode)
            {
                case DrawingMode.Color:
                    colorBrushStrength = EditorGUILayout.Slider("", colorBrushStrength, 0f, 1f, GUILayout.MaxWidth(128));
                    break;

                case DrawingMode.R:
                    redBrushStrength = EditorGUILayout.Slider("", redBrushStrength, 0f, 1f, GUILayout.MaxWidth(128));
                    break;

                case DrawingMode.G:
                    greenBrushStrength = EditorGUILayout.Slider("", greenBrushStrength, 0f, 1f, GUILayout.MaxWidth(128));
                    break;

                case DrawingMode.B:
                    blueBrushStrength = EditorGUILayout.Slider("", blueBrushStrength, 0f, 1f, GUILayout.MaxWidth(128));
                    break;

                case DrawingMode.Alpha:
                    alphaBrushStrength = EditorGUILayout.Slider("", alphaBrushStrength, 0f, 1f, GUILayout.MaxWidth(128));
                    break;
            }
            EditorGUILayout.EndHorizontal();

            switch (drawingMode)
            {
                case DrawingMode.Color:
                    // color picker field
                    colorBrushColor = SabreGUILayout.ColorField(new GUIContent("Color"), colorBrushColor, false, false, GUILayout.MaxWidth(183));

                    // color blending mode
                    EditorGUILayout.BeginHorizontal();
                    GUI.color = Color.black;
                    GUILayout.Label("Blending");
                    GUI.color = Color.white;
                    colorBrushBlendingMode = (BlendingMode)EditorGUILayout.EnumPopup(colorBrushBlendingMode, GUILayout.MaxWidth(96));
                    EditorGUILayout.EndHorizontal();
                    break;

                case DrawingMode.R:
                    // red color slider
                    EditorGUILayout.BeginHorizontal();
                    GUI.color = Color.black;
                    GUILayout.Label("Red");
                    GUI.color = Color.white;
                    redBrushColor = EditorGUILayout.Slider("", redBrushColor, 0f, 1f, GUILayout.MaxWidth(128));
                    EditorGUILayout.EndHorizontal();
                    break;

                case DrawingMode.G:
                    // green color slider
                    EditorGUILayout.BeginHorizontal();
                    GUI.color = Color.black;
                    GUILayout.Label("Green");
                    GUI.color = Color.white;
                    greenBrushColor = EditorGUILayout.Slider("", greenBrushColor, 0f, 1f, GUILayout.MaxWidth(128));
                    EditorGUILayout.EndHorizontal();
                    break;

                case DrawingMode.B:
                    // blue color slider
                    EditorGUILayout.BeginHorizontal();
                    GUI.color = Color.black;
                    GUILayout.Label("Blue");
                    GUI.color = Color.white;
                    blueBrushColor = EditorGUILayout.Slider("", blueBrushColor, 0f, 1f, GUILayout.MaxWidth(128));
                    EditorGUILayout.EndHorizontal();
                    break;

                case DrawingMode.Alpha:
                    // alpha color slider
                    EditorGUILayout.BeginHorizontal();
                    GUI.color = Color.black;
                    GUILayout.Label("Alpha");
                    GUI.color = Color.white;
                    alphaBrushColor = EditorGUILayout.Slider("", alphaBrushColor, 0f, 1f, GUILayout.MaxWidth(128));
                    EditorGUILayout.EndHorizontal();
                    break;
            }

            // draw color palette:
            DrawToolbarPalette();
        }

        /// <summary>
        /// Draws the toolbar palette.
        /// </summary>
        private void DrawToolbarPalette()
        {
            // load the appropriate palette colors:
            Color[] palette = null;
            switch (drawingMode)
            {
                case DrawingMode.Color: palette = colorPalette; break;
                case DrawingMode.R: palette = redPalette; break;
                case DrawingMode.G: palette = greenPalette; break;
                case DrawingMode.B: palette = bluePalette; break;
                case DrawingMode.Alpha: palette = alphaPalette; break;
            }

            // create constants:
            Color borderColor = new Color(0.271f, 0.271f, 0.271f);
            Texture2D whiteTexture = EditorGUIUtility.whiteTexture;
            int yoffset = (int)GUILayoutUtility.GetLastRect().yMax + 2;

            // iterate through all palette colors:
            for (int i = 0; i < palette.Length; i++)
            {
                int amount = 16;

                int x = i * 12;
                int y = (i / amount) * 12;
                x = x - ((i / amount) * (amount * 12));

                x += 5;
                y += yoffset;

                // handle the mouse down event (the user picks a color):
                if (Event.current.type == EventType.MouseDown && new Rect(x, y, 12, 12).Contains(Event.current.mousePosition))
                {
                    switch (drawingMode)
                    {
                        case DrawingMode.Color: SetSelectionColor(palette[i]); break;
                        case DrawingMode.R: redBrushColor = palette[i].r; break;
                        case DrawingMode.G: greenBrushColor = palette[i].g; break;
                        case DrawingMode.B: blueBrushColor = palette[i].b; break;
                        case DrawingMode.Alpha: alphaBrushColor = palette[i].r; break;
                    }
                }

                GUI.color = borderColor;
                GUI.DrawTexture(new Rect(x, y, 13, 13), whiteTexture, ScaleMode.StretchToFill);
                GUI.color = palette[i];
                GUI.DrawTexture(new Rect(x + 1, y + 1, 11, 11), whiteTexture, ScaleMode.StretchToFill);
            }

            GUI.color = Color.white;
        }

        public override void Deactivated()
        {
        }

        public override bool PreventBrushSelection
        {
            get
            {
                // Some special logic for clicking brushes
                return true;
            }
        }

        public override bool BrushesHandleDrawing
        {
            get
            {
                return false;
            }
        }

        private void SetSelectionColor(Color color)
        {
            this.colorBrushColor = color;
            SceneView.RepaintAll();
        }
    }
}

#endif