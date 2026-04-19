using UnityEngine;

namespace HackKU.Core
{
    // Keeps a transform (e.g., CharacterSelect anchor) floating in front of the player's
    // head at a fixed eye-height and distance, so the card fan is always visible as
    // the player walks around. Disables itself once the selection completes.
    public class CardFanFollower : MonoBehaviour
    {
        [Tooltip("Distance in front of the player's head (horizontal).")]
        public float distance = 0.6f;

        [Tooltip("Eye-height offset (world Y) so cards don't bob with head tilt.")]
        public float eyeHeight = 1.45f;

        [Tooltip("How quickly the fan catches up to the player's current facing.")]
        public float followSpeed = 4f;

        [Tooltip("Use this transform's position as the anchor for 'player head'. Auto-discovers Main Camera when null.")]
        public Transform head;

        public bool autoDisableWhenChildrenGone = true;

        void LateUpdate()
        {
            if (head == null)
            {
                var cam = Camera.main;
                if (cam != null) head = cam.transform;
                if (head == null) return;
            }

            Vector3 forward = head.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
            forward.Normalize();

            Vector3 targetPos = new Vector3(head.position.x, eyeHeight, head.position.z) + forward * distance;
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);
            transform.rotation = Quaternion.LookRotation(forward, Vector3.up);

            if (autoDisableWhenChildrenGone && transform.childCount == 0)
            {
                enabled = false;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoAttach()
        {
            // Guarantees the follower is active even if the CharacterSelect was built
            // before the follower component was added to the build step.
            var selectGO = GameObject.Find("CharacterSelect");
            if (selectGO == null) return;
            if (selectGO.GetComponent<CardFanFollower>() == null)
            {
                var f = selectGO.AddComponent<CardFanFollower>();
                f.distance = 0.6f;
                f.eyeHeight = 1.45f;
            }
        }
    }
}
