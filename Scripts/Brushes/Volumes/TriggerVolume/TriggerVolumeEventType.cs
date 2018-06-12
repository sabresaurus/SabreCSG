using System;

namespace Sabresaurus.SabreCSG
{
	[Serializable]
	public enum TriggerVolumeEventType : byte
	{
		SendMessage = 0, 
		UnityEvent = 2
	};
}
