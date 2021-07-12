#if UNITY_2021_2_OR_NEWER

using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
    [Overlay(typeof(SceneView), k_Id, "SabreCSG Tools")]
    public class SabreToolsOverlay : IMGUIOverlay
    {
        private const string k_Id = "sabrecsg-tools-overlay";
        public static SabreToolsOverlay Instance;

        public static System.Action window1;
        public static System.Action window2;

        public override void OnCreated()
        {
            base.OnCreated();

            Instance = this;
        }

        public override void OnGUI()
        {
            var model = CSGModel.GetActiveCSGModel();
            if (model)
            {
                Toolbar.CSGModel = model;
                Toolbar.OnTopToolbarGUI(0);

                window1?.Invoke();
                window2?.Invoke();
            }
        }
    }
}

#endif