using UnityEngine;
using UnityEngine.UI;

namespace HackKU.Core
{
    public class WristWatchUI : MonoBehaviour
    {
        [Tooltip("Legacy Text components used so this works without TMP. Fill via inspector or auto-find by child name.")]
        public Text yearText;
        public Text moneyText;
        public Text happinessText;

        void OnEnable()
        {
            StatsManager.OnStatsChanged += HandleStats;
            Refresh();
        }

        void OnDisable()
        {
            StatsManager.OnStatsChanged -= HandleStats;
        }

        void Update()
        {
            if (TimeManager.Instance != null && yearText != null)
            {
                yearText.text = "Year " + TimeManager.Instance.CurrentYear;
            }
        }

        void Refresh()
        {
            if (StatsManager.Instance == null) return;
            HandleStats(new StatsSnapshot
            {
                money = StatsManager.Instance.Money,
                happiness = StatsManager.Instance.Happiness,
                year = TimeManager.Instance != null ? TimeManager.Instance.CurrentYear : 0,
                lastReason = ""
            });
        }

        void HandleStats(StatsSnapshot s)
        {
            if (moneyText != null) moneyText.text = "$" + Mathf.RoundToInt(s.money);
            if (happinessText != null) happinessText.text = Mathf.RoundToInt(s.happiness) + "%";
            if (yearText != null) yearText.text = "Year " + s.year;
        }
    }
}
