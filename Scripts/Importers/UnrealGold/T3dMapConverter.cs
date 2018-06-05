#if UNITY_EDITOR || RUNTIME_CSG

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Sabresaurus.SabreCSG.Importers.UnrealGold
{
    /// <summary>
    /// Converts an Unreal Editor 1 Map to SabreCSG Brushes.
    /// </summary>
    public static class T3dMapConverter
    {
        /// <summary>
        /// Imports the specified map into the SabreCSG model.
        /// </summary>
        /// <param name="model">The model to import into.</param>
        /// <param name="map">The map to be imported.</param>
        /// <param name="scale">The scale modifier.</param>
        public static void Import(CSGModelBase model, T3dMap map, int scale = 64)
        {
            try
            {
                model.BeginUpdate();

                // create a material searcher to associate materials automatically.
                MaterialSearcher materialSearcher = new MaterialSearcher();

                List<T3dActor> brushes = map.Brushes;
                Brush[] sabreBrushes = new Brush[brushes.Count];

                // iterate through all brush actors.
                for (int k = 0; k < brushes.Count; k++)
                {
                    // get the underlying brush data.
                    T3dActor tactor = brushes[k];
                    T3dBrush tbrush = tactor.Brush;
#if UNITY_EDITOR
                    UnityEditor.EditorUtility.DisplayProgressBar("SabreCSG: Importing Unreal Gold Map", "Converting Unreal Brushes To SabreCSG Brushes (" + (k + 1) + " / " + brushes.Count + ")...", k / (float)brushes.Count);
#endif
                    // iterate through the brush polygons.
                    Polygon[] polygons = new Polygon[tbrush.Polygons.Count];
                    for (int i = 0; i < tbrush.Polygons.Count; i++)
                    {
                        T3dPolygon tpolygon = tbrush.Polygons[i];

                        // find the material in the unity project automatically.
                        Material material;
                        if (tpolygon.Texture.Contains('.'))
                        {
                            // try finding both 'PlayrShp.Ceiling.Hullwk' and 'Hullwk'.
                            string tiny = tpolygon.Texture.Substring(tpolygon.Texture.LastIndexOf('.') + 1);
                            material = materialSearcher.FindMaterial(new string[] { tpolygon.Texture, tiny });
                            if (material == null)
                                Debug.Log("SabreCSG: Tried to find material '" + tpolygon.Texture + "' and also as '" + tiny + "' but it couldn't be found in the project.");
                        }
                        else
                        {
                            // only try finding 'Hullwk'.
                            material = materialSearcher.FindMaterial(new string[] { tpolygon.Texture });
                            if (material == null)
                                Debug.Log("SabreCSG: Tried to find material '" + tpolygon.Texture + "' but it couldn't be found in the project.");
                        }

                        Vertex[] vertices = new Vertex[tpolygon.Vertices.Count];
                        for (int j = 0; j < tpolygon.Vertices.Count; j++)
                        {
                            // main-scale
                            // scale around pivot point.
                            Vector3 vertexPosition = ToVector3(tpolygon.Vertices[j]);
                            Vector3 pivot = ToVector3(tactor.PrePivot);
                            Vector3 difference = vertexPosition - pivot;
                            vertexPosition = difference.Multiply(ToVector3Raw(tactor.MainScale)) + pivot;

                            // post-scale
                            vertices[j] = new Vertex(vertexPosition.Multiply(ToVector3Raw(tactor.PostScale)) / (float)scale, ToVector3(tpolygon.Normal), GenerateUV(tpolygon, j, material));
                        }

                        // detect the polygon flags.
                        bool userExcludeFromFinal = false;
                        if ((tpolygon.Flags & T3dPolygonFlags.Invisible) > 0)
                            userExcludeFromFinal = true;

                        polygons[i] = new Polygon(vertices, material, false, userExcludeFromFinal);
                    }

                    // position and rotate the brushes around their pivot point.
                    Transform transform = model.CreateCustomBrush(polygons).transform;
                    transform.position = (ToVector3(tactor.Location) / (float)scale) - (ToVector3(tactor.PrePivot) / (float)scale);
                    Vector3 axis;
                    float angle;
                    T3dRotatorToQuaternion(tactor.Rotation).ToAngleAxis(out angle, out axis);
                    transform.RotateAround(transform.position + (ToVector3(tactor.PrePivot) / (float)scale), axis, angle);

                    PrimitiveBrush brush = transform.GetComponent<PrimitiveBrush>();
                    sabreBrushes[k] = brush;

                    object value;
                    // detect the brush mode (additive, subtractive).
                    if (tactor.Properties.TryGetValue("CsgOper", out value))
                        brush.Mode = (string)value == "CSG_Add" ? CSGMode.Add : CSGMode.Subtract;
                    // detect special brush flags.
                    if (tactor.Properties.TryGetValue("PolyFlags", out value))
                    {
                        T3dBrushFlags flags = (T3dBrushFlags)value;
                        if ((flags & T3dBrushFlags.Invisible) > 0)
                            brush.IsVisible = false;
                        if ((flags & T3dBrushFlags.NonSolid) > 0)
                            brush.HasCollision = false;
                        if ((flags & T3dBrushFlags.SemiSolid) > 0)
                            brush.IsNoCSG = true;
                    }
                    // detect single polygons.
                    if (polygons.Length == 1)
                        brush.IsNoCSG = true;
                }

                // add all new brushes to a group.
                string title = "Unreal Gold Map";
                if (map.Title != "")
                    title += " '" + map.Title + "'";
                if (map.Author != "")
                    title += " (" + map.Author + ")";

                GroupBrush groupBrush = new GameObject(title).AddComponent<GroupBrush>();
                groupBrush.transform.SetParent(model.transform);
                for (int i = 0; i < sabreBrushes.Length; i++)
                    sabreBrushes[i].transform.SetParent(groupBrush.transform);

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
        /// Converts <see cref="T3dVector3"/> to <see cref="Vector3"/>.
        /// </summary>
        /// <param name="vector3">The <see cref="T3dVector3"/> to be converted.</param>
        /// <returns>The <see cref="Vector3"/>.</returns>
        private static Vector3 ToVector3(T3dVector3 vector3)
        {
            return new Vector3(-vector3.X, vector3.Z, vector3.Y);
        }

        /// <summary>
        /// Converts <see cref="T3dVector3"/> to <see cref="Vector3"/>.
        /// </summary>
        /// <param name="vector3">The <see cref="T3dVector3"/> to be converted.</param>
        /// <returns>The <see cref="Vector3"/>.</returns>
        private static Vector3 ToVector3Raw(T3dVector3 vector3)
        {
            return new Vector3(vector3.X, vector3.Z, vector3.Y);
        }

        private static float DotProduct(float ax, float ay, float az, float bx, float by, float bz)
        {
            return (ax * bx) + (ay * by) + (az * bz);
        }

        private static Vector2 GenerateUV(T3dPolygon polygon, int vindex, Material material)
        {
            float uu;
            float vv;
            uu = DotProduct(polygon.Vertices[vindex].X - polygon.Origin.X, polygon.Vertices[vindex].Y - polygon.Origin.Y, polygon.Vertices[vindex].Z - polygon.Origin.Z, polygon.TextureU.X, polygon.TextureU.Y, polygon.TextureU.Z);
            vv = DotProduct(polygon.Vertices[vindex].X - polygon.Origin.X, polygon.Vertices[vindex].Y - polygon.Origin.Y, polygon.Vertices[vindex].Z - polygon.Origin.Z, polygon.TextureV.X, polygon.TextureV.Y, polygon.TextureV.Z);
            if (material == null || material.mainTexture == null)
                return new Vector2(uu * (1.0f / 256f), 1.0f - (vv * (1.0f / 256f)));
            else
                return new Vector2((uu + polygon.PanU) * (1.0f / material.mainTexture.width), 1.0f - ((vv + polygon.PanV) * (1.0f / material.mainTexture.height)));
        }

        private static Quaternion T3dRotatorToQuaternion(T3dRotator rotator)
        {
            // 227 variant:
            float cosp = Mathf.Cos(rotator.Pitch * 0.0000479369f);
            float cosy = Mathf.Cos(rotator.Yaw * 0.0000479369f);
            float cosr = Mathf.Cos(rotator.Roll * 0.0000479369f);
            float sinp = Mathf.Sin(rotator.Pitch * 0.0000479369f);
            float siny = Mathf.Sin(rotator.Yaw * 0.0000479369f);
            float sinr = Mathf.Sin(rotator.Roll * 0.0000479369f);

            Quaternion quaternion;
            quaternion.w = cosp * cosy * cosr + sinp * siny * sinr;
            quaternion.z = sinp * cosy * cosr + cosp * siny * sinr;
            quaternion.y = cosp * siny * cosr - sinp * cosy * sinr;
            quaternion.x = cosp * cosy * sinr - sinp * siny * cosr;

            float L = Mathf.Sqrt(Mathf.Pow(quaternion.w, 2) + Mathf.Pow(quaternion.x, 2) + Mathf.Pow(quaternion.y, 2) + Mathf.Pow(quaternion.z, 2));
            quaternion.w /= L;
            quaternion.x /= L;
            quaternion.y /= L;
            quaternion.z /= L;

            quaternion.z = -quaternion.z;

            return quaternion;
        }
    }
}

#endif