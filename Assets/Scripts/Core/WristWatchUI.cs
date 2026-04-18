using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace HackKU.Core
{
    public class WristWatchUI : MonoBehaviour
    {
        [Header("Stat labels")]
        public Text yearText;
        public Text moneyText;
        public Text happinessText;
        public Text hungerText;

        [Header("Animation")]
        [Tooltip("Pulse scale factor when a stat changes (non-passive).")]
        public float pulseScale = 1.18f;
        [Tooltip("Seconds spent scaling up then back down.")]
        public float pulseDuration = 0.35f;

        float _prevMoney;
        float _prevHappiness;
        float _prevHunger;

        void OnEnable()
        {
            StatsManager.OnStatsChanged += HandleStats;
            HungerManager.OnHungerChanged += HandleHunger;
            Refresh();
        }

        void OnDisable()
        {
            StatsManager.OnStatsChanged -= HandleStats;
            HungerManager.OnHungerChanged -= HandleHunger;
        }

        void Update()
        {
            if (TimeManager.Instance != null && yearText != null)
            {
                yearText.text = "Year " + TimeManager.Instance.CurrentYear;
            }
            if (HungerManager.Instance != null && hungerText != null)
            {
                int h = Mathf.RoundToInt(HungerManager.Instance.Hunger);
                hungerText.text = "Hunger " + h;
                hungerText.color = HungerManager.Instance.IsLow ? new Color(1f, 0.45f, 0.3f) : new Color(1f, 0.8f, 0.5f);
            }
        }

        void Refresh()
        {
            if (StatsManager.Instance != null)
            {
                _prevMoney = StatsManager.Instance.Money;
                _prevHappiness = StatsManager.Instance.Happiness;
                if (moneyText != null) moneyText.text = "$" + Mathf.RoundToInt(_prevMoney);
                if (happinessText != null) happinessText.text = Mathf.RoundToInt(_prevHappiness) + "%";
            }
            if (HungerManager.Instance != null)
            {
                _prevHunger = HungerManager.Instance.Hunger;
            }
        }

        void HandleStats(StatsSnapshot s)
        {
            if (moneyText != null) moneyText.text = "$" + Mathf.RoundToInt(s.money);
            if (happinessText != null) happinessText.text = Mathf.RoundToInt(s.happiness) + "%";
            if (yearText != null) yearText.text = "Year " + s.year;

            bool noteworthy = !string.IsNullOrEmpty(s.lastReason) && s.lastReason != "init";
            if (noteworthy)
            {
                if (!Mathf.Approximately(s.money, _prevMoney) && moneyText != null) StartCoroutine(Pulse(moneyText.transform));
                if (!Mathf.Approximately(s.happiness, _prevHappiness) && happinessText != null) StartCoroutine(Pulse(happinessText.transform));
            }
            _prevMoney = s.money;
            _prevHappiness = s.happiness;
        }

        void HandleHunger(float newVal, float delta, string reason)
        {
            if (hungerText != null && !string.IsNullOrEmpty(reason))
            {
                StartCoroutine(Pulse(hungerText.transform));
            }
            _prevHunger = newVal;
        }

        IEnumerator Pulse(Transform t)
        {
            if (t == null) yield break;
            Vector3 baseScale = t.localScale;
            Vector3 peakScale = baseScale * pulseScale;
            float half = Mathf.Max(0.05f, pulseDuration * 0.5f);
            float elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                t.localScale = Vector3.Lerp(baseScale, peakScale, elapsed / half);
                yield return null;
            }
            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                t.localScale = Vector3.Lerp(peakScale, baseScale, elapsed / half);
                yield return null;
            }
            t.localScale = baseScale;
        }
    }
}
