using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HackKU.Core
{
    public enum ToastKind { Income, Bill, HungerUp, HungerDown, HappinessUp, HappinessDown, Info }

    public class ToastHUD : MonoBehaviour
    {
        public static ToastHUD Instance { get; private set; }

        [SerializeField] RectTransform stack;
        [SerializeField] int maxOnScreen = 4;
        [SerializeField] float showSeconds = 2.5f;

        const float TOAST_HEIGHT = 70f;
        const float TOAST_SPACING = 8f;

        readonly Queue<ToastPayload> _queue = new Queue<ToastPayload>();
        readonly List<RectTransform> _liveToasts = new List<RectTransform>();

        struct ToastPayload { public string amount; public string label; public ToastKind kind; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public static void Show(string amount, string label, ToastKind kind)
        {
            if (Instance == null) return;
            Instance._queue.Enqueue(new ToastPayload { amount = amount, label = label, kind = kind });
            Instance.Pump();
        }

        void Pump()
        {
            while (_liveToasts.Count < maxOnScreen && _queue.Count > 0)
            {
                var t = _queue.Dequeue();
                StartCoroutine(SpawnAndAnimate(t.amount, t.label, t.kind));
            }
        }

        void Relayout()
        {
            // Newest = on top. Compact the stack from y=-10 downward.
            for (int i = 0; i < _liveToasts.Count; i++)
            {
                if (_liveToasts[i] == null) continue;
                float y = -10f - i * (TOAST_HEIGHT + TOAST_SPACING);
                var ap = _liveToasts[i].anchoredPosition;
                _liveToasts[i].anchoredPosition = new Vector2(-10f, y);
            }
        }

        IEnumerator SpawnAndAnimate(string amount, string label, ToastKind kind)
        {
            var go = new GameObject("Toast", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(stack != null ? stack : (RectTransform)transform, false);
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = new Vector2(280, TOAST_HEIGHT);

            // Insert at top of stack: shift others down, this one at y=-10.
            _liveToasts.Insert(0, rt);
            Relayout();

            var bg = go.AddComponent<Image>();
            bg.color = BgColor(kind);

            var amountGo = new GameObject("Amount", typeof(RectTransform));
            var amountRt = (RectTransform)amountGo.transform;
            amountRt.SetParent(rt, false);
            amountRt.anchorMin = new Vector2(0f, 0f);
            amountRt.anchorMax = new Vector2(1f, 1f);
            amountRt.offsetMin = new Vector2(12, 26);
            amountRt.offsetMax = new Vector2(-12, -6);
            var amountText = amountGo.AddComponent<Text>();
            amountText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            amountText.fontStyle = FontStyle.Bold;
            amountText.fontSize = 34;
            amountText.alignment = TextAnchor.MiddleCenter;
            amountText.color = FgColor(kind);
            amountText.text = amount;

            var labelGo = new GameObject("Label", typeof(RectTransform));
            var labelRt = (RectTransform)labelGo.transform;
            labelRt.SetParent(rt, false);
            labelRt.anchorMin = new Vector2(0f, 0f);
            labelRt.anchorMax = new Vector2(1f, 1f);
            labelRt.offsetMin = new Vector2(12, 6);
            labelRt.offsetMax = new Vector2(-12, -40);
            var labelText = labelGo.AddComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 18;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            labelText.text = label;

            // Slide in from off-screen-right.
            var target = rt.anchoredPosition;
            rt.anchoredPosition = new Vector2(target.x + 340f, target.y);
            float t = 0f;
            const float inDur = 0.22f;
            while (t < inDur)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, t / inDur);
                // NOTE: during slide-in the y coordinate may have shifted due to Relayout,
                // so re-read it each frame.
                target = new Vector2(-10f, rt.anchoredPosition.y);
                rt.anchoredPosition = new Vector2(Mathf.Lerp(target.x + 340f, target.x, u), target.y);
                yield return null;
            }

            yield return new WaitForSeconds(showSeconds);

            t = 0f;
            const float outDur = 0.25f;
            Vector3 startScale = rt.localScale;
            while (t < outDur)
            {
                t += Time.deltaTime;
                float u = t / outDur;
                bg.color = new Color(bg.color.r, bg.color.g, bg.color.b, Mathf.Lerp(bg.color.a, 0f, u));
                amountText.color = new Color(amountText.color.r, amountText.color.g, amountText.color.b, Mathf.Lerp(1f, 0f, u));
                labelText.color = new Color(labelText.color.r, labelText.color.g, labelText.color.b, Mathf.Lerp(0.9f, 0f, u));
                rt.localScale = Vector3.Lerp(startScale, startScale * 0.9f, u);
                yield return null;
            }

            _liveToasts.Remove(rt);
            Destroy(go);
            Relayout();
            Pump();
        }

        static Color BgColor(ToastKind k)
        {
            switch (k)
            {
                case ToastKind.Income:        return new Color(0.10f, 0.45f, 0.20f, 0.85f);
                case ToastKind.Bill:          return new Color(0.55f, 0.12f, 0.12f, 0.85f);
                case ToastKind.HungerUp:      return new Color(0.60f, 0.35f, 0.08f, 0.85f);
                case ToastKind.HungerDown:    return new Color(0.40f, 0.20f, 0.04f, 0.85f);
                case ToastKind.HappinessUp:   return new Color(0.45f, 0.40f, 0.10f, 0.85f);
                case ToastKind.HappinessDown: return new Color(0.30f, 0.20f, 0.50f, 0.85f);
                default:                      return new Color(0.15f, 0.15f, 0.15f, 0.85f);
            }
        }

        static Color FgColor(ToastKind k)
        {
            switch (k)
            {
                case ToastKind.Income:        return new Color(0.75f, 1f, 0.80f);
                case ToastKind.Bill:          return new Color(1f, 0.75f, 0.75f);
                case ToastKind.HungerUp:      return new Color(1f, 0.85f, 0.55f);
                case ToastKind.HungerDown:    return new Color(1f, 0.7f, 0.3f);
                case ToastKind.HappinessUp:   return new Color(1f, 1f, 0.7f);
                case ToastKind.HappinessDown: return new Color(0.9f, 0.8f, 1f);
                default:                      return Color.white;
            }
        }
    }
}
