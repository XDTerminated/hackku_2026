using UnityEngine;

namespace HackKU.Core
{
    // Periodically nudges the player when hunger drops below a threshold. Toast only —
    // doesn't spam if hunger stays low; cooldown between reminders.
    public class HungerNagger : MonoBehaviour
    {
        public float warnThreshold = 50f;
        public float criticalThreshold = 25f;
        public float cooldownSeconds = 30f;
        public float criticalCooldownSeconds = 18f;

        float _nextWarnAt;
        bool _hasShownFirst;

        void Update()
        {
            var sm = StatsManager.Instance;
            var hm = HungerManager.Instance;
            if (sm == null || sm.ActiveProfile == null || hm == null) return;

            float h = hm.Hunger;
            if (h > warnThreshold)
            {
                _hasShownFirst = false;
                return;
            }

            if (Time.time < _nextWarnAt && _hasShownFirst) return;

            bool critical = h <= criticalThreshold;
            if (critical)
            {
                ToastHUD.Show("Hunger critical!",
                    "Call to order pizza, groceries, or fast food now.",
                    ToastKind.Bill);
                SfxHub.Instance.Play("bill_late", 0.45f);
                _nextWarnAt = Time.time + criticalCooldownSeconds;
            }
            else
            {
                ToastHUD.Show("Getting hungry",
                    "Call to order food before you slow down.",
                    ToastKind.Info);
                _nextWarnAt = Time.time + cooldownSeconds;
            }
            _hasShownFirst = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoSpawn()
        {
            if (FindFirstObjectByType<HungerNagger>() != null) return;
            var go = new GameObject("[HungerNagger]");
            DontDestroyOnLoad(go);
            go.AddComponent<HungerNagger>();
        }
    }
}
