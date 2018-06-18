#if UNITY_EDITOR || RUNTIME_CSG
using System;

namespace Sabresaurus.SabreCSG
{
    /// <summary>
    /// Provides commonly used string constants.
    /// </summary>
    internal static class Constants
    {
        /// <summary>
        /// The game object volume component identifier.
        /// This is used for the hidden built volume game objects for volume brushes.
        /// </summary>
        public const string GameObjectVolumeComponentIdentifier = "SabreCSG: Volume Component (67173f4f-868c-4c70-ae40-335550c8354f)";
    }
}
#endif