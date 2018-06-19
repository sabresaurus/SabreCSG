#if UNITY_EDITOR || RUNTIME_CSG

using System;
using System.Linq;
using UnityEngine;

namespace Sabresaurus.SabreCSG.Volumes
{
    /// <summary>
    /// Applies forces to rigid bodies inside of the volume.
    /// </summary>
    /// <seealso cref="Sabresaurus.SabreCSG.Volume"/>
    [Serializable]
    public class PhysicsVolume : Volume
    {
        /// <summary>
        /// The force mode applied to rigid bodies.
        /// </summary>
        [SerializeField]
        public PhysicsVolumeForceMode forceMode = PhysicsVolumeForceMode.None;

        /// <summary>
        /// The force space mode applied to rigid bodies.
        /// </summary>
        [SerializeField]
        public PhysicsVolumeForceSpace forceSpace = PhysicsVolumeForceSpace.Relative;

        /// <summary>
        /// The force applied to rigid bodies.
        /// </summary>
        [SerializeField]
        public Vector3 force = new Vector3(0.0f, 0.0f, 0.0f);

        /// <summary>
        /// The relative force mode applied to rigid bodies.
        /// </summary>
        [SerializeField]
        public PhysicsVolumeForceMode relativeForceMode = PhysicsVolumeForceMode.None;

        /// <summary>
        /// The relative force space mode applied to rigid bodies.
        /// </summary>
        [SerializeField]
        public PhysicsVolumeForceSpace relativeForceSpace = PhysicsVolumeForceSpace.Relative;

        /// <summary>
        /// The relative force applied to rigid bodies.
        /// </summary>
        [SerializeField]
        public Vector3 relativeForce = new Vector3(0.0f, 0.0f, 0.0f);

        /// <summary>
        /// The torque force mode applied to rigid bodies.
        /// </summary>
        [SerializeField]
        public PhysicsVolumeForceMode torqueForceMode = PhysicsVolumeForceMode.None;

        /// <summary>
        /// The torque force space mode applied to rigid bodies.
        /// </summary>
        [SerializeField]
        public PhysicsVolumeForceSpace torqueSpace = PhysicsVolumeForceSpace.Relative;

        /// <summary>
        /// The torque applied to rigid bodies.
        /// </summary>
        [SerializeField]
        public Vector3 torque = new Vector3(0.0f, 0.0f, 0.0f);

        /// <summary>
        /// The relative torque force mode applied to rigid bodies.
        /// </summary>
        [SerializeField]
        public PhysicsVolumeForceMode relativeTorqueForceMode = PhysicsVolumeForceMode.None;

        /// <summary>
        /// The relative torque force space mode applied to rigid bodies.
        /// </summary>
        [SerializeField]
        public PhysicsVolumeForceSpace relativeTorqueSpace = PhysicsVolumeForceSpace.Relative;

        /// <summary>
        /// The relative torque applied to rigid bodies.
        /// </summary>
        [SerializeField]
        public Vector3 relativeTorque = new Vector3(0.0f, 0.0f, 0.0f);

        /// <summary>
        /// The gravity settings applied to rigid bodies inside the volume.
        /// </summary>
        [SerializeField]
        public PhysicsVolumeGravityMode gravity = PhysicsVolumeGravityMode.None;

        /// <summary>
        /// The layer mask to limit the effects of the physics volume to specific layers.
        /// </summary>
        [SerializeField]
        public LayerMask layer = -1;

        /// <summary>
        /// Whether to use a filter tag.
        /// </summary>
        [SerializeField]
        public bool useFilterTag = false;

        /// <summary>
        /// The filter tag to limit the effects of the physics volume to specific tags.
        /// </summary>
        [SerializeField]
        public string filterTag = "Untagged";

#if UNITY_EDITOR

        /// <summary>
        /// Gets the brush preview material shown in the editor.
        /// </summary>
        public override Material BrushPreviewMaterial
        {
            get
            {
                return (Material)SabreCSGResources.LoadObject("Materials/scsg_volume_physics.mat");
            }
        }

