using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System;

namespace Sabresaurus.SabreCSG
{
    [CustomEditor(typeof(CSGModel))]
    public class CSGModelInspector : Editor
    {
        // Build settings for the next build

        private SerializedProperty generateCollisionMeshesProperty;
        private SerializedProperty generateTangentsProperty;
        private SerializedProperty optimizeGeometryProperty;
        private SerializedProperty saveMeshesAsAssetsProperty;
        private SerializedProperty generateLightmapUVsProperty;

        private SerializedProperty unwrapAngleErrorProperty;
        private SerializedProperty unwrapAreaErrorProperty;
        private SerializedProperty unwrapHardAngleProperty;
        private SerializedProperty unwrapPackMarginProperty;

        private SerializedProperty shadowCastingModeProperty;

        private SerializedProperty reflectionProbeUsageProperty;

        private SerializedProperty defaultPhysicsMaterialProperty;
        private SerializedProperty defaultVisualMaterialProperty;

        // Build settings from the last build

        private SerializedProperty lastBuildDefaultPhysicsMaterialProperty;
        private SerializedProperty lastBuildDefaultVisualMaterialProperty;

        // Temporary importer settings.

        private static int importerUnrealGoldScale = 64;

        public void OnEnable()
        {
            // Build settings for the next build
            generateCollisionMeshesProperty = serializedObject.FindProperty("buildSettings.GenerateCollisionMeshes");
            generateTangentsProperty = serializedObject.FindProperty("buildSettings.GenerateTangents");
            optimizeGeometryProperty = serializedObject.FindProperty("buildSettings.OptimizeGeometry");
            saveMeshesAsAssetsProperty = serializedObject.FindProperty("buildSettings.SaveMeshesAsAssets");
            generateLightmapUVsProperty = serializedObject.FindProperty("buildSettings.GenerateLightmapUVs");

            unwrapAngleErrorProperty = serializedObject.FindProperty("buildSettings.UnwrapAngleError");
            unwrapAreaErrorProperty = serializedObject.FindProperty("buildSettings.UnwrapAreaError");
            unwrapHardAngleProperty = serializedObject.FindProperty("buildSettings.UnwrapHardAngle");
            unwrapPackMarginProperty = serializedObject.FindProperty("buildSettings.UnwrapPackMargin");

            shadowCastingModeProperty = serializedObject.FindProperty("buildSettings.ShadowCastingMode");

            reflectionProbeUsageProperty = serializedObject.FindProperty("buildSettings.ReflectionProbeUsage");

            defaultPhysicsMaterialProperty = serializedObject.FindProperty("buildSettings.DefaultPhysicsMaterial");
            defaultVisualMaterialProperty = serializedObject.FindProperty("buildSettings.DefaultVisualMaterial");

            // Build settings from the last build
            lastBuildDefaultPhysicsMaterialProperty = serializedObject.FindProperty("lastBuildSettings.DefaultPhysicsMaterial");
            lastBuildDefaultVisualMaterialProperty = serializedObject.FindProperty("lastBuildSettings.DefaultVisualMaterial");
        }

