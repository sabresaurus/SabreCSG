#if UNITY_2021_2_OR_NEWER

using UnityEditor;
using UnityEditor.Overlays;

namespace Sabresaurus.SabreCSG
{
    [Overlay(typeof(SceneView), k_Id, "SabreCSG Toolbar")]
    public class SabreToolbarOverlay : IMGUIOverlay
    {
        private const string k_Id = "sabrecsg-toolbar-overlay";

        public override void OnCreated()
        {
            base.OnCreated();
        }

        public override void OnGUI()
        {
            var model = CSGModel.GetActiveCSGModel();
            if (model)
            {
                Toolbar.CSGModel = model;
                Toolbar.OnBottomToolbarGUI(0);

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
            }
        }
    }
}

#endif