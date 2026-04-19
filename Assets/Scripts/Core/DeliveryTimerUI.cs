using TMPro;
using UnityEngine;

namespace HackKU.Core
{
    // Polls FoodOrderController's static pending-delivery state and renders a live
    // countdown ("Delivery: 3x Groceries — 8s") on the wrist watch. Hides when nothing
    // is on the way. Lives on the WristCanvas alongside WristWatchUI.
    public class DeliveryTimerUI : MonoBehaviour
    {
        public TMP_Text label;
        public GameObject root;

        void Update()
        {
            bool active = DeliveryState.HasPending;
            if (root != null && root.activeSelf != active) root.SetActive(active);
            if (!active) return;

            if (label != null)
            {
                float secs = Mathf.Max(0f, DeliveryState.Remaining);
                string name = string.IsNullOrEmpty(DeliveryState.Label)
                    ? "Delivery"
                    : DeliveryState.Label;
                label.text = "DELIVERY: " + name + " — " + Mathf.CeilToInt(secs) + "s";
            }
        }
    }
}
