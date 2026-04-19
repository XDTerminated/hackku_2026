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

        int _shownSecs = int.MinValue;
        string _shownName;

        void Update()
        {
            bool active = DeliveryState.HasPending;
            if (root != null && root.activeSelf != active)
            {
                root.SetActive(active);
                if (!active) { _shownSecs = int.MinValue; _shownName = null; }
            }
            if (!active) return;

            if (label != null)
            {
                int secs = Mathf.CeilToInt(Mathf.Max(0f, DeliveryState.Remaining));
                string name = string.IsNullOrEmpty(DeliveryState.Label)
                    ? "Delivery"
                    : DeliveryState.Label;
                if (secs != _shownSecs || !ReferenceEquals(name, _shownName))
                {
                    _shownSecs = secs;
                    _shownName = name;
                    label.text = "DELIVERY: " + name + " — " + secs + "s";
                }
            }
        }
    }
}