        /// <summary>
        /// Called when the inspector GUI is drawn in the editor.
        /// </summary>
        /// <param name="selectedVolumes">The selected volumes in the editor (for multi-editing).</param>
        /// <returns>True if a property changed or else false.</returns>
        public override bool OnInspectorGUI(Volume[] selectedVolumes)
        {
            var physicsVolumes = selectedVolumes.Cast<PhysicsVolume>();
            bool invalidate = false;

            // global force:

            GUILayout.BeginVertical("Box");
            {
                UnityEditor.EditorGUILayout.LabelField("Force Options", UnityEditor.EditorStyles.boldLabel);
                GUILayout.Space(4);

                UnityEditor.EditorGUI.indentLevel = 1;
                GUILayout.BeginVertical();
                {
                    PhysicsVolumeForceMode previousPhysicsVolumeForceMode;
                    forceMode = (PhysicsVolumeForceMode)UnityEditor.EditorGUILayout.EnumPopup(new GUIContent("Force Mode", "The force mode."), previousPhysicsVolumeForceMode = forceMode);
                    if (previousPhysicsVolumeForceMode != forceMode)
                    {
                        foreach (PhysicsVolume volume in physicsVolumes)
                            volume.forceMode = forceMode;
                        invalidate = true;
                    }

                    if (forceMode != PhysicsVolumeForceMode.None)
                    {
                        Vector3 previousVector3;
                        UnityEditor.EditorGUIUtility.wideMode = true;
                        force = UnityEditor.EditorGUILayout.Vector3Field(new GUIContent("Force", "The amount of force."), previousVector3 = force);
                        UnityEditor.EditorGUIUtility.wideMode = false;
                        if (previousVector3 != force)
                        {
                            foreach (PhysicsVolume volume in physicsVolumes)
                                volume.force = force;
                            invalidate = true;
                        }

                        PhysicsVolumeForceSpace previousPhysicsVolumeForceSpace;
                        forceSpace = (PhysicsVolumeForceSpace)UnityEditor.EditorGUILayout.EnumPopup(new GUIContent("Force Space", "The force space mode."), previousPhysicsVolumeForceSpace = forceSpace);
                        if (previousPhysicsVolumeForceSpace != forceSpace)
                        {
                            foreach (PhysicsVolume volume in physicsVolumes)
                                volume.forceSpace = forceSpace;
                            invalidate = true;
                        }
                    }
                }
                GUILayout.EndVertical();
                UnityEditor.EditorGUI.indentLevel = 0;
            }
            GUILayout.EndVertical();

            // relative force:

            GUILayout.BeginVertical("Box");
            {
                UnityEditor.EditorGUILayout.LabelField("Relative Force Options", UnityEditor.EditorStyles.boldLabel);
                GUILayout.Space(4);

                UnityEditor.EditorGUI.indentLevel = 1;
                GUILayout.BeginVertical();
                {
                    PhysicsVolumeForceMode previousPhysicsVolumeRelativeForceMode;
                    relativeForceMode = (PhysicsVolumeForceMode)UnityEditor.EditorGUILayout.EnumPopup(new GUIContent("Force Mode", "The relative force mode."), previousPhysicsVolumeRelativeForceMode = relativeForceMode);
                    if (previousPhysicsVolumeRelativeForceMode != relativeForceMode)
                    {
                        foreach (PhysicsVolume volume in physicsVolumes)
                            volume.relativeForceMode = relativeForceMode;
                        invalidate = true;
                    }

                    if (relativeForceMode != PhysicsVolumeForceMode.None)
                    {
                        Vector3 previousVector3;
                        UnityEditor.EditorGUIUtility.wideMode = true;
                        relativeForce = UnityEditor.EditorGUILayout.Vector3Field(new GUIContent("Force", "The amount of relative force."), previousVector3 = relativeForce);
                        UnityEditor.EditorGUIUtility.wideMode = false;
                        if (previousVector3 != relativeForce)
                        {
                            foreach (PhysicsVolume volume in physicsVolumes)
                                volume.relativeForce = relativeForce;
                            invalidate = true;
                        }

                        PhysicsVolumeForceSpace previousPhysicsVolumeForceSpace;
                        relativeForceSpace = (PhysicsVolumeForceSpace)UnityEditor.EditorGUILayout.EnumPopup(new GUIContent("Force Space", "The relative force space mode."), previousPhysicsVolumeForceSpace = relativeForceSpace);
                        if (previousPhysicsVolumeForceSpace != relativeForceSpace)
                        {
                            foreach (PhysicsVolume volume in physicsVolumes)
                                volume.relativeForceSpace = relativeForceSpace;
                            invalidate = true;
                        }
                    }
                }
                GUILayout.EndVertical();
                UnityEditor.EditorGUI.indentLevel = 0;
            }
            GUILayout.EndVertical();

            // global torque:

            GUILayout.BeginVertical("Box");
            {
                UnityEditor.EditorGUILayout.LabelField("Torque Options", UnityEditor.EditorStyles.boldLabel);
                GUILayout.Space(4);

                UnityEditor.EditorGUI.indentLevel = 1;
                GUILayout.BeginVertical();
                {
                    PhysicsVolumeForceMode previousPhysicsVolumeTorqueForceMode;
                    torqueForceMode = (PhysicsVolumeForceMode)UnityEditor.EditorGUILayout.EnumPopup(new GUIContent("Force Mode", "The torque force mode."), previousPhysicsVolumeTorqueForceMode = torqueForceMode);
                    if (previousPhysicsVolumeTorqueForceMode != torqueForceMode)
                    {
                        foreach (PhysicsVolume volume in physicsVolumes)
                            volume.torqueForceMode = torqueForceMode;
                        invalidate = true;
                    }

                    if (torqueForceMode != PhysicsVolumeForceMode.None)
                    {
                        Vector3 previousVector3;
                        UnityEditor.EditorGUIUtility.wideMode = true;
                        torque = UnityEditor.EditorGUILayout.Vector3Field(new GUIContent("Force", "The amount of torque force."), previousVector3 = torque);
                        UnityEditor.EditorGUIUtility.wideMode = false;
                        if (previousVector3 != torque)
                        {
                            foreach (PhysicsVolume volume in physicsVolumes)
                                volume.torque = torque;
                            invalidate = true;
                        }

                        PhysicsVolumeForceSpace previousPhysicsVolumeForceSpace;
                        torqueSpace = (PhysicsVolumeForceSpace)UnityEditor.EditorGUILayout.EnumPopup(new GUIContent("Force Space", "The torque force space mode."), previousPhysicsVolumeForceSpace = torqueSpace);
                        if (previousPhysicsVolumeForceSpace != torqueSpace)
                        {
                            foreach (PhysicsVolume volume in physicsVolumes)
                                volume.torqueSpace = torqueSpace;
                            invalidate = true;
                        }
                    }
                }
                GUILayout.EndVertical();
                UnityEditor.EditorGUI.indentLevel = 0;
            }
            GUILayout.EndVertical();

            // relative torque:

            GUILayout.BeginVertical("Box");
            {
                UnityEditor.EditorGUILayout.LabelField("Relative Torque Options", UnityEditor.EditorStyles.boldLabel);
                GUILayout.Space(4);

                UnityEditor.EditorGUI.indentLevel = 1;
                GUILayout.BeginVertical();
                {
                    PhysicsVolumeForceMode previousPhysicsVolumeRelativeTorqueForceMode;
                    relativeTorqueForceMode = (PhysicsVolumeForceMode)UnityEditor.EditorGUILayout.EnumPopup(new GUIContent("Force Mode", "The relative torque force mode."), previousPhysicsVolumeRelativeTorqueForceMode = relativeTorqueForceMode);
                    if (previousPhysicsVolumeRelativeTorqueForceMode != relativeTorqueForceMode)
                    {
                        foreach (PhysicsVolume volume in physicsVolumes)
                            volume.relativeTorqueForceMode = relativeTorqueForceMode;
                        invalidate = true;
                    }

                    if (relativeTorqueForceMode != PhysicsVolumeForceMode.None)
                    {
                        Vector3 previousVector3;
                        UnityEditor.EditorGUIUtility.wideMode = true;
                        relativeTorque = UnityEditor.EditorGUILayout.Vector3Field(new GUIContent("Force", "The amount of relative torque force."), previousVector3 = relativeTorque);
                        UnityEditor.EditorGUIUtility.wideMode = false;
                        if (previousVector3 != relativeTorque)
                        {
                            foreach (PhysicsVolume volume in physicsVolumes)
                                volume.relativeTorque = relativeTorque;
                            invalidate = true;
                        }

                        PhysicsVolumeForceSpace previousPhysicsVolumeForceSpace;
                        relativeTorqueSpace = (PhysicsVolumeForceSpace)UnityEditor.EditorGUILayout.EnumPopup(new GUIContent("Force Space", "The relative torque force space mode."), previousPhysicsVolumeForceSpace = relativeTorqueSpace);
                        if (previousPhysicsVolumeForceSpace != relativeTorqueSpace)
                        {
                            foreach (PhysicsVolume volume in physicsVolumes)
                                volume.relativeTorqueSpace = relativeTorqueSpace;
                            invalidate = true;
                        }
                    }
                }
                GUILayout.EndVertical();
                UnityEditor.EditorGUI.indentLevel = 0;
            }
            GUILayout.EndVertical();

            // general options:

            GUILayout.BeginVertical("Box");
            {
                UnityEditor.EditorGUILayout.LabelField("General Options", UnityEditor.EditorStyles.boldLabel);
                GUILayout.Space(4);

                UnityEditor.EditorGUI.indentLevel = 1;
                GUILayout.BeginVertical();
                {
                    LayerMask previousLayerMask;
                    layer = SabreGUILayout.LayerMaskField(new GUIContent("Layer Mask", "The layer mask to limit the effects of the physics volume to specific layers."), (previousLayerMask = layer).value);
                    if (previousLayerMask != layer)
                    {
                        foreach (PhysicsVolume volume in physicsVolumes)
                            volume.layer = layer;
                        invalidate = true;
                    }

                    bool previousBoolean;
                    useFilterTag = UnityEditor.EditorGUILayout.Toggle(new GUIContent("Use Filter Tag", "Whether to use a filter tag."), previousBoolean = useFilterTag);
                    if (useFilterTag != previousBoolean)
                    {
                        foreach (PhysicsVolume volume in physicsVolumes)
                            volume.useFilterTag = useFilterTag;
                        invalidate = true;
                    }

                    if (useFilterTag)
                    {
                        string previousString;
                        filterTag = UnityEditor.EditorGUILayout.TagField(new GUIContent("Filter Tag", "The filter tag to limit the effects of the physics volume to specific tags."), previousString = filterTag);
                        if (filterTag != previousString)
                        {
                            foreach (PhysicsVolume volume in physicsVolumes)
                                volume.filterTag = filterTag;
                            invalidate = true;
                        }
                    }

                    PhysicsVolumeGravityMode previousPhysicsVolumeGravityMode;
                    gravity = (PhysicsVolumeGravityMode)UnityEditor.EditorGUILayout.EnumPopup(new GUIContent("Gravity", "The gravity settings applied to rigid bodies inside the volume."), previousPhysicsVolumeGravityMode = gravity);
                    if (previousPhysicsVolumeGravityMode != gravity)
                    {
                        foreach (PhysicsVolume volume in physicsVolumes)
                            volume.gravity = gravity;
                        invalidate = true;
                    }
                }
                GUILayout.EndVertical();
                UnityEditor.EditorGUI.indentLevel = 0;
            }
            GUILayout.EndVertical();

            return invalidate; // true when a property changed, the brush invalidates and stores all changes.
        }

#endif

        /// <summary>
        /// Called when the volume is created in the editor.
        /// </summary>
        /// <param name="volume">The generated volume game object.</param>
        public override void OnCreateVolume(GameObject volume)
        {
            PhysicsVolumeComponent component = volume.AddComponent<PhysicsVolumeComponent>();
            component.forceMode = forceMode;
            component.forceSpace = forceSpace;
            component.force = force;
            component.relativeForceMode = relativeForceMode;
            component.relativeForceSpace = relativeForceSpace;
            component.relativeForce = relativeForce;
            component.torqueForceMode = torqueForceMode;
            component.torqueSpace = torqueSpace;
            component.torque = torque;
            component.relativeTorqueForceMode = relativeTorqueForceMode;
            component.relativeTorqueSpace = relativeTorqueSpace;
            component.relativeTorque = relativeTorque;
            component.gravity = gravity;
            component.layer = layer;
            component.useFilterTag = useFilterTag;
            component.filterTag = filterTag;
        }
    }
}

#endif