#if UNITY_EDITOR || RUNTIME_CSG

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Sabresaurus.SabreCSG.Importers.ValveMapFormat2006
{
    /// <summary>
    /// Converts a Hammer Map to SabreCSG Brushes.
    /// </summary>
    public static class VmfWorldConverter
    {
        /// <summary>
        /// Imports the specified world into the SabreCSG model.
        /// </summary>
        /// <param name="model">The model to import into.</param>
        /// <param name="world">The world to be imported.</param>
        /// <param name="scale">The scale modifier.</param>
        public static void Import(CSGModel model, VmfWorld world, int scale = 32)
        {
            try
            {
                model.BeginUpdate();

                // iterate through all solids.
                for (int i = 0; i < world.Solids.Count; i++)
                {
#if UNITY_EDITOR
                    UnityEditor.EditorUtility.DisplayProgressBar("SabreCSG: Importing Source Engine Map", "Converting Hammer Solids To SabreCSG Brushes (" + (i + 1) + " / " + world.Solids.Count + ")...", i / (float)world.Solids.Count);
#endif
                    VmfSolid solid = world.Solids[i];

                    // don't add triggers to the scene.
                    if (solid.Sides.Count > 0 && (
                        solid.Sides[0].Material == "TOOLS/TOOLSTRIGGER" ||
                        solid.Sides[0].Material == "TOOLS/TOOLSBLOCK_LOS" ||
                        solid.Sides[0].Material == "TOOLS/TOOLSBLOCKBULLETS" ||
                        solid.Sides[0].Material == "TOOLS/TOOLSBLOCKBULLETS2" ||
                        solid.Sides[0].Material == "TOOLS/TOOLSBLOCKSBULLETSFORCEFIELD" || // did the wiki have a typo or is BLOCKS truly plural?
                        solid.Sides[0].Material == "TOOLS/TOOLSBLOCKLIGHT" ||
                        solid.Sides[0].Material == "TOOLS/TOOLSCLIMBVERSUS" ||
                        solid.Sides[0].Material == "TOOLS/TOOLSHINT" ||
                        solid.Sides[0].Material == "TOOLS/TOOLSINVISIBLE" ||
                        solid.Sides[0].Material == "TOOLS/TOOLSINVISIBLENONSOLID" ||
                        solid.Sides[0].Material == "TOOLS/TOOLSINVISIBLELADDER" ||
                        solid.Sides[0].Material == "TOOLS/TOOLSINVISMETAL" ||
                        solid.Sides[0].Material == "TOOLS/TOOLSNODRAWROOF" ||
                        solid.Sides[0].Material == "TOOLS/TOOLSNODRAWWOOD" ||
                        solid.Sides[0].Material == "TOOLS/TOOLSNODRAWPORTALABLE" ||
                        solid.Sides[0].Material == "TOOLS/TOOLSSKIP" ||
                        solid.Sides[0].Material == "TOOLS/TOOLSFOG" ||
                        solid.Sides[0].Material == "TOOLS/TOOLSSKYBOX" ||
                        solid.Sides[0].Material == "TOOLS/TOOLS2DSKYBOX" ||
                        solid.Sides[0].Material == "TOOLS/TOOLSSKYFOG" ||
                        solid.Sides[0].Material == "TOOLS/TOOLSFOGVOLUME"
                        ))
                        continue;

                    // build a very large cube brush.
                    var go = model.CreateBrush(PrimitiveBrushType.Cube, Vector3.zero);
                    var pr = go.GetComponent<PrimitiveBrush>();
                    BrushUtility.Resize(pr, new Vector3(8192, 8192, 8192));

                    // clip all the sides out of the brush.
                    for (int j = solid.Sides.Count; j-- > 0;)
                    {
                        VmfSolidSide side = solid.Sides[j];
                        Plane clip = new Plane(pr.transform.InverseTransformPoint(new Vector3(side.Plane.P1.X, side.Plane.P1.Z, side.Plane.P1.Y) / scale), pr.transform.InverseTransformPoint(new Vector3(side.Plane.P2.X, side.Plane.P2.Z, side.Plane.P2.Y) / scale), pr.transform.InverseTransformPoint(new Vector3(side.Plane.P3.X, side.Plane.P3.Z, side.Plane.P3.Y) / scale));
                        ClipUtility.ApplyClipPlane(pr, clip, false);

                        // find the polygons associated with the clipping plane.
                        // the normal is unique and can never occur twice as that wouldn't allow the solid to be convex.
                        var polygons = pr.GetPolygons().Where(p => p.Plane.normal == clip.normal);
                        foreach (var polygon in polygons)
                        {
                            // detect excluded polygons.
                            if (side.Material == "TOOLS/TOOLSNODRAW")
                                polygon.UserExcludeFromFinal = true;
                            // detect collision-only brushes.
                            if (side.Material == "TOOLS/TOOLSCLIP" ||
                                side.Material == "TOOLS/TOOLSNPCCLIP" ||
                                side.Material == "TOOLS/TOOLSPLAYERCLIP" ||
                                side.Material == "TOOLS/TOOLSGRENDADECLIP" ||
                                side.Material == "TOOLS/TOOLSSTAIRS")
                                pr.IsVisible = false;
                            // try finding the material in the project.
                            polygon.Material = FindMaterial(side.Material);
                        }
                    }
                }

#if UNITY_EDITOR
                UnityEditor.EditorUtility.ClearProgressBar();
#endif
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                model.EndUpdate();
            }
        }

        /// <summary>
        /// Attempts to find a material in the project by name.
        /// </summary>
        /// <param name="name">The material name to search for.</param>
        /// <returns>The material if found or null.</returns>
        private static Material FindMaterial(string name)
        {
#if UNITY_EDITOR
            // first try finding the fully qualified texture name with '/' replaced by '.' so 'BRICK.BRICKWALL052D'.
            name = name.Replace("/", ".");
            string texture = "";
            string guid = UnityEditor.AssetDatabase.FindAssets("t:Material " + name).FirstOrDefault();
            if (guid == null)
            {
                // if it couldn't be found try a simplified name like 'BRICKWALL052D'.
                texture = name;
                if (name.Contains('.'))
                    texture = name.Substring(name.LastIndexOf('.') + 1);
                guid = UnityEditor.AssetDatabase.FindAssets("t:Material " + texture).FirstOrDefault();
            }
            // if a material could be found using either option:
            if (guid != null)
            {
                // load the material.
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                return UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
            }
            else { Debug.Log("SabreCSG: Tried to find material '" + name + "' and also as '" + texture + "' but it couldn't be found in the project."); }
#endif
            return null;
        }
    }
}

#endif