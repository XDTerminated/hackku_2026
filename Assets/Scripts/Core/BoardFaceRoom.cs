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

        Transform _camTf;

        void OnEnable()
        {
            var cam = Camera.main;
            _camTf = cam != null ? cam.transform : null;
        }

        void LateUpdate()
        {
            if (_camTf == null)
            {
                var cam = Camera.main;
                _camTf = cam != null ? cam.transform : null;
                if (_camTf == null) return;
            }
            Vector3 toCam = _camTf.position - transform.position;
            if (yawOnly) toCam.y = 0f;
            if (toCam.sqrMagnitude < 0.0001f) return;
            // TMP / UI canvas readable face is -Z, so we aim +Z AWAY from the camera.
            transform.rotation = Quaternion.LookRotation(-toCam.normalized, Vector3.up);
        }
    }
}
