using UnityEngine;

namespace HackKU.Core
{
    // Rotates a wrist-mounted canvas so its face always points toward the player's head,
    // with a slight forward-tilt. Position stays locked to the controller (parented).
    public class WristBillboardFace : MonoBehaviour
    {
        [Tooltip("Extra tilt in degrees so the face is readable from a natural arm pose. Positive = tilts top away from the head.")]
        public float tiltDegrees = 20f;

        [Tooltip("Head override. Auto-discovers Camera.main when null.")]
        public Transform head;

        void LateUpdate()
        {
            if (head == null)
            {
                var cam = Camera.main;
                if (cam != null) head = cam.transform;
                if (head == null) return;
            }

            Vector3 toHead = head.position - transform.position;
            if (toHead.sqrMagnitude < 0.0001f) return;

            Quaternion look = Quaternion.LookRotation(-toHead, Vector3.up);
            Quaternion tilt = Quaternion.AngleAxis(tiltDegrees, Vector3.right);
            transform.rotation = look * tilt;
        }
    }
}
