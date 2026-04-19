using System;
using UnityEngine;

namespace HackKU.Core
{
    // Tracks player hygiene (0-100). Drains slowly on real time; refills when the
    // player stands in a ShowerZone. Below lowThreshold, happiness drains faster
    // (wired in FinanceScheduler-style subscribers).
    public class HygieneManager : MonoBehaviour
    {
        public static HygieneManager Instance { get; private set; }

        [Header("Hygiene")]
        public float maxHygiene = 100f;
        public float startingHygiene = 100f;

        [Tooltip("Hygiene points lost per real-time second (passive drain).")]
        public float drainPerSecond = 0.15f;

        [Tooltip("At or below this, the player is 'gross' (drives extra happiness drain).")]
        public float lowThreshold = 30f;

        [Tooltip("Happiness lost per second while hygiene is below threshold.")]
        public float lowHygieneHappinessDrainPerSecond = 0.3f;

        public float Hygiene { get; private set; }
        public bool IsLow => Hygiene <= lowThreshold;
        public float Normalized => maxHygiene <= 0f ? 0f : Mathf.Clamp01(Hygiene / maxHygiene);

        public static event Action<float, float, string> OnHygieneChanged;
        public static event Action<bool> OnLowStateChanged;

        bool _wasLow;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Hygiene = Mathf.Clamp(startingHygiene, 0f, maxHygiene);
            _wasLow = IsLow;
        }

        void Update()
        {
            if (drainPerSecond > 0f)
            {
                float prev = Hygiene;
                Hygiene = Mathf.Max(0f, Hygiene - drainPerSecond * Time.deltaTime);
                if (!Mathf.Approximately(prev, Hygiene))
                    OnHygieneChanged?.Invoke(Hygiene, Hygiene - prev, null);
            }
            if (IsLow && StatsManager.Instance != null && lowHygieneHappinessDrainPerSecond > 0f)
            {
                StatsManager.Instance.ApplyDelta(0f, -lowHygieneHappinessDrainPerSecond * Time.deltaTime, "");
            }
            bool low = IsLow;
            if (low != _wasLow)
            {
                _wasLow = low;
                OnLowStateChanged?.Invoke(low);
            }
        }

        public void ApplyDelta(float delta, string reason)
        {
            float prev = Hygiene;
            Hygiene = Mathf.Clamp(Hygiene + delta, 0f, maxHygiene);
            float actual = Hygiene - prev;
            if (Mathf.Approximately(actual, 0f)) return;
            OnHygieneChanged?.Invoke(Hygiene, actual, reason);
            bool low = IsLow;
            if (low != _wasLow)
            {
                _wasLow = low;
                OnLowStateChanged?.Invoke(low);
            }
        }
    }
}
