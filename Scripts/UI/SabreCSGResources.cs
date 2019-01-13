#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace Sabresaurus.SabreCSG
{
    public static class SabreCSGResources
    {
        #region Fields

        // Cached objects keyed by AssetDatabase path relative to SabreCSG folder
        private static Dictionary<string, Object> loadedObjects = new Dictionary<string, Object>();

        // Textures generated rather than loaded
        private static Texture2D clearTexture = null; // 0 alpha texture

        private static Texture2D halfWhiteTexture = null; // white with 0.5 alpha
        private static Texture2D halfBlackTexture = null; // black with 0.5 alpha

        // ---------------------

        private static Material selectedBrushMaterial = null;
        private static Material selectedBrushDashedMaterial = null;
        private static Material selectedBrushDashedAlphaMaterial = null;
        private static Material gizmoMaterial = null;
        private static Material vertexMaterial = null;
        private static Material circleMaterial = null;
        private static Material circleOutlineMaterial = null;
        private static Material planeMaterial = null;
        private static Material previewMaterial = null;
        private static Material excludedMaterial = null;
        private static Material greyscaleUIMaterial = null;

        #endregion Fields

        /// <summary>
        /// Loads an object from a path, or if the object is already loaded returns it
        /// </summary>
        /// <param name="sabrePath">Path local to the SabreCSG folder</param>
        /// <returns></returns>
        public static Object LoadObject(string sabrePath)
        {
            bool found = false;

            Object loadedObject = null;

            // First of all see if there's a cached record
            if (loadedObjects.ContainsKey(sabrePath))
            {
                found = true;
                loadedObject = loadedObjects[sabrePath];

                // Now make sure the cached record actually points to something
                if (loadedObject != null)
                {
                    return loadedObject;
                }
            }

            // Failed to load from cache, so load it from the Asset Database
            loadedObject = AssetDatabase.LoadMainAssetAtPath(Path.Combine(CSGModel.GetSabreCSGPath(), sabrePath));
            if (loadedObject != null)
            {
                if (found)
                {
                    // A cache record was found but empty, so set the existing record to the newly loaded object
                    loadedObjects[sabrePath] = loadedObject;
                }
                else
                {
                    // We know that it's not already in the cache, so add it to the end
                    loadedObjects.Add(sabrePath, loadedObject);
                }
            }
            return loadedObject;
        }

        /// <summary>
        /// Gets an icon for a primitive brush to be displayed on a button
        /// </summary>
        /// <returns>The icon texture.</returns>
        /// <param name="brushType">Primitive Brush type.</param>
        public static Texture2D GetButtonTexture(PrimitiveBrushType brushType)
        {
            if (brushType == PrimitiveBrushType.Prism)
                return ButtonPrismTexture;
            else if (brushType == PrimitiveBrushType.Cylinder)
                return ButtonCylinderTexture;
            else if (brushType == PrimitiveBrushType.Sphere || brushType == PrimitiveBrushType.IcoSphere)
                return ButtonSphereTexture;
            else if (brushType == PrimitiveBrushType.Cone)
                return ButtonConeTexture;
            else if (brushType == PrimitiveBrushType.Capsule)
                return ButtonCapsuleTexture;
            else
                return ButtonCubeTexture;
        }

        #region Accessors

        public static Texture2D ClearTexture
        {
            get
            {
                if (clearTexture == null)
                {
                    clearTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    for (int x = 0; x < clearTexture.width; x++)
                    {
                        for (int y = 0; y < clearTexture.height; y++)
                        {
                            clearTexture.SetPixel(x, y, Color.clear);
                        }
                    }
                    clearTexture.Apply();
                }
                return clearTexture;
            }
        }

        public static Texture2D HalfWhiteTexture
        {
            get
            {
                if (halfWhiteTexture == null)
                {
                    halfWhiteTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    for (int x = 0; x < halfWhiteTexture.width; x++)
                    {
                        for (int y = 0; y < halfWhiteTexture.height; y++)
                        {
                            halfWhiteTexture.SetPixel(x, y, new Color(1, 1, 1, 0.5f));
                        }
                    }
                    halfWhiteTexture.Apply();
                }
                return halfWhiteTexture;
            }
        }

        public static Texture2D HalfBlackTexture
        {
            get
            {
                if (halfBlackTexture == null)
                {
                    halfBlackTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    for (int x = 0; x < halfBlackTexture.width; x++)
                    {
                        for (int y = 0; y < halfBlackTexture.height; y++)
                        {
                            halfBlackTexture.SetPixel(x, y, new Color(0, 0, 0, 0.5f));
                        }
                    }
                    halfBlackTexture.Apply();
                }
                return halfBlackTexture;
            }
        }

        public static Texture2D AddIconTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/Add.png");
            }
        }

        public static Texture2D SubtractIconTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/Subtract.png");
            }
        }

        public static Texture2D VolumeIconTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/Volume.png");
            }
        }

        public static Texture2D NoCSGIconTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/NoCSG.png");
            }
        }

        public static Texture2D GroupIconTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/Group.png");
            }
        }

        public static Texture2D CollisionIconTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/Collision.png");
            }
        }

        public static Texture2D SabreCSG16IconTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/SabreCSG16.png");
            }
        }

        public static Texture2D DialogOverlayTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/DialogOverlay75.png");
            }
        }

        public static Texture2D DialogOverlayRetinaTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/DialogOverlay75@2x.png");
            }
        }

        public static Texture2D ButtonCubeTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/ButtonCube.png");
            }
        }

        public static Texture2D ButtonPrismTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/ButtonPrism.png");
            }
        }

        public static Texture2D ButtonCylinderTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/ButtonCylinder.png");
            }
        }

        public static Texture2D ButtonSphereTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/ButtonSphere.png");
            }
        }

        public static Texture2D ButtonIcoSphereTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/ButtonIcoSphere.png");
            }
        }

        public static Texture2D ButtonStairsTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/ButtonStairs.png");
            }
        }

        public static Texture2D ButtonConeTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/ButtonCone.png");
            }
        }

        public static Texture2D ButtonCapsuleTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/ButtonCapsule.png");
            }
        }

        public static Texture2D ButtonCurvedStairsTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/ButtonCurvedStairs.png");
            }
        }

        public static Texture2D ButtonShapeEditorTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/ButtonShapeEditor.png");
            }
        }

        public static Texture2D ButtonMoreTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/ButtonMore.png");
            }
        }

        public static Texture2D ButtonProjectedGridTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/ButtonProjectedGrid.png");
            }
        }

        public static Texture2D GroupHeaderTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/GroupHeader.png");
            }
        }

        public static Texture2D GroupHeaderRetinaTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/GroupHeader@2x.png");
            }
        }

        public static Texture2D GroupHeaderButtonCloseTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/GroupHeaderButtonClose.png");
            }
        }

        public static Texture2D GroupHeaderButtonHelpTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/GroupHeaderButtonHelp.png");
            }
        }

        public static Texture2D ShapeEditorCreatePolygonTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/ShapeEditorCreatePolygon.png");
            }
        }

        public static Texture2D ShapeEditorShapeCreateTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/ShapeEditorShapeCreate.png");
            }
        }

        public static Texture2D ShapeEditorShapeDuplicateTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/ShapeEditorShapeDuplicate.png");
            }
        }

        public static Texture2D ShapeEditorExtrudeBevelTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/ShapeEditorExtrudeBevel.png");
            }
        }

        public static Texture2D ShapeEditorExtrudePointTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/ShapeEditorExtrudePoint.png");
            }
        }

        public static Texture2D ShapeEditorExtrudeRevolveTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/ShapeEditorExtrudeRevolve.png");
            }
        }

        public static Texture2D ShapeEditorExtrudeShapeTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/ShapeEditorExtrudeShape.png");
            }
        }

        public static Texture2D ShapeEditorFlipHorizontallyTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/ShapeEditorFlipHorizontally.png");
            }
        }

        public static Texture2D ShapeEditorFlipVerticallyTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/ShapeEditorFlipVertically.png");
            }
        }

        public static Texture2D ShapeEditorNewTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/ShapeEditorNew.png");
            }
        }

        public static Texture2D ShapeEditorOpenTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/ShapeEditorOpen.png");
            }
        }

        public static Texture2D ShapeEditorRestoreTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/ShapeEditorRestore.png");
            }
        }

        public static Texture2D ShapeEditorRotate90LeftTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/ShapeEditorRotate90Left.png");
            }
        }

        public static Texture2D ShapeEditorRotate90RightTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/ShapeEditorRotate90Right.png");
            }
        }

        public static Texture2D ShapeEditorSaveTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/ShapeEditorSave.png");
            }
        }

        public static Texture2D ShapeEditorSegmentBezierTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/ShapeEditorSegmentBezier.png");
            }
        }

        public static Texture2D ShapeEditorSegmentBezierDetailTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/ShapeEditorSegmentBezierDetail.png");
            }
        }

        public static Texture2D ShapeEditorDeleteTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/ShapeEditorDelete.png");
            }
        }

        public static Texture2D ShapeEditorSegmentInsertTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/ShapeEditorSegmentInsert.png");
            }
        }

        public static Texture2D ShapeEditorSegmentExtrudeTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/ShapeEditorSegmentExtrude.png");
            }
        }

        public static Texture2D ShapeEditorSegmentLinearTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/ShapeEditorSegmentLinear.png");
            }
        }

        public static Texture2D ShapeEditorZoomInTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/ShapeEditorZoomIn.png");
            }
        }

        public static Texture2D ShapeEditorZoomOutTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/ShapeEditorZoomOut.png");
            }
        }

        public static Texture2D ShapeEditorHomeTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/ShapeEditorHome.png");
            }
        }

        public static Texture2D ImporterUnrealGoldTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/ImporterUnrealGold.png");
            }
        }

        public static Texture2D ImporterValveMapFormat2006Texture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/ImporterValveMapFormat2006.png");
            }
        }

        public static Texture2D ImporterQuake1Texture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/ImporterQuake1.png");
            }
        }

        public static Texture2D ImporterBackgroundTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/ImporterBackground.png");
            }
        }

        public static Texture2D MouseRightClickHintTexture
        {
            get
            {
                return (Texture2D)LoadObject("Gizmos/MouseRightClickHint.png");
            }
        }

        public static Material GetExcludedMaterial()
        {
            if (excludedMaterial == null)
            {
                excludedMaterial = new Material(Shader.Find("SabreCSG/SeeExcluded"));
                excludedMaterial.hideFlags = HideFlags.HideAndDontSave;
                excludedMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
                excludedMaterial.mainTexture = (Texture2D)LoadObject("Internal/Excluded.png");
            }
            return excludedMaterial;
        }

        public static Material GetGreyscaleUIMaterial()
        {
            if (greyscaleUIMaterial == null)
            {
                greyscaleUIMaterial = new Material(Shader.Find("Hidden/Grayscale-GUITexture"));
                greyscaleUIMaterial.hideFlags = HideFlags.HideAndDontSave;
                greyscaleUIMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
            }
            return greyscaleUIMaterial;
        }

        public static Material GetSelectedBrushMaterial()
        {
            if (selectedBrushMaterial == null)
            {
                selectedBrushMaterial = new Material(Shader.Find("SabreCSG/Line"));
                selectedBrushMaterial.hideFlags = HideFlags.HideAndDontSave;
                selectedBrushMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
            }
            return selectedBrushMaterial;
        }

        public static Material GetSelectedBrushDashedMaterial()
        {
            if (selectedBrushDashedMaterial == null)
            {
                selectedBrushDashedMaterial = new Material(Shader.Find("SabreCSG/Line Dashed"));
                selectedBrushDashedMaterial.hideFlags = HideFlags.HideAndDontSave;
                selectedBrushDashedMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
            }
            return selectedBrushDashedMaterial;
        }

        public static Material GetSelectedBrushDashedAlphaMaterial()
        {
            if (selectedBrushDashedAlphaMaterial == null)
            {
                selectedBrushDashedAlphaMaterial = new Material(Shader.Find("SabreCSG/Line Dashed Alpha"));
                selectedBrushDashedAlphaMaterial.hideFlags = HideFlags.HideAndDontSave;
                selectedBrushDashedAlphaMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
            }
            return selectedBrushDashedAlphaMaterial;
        }

        public static Material GetGizmoMaterial()
        {
            if (gizmoMaterial == null)
            {
                Shader shader = Shader.Find("SabreCSG/Handle");
                gizmoMaterial = new Material(shader);
                gizmoMaterial.hideFlags = HideFlags.HideAndDontSave;
                gizmoMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
                gizmoMaterial.mainTexture = (Texture2D)LoadObject("Gizmos/SquareGizmo8x8.png");
            }
            return gizmoMaterial;
        }

        public static Material GetVertexMaterial()
        {
            if (vertexMaterial == null)
            {
                Shader shader = Shader.Find("SabreCSG/Handle");
                vertexMaterial = new Material(shader);
                vertexMaterial.hideFlags = HideFlags.HideAndDontSave;
                vertexMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
                vertexMaterial.mainTexture = (Texture2D)LoadObject("Gizmos/CircleGizmo8x8.png");
            }
            return vertexMaterial;
        }

        public static Material GetCircleMaterial()
        {
            if (circleMaterial == null)
            {
                Shader shader = Shader.Find("SabreCSG/Handle");
                circleMaterial = new Material(shader);
                circleMaterial.hideFlags = HideFlags.HideAndDontSave;
                circleMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
                circleMaterial.mainTexture = (Texture2D)LoadObject("Gizmos/Circle.png");
            }
            return circleMaterial;
        }

        public static Material GetCircleOutlineMaterial()
        {
            if (circleOutlineMaterial == null)
            {
                Shader shader = Shader.Find("SabreCSG/Handle");
                circleOutlineMaterial = new Material(shader);
                circleOutlineMaterial.hideFlags = HideFlags.HideAndDontSave;
                circleOutlineMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
                circleOutlineMaterial.mainTexture = (Texture2D)LoadObject("Gizmos/CircleOutline.png");
            }
            return circleOutlineMaterial;
        }

        public static Material GetPreviewMaterial()
        {
            if (previewMaterial == null)
            {
                Shader shader = Shader.Find("SabreCSG/Preview");

                previewMaterial = new Material(shader);
                previewMaterial.hideFlags = HideFlags.HideAndDontSave;
                previewMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
            }

            return previewMaterial;
        }

        public static Material GetNoCSGMaterial()
        {
            return (Material)LoadObject("Materials/NoCSG.mat");
        }

        public static Material GetAddMaterial()
        {
            return (Material)LoadObject("Materials/Add.mat");
        }

        public static Material GetSubtractMaterial()
        {
            return (Material)LoadObject("Materials/Subtract.mat");
        }

        public static Material GetVolumeMaterial()
        {
            return (Material)LoadObject("Materials/Volume.mat");
        }

        public static Material GetCollisionMaterial()
        {
            return (Material)LoadObject("Materials/Collision.mat");
        }

        public static Material GetPlaneMaterial()
        {
            if (planeMaterial == null)
            {
                planeMaterial = new Material(Shader.Find("SabreCSG/Plane"));
                planeMaterial.hideFlags = HideFlags.HideAndDontSave;
                planeMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
            }
            return planeMaterial;
        }

        #endregion Accessors
    }
}

#endif