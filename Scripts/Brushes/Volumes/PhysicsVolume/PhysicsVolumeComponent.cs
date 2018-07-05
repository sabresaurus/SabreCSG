using System.Collections.Generic;
using UnityEngine;

namespace Sabresaurus.SabreCSG.Volumes
{
    /// <summary>
    /// Applies forces to rigid bodies inside of the volume.
    /// </summary>
    /// <seealso cref="UnityEngine.MonoBehaviour"/>
    public class PhysicsVolumeComponent : MonoBehaviour
    {
        /// <summary>
        /// Represents a rigidbody that is currently inside of the volume.
        /// </summary>
        private class TrackedRigidbody
        {
            /// <summary>
            /// The rigidbody inside of the volume.
            /// </summary>
            public Rigidbody rigidbody;

            /// <summary>
            /// Whether the rigidbody had gravity before entering the volume.
            /// </summary>
            public bool hadGravity;

            /// <summary>
            /// Initializes a new instance of the <see cref="TrackedRigidbody"/> class.
            /// </summary>
            /// <param name="rigidbody">The rigidbody inside of the volume.</param>
            public TrackedRigidbody(Rigidbody rigidbody)
            {
                // track information about the rigidbody.
                this.rigidbody = rigidbody;
                this.hadGravity = rigidbody.useGravity;
            }
        }

        /// <summary>
        /// The force mode applied to rigid bodies.
        /// </summary>
        public PhysicsVolumeForceMode forceMode = PhysicsVolumeForceMode.None;

        /// <summary>
        /// The force space mode applied to rigid bodies.
        /// </summary>
        public PhysicsVolumeForceSpace forceSpace = PhysicsVolumeForceSpace.Relative;

        /// <summary>
        /// The force applied to rigid bodies.
        /// </summary>
        public Vector3 force = new Vector3(0.0f, 0.0f, 0.0f);

        /// <summary>
        /// The relative force mode applied to rigid bodies.
        /// </summary>
        public PhysicsVolumeForceMode relativeForceMode = PhysicsVolumeForceMode.None;

        /// <summary>
        /// The relative force space mode applied to rigid bodies.
        /// </summary>
        public PhysicsVolumeForceSpace relativeForceSpace = PhysicsVolumeForceSpace.Relative;

        /// <summary>
        /// The relative force applied to rigid bodies.
        /// </summary>
        public Vector3 relativeForce = new Vector3(0.0f, 0.0f, 0.0f);

        /// <summary>
        /// The torque force mode applied to rigid bodies.
        /// </summary>
        public PhysicsVolumeForceMode torqueForceMode = PhysicsVolumeForceMode.None;

        /// <summary>
        /// The torque force space mode applied to rigid bodies.
        /// </summary>
        public PhysicsVolumeForceSpace torqueSpace = PhysicsVolumeForceSpace.Relative;

        /// <summary>
        /// The torque applied to rigid bodies.
        /// </summary>
        public Vector3 torque = new Vector3(0.0f, 0.0f, 0.0f);

        /// <summary>
        /// The relative torque force mode applied to rigid bodies.
        /// </summary>
        public PhysicsVolumeForceMode relativeTorqueForceMode = PhysicsVolumeForceMode.None;

        /// <summary>
        /// The relative torque force space mode applied to rigid bodies.
        /// </summary>
        public PhysicsVolumeForceSpace relativeTorqueSpace = PhysicsVolumeForceSpace.Relative;

        /// <summary>
        /// The relative torque applied to rigid bodies.
        /// </summary>
        public Vector3 relativeTorque = new Vector3(0.0f, 0.0f, 0.0f);

        /// <summary>
        /// The gravity settings applied to rigid bodies inside the volume.
        /// </summary>
        public PhysicsVolumeGravityMode gravity = PhysicsVolumeGravityMode.None;

        /// <summary>
        /// The layer mask to limit the effects of the physics volume to specific layers.
        /// </summary>
        public LayerMask layer = -1;

        /// <summary>
        /// Whether to use a filter tag.
        /// </summary>
        public bool useFilterTag = false;

        /// <summary>
        /// The filter tag to limit the effects of the physics volume to specific tags.
        /// </summary>
        public string filterTag = "Untagged";

        /// <summary>
        /// The rigid bodies we are tracking as they entered the volume.
        /// </summary>
        private List<TrackedRigidbody> rigidBodies;

        /// <summary>
        /// Called whenever the volume is enabled.
        /// </summary>
        private void OnEnable()
        {
            // time may have passed, reset our list of rigid bodies.
            rigidBodies = new List<TrackedRigidbody>();
        }