        public override void OnInspectorGUI()
        {
            CSGModel csgModel = (CSGModel)target;

            // Ensure the default material is set
            csgModel.EnsureDefaultMaterialSet();

            DrawDefaultInspector();

            this.serializedObject.Update();
            using (NamedVerticalScope scope = new NamedVerticalScope("Build Settings"))
            {
                scope.WikiLink = "Build-Settings";

                EditorGUIUtility.fieldWidth = 0;
                EditorGUIUtility.labelWidth = 160;

                EditorGUILayout.PropertyField(generateCollisionMeshesProperty, new GUIContent("Generate Collision Meshes"));
                EditorGUILayout.PropertyField(generateTangentsProperty, new GUIContent("Generate Tangents"));

                EditorGUILayout.PropertyField(generateLightmapUVsProperty, new GUIContent("Generate Lightmap UVs"));
                EditorGUIUtility.labelWidth = 125;

                GUI.enabled = generateLightmapUVsProperty.boolValue;
                EditorGUI.indentLevel = 1;
                EditorGUILayout.PropertyField(unwrapAngleErrorProperty, new GUIContent("Unwrap Angle Error"));
                EditorGUILayout.PropertyField(unwrapAreaErrorProperty, new GUIContent("Unwrap Area Error"));
                EditorGUILayout.PropertyField(unwrapHardAngleProperty, new GUIContent("Unwrap Hard Angle"));
                EditorGUILayout.PropertyField(unwrapPackMarginProperty, new GUIContent("Unwrap Pack Margin"));
                EditorGUI.indentLevel = 0;
                EditorGUIUtility.labelWidth = 0;
                GUI.enabled = true;

                EditorGUILayout.PropertyField(shadowCastingModeProperty, new GUIContent("Shadow Casting Mode"));
                EditorGUILayout.PropertyField(reflectionProbeUsageProperty, new GUIContent("Reflection Probes"));

                // Experimental build settings to enable features that are not yet completely stable
                GUILayout.Label("Experimental", EditorStyles.boldLabel);
                EditorGUI.indentLevel = 1;

                EditorGUILayout.PropertyField(optimizeGeometryProperty, new GUIContent("Optimize Geometry"));
                EditorGUILayout.PropertyField(saveMeshesAsAssetsProperty, new GUIContent("Save Meshes As Assets"));
                EditorGUI.indentLevel = 0;
            }

            using (new NamedVerticalScope("Default Material"))
            {
                PhysicMaterial lastPhysicsMaterial = defaultPhysicsMaterialProperty.objectReferenceValue as PhysicMaterial;

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(defaultPhysicsMaterialProperty, new GUIContent("Default Physics Material"));
                if (EditorGUI.EndChangeCheck())
                {
                    PhysicMaterial newPhysicsMaterial = defaultPhysicsMaterialProperty.objectReferenceValue as PhysicMaterial;

                    // Update the built mesh colliders that use the old material
                    UpdatePhysicsMaterial(lastPhysicsMaterial, newPhysicsMaterial);
                }

                // Track the last visual material, so that if the user changes it we can update built renderers instantly
                Material lastVisualMaterial = defaultVisualMaterialProperty.objectReferenceValue as Material;

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(defaultVisualMaterialProperty, new GUIContent("Default Visual Material"));
                if (EditorGUI.EndChangeCheck())
                {
                    // User has changed the material, so grab the new material
                    Material newVisualMaterial = defaultVisualMaterialProperty.objectReferenceValue as Material;
                    // EnsureDefaultMaterialSet hasn't had a chance to run yet, so make sure we have a solid material reference
                    if (newVisualMaterial == null)
                    {
                        newVisualMaterial = csgModel.GetDefaultFallbackMaterial();
                        defaultVisualMaterialProperty.objectReferenceValue = newVisualMaterial;
                    }

                    // Update the built renderers that use the old material, also update source brush polygons
                    UpdateVisualMaterial(lastVisualMaterial, newVisualMaterial);

                    // Update the last build's default material because we don't need to build again
                    lastBuildDefaultVisualMaterialProperty.objectReferenceValue = newVisualMaterial;
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Lit Texture (No Tint)"))
                {
                    Material newVisualMaterial = AssetDatabase.LoadMainAssetAtPath(CSGModel.GetSabreCSGPath() + "Resources/Materials/Default_Map.mat") as Material;
                    SetVisualMaterial(lastVisualMaterial, newVisualMaterial);
                }
                if (GUILayout.Button("Lit Vertex Tint"))
                {
                    Material newVisualMaterial = AssetDatabase.LoadMainAssetAtPath(CSGModel.GetSabreCSGPath() + "Resources/Materials/Default_LitWithTint.mat") as Material;
                    SetVisualMaterial(lastVisualMaterial, newVisualMaterial);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Unlit Vertex Color"))
                {
                    Material newVisualMaterial = AssetDatabase.LoadMainAssetAtPath(CSGModel.GetSabreCSGPath() + "Resources/Materials/Default_VertexColor.mat") as Material;
                    SetVisualMaterial(lastVisualMaterial, newVisualMaterial);
                }
                if (GUILayout.Button("Lit Vertex Color"))
                {
                    Material newVisualMaterial = AssetDatabase.LoadMainAssetAtPath(CSGModel.GetSabreCSGPath() + "Resources/Materials/Default_VertexColorLit.mat") as Material;
                    SetVisualMaterial(lastVisualMaterial, newVisualMaterial);
                }
                EditorGUILayout.EndHorizontal();
            }

            using (NamedVerticalScope scope = new NamedVerticalScope("Export"))
            {
                scope.WikiLink = "Build-Settings#exporting-obj-files";

                if (GUILayout.Button("Export All To OBJ"))
                {
                    csgModel.ExportOBJ(false);
                }

                if (GUILayout.Button("Export Selected To OBJ"))
                {
                    csgModel.ExportOBJ(true);
                }
            }

            using (new NamedVerticalScope("Import"))
            {
                GuiLayoutBeginImporterSection(SabreCSGResources.ImporterUnrealGoldTexture, "Unreal Gold Importer", "Henry de Jongh");

                importerUnrealGoldScale = EditorGUILayout.IntField("Scale", importerUnrealGoldScale);
                if (importerUnrealGoldScale < 1) importerUnrealGoldScale = 1;

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Import Unreal Gold Map (*.t3d)"))
                {
                    try
                    {
                        string path = EditorUtility.OpenFilePanel("Import Unreal Gold Map", "", "t3d");
                        if (path.Length != 0)
                        {
                            EditorUtility.DisplayProgressBar("SabreCSG: Importing Unreal Gold Map", "Parsing Unreal Text File (*.t3d)...", 0.0f);
                            var importer = new Importers.UnrealGold.T3dImporter();
                            var map = importer.Import(path);
                            Importers.UnrealGold.T3dMapConverter.Import(csgModel, map, importerUnrealGoldScale);
                        }
                    }
                    catch (Exception ex)
                    {
                        EditorUtility.ClearProgressBar();
                        EditorUtility.DisplayDialog("Unreal Gold Map Import", "An exception occurred while importing the map:\r\n" + ex.Message, "Ohno!");
                    }
                }
                if (GUILayout.Button("?", GUILayout.Width(16)))
                {
                    EditorUtility.DisplayDialog("Unreal Gold Importer", "This importer was created using Unreal Gold 227 (http://oldunreal.com/).\n\nImportant Notes:\n* It will try to find the materials in your project automatically. First it looks for the full name like 'PlayrShp.Ceiling.Hullwk' then the last word 'Hullwk'. The latter option could cause some false positives, try creating a material with the full name if this happens.\n* This importer requires you to place a massive additive cube around your whole level as Unreal Editor uses the subtractive workflow.\n\nKnown Issues:\n* Concave brushes may cause corruptions.", "Okay");
                }
                EditorGUILayout.EndHorizontal();
                GuiLayoutEndImporterSection();

                //
                EditorGUILayout.Space();
                //

                GuiLayoutBeginImporterSection(SabreCSGResources.ImporterValveMapFormat2006Texture, "Source Engine 2006 Importer", "Henry de Jongh");

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Import Source Engine Map (*.vmf)"))
                {
                    try
                    {
                        string path = EditorUtility.OpenFilePanel("Import Source Engine Map", "", "vmf");
                        if (path.Length != 0)
                        {
                            EditorUtility.DisplayProgressBar("SabreCSG: Importing Source Engine Map", "Parsing Valve Map Format File (*.vmf)...", 0.0f);
                            var importer = new Importers.ValveMapFormat2006.VmfImporter();
                            var map = importer.Import(path);
                            Importers.ValveMapFormat2006.VmfWorldConverter.Import(csgModel, map);
                        }
                    }
                    catch (Exception ex)
                    {
                        EditorUtility.ClearProgressBar();
                        EditorUtility.DisplayDialog("Source Engine Map Import", "An exception occurred while importing the map:\r\n" + ex.Message, "Ohno!");
                    }
                }

                if (GUILayout.Button("?", GUILayout.Width(16)))
                {
                    EditorUtility.DisplayDialog("Source Engine 2006 Importer", "This importer was created using Source SDK maps and Hammer 4.1.\n\nImportant Notes:\n* It will try to find the materials in your project automatically. First it looks for the full name with forward slashes '/' replaced by periods '.' like 'BRICK.BRICKFLOOR001A' then the last word 'BRICKFLOOR001A'. The latter option could cause some false positives, try creating a material with the full name if this happens.", "Okay");
                }
                EditorGUILayout.EndHorizontal();
                GuiLayoutEndImporterSection();

                //
                EditorGUILayout.Space();
                //

                GuiLayoutBeginImporterSection(SabreCSGResources.ImporterQuake1Texture, "Quake 1 Importer", "Jasmine Mickle");

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Import Quake 1 Map (*.map)"))
                {
                    try
                    {
                        string path = EditorUtility.OpenFilePanel("Import Quake 1 Map", "", "map");
                        if (path.Length != 0)
                        {
                            EditorUtility.DisplayProgressBar("SabreCSG: Importing Quake 1 Map", "Parsing Quake 1 Map File (*.map)...", 0.0f);
                            var importer = new Importers.Quake1.MapImporter();
                            var map = importer.Import(path);
                            Importers.Quake1.MapWorldConverter.Import(csgModel, map);
                        }
                    }
                    catch (Exception ex)
                    {
                        EditorUtility.ClearProgressBar();
                        EditorUtility.DisplayDialog("Quake 1 Map Import", "An exception occurred while importing the map:\r\n" + ex.Message, "Ohno!");
                    }
                }

                if (GUILayout.Button("?", GUILayout.Width(16)))
                {
                    EditorUtility.DisplayDialog("Quake 1 Importer", "This importer (parser by Henry de Jongh) was created using Trenchbroom 2.0.6.\n\nImportant Notes:\n* It will try to find the materials in your project automatically. It will replace any leading '*' with '#' like '#teleport'.\n\nKnown Issues:\n* 45 degree angled walls may not have correct UV texture coordinates (are not correctly picking the dominant axis because there are two).\n* Negative vertex coordinates may not have correct UV texture coordinates (feel free to contribute improved UV mapping code if you know how it works, we had to guess most of the maths).", "Okay");
                }
                EditorGUILayout.EndHorizontal();
                GuiLayoutEndImporterSection();
            }

            using (new NamedVerticalScope("Stats"))
            {
                BuildMetrics buildMetrics = csgModel.BuildMetrics;

                GUILayout.Label("Brushes: " + csgModel.BrushCount);
                GUILayout.Label("Vertices: " + buildMetrics.TotalVertices);
                GUILayout.Label("Triangles: " + buildMetrics.TotalTriangles);
                GUILayout.Label("Meshes: " + buildMetrics.TotalMeshes);
                GUILayout.Label("Build Time: " + buildMetrics.BuildTime.ToString());
            }

            // Make sure any serialize property changes are committed to the underlying Unity Object
            bool anyApplied = this.serializedObject.ApplyModifiedProperties();

            if (anyApplied)
            {
                // Make sure that nothing else is included in the Undo, as an immediate rebuild may take place and
                // we don't want the rebuild's lastBuildSettings being included in the undo step
                Undo.IncrementCurrentGroup();
            }
        }

        private void SetVisualMaterial(Material lastVisualMaterial, Material newVisualMaterial)
        {
            // EnsureDefaultMaterialSet hasn't had a chance to run yet, so make sure we have a solid material reference
            if (newVisualMaterial == null)
            {
                CSGModel csgModel = (CSGModel)target;
                newVisualMaterial = csgModel.GetDefaultFallbackMaterial();
            }

            // Update the built renderers that use the old material, also update source brush polygons
            UpdateVisualMaterial(lastVisualMaterial, newVisualMaterial);

            defaultVisualMaterialProperty.objectReferenceValue = newVisualMaterial;
            // Update the last build's default material because we don't need to build again
            lastBuildDefaultVisualMaterialProperty.objectReferenceValue = newVisualMaterial;
        }

        private void UpdateVisualMaterial(Material oldMaterial, Material newMaterial)
        {
            // If the material has changed and the new material is not null
            if (newMaterial != oldMaterial && newMaterial != null)
            {
                CSGModel csgModel = (CSGModel)target;

                // Update the built mesh renderers that use the old material
                Transform meshGroup = csgModel.GetMeshGroupTransform();

                if (meshGroup != null)
                {
                    MeshRenderer[] meshRenderers = meshGroup.GetComponentsInChildren<MeshRenderer>();
                    for (int i = 0; i < meshRenderers.Length; i++)
                    {
                        if (meshRenderers[i].sharedMaterial == oldMaterial)
                        {
                            meshRenderers[i].sharedMaterial = newMaterial;
                        }
                    }
                }

                // Update all the polygons in the source brushes
                List<Brush> brushes = csgModel.GetBrushes();
                for (int i = 0; i < brushes.Count; i++)
                {
                    if (brushes[i] != null) // Make sure the tracked brush still exists
                    {
                        Polygon[] polygons = brushes[i].GetPolygons();
                        for (int j = 0; j < polygons.Length; j++)
                        {
                            // If the polygon uses the old material (as an override)
                            if (polygons[j].Material == oldMaterial)
                            {
                                // Set the polygon to null, this removes the material override which gives consistent behaviour
                                polygons[j].Material = null;
                            }
                        }
                    }
                }
            }
        }

        private void UpdatePhysicsMaterial(PhysicMaterial oldMaterial, PhysicMaterial newMaterial)
        {
            // If the physics material has changed
            if (newMaterial != oldMaterial)
            {
                CSGModel csgModel = (CSGModel)target;

                // Update the built mesh renderers that use the old material
                Transform meshGroup = csgModel.GetMeshGroupTransform();

                if (meshGroup != null)
                {
                    MeshCollider[] meshColliders = meshGroup.GetComponentsInChildren<MeshCollider>();
                    for (int i = 0; i < meshColliders.Length; i++)
                    {
                        if (meshColliders[i].sharedMaterial == oldMaterial)
                        {
                            meshColliders[i].sharedMaterial = newMaterial;
                        }
                    }
                }

                // Update the last build's default material because we don't need to build again
                lastBuildDefaultPhysicsMaterialProperty.objectReferenceValue = newMaterial;
            }
        }

        private void GuiLayoutBeginImporterSection(Texture2D icon, string title, string author)
        {
            GUIStyle style = new GUIStyle();
            style.normal.background = SabreCSGResources.ImporterBackgroundTexture;

            EditorGUILayout.BeginVertical(style);
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label(icon);

            EditorGUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(title, SabreGUILayout.GetTitleStyle());
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Author: " + author);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void GuiLayoutEndImporterSection()
        {
            EditorGUILayout.EndVertical();
        }
    }
}