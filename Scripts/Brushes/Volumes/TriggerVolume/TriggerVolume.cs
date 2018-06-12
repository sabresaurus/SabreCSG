#if UNITY_EDITOR || RUNTIME_CSG

using System;
using System.Linq;
using UnityEngine;

namespace Sabresaurus.SabreCSG
{
    /// <summary>
    /// Executes trigger logic when objects interact with the volume.
    /// </summary>
    /// <seealso cref="Sabresaurus.SabreCSG.Volume"/>
	[Serializable]
	public class TriggerVolume : Volume
	{
		[SerializeField]
		public TriggerVolumeEventType volumeEventType = TriggerVolumeEventType.SendMessage;

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

            GUILayout.BeginVertical( "Box" );
			{
				UnityEditor.EditorGUILayout.LabelField( "Trigger Options", UnityEditor.EditorStyles.boldLabel );
				GUILayout.Space( 4 );

				UnityEditor.EditorGUI.indentLevel = 1;

				GUILayout.BeginVertical();
				{
                    TriggerVolumeEventType previousVolumeEventType;
                    volumeEventType = (TriggerVolumeEventType)UnityEditor.EditorGUILayout.EnumPopup( new GUIContent( "Trigger Event Type" ), previousVolumeEventType = volumeEventType);
                    if (volumeEventType != previousVolumeEventType)
                    {
                        foreach (TriggerVolume volume in triggerVolumes)
                            volume.volumeEventType = volumeEventType;
                        invalidate = true;
                    }

                    TriggerVolumeTriggerMode previousTriggerMode;
                    triggerMode = (TriggerVolumeTriggerMode)UnityEditor.EditorGUILayout.EnumPopup( new GUIContent( "Trigger Mode", "What kind of trigger events do we want to use?" ), previousTriggerMode = triggerMode );
                    if (triggerMode != previousTriggerMode)
                    {
                        foreach (TriggerVolume volume in triggerVolumes)
                            volume.triggerMode = triggerMode;
                        invalidate = true;
                    }

                    LayerMask previousLayerMask;
                    layerMask = UnityEditor.EditorGUILayout.LayerField( new GUIContent( "Layer", "The layer that is detected by this trigger." ), previousLayerMask = layerMask);
                    if (layerMask != previousLayerMask)
                    {
                        foreach (TriggerVolume volume in triggerVolumes)
                            volume.layerMask = layerMask;
                        invalidate = true;
                    }

                    string previousFilterTag;
					filterTag = UnityEditor.EditorGUILayout.TagField( new GUIContent( "Tag", "The tag that is detected by this trigger." ), previousFilterTag = filterTag);
                    if (filterTag != previousFilterTag)
                    {
                        foreach (TriggerVolume volume in triggerVolumes)
                            volume.filterTag = filterTag;
                        invalidate = true;
                    }

                    bool previousTriggerOnce;
					triggerOnce = UnityEditor.EditorGUILayout.Toggle( new GUIContent( "Trigger Once", "Is this a one use only trigger?" ), previousTriggerOnce = triggerOnce );
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

			GUILayout.BeginVertical( "Box" );
			{
				UnityEditor.EditorGUILayout.LabelField( "Trigger Events", UnityEditor.EditorStyles.boldLabel );
				GUILayout.Space( 4 );

				UnityEditor.EditorGUI.indentLevel = 1;

				GUILayout.BeginVertical();
				{
#if UNITY_2018
                    UnityEditor.SerializedObject tv = new UnityEditor.SerializedObject( this );
					UnityEditor.SerializedProperty prop1 = tv.FindProperty( "onEnterEvent" );
					UnityEditor.SerializedProperty prop2 = tv.FindProperty( "onStayEvent" );
					UnityEditor.SerializedProperty prop3 = tv.FindProperty( "onExitEvent" );

					UnityEditor.EditorGUI.BeginChangeCheck();

					UnityEditor.EditorGUILayout.PropertyField( prop1 );
					UnityEditor.EditorGUILayout.PropertyField( prop2 );
					UnityEditor.EditorGUILayout.PropertyField( prop3 );

					if( UnityEditor.EditorGUI.EndChangeCheck() )
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
#else
                    var e = UnityEditor.Editor.CreateEditor(this);
                    if (e != null)
                    {
                        UnityEditor.EditorGUI.BeginChangeCheck();
                        var so = e.serializedObject;
                        so.Update();
                        var prop = so.GetIterator();
                        prop.NextVisible(true);
                        while (prop.NextVisible(true))
                        {
                            string path = prop.propertyPath;
                            if (path == "onEnterEvent")
                            {
                                UnityEditor.EditorGUILayout.LabelField("On Enter Event", UnityEditor.EditorStyles.boldLabel);
                                UnityEditor.EditorGUILayout.HelpBox("This Unity version has a bug that does not save the value you enter in the event. Please make use of the fields below them instead. If you know why this happens and a way to fix it, please contact us!", UnityEditor.MessageType.Warning);
                            }
                            if (path == "onStayEvent")
                            {
                                UnityEditor.EditorGUILayout.Space();
                                UnityEditor.EditorGUILayout.LabelField("On Stay Event", UnityEditor.EditorStyles.boldLabel);
                                UnityEditor.EditorGUILayout.HelpBox("This Unity version has a bug that does not save the value you enter in the event. Please make use of the fields below them instead. If you know why this happens and a way to fix it, please contact us!", UnityEditor.MessageType.Warning);
                            }
                            if (path == "onExitEvent")
                            {
                                UnityEditor.EditorGUILayout.Space();
                                UnityEditor.EditorGUILayout.LabelField("On Exit Event", UnityEditor.EditorStyles.boldLabel);
                                UnityEditor.EditorGUILayout.HelpBox("This Unity version has a bug that does not save the value you enter in the event. Please make use of the fields below them instead. If you know why this happens and a way to fix it, please contact us!", UnityEditor.MessageType.Warning);
                            }
                            if (path != "onEnterEvent" && path != "onStayEvent" && path != "onExitEvent")
                            {
                                int idx = path.IndexOf(".data[");
                                if (idx == -1)
                                    continue;
                                int end = path.IndexOf(']', idx);
                                int num;
                                if (!Int32.TryParse(path.Substring(idx + 6, end - idx - 6), out num))
                                    continue;
                                if (!path.Contains("m_IntArgument") && !path.Contains("m_FloatArgument") && !path.Contains("m_StringArgument") && !path.Contains("m_BoolArgument"))
                                    continue;
                            }
                            UnityEditor.EditorGUILayout.PropertyField(prop, new GUIContent("Event " + prop.propertyPath.Replace("onEnterEvent", "").Replace("onStayEvent", "").Replace("onExitEvent", "").Replace(".m_PersistentCalls.m_Calls.Array.data[", "").Replace("].m_Arguments.m_", " ").Replace("Argument", " Argument")));
                        }

                        if (UnityEditor.EditorGUI.EndChangeCheck())
                        {
                            so.ApplyModifiedProperties();
                            foreach (TriggerVolume volume in triggerVolumes)
                            {
                                volume.onEnterEvent = onEnterEvent;
                                volume.onStayEvent = onStayEvent;
                                volume.onExitEvent = onExitEvent;
                            }
                            invalidate = true;
                        }
                    }
#endif
                }
				GUILayout.EndVertical();

				UnityEditor.EditorGUI.indentLevel = 0;
			}
			GUILayout.EndVertical();

			return invalidate;
		}
#endif

        public override void OnCreateVolume( GameObject volume )
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
		}
	}
}

#endif
