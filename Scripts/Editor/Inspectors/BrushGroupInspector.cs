using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(GroupBrush), true)]
    public class BrushGroupInspector : BrushBaseInspector
    {
        SerializedProperty alwaysSelectGroup;

        protected override void OnEnable()
        {
            base.OnEnable();

            // Setup the SerializedProperties.
            alwaysSelectGroup = serializedObject.FindProperty("alwaysSelectGroup");
        }

        // disable the group editor.
        protected override bool ShowGroupInspector { get { return false; } }

        public override void DoInspectorGUI()
        {
            using (new NamedVerticalScope("Group"))
            {
                GroupBrush group = (GroupBrush)target;

                GUILayout.BeginHorizontal();

                if (GUILayout.Button("Select Brushes"))
                {
                    // disable the always select group flag.
                    group.AlwaysSelectGroup = false;

                    // select all of the child brush objects.
                    List<Object> objects = Selection.objects.ToList();
                    objects.Remove(group.gameObject);
                    foreach (Transform child in group.transform)
                        if (child.GetComponent<BrushBase>())
                            objects.Add(child.gameObject);
                    Selection.objects = objects.ToArray();
                }

                if (GUILayout.Button("Ungroup Brushes"))
                {
                    TransformHelper.UngroupSelection();
                }

                GUILayout.EndHorizontal();

                bool oldBool;
                alwaysSelectGroup.boolValue = GUILayout.Toggle(oldBool = alwaysSelectGroup.boolValue, "Always Select Group", EditorStyles.toolbarButton);
                if (alwaysSelectGroup.boolValue != oldBool)
                    serializedObject.ApplyModifiedProperties();
            }
        }
    }
}