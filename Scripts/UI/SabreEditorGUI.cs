#if UNITY_EDITOR

using UnityEditor;

namespace Sabresaurus.SabreCSG
{
    public class SabreEditorGUI
    {
#if UNITY_5

        /// <summary>
        /// Scope for managing the indent level of the field labels.
        /// </summary>
        public class IndentLevelScope : System.IDisposable
        {
            public IndentLevelScope()
            {
                EditorGUI.indentLevel++;
            }

            public void Dispose()
            {
                EditorGUI.indentLevel--;
            }
        }

#else

        /// <summary>
        /// Scope for managing the indent level of the field labels.
        /// </summary>
        public class IndentLevelScope : EditorGUI.IndentLevelScope { }

#endif
    }
}

#endif