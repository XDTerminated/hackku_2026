using UnityEngine;
using UnityEngine.InputSystem;

namespace HackKU.Core
{
    // Self-contained smooth turn: reads the right thumbstick's X axis directly
    // via the Input System and rotates the target transform (typically the XR Origin)
    // around its Y axis. Side-steps the XRI turn providers entirely to avoid wiring
    // issues with XRInputValueReader source modes.
    public class SimpleSmoothTurn : MonoBehaviour
    {
        [Tooltip("What to rotate — usually the XR Origin itself.")]
        public Transform target;

        [Tooltip("Degrees per second at full stick deflection.")]
        public float turnSpeed = 180f;

        [Tooltip("Dead zone — stick values with |x| below this are ignored.")]
        public float deadZone = 0.2f;

        [Tooltip("Binding path for the turn axis. Default: right-hand thumbstick.")]
        public string bindingPath = "<XRController>{RightHand}/thumbstick";

        [Tooltip("Also allow keyboard Q/E for desktop testing.")]
        public bool editorKeyboardShortcut = true;

        InputAction turnAction;
        InputAction kbLeft;
        InputAction kbRight;

        void Awake()
        {
            if (target == null) target = transform;
        }

        void OnEnable()
        {
            turnAction = new InputAction("Turn", InputActionType.Value, expectedControlType: "Vector2");
            turnAction.AddBinding(bindingPath);
            turnAction.Enable();

            if (editorKeyboardShortcut)
            {
                kbLeft = new InputAction("TurnLeft", InputActionType.Value, binding: "<Keyboard>/q");
                kbRight = new InputAction("TurnRight", InputActionType.Value, binding: "<Keyboard>/e");
                kbLeft.Enable();
                kbRight.Enable();
            }
        }

        void OnDisable()
        {
            turnAction?.Disable();
            turnAction?.Dispose();
            turnAction = null;
            kbLeft?.Disable();
            kbLeft?.Dispose();
            kbLeft = null;
            kbRight?.Disable();
            kbRight?.Dispose();
            kbRight = null;
        }

        void Update()
        {
            if (target == null) return;
            float x = 0f;
            if (turnAction != null)
            {
                var v = turnAction.ReadValue<Vector2>();
                x = v.x;
            }
            if (kbLeft != null && kbLeft.IsPressed()) x -= 1f;
            if (kbRight != null && kbRight.IsPressed()) x += 1f;
            if (Mathf.Abs(x) < deadZone) return;
            float delta = x * turnSpeed * Time.deltaTime;
            target.Rotate(0f, delta, 0f, Space.World);
        }
    }
}
