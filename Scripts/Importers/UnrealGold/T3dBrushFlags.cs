#if UNITY_EDITOR || RUNTIME_CSG

using System;

namespace Sabresaurus.SabreCSG.Importers.UnrealGold
{
    /// <summary>
    /// Represents Unreal Editor 1's Brush Flags.
    /// </summary>
    [Flags]
    public enum T3dBrushFlags
    {
        /// <summary>
        /// Whether the brush is invisible.
        /// </summary>
        Invisible = 1,

        /// <summary>
        /// Whether the brush is using masked textures.
        /// </summary>
        Masked = 2,

        /// <summary>
        /// Whether the brush is using transparent rendering.
        /// </summary>
        Transparent = 4,

        /// <summary>
        /// Whether the brush doesn't have collision.
        /// </summary>
        NonSolid = 8,

        /// <summary>
        /// Whether the brush is essentially Sabre's NoCSG.
        /// </summary>
        SemiSolid = 32,

        /// <summary>
        /// Whether the brush is used to split off sections of the world.
        /// </summary>
        ZonePortal = 67108864,

        /// <summary>
        /// Whether the brush is using two-sided rendering.
        /// </summary>
        TwoSided = 256
    }
}

#endif