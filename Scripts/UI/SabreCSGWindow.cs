#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
    public class SabreCSGWindow : EditorWindow
    {
        private static SabreCSGWindow currentWindow;

        public static SabreCSGWindow CurrentWindow
        {
            get
            {
                return currentWindow;
            }
        }

        CSGModel csgModel;

        [MenuItem("Window/SabreCSG")]
        public static void CreateAndShow()
        {
            EditorWindow window = EditorWindow.GetWindow<SabreCSGWindow>("SabreCSG");

            window.Show();
        }

        void OnEnable()
        {
            currentWindow = this;
        }

        void OnGUI()
        {
            currentWindow = this;

            if(csgModel == null)
            {
                // Link to face tool has been lost, so attempt to reacquire
                CSGModel[] csgModels = FindObjectsOfType<CSGModel>();

                // Build the first csg model that is currently being edited
                for (int i = 0; i < csgModels.Length; i++) 
                {
                    if(csgModels[i].EditMode)
                    {
                        csgModel = csgModels[i];
                        break;
                    }
                }
            }

            if(csgModel != null)
            {
                Toolbar.OnTopToolbarGUI(-1);


                csgModel.ActiveTool.OnToolbarGUI(-1);
            }
            else
            {
                GUILayout.Label("No CSGModel active.");
            }
        }

    }
}
#endif