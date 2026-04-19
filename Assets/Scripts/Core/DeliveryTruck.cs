using System.Collections;
using UnityEngine;

namespace HackKU.Core
{
    // Full delivery-truck animation cycle: sits off-screen, drives in to a stop point
    // near the house, pauses so the box can appear on the doorstep, then drives away.
    public class DeliveryTruck : MonoBehaviour
    {
        [Header("Stops (world space)")]
        [Tooltip("Where the truck sits off-screen when idle (start + end of the cycle).")]
        public Vector3 offscreenStart = new Vector3(-30f, 0f, -8f);
        [Tooltip("Where the truck pulls up to during a delivery.")]
        public Vector3 curbsidePose = new Vector3(-1.7f, 0f, -7f);
        [Tooltip("Where the truck drives off to after dropping.")]
        public Vector3 offscreenEnd = new Vector3(30f, 0f, -8f);

        [Header("Timing")]
        public float arriveSeconds = 2.5f;
        public float pauseSeconds = 1.0f;
        public float leaveSeconds = 7f;

        [Header("Facing")]
        [Tooltip("Yaw (Y rotation) in degrees while driving along the road.")]
        public float drivingYaw = 90f;

        [Header("Sound")]
        [Tooltip("Honk clip played when the truck arrives at the curb.")]
        public AudioClip honkClip;
        [Range(0f, 1f)] public float honkVolume = 0.9f;
        AudioSource _audio;

        [Header("Legacy")]
        [Tooltip("Local direction the truck drives away along (usually forward). Used by DriveAway().")]
        public Vector3 driveDirection = Vector3.forward;
        [Tooltip("Seconds the truck spends driving away before hiding.")]
        public float driveDuration = 3.5f;
        [Tooltip("Top speed at end of drive (units/sec).")]
        public float maxSpeed = 8f;
        [Tooltip("Hide the GameObject after driving off.")]
        public bool hideWhenGone = false;

        bool _busy;
        bool _doorOpened;
        OpenableDoor _frontDoor;

        void OnEnable()
        {
            if (!_busy) transform.position = offscreenStart;
            transform.rotation = Quaternion.Euler(0f, drivingYaw, 0f);
            HookFrontDoor();
            EnsureAudio();
        }

        void EnsureAudio()
        {
            if (_audio != null) return;
            _audio = GetComponent<AudioSource>();
            if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.loop = false;
            _audio.spatialBlend = 1f;
            _audio.minDistance = 3f;
            _audio.maxDistance = 40f;
            if (honkClip == null)
                honkClip = Resources.Load<AudioClip>("DeliveryBeep");
#if UNITY_EDITOR
            if (honkClip == null)
                honkClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/DeliveryBeep.mp3");
#endif
        }

        void Honk()
        {
            EnsureAudio();
            if (_audio != null && honkClip != null)
                _audio.PlayOneShot(honkClip, honkVolume);
        }

        void HookFrontDoor()
        {
            if (_frontDoor != null) return;
            var pivot = GameObject.Find("FrontDoorPivot");
            if (pivot == null) return;
            _frontDoor = pivot.GetComponent<OpenableDoor>();
            if (_frontDoor == null) return;
            _frontDoor.onOpened.AddListener(NotifyDoorOpened);
        }

        void OnDisable()
        {
            if (_frontDoor != null)
            {
                _frontDoor.onOpened.RemoveListener(NotifyDoorOpened);
                _frontDoor = null;
            }
        }

        // Hook this up (via Unity inspector or code) to the front-door's onOpened UnityEvent
        // so the truck waits at the curb until the player opens the door, then drives away.
        public void NotifyDoorOpened()
        {
            _doorOpened = true;
        }

        // Full cycle: drive in, spawn box, WAIT FOR DOOR, drive away + hide.
        public void DeliverCycle(System.Action onCurbside = null)
        {
            if (_busy) return;
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            _busy = true;
            _doorOpened = false;
            StartCoroutine(DeliverRoutine(onCurbside));
        }

        IEnumerator DeliverRoutine(System.Action onCurbside)
        {
            transform.position = offscreenStart;
            transform.rotation = Quaternion.Euler(0f, drivingYaw, 0f);

            // 1) Drive in to the curb.
            yield return Drive(offscreenStart, curbsidePose, arriveSeconds);

            // 2) Honk + drop the box.
            Honk();
            onCurbside?.Invoke();

            // 3) Idle indefinitely until the player opens the door. A short minimum pause
            //    first so it doesn't peel out instantly if the door is already open.
            yield return new WaitForSeconds(Mathf.Max(0.2f, pauseSeconds));
            while (!_doorOpened) yield return null;

            // 4) Drive away to the east, then hide the truck until the next delivery.
            yield return Drive(curbsidePose, offscreenEnd, leaveSeconds);
            gameObject.SetActive(false);

            _busy = false;
        }

        IEnumerator Drive(Vector3 from, Vector3 to, float seconds)
        {
            float t = 0f;
            float dur = Mathf.Max(0.05f, seconds);
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                // Ease in/out for a more truck-y feel.
                float eased = k < 0.5f ? 2f * k * k : 1f - Mathf.Pow(-2f * k + 2f, 2f) * 0.5f;
                transform.position = Vector3.Lerp(from, to, eased);
                yield return null;
            }
            transform.position = to;
        }

        // Legacy one-shot drive-away — kept for existing callers.
        public void DriveAway()
        {
            if (_busy || !gameObject.activeInHierarchy) return;
            _busy = true;
            StartCoroutine(DriveRoutine());
        }

        IEnumerator DriveRoutine()
        {
            Vector3 dirWorld = transform.TransformDirection(driveDirection.normalized);
            float t = 0f;
            while (t < driveDuration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / driveDuration);
                float speed = Mathf.Lerp(0f, maxSpeed, k * k);
                transform.position += dirWorld * speed * Time.deltaTime;
                yield return null;
            }
            if (hideWhenGone) gameObject.SetActive(false);
            _busy = false;
        }
    }
}
