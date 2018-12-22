#if UNITY_EDITOR

using System;
using UnityEngine;
using UnityEditor;

namespace Sabresaurus.SabreCSG
{
    /// <summary>
    /// Provides the context menus for the asset browser.
    /// </summary>
    /// <seealso cref="UnityEditor.EditorWindow"/>
    public class AssetBrowserContextMenu : UnityEditor.EditorWindow
    {
        [MenuItem("Assets/SabreCSG/Create Material For Texture(s)")]
        private static void CreateMaterialForTextures()
        {
            // get all selected textures in the asset browser.
            Texture2D[] textures = Array.ConvertAll(Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets), item => (Texture2D)item);

            // begin asset editing, this prevents unity from importing the materials immediately once they are created (that's slow).
            AssetDatabase.StartAssetEditing();

            // iterate through each selected texture:
            for (int i = 0; i < textures.Length; i++)
            {
                Texture2D texture = textures[i];

                EditorUtility.DisplayProgressBar("SabreCSG: Creating Material For Texture(s)", "Creating Material '" + texture.name + "'...", i / (float)textures.Length);

                // create a material asset for the texture.
                Material material = new Material(Shader.Find("Standard"));
                material.SetTexture("_MainTex", texture);
                material.SetFloat("_Glossiness", 0.0f);

                // get the directory path.
                string path = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(texture)) + "\\";
                string file = System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(texture)) + ".mat";

                AssetDatabase.CreateAsset(material, path + file);
            }

            // stop asset editing, this allows unity to import all the materials we created in one go.
            AssetDatabase.StopAssetEditing();

            EditorUtility.ClearProgressBar();
        }

        [MenuItem("Assets/SabreCSG/Create Material For Texture(s)", true)]
        private static bool IsCreateMaterialForTexturesEnabled()
        {
            // must have a texture selected in the asset browser.
            return Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets).Length > 0;
        }
    }
}

#endif