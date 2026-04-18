using System;
using UnityEngine;

namespace HackKU.Core
{
    // Tracks the player's hunger (0-100), drains continuously in real time.
    // Below lowThreshold: triggers slowdown + faster happiness drain (via other subscribers).
    // Eats via ApplyDelta from food prefabs.
    public class HungerManager : MonoBehaviour
    {
        public static HungerManager Instance { get; private set; }

        [Header("Hunger")]
        public float maxHunger = 100f;
        public float startingHunger = 100f;

        [Tooltip("Hunger points lost per real-time second.")]
        public float drainPerSecond = 0.5f;

        [Tooltip("At or below this value, player is 'hungry' (triggers slowdown + extra happiness drain).")]
        public float lowThreshold = 30f;

        public float Hunger { get; private set; }
        public bool IsLow => Hunger <= lowThreshold;
        public float Normalized => maxHunger <= 0f ? 0f : Mathf.Clamp01(Hunger / maxHunger);

        // (newValue, delta, reason). reason == null for passive drain so UIs can filter.
        public static event Action<float, float, string> OnHungerChanged;
        public static event Action<bool> OnLowStateChanged;

        bool _wasLow;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Hunger = Mathf.Clamp(startingHunger, 0f, maxHunger);
            _wasLow = IsLow;
        }

        void Update()
        {
            if (drainPerSecond <= 0f) return;
            float prev = Hunger;
            Hunger = Mathf.Max(0f, Hunger - drainPerSecond * Time.deltaTime);
            if (!Mathf.Approximately(prev, Hunger))
            {
                // Passive drain: reason = null so the toast HUD ignores it.
                OnHungerChanged?.Invoke(Hunger, Hunger - prev, null);
            }
            bool low = IsLow;
            if (low != _wasLow)
            {
                _wasLow = low;
                OnLowStateChanged?.Invoke(low);
            }
        }

        // Explicit change (eating, cheat). Fires a reason so the HUD shows a toast.
        public void ApplyDelta(float delta, string reason)
        {
            float prev = Hunger;
            Hunger = Mathf.Clamp(Hunger + delta, 0f, maxHunger);
            float actual = Hunger - prev;
            if (Mathf.Approximately(actual, 0f)) return;
            OnHungerChanged?.Invoke(Hunger, actual, reason);
            bool low = IsLow;
            if (low != _wasLow)
            {
                _wasLow = low;
                OnLowStateChanged?.Invoke(low);
            }
        }
    }
}
