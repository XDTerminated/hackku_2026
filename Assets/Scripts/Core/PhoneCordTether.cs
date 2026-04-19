using UnityEngine;

namespace HackKU.Core
{
    // When the player is holding the handset, keeps them within the cord's max length by
    // pushing the XR Origin back along the cord direction whenever they overshoot.
    // No spring / no velocity — hard stop that feels like hitting the end of the cord.
    public class PhoneCordTether : MonoBehaviour
    {
        [Tooltip("Fixed cradle anchor on the wall phone (where the cord enters the body).")]
        public Transform cradleAnchor;

        [Tooltip("The handset the player is holding.")]
        public HandsetController handset;

        [Tooltip("Max distance from cradle before the player is pushed back. Keep in sync with PhoneCord.maxLength.")]
        public float maxDistance = 2.5f;

        [Tooltip("Optional — CharacterController on the XR Origin. Auto-found if left empty.")]
        public CharacterController playerController;

        [Tooltip("Optional — XR Origin transform. Auto-found if left empty.")]
        public Transform xrOrigin;

        void Start()
        {
            if (playerController == null || xrOrigin == null)
            {
                var rig = GameObject.Find("XR Origin (XR Rig)") ?? GameObject.Find("XR Origin");
                if (rig != null)
                {
                    xrOrigin = rig.transform;
                    if (playerController == null) playerController = rig.GetComponent<CharacterController>();
                }
            }
        }

        void LateUpdate()
        {
            if (handset == null || cradleAnchor == null || xrOrigin == null) return;
            if (!handset.IsHeld) return;

            Vector3 from = cradleAnchor.position;
            Vector3 to = handset.transform.position;
            Vector3 delta = to - from;
            // Flatten to horizontal — we only want to restrict lateral walking, not vertical hand height.
            delta.y = 0f;
            float dist = delta.magnitude;
            if (dist <= maxDistance) return;

            Vector3 overshoot = delta.normalized * (dist - maxDistance);
            if (playerController != null) playerController.Move(-overshoot);
            else xrOrigin.position -= overshoot;
        }
    }
}
