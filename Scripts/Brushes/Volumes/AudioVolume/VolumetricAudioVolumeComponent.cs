#if UNITY_5_6_OR_NEWER

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Sabresaurus.SabreCSG.Volumes
{
    /// <summary>
    /// The Volumetric Audio for SabreCSG Volume Component.
    /// </summary>
    /// <seealso cref="UnityEngine.MonoBehaviour"/>
    /// <remarks>
    /// Previously sold commercially in the Unity Asset Store and now Open Source.
	/// <para>
	/// Made by Henry de Jongh for SabreCSG. https://00laboratories.com/
	/// If you wish to say thanks a donation is more than welcome! :D
	/// </para>
    /// </remarks>
    public class VolumetricAudioVolumeComponent : MonoBehaviour
    {
        /// <summary>
        /// The spatial 2D distance affects the distance from the volume before the volumetric sound fully transitions to 2D (inner radius).
        /// </summary>
        public float spatialDistance2D = 2.0f;

        /// <summary>
        /// The spatial 3D distance affects the distance from the volume before the volumetric sound fully transitions to 3D (outer radius).
        /// </summary>
        public float spatialDistance3D = 4.0f;

        /// <summary>
        /// The asset store version, for backwards compatibility in case we update these components in the future.
        /// </summary>
        [HideInInspector]
        public int assetStoreVersion = 1;

        /// <summary>
        /// The unique identifier of this volumetric audio volume.
        /// </summary>
        public string uniqueIdentifier = "";

        /// <summary>
        /// The parent identifier of this volumetric audio volume.
        /// </summary>
        public string parentIdentifier = "";

#if UNITY_EDITOR

        /// <summary>
        /// The previous parent identifier, cache used to reduce editor slowdown.
        /// </summary>
        private string previousParentIdentifier = "";

        private VolumetricAudioVolumeComponent editorParentAudioVolumeComponent;

        /// <summary>
        /// The previous editor linking counter number that we encountered.
        /// </summary>
        private int previousEditorLinkingCounter = -1;

        /// <summary>
        /// The editor linking counter helps notify all volumetric audio volume components that
        /// parent/child relationships changed.
        /// </summary>
        private static int s_EditorLinkingCounter = 0;

#endif

        /// <summary>
        /// The audio source that moves around the scene.
        /// </summary>
        private AudioSource audioSource;

        /// <summary>
        /// The mesh collider containing the convex mesh.
        /// </summary>
        private MeshCollider meshCollider;

        /// <summary>
        /// The audio listener currently active in the scene. There can only be one active audio
        /// listener in the scene.
        /// </summary>
        private AudioListener audioListener;

        /// <summary>
        /// The child volumes that have this volume set as their parent.
        /// </summary>
        private VolumetricAudioVolumeComponent[] childVolumes;

        /// <summary>
        /// The desired audio volume that we fade to when the game starts.
        /// </summary>
        private float desiredAudioVolume = 0.0f;

        /// <summary>
        /// The audio volume fade in time.
        /// </summary>
        private float audioVolumeFadeInTime = 0.0f;

        /// <summary>
        /// Whether we have initialized the audio volume.
        /// </summary>
        private bool initializedAudioVolume = false;

        /// <summary>
        /// Whether the game has been initialized (must have rendered one frame).
        /// </summary>
        private bool initializedGame = false;

        /// <summary>
        /// Called whenever the volume is enabled.
        /// </summary>
        private void OnEnable()
        {
            // find the audio source.
            if (!audioSource) audioSource = GetComponentInChildren<AudioSource>();
            // find our mesh collider.
            if (!meshCollider) meshCollider = GetComponent<MeshCollider>();
            // game-only code that fades-in the volume to prevent a sudden burst of sound in built games.
            if (Application.isPlaying && audioSource)
            {
                initializedAudioVolume = false;
                initializedGame = false;
                desiredAudioVolume = audioSource.volume;
                audioSource.volume = 0.0f;
            }

#if UNITY_EDITOR
            // editor-only code to reduce slowdown.
            if (!Application.isPlaying)
            {
                // only execute when the parent identifier has changed.
                if (parentIdentifier != previousParentIdentifier)
                {
                    // execute once.
                    previousParentIdentifier = parentIdentifier;
                    // notify all volumetric audio volume components that a parent/child relationship changed.
                    s_EditorLinkingCounter++;
                }

                // only execute when there was a global parent/child relationship change.
                if (s_EditorLinkingCounter != previousEditorLinkingCounter)
                {
                    // execute once.
                    previousEditorLinkingCounter = s_EditorLinkingCounter;
                    // update the array of our children.
                    childVolumes = FindChildVolumetricAudioVolumeComponents();
                    // find our parent.
                    editorParentAudioVolumeComponent = FindParentVolumetricAudioVolumeComponent();
                }

                // the editor exits here.
                return;
            }
#endif

            // if we are a child volume we don't have to do anything.
            if (!IsChildVolume)
            {
                // we are not a child volume so find any children we may have.
                childVolumes = FindChildVolumetricAudioVolumeComponents();
            }
        }

        /// <summary>
        /// Called after the camera has moved to a new position in the scene (we assume that the
        /// camera is moved during the regular update). We move our audio source accordingly.
        /// </summary>
        private void LateUpdate()
        {
            // child volumes do not have any execution logic themselves.
            if (IsChildVolume) return;

            // emergency - when the audio source no longer exists (user error), skip this.
            if (!audioSource) return;

            // there can only be one active audio listener in the scene.
            // we use this to automatically determine the position of the camera.

            // if we can't find an appropriate audio listener we stop here.
            if (!TryGetAudioListener()) return;

            // find the closest position to our mesh collider position.
            Vector3 audioListenerPosition = GetAudioListenerPosition();
            Vector3 closestPosition = meshCollider.ClosestPoint(audioListenerPosition);
            float distance = Vector3.Distance(audioListenerPosition, closestPosition);

            // there can be floating point errors on the exact border of colliders that apparently cause values of 0,0,0.
            // if this happens we stop execution here to prevent audio glitches, it simply uses the previous values.
            if (transform.InverseTransformPoint(closestPosition) == Vector3.zero)
                return;

            // iterate through our child volumes and find an even closer position.
            for (int i = 0; i < childVolumes.Length; i++)
            {
                // emergency - when a child volume no longer exists (user error or editor undo issue), skip it.
                if (!childVolumes[i]) continue;

                Vector3 closestChildPosition = childVolumes[i].GetComponent<MeshCollider>().ClosestPoint(audioListenerPosition);
                float closestChildDistance = Vector3.Distance(audioListenerPosition, closestChildPosition);

                // there can be floating point errors on the exact border of colliders that apparently cause values of 0,0,0.
                // if this happens we stop execution here to prevent audio glitches, it simply uses the previous values.
                if (transform.InverseTransformPoint(closestChildPosition) == Vector3.zero)
                    return;

                if (closestChildDistance < distance)
                {
                    distance = closestChildDistance;
                    closestPosition = closestChildPosition;
                }
            }

            // there can be floating point errors on the exact border of colliders that apparently cause values of 0,0,0.
            // if this happens we stop execution here to prevent audio glitches, it simply uses the previous values.
            if (transform.InverseTransformPoint(closestPosition) == Vector3.zero)
                return;

            // move the audio source to the closest point we just calculated.
            audioSource.transform.position = closestPosition;

            // when the audio listener enters the volume we switch to 2D.
            // this gives the illusion that the sound surrounds you completely.
            audioSource.spatialBlend = LerpInnerOuterRadius(spatialDistance2D, spatialDistance3D, distance);

            // game-only code that fades-in the volume to prevent a sudden burst of sound in built games.
            if (Application.isPlaying && !initializedAudioVolume)
            {
                // we wait until a frame has been rendered so we know there's no more loading times.
                if (!initializedGame)
                {
                    initializedGame = true;
                    audioVolumeFadeInTime = Time.time;
                }
                // fade in the audio source volume.
                audioSource.volume = Mathf.Lerp(audioSource.volume, desiredAudioVolume, Time.time - audioVolumeFadeInTime);
                if (Mathf.Approximately(audioSource.volume, desiredAudioVolume))
                {
                    audioSource.volume = desiredAudioVolume;
                    initializedAudioVolume = true;
                }
            }
        }

        /// <summary>
        /// Lerps a value between an inner and outer radius. If the value is smaller than the inner
        /// radius it's 0.0, between inner and outer radius it's 0.0 to 1.0, above the outer radius
        /// it's 1.0.
        /// </summary>
        /// <param name="inner">The inner radius.</param>
        /// <param name="outer">The outer radius.</param>
        /// <param name="value">The current value.</param>
        /// <returns>The lerped value.</returns>
        private float LerpInnerOuterRadius(float inner, float outer, float value)
        {
            // calculate inner radius.
            float distance = value - inner;
            if (distance < 0) distance = 0;

            // calculate outer radius.
            if (distance > 0)
            {
                float div = (outer - inner);
                if (div == 0) distance = 1; else distance = distance / div;
            }

            return Mathf.Lerp(0.0f, 1.0f, distance);
        }

        /// <summary>
        /// Gets the audio listener position.
        /// </summary>
        /// <returns>The position of the current audio listener.</returns>
        private Vector3 GetAudioListenerPosition()
        {
#if UNITY_EDITOR
            // real-time editor preview:
            if (!Application.isPlaying && Camera.current)
                return Camera.current.transform.position;
#endif
            // use the game audio listener:
            return audioListener.transform.position;
        }

        /// <summary>
        /// Tries to get the current audio listener that's active in the scene.
        /// </summary>
        /// <returns><c>true</c> if an appropriate audio listener was found; otherwise, <c>false</c>.</returns>
        private bool TryGetAudioListener()
        {
            // the audio listener no longer exists or isn't enabled.
            if (!IsBehaviourEnabled(audioListener))
            {
                // find an audio listener in the scene that's enabled.
                audioListener = FindObjectsOfType<AudioListener>().Where(e => IsBehaviourEnabled(e)).FirstOrDefault();
                return audioListener != null;
            }
            // we have a valid audio listener.
            return true;
        }

        /// <summary>
        /// Determines whether the specified behaviour is enabled.
        /// </summary>
        /// <param name="behaviour">The behaviour to check.</param>
        /// <returns><c>true</c> if the specified behaviour is enabled; otherwise, <c>false</c>.</returns>
        private bool IsBehaviourEnabled(Behaviour behaviour)
        {
            return behaviour != null && behaviour.enabled && behaviour.gameObject.activeInHierarchy;
        }

        /// <summary>
        /// Attempts to find the parent volumetric audio volume component.
        /// </summary>
        /// <returns>The parent volumetric audio volume component or null if not found.</returns>
        private VolumetricAudioVolumeComponent FindParentVolumetricAudioVolumeComponent()
        {
            return FindObjectsOfType<VolumetricAudioVolumeComponent>().FirstOrDefault(volume => volume.uniqueIdentifier == parentIdentifier);
        }

        /// <summary>
        /// Attempts to find the child volumetric audio volume components.
        /// </summary>
        /// <returns>The child volumetric audio volume components or null if none were found.</returns>
        private VolumetricAudioVolumeComponent[] FindChildVolumetricAudioVolumeComponents()
        {
            return FindObjectsOfType<VolumetricAudioVolumeComponent>().Where(volume => volume.parentIdentifier == uniqueIdentifier).ToArray();
        }

        /// <summary>
        /// Gets a value indicating whether this volume is a child volume.
        /// </summary>
        /// <value><c>true</c> if this volume is a child volume; otherwise, <c>false</c>.</value>
        private bool IsChildVolume { get { return !string.IsNullOrEmpty(parentIdentifier); } }

#if UNITY_EDITOR

        /// <summary>
        /// Called in the editor at every OnGUI.
        /// </summary>
        public void OnEditorPreview()
        {
            // real-time editor preview:
            if (!Application.isPlaying)
            {
                OnEnable();
                LateUpdate();

                // if the audio volume brush is selected, we draw additional gizmos.
                if (UnityEditor.Selection.Contains(transform.parent.gameObject))
                    OnDrawVolumeGizmos();
            }
        }

        /// <summary>
        /// Called when we can draw additional gizmos for our volume.
        /// </summary>
        public void OnDrawVolumeGizmos()
        {
            // child volumes do not have any audio source themselves.
            if (IsChildVolume)
            {
                // invoke the draw volume gizmos of the parent volume instead.
                if (!editorParentAudioVolumeComponent) return;
                editorParentAudioVolumeComponent.OnDrawVolumeGizmos();
                return;
            }
            // in case something goes wrong, if there is no audio source, quit here.
            if (!audioSource) return;

            Sabresaurus.SabreCSG.SabreCSGResources.GetSelectedBrushMaterial().SetPass(0);

            if (spatialDistance2D != 0)
            {
                GL.PushMatrix();
                GL.Begin(GL.LINES);
                GL.Color(Color.green);
                GL.MultMatrix(Matrix4x4.TRS(audioSource.transform.position, Quaternion.Euler(0, (float)UnityEditor.EditorApplication.timeSinceStartup * 15.0f, 0), Vector3.one));
                GlDrawSphere(Vector3.zero, spatialDistance2D, true, 128);
                GL.End();
                GL.PopMatrix();
            }
            if (spatialDistance3D != 0)
            {
                GL.PushMatrix();
                GL.Begin(GL.LINES);
                GL.Color(Color.red);
                GL.MultMatrix(Matrix4x4.TRS(audioSource.transform.position, Quaternion.Euler(0, (float)UnityEditor.EditorApplication.timeSinceStartup * -15.0f, 0), Vector3.one));
                GlDrawSphere(Vector3.zero, spatialDistance3D, true, 128);
                GL.End();
                GL.PopMatrix();
            }

            UnityEditor.SceneView.RepaintAll();
        }

        private void GlDrawSphere(Vector3 center, float radius, bool dotted = false, int segments = 32)
        {
            for (int j = 0; j < 3; j++)
            {
                switch (j)
                {
                    case 0: GlDrawCircle(center, radius, CirclePlane.X, dotted, segments); break;
                    case 1: GlDrawCircle(center, radius, CirclePlane.Y, dotted, segments); break;
                    case 2: GlDrawCircle(center, radius, CirclePlane.Z, dotted, segments); break;
                }
            }
        }

        private enum CirclePlane
        {
            X,
            Y,
            Z
        }

        private void GlDrawCircle(Vector3 center, float radius, CirclePlane plane, bool dotted = false, int segments = 32)
        {
            for (int i = 0; i < segments; i++)
            {
                float x = radius * Mathf.Sin(Mathf.Deg2Rad * (((float)i / segments) * 360.0f));
                float y = radius * Mathf.Cos(Mathf.Deg2Rad * (((float)i / segments) * 360.0f));
                switch (plane)
                {
                    case CirclePlane.X: GL.Vertex(center + new Vector3(x, y, 0)); break;
                    case CirclePlane.Y: GL.Vertex(center + new Vector3(x, 0, y)); break;
                    case CirclePlane.Z: GL.Vertex(center + new Vector3(0, x, y)); break;
                }

                if (!dotted)
                {
                    x = radius * Mathf.Sin(Mathf.Deg2Rad * (((float)(i + 1) / segments) * 360.0f));
                    y = radius * Mathf.Cos(Mathf.Deg2Rad * (((float)(i + 1) / segments) * 360.0f));
                    switch (plane)
                    {
                        case CirclePlane.X: GL.Vertex(center + new Vector3(x, y, 0)); break;
                        case CirclePlane.Y: GL.Vertex(center + new Vector3(x, 0, y)); break;
                        case CirclePlane.Z: GL.Vertex(center + new Vector3(0, x, y)); break;
                    }
                }
            }
        }

#endif
    }
}

#endif