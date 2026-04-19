using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace HackKU.Core
{
    public class HandsetController : MonoBehaviour
    {
        [Tooltip("Distance from cradle rest pose below which the handset will auto-dock. Larger = more forgiving.")]
        public float cradleProximity = 0.6f;

        [Tooltip("Generous AABB (half-extents, local cradle space) around the rest pose. Release anywhere inside this box and the handset snaps back.")]
        public Vector3 cradleZoneHalfExtents = new Vector3(0.6f, 0.8f, 0.5f);

        [Tooltip("Seconds to smoothly blend back to the exact cradle rest pose when docking.")]
        public float dockBlendSeconds = 0.2f;

        [Tooltip("Seconds a released handset has to settle before being considered 'on floor' (non-cradle).")]
        public float dropSettleSeconds = 0.8f;

        public RotaryPhone Phone { get; set; }

        // True only when the handset is settled on its cradle at rest pose.
        public bool IsOnCradle { get; private set; } = true;

        // True while the player is actively holding it (XR grab select).
        public bool IsHeld { get; private set; }

        // True when the handset has been dropped and is currently subject to physics (falling / lying on floor).
        public bool IsFallen { get; private set; }

        Vector3 restLocalPos;
        Quaternion restLocalRot;
        Transform cradleSpace;
        Rigidbody rb;
        XRGrabInteractable grab;
        Coroutine dockAnim;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            cradleSpace = transform.parent;
            restLocalPos = transform.localPosition;
            restLocalRot = transform.localRotation;
            grab = GetComponent<XRGrabInteractable>();
            if (grab != null)
            {
                grab.selectEntered.AddListener(_ => OnGrabbed());
                grab.selectExited.AddListener(_ => OnReleased());
            }
        }

        public void OnGrabbed()
        {
            IsHeld = true;
            IsFallen = false;
            StopDockAnim();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
                rb.useGravity = false;
            }
            // Only firing NotifyHandsetLifted on cradle pickup — that's the exclusive
            // trigger for incoming-call answer / dial-out. Picking up a fallen handset
            // mid-call just lets the player hold the earpiece again; audio continues.
            if (IsOnCradle)
            {
                IsOnCradle = false;
                SfxHub.Instance.PlayAt("phone_pickup", transform.position, 0.9f);
                if (Phone != null) Phone.NotifyHandsetLifted();
            }
        }

        public void OnReleased()
        {
            IsHeld = false;
            if (TryDockIfClose()) return;
            DropToFloor();
        }

        void LateUpdate()
        {
            // If the handset drifts toward the cradle while physics is settling, snap it.
            if (!IsHeld && !IsOnCradle && !IsFallen && cradleSpace != null)
            {
                TryDockIfClose();
            }
        }

        bool TryDockIfClose()
        {
            if (cradleSpace == null) return false;
            var worldRest = cradleSpace.TransformPoint(restLocalPos);

            // Primary test: sphere proximity (quick, isotropic).
            if (Vector3.Distance(transform.position, worldRest) <= cradleProximity)
            {
                DockToCradle();
                return true;
            }

            // Secondary test: wider AABB in the cradle's local space. Release anywhere
            // inside this box snaps to the cradle — much more forgiving than a pure radius.
            Vector3 localHandset = cradleSpace.InverseTransformPoint(transform.position);
            Vector3 localRest = restLocalPos;
            Vector3 d = localHandset - localRest;
            if (Mathf.Abs(d.x) <= cradleZoneHalfExtents.x &&
                Mathf.Abs(d.y) <= cradleZoneHalfExtents.y &&
                Mathf.Abs(d.z) <= cradleZoneHalfExtents.z)
            {
                DockToCradle();
                return true;
            }
            return false;
        }

        void DropToFloor()
        {
            IsFallen = true;
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
            if (Phone != null) Phone.NotifyHandsetDropped();
        }

        // Call at end of a call. Forces the XR interactor to release the handset if held,
        // then runs a smooth dock animation back to the cradle rest pose.
        public void ForceHangUp()
        {
            ForceReleaseFromInteractor();
            DockToCradle();
        }

        void ForceReleaseFromInteractor()
        {
            if (grab == null || !grab.isSelected) return;
            // Toggling enabled forces XRI's InteractionManager to issue a select-exit on the
            // current interactor(s), cleanly releasing the object.
            grab.enabled = false;
            grab.enabled = true;
            IsHeld = false;
        }

        public void DockToCradle()
        {
            StopDockAnim();

            // IMPORTANT: flip the cradle state and notify the phone IMMEDIATELY, before the
            // smooth-snap animation starts. If we wait until the end of the animation, the
            // player can grab the handset again within that window and the call never ends.
            if (rb != null) { rb.isKinematic = true; rb.useGravity = false; rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
            bool wasOnCradle = IsOnCradle;
            IsOnCradle = true;
            IsFallen = false;

            if (cradleSpace == null)
            {
                transform.localPosition = restLocalPos;
                transform.localRotation = restLocalRot;
                if (!wasOnCradle)
                {
                    SfxHub.Instance.PlayAt("phone_wall_place", transform.position, 0.9f);
                    if (Phone != null) Phone.NotifyHandsetPlacedBack();
                }
                return;
            }
            if (!wasOnCradle)
            {
                SfxHub.Instance.PlayAt("phone_wall_place", transform.position, 0.9f);
                if (Phone != null) Phone.NotifyHandsetPlacedBack();
            }
            dockAnim = StartCoroutine(DockAnimation());
        }

        IEnumerator DockAnimation()
        {
            Vector3 startPos = transform.localPosition;
            Quaternion startRot = transform.localRotation;
            float t = 0f;
            float dur = Mathf.Max(0.01f, dockBlendSeconds);
            while (t < dur)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, t / dur);
                transform.localPosition = Vector3.Lerp(startPos, restLocalPos, u);
                transform.localRotation = Quaternion.Slerp(startRot, restLocalRot, u);
                yield return null;
            }
            transform.localPosition = restLocalPos;
            transform.localRotation = restLocalRot;
            dockAnim = null;
            // NotifyHandsetPlacedBack was already fired at the START of DockToCradle so the
            // call ends instantly — don't fire it again here.
        }

        void StopDockAnim()
        {
            if (dockAnim != null)
            {
                StopCoroutine(dockAnim);
                dockAnim = null;
            }
        }
    }
}
