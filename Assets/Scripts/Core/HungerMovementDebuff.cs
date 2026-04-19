using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace HackKU.Core
{
    // Halves the XR rig's move speed while HungerManager.IsLow is true.
    // Attach to any GameObject (the XR Origin is fine).
    public class HungerMovementDebuff : MonoBehaviour
    {
        [Range(0.1f, 1f)]
        public float hungryMoveSpeedMultiplier = 0.5f;

        DynamicMoveProvider _provider;
        float _originalSpeed;
        bool _captured;
        float _nextResolveAt;

        void Awake() { TryCapture(); }

        void TryCapture()
        {
            _provider = Object.FindFirstObjectByType<DynamicMoveProvider>();
            if (_provider == null) return;
            _originalSpeed = _provider.moveSpeed;
            _captured = true;
        }

        void LateUpdate()
        {
            if (!_captured)
            {
                // Retry at most once per second so we don't re-scan every frame during
                // the stretch before the XR rig spawns.
                if (Time.unscaledTime < _nextResolveAt) return;
                _nextResolveAt = Time.unscaledTime + 1f;
                TryCapture();
                if (!_captured) return;
            }
            if (_provider == null) return;

            bool low = HungerManager.Instance != null && HungerManager.Instance.IsLow;
            float target = low ? _originalSpeed * hungryMoveSpeedMultiplier : _originalSpeed;
            if (!Mathf.Approximately(_provider.moveSpeed, target))
            {
                _provider.moveSpeed = target;
            }
        }
    }
}
