using HackKU.Core;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace HackKU.EditorTools
{
    // Rebuilds the world-space WristCanvas from scratch using TextMeshPro so long strings
    // never clip. Layout (top to bottom):
    //   [ YEAR 2        0:37 ]
    //   ─────────────────────
    //   CHECKING              $1,240
    //   DEBT                  $18,450
    //   [ red debt bar fill ..........]
    //   HAPPINESS :) 74%   HUNGER 64
    //   "Pay off your loans before you burn out."
    public static class WristWatchBuilder
    {
        [MenuItem("HackKU/Build/Wrist Watch UI")]
        public static void Build()
        {
            var canvasGo = GameObject.Find("WristCanvas");
            if (canvasGo == null) { Debug.LogError("[WristWatchBuilder] no WristCanvas"); return; }

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var rt = canvasGo.GetComponent<RectTransform>();
            if (rt == null) rt = canvasGo.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(420, 300);
            rt.pivot = new Vector2(0.5f, 0.5f);

            ClearChildren(canvasGo.transform);

            // Background card with soft dark fill + rounded corners.
            var bg = NewChild(canvasGo.transform, "BG");
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.04f, 0.05f, 0.08f, 0.94f);
            bgImg.sprite = MakeRoundedSprite(96, 96, 24);
            bgImg.type = Image.Type.Sliced;
            SetFill(bg);

            // Top bar — year + elapsed timer.
            var top = NewChild(canvasGo.transform, "TopBar");
            SetRect(top, new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -50), new Vector2(-16, -8));

            var yearText = AddTMP(top.transform, "Year", "YEAR 0", 28, new Color(0.85f, 0.88f, 1f), TextAlignmentOptions.MidlineLeft);
            SetFill(yearText.gameObject);
            yearText.fontStyle = FontStyles.Bold;
            ContainText(yearText, 14, 28);

            var elapsedText = AddTMP(top.transform, "Elapsed", "0:00", 28, new Color(0.6f, 0.85f, 1f), TextAlignmentOptions.MidlineRight);
            SetFill(elapsedText.gameObject);
            elapsedText.fontStyle = FontStyles.Bold;
            ContainText(elapsedText, 14, 28);

            // Thin divider line under top bar.
            var div = NewChild(canvasGo.transform, "Divider");
            var divImg = div.AddComponent<Image>();
            divImg.color = new Color(1f, 1f, 1f, 0.12f);
            SetRect(div, new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -56), new Vector2(-16, -54));

            // CHECKING row — small caps label + big dollar number right-aligned.
            var checkLabel = AddTMP(canvasGo.transform, "CheckingLabel", "CHECKING", 22, new Color(0.55f, 0.6f, 0.7f), TextAlignmentOptions.MidlineLeft);
            SetRect(checkLabel.gameObject, new Vector2(0, 1), new Vector2(0.55f, 1), new Vector2(20, -100), new Vector2(0, -64));
            checkLabel.fontStyle = FontStyles.Bold;

            var moneyText = AddTMP(canvasGo.transform, "Money", "$0", 40, new Color(0.55f, 1f, 0.65f), TextAlignmentOptions.MidlineRight);
            SetRect(moneyText.gameObject, new Vector2(0.4f, 1), new Vector2(1, 1), new Vector2(0, -108), new Vector2(-20, -60));
            moneyText.fontStyle = FontStyles.Bold;
            ContainText(moneyText, 18, 40);

            // DEBT row
            var debtLabel = AddTMP(canvasGo.transform, "DebtLabel", "DEBT", 22, new Color(0.55f, 0.6f, 0.7f), TextAlignmentOptions.MidlineLeft);
            SetRect(debtLabel.gameObject, new Vector2(0, 1), new Vector2(0.55f, 1), new Vector2(20, -150), new Vector2(0, -114));
            debtLabel.fontStyle = FontStyles.Bold;

            var debtValue = AddTMP(canvasGo.transform, "DebtValue", "$0", 36, new Color(1f, 0.55f, 0.55f), TextAlignmentOptions.MidlineRight);
            SetRect(debtValue.gameObject, new Vector2(0.4f, 1), new Vector2(1, 1), new Vector2(0, -156), new Vector2(-20, -110));
            debtValue.fontStyle = FontStyles.Bold;
            ContainText(debtValue, 16, 36);

            // Debt bar (dark track + red fill)
            var debtBarBg = NewChild(canvasGo.transform, "DebtBarBg");
            var debtBarBgImg = debtBarBg.AddComponent<Image>();
            debtBarBgImg.color = new Color(0.14f, 0.06f, 0.06f, 1f);
            SetRect(debtBarBg, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -178), new Vector2(-20, -164));

            var debtFill = NewChild(debtBarBg.transform, "DebtFill");
            var debtFillImg = debtFill.AddComponent<Image>();
            debtFillImg.color = new Color(0.9f, 0.3f, 0.3f, 1f);
            debtFillImg.sprite = MakeWhiteSprite();
            debtFillImg.type = Image.Type.Filled;
            debtFillImg.fillMethod = Image.FillMethod.Horizontal;
            debtFillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            debtFillImg.fillAmount = 1f;
            SetFill(debtFill);

            // Happiness — single full-width row (needs room for emoji + number).
            var hapText = AddTMP(canvasGo.transform, "Happiness", "HAPPINESS 0%", 20,
                new Color(1f, 0.82f, 0.45f), TextAlignmentOptions.Center);
            SetRect(hapText.gameObject, new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(20, 130), new Vector2(-20, 160));
            hapText.fontStyle = FontStyles.Bold;
            ContainText(hapText, 10, 22);

            // Hunger + Hygiene side-by-side.
            var hungerText = AddTMP(canvasGo.transform, "Hunger", "HUNGER 100", 18,
                new Color(0.95f, 0.75f, 0.5f), TextAlignmentOptions.Center);
            SetRect(hungerText.gameObject, new Vector2(0, 0), new Vector2(0.5f, 0),
                new Vector2(16, 98), new Vector2(-4, 128));
            hungerText.fontStyle = FontStyles.Bold;
            ContainText(hungerText, 10, 20);

            var hygieneText = AddTMP(canvasGo.transform, "Hygiene", "HYGIENE 100", 18,
                new Color(0.55f, 0.85f, 1f), TextAlignmentOptions.Center);
            SetRect(hygieneText.gameObject, new Vector2(0.5f, 0), new Vector2(1, 0),
                new Vector2(4, 98), new Vector2(-16, 128));
            hygieneText.fontStyle = FontStyles.Bold;
            ContainText(hygieneText, 10, 20);

            // Invested readout — compact, bottom edge.
            var investedText = AddTMP(canvasGo.transform, "Invested", "INVESTED $0", 18,
                new Color(0.75f, 0.9f, 0.8f), TextAlignmentOptions.Center);
            SetRect(investedText.gameObject, new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(16, 72), new Vector2(-16, 96));
            investedText.fontStyle = FontStyles.Bold;
            ContainText(investedText, 10, 20);

            // Delivery timer row — hidden when nothing pending.
            var delivery = NewChild(canvasGo.transform, "DeliveryTimer");
            SetRect(delivery, new Vector2(0, 0), new Vector2(1, 0), new Vector2(16, 50), new Vector2(-16, 80));
            var deliveryBg = delivery.AddComponent<Image>();
            deliveryBg.color = new Color(0.12f, 0.16f, 0.08f, 0.85f);
            var deliveryLabel = AddTMP(delivery.transform, "Label", "DELIVERY: — 0s", 20,
                new Color(0.75f, 1f, 0.7f), TextAlignmentOptions.Center);
            SetFill(deliveryLabel.gameObject);
            deliveryLabel.fontStyle = FontStyles.Bold;
            delivery.SetActive(false);
            var deliveryUI = canvasGo.GetComponent<DeliveryTimerUI>();
            if (deliveryUI == null) deliveryUI = canvasGo.AddComponent<DeliveryTimerUI>();
            deliveryUI.root = delivery;
            deliveryUI.label = deliveryLabel;

            // Footer tag line.
            var footer = AddTMP(canvasGo.transform, "Footer", "Pay off your loans before you burn out.", 18,
                new Color(0.65f, 0.68f, 0.75f), TextAlignmentOptions.Center);
            SetRect(footer.gameObject, new Vector2(0, 0), new Vector2(1, 0), new Vector2(16, 20), new Vector2(-16, 48));
            footer.fontStyle = FontStyles.Italic;

            // Wire components.
            var watch = canvasGo.GetComponent<WristWatchUI>();
            if (watch == null) watch = canvasGo.AddComponent<WristWatchUI>();
            watch.yearText = yearText;
            watch.moneyText = moneyText;
            watch.happinessText = hapText;
            watch.hungerText = hungerText;
            watch.hygieneText = hygieneText;
            watch.investedText = investedText;
            watch.debtText = debtValue;
            watch.debtFillImage = debtFillImg;

            var timer = canvasGo.GetComponent<SessionTimerUI>();
            if (timer == null) timer = canvasGo.AddComponent<SessionTimerUI>();
            timer.elapsedText = elapsedText;

            // Visibility controller — wrist UI only appears when the arm is raised to face.
            var cg = canvasGo.GetComponent<CanvasGroup>();
            if (cg == null) cg = canvasGo.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            var vis = canvasGo.GetComponent<HackKU.Core.WristVisibilityController>();
            if (vis == null) vis = canvasGo.AddComponent<HackKU.Core.WristVisibilityController>();

            EditorUtility.SetDirty(canvasGo);
            EditorSceneManager.MarkSceneDirty(canvasGo.scene);
            Debug.Log("[WristWatchBuilder] rebuilt wrist UI (TMP, rounded, visibility)");
        }

        // Procedural rounded-rect sprite with 9-slice borders so the corners stay crisp
        // at any canvas size.
        static Sprite _rounded;
        static Sprite MakeRoundedSprite(int w, int h, int radius)
        {
            if (_rounded != null) return _rounded;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float cx = Mathf.Clamp(x, radius, w - 1 - radius);
                float cy = Mathf.Clamp(y, radius, h - 1 - radius);
                float dx = x - cx, dy = y - cy;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(radius - d + 0.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            tex.Apply();
            _rounded = Sprite.Create(tex, new Rect(0, 0, w, h),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
            return _rounded;
        }

        // ---- helpers ----

        static Sprite _whiteSprite;
        static Sprite MakeWhiteSprite()
        {
            if (_whiteSprite != null) return _whiteSprite;
            var tex = Texture2D.whiteTexture;
            _whiteSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            return _whiteSprite;
        }

        static void ClearChildren(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) Object.DestroyImmediate(t.GetChild(i).gameObject);
        }

        static GameObject NewChild(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        static void SetFill(GameObject go)
        {
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static void SetRect(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        static void ContainText(TMP_Text t, float minSize, float maxSize)
        {
            if (t == null) return;
            t.enableAutoSizing = true;
            t.fontSizeMin = minSize;
            t.fontSizeMax = maxSize;
            t.overflowMode = TextOverflowModes.Ellipsis;
            t.textWrappingMode = TextWrappingModes.NoWrap;
        }

        static TMP_Text AddTMP(Transform parent, string name, string text, float size, Color color, TextAlignmentOptions align)
        {
            var go = NewChild(parent, name);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Overflow;
            return t;
        }
    }
}
