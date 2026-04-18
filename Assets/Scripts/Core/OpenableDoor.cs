using System.Collections;
using UnityEngine;

namespace HackKU.Core
{
    public class OpenableDoor : MonoBehaviour
    {
        [Tooltip("Hinge pivot. Door rotates around this transform's Y axis. If null, rotates around own transform.")]
        public Transform hinge;

        [Tooltip("Degrees to rotate when opened (around hinge Y axis).")]
        public float openAngle = 90f;

        [Tooltip("Seconds for open/close animation.")]
        public float duration = 0.5f;

        [Tooltip("If true, door opens automatically when the player enters the trigger.")]
        public bool autoOpenOnTriggerEnter = true;

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
        }

        public void Close()
        {
            if (!IsOpen || IsAnimating) return;
            IsOpen = false;
            if (anim != null) StopCoroutine(anim);
            anim = StartCoroutine(AnimateTo(closedRotation));
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

        void OnTriggerEnter(Collider other)
        {
            if (!autoOpenOnTriggerEnter) return;
            if (other.CompareTag("Player") || other.CompareTag("MainCamera"))
            {
                Open();
            }
        }
    }
}
