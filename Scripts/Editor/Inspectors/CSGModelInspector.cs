using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;

namespace Sabresaurus.SabreCSG
{
    [CustomEditor(typeof(CSGModel))]
    public class CSGModelInspector : Editor
    {
		// Build settings for the next build
		SerializedProperty generateCollisionMeshesProperty;
		SerializedProperty generateTangentsProperty;
        SerializedProperty optimizeGeometryProperty;
		SerializedProperty saveMeshesAsAssetsProperty;
		SerializedProperty generateLightmapUVsProperty;

		SerializedProperty unwrapAngleErrorProperty;
		SerializedProperty unwrapAreaErrorProperty;
		SerializedProperty unwrapHardAngleProperty;
		SerializedProperty unwrapPackMarginProperty;

        SerializedProperty shadowCastingModeProperty;

		SerializedProperty defaultPhysicsMaterialProperty;
		SerializedProperty defaultVisualMaterialProperty;

		// Build settings from the last build
		SerializedProperty lastBuildDefaultPhysicsMaterialProperty;
		SerializedProperty lastBuildDefaultVisualMaterialProperty;

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
			using (new NamedVerticalScope("Build Settings"))
			{
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
				if(EditorGUI.EndChangeCheck())
				{
					PhysicMaterial newPhysicsMaterial = defaultPhysicsMaterialProperty.objectReferenceValue as PhysicMaterial;

					// Update the built mesh colliders that use the old material
					UpdatePhysicsMaterial(lastPhysicsMaterial, newPhysicsMaterial);
				}

				// Track the last visual material, so that if the user changes it we can update built renderers instantly
				Material lastVisualMaterial = defaultVisualMaterialProperty.objectReferenceValue as Material;

				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField(defaultVisualMaterialProperty, new GUIContent("Default Visual Material"));
				if(EditorGUI.EndChangeCheck())
				{
					// User has changed the material, so grab the new material
					Material newVisualMaterial = defaultVisualMaterialProperty.objectReferenceValue as Material;
					// EnsureDefaultMaterialSet hasn't had a chance to run yet, so make sure we have a solid material reference
					if(newVisualMaterial == null)
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
				if(GUILayout.Button("Lit Texture (No Tint)"))
				{
					Material newVisualMaterial = AssetDatabase.LoadMainAssetAtPath(CSGModel.GetSabreCSGPath() + "Resources/Materials/Default_Map.mat") as Material;
					SetVisualMaterial(lastVisualMaterial, newVisualMaterial);
				}
				if(GUILayout.Button("Lit Vertex Tint"))
				{
					Material newVisualMaterial = AssetDatabase.LoadMainAssetAtPath(CSGModel.GetSabreCSGPath() + "Resources/Materials/Default_LitWithTint.mat") as Material;
					SetVisualMaterial(lastVisualMaterial, newVisualMaterial);
				}
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();
				if(GUILayout.Button("Unlit Vertex Color"))
				{
					Material newVisualMaterial = AssetDatabase.LoadMainAssetAtPath(CSGModel.GetSabreCSGPath() + "Resources/Materials/Default_VertexColor.mat") as Material;
					SetVisualMaterial(lastVisualMaterial, newVisualMaterial);
				}
				if(GUILayout.Button("Lit Vertex Color"))
				{
					Material newVisualMaterial = AssetDatabase.LoadMainAssetAtPath(CSGModel.GetSabreCSGPath() + "Resources/Materials/Default_VertexColorLit.mat") as Material;
					SetVisualMaterial(lastVisualMaterial, newVisualMaterial);
				}
				EditorGUILayout.EndHorizontal();
			}

			using (new NamedVerticalScope("Export"))
			{
				if(GUILayout.Button("Export All To OBJ"))
				{
					csgModel.ExportOBJ(false);
				}

	            if (GUILayout.Button("Export Selected To OBJ"))
	            {
	                csgModel.ExportOBJ(true);
	            }
			}

			using (new NamedVerticalScope("Stats"))
			{
	            BuildMetrics buildMetrics = csgModel.BuildMetrics;

				GUILayout.Label("Vertices: " + buildMetrics.TotalVertices);
				GUILayout.Label("Triangles: " + buildMetrics.TotalTriangles);
				GUILayout.Label("Meshes: " + buildMetrics.TotalMeshes);
				GUILayout.Label("Build Time: " + buildMetrics.BuildTime.ToString());
			}

			// Make sure any serialize property changes are committed to the underlying Unity Object
			bool anyApplied = this.serializedObject.ApplyModifiedProperties();

			if(anyApplied)
			{
				// Make sure that nothing else is included in the Undo, as an immediate rebuild may take place and 
				// we don't want the rebuild's lastBuildSettings being included in the undo step
				Undo.IncrementCurrentGroup();
			}
        }

		private void SetVisualMaterial(Material lastVisualMaterial, Material newVisualMaterial)
		{
			// EnsureDefaultMaterialSet hasn't had a chance to run yet, so make sure we have a solid material reference
			if(newVisualMaterial == null)
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
			if(newMaterial != oldMaterial && newMaterial != null)
			{
				CSGModel csgModel = (CSGModel)target;

				// Update the built mesh renderers that use the old material
				Transform meshGroup = csgModel.GetMeshGroupTransform();

				if(meshGroup != null)
				{
					MeshRenderer[] meshRenderers = meshGroup.GetComponentsInChildren<MeshRenderer>();
					for (int i = 0; i < meshRenderers.Length; i++) 
					{
						if(meshRenderers[i].sharedMaterial == oldMaterial)
						{
							meshRenderers[i].sharedMaterial = newMaterial;
						}
					}
				}

				// Update all the polygons in the source brushes
				List<Brush> brushes = csgModel.GetBrushes();
				for (int i = 0; i < brushes.Count; i++) 
				{
					if(brushes[i] != null) // Make sure the tracked brush still exists
					{
						Polygon[] polygons = brushes[i].GetPolygons();
						for (int j = 0; j < polygons.Length; j++) 
						{
							// If the polygon uses the old material (as an override)
							if(polygons[j].Material == oldMaterial)
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
			if(newMaterial != oldMaterial)
			{
				CSGModel csgModel = (CSGModel)target;

				// Update the built mesh renderers that use the old material
				Transform meshGroup = csgModel.GetMeshGroupTransform();

				if(meshGroup != null)
				{
					MeshCollider[] meshColliders = meshGroup.GetComponentsInChildren<MeshCollider>();
					for (int i = 0; i < meshColliders.Length; i++) 
					{
						if(meshColliders[i].sharedMaterial == oldMaterial)
						{
							meshColliders[i].sharedMaterial = newMaterial;
						}
					}
				}

				// Update the last build's default material because we don't need to build again
				lastBuildDefaultPhysicsMaterialProperty.objectReferenceValue = newMaterial;
			}
		}
    }
}