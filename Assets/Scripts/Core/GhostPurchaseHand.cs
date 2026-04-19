using UnityEngine;
using UnityEngine.InputSystem;

namespace HackKU.Core
{
    public class GhostPurchaseHand : MonoBehaviour
    {
        public float aimRange = 4f;
        public float aimRadius = 0.12f;
        public LayerMask aimMask = ~0;

        public string buyBinding = "<XRController>{RightHand}/triggerPressed";
        public bool editorKeyboardShortcut = true;

        InputAction _buy;
        InputAction _kbBuy;
        GhostFurnitureItem _currentTarget;
        GhostPurchaseUI _ui;
        int _aimFrameOffset;
        GhostFurnitureItem _cachedHit;

        void OnEnable()
        {
            _buy = new InputAction("GhostBuy", InputActionType.Button, binding: buyBinding);
            _buy.Enable();
            if (editorKeyboardShortcut)
            {
                _kbBuy = new InputAction("GhostBuyKB", InputActionType.Button, binding: "<Keyboard>/b");
                _kbBuy.Enable();
            }
            // Stagger left/right hand physics queries so they don't land on the same frame.
            _aimFrameOffset = GetInstanceID() & 0x3;
        }

        void OnDisable()
        {
            _buy?.Disable(); _buy?.Dispose(); _buy = null;
            _kbBuy?.Disable(); _kbBuy?.Dispose(); _kbBuy = null;
            if (_currentTarget != null) _currentTarget.CancelHold();
            _currentTarget = null;
            if (_ui != null) _ui.Hide();
        }

        void Update()
        {
            // Suppress everything (UI + SFX + raycasts) until the player has picked a character.
            var sm = StatsManager.Instance;
            if (sm == null || sm.ActiveProfile == null)
            {
                if (_currentTarget != null) _currentTarget.CancelHold();
                _currentTarget = null;
                _cachedHit = null;
                if (_ui != null) _ui.Hide();
                SfxHub.Instance.StopTracked("ghostcharge_" + GetInstanceID());
                return;
            }

            // Physics queries are expensive; re-aim only every 3rd frame. At 90Hz VR
            // the worst-case staleness is ~33ms which is well below perceptible for
            // a hold-to-buy interaction.
            bool holdingNow = (_buy != null && _buy.IsPressed()) || (_kbBuy != null && _kbBuy.IsPressed());
            GhostFurnitureItem hit;
            if (holdingNow && _cachedHit != null && !_cachedHit.IsOwned)
            {
                // While committed to a buy, don't re-raycast — stay locked on.
                hit = _cachedHit;
            }
            else if (((Time.frameCount + _aimFrameOffset) % 3) == 0 || _cachedHit == null)
            {
                hit = FindAimedGhost();
                _cachedHit = hit;
            }
            else
            {
                hit = _cachedHit;
            }

            if (hit != _currentTarget)
            {
                if (_currentTarget != null) _currentTarget.CancelHold();
                _currentTarget = hit;
                if (_ui == null) _ui = GhostPurchaseUI.Create();
                if (_currentTarget == null) _ui.Hide();
                else _ui.AttachTo(_currentTarget);
            }

            if (_currentTarget == null || _currentTarget.IsOwned)
            {
                if (_ui != null) _ui.Hide();
                return;
            }

            bool holding = (_buy != null && _buy.IsPressed()) || (_kbBuy != null && _kbBuy.IsPressed());
            string sfxKey = "ghostcharge_" + GetInstanceID();
            if (holding)
            {
                bool wasCharging = _currentTarget.HoldProgress01 > 0f;
                if (!wasCharging)
                {
                    _currentTarget.BeginHold();
                    SfxHub.Instance.StartLoopedOrOneShot(sfxKey, "charge_start", 0.55f, loop: false);
                }
                _currentTarget.TickHold(Time.deltaTime);
            }
            else
            {
                bool wasCharging = _currentTarget.HoldProgress01 > 0f;
                _currentTarget.CancelHold();
                if (wasCharging)
                {
                    SfxHub.Instance.StopTracked(sfxKey);
                    if (!_currentTarget.IsOwned)
                        SfxHub.Instance.PlayAt("charge_cancel", _currentTarget.transform.position, 0.55f);
                }
            }

            if (_ui != null) _ui.Show(_currentTarget);
        }

        GhostFurnitureItem FindAimedGhost()
        {
            if (Physics.SphereCast(transform.position, aimRadius, transform.forward,
                                   out var hit, aimRange, aimMask, QueryTriggerInteraction.Collide))
            {
                var gf = hit.collider.GetComponentInParent<GhostFurnitureItem>();
                if (gf != null && !gf.IsOwned) return gf;
            }
            var cols = Physics.OverlapSphere(transform.position + transform.forward * 0.4f, 0.8f, aimMask, QueryTriggerInteraction.Collide);
            foreach (var c in cols)
            {
                var gf = c.GetComponentInParent<GhostFurnitureItem>();
                if (gf != null && !gf.IsOwned) return gf;
            }
            return null;
        }
    }
}
