using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HackKU.Core
{
    public class WristWatchUI : MonoBehaviour
    {
        [Header("Stat labels")]
        public TMP_Text yearText;
        public TMP_Text moneyText;
        public TMP_Text happinessText;
        public TMP_Text hungerText;
        public TMP_Text debtText;
        public Image debtFillImage;
        public TMP_Text investedText;

        [Header("Invested colors")]
        public Color investedUpColor = new Color(0.45f, 0.9f, 0.5f);
        public Color investedDownColor = new Color(0.95f, 0.45f, 0.4f);
        public Color investedIdleColor = new Color(0.8f, 0.8f, 0.85f);

        [Header("Animation")]
        [Tooltip("Pulse scale factor when a stat changes (non-passive).")]
        public float pulseScale = 1.18f;
        [Tooltip("Seconds spent scaling up then back down.")]
        public float pulseDuration = 0.35f;

        float _prevMoney;
        float _prevHappiness;
        float _prevHunger;
        float _investedTrendResetAt;

        void OnEnable()
        {
            StatsManager.OnStatsChanged += HandleStats;
            HungerManager.OnHungerChanged += HandleHunger;
            InvestmentManager.OnInvestedChanged += HandleInvested;
            Refresh();
        }

        void OnDisable()
        {
            StatsManager.OnStatsChanged -= HandleStats;
            HungerManager.OnHungerChanged -= HandleHunger;
            InvestmentManager.OnInvestedChanged -= HandleInvested;
        }

        void Update()
        {
            if (TimeManager.Instance != null && yearText != null)
            {
                yearText.text = "YEAR " + TimeManager.Instance.CurrentYear;
            }
            if (HungerManager.Instance != null && hungerText != null)
            {
                int h = Mathf.RoundToInt(HungerManager.Instance.Hunger);
                hungerText.text = "HUNGER " + h;
                hungerText.color = HungerManager.Instance.IsLow ? new Color(1f, 0.45f, 0.3f) : new Color(0.95f, 0.75f, 0.5f);
            }
            if (investedText != null && InvestmentManager.Instance != null)
            {
                investedText.text = "INVESTED " + FormatMoney(InvestmentManager.Instance.Invested);
                // Trend color fades back to idle a moment after the last tick.
                if (Time.time >= _investedTrendResetAt)
                    investedText.color = investedIdleColor;
            }
        }

        void Refresh()
        {
            if (StatsManager.Instance != null)
            {
                _prevMoney = StatsManager.Instance.Money;
                _prevHappiness = StatsManager.Instance.Happiness;
                if (moneyText != null) moneyText.text = FormatMoney(_prevMoney);
                if (happinessText != null) happinessText.text = FormatHappiness(_prevHappiness);
            }
            if (HungerManager.Instance != null)
            {
                _prevHunger = HungerManager.Instance.Hunger;
            }
        }

        // Real Unicode emoji — rendered via the OS emoji-font fallback installed at runtime
        // by EmojiFontBootstrap. If that fallback failed to install, these show as tofu.
        static string HappinessEmoji(float h)
        {
            if (h >= 75f) return "\U0001F604"; // 😄
            if (h >= 55f) return "\U0001F642"; // 🙂
            if (h >= 35f) return "\U0001F610"; // 😐
            if (h >= 20f) return "\U0001F61F"; // 😟
            return "\U0001F622";               // 😢
        }

        static string FormatMoney(float m)
        {
            int rounded = Mathf.RoundToInt(m);
            return (rounded < 0 ? "-$" : "$") + Mathf.Abs(rounded).ToString("N0");
        }
        static string FormatHappiness(float h) => "HAPPINESS " + HappinessEmoji(h) + " " + Mathf.RoundToInt(h) + "%";

        void HandleStats(StatsSnapshot s)
        {
            if (moneyText != null) moneyText.text = FormatMoney(s.money);
            if (happinessText != null) happinessText.text = FormatHappiness(s.happiness);
            if (yearText != null) yearText.text = "YEAR " + s.year;
            if (debtText != null) debtText.text = s.debt > 0f ? FormatMoney(s.debt) : "PAID OFF";
            if (debtFillImage != null)
                debtFillImage.fillAmount = s.startingDebt > 0f ? Mathf.Clamp01(s.debt / s.startingDebt) : 0f;

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

        void HandleInvested(float newInvested, float delta)
        {
            if (investedText == null) return;
            investedText.text = "INVESTED " + FormatMoney(newInvested);
            if (delta > 0f) investedText.color = investedUpColor;
            else if (delta < 0f) investedText.color = investedDownColor;
            else investedText.color = investedIdleColor;
            _investedTrendResetAt = Time.time + 0.9f;
            // Pulse only on large deltas (deposits/withdrawals), not every tick.
            if (Mathf.Abs(delta) >= 25f) StartCoroutine(Pulse(investedText.transform));
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
