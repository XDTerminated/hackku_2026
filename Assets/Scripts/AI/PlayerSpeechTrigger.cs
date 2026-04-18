using UnityEngine;
using UnityEngine.InputSystem;

namespace HackKU.AI
{
    // Single-tap "I'm done talking" signal during an active call.
    // CallController auto-records continuously during its listen phase;
    // this component just tells it "okay, transcribe now."
    public class PlayerSpeechTrigger : MonoBehaviour
    {
        [SerializeField] CallController callController;
        [SerializeField] MicrophoneCapture mic;

        [Tooltip("InputSystem binding path for end-of-turn. Default: right-hand A button.")]
        [SerializeField] string primaryBindingPath = "<XRController>{RightHand}/primaryButton";

        [Tooltip("Fallback binding (right trigger).")]
        [SerializeField] string fallbackBindingPath = "<XRController>{RightHand}/triggerPressed";

        [Tooltip("Also respond to keyboard Space in Editor for desktop testing.")]
        [SerializeField] bool editorKeyboardShortcut = true;

        [Tooltip("Min seconds between consecutive end-of-turn signals.")]
        [SerializeField] float debounceSeconds = 0.5f;

        InputAction doneAction;
        float nextAllowedTime;

        void OnEnable()
        {
            doneAction = new InputAction("EndOfTurn", InputActionType.Button);
            doneAction.AddBinding(primaryBindingPath);
            if (!string.IsNullOrEmpty(fallbackBindingPath)) doneAction.AddBinding(fallbackBindingPath);
            if (editorKeyboardShortcut) doneAction.AddBinding("<Keyboard>/space");
            doneAction.performed += OnPerformed;
            doneAction.Enable();
        }

        void OnDisable()
        {
            if (doneAction == null) return;
            doneAction.performed -= OnPerformed;
            doneAction.Disable();
            doneAction.Dispose();
            doneAction = null;
        }

        void OnPerformed(InputAction.CallbackContext ctx)
        {
            if (callController == null || !callController.IsCallActive) return;
            if (Time.unscaledTime < nextAllowedTime) return;
            nextAllowedTime = Time.unscaledTime + debounceSeconds;
            callController.OnPlayerFinishedSpeaking();
        }
    }
}
