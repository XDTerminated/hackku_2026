using System;
using UnityEngine;

namespace HackKU.Core
{
    public class StatsManager : MonoBehaviour
    {
        public static event Action<StatsSnapshot> OnStatsChanged;
        public static event Action<GameOverInfo> OnGameOver;
        public static event Action<GameOverInfo> OnGameWon;

        public static StatsManager Instance { get; private set; }

        [SerializeField] private float money;
        [SerializeField] private float happiness;
        [SerializeField] private float debt;
        [SerializeField] private float startingDebt;
        [SerializeField] private float moneyLossThreshold = 0f;
        [SerializeField] private float happinessLossThreshold = 20f;
        [Tooltip("Seconds the player must stay below the money threshold before game-over fires.")]
        [SerializeField] private float moneyLossSustainSeconds = 45f;
        [Tooltip("Seconds the player must stay below the happiness threshold before game-over fires.")]
        [SerializeField] private float happinessLossSustainSeconds = 45f;
        [SerializeField] private CharacterProfile activeProfile;

        private bool gameOverFired;
        private string lastReason;
        private float _moneyBelowSince = -1f;
        private float _happinessBelowSince = -1f;

        public float MoneyDebtTimeRemaining =>
            _moneyBelowSince < 0f ? -1f : Mathf.Max(0f, moneyLossSustainSeconds - (Time.time - _moneyBelowSince));
        public float HappinessFailTimeRemaining =>
            _happinessBelowSince < 0f ? -1f : Mathf.Max(0f, happinessLossSustainSeconds - (Time.time - _happinessBelowSince));

        public float Money => money;
        public float Happiness => happiness;
        public float Debt => debt;
        public float StartingDebt => startingDebt;
        public CharacterProfile ActiveProfile => activeProfile;

        private bool gameWonFired;

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
            gameWonFired = false;
            if (profile != null)
            {
                money = profile.startingMoney;
                happiness = Mathf.Clamp(profile.startingHappiness, 0f, 100f);
                debt = Mathf.Max(0f, profile.startingDebt);
                startingDebt = debt;
            }
            lastReason = "init";
            RaiseStatsChanged();
        }

        public void ApplyDelta(float moneyDelta, float happinessDelta, string reason)
        {
            money += moneyDelta;
            // Furniture happiness bonus ONLY multiplies meaningful earned events
            // (food orders, phone-call yeses — deltas >= 1). Passive regen drift and
            // sub-unit ticks pass through unchanged so furniture doesn't create a
            // constant drip that keeps happiness permanently pinned at 100.
            float scaledHappiness = happinessDelta >= 1f
                ? HappinessMultiplierStack.ApplyToGain(happinessDelta)
                : happinessDelta;
            happiness = Mathf.Clamp(happiness + scaledHappiness, 0f, 100f);
            lastReason = reason;
            RaiseStatsChanged();
            CheckGameOver();
        }

        // Changes only the debt balance. Positive = debt grew (e.g. interest). Negative = payment.
        public void ApplyDebtDelta(float debtDelta, string reason)
        {
            debt = Mathf.Max(0f, debt + debtDelta);
            lastReason = reason;
            RaiseStatsChanged();
            CheckWin();
        }

        // Combined: pay N from checking toward debt principal in one call (used by paycheck split).
        public void ApplyPaymentToDebt(float amount, string reason)
        {
            if (amount <= 0f) return;
            money -= amount;
            debt = Mathf.Max(0f, debt - amount);
            lastReason = reason;
            RaiseStatsChanged();
            CheckWin();
        }

        private void CheckWin()
        {
            if (gameWonFired || gameOverFired) return;
            if (startingDebt <= 0f) return; // no debt to win from
            if (debt <= 0f)
            {
                gameWonFired = true;
                GameOverInfo info = new GameOverInfo
                {
                    yearReached = TimeManager.Instance != null ? TimeManager.Instance.CurrentYear : 0,
                    cause = "debt_free",
                    lesson = "You paid off your student loans. Freedom starts here."
                };
                OnGameWon?.Invoke(info);
            }
        }

        private void HandleYearTick(int year)
        {
            // Monthly paychecks / bills / happiness drift now handled by FinanceScheduler
            // so it can emit individual toasts. StatsManager is purely a store of state.
        }

        private void RaiseStatsChanged()
        {
            StatsSnapshot snap = new StatsSnapshot
            {
                money = money,
                happiness = happiness,
                debt = debt,
                startingDebt = startingDebt,
                year = TimeManager.Instance != null ? TimeManager.Instance.CurrentYear : 0,
                lastReason = lastReason
            };
            OnStatsChanged?.Invoke(snap);
        }

        private void Update()
        {
            if (gameOverFired) return;

            if (money < moneyLossThreshold)
            {
                if (_moneyBelowSince < 0f) _moneyBelowSince = Time.time;
                if (Time.time - _moneyBelowSince >= moneyLossSustainSeconds)
                {
                    FireGameOver("broke", "Spending beyond your means catches up. Build a buffer before lifestyle grows.");
                    return;
                }
            }
            else _moneyBelowSince = -1f;

            if (happiness < happinessLossThreshold)
            {
                if (_happinessBelowSince < 0f) _happinessBelowSince = Time.time;
                if (Time.time - _happinessBelowSince >= happinessLossSustainSeconds)
                {
                    FireGameOver("miserable", "Money without wellbeing is not wealth. Budget for joy, not just survival.");
                    return;
                }
            }
            else _happinessBelowSince = -1f;
        }

        private void FireGameOver(string cause, string lesson)
        {
            gameOverFired = true;
            GameOverInfo info = new GameOverInfo
            {
                yearReached = TimeManager.Instance != null ? TimeManager.Instance.CurrentYear : 0,
                cause = cause,
                lesson = lesson
            };
            OnGameOver?.Invoke(info);
        }

        private void CheckGameOver() { /* sustained check now in Update */ }
    }
}
