using UnityEngine;

namespace HackKU.Core
{
    // Makes a single card always face the player's head. Attach per card (not to the anchor).
    public class CardBillboard : MonoBehaviour
    {
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
            toHead.y = 0f;
            if (toHead.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.LookRotation(-toHead, Vector3.up);
        }
    }
}
