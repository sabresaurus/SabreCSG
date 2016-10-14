#if UNITY_EDITOR || RUNTIME_CSG
using UnityEngine;
using System.Collections;

namespace Sabresaurus.SabreCSG
{
	[System.Serializable]
	public class CSGBuildSettings
	{
		// Whether to also do a collision pass
		public bool GenerateCollisionMeshes = true;

		// Also calculate tangents (needed for Unity's built in bump mapping)
		public bool GenerateTangents = true;

        public bool OptimizeGeometry = true;

        // Generate a UV2 channel for lightmapping
        [Tooltip("Note that when enabled this will have a non-trivial increase in build times")]
		public bool GenerateLightmapUVs = false;
		
		// Unwrap settings, note these are different to what is displayed in the Model Importer, see http://docs.unity3d.com/401/Documentation/Manual/LightmappingUV.html
		[Range(0f,1f)]
		public float UnwrapAngleError = 0.08f; // 0 to 1
		[Range(0f,1f)]
		public float UnwrapAreaError = 0.15f; // 0 to 1
		[Range(0f,180f)]
		public float UnwrapHardAngle = 88f; // degrees, 0 to 180
		public float UnwrapPackMargin = 0.00390625f; // Assumes a 1024 texture, pack margin = PadPixels / 1024

		// What default physics material to use on collision meshes
		public PhysicMaterial DefaultPhysicsMaterial = null;

		public Material DefaultVisualMaterial = null;

		[HideInInspector]
		public bool IsBuilt = false; // Only really relevant to last build settings

		public CSGBuildSettings ShallowCopy()
		{
			return (CSGBuildSettings)this.MemberwiseClone();
		}

		/// <summary>
		/// Compares two build settings and returns if they are practially different. 
		/// </summary>
		public static bool AreDifferent(CSGBuildSettings settings1, CSGBuildSettings settings2)
		{
			if(settings1.GenerateCollisionMeshes != settings2.GenerateCollisionMeshes)
			{
				return true;
			}
			if(settings1.GenerateTangents != settings2.GenerateTangents)
			{
				return true;
			}
            if (settings1.OptimizeGeometry != settings2.OptimizeGeometry)
            {
                return true;
            }
            if (settings1.GenerateLightmapUVs != settings2.GenerateLightmapUVs)
			{
				return true;
			}

			// Only compare UV unwrap settings if unwrapping is in use
			if(settings1.GenerateLightmapUVs && settings1.GenerateLightmapUVs)
			{
				if(settings1.UnwrapAngleError != settings2.UnwrapAngleError)
				{
					return true;
				}

				if(settings1.UnwrapAreaError != settings2.UnwrapAreaError)
				{
					return true;
				}

				if(settings1.UnwrapHardAngle != settings2.UnwrapHardAngle)
				{
					return true;
				}

				if(settings1.UnwrapPackMargin != settings2.UnwrapPackMargin)
				{
					return true;
				}
			}

			if(settings1.DefaultPhysicsMaterial != settings2.DefaultPhysicsMaterial)
			{
				return true;
			}
			if(settings1.DefaultVisualMaterial != settings2.DefaultVisualMaterial)
			{
				return true;
			}

			// Don't compare IsBuilt

			// No practical differences found
			return false;
		}
	}
}
#endif