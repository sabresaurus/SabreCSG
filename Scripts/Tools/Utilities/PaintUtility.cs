#if UNITY_EDITOR || RUNTIME_CSG

using UnityEngine;
using UnityEditor;

namespace Sabresaurus.SabreCSG
{
    public static class PaintUtility
    {
        public static Color[] ExtractPalette(Object colorPresetLibrary)
        {
            SerializedObject obj = new SerializedObject(colorPresetLibrary);
            SerializedProperty presetsProperty = obj.FindProperty("m_Presets");
            Color[] colors = new Color[presetsProperty.arraySize];

            for (int i = 0; i < presetsProperty.arraySize; i++)
            {
                colors[i] = presetsProperty.GetArrayElementAtIndex(i).FindPropertyRelative("m_Color").colorValue;
            }

            return colors;
        }
    }
}
#endif