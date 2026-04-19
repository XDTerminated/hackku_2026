using UnityEngine;

namespace HackKU.Core
{
    // Replaces StatsManager's yearly lump-sum with itemized monthly paychecks and
    // quarterly bills. Each event fires a toast via ToastHUD. Also amplifies happiness
    // drain when the player is hungry (via HungerManager.IsLow).
    public class FinanceScheduler : MonoBehaviour
    {
        public static FinanceScheduler Instance { get; private set; }

        [Header("Bills (month 0-11, amount, label) — now spawned as physical papers via BillSpawner.")]
        [SerializeField] Bill[] bills = new Bill[0];
        [Tooltip("Paychecks per year. 2 = every 6 months (slower, encourages saving).")]
        [SerializeField] int paychecksPerYear = 2;
        [Tooltip("Multiplier on the paycheck amount. 0.2 keeps income scarce so debt payoff matters.")]
        [SerializeField] float paycheckMultiplier = 0.2f;

        [Header("Student-loan debt")]
        [Tooltip("Monthly compounding interest rate on outstanding debt (0.005 = 0.5% per in-game month).")]
        [SerializeField] float monthlyInterestRate = 0.005f;

        [Header("Happiness")]
        [Tooltip("Happiness points added per month if the player is fine (hunger OK, money OK).")]
        [SerializeField] float monthlyHappinessDrift = 0f;

        [Tooltip("Happiness lost per second when HungerManager.IsLow is true.")]
        [SerializeField] float lowHungerHappinessDrainPerSecond = 0.8f;

        int _lastTotalMonths = int.MinValue;

        [System.Serializable]
        public struct Bill { public int month; public float amount; public string label; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Update()
        {
            MonthlyTick();
            HungerHappinessDrain();
        }

        void MonthlyTick()
        {
            var tm = TimeManager.Instance;
            var sm = StatsManager.Instance;
            if (tm == null || sm == null || sm.ActiveProfile == null) return;

            int year = tm.CurrentYear;
            int monthWithinYear = Mathf.Clamp(Mathf.FloorToInt(tm.YearProgress01 * 12f), 0, 11);
            int totalMonths = year * 12 + monthWithinYear;

            if (_lastTotalMonths == int.MinValue)
            {
                _lastTotalMonths = totalMonths;
                return;
            }
            if (totalMonths == _lastTotalMonths) return;

            while (_lastTotalMonths < totalMonths)
            {
                _lastTotalMonths++;
                int m = _lastTotalMonths % 12;
                ApplyMonth(m, sm);
            }
        }

        void ApplyMonth(int monthIndex, StatsManager sm)
        {
            var profile = sm.ActiveProfile;
            if (profile == null) return;

            // Tunable paycheck cadence + amount multiplier. Default: every 6 months at 60% of
            // the raw quarterly rate — deliberately tight so phone calls actually matter.
            int n = Mathf.Max(1, paychecksPerYear);
            int monthsBetween = Mathf.Max(1, 12 / n);
            if (profile.yearlyIncome > 0f && (monthIndex % monthsBetween) == 0)
            {
                // Whole paycheck lands in Checking. Paying down debt is now a deliberate
                // action — call the bank and say how much to pay.
                float amount = (profile.yearlyIncome / n) * paycheckMultiplier;
                if (amount > 0f)
                {
                    sm.ApplyDelta(amount, 0f, "Paycheck");
                    ToastHUD.Show("+$" + Mathf.Round(amount), "Paycheck", ToastKind.Income);
                }
            }

            // Monthly debt interest — silent, no toast; the wrist debt bar reflects it.
            if (monthlyInterestRate > 0f && sm.Debt > 0f)
            {
                float interest = sm.Debt * monthlyInterestRate;
                sm.ApplyDebtDelta(interest, "");
            }

            // Old monthly auto-bill loop and living-costs drain have been DELETED on purpose.
            // The only way money leaves Checking is now: physical bill pickup, food call, or bank call.
            // Even if the old `bills` serialized field still has entries in the scene file,
            // we never read it — nothing auto-deducts here.

            if (!Mathf.Approximately(monthlyHappinessDrift, 0f))
            {
                sm.ApplyDelta(0f, monthlyHappinessDrift, "");
            }
            if (!Mathf.Approximately(profile.yearlyHappinessRegen, 0f))
            {
                float perMonth = profile.yearlyHappinessRegen / 12f;
                sm.ApplyDelta(0f, perMonth, "");
            }
        }

        void HungerHappinessDrain()
        {
            if (HungerManager.Instance == null || StatsManager.Instance == null) return;
            if (!HungerManager.Instance.IsLow) return;
            float drain = lowHungerHappinessDrainPerSecond * Time.deltaTime;
            if (drain <= 0f) return;
            StatsManager.Instance.ApplyDelta(0f, -drain, "");
        }
    }
}
