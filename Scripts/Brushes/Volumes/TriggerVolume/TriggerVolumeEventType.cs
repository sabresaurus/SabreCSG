using System;

namespace Sabresaurus.SabreCSG.Volumes
{
	[Serializable]
	public enum TriggerVolumeEventType : byte
	{
		SendMessage = 0, 
		UnityEvent = 2
	};
}
