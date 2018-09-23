#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Sabresaurus.SabreCSG
{
    public class NamedVerticalScope : GUILayout.VerticalScope
    {
        const int SPACING = 2;

        string name;

        public NamedVerticalScope(string name)
            : base(EditorStyles.helpBox)
        {
            this.name = name;
            GUILayout.Space(SabreGUILayout.NAME_TAG_HEIGHT + SPACING);
        }

        protected override void CloseScope()
        {
            base.CloseScope();

            Rect lastRect = GUILayoutUtility.GetLastRect();

            SabreGUILayout.DrawNameTag(lastRect.position, name);
        }
    }
}
#endif