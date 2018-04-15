using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
    public class TJunctionEliminator : MonoBehaviour
    {
        private Material material;

        // Use this for initialization
        private void Start()
        {
            material = new Material(Shader.Find("SabreCSG/TJunctionEliminator"));
        }

        // Postprocess the image
        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            //material.SetFloat("_bwBlend", intensity);
            Graphics.Blit(source, destination, material);
        }
    }
}