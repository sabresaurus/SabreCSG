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
    public static class T3dMapToSabreCSG
    {
        /// <summary>
        /// Imports the specified map into the SabreCSG model.
        /// </summary>
        /// <param name="model">The model to import into.</param>
        /// <param name="map">The map to be imported.</param>
        public static void Import(CSGModel model, T3dMap map)
        {
            List<T3dActor> brushes = map.Brushes;

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
                    Material material = FindMaterial(tpolygon.Texture);

                    Vertex[] vertices = new Vertex[tpolygon.Vertices.Count];
                    for (int j = 0; j < tpolygon.Vertices.Count; j++)
                    {
                        vertices[j] = new Vertex(ToVector3(tpolygon.Vertices[j]) / 64.0f, ToVector3(tpolygon.Normal), GenerateUV(tpolygon, j, material));
                    }

                    polygons[i] = new Polygon(vertices, material, false, false);
                }

                // position and rotate the brushes.
                Transform transform = model.CreateCustomBrush(polygons).transform;
                transform.position = (ToVector3(tactor.Location) / 64.0f) - (ToVector3(tactor.PrePivot) / 64.0f);
                Vector3 axis;
                float angle;
                T3dRotatorToQuaternion(tactor.Rotation).ToAngleAxis(out angle, out axis);
                transform.RotateAround(transform.position + (ToVector3(tactor.PrePivot) / 64.0f), axis, angle);

                PrimitiveBrush brush = transform.GetComponent<PrimitiveBrush>();
                object value;
                // detect the brush mode (additive, subtractive).
                if (tactor.Properties.TryGetValue("CsgOper", out value))
                    brush.Mode = (string)value == "CSG_Add" ? CSGMode.Add : CSGMode.Subtract;
                // detect special brush flags.
                if (tactor.Properties.TryGetValue("PolyFlags", out value))
                {
                    T3dPolyFlags flags = (T3dPolyFlags)value;
                    if ((flags & T3dPolyFlags.Invisible) > 0)
                        brush.IsVisible = false;
                    if ((flags & T3dPolyFlags.NonSolid) > 0)
                        brush.HasCollision = false;
                    if ((flags & T3dPolyFlags.SemiSolid) > 0)
                        brush.IsNoCSG = true;
                }
                // detect single polygons.
                if (polygons.Length == 1)
                    brush.IsNoCSG = true;
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.ClearProgressBar();
#endif
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

        /// <summary>
        /// Attempts to find a material in the project by name.
        /// </summary>
        /// <param name="name">The material name to search for.</param>
        /// <returns>The material if found or null.</returns>
        private static Material FindMaterial(string name)
        {
#if UNITY_EDITOR
            // first try finding the fully qualified texture name like 'PlayrShp.Ceiling.Hullwk'.
            string texture = "";
            string guid = UnityEditor.AssetDatabase.FindAssets("t:Material " + name).FirstOrDefault();
            if (guid == null)
            {
                // if it couldn't be found try a simplified name which UnrealEd typically exports like 'Hullwk'.
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