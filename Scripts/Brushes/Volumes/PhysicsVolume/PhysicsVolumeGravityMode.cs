using UnityEngine;

namespace Sabresaurus.SabreCSG.Volumes
{
    /// <summary>
    /// The gravity modes for the <see cref="PhysicsVolume"/>.
    /// </summary>
    public enum PhysicsVolumeGravityMode
    {
        /// <summary>
        /// Don't do anything.
        /// </summary>
        None = 0,
        /// <summary>
        /// Enable gravity on the rigidbody.
        /// </summary>
        Enable = 1,
        /// <summary>
        /// Disable gravity on the rigidbody.
        /// </summary>
        Disable = 2,
        /// <summary>
        /// Disable gravity on the rigidbody while inside the volume, enable it on exit.
        /// </summary>
        ZeroGravity = 3,
        /// <summary>
        /// Disable gravity on the rigidbody while inside the volume, restore the original gravity settings on exit.
        /// </summary>
        ZeroGravityRestore = 4
    }
}