        private void FixedUpdate()
        {
            // iterate the tracked rigid bodies in reverse.
            for (int i = rigidBodies.Count - 1; i >= 0; i--)
            {
                TrackedRigidbody trackedRigidbody = rigidBodies[i];
                Rigidbody rigidbody = trackedRigidbody.rigidbody;
                // if the rigid body was deleted, stop tracking it.
                if (!rigidbody)
                {
                    rigidBodies.RemoveAt(i);
                    continue;
                }
                // apply the force to the rigid body.
                Vector3 appliedForce = forceSpace == PhysicsVolumeForceSpace.Relative ? transform.rotation * force : force;
                switch (forceMode)
                {
                    case PhysicsVolumeForceMode.Force:
                        rigidbody.AddForce(appliedForce, ForceMode.Force);
                        break;

                    case PhysicsVolumeForceMode.Impulse:
                        rigidbody.AddForce(appliedForce, ForceMode.Impulse);
                        break;

                    case PhysicsVolumeForceMode.VelocityChange:
                        rigidbody.AddForce(appliedForce, ForceMode.VelocityChange);
                        break;

                    case PhysicsVolumeForceMode.Acceleration:
                        rigidbody.AddForce(appliedForce, ForceMode.Acceleration);
                        break;
                }
                // apply the relative force to the rigid body.
                Vector3 appliedRelativeForce = relativeForceSpace == PhysicsVolumeForceSpace.Relative ? transform.rotation * relativeForce : relativeForce;
                switch (relativeForceMode)
                {
                    case PhysicsVolumeForceMode.Force:
                        rigidbody.AddRelativeForce(appliedRelativeForce, ForceMode.Force);
                        break;

                    case PhysicsVolumeForceMode.Impulse:
                        rigidbody.AddRelativeForce(appliedRelativeForce, ForceMode.Impulse);
                        break;

                    case PhysicsVolumeForceMode.VelocityChange:
                        rigidbody.AddRelativeForce(appliedRelativeForce, ForceMode.VelocityChange);
                        break;

                    case PhysicsVolumeForceMode.Acceleration:
                        rigidbody.AddRelativeForce(appliedRelativeForce, ForceMode.Acceleration);
                        break;
                }
                // apply the torque to the rigid body.
                Vector3 appliedTorque = torqueSpace == PhysicsVolumeForceSpace.Relative ? transform.rotation * torque : torque;
                switch (torqueForceMode)
                {
                    case PhysicsVolumeForceMode.Force:
                        rigidbody.AddTorque(appliedTorque, ForceMode.Force);
                        break;

                    case PhysicsVolumeForceMode.Impulse:
                        rigidbody.AddTorque(appliedTorque, ForceMode.Impulse);
                        break;

                    case PhysicsVolumeForceMode.VelocityChange:
                        rigidbody.AddTorque(appliedTorque, ForceMode.VelocityChange);
                        break;

                    case PhysicsVolumeForceMode.Acceleration:
                        rigidbody.AddTorque(appliedTorque, ForceMode.Acceleration);
                        break;
                }
                // apply the relative torque to the rigid body.
                Vector3 appliedRelativeTorque = relativeTorqueSpace == PhysicsVolumeForceSpace.Relative ? transform.rotation * relativeTorque : relativeTorque;
                switch (relativeTorqueForceMode)
                {
                    case PhysicsVolumeForceMode.Force:
                        rigidbody.AddRelativeTorque(appliedRelativeTorque, ForceMode.Force);
                        break;

                    case PhysicsVolumeForceMode.Impulse:
                        rigidbody.AddRelativeTorque(appliedRelativeTorque, ForceMode.Impulse);
                        break;

                    case PhysicsVolumeForceMode.VelocityChange:
                        rigidbody.AddRelativeTorque(appliedRelativeTorque, ForceMode.VelocityChange);
                        break;

                    case PhysicsVolumeForceMode.Acceleration:
                        rigidbody.AddRelativeTorque(appliedRelativeTorque, ForceMode.Acceleration);
                        break;
                }
            }
        }

        /// <summary>
        /// Called when a collider enters the volume. We track any rigid bodies.
        /// </summary>
        /// <param name="other">The collider that entered the volume.</param>
        private void OnTriggerEnter(Collider other)
        {
            if (!other) return;
            // apply the layer mask limit.
            if (!layer.Contains(other.gameObject.layer)) return;
            // apply the tag filter.
            if (useFilterTag && other.tag != filterTag) return;
            Rigidbody rigidbody = other.GetComponent<Rigidbody>();
            if (!rigidbody) return;
            if (rigidBodies.Find(r => r.rigidbody == rigidbody) == null)
                rigidBodies.Add(new TrackedRigidbody(rigidbody));
            // apply the gravity mode to the rigid body.
            switch (gravity)
            {
                case PhysicsVolumeGravityMode.Enable:
                    rigidbody.useGravity = true;
                    break;

                case PhysicsVolumeGravityMode.Disable:
                case PhysicsVolumeGravityMode.ZeroGravity:
                case PhysicsVolumeGravityMode.ZeroGravityRestore:
                    rigidbody.useGravity = false;
                    break;
            }
        }

        /// <summary>
        /// Called when a collider exits the volume. We stop tracking any rigid bodies.
        /// </summary>
        /// <param name="other">The collider that exited the volume.</param>
        private void OnTriggerExit(Collider other)
        {
            if (!other) return;
            // apply the layer mask limit.
            if (!layer.Contains(other.gameObject.layer)) return;
            // apply the tag filter.
            if (useFilterTag && other.tag != filterTag) return;
            Rigidbody rigidbody = other.GetComponent<Rigidbody>();
            if (!rigidbody) return;
            int index = rigidBodies.FindIndex(r => r.rigidbody == rigidbody);
            TrackedRigidbody trackedRigidbody = rigidBodies[index];
            rigidBodies.RemoveAt(index);
            // apply the gravity mode to the rigid body.
            switch (gravity)
            {
                case PhysicsVolumeGravityMode.ZeroGravity:
                    rigidbody.useGravity = true;
                    break;

                case PhysicsVolumeGravityMode.ZeroGravityRestore:
                    rigidbody.useGravity = trackedRigidbody.hadGravity;
                    break;
            }
        }
    }
}