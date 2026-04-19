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
        public TMP_Text hygieneText;
        public Image happinessFillImage;

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

        // Guards so we only rewrite TMP text (which dirties mesh + allocates) when
        // the integer-displayed value actually changes, not every frame.
        int _shownYear = int.MinValue;
        int _shownHunger = int.MinValue;
        int _shownHygiene = int.MinValue;
        int _shownInvested = int.MinValue;
        bool _hungerLowShown;
        bool _hygieneLowShown;
        bool _investedTrendActive;

        static readonly Color HungerLowColor = new Color(1f, 0.45f, 0.3f);
        static readonly Color HungerOkColor = new Color(0.95f, 0.75f, 0.5f);
        static readonly Color HygieneLowColor = new Color(1f, 0.5f, 0.5f);
        static readonly Color HygieneOkColor = new Color(0.6f, 0.85f, 1f);

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
            var tm = TimeManager.Instance;
            if (tm != null && yearText != null && tm.CurrentYear != _shownYear)
            {
                _shownYear = tm.CurrentYear;
                yearText.text = "YEAR " + _shownYear;
            }

            var hm = HungerManager.Instance;
            if (hm != null && hungerText != null)
            {
                int h = Mathf.RoundToInt(hm.Hunger);
                if (h != _shownHunger)
                {
                    _shownHunger = h;
                    hungerText.text = "HUNGER " + h;
                }
                bool low = hm.IsLow;
                if (low != _hungerLowShown)
                {
                    _hungerLowShown = low;
                    hungerText.color = low ? HungerLowColor : HungerOkColor;
                }
            }

            var hyg = HygieneManager.Instance;
            if (hyg != null && hygieneText != null)
            {
                int v = Mathf.RoundToInt(hyg.Hygiene);
                if (v != _shownHygiene)
                {
                    _shownHygiene = v;
                    hygieneText.text = "HYGIENE " + v;
                }
                bool low = hyg.IsLow;
                if (low != _hygieneLowShown)
                {
                    _hygieneLowShown = low;
                    hygieneText.color = low ? HygieneLowColor : HygieneOkColor;
                }
            }

            var im = InvestmentManager.Instance;
            if (investedText != null && im != null)
            {
                int inv = Mathf.RoundToInt(im.Invested);
                if (inv != _shownInvested)
                {
                    _shownInvested = inv;
                    investedText.text = "INVESTED " + FormatMoney(inv);
                }
                // Trend color fades back to idle a moment after the last tick.
                if (_investedTrendActive && Time.time >= _investedTrendResetAt)
                {
                    _investedTrendActive = false;
                    investedText.color = investedIdleColor;
                }
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
        // Using only widely-supported Unicode 6.0/6.1 emoji (in every modern emoji font).
        // The 🙂 and 😟 glyphs from the earlier pass were newer Unicode additions that
        // didn't always render, so they got swapped out here.
        static string HappinessEmoji(float h)
        {
            if (h >= 75f) return "\U0001F604"; // 😄 grinning face (6.0)
            if (h >= 55f) return "\U0001F60A"; // 😊 smiling face (6.0)
            if (h >= 35f) return "\U0001F610"; // 😐 neutral face (6.1)
            if (h >= 20f) return "\U0001F61E"; // 😞 disappointed (6.0)
            return "\U0001F62D";               // 😭 loudly crying (6.0)
        }

        static string FormatMoney(float m)
        {
            int rounded = Mathf.RoundToInt(m);
            return (rounded < 0 ? "-$" : "$") + Mathf.Abs(rounded).ToString("N0");
        }
        static string FormatHappiness(float h) => HappinessEmoji(h);

        void HandleStats(StatsSnapshot s)
        {
            if (moneyText != null) moneyText.text = FormatMoney(s.money);
            if (happinessText != null) happinessText.text = FormatHappiness(s.happiness);
            if (happinessFillImage != null)
                happinessFillImage.fillAmount = Mathf.Clamp01(s.happiness / 100f);
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
            int inv = Mathf.RoundToInt(newInvested);
            if (inv != _shownInvested)
            {
                _shownInvested = inv;
                investedText.text = "INVESTED " + FormatMoney(inv);
            }
            if (delta > 0f) investedText.color = investedUpColor;
            else if (delta < 0f) investedText.color = investedDownColor;
            else investedText.color = investedIdleColor;
            _investedTrendResetAt = Time.time + 0.9f;
            _investedTrendActive = true;
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
