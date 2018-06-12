using System;

namespace Sabresaurus.SabreCSG.Volumes
{
    [Serializable]
    public enum TriggerVolumeTriggerMode : byte
    {
        Enter = 0,
        Exit = 2,
        Stay = 4,
        EnterExit = 8,
        EnterStay = 16,
        All = 32
    };
}
