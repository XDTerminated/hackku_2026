using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HackKU.Core
{
    // World-space recap card. Spawned at runtime in front of the player when the
    // game ends (won or lost). Shows time-to-payoff, totals, and a restart button.
    public class WinScreen : MonoBehaviour
    {
        void OnEnable()
        {
            StatsManager.OnGameWon += OnWin;
            StatsManager.OnGameOver += OnLose;
        }

        void OnDisable()
        {
            StatsManager.OnGameWon -= OnWin;
            StatsManager.OnGameOver -= OnLose;
        }

        void OnWin(GameOverInfo info) => StartCoroutine(Show(info, true));
        void OnLose(GameOverInfo info) => StartCoroutine(Show(info, false));

        IEnumerator Show(GameOverInfo info, bool won)
        {
            yield return null; // let the final stat event settle first

            // Confetti / cha-ching flourish
            if (won) SfxHub.Instance.Play("cha_ching", 0.9f);

            var cam = Camera.main;
            Vector3 pos = cam != null ? cam.transform.position + cam.transform.forward * 1.3f : Vector3.zero;
            Quaternion rot = cam != null
                ? Quaternion.LookRotation(pos - cam.transform.position, Vector3.up)
                : Quaternion.identity;

            var go = new GameObject("[WinScreen]");
            go.transform.position = pos;
            go.transform.rotation = rot;

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 200;
            go.AddComponent<CanvasScaler>();
            var gr = go.AddComponent<GraphicRaycaster>();
            var xrRaycaster = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
            if (xrRaycaster != null) go.AddComponent(xrRaycaster);
            _ = gr;

            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(640, 740);
            go.transform.localScale = Vector3.one * 0.0018f;

            // Card background
            var bg = NewChild(go.transform, "Bg");
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = won ? new Color(0.08f, 0.14f, 0.10f, 0.97f) : new Color(0.14f, 0.08f, 0.10f, 0.97f);
            bgImg.sprite = RoundedRect(96, 96, 28);
            bgImg.type = Image.Type.Sliced;
            Fill(bg);

            // Accent stripe
            var stripe = NewChild(bg.transform, "Accent");
            var stripeImg = stripe.AddComponent<Image>();
            stripeImg.color = won ? new Color(0.55f, 0.95f, 0.55f) : new Color(0.95f, 0.45f, 0.4f);
            stripeImg.sprite = RoundedRect(96, 96, 24);
            stripeImg.type = Image.Type.Sliced;
            SetRect(stripe, new Vector2(0, 1), new Vector2(1, 1), new Vector2(24, -120), new Vector2(-24, -22));

            var title = NewChild(stripe.transform, "Title");
            var titleText = title.AddComponent<TextMeshProUGUI>();
            titleText.fontSize = 64;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = new Color(0.08f, 0.12f, 0.08f);
            titleText.text = won ? "DEBT FREE" : "GAME OVER";
            titleText.characterSpacing = 8f;
            titleText.raycastTarget = false;
            Fill(title);

            // Stats block
            var tracker = RunStatsTracker.Instance;
            string time = tracker != null ? FormatTime(tracker.runDuration) : "—";
            float loan = tracker != null ? tracker.totalLoanPaid : 0f;
            float spent = tracker != null ? tracker.totalSpent : 0f;
            int bills = tracker != null ? tracker.billsPaid : 0;
            int furniture = tracker != null ? tracker.furnitureBought : 0;

            AddStatRow(bg.transform, "TIME",        time,                                         1, -150);
            AddStatRow(bg.transform, "LOAN PAID",   "$" + Mathf.RoundToInt(loan).ToString("N0"),   2, -220);
            AddStatRow(bg.transform, "TOTAL SPENT", "$" + Mathf.RoundToInt(spent).ToString("N0"),  3, -290);
            AddStatRow(bg.transform, "BILLS PAID",  bills.ToString(),                              4, -360);
            AddStatRow(bg.transform, "FURNITURE",   furniture.ToString(),                          5, -430);

            var lesson = NewChild(bg.transform, "Lesson");
            var lessonText = lesson.AddComponent<TextMeshProUGUI>();
            lessonText.fontSize = 24;
            lessonText.alignment = TextAlignmentOptions.Center;
            lessonText.fontStyle = FontStyles.Italic;
            lessonText.color = new Color(0.85f, 0.85f, 0.92f);
            lessonText.text = BuildLesson(info.cause);
            lessonText.textWrappingMode = TextWrappingModes.Normal;
            lessonText.raycastTarget = false;
            SetRect(lesson, new Vector2(0, 0), new Vector2(1, 0), new Vector2(40, 130), new Vector2(-40, 220));

            // Restart button
            var btnGo = NewChild(bg.transform, "Restart");
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = new Color(0.25f, 0.30f, 0.38f, 1f);
            btnImg.sprite = RoundedRect(64, 64, 18);
            btnImg.type = Image.Type.Sliced;
            SetRect(btnGo, new Vector2(0, 0), new Vector2(1, 0), new Vector2(80, 40), new Vector2(-80, 110));

            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            var cb = btn.colors;
            cb.highlightedColor = new Color(1.1f, 1.05f, 0.8f, 1);
            btn.colors = cb;
            btn.onClick.AddListener(() => SceneManager.LoadScene(SceneManager.GetActiveScene().name));

            // Rely on the canvas' TrackedDeviceGraphicRaycaster for VR button press.

            var btnLabel = NewChild(btnGo.transform, "Label");
            var btnText = btnLabel.AddComponent<TextMeshProUGUI>();
            btnText.fontSize = 34;
            btnText.fontStyle = FontStyles.Bold;
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.color = new Color(1f, 0.95f, 0.75f);
            btnText.text = "RESTART";
            btnText.characterSpacing = 6f;
            btnText.raycastTarget = false;
            Fill(btnLabel);

            // Pop-in bounce
            go.transform.localScale = Vector3.zero;
            float t = 0f;
            float dur = 0.45f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / dur);
                float ease = 1f - Mathf.Pow(1f - u, 3f);
                float s = 0.0018f * Mathf.Lerp(0.2f, 1.05f, ease);
                go.transform.localScale = Vector3.one * s;
                yield return null;
            }
            go.transform.localScale = Vector3.one * 0.0018f;
        }

        static string FormatTime(float t)
        {
            int total = Mathf.Max(0, Mathf.RoundToInt(t));
            int m = total / 60;
            int s = total % 60;
            return m + ":" + s.ToString("D2");
        }

        static string BuildLesson(string cause)
        {
            switch (cause)
            {
                case "debt_free": return "Freedom starts the day your last loan payment clears.";
                case "broke": return "Every dollar you let slip adds up. Budget first, splurge second.";
                case "miserable": return "Money means nothing if you never did what made you happy.";
                default: return "Balance what you earn against what actually makes life worth living.";
            }
        }

        void AddStatRow(Transform parent, string label, string value, int row, float offsetY)
        {
            var rowGo = NewChild(parent, "Row_" + row);
            var bgi = rowGo.AddComponent<Image>();
            bgi.color = new Color(0.14f, 0.17f, 0.22f, 0.9f);
            bgi.sprite = RoundedRect(48, 48, 14);
            bgi.type = Image.Type.Sliced;
            SetRect(rowGo, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(32, offsetY - 54), new Vector2(-32, offsetY));

            var l = NewChild(rowGo.transform, "L");
            var lt = l.AddComponent<TextMeshProUGUI>();
            lt.fontSize = 22;
            lt.fontStyle = FontStyles.Bold;
            lt.alignment = TextAlignmentOptions.MidlineLeft;
            lt.color = new Color(0.75f, 0.8f, 0.9f);
            lt.characterSpacing = 4f;
            lt.text = label;
            lt.raycastTarget = false;
            SetRect(l, Vector2.zero, Vector2.one, new Vector2(22, 0), new Vector2(-22, 0));

            var v = NewChild(rowGo.transform, "V");
            var vt = v.AddComponent<TextMeshProUGUI>();
            vt.fontSize = 32;
            vt.fontStyle = FontStyles.Bold;
            vt.alignment = TextAlignmentOptions.MidlineRight;
            vt.color = new Color(0.95f, 0.95f, 1f);
            vt.text = value;
            vt.raycastTarget = false;
            SetRect(v, Vector2.zero, Vector2.one, new Vector2(22, 0), new Vector2(-22, 0));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoSpawn()
        {
            if (FindFirstObjectByType<WinScreen>() != null) return;
            var go = new GameObject("[WinScreenHost]");
            DontDestroyOnLoad(go);
            go.AddComponent<WinScreen>();
        }

        // --- sprite helpers (duplicated from builder to keep this file self-contained) ---
        static System.Collections.Generic.Dictionary<long, Sprite> _cache = new System.Collections.Generic.Dictionary<long, Sprite>();
        static Sprite RoundedRect(int w, int h, int radius)
        {
            long key = ((long)w << 32) ^ ((long)h << 16) ^ radius;
            if (_cache.TryGetValue(key, out var cached) && cached != null) return cached;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float cx = Mathf.Clamp(x, radius, w - 1 - radius);
                    float cy = Mathf.Clamp(y, radius, h - 1 - radius);
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    float a = Mathf.Clamp01(radius + 0.5f - d);
                    tex.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            tex.Apply();
            var s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
            _cache[key] = s;
            return s;
        }

        static GameObject NewChild(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        static void Fill(GameObject go)
        {
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        static void SetRect(GameObject go, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
        {
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = oMin; rt.offsetMax = oMax;
        }
    }
}
