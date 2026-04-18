using System.IO;
using HackKU.Core;
using HackKU.Game;
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
            var list = new System.Collections.Generic.List<CharacterProfile>();
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
            rootRT.sizeDelta = new Vector2(600, 800);
            rootRT.localScale = Vector3.one * 0.002f;

            var bg = NewUIChild(root.transform, "Background");
            var img = bg.AddComponent<Image>();
            img.color = new Color(0.08f, 0.1f, 0.14f, 0.95f);
            Fill(bg);

            var button = bg.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            var cb = button.colors;
            cb.normalColor = new Color(1, 1, 1, 1);
            cb.highlightedColor = new Color(1, 0.95f, 0.6f, 1);
            cb.pressedColor = new Color(0.8f, 0.8f, 0.9f, 1);
            button.colors = cb;
            button.targetGraphic = img;

            var name = NewUIChild(root.transform, "Name");
            var nameText = name.AddComponent<Text>();
            StyleText(nameText, 56, TextAnchor.UpperCenter, FontStyle.Bold, Color.white, "Name");
            SetRect(name, new Vector2(0, 0.82f), new Vector2(1, 1), new Vector2(20, 0), new Vector2(-20, -20));

            var gimmick = NewUIChild(root.transform, "Gimmick");
            var gimText = gimmick.AddComponent<Text>();
            StyleText(gimText, 30, TextAnchor.UpperCenter, FontStyle.Italic, new Color(1, 0.85f, 0.4f), "Gimmick");
            SetRect(gimmick, new Vector2(0, 0.73f), new Vector2(1, 0.82f), new Vector2(20, 0), new Vector2(-20, 0));

            var stats = NewUIChild(root.transform, "StatsRow");
            SetRect(stats, new Vector2(0, 0.55f), new Vector2(1, 0.72f), new Vector2(20, 0), new Vector2(-20, 0));

            var money = NewUIChild(stats.transform, "Money");
            var moneyText = money.AddComponent<Text>();
            StyleText(moneyText, 40, TextAnchor.MiddleCenter, FontStyle.Bold, new Color(0.5f, 1, 0.7f), "$0");
            SetRect(money, new Vector2(0, 0), new Vector2(0.5f, 1), new Vector2(0, 0), new Vector2(0, 0));

            var hap = NewUIChild(stats.transform, "Happiness");
            var hapText = hap.AddComponent<Text>();
            StyleText(hapText, 40, TextAnchor.MiddleCenter, FontStyle.Bold, new Color(1, 0.75f, 0.4f), "0/100");
            SetRect(hap, new Vector2(0.5f, 0), new Vector2(1, 1), new Vector2(0, 0), new Vector2(0, 0));

            var desc = NewUIChild(root.transform, "Description");
            var descText = desc.AddComponent<Text>();
            StyleText(descText, 26, TextAnchor.UpperCenter, FontStyle.Normal, new Color(0.85f, 0.85f, 0.9f), "Description...");
            descText.horizontalOverflow = HorizontalWrapMode.Wrap;
            SetRect(desc, new Vector2(0, 0.05f), new Vector2(1, 0.55f), new Vector2(30, 0), new Vector2(-30, 0));

            root.AddComponent<CharacterCardUI>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, CardPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static void BuildSelectorScene(CharacterCatalog catalog, GameObject cardPrefab)
        {
            var existing = GameObject.Find("CharacterSelect");
            if (existing != null) Object.DestroyImmediate(existing);

            var root = new GameObject("CharacterSelect");
            root.transform.position = new Vector3(0, 1.4f, 1.0f);

            var anchor = new GameObject("CardAnchor");
            anchor.transform.SetParent(root.transform, false);

            var selector = root.AddComponent<CharacterSelector>();
            typeof(CharacterSelector).GetField("catalog", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(selector, catalog);
            typeof(CharacterSelector).GetField("cardAnchor", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(selector, anchor.transform);
            typeof(CharacterSelector).GetField("cardPrefab", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(selector, cardPrefab);

            root.AddComponent<GameBootstrap>();

            EditorSceneManager.MarkSceneDirty(root.scene);
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

        static void StyleText(Text t, int size, TextAnchor align, FontStyle style, Color color, string value)
        {
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size;
            t.alignment = align;
            t.text = value;
            t.color = color;
            t.fontStyle = style;
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
