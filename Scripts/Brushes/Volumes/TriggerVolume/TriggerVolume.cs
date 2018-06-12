#if UNITY_EDITOR || RUNTIME_CSG

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Sabresaurus.SabreCSG.Volumes
{
    /// <summary>
    /// Executes trigger logic when objects interact with the volume.
    /// </summary>
    /// <seealso cref="Sabresaurus.SabreCSG.Volume"/>
    [Serializable]
    public class TriggerVolume : Volume
    {
        [SerializeField]
        public TriggerVolumeEventType volumeEventType = TriggerVolumeEventType.UnityEvent;

        [SerializeField]
        public TriggerVolumeTriggerMode triggerMode = TriggerVolumeTriggerMode.Enter;

        [SerializeField]
        public string filterTag = "Untagged";

        [SerializeField]
        public LayerMask layerMask = 0;

        [SerializeField]
        public bool triggerOnce = false;

        [SerializeField]
        public TriggerVolumeEvent onEnterEvent;

        [SerializeField]
        public TriggerVolumeEvent onStayEvent;

        [SerializeField]
        public TriggerVolumeEvent onExitEvent;

        [SerializeField]
        public List<TriggerVolumeSendMessageEvent> smOnEnterEvent;

        [SerializeField]
        public List<TriggerVolumeSendMessageEvent> smOnStayEvent;

        [SerializeField]
        public List<TriggerVolumeSendMessageEvent> smOnExitEvent;

#if UNITY_EDITOR

        /// <summary>
        /// Called when the inspector GUI is drawn in the editor.
        /// </summary>
        /// <param name="selectedVolumes">The selected volumes in the editor (for multi-editing).</param>
        /// <returns>True if a property changed or else false.</returns>
        public override bool OnInspectorGUI(Volume[] selectedVolumes)
        {
            var triggerVolumes = selectedVolumes.Cast<TriggerVolume>();
            bool invalidate = false;

            GUILayout.BeginVertical("Box");
            {
                UnityEditor.EditorGUILayout.LabelField("Trigger Options", UnityEditor.EditorStyles.boldLabel);
                GUILayout.Space(4);

                UnityEditor.EditorGUI.indentLevel = 1;

                GUILayout.BeginVertical();
                {
                    TriggerVolumeEventType previousVolumeEventType;
                    volumeEventType = (TriggerVolumeEventType)UnityEditor.EditorGUILayout.EnumPopup(new GUIContent("Trigger Event Type"), previousVolumeEventType = volumeEventType);
                    if (volumeEventType != previousVolumeEventType)
                    {
                        foreach (TriggerVolume volume in triggerVolumes)
                            volume.volumeEventType = volumeEventType;
                        invalidate = true;
                    }

                    TriggerVolumeTriggerMode previousTriggerMode;
                    triggerMode = (TriggerVolumeTriggerMode)UnityEditor.EditorGUILayout.EnumPopup(new GUIContent("Trigger Mode", "What kind of trigger events do we want to use?"), previousTriggerMode = triggerMode);
                    if (triggerMode != previousTriggerMode)
                    {
                        foreach (TriggerVolume volume in triggerVolumes)
                            volume.triggerMode = triggerMode;
                        invalidate = true;
                    }

                    LayerMask previousLayerMask;
                    layerMask = UnityEditor.EditorGUILayout.LayerField(new GUIContent("Layer", "The layer that is detected by this trigger."), previousLayerMask = layerMask);
                    if (layerMask != previousLayerMask)
                    {
                        foreach (TriggerVolume volume in triggerVolumes)
                            volume.layerMask = layerMask;
                        invalidate = true;
                    }

                    string previousFilterTag;
                    filterTag = UnityEditor.EditorGUILayout.TagField(new GUIContent("Tag", "The tag that is detected by this trigger."), previousFilterTag = filterTag);
                    if (filterTag != previousFilterTag)
                    {
                        foreach (TriggerVolume volume in triggerVolumes)
                            volume.filterTag = filterTag;
                        invalidate = true;
                    }

                    bool previousTriggerOnce;
                    triggerOnce = UnityEditor.EditorGUILayout.Toggle(new GUIContent("Trigger Once", "Is this a one use only trigger?"), previousTriggerOnce = triggerOnce);
                    if (triggerOnce != previousTriggerOnce)
                    {
                        foreach (TriggerVolume volume in triggerVolumes)
                            volume.triggerOnce = triggerOnce;
                        invalidate = true;
                    }
                }
                GUILayout.EndVertical();

                UnityEditor.EditorGUI.indentLevel = 0;
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical("Box");
            {
                UnityEditor.EditorGUILayout.LabelField("Trigger Events", UnityEditor.EditorStyles.boldLabel);
                GUILayout.Space(4);

                if (volumeEventType == TriggerVolumeEventType.UnityEvent)
                {
                    UnityEditor.EditorGUI.indentLevel = 1;

                    GUILayout.BeginVertical();
                    {
                        UnityEditor.SerializedObject tv = new UnityEditor.SerializedObject(this);
                        UnityEditor.SerializedProperty prop1 = tv.FindProperty("onEnterEvent");
                        UnityEditor.SerializedProperty prop2 = tv.FindProperty("onStayEvent");
                        UnityEditor.SerializedProperty prop3 = tv.FindProperty("onExitEvent");

                        UnityEditor.EditorGUI.BeginChangeCheck();

                        if (triggerMode == TriggerVolumeTriggerMode.Enter ||
                            triggerMode == TriggerVolumeTriggerMode.EnterExit ||
                            triggerMode == TriggerVolumeTriggerMode.EnterStay ||
                            triggerMode == TriggerVolumeTriggerMode.All)
                        {
                            UnityEditor.EditorGUILayout.PropertyField(prop1);
                        }

                        if (triggerMode == TriggerVolumeTriggerMode.Stay ||
                            triggerMode == TriggerVolumeTriggerMode.EnterStay ||
                            triggerMode == TriggerVolumeTriggerMode.All)
                        {
                            UnityEditor.EditorGUILayout.PropertyField(prop2);
                        }

                        if (triggerMode == TriggerVolumeTriggerMode.Exit ||
                            triggerMode == TriggerVolumeTriggerMode.EnterExit ||
                            triggerMode == TriggerVolumeTriggerMode.All)
                        {
                            UnityEditor.EditorGUILayout.PropertyField(prop3);
                        }

                        if (UnityEditor.EditorGUI.EndChangeCheck())
                        {
                            tv.ApplyModifiedProperties();
                            foreach (TriggerVolume volume in triggerVolumes)
                            {
                                volume.onEnterEvent = onEnterEvent;
                                volume.onStayEvent = onStayEvent;
                                volume.onExitEvent = onExitEvent;
                            }
                            invalidate = true;
                        }
                    }
                    GUILayout.EndVertical();

                    UnityEditor.EditorGUI.indentLevel = 0;
                }
                else
                {
                    UnityEditor.EditorGUI.BeginChangeCheck();

                    if (triggerMode == TriggerVolumeTriggerMode.Enter ||
                        triggerMode == TriggerVolumeTriggerMode.EnterExit ||
                        triggerMode == TriggerVolumeTriggerMode.EnterStay ||
                        triggerMode == TriggerVolumeTriggerMode.All)
                    {
                        smOnEnterEvent = TriggerVolumeUIUtils.DrawSendMessageEventInspector(new GUIContent("On Enter Message ()"), smOnEnterEvent);
                    }

                    if (triggerMode == TriggerVolumeTriggerMode.Stay ||
                        triggerMode == TriggerVolumeTriggerMode.EnterStay ||
                        triggerMode == TriggerVolumeTriggerMode.All)
                    {
                        smOnStayEvent = TriggerVolumeUIUtils.DrawSendMessageEventInspector(new GUIContent("On Stay Message ()"), smOnStayEvent);
                    }

                    if (triggerMode == TriggerVolumeTriggerMode.Exit ||
                        triggerMode == TriggerVolumeTriggerMode.EnterExit ||
                        triggerMode == TriggerVolumeTriggerMode.All)
                    {
                        smOnExitEvent = TriggerVolumeUIUtils.DrawSendMessageEventInspector(new GUIContent("On Exit Message ()"), smOnExitEvent);
                    }

                    if (UnityEditor.EditorGUI.EndChangeCheck())
                    {
                        foreach (TriggerVolume volume in triggerVolumes)
                        {
                            volume.smOnEnterEvent = smOnEnterEvent;
                            volume.smOnStayEvent = smOnStayEvent;
                            volume.smOnExitEvent = smOnExitEvent;
                        }
                        invalidate = true;
                    }
                }
            }
            GUILayout.EndVertical();

            return invalidate;
        }

#endif

        public override void OnCreateVolume(GameObject volume)
        {
            TriggerVolumeComponent tvc = volume.AddComponent<TriggerVolumeComponent>();
            tvc.volumeEventType = volumeEventType;
            tvc.triggerMode = triggerMode;
            tvc.filterTag = filterTag;
            tvc.layerMask = layerMask;
            tvc.triggerOnce = triggerOnce;
            tvc.onEnterEvent = onEnterEvent;
            tvc.onStayEvent = onStayEvent;
            tvc.onExitEvent = onExitEvent;
            tvc.smOnEnterEvent = smOnEnterEvent;
            tvc.smOnStayEvent = smOnStayEvent;
            tvc.smOnExitEvent = smOnExitEvent;
        }
    }
}

#endif