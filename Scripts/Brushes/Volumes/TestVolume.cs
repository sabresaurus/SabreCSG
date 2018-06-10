using Sabresaurus.SabreCSG;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Henry
{
    [System.Serializable]
    public class TestVolume : Volume
    {
        public override void OnInspectorGUI()
        {
            UnityEditor.EditorGUILayout.LabelField("Test Volume");
        }
    }
}
