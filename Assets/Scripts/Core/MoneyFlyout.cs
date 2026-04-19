using System.Collections;
using TMPro;
using UnityEngine;

namespace HackKU.Core
{
    // Watches money changes and spawns a world-space floating "+$X" / "-$X" label
    // in front of the player's face that rises, drifts toward the wrist watch,
    // and fades. Makes every transaction feel tactile in VR.
    public class MoneyFlyout : MonoBehaviour
    {
        float _prevMoney;
        float _prevDebt;
        bool _seeded;

        void OnEnable() { StatsManager.OnStatsChanged += OnStats; }
        void OnDisable() { StatsManager.OnStatsChanged -= OnStats; }

        void OnStats(StatsSnapshot s)
        {
            if (!_seeded) { _prevMoney = s.money; _prevDebt = s.debt; _seeded = true; return; }

            float moneyDelta = s.money - _prevMoney;
            float debtDelta = s.debt - _prevDebt;
            _prevMoney = s.money;
            _prevDebt = s.debt;

            bool init = string.IsNullOrEmpty(s.lastReason) || s.lastReason == "init";
            if (init) return;

            if (Mathf.Abs(moneyDelta) >= 1f)
            {
                bool gain = moneyDelta > 0f;
                Color col = gain ? new Color(0.55f, 0.95f, 0.55f) : new Color(0.95f, 0.5f, 0.45f);
                Spawn((gain ? "+$" : "-$") + Mathf.RoundToInt(Mathf.Abs(moneyDelta)).ToString("N0"), col);
            }
            else if (debtDelta <= -1f)
            {
                Spawn("-$" + Mathf.RoundToInt(-debtDelta).ToString("N0") + " LOAN",
                    new Color(0.7f, 0.9f, 1f));
            }
        }

        void Spawn(string text, Color color)
        {
            var cam = Camera.main;
            if (cam == null) return;

            var go = new GameObject("[Flyout]");
            Vector3 startPos = cam.transform.position + cam.transform.forward * 0.6f + cam.transform.right * 0.15f + Vector3.up * -0.1f;
            go.transform.position = startPos;

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 150;
            go.AddComponent<UnityEngine.UI.CanvasScaler>();

            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(280, 80);
            go.transform.localScale = Vector3.one * 0.0022f;

            var child = new GameObject("T", typeof(RectTransform));
            child.transform.SetParent(go.transform, false);
            var tmp = child.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 52;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.text = text;
            tmp.outlineColor = new Color(0, 0, 0, 1);
            tmp.outlineWidth = 0.18f;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.raycastTarget = false;
            var crt = (RectTransform)child.transform;
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
            crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;

            StartCoroutine(Animate(go, tmp, startPos, cam.transform));
        }

        IEnumerator Animate(GameObject go, TMP_Text tmp, Vector3 start, Transform cam)
        {
            float t = 0f;
            float dur = 1.1f;
            Vector3 drift = cam.right * -0.25f + Vector3.up * 0.35f; // toward left wrist-ish
            while (t < dur && go != null)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / dur);
                go.transform.position = start + drift * u;
                // Billboard to camera
                Vector3 toCam = cam.position - go.transform.position;
                if (toCam.sqrMagnitude > 0.0001f)
                    go.transform.rotation = Quaternion.LookRotation(-toCam.normalized, Vector3.up);
                // Scale pop then hold
                float s = u < 0.2f ? Mathf.Lerp(0.4f, 1.15f, u / 0.2f)
                        : u < 0.35f ? Mathf.Lerp(1.15f, 1f, (u - 0.2f) / 0.15f)
                        : 1f;
                go.transform.localScale = Vector3.one * (0.0022f * s);
                // Fade out in last third
                if (u > 0.6f) tmp.alpha = 1f - (u - 0.6f) / 0.4f;
                yield return null;
            }
            if (go != null) Destroy(go);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoSpawn()
        {
            if (FindFirstObjectByType<MoneyFlyout>() != null) return;
            var go = new GameObject("[MoneyFlyout]");
            DontDestroyOnLoad(go);
            go.AddComponent<MoneyFlyout>();
        }
    }
}
