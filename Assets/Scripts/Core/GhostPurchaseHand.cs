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

        void OnEnable()
        {
            _buy = new InputAction("GhostBuy", InputActionType.Button, binding: buyBinding);
            _buy.Enable();
            if (editorKeyboardShortcut)
            {
                _kbBuy = new InputAction("GhostBuyKB", InputActionType.Button, binding: "<Keyboard>/b");
                _kbBuy.Enable();
            }
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
            GhostFurnitureItem hit = FindAimedGhost();

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
            if (holding)
            {
                if (_currentTarget.HoldProgress01 <= 0f) _currentTarget.BeginHold();
                _currentTarget.TickHold(Time.deltaTime);
            }
            else
            {
                _currentTarget.CancelHold();
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
