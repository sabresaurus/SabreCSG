#if UNITY_2021_2_OR_NEWER

using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
    [Overlay(typeof(SceneView), k_Id, "SabreCSG Toolbar")]
    public class SabreToolbarOverlay : IMGUIOverlay
    {
        private const string k_Id = "sabrecsg-toolbar-overlay";
        public static SabreToolbarOverlay Instance;

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

                if (Toolbar.primitiveMenuShowing)
                {
                    Toolbar.OnPrimitiveMenuGUI(0);
                }

                if (Toolbar.viewMenuShowing)
                {
                    Toolbar.OnViewMenuGUI(0);
                }

                if (!string.IsNullOrEmpty(Toolbar.WarningMessage))
                {
                    Toolbar.OnWarningToolbar(0);
                }

                Toolbar.OnBottomToolbarGUI(0);
            }
        }
    }
}

#endif