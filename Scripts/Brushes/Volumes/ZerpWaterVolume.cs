using Sabresaurus.SabreCSG;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.SabreCSG.Scripts.Brushes.Volumes
{
    [System.Serializable]
    public class ZerpWaterVolume : Volume
    {
        public override void OnInspectorGUI()
        {
            UnityEditor.EditorGUILayout.LabelField("Zerp Water Volume");
        }
    }
}
