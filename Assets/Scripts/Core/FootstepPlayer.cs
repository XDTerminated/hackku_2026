using UnityEngine;

namespace HackKU.Core
{
    // Plays footsteps while the player head (main camera) moves horizontally.
    // Tracking the head avoids CharacterController.isGrounded quirks in XR when
    // locomotion providers bypass ground detection.
    public class FootstepPlayer : MonoBehaviour
    {
        public float minHorizontalSpeed = 0.15f;
        public float stepIntervalAtWalk = 0.45f;
        public float minPitch = 0.9f;
        public float maxPitch = 1.1f;
        public float volume = 0.18f;

        Transform _head;
        Vector3 _prevPos;
        float _nextStep;
        int _variant;
        bool _initialized;

        void Update()
        {
            if (_head == null)
            {
                var cam = Camera.main;
                _head = cam != null ? cam.transform : null;
                if (_head == null) return;
                _prevPos = _head.position;
                _nextStep = Time.time + 0.2f;
                _initialized = true;
                return;
            }
            if (!_initialized)
            {
                _prevPos = _head.position;
                _initialized = true;
                return;
            }

            Vector3 now = _head.position;
            Vector3 delta = now - _prevPos;
            _prevPos = now;
            delta.y = 0f;
            float speed = delta.magnitude / Mathf.Max(Time.deltaTime, 1e-4f);

            if (speed < minHorizontalSpeed)
            {
                _nextStep = Time.time + 0.1f;
                return;
            }

            if (Time.time < _nextStep) return;
            _variant = (_variant + 1) % 3;
            string clip = "footstep_" + (_variant + 1);
            float pitch = Random.Range(minPitch, maxPitch);
            SfxHub.Instance.Play(clip, volume, pitch);
            float interval = stepIntervalAtWalk * Mathf.Clamp(1.4f / Mathf.Max(speed, 0.1f), 0.55f, 1.3f);
            _nextStep = Time.time + interval;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoAttach()
        {
            var existing = Object.FindFirstObjectByType<FootstepPlayer>();
            if (existing != null) return;
            var go = new GameObject("[FootstepPlayer]");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<FootstepPlayer>();
        }
    }
}
