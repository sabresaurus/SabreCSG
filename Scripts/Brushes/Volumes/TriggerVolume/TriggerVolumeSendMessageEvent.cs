using UnityEngine;

namespace Sabresaurus.SabreCSG.Volumes
{
	[System.Serializable]
	public class TriggerVolumeSendMessageEvent
    {
		public TriggerVolumeParamTypeCode typeCode;
		public GameObject target;
		public string message;
		public object value;

		public TriggerVolumeSendMessageEvent( GameObject _target, string _message, object _value )
		{
			target = _target;
			message = _message;
			value = _value;
		}
	}
}
