using System;
using UnityEngine;

namespace HackKU.Core
{
    public class StatsManager : MonoBehaviour
    {
        public static event Action<StatsSnapshot> OnStatsChanged;
        public static event Action<GameOverInfo> OnGameOver;

        public static StatsManager Instance { get; private set; }

        [SerializeField] private float money;
        [SerializeField] private float happiness;
        [SerializeField] private float moneyLossThreshold = -500f;
        [SerializeField] private float happinessLossThreshold = 10f;
        [SerializeField] private CharacterProfile activeProfile;

        private bool gameOverFired;
        private string lastReason;

        public float Money => money;
        public float Happiness => happiness;
        public CharacterProfile ActiveProfile => activeProfile;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnEnable()
        {
            TimeManager.OnYearTick += HandleYearTick;
        }

        private void OnDisable()
        {
            TimeManager.OnYearTick -= HandleYearTick;
        }

        public void Initialize(CharacterProfile profile)
        {
            activeProfile = profile;
            gameOverFired = false;
            if (profile != null)
            {
                money = profile.startingMoney;
                happiness = Mathf.Clamp(profile.startingHappiness, 0f, 100f);
            }
            lastReason = "init";
            RaiseStatsChanged();
        }

        public void ApplyDelta(float moneyDelta, float happinessDelta, string reason)
        {
            money += moneyDelta;
            happiness = Mathf.Clamp(happiness + happinessDelta, 0f, 100f);
            lastReason = reason;
            RaiseStatsChanged();
            CheckGameOver();
        }

        private void HandleYearTick(int year)
        {
            if (activeProfile == null)
            {
                return;
            }

            float net = activeProfile.yearlyIncome - activeProfile.yearlyExpenses;
            ApplyDelta(net, activeProfile.yearlyHappinessRegen, "yearly");
        }

        private void RaiseStatsChanged()
        {
            StatsSnapshot snap = new StatsSnapshot
            {
                money = money,
                happiness = happiness,
                year = TimeManager.Instance != null ? TimeManager.Instance.CurrentYear : 0,
                lastReason = lastReason
            };
            OnStatsChanged?.Invoke(snap);
        }

        private void CheckGameOver()
        {
            if (gameOverFired)
            {
                return;
            }

            if (money <= moneyLossThreshold)
            {
                gameOverFired = true;
                GameOverInfo info = new GameOverInfo
                {
                    yearReached = TimeManager.Instance != null ? TimeManager.Instance.CurrentYear : 0,
                    cause = "broke",
                    lesson = "Spending beyond your means catches up. Build a buffer before lifestyle grows."
                };
                OnGameOver?.Invoke(info);
                return;
            }

            if (happiness <= happinessLossThreshold)
            {
                gameOverFired = true;
                GameOverInfo info = new GameOverInfo
                {
                    yearReached = TimeManager.Instance != null ? TimeManager.Instance.CurrentYear : 0,
                    cause = "miserable",
                    lesson = "Money without wellbeing is not wealth. Budget for joy, not just survival."
                };
                OnGameOver?.Invoke(info);
            }
        }
    }
}
