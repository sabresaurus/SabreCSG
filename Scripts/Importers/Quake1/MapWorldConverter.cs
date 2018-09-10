#if UNITY_EDITOR || RUNTIME_CSG

using System;
using System.Collections.Generic;
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

                    // skip entities that sabrecsg can't handle.
                    switch (entity.ClassName)
                    {
                        case "func_areaportal":
                        case "func_areaportalwindow":
                        case "func_capturezone":
                        case "func_changeclass":
                        case "func_combine_ball_spawner":
                        case "func_dustcloud":
                        case "func_dustmotes":
                        case "func_nobuild":
                        case "func_nogrenades":
                        case "func_occluder":
                        case "func_precipitation":
                        case "func_proprespawnzone":
                        case "func_regenerate":
                        case "func_respawnroom":
                        case "func_smokevolume":
                        case "func_viscluster":
                            continue;
                    }

                    // iterate through all entity solids.
                    for (int i = 0; i < entity.Brushes.Count; i++)
                    {
                        MapBrush brush = entity.Brushes[i];

                        if (world.Entities[e].ClassName == "worldspawn")
                            UnityEditor.EditorUtility.DisplayProgressBar("SabreCSG: Importing Quake 1 Map", "Converting Quake 1 Brushes To SabreCSG Brushes (" + (i + 1) + " / " + entity.Brushes.Count + ")...", i / (float)entity.Brushes.Count);

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
                            var polygons = pr.GetPolygons().Where(p => p.Plane.normal == clip.normal);
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
                                // try finding the fully qualified texture name with '/' replaced by '.' so 'BRICK.BRICKWALL052D'.
                                string materialName = side.Material.Replace("/", ".");
                                // only try finding 'mmetal1_3'.
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
                                CalculateTextureCoordinates(pr, polygon, w, h, side.Plane, side.Offset, side.Scale, side.Rotation);
                            }
                        }

                        // detail brushes that do not affect the CSG world.
                        if (entity.ClassName == "func_detail")
                            pr.IsNoCSG = true;
                        // collision only brushes.
                        if (entity.ClassName == "func_vehicleclip")
                            pr.IsVisible = false;

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

        private static void CalculateTextureCoordinates(PrimitiveBrush pr, Polygon polygon, int textureWidth, int textureHeight, MapPlane plane, MapVector2 offset, MapVector2 scale, float rotation)
        {
            //UAxis.Translation = UAxis.Translation % textureWidth;
            //VAxis.Translation = VAxis.Translation % textureHeight;

            //if (UAxis.Translation < -textureWidth / 2f)
            //    UAxis.Translation += textureWidth;

            //if (VAxis.Translation < -textureHeight / 2f)
            //    VAxis.Translation += textureHeight;

            // calculate texture coordinates.
            for (int i = 0; i < polygon.Vertices.Length; i++)
            {
                var vertex = pr.transform.position + polygon.Vertices[i].Position;

                //Vector3 uaxis = new Vector3(UAxis.Vector.X, UAxis.Vector.Z, UAxis.Vector.Y);
                //Vector3 vaxis = new Vector3(VAxis.Vector.X, VAxis.Vector.Z, VAxis.Vector.Y);

                //var u = Vector3.Dot(vertex, uaxis) / (textureWidth * (UAxis.Scale * inchesInMeters)) + UAxis.Translation / textureWidth;
                //var v = Vector3.Dot(vertex, vaxis) / (textureHeight * (VAxis.Scale * inchesInMeters)) + VAxis.Translation / textureHeight;

                // return (computeTexCoords(point, attribs.scale()) + attribs.offset()) / attribs.textureSize();
                // return Vec2f(point.dot(safeScaleAxis(getXAxis(), scale.x())), point.dot(safeScaleAxis(getYAxis(), scale.y())));

                //var x = (computeTexCoords(polygon, vertex, new Vector2(scale.X, scale.Y)) + new Vector2(offset.X, offset.Y)).Divide(new Vector2(textureWidth, textureHeight));

                //float u = x.x;// * (float)32;
                //float v = x.y;// * (float)32;

                Vector2 vScale = new Vector2(scale.X, scale.Y) / s_Scale;
                Vector2 vOffset = new Vector2(offset.X, offset.Y);
                Vector2 vTexSize = new Vector2(textureWidth, textureHeight);

                ParaxialTexCoordSystem paraxialTexCoordSystem = new ParaxialTexCoordSystem(polygon.Plane.normal, vScale, vOffset, vTexSize, rotation);

                Vector2 uv = (computeTexCoords(paraxialTexCoordSystem, vertex, vScale) + vOffset).Divide(vTexSize);

                polygon.Vertices[i].UV.x = uv.x;
                polygon.Vertices[i].UV.y = -uv.y;
            }
        }

        private static Vector2 computeTexCoords(ParaxialTexCoordSystem paraxialTexCoordSystem, Vector3 point, Vector2 scale)
        {
            return new Vector2(Vector2.Dot(point, safeScaleAxis(paraxialTexCoordSystem.XAxis, scale.x)), Vector2.Dot(point, safeScaleAxis(paraxialTexCoordSystem.YAxis, scale.y)));
            //return new Vector2(Vector2.Dot(point, safeScaleAxis(getXAxis(), scale.x())), Vector2.Dot(point, safeScaleAxis(getYAxis(), scale.y())));
        }

        private static Vector2 safeScaleAxis(Vector2 axis, float factor)
        {
            return factor == 0.0f ? axis : axis / factor;
        }

        private class ParaxialTexCoordSystem
        {
            private static Vector3[] s_BaseAxes = {
                new Vector3( 0.0f,  0.0f,  1.0f), new Vector3( 1.0f,  0.0f,  0.0f), new Vector3( 0.0f, -1.0f,  0.0f),
                new Vector3( 0.0f,  0.0f, -1.0f), new Vector3( 1.0f,  0.0f,  0.0f), new Vector3( 0.0f, -1.0f,  0.0f),
                new Vector3( 1.0f,  0.0f,  0.0f), new Vector3( 0.0f,  1.0f,  0.0f), new Vector3( 0.0f,  0.0f, -1.0f),
                new Vector3(-1.0f,  0.0f,  0.0f), new Vector3( 0.0f,  1.0f,  0.0f), new Vector3( 0.0f,  0.0f, -1.0f),
                new Vector3( 0.0f,  1.0f,  0.0f), new Vector3( 1.0f,  0.0f,  0.0f), new Vector3( 0.0f,  0.0f, -1.0f),
                new Vector3( 0.0f, -1.0f,  0.0f), new Vector3( 1.0f,  0.0f,  0.0f), new Vector3( 0.0f,  0.0f, -1.0f),
            };

            private int m_index;
            private Vector2 m_Scale;
            private Vector2 m_Offset;
            private Vector2 m_TexSize;
            private float m_Rotation;

            private Vector3 m_xAxis;
            private Vector3 m_yAxis;

            public Vector3 XAxis { get { return m_xAxis; } }
            public Vector3 YAxis { get { return m_yAxis; } }

            public ParaxialTexCoordSystem(Vector3 normal, Vector2 scale, Vector2 offset, Vector2 texsize, float rotation)
            {
                m_Scale = scale;
                m_Offset = offset;
                m_TexSize = texsize;
                m_Rotation = rotation;

                SetRotation(normal, 0.0f, rotation);
            }

            public ParaxialTexCoordSystem(Vector3 point0, Vector3 point1, Vector3 point2, Vector2 scale, Vector2 offset, Vector2 texsize, float rotation)
            {
                m_index = 0;
                m_Scale = scale;
                m_Offset = offset;
                m_TexSize = texsize;
                m_Rotation = rotation;
                ResetCache(point0, point1, point2);
            }

            public void ResetCache(Vector3 point0, Vector3 point1, Vector3 point2)
            {
                Vector3 normal = Vector3.Cross(point2 - point0, point1 - point0).normalized;
                SetRotation(normal, 0.0f, m_Rotation);
            }

            public static int PlaneNormalIndex(Vector3 normal)
            {
                int bestIndex = 0;
                float bestDot = 0.0f;
                for (int i = 0; i < 6; ++i)
                {
                    float dot = Vector3.Dot(normal, s_BaseAxes[i * 3]);
                    if (dot > bestDot)
                    {
                        bestDot = dot;
                        bestIndex = i;
                    }
                }

                return bestIndex;
            }

            public void SetRotation(Vector3 normal, float oldAngle, float newAngle)
            {
                m_index = PlaneNormalIndex(normal);
                Vector3 projectionAxis;
                Axes(m_index, out m_xAxis, out m_yAxis, out projectionAxis);
                RotateAxes(ref m_xAxis, ref m_yAxis, newAngle, m_index);
            }

            public void Axes(int index, out Vector3 xAxis, out Vector3 yAxis, out Vector3 projectionAxis)
            {
                xAxis = s_BaseAxes[index * 3 + 1];
                yAxis = s_BaseAxes[index * 3 + 2];
                projectionAxis = s_BaseAxes[(index / 2) * 6];
            }

            public void RotateAxes(ref Vector3 xAxis, ref Vector3 yAxis, float angleInRadians, int planeNormIndex)
            {
                Vector3 rotAxis = Vector3.Cross(s_BaseAxes[planeNormIndex * 3 + 2], s_BaseAxes[planeNormIndex * 3 + 1]);
                Quaternion rot = Quaternion.AngleAxis(angleInRadians, rotAxis);
                xAxis = rot * xAxis;
                yAxis = rot * yAxis;

                //xAxis.correct(); ???????????????????
                //yAxis.correct(); ???????????????????
            }
        }

        /// <summary>
        /// Determines whether the specified name is an excluded material.
        /// </summary>
        /// <param name="name">The name of the material.</param>
        /// <returns><c>true</c> if the specified name is an excluded material; otherwise, <c>false</c>.</returns>
        private static bool IsExcludedMaterial(string name)
        {
            switch (name)
            {
                case "TOOLS/TOOLSNODRAW":
                    return true;
            }
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
                case "TOOLS/TOOLSCLIP":
                case "TOOLS/TOOLSNPCCLIP":
                case "TOOLS/TOOLSPLAYERCLIP":
                case "TOOLS/TOOLSGRENDADECLIP":
                case "TOOLS/TOOLSSTAIRS":
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
                case "TOOLS/TOOLSTRIGGER":
                case "TOOLS/TOOLSBLOCK_LOS":
                case "TOOLS/TOOLSBLOCKBULLETS":
                case "TOOLS/TOOLSBLOCKBULLETS2":
                case "TOOLS/TOOLSBLOCKSBULLETSFORCEFIELD": // did the wiki have a typo or is BLOCKS truly plural?
                case "TOOLS/TOOLSBLOCKLIGHT":
                case "TOOLS/TOOLSCLIMBVERSUS":
                case "TOOLS/TOOLSHINT":
                case "TOOLS/TOOLSINVISIBLE":
                case "TOOLS/TOOLSINVISIBLENONSOLID":
                case "TOOLS/TOOLSINVISIBLELADDER":
                case "TOOLS/TOOLSINVISMETAL":
                case "TOOLS/TOOLSNODRAWROOF":
                case "TOOLS/TOOLSNODRAWWOOD":
                case "TOOLS/TOOLSNODRAWPORTALABLE":
                case "TOOLS/TOOLSSKIP":
                case "TOOLS/TOOLSFOG":
                case "TOOLS/TOOLSSKYBOX":
                case "TOOLS/TOOLS2DSKYBOX":
                case "TOOLS/TOOLSSKYFOG":
                case "TOOLS/TOOLSFOGVOLUME":
                    return true;
            }
            return false;
        }
    }
}

#endif