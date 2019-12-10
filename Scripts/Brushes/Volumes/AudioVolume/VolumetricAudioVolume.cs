#if UNITY_EDITOR && UNITY_5_6_OR_NEWER

using Sabresaurus.SabreCSG;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEngine.Audio;
using System.Reflection;

namespace Sabresaurus.SabreCSG.Volumes
{
    /// <summary>
    /// The Volumetric Audio for SabreCSG Volume.
    /// </summary>
    /// <remarks>
    /// Previously sold commercially in the Unity Asset Store and now Open Source.
	/// <para>
	/// Made by Henry de Jongh for SabreCSG. https://00laboratories.com/
	/// If you wish to say thanks a donation is more than welcome! :D
	/// </para>
    /// </remarks>
    [System.Serializable]
    [InitializeOnLoad]
    public class VolumetricAudioVolume : Volume
    {
        /// <summary>
        /// The audio clip played by the volumetric audio source.
        /// </summary>
        [SerializeField]
        public AudioClip audioClip;

        /// <summary>
        /// Set whether the sound should play through an Audio Mixer first or directly to the Audio Listener.
        /// </summary>
        [SerializeField]
        public AudioMixerGroup outputAudioMixerGroup;

        /// <summary>
        /// Bypass/ignore any applied effects on the volumetric audio source.
        /// </summary>
        [SerializeField]
        public bool bypassEffects;

        /// <summary>
        /// Bypass/ignore any applied effects from the Audio Listener.
        /// </summary>
        [SerializeField]
        public bool bypassListenerEffects;

        /// <summary>
        /// Bypass/ignore any reverb zones.
        /// </summary>
        [SerializeField]
        public bool bypassReverbZones;

        /// <summary>
        /// Sets the priority of the volumetric audio source. Note that a sound with a larger
        /// priority value will more likely be stolen by sounds with smaller priority values.
        /// </summary>
        [SerializeField]
        public int priority = 128;

        /// <summary>
        /// Sets the overall volume of the sound.
        /// </summary>
        [SerializeField]
        public float audioVolume = 1.0f;

        /// <summary>
        /// Sets the frequency of the sound. Use this to slow down or speed up the sound.
        /// </summary>
        [SerializeField]
        public float pitch = 1.0f;

        /// <summary>
        /// Sets how much of the signal this volumetric audio source is mixing into the global reverb
        /// associated with the zones. [0, 1] is the linear range (like sound volume) while [1, 1.1]
        /// lets you boost the reverb mix by 10 dB.
        /// </summary>
        [SerializeField]
        public float reverbZoneMix = 1.0f;

        /// <summary>
        /// Specifies how much the pitch is changed based on the relative velocity between the audio
        /// listener and the volumetric audio source.
        /// </summary>
        [SerializeField]
        public float dopplerLevel = 1.0f;

        /// <summary>
        /// Sets the spread of a 3D sound in speaker space (prevents having sound all in one ear sometimes).
        /// </summary>
        [SerializeField]
        public int spread = 0;

        /// <summary>
        /// The spatial 2D distance affects the distance from the volume before the volumetric sound fully transitions to 2D (inner radius).
        /// </summary>
        [SerializeField]
        public float spatialDistance2D = 0.0f;

        /// <summary>
        /// The spatial 3D distance affects the distance from the volume before the volumetric sound fully transitions to 3D (outer radius).
        /// </summary>
        [SerializeField]
        public float spatialDistance3D = 1.0f;

        /// <summary>
        /// Which type of volume rolloff curve to use for the volumetric audio source.
        /// </summary>
        [SerializeField]
        public AudioRolloffMode rolloffMode = AudioRolloffMode.Linear;

        /// <summary>
        /// The custom rolloff animation curve to use for the volumetric audio source.
        /// </summary>
        [SerializeField]
        public AnimationCurve customRolloff;

        /// <summary>
        /// The minimum distance where the volumetric audio source volume stays the loudest possible. Outside of this minimum distance it begins to attenuate.
        /// </summary>
        [SerializeField]
        public float minDistance = 2f;

        /// <summary>
        /// The maximum distance the volumetric audio source stops attenuating at.
        /// </summary>
        [SerializeField]
        public float maxDistance = 20f;

        /// <summary>
        /// The asset store version, for backwards compatibility in case we update these components in the future.
        /// </summary>
        [SerializeField]
        public int assetStoreVersion = 1;

        /// <summary>
        /// The unique identifier of this volumetric audio volume.
        /// </summary>
        [SerializeField]
        public string uniqueIdentifier = "";

