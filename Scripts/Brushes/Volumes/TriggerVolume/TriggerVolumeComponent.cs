using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sabresaurus.SabreCSG.Volumes
{
	public class TriggerVolumeComponent : MonoBehaviour
	{
		public TriggerVolumeEventType volumeEventType;
		public TriggerVolumeTriggerMode triggerMode;
		public LayerMask layerMask;
		public string filterTag;
		public bool triggerOnce;
		public TriggerVolumeEvent onEnterEvent;
		public TriggerVolumeEvent onStayEvent;
		public TriggerVolumeEvent onExitEvent;
		public List<TriggerVolumeSendMessageEvent> smOnEnterEvent;
		public List<TriggerVolumeSendMessageEvent> smOnStayEvent;
		public List<TriggerVolumeSendMessageEvent> smOnExitEvent;

		private bool isTriggered = false;

		private void OnTriggerEnter( Collider other )
		{
			if( triggerMode == TriggerVolumeTriggerMode.Enter ||
				triggerMode == TriggerVolumeTriggerMode.EnterExit ||
				triggerMode == TriggerVolumeTriggerMode.EnterStay ||
				triggerMode == TriggerVolumeTriggerMode.All )
			{
				if( other.tag == filterTag && other.gameObject.layer == layerMask )
				{
					if( isTriggered )
						return;

					if( triggerOnce )
					{
						isTriggered = true;
					}

					if( volumeEventType == TriggerVolumeEventType.UnityEvent )
					{
						onEnterEvent.Invoke();
					}
					else
					{
						foreach(TriggerVolumeSendMessageEvent e in smOnEnterEvent )
						{
							e.target.SendMessage( e.message, e.value, SendMessageOptions.DontRequireReceiver );

							//Debug.Log( "AAAAAAAAAAAAAAAAAAAA = Enter" );
						}
					}
				}
			}
		}

		private void OnTriggerExit( Collider other )
		{
			if( triggerMode == TriggerVolumeTriggerMode.Exit ||
				triggerMode == TriggerVolumeTriggerMode.EnterExit ||
				triggerMode == TriggerVolumeTriggerMode.All )
			{
				if( other.tag == filterTag && other.gameObject.layer == layerMask )
				{
					if( isTriggered )
						return;

					if( triggerOnce )
					{
						isTriggered = true;
					}
					if( volumeEventType == TriggerVolumeEventType.UnityEvent )
					{
						onExitEvent.Invoke();
					}
					else
					{
						foreach(TriggerVolumeSendMessageEvent e in smOnExitEvent )
						{
							e.target.SendMessage( e.message, e.value, SendMessageOptions.DontRequireReceiver );

							//Debug.Log( "AAAAAAAAAAAAAAAAAAAA = Exit" );
						}
					}
				}
			}
		}

		private void OnTriggerStay( Collider other )
		{
			if( triggerMode == TriggerVolumeTriggerMode.Stay ||
				triggerMode == TriggerVolumeTriggerMode.EnterStay ||
				triggerMode == TriggerVolumeTriggerMode.All )
			{
				if( other.tag == filterTag && other.gameObject.layer == layerMask )
				{
					if( isTriggered )
						return;

					if( triggerOnce )
					{
						isTriggered = true;
					}

					if( volumeEventType == TriggerVolumeEventType.UnityEvent )
					{
						onStayEvent.Invoke();
					}
					else
					{
						foreach(TriggerVolumeSendMessageEvent e in smOnStayEvent )
						{
							e.target.SendMessage( e.message, e.value, SendMessageOptions.DontRequireReceiver );

							//Debug.Log( "AAAAAAAAAAAAAAAAAAAA = Stay" );
						}
					}
				}
			}
		}
	}
}
