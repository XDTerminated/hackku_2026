using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace HackKU.Core
{
    // Plays pickup/drop SFX for any XRGrabInteractable. Auto-attached at scene load
    // to every grab interactable except the wall-phone handset (which has its own
    // phone-specific pickup sound).
    [DisallowMultipleComponent]
    public class GrabSfx : MonoBehaviour
    {
        XRGrabInteractable _grab;

        void Awake()
        {
            _grab = GetComponent<XRGrabInteractable>();
            if (_grab == null) { enabled = false; return; }
            _grab.selectEntered.AddListener(OnGrabbed);
            _grab.selectExited.AddListener(OnReleased);
        }

        void OnDestroy()
        {
            if (_grab == null) return;
            _grab.selectEntered.RemoveListener(OnGrabbed);
            _grab.selectExited.RemoveListener(OnReleased);
        }

        void OnGrabbed(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs _)
        {
            SfxHub.Instance.PlayAt("pickup", transform.position, 0.7f);
        }

        void OnReleased(UnityEngine.XR.Interaction.Toolkit.SelectExitEventArgs _)
        {
            SfxHub.Instance.PlayAt("drop", transform.position, 0.7f);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoAttachAll()
        {
            var grabs = Object.FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < grabs.Length; i++)
            {
                var g = grabs[i];
                if (g == null) continue;
                // The wall-phone handset has its own dedicated pickup SFX.
                if (g.GetComponent<HandsetController>() != null) continue;
                if (g.GetComponent<GroceryBox>() != null) continue;
                if (g.GetComponent<GrabSfx>() != null) continue;
                g.gameObject.AddComponent<GrabSfx>();
            }
        }
    }
}