        /// <summary>
        /// The parent identifier of this volumetric audio volume.
        /// </summary>
        [SerializeField]
        public string parentIdentifier = "";

        /// <summary>
        /// The volume preview material shown by SabreCSG.
        /// </summary>
        private static Material s_VolumePreviewMaterial;

        /// <summary>
        /// Gets the brush preview material shown in the editor.
        /// </summary>
        public override Material BrushPreviewMaterial
        {
            get
            {
                if (s_VolumePreviewMaterial == null)
                {
                    string[] results = AssetDatabase.FindAssets("t:Material scsg_volumetric_audio_volume");
                    if (results.Length == 0) return base.BrushPreviewMaterial;
                    s_VolumePreviewMaterial = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(results[0]));
                    if (s_VolumePreviewMaterial == null) return base.BrushPreviewMaterial;
                }
                return s_VolumePreviewMaterial;
            }
        }

        /// <summary>
        /// Called when the inspector GUI is drawn in the editor.
        /// </summary>
        /// <param name="selectedVolumes">The selected volumes in the editor (for multi-editing).</param>
        /// <returns>True if a property changed or else false.</returns>
        public override bool OnInspectorGUI(Volume[] selectedVolumes)
        {
            SerializedObject serializedVolume = new SerializedObject(this);
            var audioVolumes = selectedVolumes.Cast<VolumetricAudioVolume>();
            bool invalidate = false;

            // asset store insists we add undo/redo support.
            // sadly the way volumes were implemented it's a bit of a nearly impossible task...
            // but we can re-create the audio here and hope it covers most cases.
            if (Event.current.type == EventType.ValidateCommand && Event.current.commandName == "UndoRedoPerformed")
            {
                // we look at the current editor selection (which undo operations recover) to find brushes:
                BrushBase[] results;
                if ((results = Selection.GetFiltered<BrushBase>(SelectionMode.Unfiltered)).Any())
                {
                    // iterate through all the brushes that are currently selected:
                    for (int i = 0; i < results.Length; i++)
                    {
                        // if the brush has a volumetric audio volume then we have to act:
                        if (results[i].Volume && results[i].Volume.GetType() == typeof(VolumetricAudioVolume))
                        {
                            // this is where the hack comes in, find the hidden volume game object:
                            Transform volumeTransform = results[i].transform.Find(Constants.GameObjectVolumeComponentIdentifier);
                            if (volumeTransform)
                            {
                                // delete the audio source we create in "OnCreateVolume".
                                AudioSource audioSource = volumeTransform.GetComponentInChildren<AudioSource>();
                                if (audioSource) DestroyImmediate(audioSource.gameObject);
                                // delete the volumetric audio volume component we create in "OnCreateVolume".
                                DestroyImmediate(volumeTransform.GetComponent<VolumetricAudioVolumeComponent>());
                                // now call the sabrecsg method to pretend like we are a new volume, recreating our stuff:
                                ((VolumetricAudioVolume)results[i].Volume).OnCreateVolume(volumeTransform.gameObject);
                            }
                        }
                    }
                    return false;
                }
            }

            for (int i = 0; i < selectedVolumes.Length; i++)
            {
                Undo.RecordObject(selectedVolumes[i], "Volumetric Audio Settings");
                // find the volume brush in the scene.
                BrushBase volumeBrush = FindObjectsOfType<BrushBase>().Where(brush => brush.Volume == selectedVolumes[i]).FirstOrDefault();
                if (volumeBrush)
                {
                    VolumetricAudioVolumeComponent component = volumeBrush.GetComponentInChildren<VolumetricAudioVolumeComponent>();
                    if (component)
                        Undo.RecordObject(component, "Volumetric Audio Settings");
                }
            }

            GUILayout.BeginVertical("Box");
            {
                EditorGUILayout.LabelField("Volumetric Geometry Options", EditorStyles.boldLabel);
                GUILayout.Space(4);

                EditorGUI.indentLevel = 1;
                GUILayout.BeginVertical();
                {
                    if (!IsChildVolume)
                    {
                        // display instructions on how to link multiple volumetric audio volumes together.
                        EditorGUILayout.HelpBox("You can combine multiple volumetric audio volumes together so that they will share the same audio source. This is useful for complex shapes like pipelines that shouldn't play the same sound multiple times at every twist and turn. To begin, simply parent volumetric audio volumes that you wish to combine under this one in the hierarchy.", MessageType.Info);
                        GUILayout.Space(8);
                    }
                    else
                    {
                        // display notifications that this volumetric audio volume is now a child volume.
                        EditorGUILayout.HelpBox("This volumetric audio volume is a child and does not have its own audio source.", MessageType.Info);
                        GUILayout.Space(8);

                        // provide button to select the parent volume.
                        if (GUILayout.Button("Select Parent"))
                        {
                            // find this volume brush in the scene.
                            BrushBase thisBrush = FindObjectsOfType<BrushBase>().Where(brush => brush.Volume == this).FirstOrDefault();
                            if (thisBrush && thisBrush.transform.parent)
                                Selection.objects = new Object[] { thisBrush.transform.parent.gameObject };
                        }
                    }
                }

                GUILayout.EndVertical();
                EditorGUI.indentLevel = 0;
            }
            GUILayout.EndVertical();

            // child volumes do not have audio source options.
            if (!IsChildVolume)
            {
                GUILayout.BeginVertical("Box");
                {
                    EditorGUILayout.LabelField("Audio Source Options", EditorStyles.boldLabel);
                    GUILayout.Space(4);

                    EditorGUI.indentLevel = 1;
                    GUILayout.BeginVertical();
                    {
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.ObjectField(serializedVolume.FindProperty("audioClip"), new GUIContent("Audio Clip", "The audio clip played by the volumetric audio source."));
                        if (EditorGUI.EndChangeCheck())
                        {
                            serializedVolume.ApplyModifiedProperties();
                            foreach (VolumetricAudioVolume volume in audioVolumes)
                                volume.audioClip = audioClip;
                            invalidate = true;
                        }

                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.ObjectField(serializedVolume.FindProperty("outputAudioMixerGroup"), new GUIContent("Output Mixer Group", "Set whether the sound should play through an Audio Mixer first or directly to the Audio Listener."));
                        if (EditorGUI.EndChangeCheck())
                        {
                            serializedVolume.ApplyModifiedProperties();
                            foreach (VolumetricAudioVolume volume in audioVolumes)
                                volume.outputAudioMixerGroup = outputAudioMixerGroup;
                            invalidate = true;
                        }

                        bool previousBoolean;
                        bypassEffects = EditorGUILayout.Toggle(new GUIContent("Bypass Effects", "Bypass/ignore any applied effects on the volumetric audio source."), previousBoolean = bypassEffects);
                        if (bypassEffects != previousBoolean)
                        {
                            foreach (VolumetricAudioVolume volume in audioVolumes)
                                volume.bypassEffects = bypassEffects;
                            invalidate = true;
                        }

                        bypassListenerEffects = EditorGUILayout.Toggle(new GUIContent("Bypass Listener Effects", "Bypass/ignore any applied effects from the Audio Listener."), previousBoolean = bypassListenerEffects);
                        if (bypassListenerEffects != previousBoolean)
                        {
                            foreach (VolumetricAudioVolume volume in audioVolumes)
                                volume.bypassListenerEffects = bypassListenerEffects;
                            invalidate = true;
                        }

                        bypassReverbZones = EditorGUILayout.Toggle(new GUIContent("Bypass Reverb Zones", "Bypass/ignore any reverb zones."), previousBoolean = bypassReverbZones);
                        if (bypassReverbZones != previousBoolean)
                        {
                            foreach (VolumetricAudioVolume volume in audioVolumes)
                                volume.bypassReverbZones = bypassReverbZones;
                            invalidate = true;
                        }

                        int previousInteger;
                        priority = EditorGUILayout.IntSlider(new GUIContent("Priority", "Sets the priority of the volumetric audio source. Note that a sound with a larger priority value will more likely be stolen by sounds with smaller priority values."), previousInteger = priority, 0, 256);
                        if (priority != previousInteger)
                        {
                            foreach (VolumetricAudioVolume volume in audioVolumes)
                                volume.priority = priority;
                            invalidate = true;
                        }

                        float previousFloat;
                        audioVolume = EditorGUILayout.Slider(new GUIContent("Volume", "Sets the overall volume of the sound."), previousFloat = audioVolume, 0, 1);
                        if (audioVolume != previousFloat)
                        {
                            foreach (VolumetricAudioVolume volume in audioVolumes)
                                volume.audioVolume = audioVolume;
                            invalidate = true;
                        }

                        pitch = EditorGUILayout.Slider(new GUIContent("Pitch", "Sets the frequency of the sound. Use this to slow down or speed up the sound."), previousFloat = pitch, -3, 3);
                        if (pitch != previousFloat)
                        {
                            foreach (VolumetricAudioVolume volume in audioVolumes)
                                volume.pitch = pitch;
                            invalidate = true;
                        }

                        reverbZoneMix = EditorGUILayout.Slider(new GUIContent("Reverb Zone Mix", "Sets how much of the signal this volumetric audio source is mixing into the global reverb associated with the zones. [0, 1] is the linear range (like sound volume) while [1, 1.1] lets you boost the reverb mix by 10 dB."), previousFloat = reverbZoneMix, 0, 1.1f);
                        if (reverbZoneMix != previousFloat)
                        {
                            foreach (VolumetricAudioVolume volume in audioVolumes)
                                volume.reverbZoneMix = reverbZoneMix;
                            invalidate = true;
                        }

                        dopplerLevel = EditorGUILayout.Slider(new GUIContent("Doppler Level", "Specifies how much the pitch is changed based on the relative velocity between the audio listener and the volumetric audio source."), previousFloat = dopplerLevel, 0, 5);
                        if (dopplerLevel != previousFloat)
                        {
                            foreach (VolumetricAudioVolume volume in audioVolumes)
                                volume.dopplerLevel = dopplerLevel;
                            invalidate = true;
                        }

                        spread = EditorGUILayout.IntSlider(new GUIContent("Spread", "Sets the spread of a 3D sound in speaker space (prevents having sound all in one ear sometimes)."), previousInteger = spread, 0, 360);
                        if (spread != previousInteger)
                        {
                            foreach (VolumetricAudioVolume volume in audioVolumes)
                                volume.spread = spread;
                            invalidate = true;
                        }

                        AudioRolloffMode previousAudioRolloffMode;
                        rolloffMode = (AudioRolloffMode)EditorGUILayout.EnumPopup(new GUIContent("Volume Rolloff", "Which type of volume rolloff curve to use for the volumetric audio source."), previousAudioRolloffMode = rolloffMode);
                        if (rolloffMode != previousAudioRolloffMode)
                        {
                            foreach (VolumetricAudioVolume volume in audioVolumes)
                                volume.rolloffMode = rolloffMode;
                            invalidate = true;
                        }

                        if (rolloffMode == AudioRolloffMode.Custom)
                        {
                            EditorGUI.BeginChangeCheck();
                            customRolloff = EditorGUILayout.CurveField(new GUIContent("Custom Rolloff", "The custom rolloff animation curve to use for the volumetric audio source."), customRolloff);
                            if (EditorGUI.EndChangeCheck())
                            {
                                foreach (VolumetricAudioVolume volume in audioVolumes)
                                    volume.customRolloff = customRolloff;
                                invalidate = true;
                            }
                        }

                        minDistance = EditorGUILayout.FloatField(new GUIContent("Min Distance", "The minimum distance where the volumetric audio source volume stays the loudest possible. Outside of this minimum distance it begins to attenuate."), previousFloat = minDistance);
                        if (minDistance < 0) minDistance = 0;
                        if (minDistance != previousFloat)
                        {
                            foreach (VolumetricAudioVolume volume in audioVolumes)
                                volume.minDistance = minDistance;
                            invalidate = true;
                        }

                        maxDistance = EditorGUILayout.FloatField(new GUIContent("Max Distance", "The maximum distance the volumetric audio source stops attenuating at."), previousFloat = maxDistance);
                        if (maxDistance < minDistance) maxDistance = minDistance + 0.001f;
                        if (maxDistance != previousFloat)
                        {
                            foreach (VolumetricAudioVolume volume in audioVolumes)
                                volume.maxDistance = maxDistance;
                            invalidate = true;
                        }
                    }
                    GUILayout.EndVertical();
                    EditorGUI.indentLevel = 0;
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical("Box");
                {
                    EditorGUILayout.LabelField("Volumetric Audio Options", EditorStyles.boldLabel);
                    GUILayout.Space(4);

                    EditorGUI.indentLevel = 1;
                    GUILayout.BeginVertical();
                    {
                        float previousFloat;
                        spatialDistance2D = EditorGUILayout.FloatField(new GUIContent("Spatial 2D Distance", "The spatial 2D distance affects the distance from the volume before the volumetric sound fully transitions to 2D (inner radius)."), previousFloat = spatialDistance2D);
                        if (spatialDistance2D < 0) spatialDistance2D = 0;
                        if (spatialDistance2D != previousFloat)
                        {
                            foreach (VolumetricAudioVolume volume in audioVolumes)
                                volume.spatialDistance2D = spatialDistance2D;
                            invalidate = true;
                        }

                        spatialDistance3D = EditorGUILayout.FloatField(new GUIContent("Spatial 3D Distance", "The spatial 3D distance affects the distance from the volume before the volumetric sound fully transitions to 3D (outer radius)."), previousFloat = spatialDistance3D);
                        if (spatialDistance3D < spatialDistance2D) spatialDistance3D = spatialDistance2D;
                        if (spatialDistance3D != previousFloat)
                        {
                            foreach (VolumetricAudioVolume volume in audioVolumes)
                                volume.spatialDistance3D = spatialDistance3D;
                            invalidate = true;
                        }
                    }

                    GUILayout.EndVertical();
                    EditorGUI.indentLevel = 0;
                }
                GUILayout.EndVertical();
            }

            return invalidate;
        }

        /// <summary>
        /// Called when the volume is created in the editor.
        /// </summary>
        /// <param name="volume">The generated volume game object.</param>
        public override void OnCreateVolume(GameObject volume)
        {
            VolumetricAudioVolumeComponent component = volume.AddComponent<VolumetricAudioVolumeComponent>();

            // configure the volume:

            component.spatialDistance2D = spatialDistance2D;
            component.spatialDistance3D = spatialDistance3D;

            // if the user duplicated this brush the unique identifier may appear twice in the scene.
            // make sure we change our unique identifier in case this happens.
            if (FindObjectsOfType<BrushBase>().Any(brush => brush.Volume && brush.Volume != this && brush.Volume.GetType() == typeof(VolumetricAudioVolume) && ((VolumetricAudioVolume)brush.Volume).uniqueIdentifier == uniqueIdentifier))
                // generate a unique identifier for this volumetric audio volume.
                uniqueIdentifier = GUID.Generate().ToString();
            component.uniqueIdentifier = uniqueIdentifier;

            // check whether we have a parent volume and set or reset the parent identifier accordingly.
            parentIdentifier = "";
            // find this volume brush in the scene.
            BrushBase thisBrush = FindObjectsOfType<BrushBase>().Where(brush => brush.Volume == this).FirstOrDefault();
            if (thisBrush && thisBrush.transform.parent)
            {
                // find our parent brush (if there is one).
                BrushBase parentBrush = thisBrush.transform.parent.GetComponent<BrushBase>();
                if (parentBrush && parentBrush.Volume && parentBrush.Volume.GetType() == typeof(VolumetricAudioVolume))
                {
                    // we found a parent volumetric audio volume.
                    parentIdentifier = ((VolumetricAudioVolume)parentBrush.Volume).uniqueIdentifier;
                }
            }
            // store the parent identifier in the component.
            component.parentIdentifier = parentIdentifier;

            // we only create an audio source if we have a parent.
            if (!IsChildVolume)
            {
                // create a new game object to act as an audio source.
                GameObject audioSource = new GameObject("Audio Source");
                // parent it to the volume.
                audioSource.transform.SetParent(volume.transform, false);
                // add an audio source component.
                AudioSource audioSourceComponent = audioSource.AddComponent<AudioSource>();
                // hide the icon.
                RemoveEditorIcon(audioSource);

                // configure the audio source:

                // immediately start playing the sound.
                audioSourceComponent.playOnAwake = true;
                // always loop the sound.
                audioSourceComponent.loop = true;
                // enable full spatial blend (precaution against initial 2D sound on start).
                audioSourceComponent.spatialBlend = 1.0f;
                // set the user-defined volume properties.
                audioSourceComponent.clip = audioClip;
                audioSourceComponent.outputAudioMixerGroup = outputAudioMixerGroup;
                audioSourceComponent.bypassEffects = bypassEffects;
                audioSourceComponent.bypassListenerEffects = bypassListenerEffects;
                audioSourceComponent.bypassReverbZones = bypassReverbZones;
                audioSourceComponent.priority = priority;
                audioSourceComponent.volume = audioVolume;
                audioSourceComponent.pitch = pitch;
                audioSourceComponent.reverbZoneMix = reverbZoneMix;
                audioSourceComponent.dopplerLevel = dopplerLevel;
                audioSourceComponent.spread = spread;
                audioSourceComponent.rolloffMode = rolloffMode;
                audioSourceComponent.minDistance = minDistance;
                audioSourceComponent.maxDistance = maxDistance;
                if (rolloffMode == AudioRolloffMode.Custom)
                    audioSourceComponent.SetCustomCurve(AudioSourceCurveType.CustomRolloff, customRolloff);

                // editor preview tends to stop playing any audio sources that were previously here.
                // we force it to play this one at least unless they mute the game window.
                if (!Application.isPlaying && !EditorUtility.audioMasterMute)
                    audioSourceComponent.Play();
            }

            // update the collection of volumetric audio volume components in the scene.
            UpdateVolumetricAudioVolumeComponents();
        }

        /// <summary>
        /// The collection of volumetric audio volume components in the scene.
        /// </summary>
        private static List<VolumetricAudioVolumeComponent> s_VolumetricAudioVolumeComponents = new List<VolumetricAudioVolumeComponent>();

        /// <summary>
        /// Updates the collection of volumetric audio volume components in the scene.
        /// Extremely slow method. Do not call in the editor Scene GUI method!
        /// </summary>
        private static void UpdateVolumetricAudioVolumeComponents()
        {
            // clear the list of volumetric audio volume components.
            s_VolumetricAudioVolumeComponents.Clear();
            // iterate through all volumetric audio volume components in the scene:
            foreach (var audioVolumeComponent in FindObjectsOfType(typeof(VolumetricAudioVolumeComponent)))
                s_VolumetricAudioVolumeComponents.Add((VolumetricAudioVolumeComponent)audioVolumeComponent);
        }

        /// <summary>
        /// Initializes the <see cref="VolumetricAudioVolume"/> class in the editor.
        /// </summary>
        static VolumetricAudioVolume()
        {
            // hook into the ongui method of the editor.
#if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
#else
            if (!EditorHelper.SceneViewHasDelegate(OnSceneGUI))
                SceneView.onSceneGUIDelegate += OnSceneGUI;
#endif
        }

        /// <summary>
        /// Called when this script is enabled i.e. when a scene is loaded in the editor.
        /// </summary>
        private void OnEnable()
        {
            // generate a unique identifier for this volumetric audio volume.
            if (string.IsNullOrEmpty(uniqueIdentifier))
                uniqueIdentifier = GUID.Generate().ToString();

            // make sure that the custom rolloff isn't null.
            if (customRolloff == null)
                customRolloff = AnimationCurve.EaseInOut(0.0f, 1.0f, 1.0f, 0.0f);

            // initialize the collection of volumetric audio volume components in the scene.
            UpdateVolumetricAudioVolumeComponents();
        }

        /// <summary>
        /// Called by the editor every time it renders the GUI.
        /// </summary>
        /// <param name="sceneView">The scene view getting rendered.</param>
        private static void OnSceneGUI(SceneView sceneView)
        {
            // iterate through all cached volumetric audio volume components in the scene:
            foreach (VolumetricAudioVolumeComponent audioVolumeComponent in s_VolumetricAudioVolumeComponents)
                // call the editor preview method.
                if (audioVolumeComponent) audioVolumeComponent.OnEditorPreview();
        }

        /// <summary>
        /// Gets a value indicating whether this volume is a child volume.
        /// </summary>
        /// <value><c>true</c> if this volume is a child volume; otherwise, <c>false</c>.</value>
        private bool IsChildVolume { get { return !string.IsNullOrEmpty(parentIdentifier); } }

        /// <summary>
        /// If the AssignLabel method fails in the current unity version, don't try again.
        /// </summary>
        private static bool s_RemoveEditorIconFailure = false;

        /// <summary>
        /// Removes the editor icon (sets it to a 1x1 transparent pixel).
        /// </summary>
        /// <param name="g">The game object to be modified.</param>
        private void RemoveEditorIcon(GameObject g)
        {
            // if this method failed then don't try again.
            if (s_RemoveEditorIconFailure) return;

            // find the gizmo that is 1x1 transparent in order to hide the audio icon.
            string[] results = AssetDatabase.FindAssets("t:Texture2D VolumetricAudioGizmo");
            if (results.Length == 0) return;
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(results[0]));
            if (tex == null) return;

            // this could break at some point in the future (or past), so I use a try/catch just to be safe.
            try
            {
                BindingFlags bindingFlags = BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.NonPublic;
                typeof(EditorGUIUtility).InvokeMember("SetIconForObject", bindingFlags, null, null, new object[] { g, tex });
            }
            catch (System.Exception)
            {
                // don't try to call this method again.
                s_RemoveEditorIconFailure = true;
            }
        }
    }
}

#endif