#if UNITY_EDITOR || RUNTIME_CSG

using System;
using System.Linq;
using UnityEngine;

namespace Sabresaurus.SabreCSG.Importers.Quake1
{
    /// <summary>
    /// Converts a Quake 1 Map to SabreCSG Brushes.
    /// </summary>
    public static class MapWorldConverter
    {
        private static int s_Scale = 32;

        /// <summary>
        /// Imports the specified world into the SabreCSG model.
        /// </summary>
        /// <param name="model">The model to import into.</param>
        /// <param name="world">The world to be imported.</param>
        /// <param name="scale">The scale modifier.</param>
        public static void Import(CSGModelBase model, MapWorld world)
        {
            try
            {
                model.BeginUpdate();

                // create a material searcher to associate materials automatically.
                MaterialSearcher materialSearcher = new MaterialSearcher();

                // group all the brushes together.
                GroupBrush groupBrush = new GameObject("Quake 1 Map").AddComponent<GroupBrush>();
                groupBrush.transform.SetParent(model.transform);

                // iterate through all entities.
                for (int e = 0; e < world.Entities.Count; e++)
                {
#if UNITY_EDITOR
                    UnityEditor.EditorUtility.DisplayProgressBar("SabreCSG: Importing Quake 1 Map", "Converting Quake 1 Entities To SabreCSG Brushes (" + (e + 1) + " / " + world.Entities.Count + ")...", e / (float)world.Entities.Count);
#endif
                    MapEntity entity = world.Entities[e];

                    // iterate through all entity solids.
                    for (int i = 0; i < entity.Brushes.Count; i++)
                    {
                        MapBrush brush = entity.Brushes[i];
#if UNITY_EDITOR
                        if (world.Entities[e].ClassName == "worldspawn")
                            UnityEditor.EditorUtility.DisplayProgressBar("SabreCSG: Importing Quake 1 Map", "Converting Quake 1 Brushes To SabreCSG Brushes (" + (i + 1) + " / " + entity.Brushes.Count + ")...", i / (float)entity.Brushes.Count);
#endif
                        // don't add triggers to the scene.
                        if (brush.Sides.Count > 0 && IsSpecialMaterial(brush.Sides[0].Material))
                            continue;

                        // build a very large cube brush.
                        var go = model.CreateBrush(PrimitiveBrushType.Cube, Vector3.zero);
                        var pr = go.GetComponent<PrimitiveBrush>();
                        BrushUtility.Resize(pr, new Vector3(8192, 8192, 8192));

                        // clip all the sides out of the brush.
                        for (int j = brush.Sides.Count; j-- > 0;)
                        {
                            MapBrushSide side = brush.Sides[j];
                            Plane clip = new Plane(pr.transform.InverseTransformPoint(new Vector3(side.Plane.P1.X, side.Plane.P1.Z, side.Plane.P1.Y) / (float)s_Scale), pr.transform.InverseTransformPoint(new Vector3(side.Plane.P2.X, side.Plane.P2.Z, side.Plane.P2.Y) / (float)s_Scale), pr.transform.InverseTransformPoint(new Vector3(side.Plane.P3.X, side.Plane.P3.Z, side.Plane.P3.Y) / (float)s_Scale));
                            ClipUtility.ApplyClipPlane(pr, clip, false);

                            // find the polygons associated with the clipping plane.
                            // the normal is unique and can never occur twice as that wouldn't allow the solid to be convex.
                            var polygons = pr.GetPolygons().Where(p => p.Plane.normal.EqualsWithEpsilonLower3(clip.normal));
                            foreach (var polygon in polygons)
                            {
                                // detect excluded polygons.
                                if (IsExcludedMaterial(side.Material))
                                    polygon.UserExcludeFromFinal = true;
                                // detect collision-only brushes.
                                if (IsInvisibleMaterial(side.Material))
                                    pr.IsVisible = false;
                                // find the material in the unity project automatically.
                                Material material;
                                // try finding the texture name with '*' replaced by '#' so '#teleport'.
                                string materialName = side.Material.Replace("*", "#");
                                material = materialSearcher.FindMaterial(new string[] { materialName });
                                if (material == null)
                                    Debug.Log("SabreCSG: Tried to find material '" + materialName + "' but it couldn't be found in the project.");
                                polygon.Material = material;
                                // calculate the texture coordinates.
                                int w = 256;
                                int h = 256;
                                if (polygon.Material != null && polygon.Material.mainTexture != null)
                                {
                                    w = polygon.Material.mainTexture.width;
                                    h = polygon.Material.mainTexture.height;
                                }
                                CalculateTextureCoordinates(pr, polygon, w, h, new Vector2(side.Offset.X, -side.Offset.Y), new Vector2(side.Scale.X, side.Scale.Y), side.Rotation);
                            }
                        }

                        // add the brush to the group.
                        pr.transform.SetParent(groupBrush.transform);
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

        // shoutouts to Jasmine Mickle for your insight and UV texture coordinates code.
        private static void CalculateTextureCoordinates(PrimitiveBrush pr, Polygon polygon, int textureWidth, int textureHeight, Vector2 offset, Vector2 scale, float rotation)
        {
            // feel free to improve this uv mapping code, it has some issues.
            // • 45 degree angled walls may not have correct UV texture coordinates (are not correctly picking the dominant axis because there are two).
            // • negative vertex coordinates may not have correct UV texture coordinates.

            // calculate texture coordinates.
            for (int i = 0; i < polygon.Vertices.Length; i++)
            {
                // we scaled down the level so scale up the math here.
                var vertex = (pr.transform.position + polygon.Vertices[i].Position) * s_Scale;

                Vector2 uv = new Vector2(0, 0);

                int dominantAxis = 0; // 0 == x, 1 == y, 2 == z

                // find the axis closest to the polygon's normal.
                float[] axes =
                {
                    Mathf.Abs(polygon.Plane.normal.x),
                    Mathf.Abs(polygon.Plane.normal.z),
                    Mathf.Abs(polygon.Plane.normal.y)
                };

                // defaults to use x-axis.
                dominantAxis = 0;
                // check whether the y-axis is more likely.
                if (axes[1] > axes[dominantAxis])
                    dominantAxis = 1;
                // check whether the z-axis is more likely.
                if (axes[2] >= axes[dominantAxis])
                    dominantAxis = 2;

                // x-axis:
                if (dominantAxis == 0)
                {
                    uv.x = vertex.z;
                    uv.y = vertex.y;
                }

                // y-axis:
                if (dominantAxis == 1)
                {
                    uv.x = vertex.x;
                    uv.y = vertex.y;
                }

                // z-axis:
                if (dominantAxis == 2)
                {
                    uv.x = vertex.x;
                    uv.y = vertex.z;
                }

                // rotate the texture coordinates.
                uv = uv.Rotate(-rotation);
                // scale the texture coordinates.
                uv = uv.Divide(scale);
                // move the texture coordinates.
                uv += offset;
                // finally divide the result by the texture size.
                uv = uv.Divide(new Vector2(textureWidth, textureHeight));

                polygon.Vertices[i].UV = uv;
            }
        }

        /// <summary>
        /// Determines whether the specified name is an excluded material.
        /// </summary>
        /// <param name="name">The name of the material.</param>
        /// <returns><c>true</c> if the specified name is an excluded material; otherwise, <c>false</c>.</returns>
        private static bool IsExcludedMaterial(string name)
        {
            if (name.StartsWith("sky"))
                return true;
            return false;
        }

        /// <summary>
        /// Determines whether the specified name is an invisible material.
        /// </summary>
        /// <param name="name">The name of the material.</param>
        /// <returns><c>true</c> if the specified name is an invisible material; otherwise, <c>false</c>.</returns>
        private static bool IsInvisibleMaterial(string name)
        {
            switch (name)
            {
                case "clip":
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Determines whether the specified name is a special material, these brush will not be
        /// imported into SabreCSG.
        /// </summary>
        /// <param name="name">The name of the material.</param>
        /// <returns><c>true</c> if the specified name is a special material; otherwise, <c>false</c>.</returns>
        private static bool IsSpecialMaterial(string name)
        {
            switch (name)
            {
                case "skip":
                case "waterskip":
                    return true;
            }
            return false;
        }
    }
}

#endif