using System;
using UnityEngine;

namespace HackKU.Core
{
    // Tracks money the player has parked in the market. Value ticks on real time
    // with a positive drift bias so holding usually pays off, but losing streaks
    // are possible. Deposits/withdrawals route through StatsManager so Checking
    // updates via the normal stats event.
    public class InvestmentManager : MonoBehaviour
    {
        public static event Action<float, float> OnInvestedChanged;
        public static InvestmentManager Instance { get; private set; }

        [SerializeField] float invested;
        public float Invested => invested;

        [Header("Fluctuation (real-time)")]
        [Tooltip("Seconds between price ticks.")]
        [SerializeField] float tickSeconds = 2f;
        [Tooltip("Chance a tick is a positive return. Markets drift up on average.")]
        [Range(0f, 1f)]
        [SerializeField] float upChance = 0.68f;
        [Tooltip("Min/max fractional gain on an up tick (e.g. 0.002 = +0.2%).")]
        [SerializeField] Vector2 upRange = new Vector2(0.002f, 0.012f);
        [Tooltip("Min/max fractional loss on a down tick (positive numbers).")]
        [SerializeField] Vector2 downRange = new Vector2(0.002f, 0.010f);

        float _nextTickAt;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnEnable() { _nextTickAt = Time.time + tickSeconds; }

        void Update()
        {
            if (invested <= 0f) { _nextTickAt = Time.time + tickSeconds; return; }
            if (Time.time < _nextTickAt) return;

            float rate = UnityEngine.Random.value < upChance
                ? UnityEngine.Random.Range(upRange.x, upRange.y)
                : -UnityEngine.Random.Range(downRange.x, downRange.y);

            float delta = invested * rate;
            invested = Mathf.Max(0f, invested + delta);
            OnInvestedChanged?.Invoke(invested, delta);

            _nextTickAt = Time.time + tickSeconds;
        }

        // Move `amount` from Checking into the invested pool. Returns the amount
        // actually moved (capped by available Checking balance).
        public float Deposit(float amount)
        {
            if (amount <= 0f) return 0f;
            var sm = StatsManager.Instance;
            if (sm == null) return 0f;
            float applied = Mathf.Min(amount, sm.Money);
            if (applied <= 0f) return 0f;
            sm.ApplyDelta(-applied, 0f, "Investment deposit");
            invested += applied;
            OnInvestedChanged?.Invoke(invested, applied);
            return applied;
        }

        // Move `amount` from the invested pool back into Checking. Returns the
        // amount actually moved (capped by the invested balance).
        public float Withdraw(float amount)
        {
            if (amount <= 0f) return 0f;
            var sm = StatsManager.Instance;
            if (sm == null) return 0f;
            float applied = Mathf.Min(amount, invested);
            if (applied <= 0f) return 0f;
            invested -= applied;
            sm.ApplyDelta(applied, 0f, "Investment withdrawal");
            OnInvestedChanged?.Invoke(invested, -applied);
            return applied;
        }

        public float WithdrawAll() => Withdraw(invested);
    }
}
