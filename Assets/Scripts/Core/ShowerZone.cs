using UnityEngine;

namespace HackKU.Core
{
    // Attach to a GameObject with a trigger collider. While the player's head (or
    // anything on the configured layer mask) is inside, hygiene fills up.
    [RequireComponent(typeof(Collider))]
    public class ShowerZone : MonoBehaviour
    {
        [Tooltip("Hygiene points per real second while the player is inside.")]
        public float fillPerSecond = 25f;

        [Tooltip("Optional: filter which colliders count (by tag). Leave blank to accept any.")]
        public string requiredTag = "";

        [Tooltip("Toast the player the first time they step in.")]
        public bool toastOnEnter = true;

        int _occupants;

        void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag)) return;
            _occupants++;
            if (toastOnEnter && _occupants == 1)
                ToastHUD.Show("Shower", "Hygiene refilling", ToastKind.Info);
        }

        void OnTriggerExit(Collider other)
        {
            if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag)) return;
            _occupants = Mathf.Max(0, _occupants - 1);
        }

        void Update()
        {
            if (_occupants <= 0) return;
            // Gated behind purchasing the shower ghost item. Until bought, the zone does
            // nothing — the only way to gain hygiene is a dentist / doctor / therapist call.
            if (!GhostRegistry.IsOwned("shower")) return;
            var hm = HygieneManager.Instance;
            if (hm == null) return;
            hm.ApplyDelta(fillPerSecond * Time.deltaTime, null);
        }
    }
}
