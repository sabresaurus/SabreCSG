using UnityEngine;

namespace Sabresaurus.SabreCSG.Volumes
{
    /// <summary>
    /// Executes trigger logic when objects interact with the volume.
    /// </summary>
    /// <seealso cref="UnityEngine.MonoBehaviour" />
    public class TriggerVolumeComponent : MonoBehaviour
    {
        /// <summary>
        /// The trigger type, this is reserved for future use.
        /// </summary>
        public TriggerVolumeTriggerType triggerType = TriggerVolumeTriggerType.UnityEvent;

        /// <summary>
        /// Whether to use a filter tag.
        /// </summary>
        public bool useFilterTag = false;

        /// <summary>
        /// The filter tag to limit the colliders that can invoke the trigger.
        /// </summary>
        public string filterTag = "Untagged";

        /// <summary>
        /// The layer mask to limit the colliders that can invoke the trigger.
        /// </summary>
        public LayerMask layer = -1;

        /// <summary>
        /// Whether the trigger can only be instigated once.
        /// </summary>
        public bool triggerOnceOnly = false;

        /// <summary>
        /// The event called when a collider enters the trigger volume.
        /// </summary>
        public TriggerVolumeEvent onEnterEvent;

        /// <summary>
        /// The event called when a collider stays in the trigger volume.
        /// </summary>
        public TriggerVolumeEvent onStayEvent;

        /// <summary>
        /// The event called when a collider exits the trigger volume.
        /// </summary>
        public TriggerVolumeEvent onExitEvent;

        /// <summary>
        /// Whether the trigger can still be triggered (used with <see cref="triggerOnceOnly"/>).
        /// </summary>
        private bool canTrigger = true;

        /// <summary>
        /// Called when a collider enters the volume.
        /// </summary>
        /// <param name="other">The collider that entered the volume.</param>
        private void OnTriggerEnter(Collider other)
        {
            // ignore empty events.
            if (onEnterEvent.GetPersistentEventCount() == 0) return;
            // tag filter:
            if (useFilterTag && other.tag != filterTag) return;
            // layer filter:
            if (!layer.Contains(other.gameObject.layer)) return;
            // trigger once only:
            if (!triggerOnceOnly) canTrigger = true;
            if (!canTrigger) return;
            if (triggerOnceOnly) canTrigger = false;

            switch (triggerType)
            {
                case TriggerVolumeTriggerType.UnityEvent:
                    onEnterEvent.Invoke();
                    break;
            }
        }

        /// <summary>
        /// Called when a collider exits the volume.
        /// </summary>
        /// <param name="other">The collider that exited the volume.</param>
        private void OnTriggerExit(Collider other)
        {
            // ignore empty events.
            if (onExitEvent.GetPersistentEventCount() == 0) return;
            // tag filter:
            if (useFilterTag && other.tag != filterTag) return;
            // layer filter:
            if (!layer.Contains(other.gameObject.layer)) return;
            // trigger once only:
            if (!triggerOnceOnly) canTrigger = true;
            if (!canTrigger) return;
            if (triggerOnceOnly) canTrigger = false;

            switch (triggerType)
            {
                case TriggerVolumeTriggerType.UnityEvent:
                    onExitEvent.Invoke();
                    break;
            }
        }

        /// <summary>
        /// Called every frame while a collider stays inside the volume.
        /// </summary>
        /// <param name="other">The collider that is inside of the volume.</param>
        private void OnTriggerStay(Collider other)
        {
            // ignore empty events.
            if (onStayEvent.GetPersistentEventCount() == 0) return;
            // tag filter:
            if (useFilterTag && other.tag != filterTag) return;
            // layer filter:
            if (!layer.Contains(other.gameObject.layer)) return;
            // trigger once only:
            if (!triggerOnceOnly) canTrigger = true;
            if (!canTrigger) return;
            if (triggerOnceOnly) canTrigger = false;

            switch (triggerType)
            {
                case TriggerVolumeTriggerType.UnityEvent:
                    onStayEvent.Invoke();
                    break;
            }
        }
    }
}