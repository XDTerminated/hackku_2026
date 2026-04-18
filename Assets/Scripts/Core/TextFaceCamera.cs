using UnityEngine;

namespace HackKU.Core
{
    // Billboard that keeps TextMeshPro readable — rotates so the object's +Z (TMP's
    // readable face) points TOWARD the head (unlike CardBillboard which assumes -Z front).
    // Yaw-only by default so text stays upright.
    public class TextFaceCamera : MonoBehaviour
    {
        public Transform head;
        public bool lockUpright = true;
        [Tooltip("Optional: if set, the billboard keeps a fixed WORLD-space offset above this target, " +
                 "so a tumbling parent Rigidbody can't drag the info panel sideways or underground.")]
        public Transform anchor;
        public Vector3 worldOffset = new Vector3(0f, 0.4f, 0f);

        void LateUpdate()
        {
            if (anchor != null) transform.position = anchor.position + worldOffset;

            if (head == null)
            {
                var cam = Camera.main;
                if (cam != null) head = cam.transform;
                if (head == null) return;
            }
            Vector3 toHead = head.position - transform.position;
            if (lockUpright) toHead.y = 0f;
            if (toHead.sqrMagnitude < 0.0001f) return;
            // TMP (and Unity UI) render their readable face on -Z, so we point +Z AWAY from
            // the head so the player sees text the right way around.
            transform.rotation = Quaternion.LookRotation(-toHead, Vector3.up);
        }
    }
}
