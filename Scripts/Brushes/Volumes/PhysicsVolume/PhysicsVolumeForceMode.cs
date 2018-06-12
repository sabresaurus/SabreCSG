using UnityEngine;

namespace Sabresaurus.SabreCSG.Volumes
{
    /// <summary>
    /// The force modes for the <see cref="PhysicsVolume"/>.
    /// </summary>
    public enum PhysicsVolumeForceMode
    {
        /// <summary>
        /// Don't do anything.
        /// </summary>
        None = 0,
        /// <summary>
        /// Add a continuous force to the rigidbody, using its mass.
        /// </summary>
        Force = 1,
        /// <summary>
        /// Add an instant force impulse to the rigidbody, using its mass.
        /// </summary>
        Impulse = 2,
        /// <summary>
        /// Add an instant velocity change to the rigidbody, ignoring its mass.
        /// </summary>
        VelocityChange = 3,
        /// <summary>
        /// Add a continuous acceleration to the rigidbody, ignoring its mass.
        /// </summary>
        Acceleration = 4
    }
}
