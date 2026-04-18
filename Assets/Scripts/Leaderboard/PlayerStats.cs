using System;
using UnityEngine;

namespace HackKU.Leaderboard
{
    // Optional convenience component. Drop it on a singleton GameObject and
    // mutate Money / Happiness anywhere — the leaderboard gets the latest
    // values automatically (throttled inside LeaderboardClient).
    //
    // If your game already has its own state holder, skip this and call
    // LeaderboardClient.Instance.UpsertStats(money, happiness) from there.
    [DefaultExecutionOrder(-50)]
    public class PlayerStats : MonoBehaviour
    {
        [SerializeField] private int money;
        [SerializeField] private int happiness;

        [Tooltip("Push to the leaderboard on every change. Turn off to batch manually.")]
        [SerializeField] private bool autoUpload = true;

        [Tooltip("Send the current values once on Start so the player appears on the board immediately.")]
        [SerializeField] private bool uploadOnStart = true;

        public event Action<int, int> OnChanged;

        public int Money
        {
            get => money;
            set
            {
                if (money == value) return;
                money = value;
                Raise();
            }
        }

        public int Happiness
        {
            get => happiness;
            set
            {
                if (happiness == value) return;
                happiness = value;
                Raise();
            }
        }

        public int Score => money + happiness;

        public void AddMoney(int delta) => Money = money + delta;
        public void AddHappiness(int delta) => Happiness = happiness + delta;

        public void SetBoth(int newMoney, int newHappiness)
        {
            if (money == newMoney && happiness == newHappiness) return;
            money = newMoney;
            happiness = newHappiness;
            Raise();
        }

        public void UploadNow()
        {
            LeaderboardClient.Instance.UpsertStatsNow(money, happiness);
        }

        private void Start()
        {
            if (uploadOnStart && autoUpload) LeaderboardClient.Instance.UpsertStats(money, happiness);
        }

        private void OnApplicationQuit()
        {
            if (autoUpload) LeaderboardClient.Instance.UpsertStatsNow(money, happiness);
        }

        private void Raise()
        {
            OnChanged?.Invoke(money, happiness);
            if (autoUpload) LeaderboardClient.Instance.UpsertStats(money, happiness);
        }
    }
}
