#if UNITY_EDITOR || RUNTIME_CSG

using System;

namespace Sabresaurus.SabreCSG.Importers.UnrealGold
{
    /// <summary>
    /// Represents an Unreal Editor 1 rotator type.
    /// </summary>
    public class T3dRotator
    {
        /// <summary>
        /// Gets or sets the pitch.
        /// </summary>
        /// <value>The pitch.</value>
        public int Pitch { get; set; }

        /// <summary>
        /// Gets or sets the yaw.
        /// </summary>
        /// <value>The yaw.</value>
        public int Yaw { get; set; }

        /// <summary>
        /// Gets or sets the roll.
        /// </summary>
        /// <value>The roll.</value>
        public int Roll { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="T3dRotator"/> class.
        /// </summary>
        public T3dRotator()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T3dRotator"/> class.
        /// </summary>
        /// <param name="pitch">The pitch of the rotator.</param>
        /// <param name="yaw">The yaw of the rotator.</param>
        /// <param name="roll">The roll of the rotator.</param>
        public T3dRotator(int pitch, int yaw, int roll)
        {
            Pitch = pitch;
            Yaw = yaw;
            Roll = roll;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents this instance.</returns>
        public override string ToString()
        {
            return "Pitch: " + Pitch + ", Yaw: " + Yaw + ", Roll: " + Roll;
        }
    }
}

#endif