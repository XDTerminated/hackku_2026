using UnityEngine;

namespace HackKU.Core
{
    // Runtime helper that auto-orients a world-space canvas so its readable face
    // always points toward the player's camera. Fixes the endless "text is backwards"
    // problems caused by mixing TMP's -Z readable side with arbitrary parent rotations.
    [ExecuteAlways]
    public class BoardFaceRoom : MonoBehaviour
    {
        public bool yawOnly = true;

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 toCam = cam.transform.position - transform.position;
            if (yawOnly) toCam.y = 0f;
            if (toCam.sqrMagnitude < 0.0001f) return;
            // TMP / UI canvas readable face is -Z, so we aim +Z AWAY from the camera.
            transform.rotation = Quaternion.LookRotation(-toCam.normalized, Vector3.up);
        }
    }
}
