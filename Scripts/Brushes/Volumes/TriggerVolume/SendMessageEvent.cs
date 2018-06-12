using UnityEngine;

namespace Sabresaurus.SabreCSG
{
	[System.Serializable]
	public class SendMessageEvent
	{
		public TriggerVolumeParamTypeCode typeCode;
		public GameObject target;
		public string message;
		public object value;

		public SendMessageEvent( GameObject _target, string _message, object _value )
		{
			target = _target;
			message = _message;
			value = _value;
		}
	}
}
