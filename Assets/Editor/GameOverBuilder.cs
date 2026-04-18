using HackKU.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace HackKU.EditorTools
{
    public static class GameOverBuilder
    {
        [MenuItem("HackKU/Build/Game Over Screen")]
        public static void Build()
        {
            var existing = GameObject.Find("GameOverCanvas");
            if (existing != null) Object.DestroyImmediate(existing);

            var canvasGo = new GameObject("GameOverCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var rt = (RectTransform)canvasGo.transform;
            rt.sizeDelta = new Vector2(900, 600);
            rt.position = new Vector3(0f, 1.5f, 1f);
            rt.localScale = Vector3.one * 0.0015f;

            var bg = NewUIChild(canvasGo.transform, "Background");
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.02f, 0.02f, 0.05f, 0.92f);
            SetFill(bg);

            var yearGo = NewUIChild(canvasGo.transform, "YearText");
            var yearText = yearGo.AddComponent<Text>();
            StyleText(yearText, 72, TextAnchor.UpperCenter, "Year 0");
            SetRect(yearGo, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -120), new Vector2(-20, -20));

            var causeGo = NewUIChild(canvasGo.transform, "CauseText");
            var causeText = causeGo.AddComponent<Text>();
            StyleText(causeText, 42, TextAnchor.UpperCenter, "Game Over");
            causeText.color = new Color(1f, 0.5f, 0.5f);
            SetRect(causeGo, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -200), new Vector2(-20, -130));

            var lessonGo = NewUIChild(canvasGo.transform, "LessonText");
            var lessonText = lessonGo.AddComponent<Text>();
            StyleText(lessonText, 28, TextAnchor.MiddleCenter, "Lesson goes here.");
            lessonText.horizontalOverflow = HorizontalWrapMode.Wrap;
            lessonText.verticalOverflow = VerticalWrapMode.Overflow;
            lessonText.color = new Color(0.9f, 0.9f, 0.85f);
            SetRect(lessonGo, new Vector2(0, 0), new Vector2(1, 1), new Vector2(60, 140), new Vector2(-60, -220));

            var btnGo = new GameObject("RestartButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(canvasGo.transform, false);
            var btnRt = (RectTransform)btnGo.transform;
            btnRt.anchorMin = new Vector2(0.5f, 0f);
            btnRt.anchorMax = new Vector2(0.5f, 0f);
            btnRt.pivot = new Vector2(0.5f, 0f);
            btnRt.sizeDelta = new Vector2(300, 90);
            btnRt.anchoredPosition = new Vector2(0, 30);
            btnGo.GetComponent<Image>().color = new Color(0.18f, 0.45f, 0.25f, 1f);

            var btnLabelGo = NewUIChild(btnGo.transform, "Label");
            var btnLabel = btnLabelGo.AddComponent<Text>();
            StyleText(btnLabel, 34, TextAnchor.MiddleCenter, "Restart");
            SetFill(btnLabelGo);

            var screen = canvasGo.AddComponent<GameOverScreen>();
            screen.screenCanvas = canvas;
            screen.yearText = yearText;
            screen.causeText = causeText;
            screen.lessonText = lessonText;
            screen.restartButton = btnGo.GetComponent<Button>();

            canvasGo.SetActive(false);

            EditorUtility.SetDirty(canvasGo);
            EditorSceneManager.MarkSceneDirty(canvasGo.scene);
            Debug.Log("[GameOverBuilder] built GameOverCanvas");
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

        static void SetRect(GameObject go, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
        {
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = oMin;
            rt.offsetMax = oMax;
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
