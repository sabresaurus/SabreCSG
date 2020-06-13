#if UNITY_EDITOR || RUNTIME_CSG

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sabresaurus.SabreCSG.Importers
{
    /// <summary>
    /// Searches for materials by name more precisely to prevent false positives and caches them for
    /// increasingly faster search results.
    /// </summary>
    public class MaterialSearcher
    {
        /// <summary>
        /// The material cache dictionary.
        /// </summary>
        private readonly Dictionary<string, Material> m_MaterialCache =
            new Dictionary<string, Material>(StringComparer.InvariantCultureIgnoreCase);

        /// <summary>
        /// Attempts to find a material in the project by name.
        /// </summary>
        /// <param name="names">The material names to search for, longest to shortest like 'PlayrShp.Ceiling.Hullwk' and 'Hullwk'.</param>
        /// <returns>The material if found or null.</returns>
        public Material FindMaterial(string[] names)
        {
            // Iterate through all the names to search for, assuming it's longest to shortest.
            for (int i = 0; i < names.Length; i++)
            {
                Material output = FindMaterial(names[i]);
                if (output != null)
                    return output;
            }

            // We didn't find anything...
            return null;
        }

        /// <summary>
        /// Attempts to find a material in the project by name.
        /// </summary>
        /// <param name="name">The material name to search for.</param>
        /// <returns>The material if found or null.</returns>
        private Material FindMaterial(string name)
        {
            // Pre-cache the available materials
            if (m_MaterialCache.Count == 0)
                BuildCache();

            // Check whether we already have this material in our cache.
            Material output;
            m_MaterialCache.TryGetValue(name, out output);
            return output;
        }

        /// <summary>
        /// Populates the cache of filename to Material.
        /// </summary>
        private void BuildCache()
        {
#if UNITY_EDITOR
            // Have unity search for the asset (this can get very slow when there are a lot of materials in the project).
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Material");

            // Iterate through Unity's search results:
            for (int j = 0; j < guids.Length; j++)
            {
                // Get the file system path of the file.
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[j]);
                string file = System.IO.Path.GetFileNameWithoutExtension(path);
                if (file == null)
                    continue;

                // Cache the material
                Material material = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
                m_MaterialCache[file] = material;
            }
#endif
        }
    }
}

#endif