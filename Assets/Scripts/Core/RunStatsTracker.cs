using UnityEngine;

namespace HackKU.Core
{
    // Tracks per-run stats (runtime, spent, earned, bills paid, furniture bought)
    // so the win screen can show a meaningful recap. Auto-spawns on scene load.
    public class RunStatsTracker : MonoBehaviour
    {
        public static RunStatsTracker Instance { get; private set; }

        public float runStartTime;
        public float runDuration;
        public float totalSpent;
        public float totalEarned;
        public float totalLoanPaid;
        public int billsPaid;
        public int furnitureBought;
        public bool runFinished;

        float _prevMoney;
        float _prevDebt;
        bool _seeded;

        // Wipe counters so the next run reports clean stats.
        public void ResetRun()
        {
            runStartTime = Time.time;
            runDuration = 0f;
            totalSpent = 0f;
            totalEarned = 0f;
            totalLoanPaid = 0f;
            billsPaid = 0;
            furnitureBought = 0;
            runFinished = false;
            _prevMoney = 0f;
            _prevDebt = 0f;
            _seeded = false;
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            runStartTime = Time.time;
        }

        void OnEnable()
        {
            StatsManager.OnStatsChanged += OnStats;
            StatsManager.OnGameWon += OnWonOrLost;
            StatsManager.OnGameOver += OnWonOrLost;
        }

        void OnDisable()
        {
            StatsManager.OnStatsChanged -= OnStats;
            StatsManager.OnGameWon -= OnWonOrLost;
            StatsManager.OnGameOver -= OnWonOrLost;
        }

        void Update()
        {
            if (!runFinished) runDuration = Time.time - runStartTime;
        }

        void OnStats(StatsSnapshot s)
        {
            if (!_seeded)
            {
                _prevMoney = s.money;
                _prevDebt = s.debt;
                _seeded = true;
                runStartTime = Time.time;
                return;
            }

            float moneyDelta = s.money - _prevMoney;
            float debtDelta = s.debt - _prevDebt;

            if (moneyDelta <= -1f) totalSpent += -moneyDelta;
            if (moneyDelta >= 1f && !string.IsNullOrEmpty(s.lastReason) && s.lastReason != "init")
                totalEarned += moneyDelta;
            if (debtDelta <= -1f) totalLoanPaid += -debtDelta;

            if (!string.IsNullOrEmpty(s.lastReason))
            {
                string r = s.lastReason.ToLowerInvariant();
                if (moneyDelta <= -1f)
                {
                    if (r.Contains("bought ")) furnitureBought++;
                    else if (r.Contains("rent") || r.Contains("utilities") || r.Contains("internet") ||
                             r.Contains("credit") || r.Contains("streaming") || r.Contains("phone bill"))
                        billsPaid++;
                }
            }

            _prevMoney = s.money;
            _prevDebt = s.debt;
        }

        void OnWonOrLost(GameOverInfo info)
        {
            runFinished = true;
            runDuration = Time.time - runStartTime;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoSpawn()
        {
            if (FindFirstObjectByType<RunStatsTracker>() != null) return;
            var go = new GameObject("[RunStats]");
            DontDestroyOnLoad(go);
            go.AddComponent<RunStatsTracker>();
        }
    }
}
