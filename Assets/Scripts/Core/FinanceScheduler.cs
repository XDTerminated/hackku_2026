using UnityEngine;

namespace HackKU.Core
{
    // Replaces StatsManager's yearly lump-sum with itemized monthly paychecks and
    // quarterly bills. Each event fires a toast via ToastHUD. Also amplifies happiness
    // drain when the player is hungry (via HungerManager.IsLow).
    public class FinanceScheduler : MonoBehaviour
    {
        public static FinanceScheduler Instance { get; private set; }

        [Header("Bills (month 0-11, amount, label)")]
        [SerializeField] Bill[] bills = new[]
        {
            new Bill{ month = 2,  amount = 1400f, label = "Rent" },
            new Bill{ month = 5,  amount = 180f,  label = "Utilities" },
            new Bill{ month = 8,  amount = 80f,   label = "Internet" },
            new Bill{ month = 11, amount = 15f,   label = "Streaming" },
        };

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

            // Quarterly paycheck — fires only on months 0, 3, 6, 9 so player isn't drowned
            // in constant small paychecks. Each quarterly paycheck is yearlyIncome / 4.
            if (profile.yearlyIncome > 0f && monthIndex % 3 == 0)
            {
                float amount = profile.yearlyIncome / 4f;
                sm.ApplyDelta(amount, 0f, "Paycheck");
                ToastHUD.Show("+$" + Mathf.Round(amount), "Paycheck", ToastKind.Income);
            }

            if (profile.yearlyExpenses > 0f)
            {
                float amount = profile.yearlyExpenses / 12f;
                sm.ApplyDelta(-amount, 0f, "Living costs");
            }

            for (int i = 0; i < bills.Length; i++)
            {
                if (bills[i].month == monthIndex)
                {
                    sm.ApplyDelta(-bills[i].amount, 0f, bills[i].label);
                    ToastHUD.Show("-$" + Mathf.Round(bills[i].amount), bills[i].label, ToastKind.Bill);
                }
            }

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
