using HackKU.Leaderboard;
using UnityEngine;

namespace HackKU.Core
{
    // Samples every live metric manager on a fixed cadence and pushes a
    // composite score to the leaderboard server.
    //
    // Composite (0..100):
    //   mean(happiness, hunger%, hygiene%, debtPayoff%, wealth%, longevity%)
    // where:
    //   wealth%       = clamp01((money + invested) / wealthReference) * 100
    //   debtPayoff%   = startingDebt>0 ? (1 - debt/startingDebt) * 100 : 100
    //   longevity%    = clamp01(year / longevityReferenceYears) * 100
    [DisallowMultipleComponent]
    public class MetricsUploader : MonoBehaviour
    {
        [Tooltip("Seconds between leaderboard pushes.")]
        [SerializeField] private float uploadIntervalSeconds = 10f;

        [Tooltip("Money + invested value that normalizes to 100% 'wealth'.")]
        [SerializeField] private float wealthReference = 10000f;

        [Tooltip("Years survived that normalizes to 100% 'longevity'.")]
        [SerializeField] private float longevityReferenceYears = 10f;

        [SerializeField] private bool uploadOnStart = true;
        [SerializeField] private bool uploadOnQuit = true;

        private float _nextUploadAt;

        private void Start()
        {
            _nextUploadAt = Time.unscaledTime + uploadIntervalSeconds;
            if (uploadOnStart) Push(false);
        }

        private void Update()
        {
            if (uploadIntervalSeconds <= 0f) return;
            if (Time.unscaledTime < _nextUploadAt) return;
            _nextUploadAt = Time.unscaledTime + uploadIntervalSeconds;
            Push(false);
        }

        private void OnApplicationQuit()
        {
            if (uploadOnQuit) Push(true);
        }

        public void PushNow() => Push(true);

        private void Push(bool immediate)
        {
            var sample = BuildSample();
            if (immediate) LeaderboardClient.Instance.UpsertStatsNow(sample);
            else LeaderboardClient.Instance.UpsertStats(sample);
        }

        private MetricsSample BuildSample()
        {
            var stats = StatsManager.Instance;
            var hunger = HungerManager.Instance;
            var hygiene = HygieneManager.Instance;
            var invest = InvestmentManager.Instance;
            var time = TimeManager.Instance;

            float money = stats != null ? stats.Money : 0f;
            float happiness = stats != null ? stats.Happiness : 0f;
            float debt = stats != null ? stats.Debt : 0f;
            float startingDebt = stats != null ? stats.StartingDebt : 0f;
            float invested = invest != null ? invest.Invested : 0f;
            float hungerVal = hunger != null ? hunger.Hunger : 100f;
            float hungerNorm = hunger != null ? hunger.Normalized : 1f;
            float hygieneVal = hygiene != null ? hygiene.Hygiene : 100f;
            float hygieneNorm = hygiene != null ? hygiene.Normalized : 1f;
            int year = time != null ? time.CurrentYear : 0;

            float happinessPct = Mathf.Clamp(happiness, 0f, 100f);
            float hungerPct = Mathf.Clamp01(hungerNorm) * 100f;
            float hygienePct = Mathf.Clamp01(hygieneNorm) * 100f;
            float debtPayoffPct = startingDebt > 0f
                ? Mathf.Clamp01(1f - debt / startingDebt) * 100f
                : 100f;
            float wealthPct = wealthReference > 0f
                ? Mathf.Clamp01((money + invested) / wealthReference) * 100f
                : 0f;
            float longevityPct = longevityReferenceYears > 0f
                ? Mathf.Clamp01(year / longevityReferenceYears) * 100f
                : 0f;

            float composite = (happinessPct + hungerPct + hygienePct + debtPayoffPct + wealthPct + longevityPct) / 6f;

            return new MetricsSample
            {
                money = Mathf.RoundToInt(money),
                happiness = Mathf.RoundToInt(happinessPct),
                hunger = Mathf.RoundToInt(hungerVal),
                hygiene = Mathf.RoundToInt(hygieneVal),
                debt = Mathf.RoundToInt(debt),
                startingDebt = Mathf.RoundToInt(startingDebt),
                invested = Mathf.RoundToInt(invested),
                year = year,
                compositeScore = Mathf.RoundToInt(composite),
            };
        }
    }
}
