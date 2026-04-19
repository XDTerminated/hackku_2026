using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace HackKU.Game
{
    // Physics-based XR click — XRSimpleInteractable + BoxCollider on the card.
    // Forwards selectEntered to the CharacterCardUI so a trigger press anywhere
    // on the card counts as a "click", independent of uGUI raycasting.
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class CardXRClick : MonoBehaviour
    {
        XRSimpleInteractable simple;
        CharacterCardUI cardUI;

        void Awake()
        {
            simple = GetComponent<XRSimpleInteractable>();
            cardUI = GetComponent<CharacterCardUI>();
            if (cardUI == null) cardUI = GetComponentInChildren<CharacterCardUI>(true);
        }

        void OnEnable()
        {
            if (simple != null) simple.selectEntered.AddListener(OnSelect);
        }

        void OnDisable()
        {
            if (simple != null) simple.selectEntered.RemoveListener(OnSelect);
        }

        void OnSelect(SelectEnterEventArgs args)
        {
            if (cardUI == null) return;
            HackKU.Core.SfxHub.Instance.PlayAt("ui_click", transform.position, 0.85f);
            var btn = cardUI.GetComponentInChildren<UnityEngine.UI.Button>(true);
            if (btn != null) btn.onClick.Invoke();
        }
    }
}
