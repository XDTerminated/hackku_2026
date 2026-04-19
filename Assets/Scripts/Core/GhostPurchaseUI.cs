using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HackKU.Core
{
    // Sleek world-space buy card. Layout: centered vertical stack — item name (top),
    // radial progress ring with a dollar value in the middle, happiness pill,
    // tooltip at the bottom. Card pops in with a scale bounce when a new ghost is
    // hovered; ring pulses while holding.
    public class GhostPurchaseUI : MonoBehaviour
    {
        public TMP_Text nameText;
        public TMP_Text priceText;
        public TMP_Text bonusText;
        public TMP_Text tooltipText;
        public Image ringFill;
        public RectTransform ringRect;
        public CanvasGroup canvasGroup;

        Transform _target;
        GhostFurnitureItem _lastTarget;
        float _showAt;

        const float CardWidth = 340f;
        const float CardHeight = 360f;
        const float RingSize = 170f;

        public static GhostPurchaseUI Create()
        {
            var root = new GameObject("[GhostPurchaseUI]");
            Object.DontDestroyOnLoad(root);
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 5;
            var cg = root.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            var rt = (RectTransform)root.transform;
            rt.sizeDelta = new Vector2(CardWidth, CardHeight);
            rt.pivot = new Vector2(0.5f, 0f);  // anchor bottom-center
            root.transform.localScale = Vector3.one * 0.0028f;

            // Background card.
            var bg = NewChild(root.transform, "BG");
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.04f, 0.06f, 0.10f, 0.96f);
            FillRect(bg);

            // Top accent bar.
            var accent = NewChild(root.transform, "Accent");
            var accImg = accent.AddComponent<Image>();
            accImg.color = new Color(0.25f, 0.9f, 1f, 1f);
            SetRect(accent, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -8), new Vector2(0, 0));

            // Name — single line, auto-shrinks if too long.
            var nameText = AddTMP(root.transform, "Name", "Couch", 34, FontStyles.Bold,
                Color.white, TextAlignmentOptions.Center);
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = 18;
            nameText.fontSizeMax = 42;
            SetRect(nameText.gameObject, new Vector2(0, 0.8f), new Vector2(1, 0.97f),
                new Vector2(20, 0), new Vector2(-20, 0));

            // Ring (centered).
            var ringCenter = NewChild(root.transform, "RingCenter");
            var ringRt = (RectTransform)ringCenter.transform;
            ringRt.anchorMin = new Vector2(0.5f, 0.45f);
            ringRt.anchorMax = new Vector2(0.5f, 0.45f);
            ringRt.pivot = new Vector2(0.5f, 0.5f);
            ringRt.sizeDelta = new Vector2(RingSize, RingSize);
            ringRt.anchoredPosition = Vector2.zero;

            var ringBgImg = ringCenter.AddComponent<Image>();
            ringBgImg.sprite = MakeRingSprite(false);
            ringBgImg.color = new Color(0.16f, 0.2f, 0.28f, 0.9f);

            var ringFillGO = NewChild(ringCenter.transform, "RingFill");
            var ringFill = ringFillGO.AddComponent<Image>();
            ringFill.sprite = MakeRingSprite(true);
            ringFill.color = new Color(0.4f, 1f, 0.55f, 1f);
            ringFill.type = Image.Type.Filled;
            ringFill.fillMethod = Image.FillMethod.Radial360;
            ringFill.fillOrigin = (int)Image.Origin360.Top;
            ringFill.fillClockwise = true;
            ringFill.fillAmount = 0f;
            FillRect(ringFillGO);

            // Price inside the ring.
            var priceText = AddTMP(ringCenter.transform, "Price", "$200", 40, FontStyles.Bold,
                new Color(0.7f, 1f, 0.8f), TextAlignmentOptions.Center);
            priceText.enableAutoSizing = true;
            priceText.fontSizeMin = 18;
            priceText.fontSizeMax = 48;
            SetRect(priceText.gameObject, new Vector2(0.15f, 0.25f), new Vector2(0.85f, 0.75f),
                Vector2.zero, Vector2.zero);

            // Happiness pill.
            var bonusBg = NewChild(root.transform, "BonusBg");
            var bonusBgImg = bonusBg.AddComponent<Image>();
            bonusBgImg.color = new Color(0.15f, 0.28f, 0.18f, 0.9f);
            SetRect(bonusBg, new Vector2(0.18f, 0.2f), new Vector2(0.82f, 0.3f),
                Vector2.zero, Vector2.zero);

            var bonusText = AddTMP(root.transform, "Bonus", "+10% HAPPINESS", 20, FontStyles.Bold,
                new Color(0.75f, 1f, 0.8f), TextAlignmentOptions.Center);
            bonusText.enableAutoSizing = true;
            bonusText.fontSizeMin = 12;
            bonusText.fontSizeMax = 24;
            SetRect(bonusText.gameObject, new Vector2(0.18f, 0.2f), new Vector2(0.82f, 0.3f),
                Vector2.zero, Vector2.zero);

            // Tooltip across bottom.
            var tipBg = NewChild(root.transform, "TooltipBg");
            tipBg.AddComponent<Image>().color = new Color(0.08f, 0.11f, 0.15f, 0.95f);
            SetRect(tipBg, new Vector2(0, 0), new Vector2(1, 0.14f),
                Vector2.zero, Vector2.zero);

            var tipText = AddTMP(root.transform, "Tooltip",
                "<b>HOLD</b> to buy", 18, FontStyles.Normal,
                new Color(0.85f, 0.9f, 1f), TextAlignmentOptions.Center);
            tipText.enableAutoSizing = true;
            tipText.fontSizeMin = 12;
            tipText.fontSizeMax = 22;
            SetRect(tipText.gameObject, new Vector2(0, 0), new Vector2(1, 0.14f),
                new Vector2(12, 0), new Vector2(-12, 0));

            var ui = root.AddComponent<GhostPurchaseUI>();
            ui.nameText = nameText;
            ui.priceText = priceText;
            ui.bonusText = bonusText;
            ui.tooltipText = tipText;
            ui.ringFill = ringFill;
            ui.ringRect = ringRt;
            ui.canvasGroup = cg;
            ui.Hide();
            return ui;
        }

        // Produces a clean anti-aliased ring sprite. `fill=false` for the track,
        // `fill=true` for a slightly brighter inner ring that the radial uses.
        static Sprite _ringTrack, _ringFill;
        static Sprite MakeRingSprite(bool fill)
        {
            if (!fill && _ringTrack != null) return _ringTrack;
            if (fill && _ringFill != null) return _ringFill;
            const int S = 128;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            float cx = (S - 1) * 0.5f, cy = (S - 1) * 0.5f;
            float inner = fill ? 42f : 44f;
            float outer = fill ? 58f : 58f;
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float dx = x - cx, dy = y - cy;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(outer - d) * Mathf.Clamp01(d - inner);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            tex.Apply();
            var spr = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f));
            if (fill) _ringFill = spr; else _ringTrack = spr;
            return spr;
        }

        static GameObject NewChild(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        static void FillRect(GameObject go)
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

        static TextMeshProUGUI AddTMP(Transform parent, string name, string text, float size,
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
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Truncate;
            return t;
        }

        public void AttachTo(GhostFurnitureItem target) { _target = target != null ? target.transform : null; }
        public void Hide() { gameObject.SetActive(false); _target = null; _lastTarget = null; }

        public void Show(GhostFurnitureItem target)
        {
            if (target == null) { Hide(); return; }
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
            _target = target.transform;
            if (_lastTarget != target)
            {
                _lastTarget = target;
                _showAt = Time.unscaledTime; // trigger pop-in
            }
            if (nameText != null) nameText.text = target.displayName;
            if (priceText != null) priceText.text = "$" + Mathf.Round(target.price);
            if (bonusText != null)
                bonusText.text = "+" + Mathf.RoundToInt(target.happinessBonus * 100f) + "% HAPPINESS";
            if (tooltipText != null)
                tooltipText.text = target.HoldProgress01 > 0f
                    ? "<b>KEEP HOLDING</b>  " + Mathf.RoundToInt(target.HoldProgress01 * 100f) + "%"
                    : "<b>HOLD TRIGGER</b> to buy";
            if (ringFill != null) ringFill.fillAmount = target.HoldProgress01;
        }

        void LateUpdate()
        {
            if (_target == null) return;
            var cam = Camera.main;
            if (cam == null) return;

            var bounds = ComputeBounds(_target);
            Vector3 pos = new Vector3(bounds.center.x, bounds.max.y + 0.35f, bounds.center.z);
            transform.position = pos;

            Vector3 toCam = cam.transform.position - transform.position;
            toCam.y = 0f;
            if (toCam.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(-toCam.normalized, Vector3.up);

            // Pop-in scale bounce.
            float sinceShow = Time.unscaledTime - _showAt;
            float popK = Mathf.Clamp01(sinceShow / 0.25f);
            float popped = EaseOutBack(popK);
            transform.localScale = Vector3.one * 0.0028f * Mathf.Lerp(0.6f, 1f, popped);

            // Fade-in.
            if (canvasGroup != null) canvasGroup.alpha = Mathf.Clamp01(sinceShow / 0.2f);

            // Ring pulse while holding.
            if (ringRect != null)
            {
                float held = _lastTarget != null ? _lastTarget.HoldProgress01 : 0f;
                float pulse = 1f + (held > 0f ? Mathf.Sin(Time.unscaledTime * 12f) * 0.04f : 0f);
                ringRect.localScale = Vector3.one * pulse;
            }
        }

        static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float x = t - 1f;
            return 1f + c3 * x * x * x + c1 * x * x;
        }

        static Bounds ComputeBounds(Transform t)
        {
            bool first = true;
            var b = new Bounds(t.position, Vector3.zero);
            foreach (var r in t.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;
                if (first) { b = r.bounds; first = false; }
                else b.Encapsulate(r.bounds);
            }
            return b;
        }
    }
}
