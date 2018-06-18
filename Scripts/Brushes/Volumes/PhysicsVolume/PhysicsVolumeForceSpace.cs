using UnityEngine;

namespace Sabresaurus.SabreCSG.Volumes
{
    /// <summary>
    /// The force space modes for the <see cref="PhysicsVolume"/>.
    /// </summary>
    public enum PhysicsVolumeForceSpace
    {
        /// <summary>
        /// Rotates physical forces relative to the volume brush.
        /// </summary>
        Relative = 0,

        /// <summary>
        /// The physical forces are applied in world space.
        /// </summary>
        World = 1
    }
}