using System;

namespace Sabresaurus.SabreCSG.Volumes
{
    /// <summary>
    /// The trigger types for the <see cref="TriggerVolume" />.
    /// </summary>
    [Serializable]
    public enum TriggerVolumeTriggerType
    {
        /// <summary>
        /// Uses unity events to trigger things in the scene.
        /// </summary>
        UnityEvent = 0
    };
}