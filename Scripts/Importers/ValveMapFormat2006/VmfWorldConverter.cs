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

                    // build a very large cube brush.
                    var go = model.CreateBrush(PrimitiveBrushType.Cube, Vector3.zero);
                    var pr = go.GetComponent<PrimitiveBrush>();
                    BrushUtility.Resize(pr, new Vector3(8192, 8192, 8192));

                    // clip all the sides out of the brush.
                    for (int j = solid.Sides.Count; j-- > 0;)
                    {
                        VmfSolidSide side = solid.Sides[j];
                        ClipUtility.ApplyClipPlane(pr, new Plane(pr.transform.InverseTransformPoint(new Vector3(side.Plane.P1.X, side.Plane.P1.Z, side.Plane.P1.Y) / scale), pr.transform.InverseTransformPoint(new Vector3(side.Plane.P2.X, side.Plane.P2.Z, side.Plane.P2.Y) / scale), pr.transform.InverseTransformPoint(new Vector3(side.Plane.P3.X, side.Plane.P3.Z, side.Plane.P3.Y) / scale)), false);
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
    }
}

#endif