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

            // Background card with soft dark fill.
            var bg = NewChild(canvasGo.transform, "BG");
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.04f, 0.05f, 0.08f, 0.92f);
            SetFill(bg);

            // Top bar — year + elapsed timer.
            var top = NewChild(canvasGo.transform, "TopBar");
            SetRect(top, new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -50), new Vector2(-16, -8));

            var yearText = AddTMP(top.transform, "Year", "YEAR 0", 28, new Color(0.85f, 0.88f, 1f), TextAlignmentOptions.MidlineLeft);
            SetFill(yearText.gameObject);
            yearText.fontStyle = FontStyles.Bold;

            var elapsedText = AddTMP(top.transform, "Elapsed", "0:00", 28, new Color(0.6f, 0.85f, 1f), TextAlignmentOptions.MidlineRight);
            SetFill(elapsedText.gameObject);
            elapsedText.fontStyle = FontStyles.Bold;

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

            // DEBT row
            var debtLabel = AddTMP(canvasGo.transform, "DebtLabel", "DEBT", 22, new Color(0.55f, 0.6f, 0.7f), TextAlignmentOptions.MidlineLeft);
            SetRect(debtLabel.gameObject, new Vector2(0, 1), new Vector2(0.55f, 1), new Vector2(20, -150), new Vector2(0, -114));
            debtLabel.fontStyle = FontStyles.Bold;

            var debtValue = AddTMP(canvasGo.transform, "DebtValue", "$0", 36, new Color(1f, 0.55f, 0.55f), TextAlignmentOptions.MidlineRight);
            SetRect(debtValue.gameObject, new Vector2(0.4f, 1), new Vector2(1, 1), new Vector2(0, -156), new Vector2(-20, -110));
            debtValue.fontStyle = FontStyles.Bold;

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

            // Bottom row — happiness + hunger side by side.
            var hapText = AddTMP(canvasGo.transform, "Happiness", "HAPPINESS :) 0%", 22, new Color(1f, 0.82f, 0.45f), TextAlignmentOptions.MidlineLeft);
            SetRect(hapText.gameObject, new Vector2(0, 0), new Vector2(0.55f, 0), new Vector2(20, 56), new Vector2(0, 96));
            hapText.fontStyle = FontStyles.Bold;

            var hungerText = AddTMP(canvasGo.transform, "Hunger", "HUNGER 100", 22, new Color(0.95f, 0.75f, 0.5f), TextAlignmentOptions.MidlineRight);
            SetRect(hungerText.gameObject, new Vector2(0.45f, 0), new Vector2(1, 0), new Vector2(0, 56), new Vector2(-20, 96));
            hungerText.fontStyle = FontStyles.Bold;

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
            watch.debtText = debtValue;
            watch.debtFillImage = debtFillImg;

            var timer = canvasGo.GetComponent<SessionTimerUI>();
            if (timer == null) timer = canvasGo.AddComponent<SessionTimerUI>();
            timer.elapsedText = elapsedText;

            EditorUtility.SetDirty(canvasGo);
            EditorSceneManager.MarkSceneDirty(canvasGo.scene);
            Debug.Log("[WristWatchBuilder] rebuilt wrist UI (TMP)");
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
