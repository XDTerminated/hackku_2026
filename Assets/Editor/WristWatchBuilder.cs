using HackKU.Core;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace HackKU.EditorTools
{
    // Sleek wrist UI rebuild. Layout (no hygiene):
    //   ╭───────────────── YEAR · time ──────────────╮
    //   │  CHECKING                    $1,240         │
    //   │  ───────────────────────────────            │
    //   │  DEBT                       $18,450         │
    //   │  ▓▓▓▓▓▓▓▓░░░░░░░░░░░░                       │
    //   │  ┌── HAPPY ──┬── HUNGER ──┬── STOCKS ──┐    │
    //   │  │ 😄 74%     │  64        │  $0         │   │
    //   │  └────────────┴────────────┴────────────┘    │
    //   ╰─────────────────────────────────────────────╯
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

            // Outer rounded card.
            var bg = NewChild(canvasGo.transform, "BG");
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.05f, 0.06f, 0.10f, 0.96f);
            bgImg.sprite = RoundedSprite(96, 96, 28);
            bgImg.type = Image.Type.Sliced;
            SetFill(bg);

            // --- Top ribbon: YEAR + time (rounded pill across the top) ------------
            var ribbon = NewChild(canvasGo.transform, "Ribbon");
            var ribbonImg = ribbon.AddComponent<Image>();
            ribbonImg.color = new Color(0.09f, 0.12f, 0.18f, 1f);
            ribbonImg.sprite = RoundedSprite(64, 64, 18);
            ribbonImg.type = Image.Type.Sliced;
            SetRect(ribbon, new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -54), new Vector2(-16, -10));

            var yearText = AddTMP(ribbon.transform, "Year", "YEAR 0", 22, FontStyles.Bold,
                new Color(0.55f, 0.85f, 1f), TextAlignmentOptions.MidlineLeft);
            SetRect(yearText.gameObject, new Vector2(0, 0), new Vector2(0.5f, 1), new Vector2(18, 0), new Vector2(0, 0));
            ContainText(yearText, 12, 24);

            var elapsedText = AddTMP(ribbon.transform, "Elapsed", "0:00", 22, FontStyles.Bold,
                new Color(0.85f, 0.92f, 1f), TextAlignmentOptions.MidlineRight);
            SetRect(elapsedText.gameObject, new Vector2(0.5f, 0), new Vector2(1, 1), new Vector2(0, 0), new Vector2(-18, 0));
            ContainText(elapsedText, 12, 24);

            // --- Checking hero line -----------------------------------------------
            var checkingLabel = AddTMP(canvasGo.transform, "CheckingLabel", "CHECKING", 16, FontStyles.Bold,
                new Color(0.5f, 0.55f, 0.65f), TextAlignmentOptions.MidlineLeft);
            SetRect(checkingLabel.gameObject, new Vector2(0, 1), new Vector2(0.5f, 1),
                new Vector2(30, -92), new Vector2(0, -62));
            checkingLabel.characterSpacing = 4f;

            var moneyText = AddTMP(canvasGo.transform, "Money", "$0", 44, FontStyles.Bold,
                new Color(0.55f, 1f, 0.7f), TextAlignmentOptions.MidlineRight);
            SetRect(moneyText.gameObject, new Vector2(0.4f, 1), new Vector2(1, 1),
                new Vector2(0, -100), new Vector2(-24, -52));
            ContainText(moneyText, 20, 52);

            // --- Divider ----------------------------------------------------------
            var div = NewChild(canvasGo.transform, "Divider");
            div.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);
            SetRect(div, new Vector2(0, 1), new Vector2(1, 1), new Vector2(26, -108), new Vector2(-26, -107));

            // --- Debt row + progress bar ------------------------------------------
            var debtLabel = AddTMP(canvasGo.transform, "DebtLabel", "DEBT", 16, FontStyles.Bold,
                new Color(0.5f, 0.55f, 0.65f), TextAlignmentOptions.MidlineLeft);
            SetRect(debtLabel.gameObject, new Vector2(0, 1), new Vector2(0.5f, 1),
                new Vector2(30, -136), new Vector2(0, -112));
            debtLabel.characterSpacing = 4f;

            var debtValue = AddTMP(canvasGo.transform, "DebtValue", "$0", 34, FontStyles.Bold,
                new Color(1f, 0.58f, 0.55f), TextAlignmentOptions.MidlineRight);
            SetRect(debtValue.gameObject, new Vector2(0.4f, 1), new Vector2(1, 1),
                new Vector2(0, -142), new Vector2(-24, -108));
            ContainText(debtValue, 16, 38);

            var debtBarBg = NewChild(canvasGo.transform, "DebtBarBg");
            var debtBarBgImg = debtBarBg.AddComponent<Image>();
            debtBarBgImg.color = new Color(0.15f, 0.06f, 0.06f, 1f);
            debtBarBgImg.sprite = RoundedSprite(32, 32, 10);
            debtBarBgImg.type = Image.Type.Sliced;
            SetRect(debtBarBg, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(26, -164), new Vector2(-26, -146));

            var debtFill = NewChild(debtBarBg.transform, "DebtFill");
            var debtFillImg = debtFill.AddComponent<Image>();
            debtFillImg.color = new Color(0.95f, 0.3f, 0.3f, 1f);
            debtFillImg.sprite = RoundedSprite(32, 32, 10);
            debtFillImg.type = Image.Type.Filled;
            debtFillImg.fillMethod = Image.FillMethod.Horizontal;
            debtFillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            debtFillImg.fillAmount = 1f;
            SetFill(debtFill);

            // --- 3-tile row at the bottom: HAPPY · HUNGER · STOCKS ----------------
            TMP_Text hapText, hungerText, investedText;
            Tile(canvasGo.transform, "TileHappy", new Color(0.18f, 0.25f, 0.15f, 0.9f),
                "HAPPY", "😄",
                new Color(0.6f, 0.8f, 0.55f), new Color(0.8f, 1f, 0.75f),
                new Vector2(0, 0), new Vector2(0.34f, 0),
                new Vector2(22, 20), new Vector2(-6, 100),
                out _, out hapText);
            var happyTile = canvasGo.transform.Find("TileHappy");
            var happyBarBg = NewChild(happyTile, "BarBg");
            var happyBarBgImg = happyBarBg.AddComponent<Image>();
            happyBarBgImg.color = new Color(0.08f, 0.12f, 0.06f, 0.9f);
            happyBarBgImg.sprite = RoundedSprite(16, 16, 6);
            happyBarBgImg.type = Image.Type.Sliced;
            SetRect(happyBarBg, new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(8, 6), new Vector2(-8, 14));
            var happyFill = NewChild(happyBarBg.transform, "Fill");
            var happyFillImg = happyFill.AddComponent<Image>();
            happyFillImg.color = new Color(0.55f, 0.95f, 0.55f, 1f);
            happyFillImg.sprite = RoundedSprite(16, 16, 6);
            happyFillImg.type = Image.Type.Filled;
            happyFillImg.fillMethod = Image.FillMethod.Horizontal;
            happyFillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            happyFillImg.fillAmount = 1f;
            SetFill(happyFill);

            Tile(canvasGo.transform, "TileHunger", new Color(0.24f, 0.20f, 0.10f, 0.9f),
                "HUNGER", "100",
                new Color(0.85f, 0.7f, 0.45f), new Color(1f, 0.88f, 0.55f),
                new Vector2(0.34f, 0), new Vector2(0.66f, 0),
                new Vector2(4, 20), new Vector2(-4, 100),
                out _, out hungerText);

            Tile(canvasGo.transform, "TileInvested", new Color(0.10f, 0.20f, 0.26f, 0.9f),
                "STOCKS", "$0",
                new Color(0.5f, 0.75f, 0.9f), new Color(0.75f, 0.9f, 1f),
                new Vector2(0.66f, 0), new Vector2(1, 0),
                new Vector2(6, 20), new Vector2(-22, 100),
                out _, out investedText);

            // --- Wire components --------------------------------------------------
            var watch = canvasGo.GetComponent<WristWatchUI>();
            if (watch == null) watch = canvasGo.AddComponent<WristWatchUI>();
            watch.yearText = yearText;
            watch.moneyText = moneyText;
            watch.happinessText = hapText;
            watch.happinessFillImage = happyFillImg;
            watch.hungerText = hungerText;
            watch.investedText = investedText;
            watch.debtText = debtValue;
            watch.debtFillImage = debtFillImg;
            // Hygiene explicitly null'd — no longer displayed.
            watch.hygieneText = null;

            var timer = canvasGo.GetComponent<SessionTimerUI>();
            if (timer == null) timer = canvasGo.AddComponent<SessionTimerUI>();
            timer.elapsedText = elapsedText;

            // Delivery timer row still present — slim rounded pill tucked below debt bar.
            var delivery = NewChild(canvasGo.transform, "DeliveryTimer");
            var deliveryBg = delivery.AddComponent<Image>();
            deliveryBg.color = new Color(0.1f, 0.18f, 0.08f, 0.95f);
            deliveryBg.sprite = RoundedSprite(32, 32, 10);
            deliveryBg.type = Image.Type.Sliced;
            SetRect(delivery, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(26, -192), new Vector2(-26, -170));
            var deliveryLabel = AddTMP(delivery.transform, "Label", "DELIVERY — 0s", 16, FontStyles.Bold,
                new Color(0.75f, 1f, 0.7f), TextAlignmentOptions.Center);
            SetFill(deliveryLabel.gameObject);
            ContainText(deliveryLabel, 10, 18);
            delivery.SetActive(false);
            var deliveryUI = canvasGo.GetComponent<DeliveryTimerUI>();
            if (deliveryUI == null) deliveryUI = canvasGo.AddComponent<DeliveryTimerUI>();
            deliveryUI.root = delivery;
            deliveryUI.label = deliveryLabel;

            // Visibility controller (raise wrist to face) — keep.
            var cg = canvasGo.GetComponent<CanvasGroup>();
            if (cg == null) cg = canvasGo.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            if (canvasGo.GetComponent<HackKU.Core.WristVisibilityController>() == null)
                canvasGo.AddComponent<HackKU.Core.WristVisibilityController>();

            EditorUtility.SetDirty(canvasGo);
            EditorSceneManager.MarkSceneDirty(canvasGo.scene);
            Debug.Log("[WristWatchBuilder] rebuilt wrist UI (sleek, tiles)");
        }

        // One stat tile: rounded panel + uppercase label + value line.
        static void Tile(Transform parent, string name, Color bgColor,
                         string label, string value,
                         Color labelColor, Color valueColor,
                         Vector2 anchorMin, Vector2 anchorMax,
                         Vector2 offsetMin, Vector2 offsetMax,
                         out TMP_Text labelText, out TMP_Text valueText)
        {
            var tile = NewChild(parent, name);
            var tileImg = tile.AddComponent<Image>();
            tileImg.color = bgColor;
            tileImg.sprite = RoundedSprite(48, 48, 14);
            tileImg.type = Image.Type.Sliced;
            SetRect(tile, anchorMin, anchorMax, offsetMin, offsetMax);

            labelText = AddTMP(tile.transform, "Label", label, 13, FontStyles.Bold,
                labelColor, TextAlignmentOptions.Center);
            SetRect(labelText.gameObject, new Vector2(0, 0.55f), new Vector2(1, 1),
                new Vector2(6, 0), new Vector2(-6, -2));
            labelText.characterSpacing = 4f;
            ContainText(labelText, 9, 14);

            valueText = AddTMP(tile.transform, "Value", value, 22, FontStyles.Bold,
                valueColor, TextAlignmentOptions.Center);
            SetRect(valueText.gameObject, new Vector2(0, 0), new Vector2(1, 0.55f),
                new Vector2(6, 4), new Vector2(-6, 0));
            ContainText(valueText, 12, 26);
        }

        // Rounded-rect sprite with 9-slice borders matching the radius.
        static System.Collections.Generic.Dictionary<long, Sprite> _roundedCache =
            new System.Collections.Generic.Dictionary<long, Sprite>();
        static Sprite RoundedSprite(int w, int h, int radius)
        {
            long key = ((long)w << 32) ^ ((long)h << 16) ^ radius;
            if (_roundedCache.TryGetValue(key, out var cached) && cached != null) return cached;
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
            var spr = Sprite.Create(tex, new Rect(0, 0, w, h),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
            _roundedCache[key] = spr;
            return spr;
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

        static void ClearChildren(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(t.GetChild(i).gameObject);
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
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        static void SetRect(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
        }

        static TMP_Text AddTMP(Transform parent, string name, string text, float size,
                               FontStyles style, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            return t;
        }
    }
}
