using System;
using UnityEngine;

namespace HackKU.Leaderboard
{
    // Optional minimal drop-in. The real game uses MetricsUploader which reads
    // every live manager and computes a composite score. This component is kept
    // for quick debug/standalone scenes that don't have the full stack.
    [DefaultExecutionOrder(-50)]
    public class PlayerStats : MonoBehaviour
    {
        [SerializeField] private int money;
        [SerializeField] private int happiness;

        [SerializeField] private bool autoUpload = true;
        [SerializeField] private bool uploadOnStart = true;

        public event Action<int, int> OnChanged;

        public int Money
        {
            get => money;
            set { if (money != value) { money = value; Raise(); } }
        }

        public int Happiness
        {
            get => happiness;
            set { if (happiness != value) { happiness = value; Raise(); } }
        }

        public void AddMoney(int delta) => Money = money + delta;
        public void AddHappiness(int delta) => Happiness = happiness + delta;

        public void UploadNow() => LeaderboardClient.Instance.UpsertStatsNow(BuildSample());

        private MetricsSample BuildSample() => new MetricsSample
        {
            money = money,
            happiness = Mathf.Clamp(happiness, 0, 100),
            hunger = 100,
            hygiene = 100,
            debt = 0,
            startingDebt = 0,
            invested = 0,
            year = 0,
            compositeScore = Mathf.Clamp(happiness, 0, 100),
        };

        private void Start()
        {
            if (uploadOnStart && autoUpload) LeaderboardClient.Instance.UpsertStats(BuildSample());
        }

        private void OnApplicationQuit()
        {
            if (autoUpload) LeaderboardClient.Instance.UpsertStatsNow(BuildSample());
        }

        private void Raise()
        {
            OnChanged?.Invoke(money, happiness);
            if (autoUpload) LeaderboardClient.Instance.UpsertStats(BuildSample());
        }
    }
}
