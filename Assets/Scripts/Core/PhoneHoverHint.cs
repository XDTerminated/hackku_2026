using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace HackKU.Core
{
    // Shows a floating "Hold Grip to pick up" tooltip above the wall-phone handset
    // whenever an XR controller is hovering over it. Auto-attaches at scene load
    // to any HandsetController found.
    public class PhoneHoverHint : MonoBehaviour
    {
        public string hintText = "Hold Grip to pick up";
        public float heightAboveHandset = 0.25f;

        XRGrabInteractable _grab;
        HandsetController _handset;
        Canvas _canvas;
        CanvasGroup _cg;
        TMP_Text _label;
        int _hoverCount;
        float _targetAlpha;

        void Awake()
        {
            _handset = GetComponent<HandsetController>();
            _grab = GetComponent<XRGrabInteractable>();
            if (_grab != null)
            {
                _grab.hoverEntered.AddListener(_ => { _hoverCount++; });
                _grab.hoverExited.AddListener(_ => { _hoverCount = Mathf.Max(0, _hoverCount - 1); });
            }
            BuildCanvas();
        }

        void OnDestroy()
        {
            if (_canvas != null) Destroy(_canvas.gameObject);
        }

        void BuildCanvas()
        {
            var go = new GameObject("[PhoneHint]");
            go.transform.SetParent(null, false);
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            _canvas.sortingOrder = 50;
            go.AddComponent<CanvasScaler>();

            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(420, 100);
            go.transform.localScale = Vector3.one * 0.0014f;

            _cg = go.AddComponent<CanvasGroup>();
            _cg.alpha = 0f;
            _cg.interactable = false;
            _cg.blocksRaycasts = false;

            var bg = new GameObject("Bg", typeof(RectTransform));
            bg.transform.SetParent(go.transform, false);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.08f, 0.12f, 0.18f, 0.92f);
            bgImg.sprite = Rounded(48, 48, 14);
            bgImg.type = Image.Type.Sliced;
            bgImg.raycastTarget = false;
            var brt = (RectTransform)bg.transform;
            brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
            brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;

            var lbl = new GameObject("Lbl", typeof(RectTransform));
            lbl.transform.SetParent(bg.transform, false);
            _label = lbl.AddComponent<TextMeshProUGUI>();
            _label.fontSize = 30;
            _label.fontStyle = FontStyles.Bold;
            _label.alignment = TextAlignmentOptions.Center;
            _label.color = new Color(1f, 0.95f, 0.75f);
            _label.characterSpacing = 2f;
            _label.textWrappingMode = TextWrappingModes.NoWrap;
            _label.raycastTarget = false;
            _label.text = hintText;
            var lrt = (RectTransform)lbl.transform;
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(16, 6); lrt.offsetMax = new Vector2(-16, -6);
        }

        void LateUpdate()
        {
            if (_canvas == null || _handset == null) return;

            // Never show while a call is happening or the phone is off-cradle.
            bool canShow = _handset.IsOnCradle && !_handset.IsHeld && _hoverCount > 0;
            _targetAlpha = canShow ? 1f : 0f;
            _cg.alpha = Mathf.MoveTowards(_cg.alpha, _targetAlpha, Time.deltaTime * 5f);

            // Position above the handset, billboarded to the player.
            Vector3 anchor = transform.position + Vector3.up * heightAboveHandset;
            _canvas.transform.position = anchor;
            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 toCam = cam.transform.position - anchor;
                toCam.y = 0f;
                if (toCam.sqrMagnitude > 0.0001f)
                    _canvas.transform.rotation = Quaternion.LookRotation(-toCam.normalized, Vector3.up);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoAttach()
        {
            var handsets = Object.FindObjectsByType<HandsetController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < handsets.Length; i++)
            {
                var h = handsets[i];
                if (h == null) continue;
                if (h.GetComponent<PhoneHoverHint>() != null) continue;
                h.gameObject.AddComponent<PhoneHoverHint>();
            }
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
