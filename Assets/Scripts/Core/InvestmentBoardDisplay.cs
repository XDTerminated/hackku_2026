using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace HackKU.Core
{
    // Live stock-tracker on the InvestmentBoard. Shows current invested balance, the
    // latest tick delta with an up/down arrow + color, and a small sparkline of the
    // last N ticks so the player can see the trend at a glance.
    public class InvestmentBoardDisplay : MonoBehaviour
    {
        public TMP_Text balanceText;
        public TMP_Text deltaText;
        public TMP_Text tickerText;
        public RectTransform sparklineParent;
        public int sparklineSamples = 16;

        [Tooltip("Tinted green when the latest tick is up.")]
        public Color upColor = new Color(0.35f, 0.95f, 0.45f);
        [Tooltip("Tinted red when the latest tick is down.")]
        public Color downColor = new Color(0.95f, 0.45f, 0.4f);
        public Color idleColor = new Color(0.8f, 0.8f, 0.85f);

        readonly Queue<float> _history = new Queue<float>();
        readonly List<RectTransform> _barCache = new List<RectTransform>();

        void OnEnable()
        {
            InvestmentManager.OnInvestedChanged += Handle;
            Refresh();
        }

        void OnDisable()
        {
            InvestmentManager.OnInvestedChanged -= Handle;
        }

        void Handle(float newInvested, float delta)
        {
            _history.Enqueue(newInvested);
            while (_history.Count > sparklineSamples) _history.Dequeue();
            Refresh(delta);
        }

        void Refresh(float lastDelta = 0f)
        {
            float invested = InvestmentManager.Instance != null ? InvestmentManager.Instance.Invested : 0f;
            if (balanceText != null) balanceText.text = "$" + Mathf.RoundToInt(invested).ToString("N0");

            if (deltaText != null)
            {
                if (Mathf.Abs(lastDelta) < 0.01f)
                {
                    deltaText.text = "—";
                    deltaText.color = idleColor;
                }
                else if (lastDelta > 0f)
                {
                    deltaText.text = "▲ +$" + lastDelta.ToString("0.00");
                    deltaText.color = upColor;
                }
                else
                {
                    deltaText.text = "▼ -$" + Mathf.Abs(lastDelta).ToString("0.00");
                    deltaText.color = downColor;
                }
            }

            if (tickerText != null)
            {
                string state = lastDelta > 0f ? "<color=#59ee72>UP</color>" :
                               lastDelta < 0f ? "<color=#f25b58>DOWN</color>" :
                               "<color=#bbbbbb>FLAT</color>";
                tickerText.text = "MKT " + state;
            }

            DrawSparkline();
        }

        void DrawSparkline()
        {
            if (sparklineParent == null) return;
            int desired = Mathf.Max(2, sparklineSamples);

            // Lazy-create bars.
            while (_barCache.Count < desired)
            {
                var bar = new GameObject("Bar_" + _barCache.Count, typeof(RectTransform), typeof(UnityEngine.UI.Image));
                bar.transform.SetParent(sparklineParent, false);
                var img = bar.GetComponent<UnityEngine.UI.Image>();
                img.color = new Color(0.55f, 0.9f, 0.6f, 0.9f);
                var rt = (RectTransform)bar.transform;
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(0, 0);
                rt.pivot = new Vector2(0.5f, 0f);
                _barCache.Add(rt);
            }

            var vals = new List<float>(_history);
            if (vals.Count < 2)
            {
                for (int i = 0; i < _barCache.Count; i++) _barCache[i].gameObject.SetActive(false);
                return;
            }

            float min = float.MaxValue, max = float.MinValue;
            foreach (var v in vals) { if (v < min) min = v; if (v > max) max = v; }
            float range = Mathf.Max(0.01f, max - min);

            float width = sparklineParent.rect.width;
            float height = sparklineParent.rect.height;
            float barWidth = width / desired * 0.75f;

            for (int i = 0; i < _barCache.Count; i++)
            {
                var rt = _barCache[i];
                if (i < desired - vals.Count)
                {
                    rt.gameObject.SetActive(false);
                    continue;
                }
                rt.gameObject.SetActive(true);
                int idx = i - (desired - vals.Count);
                float norm = (vals[idx] - min) / range;
                float h = Mathf.Max(2f, norm * height);
                rt.sizeDelta = new Vector2(barWidth, h);
                float x = (i + 0.5f) * (width / desired);
                rt.anchoredPosition = new Vector2(x - width * 0.5f + width * 0.5f * 0f, 0f);
                rt.anchoredPosition = new Vector2((i + 0.5f) * (width / desired), 0f);
                // Tint based on rising or falling from previous sample.
                var img = rt.GetComponent<UnityEngine.UI.Image>();
                if (idx == 0) img.color = new Color(0.7f, 0.7f, 0.75f, 0.9f);
                else img.color = vals[idx] >= vals[idx - 1] ? upColor : downColor;
            }
        }
    }
}
