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
        private Dictionary<string, Material> m_MaterialCache = new Dictionary<string, Material>();

        /// <summary>
        /// Attempts to find a material in the project by name.
        /// </summary>
        /// <param name="names">The material names to search for, longest to shortest like 'PlayrShp.Ceiling.Hullwk' and 'Hullwk'.</param>
        /// <returns>The material if found or null.</returns>
        public Material FindMaterial(string[] names)
        {
#if UNITY_EDITOR
            // iterate through all the names to search for, assuming it's longest to shortest.
            for (int i = 0; i < names.Length; i++)
            {
                string name = names[i];
                // check whether we already have this material in our cache.
                if (m_MaterialCache.ContainsKey(name))
                    // if found immediately return this result.
                    return m_MaterialCache[name];

                // have unity search for the asset (this can get very slow when there are a lot of materials in the project).
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Material " + name);

                // iterate through unity's search results:
                for (int j = 0; j < guids.Length; j++)
                {
                    // get the file system path of the file.
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[j]);
                    string file = System.IO.Path.GetFileNameWithoutExtension(path);

                    // make sure the file name matches the desired material name exactly to prevent false positives as much as possible.
                    if (file.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        // we found a perfect match, load the material.
                        Material material = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
                        // put the material in the cache.
                        m_MaterialCache.Add(name, material);
                        // return the material.
                        return material;
                    }
                }
            }
            // we didn't find anything...
#endif
            return null;
        }
    }
}

#endif