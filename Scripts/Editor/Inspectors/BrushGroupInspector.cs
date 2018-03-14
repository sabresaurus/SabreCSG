using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(BrushGroup), true)]
    public class BrushGroupInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            using (new NamedVerticalScope("Group"))
            {
                BrushGroup group = (BrushGroup)target;

                GUILayout.BeginHorizontal();

                if (GUILayout.Button("Select Brushes"))
                {
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
            }
        }
    }
}