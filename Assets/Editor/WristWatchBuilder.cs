using HackKU.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace HackKU.EditorTools
{
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
            rt.sizeDelta = new Vector2(300, 180);
            rt.pivot = new Vector2(0.5f, 0.5f);

            ClearChildren(canvasGo.transform);

            var bg = NewUIChild(canvasGo.transform, "Background");
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.05f, 0.05f, 0.08f, 0.85f);
            SetFill(bg);

            var year = NewUIChild(canvasGo.transform, "Year");
            var yearText = year.AddComponent<Text>();
            StyleText(yearText, 36, TextAnchor.UpperCenter, "Year 0");
            SetRect(year, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -50), new Vector2(0, -10));

            var money = NewUIChild(canvasGo.transform, "Money");
            var moneyText = money.AddComponent<Text>();
            StyleText(moneyText, 30, TextAnchor.MiddleLeft, "$0");
            moneyText.color = new Color(0.6f, 1f, 0.7f);
            SetRect(money, new Vector2(0, 0.3f), new Vector2(0.5f, 0.75f), new Vector2(10, 0), new Vector2(0, 0));

            var hap = NewUIChild(canvasGo.transform, "Happiness");
            var hapText = hap.AddComponent<Text>();
            StyleText(hapText, 30, TextAnchor.MiddleRight, "0%");
            hapText.color = new Color(1f, 0.8f, 0.5f);
            SetRect(hap, new Vector2(0.5f, 0.3f), new Vector2(1f, 0.75f), new Vector2(0, 0), new Vector2(-10, 0));

            var watch = canvasGo.GetComponent<WristWatchUI>();
            if (watch == null) watch = canvasGo.AddComponent<WristWatchUI>();
            watch.yearText = yearText;
            watch.moneyText = moneyText;
            watch.happinessText = hapText;

            EditorUtility.SetDirty(canvasGo);
            EditorSceneManager.MarkSceneDirty(canvasGo.scene);
            Debug.Log("[WristWatchBuilder] built wrist watch UI");
        }

        static void ClearChildren(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) Object.DestroyImmediate(t.GetChild(i).gameObject);
        }

        static GameObject NewUIChild(Transform parent, string name)
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

        static void StyleText(Text t, int size, TextAnchor align, string value)
        {
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size;
            t.alignment = align;
            t.text = value;
            t.color = Color.white;
            t.fontStyle = FontStyle.Bold;
        }
    }
}
