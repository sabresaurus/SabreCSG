#if UNITY_EDITOR || RUNTIME_CSG

using System;
using System.Linq;
using UnityEngine;

namespace Sabresaurus.SabreCSG.Import.UnrealGold
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
            // iterate through all brush actors.
            foreach (T3dActor tactor in map.Brushes)
            {
                // get the underlying brush data.
                T3dBrush tbrush = tactor.Brush;

                // iterate through the brush polygons.
                Polygon[] polygons = new Polygon[tbrush.Polygons.Count];
                for (int i = 0; i < tbrush.Polygons.Count; i++)
                {
                    T3dPolygon tpolygon = tbrush.Polygons[i];

                    // find the material in the unity project automatically.
                    Material material = null;
#if UNITY_EDITOR
                    string texture = tpolygon.Texture;
                    if (tpolygon.Texture.Contains('.'))
                        texture = tpolygon.Texture.Substring(tpolygon.Texture.LastIndexOf('.') + 1);
                    string guid = UnityEditor.AssetDatabase.FindAssets("t:Material " + texture).FirstOrDefault();
                    if (guid != null)
                    {
                        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                        material = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
                    }
                    else { Debug.Log("Tried to find material '" + tpolygon.Texture + "' as '" + texture + "' but it couldn't be found in the project."); }
#endif

                    Vertex[] vertices = new Vertex[tpolygon.Vertices.Count];
                    for (int j = 0; j < tpolygon.Vertices.Count; j++)
                    {
                        vertices[j] = new Vertex(ToVector3(tpolygon.Vertices[j]) / 64.0f, ToVector3(tpolygon.Normal), GenerateUV(tpolygon, j, material));
                    }

                    polygons[i] = new Polygon(vertices, material, false, false);
                }

                Transform transform = model.CreateCustomBrush(polygons).transform;
                transform.position = ToVector3(tactor.Location) / 64.0f;
                //Vector3 axis;
                //float angle;
                //Quaternion.Euler((tactor.Rotation.Roll / 65535f) * 360.0f, (tactor.Rotation.Yaw / 65535f) * 360.0f, (tactor.Rotation.Pitch / 65535f) * 360.0f).ToAngleAxis(out angle, out axis);
                //transform.RotateAround(transform.TransformPoint(ToVector3(tactor.PrePivot) / 64.0f), axis, angle);
                transform.rotation = Quaternion.Euler((tactor.Rotation.Roll / 65535f) * 360.0f, (tactor.Rotation.Yaw / 65535f) * 360.0f, (tactor.Rotation.Pitch / 65535f) * 360.0f);

                PrimitiveBrush brush = transform.GetComponent<PrimitiveBrush>();
                object value;
                if (tactor.Properties.TryGetValue("CsgOper", out value))
                    brush.Mode = (string)value == "CSG_Add" ? CSGMode.Add : CSGMode.Subtract;
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
    }
}

#endif