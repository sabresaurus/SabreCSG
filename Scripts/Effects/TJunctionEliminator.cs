using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
    [RequireComponent(typeof(Camera))]
    public class TJunctionEliminator : MonoBehaviour
    {
        private Material m_Material;
        private Camera m_Camera;
        public bool m_DebugMode = false;

        [Header("MSAA Settings")]
        public float m_ScreenDetectionAggressiveness = 0.03f;

        private void Start()
        {
            m_Material = new Material(Shader.Find("SabreCSG/TJunctionEliminator"));
        }

        private void Update()
        {
            // make sure we have a reference to the camera.
            if (!m_Camera)
                m_Camera = GetComponent<Camera>();

            // upload properties of interest to the shader.
            m_Material.SetInt("_MSAA", m_Camera.allowMSAA ? 1 : 0);
            m_Material.SetInt("_DebugMode", m_DebugMode ? 1 : 0);
            if (m_Camera.allowMSAA)
                m_Material.SetFloat("_ScreenDetectionAggressiveness", m_ScreenDetectionAggressiveness);
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            //material.SetFloat("_bwBlend", intensity);
            Graphics.Blit(source, destination, m_Material);
        }
    }
}