using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HackKU.Core
{
    // Brief "what do I do?" card that appears in front of the player right after
    // character select. Auto-fades after a few seconds so it doesn't linger.
    public class ObjectiveCard : MonoBehaviour
    {
        bool _triggered;

        void Update()
        {
            if (_triggered) return;
            var sm = StatsManager.Instance;
            if (sm != null && sm.ActiveProfile != null)
            {
                _triggered = true;
                StartCoroutine(Show());
            }
        }

        IEnumerator Show()
        {
            yield return new WaitForSeconds(0.3f);

            var cam = Camera.main;
            if (cam == null) yield break;

            var go = new GameObject("[ObjectiveCard]");
            go.transform.position = cam.transform.position + cam.transform.forward * 1.4f + Vector3.up * -0.05f;
            go.transform.rotation = Quaternion.LookRotation(go.transform.position - cam.transform.position, Vector3.up);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();

            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(640, 480);
            go.transform.localScale = Vector3.one * 0.0016f;

            var bg = NewChild(go.transform, "Bg");
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.09f, 0.11f, 0.16f, 0.95f);
            bgImg.sprite = Rounded(96, 96, 28);
            bgImg.type = Image.Type.Sliced;
            Fill(bg);

            var stripe = NewChild(bg.transform, "Stripe");
            var stripeImg = stripe.AddComponent<Image>();
            stripeImg.color = new Color(1f, 0.85f, 0.35f);
            stripeImg.sprite = Rounded(96, 96, 20);
            stripeImg.type = Image.Type.Sliced;
            SetRect(stripe, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -84), new Vector2(-20, -18));

            var title = NewChild(stripe.transform, "Title");
            var titleText = title.AddComponent<TextMeshProUGUI>();
            titleText.fontSize = 30;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = new Color(0.1f, 0.12f, 0.08f);
            titleText.text = "PAY OFF YOUR STUDENT LOAN";
            titleText.characterSpacing = 2f;
            titleText.textWrappingMode = TextWrappingModes.NoWrap;
            titleText.enableAutoSizing = true;
            titleText.fontSizeMin = 18;
            titleText.fontSizeMax = 30;
            titleText.raycastTarget = false;
            SetRect(title, Vector2.zero, Vector2.one, new Vector2(16, 4), new Vector2(-16, -4));

            var body = NewChild(bg.transform, "Body");
            var bodyText = body.AddComponent<TextMeshProUGUI>();
            bodyText.fontSize = 22;
            bodyText.alignment = TextAlignmentOptions.TopLeft;
            bodyText.color = new Color(0.85f, 0.88f, 0.95f);
            bodyText.textWrappingMode = TextWrappingModes.Normal;
            bodyText.raycastTarget = false;
            bodyText.lineSpacing = 10f;
            bodyText.margin = new Vector4(6, 0, 6, 0);
            bodyText.text =
                "•  Pick up <b>bills</b> on the floor to pay them\n" +
                "•  Answer the <b>wall phone</b> when it rings\n" +
                "•  Call the <b>bank</b> to pay down debt or invest\n" +
                "•  Call to <b>order food</b> when you're hungry\n" +
                "•  Raise your <b>left wrist</b> to check stats";
            SetRect(body, new Vector2(0, 0), new Vector2(1, 1), new Vector2(36, 30), new Vector2(-36, -104));

            var cg = go.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            // Pop-in
            float t = 0f;
            while (t < 0.4f)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Clamp01(t / 0.4f);
                yield return null;
            }
            cg.alpha = 1f;

            // Hold. Also gently follow the head so it stays readable if the player turns.
            float hold = 9f;
            float elapsed = 0f;
            while (elapsed < hold)
            {
                elapsed += Time.unscaledDeltaTime;
                Vector3 target = cam.transform.position + cam.transform.forward * 1.4f + Vector3.up * -0.05f;
                go.transform.position = Vector3.Lerp(go.transform.position, target, Time.unscaledDeltaTime * 1.5f);
                Vector3 toCam = cam.transform.position - go.transform.position;
                toCam.y = 0f;
                if (toCam.sqrMagnitude > 0.0001f)
                    go.transform.rotation = Quaternion.LookRotation(-toCam.normalized, Vector3.up);
                yield return null;
            }

            // Fade out
            t = 0f;
            while (t < 0.8f)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = 1f - Mathf.Clamp01(t / 0.8f);
                yield return null;
            }
            Destroy(go);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoSpawn()
        {
            if (FindFirstObjectByType<ObjectiveCard>() != null) return;
            var go = new GameObject("[ObjectiveHost]");
            DontDestroyOnLoad(go);
            go.AddComponent<ObjectiveCard>();
        }

        // Helpers (shared pattern).
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
        static Sprite Rounded(int w, int h, int radius)
        {
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
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
        }
    }
}
