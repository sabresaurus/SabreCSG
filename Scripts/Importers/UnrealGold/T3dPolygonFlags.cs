#if UNITY_EDITOR || RUNTIME_CSG

using System;

namespace Sabresaurus.SabreCSG.Importers.UnrealGold
{
    /// <summary>
    /// Represents Unreal Editor 1's Polygon Flags.
    /// </summary>
    [Flags]
    public enum T3dPolygonFlags
    {
        Invisible = 1,
        Masked = 2,
        Translucent = 4,
        Environment = 16,
        Modulated = 64,
        FakeBackdrop = 128,
        TwoSided = 256,
        UPan = 512,
        VPan = 1024,
        NoSmooth = 2048,
        SpecialPoly = 4096,
        SmallWavy = 8192,
        ForceViewZone = 16384,
        LowShadowDetail = 32768,
        AlphaBlend = 131072,
        DirtyShadows = 262144,
        BrightCorners = 524288,
        SpecialLit = 1048576,
        NoBoundsReject = 2097152,
        Unlit = 4194304,
        HighShadowDetail = 8388608,
        Portal = 67108864,
        Mirror = 134217728
    }
}

#endif