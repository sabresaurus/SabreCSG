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
        /// The force mode applied to rigid bodies.
        /// </summary>
        public PhysicsVolumeForceMode forceMode = PhysicsVolumeForceMode.Force;

        /// <summary>
        /// The force applied to rigid bodies.
        /// </summary>
        public Vector3 force = new Vector3(0.0f, 10.0f, 0.0f);

        /// <summary>
        /// The relative force mode applied to rigid bodies.
        /// </summary>
        public PhysicsVolumeForceMode relativeForceMode = PhysicsVolumeForceMode.None;

        /// <summary>
        /// The relative force applied to rigid bodies.
        /// </summary>
        public Vector3 relativeForce = new Vector3(0.0f, 0.0f, 0.0f);

        /// <summary>
        /// The torque force mode applied to rigid bodies.
        /// </summary>
        public PhysicsVolumeForceMode torqueForceMode = PhysicsVolumeForceMode.None;

        /// <summary>
        /// The torque applied to rigid bodies.
        /// </summary>
        public Vector3 torque = new Vector3(0.0f, 0.0f, 0.0f);

        /// <summary>
        /// The relative torque force mode applied to rigid bodies.
        /// </summary>
        public PhysicsVolumeForceMode relativeTorqueForceMode = PhysicsVolumeForceMode.None;

        /// <summary>
        /// The relative torque applied to rigid bodies.
        /// </summary>
        public Vector3 relativeTorque = new Vector3(0.0f, 0.0f, 0.0f);

        /// <summary>
        /// The rigid bodies we are tracking as they entered the volume.
        /// </summary>
        private List<Rigidbody> rigidBodies;

        /// <summary>
        /// Called whenever the volume is enabled.
        /// </summary>
        private void OnEnable()
        {
            // time may have passed, reset our list of rigid bodies.
            rigidBodies = new List<Rigidbody>();
        }

        private void FixedUpdate()
        {
            // iterate the rigid bodies in reverse.
            for (int i = rigidBodies.Count - 1; i >= 0; i--)
            {
                Rigidbody rigidbody = rigidBodies[i];
                // if the rigid body was deleted, stop tracking it.
                if (!rigidbody)
                {
                    rigidBodies.RemoveAt(i);
                    continue;
                }
                // apply the force to the rigid body.
                switch (forceMode)
                {
                    case PhysicsVolumeForceMode.None:
                        break;

                    case PhysicsVolumeForceMode.Force:
                        rigidbody.AddForce(force, ForceMode.Force);
                        break;

                    case PhysicsVolumeForceMode.Impulse:
                        rigidbody.AddForce(force, ForceMode.Impulse);
                        break;

                    case PhysicsVolumeForceMode.VelocityChange:
                        rigidbody.AddForce(force, ForceMode.VelocityChange);
                        break;

                    case PhysicsVolumeForceMode.Acceleration:
                        rigidbody.AddForce(force, ForceMode.Acceleration);
                        break;
                }
                // apply the relative force to the rigid body.
                switch (relativeForceMode)
                {
                    case PhysicsVolumeForceMode.None:
                        break;

                    case PhysicsVolumeForceMode.Force:
                        rigidbody.AddRelativeForce(relativeForce, ForceMode.Force);
                        break;

                    case PhysicsVolumeForceMode.Impulse:
                        rigidbody.AddRelativeForce(relativeForce, ForceMode.Impulse);
                        break;

                    case PhysicsVolumeForceMode.VelocityChange:
                        rigidbody.AddRelativeForce(relativeForce, ForceMode.VelocityChange);
                        break;

                    case PhysicsVolumeForceMode.Acceleration:
                        rigidbody.AddRelativeForce(relativeForce, ForceMode.Acceleration);
                        break;
                }
                // apply the torque to the rigid body.
                switch (torqueForceMode)
                {
                    case PhysicsVolumeForceMode.None:
                        break;

                    case PhysicsVolumeForceMode.Force:
                        rigidbody.AddTorque(torque, ForceMode.Force);
                        break;

                    case PhysicsVolumeForceMode.Impulse:
                        rigidbody.AddTorque(torque, ForceMode.Impulse);
                        break;

                    case PhysicsVolumeForceMode.VelocityChange:
                        rigidbody.AddTorque(torque, ForceMode.VelocityChange);
                        break;

                    case PhysicsVolumeForceMode.Acceleration:
                        rigidbody.AddTorque(torque, ForceMode.Acceleration);
                        break;
                }
                // apply the relative torque to the rigid body.
                switch (relativeTorqueForceMode)
                {
                    case PhysicsVolumeForceMode.None:
                        break;

                    case PhysicsVolumeForceMode.Force:
                        rigidbody.AddRelativeTorque(relativeTorque, ForceMode.Force);
                        break;

                    case PhysicsVolumeForceMode.Impulse:
                        rigidbody.AddRelativeTorque(relativeTorque, ForceMode.Impulse);
                        break;

                    case PhysicsVolumeForceMode.VelocityChange:
                        rigidbody.AddRelativeTorque(relativeTorque, ForceMode.VelocityChange);
                        break;

                    case PhysicsVolumeForceMode.Acceleration:
                        rigidbody.AddRelativeTorque(relativeTorque, ForceMode.Acceleration);
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
            Rigidbody rigidbody = other.GetComponent<Rigidbody>();
            if (!rigidbody) return;
            if (!rigidBodies.Contains(rigidbody))
                rigidBodies.Add(rigidbody);
        }

        /// <summary>
        /// Called when a collider exits the volume. We stop tracking any rigid bodies.
        /// </summary>
        /// <param name="other">The collider that exited the volume.</param>
        private void OnTriggerExit(Collider other)
        {
            if (!other) return;
            Rigidbody rigidbody = other.GetComponent<Rigidbody>();
            if (!rigidbody) return;
            rigidBodies.Remove(rigidbody);
        }
    }
}