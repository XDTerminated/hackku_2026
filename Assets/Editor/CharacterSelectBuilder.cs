using System.Collections.Generic;
using System.IO;
using HackKU.Core;
using HackKU.Game;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace HackKU.EditorTools
{
    public static class CharacterSelectBuilder
    {
        const string CardPrefabPath = "Assets/Data/Prefabs/CharacterCard.prefab";
        const string CatalogPath = "Assets/Data/CharacterCatalog.asset";

        [MenuItem("HackKU/Build/Character Select")]
        public static void Build()
        {
            EnsureFolder("Assets/Data");
            EnsureFolder("Assets/Data/Prefabs");

            var catalog = BuildCatalog();
            var cardPrefab = BuildCardPrefab();
            BuildSelectorScene(catalog, cardPrefab);

            Debug.Log("[CharacterSelectBuilder] done.");
        }

        static CharacterCatalog BuildCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CharacterCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<CharacterCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var paths = new[]
            {
                "Assets/Data/Characters/CorporateClimber.asset",
                "Assets/Data/Characters/EasygoingBarista.asset",
                "Assets/Data/Characters/GradStudent.asset",
            };
            var list = new List<CharacterProfile>();
            foreach (var p in paths)
            {
                var prof = AssetDatabase.LoadAssetAtPath<CharacterProfile>(p);
                if (prof != null) list.Add(prof);
            }
            catalog.characters = list.ToArray();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        static GameObject BuildCardPrefab()
        {
            var root = new GameObject("CharacterCard");
            root.AddComponent<RectTransform>();
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            root.AddComponent<CanvasScaler>();
            root.AddComponent<GraphicRaycaster>();
            var xrRaycaster = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
            if (xrRaycaster != null) root.AddComponent(xrRaycaster);

            var rootRT = (RectTransform)root.transform;
            rootRT.sizeDelta = new Vector2(600, 820);
            rootRT.localScale = Vector3.one * 0.0011f;

            // --- Soft drop shadow (slightly larger, darker, offset down) -----------
            var shadow = NewUIChild(root.transform, "Shadow");
            var shadowImg = shadow.AddComponent<Image>();
            shadowImg.color = new Color(0f, 0f, 0f, 0.4f);
            shadowImg.sprite = RoundedRect(96, 96, 32);
            shadowImg.type = Image.Type.Sliced;
            shadowImg.raycastTarget = false;
            SetRect(shadow, Vector2.zero, Vector2.one, new Vector2(-10, -18), new Vector2(10, 2));

            // --- Main rounded card body --------------------------------------------
            var bg = NewUIChild(root.transform, "Background");
            var img = bg.AddComponent<Image>();
            img.color = new Color(0.10f, 0.12f, 0.17f, 1f);
            img.sprite = RoundedRect(96, 96, 28);
            img.type = Image.Type.Sliced;
            Fill(bg);

            var button = bg.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            var cb = button.colors;
            cb.normalColor = new Color(1, 1, 1, 1);
            cb.highlightedColor = new Color(1.1f, 1.05f, 0.85f, 1);
            cb.pressedColor = new Color(0.85f, 0.85f, 0.95f, 1);
            cb.selectedColor = cb.highlightedColor;
            button.colors = cb;
            button.targetGraphic = img;

            // --- Accent stripe across the top (colored per-character at bind) ------
            var stripe = NewUIChild(root.transform, "AccentStripe");
            var stripeImg = stripe.AddComponent<Image>();
            stripeImg.color = new Color(0.6f, 0.85f, 1f, 1f);
            stripeImg.sprite = RoundedRect(96, 96, 24);
            stripeImg.type = Image.Type.Sliced;
            stripeImg.raycastTarget = false;
            SetRect(stripe, new Vector2(0, 1), new Vector2(1, 1), new Vector2(18, -140), new Vector2(-18, -14));

            // --- Portrait circle (initial letter placeholder) ---------------------
            var portrait = NewUIChild(stripe.transform, "PortraitCircle");
            var portraitImg = portrait.AddComponent<Image>();
            portraitImg.color = new Color(1f, 1f, 1f, 0.95f);
            portraitImg.sprite = CircleSprite(128);
            portraitImg.raycastTarget = false;
            SetRect(portrait, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(18, -48), new Vector2(114, 48));

            var portraitLetter = NewUIChild(portrait.transform, "Letter");
            var letterText = portraitLetter.AddComponent<TextMeshProUGUI>();
            StyleTMP(letterText, 70, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.15f, 0.15f, 0.2f), "A");
            Fill(portraitLetter);

            // --- Name + tagline on the accent stripe (right of portrait) ----------
            var name = NewUIChild(stripe.transform, "Name");
            var nameText = name.AddComponent<TextMeshProUGUI>();
            StyleTMP(nameText, 48, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, new Color(0.1f, 0.1f, 0.15f), "Name");
            SetRect(name, new Vector2(0, 0), new Vector2(1, 1), new Vector2(148, 8), new Vector2(-22, -4));

            var gimmick = NewUIChild(stripe.transform, "Gimmick");
            var gimText = gimmick.AddComponent<TextMeshProUGUI>();
            StyleTMP(gimText, 24, FontStyles.Italic, TextAlignmentOptions.MidlineLeft, new Color(0.15f, 0.15f, 0.2f, 0.85f), "Gimmick");
            SetRect(gimmick, new Vector2(0, 0), new Vector2(1, 0), new Vector2(148, 12), new Vector2(-22, 48));

            // --- 3 stat tiles in a row (MONEY / DEBT / HAPPY) ---------------------
            var stats = NewUIChild(root.transform, "StatsRow");
            SetRect(stats, new Vector2(0, 1), new Vector2(1, 1), new Vector2(24, -310), new Vector2(-24, -160));

            BuildStatTile(stats.transform, "MoneyTile", "Money", "$", "CHECKING", "$0",
                new Color(0.55f, 0.85f, 0.55f),
                new Vector2(0, 0), new Vector2(0.33f, 1), new Vector2(0, 0), new Vector2(-6, 0));
            BuildStatTile(stats.transform, "DebtTile", "Debt", "!", "DEBT", "$0",
                new Color(1f, 0.55f, 0.5f),
                new Vector2(0.33f, 0), new Vector2(0.66f, 1), new Vector2(3, 0), new Vector2(-3, 0));
            BuildStatTile(stats.transform, "HappyTile", "Happiness", "", "HAPPY", "0%",
                new Color(1f, 0.8f, 0.45f),
                new Vector2(0.66f, 0), new Vector2(1, 1), new Vector2(6, 0), new Vector2(0, 0));

            // --- Description panel ------------------------------------------------
            var descPanel = NewUIChild(root.transform, "DescPanel");
            var descBg = descPanel.AddComponent<Image>();
            descBg.color = new Color(0.15f, 0.17f, 0.22f, 1f);
            descBg.sprite = RoundedRect(64, 64, 18);
            descBg.type = Image.Type.Sliced;
            descBg.raycastTarget = false;
            SetRect(descPanel, new Vector2(0, 0), new Vector2(1, 1), new Vector2(24, 130), new Vector2(-24, -320));

            var desc = NewUIChild(descPanel.transform, "Description");
            var descText = desc.AddComponent<TextMeshProUGUI>();
            StyleTMP(descText, 26, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color(0.82f, 0.85f, 0.92f), "Description...");
            descText.textWrappingMode = TextWrappingModes.Normal;
            SetRect(desc, Vector2.zero, Vector2.one, new Vector2(22, 22), new Vector2(-22, -22));

            // --- "SELECT" button-like stripe at the bottom ------------------------
            var selectStripe = NewUIChild(root.transform, "SelectStripe");
            var selectImg = selectStripe.AddComponent<Image>();
            selectImg.color = new Color(0.22f, 0.26f, 0.34f, 1f);
            selectImg.sprite = RoundedRect(64, 64, 20);
            selectImg.type = Image.Type.Sliced;
            selectImg.raycastTarget = false;
            SetRect(selectStripe, new Vector2(0, 0), new Vector2(1, 0), new Vector2(24, 22), new Vector2(-24, 114));

            var selectLabel = NewUIChild(selectStripe.transform, "Label");
            var selText = selectLabel.AddComponent<TextMeshProUGUI>();
            StyleTMP(selText, 30, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.95f, 0.75f), "SQUEEZE TRIGGER TO PICK");
            selText.characterSpacing = 3f;
            Fill(selectLabel);

            root.AddComponent<CharacterCardUI>();

            // XRSimpleInteractable (added by CardXRClick's RequireComponent) needs a Collider
            // on the same GameObject. Size it to the card so the ray can hit it.
            var col = root.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(600, 820, 4);
            col.center = Vector3.zero;

            // Belt-and-suspenders: XR ray + trigger forwards directly to the Button.
            root.AddComponent<HackKU.Game.CardXRClick>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, CardPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static void BuildStatTile(Transform parent, string goName, string valueChildName,
                                  string icon, string label, string initialValue, Color accent,
                                  Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
        {
            var tile = NewUIChild(parent, goName);
            var bg = tile.AddComponent<Image>();
            bg.color = new Color(0.14f, 0.17f, 0.22f, 1f);
            bg.sprite = RoundedRect(48, 48, 14);
            bg.type = Image.Type.Sliced;
            bg.raycastTarget = false;
            SetRect(tile, aMin, aMax, oMin, oMax);

            var header = NewUIChild(tile.transform, "Header");
            var headerText = header.AddComponent<TextMeshProUGUI>();
            string headerStr = string.IsNullOrEmpty(icon) ? label : icon + "  " + label;
            StyleTMP(headerText, 18, FontStyles.Bold, TextAlignmentOptions.Center, accent, headerStr);
            headerText.characterSpacing = 3f;
            SetRect(header, new Vector2(0, 0.55f), new Vector2(1, 1), new Vector2(6, 0), new Vector2(-6, -6));

            var value = NewUIChild(tile.transform, valueChildName);
            var valueText = value.AddComponent<TextMeshProUGUI>();
            StyleTMP(valueText, 32, FontStyles.Bold, TextAlignmentOptions.Center, accent, initialValue);
            SetRect(value, new Vector2(0, 0), new Vector2(1, 0.55f), new Vector2(6, 6), new Vector2(-6, 0));
        }

        static void BuildSelectorScene(CharacterCatalog catalog, GameObject cardPrefab)
        {
            var existing = GameObject.Find("CharacterSelect");
            if (existing != null) Object.DestroyImmediate(existing);

            var root = new GameObject("CharacterSelect");
            root.transform.position = new Vector3(0, 1.4f, 1.0f);

            var anchor = new GameObject("CardAnchor");
            anchor.transform.SetParent(root.transform, false);

            // Hint is baked into each card's bottom stripe ("👉 SQUEEZE TRIGGER TO PICK"),
            // so no separate hint banner is needed.

            var selector = root.AddComponent<CharacterSelector>();
            typeof(CharacterSelector).GetField("catalog", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(selector, catalog);
            typeof(CharacterSelector).GetField("cardAnchor", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(selector, anchor.transform);
            typeof(CharacterSelector).GetField("cardPrefab", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(selector, cardPrefab);
            // Widen the spread so the new, thicker cards have breathing room.
            typeof(CharacterSelector).GetField("arcDegrees", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(selector, 100f);
            typeof(CharacterSelector).GetField("radius", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(selector, 1.0f);

            root.AddComponent<GameBootstrap>();

            var follower = root.AddComponent<HackKU.Core.CardFanFollower>();
            follower.distance = 0.6f;
            follower.eyeHeight = 1.45f;
            follower.followSpeed = 4f;

            EditorSceneManager.MarkSceneDirty(root.scene);
        }

        // --- Sprite helpers ---------------------------------------------------------

        static readonly Dictionary<long, Sprite> _roundedCache = new Dictionary<long, Sprite>();

        static Sprite RoundedRect(int w, int h, int radius)
        {
            long key = ((long)w << 32) ^ ((long)h << 16) ^ radius;
            if (_roundedCache.TryGetValue(key, out var cached) && cached != null) return cached;
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
            s.name = "RoundedRect_" + w + "x" + h + "_r" + radius;
            _roundedCache[key] = s;
            return s;
        }

        static Sprite CircleSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - r + 0.5f) * (x - r + 0.5f) + (y - r + 0.5f) * (y - r + 0.5f));
                    float a = Mathf.Clamp01(r - d);
                    tex.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            tex.Apply();
            var s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            s.name = "Circle_" + size;
            return s;
        }

        static GameObject NewUIChild(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        static void Fill(GameObject go)
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

        static void StyleTMP(TMP_Text t, int size, FontStyles style, TextAlignmentOptions align, Color color, string value)
        {
            t.fontSize = size;
            t.fontStyle = style;
            t.alignment = align;
            t.color = color;
            t.text = value;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.raycastTarget = false;
        }

        static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                var parent = Path.GetDirectoryName(path).Replace('\\', '/');
                var name = Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
