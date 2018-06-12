using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sabresaurus.SabreCSG
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

		private bool isTriggered = false;

		private void OnTriggerEnter( Collider other )
		{
			if( other.tag == filterTag && other.gameObject.layer == layerMask )
			{
				if( isTriggered )
					return;

				if( triggerOnce )
				{
					isTriggered = true;
				}

				onEnterEvent.Invoke();
			}
		}

		private void OnTriggerExit( Collider other )
		{
			if( other.tag == filterTag && other.gameObject.layer == layerMask )
			{
				if( isTriggered )
					return;

				if( triggerOnce )
				{
					isTriggered = true;
				}

				onExitEvent.Invoke();
			}
		}

		private void OnTriggerStay( Collider other )
		{
			if( other.tag == filterTag && other.gameObject.layer == layerMask )
			{
				if( isTriggered )
					return;

				if( triggerOnce )
				{
					isTriggered = true;
				}

				onStayEvent.Invoke();
			}
		}
	}
}
