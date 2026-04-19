using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace HackKU.Core
{
    public class OpenableDoor : MonoBehaviour
    {
        [Tooltip("Fired when the door starts opening.")]
        public UnityEvent onOpened;


        [Tooltip("Hinge pivot. Door rotates around this transform's Y axis. If null, rotates around own transform.")]
        public Transform hinge;

        [Tooltip("Degrees to rotate when opened (around hinge Y axis).")]
        public float openAngle = 90f;

        [Tooltip("Seconds for open/close animation.")]
        public float duration = 0.5f;

        [Tooltip("If true, door opens automatically when the player enters the trigger.")]
        public bool autoOpenOnTriggerEnter = true;

        [Tooltip("If true, door closes automatically after the player leaves the trigger.")]
        public bool autoCloseOnTriggerExit = true;

        [Tooltip("Seconds to wait after the player exits the trigger before closing.")]
        public float autoCloseDelay = 0.8f;

        public bool IsOpen { get; private set; }
        public bool IsAnimating { get; private set; }

        Quaternion closedRotation;
        Quaternion openRotation;
        Coroutine anim;

        void Awake()
        {
            if (hinge == null) hinge = transform;
            closedRotation = hinge.localRotation;
            openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public void Open()
        {
            if (IsOpen || IsAnimating) return;
            IsOpen = true;
            if (anim != null) StopCoroutine(anim);
            anim = StartCoroutine(AnimateTo(openRotation));
            SfxHub.Instance.PlayAt("door_open", transform.position, 0.75f);
            onOpened?.Invoke();
        }

        public void Close()
        {
            if (!IsOpen || IsAnimating) return;
            IsOpen = false;
            if (anim != null) StopCoroutine(anim);
            anim = StartCoroutine(AnimateTo(closedRotation));
            SfxHub.Instance.PlayAt("door_close", transform.position, 0.75f);
        }

        IEnumerator AnimateTo(Quaternion target)
        {
            IsAnimating = true;
            var start = hinge.localRotation;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                hinge.localRotation = Quaternion.Slerp(start, target, Mathf.Clamp01(t / duration));
                yield return null;
            }
            hinge.localRotation = target;
            IsAnimating = false;
        }

        Coroutine closeDelayRoutine;

        static bool IsPlayerCollider(Collider other)
        {
            if (other == null) return false;
            if (other.CompareTag("Player") || other.CompareTag("MainCamera")) return true;
            // Fallback: any non-trigger collider attached to a CharacterController/Rigidbody
            // hierarchy counts as "the player" so we don't rely on tags being set correctly.
            if (other.GetComponentInParent<CharacterController>() != null) return true;
            var rb = other.attachedRigidbody;
            if (rb != null && rb.CompareTag("Player")) return true;
            return false;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!autoOpenOnTriggerEnter) return;
            if (!IsPlayerCollider(other)) return;
            if (closeDelayRoutine != null) { StopCoroutine(closeDelayRoutine); closeDelayRoutine = null; }
            Open();
        }

        void OnTriggerExit(Collider other)
        {
            if (!autoCloseOnTriggerExit) return;
            if (!IsPlayerCollider(other)) return;
            if (closeDelayRoutine != null) StopCoroutine(closeDelayRoutine);
            closeDelayRoutine = StartCoroutine(CloseAfterDelay());
        }

        IEnumerator CloseAfterDelay()
        {
            yield return new WaitForSeconds(autoCloseDelay);
            Close();
            closeDelayRoutine = null;
        }
    }
}
