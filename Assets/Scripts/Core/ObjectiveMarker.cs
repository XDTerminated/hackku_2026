using TMPro;
using UnityEngine;

namespace HackKU.Core
{
    // Bouncing yellow arrow that hovers above a target ghost to guide the player to it.
    // Automatically hides once the linked GhostFurnitureItem is purchased.
    [ExecuteAlways]
    public class ObjectiveMarker : MonoBehaviour
    {
        public GhostFurnitureItem target;
        public string label = "BUY";
        public Color arrowColor = new Color(1f, 0.85f, 0.25f);
        public float heightAboveTarget = 0.45f;
        public float bobAmplitude = 0.1f;
        public float bobSpeed = 3f;
        [Tooltip("When true, the marker floats above the marker's own transform rather than the target's bounds — useful for path breadcrumbs.")]
        public bool useSelfPosition = false;
        [Tooltip("Uniform UI scale multiplier — smaller = smaller arrow card.")]
        public float scale = 1f;

        GameObject _canvasGo;
        TMP_Text _arrowText;
        TMP_Text _labelText;
        CanvasGroup _cg;
        float _baseY;
        Transform _camTf;
        Vector3 _cachedAnchor;
        bool _boundsCached;
        bool _shownAlpha01;
        Color _shownArrowColor;
        string _shownLabel;

        void Awake() { Build(); CacheAnchor(); ResolveCamera(); }

        void ResolveCamera()
        {
            var cam = Camera.main;
            _camTf = cam != null ? cam.transform : null;
        }

        void CacheAnchor()
        {
            if (useSelfPosition) { _cachedAnchor = transform.position; _boundsCached = true; return; }
            if (target == null) { _cachedAnchor = transform.position; _boundsCached = false; return; }
            Bounds b = new Bounds(target.transform.position, Vector3.zero);
            bool first = true;
            foreach (var r in target.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;
                if (first) { b = r.bounds; first = false; }
                else b.Encapsulate(r.bounds);
            }
            _cachedAnchor = first
                ? target.transform.position
                : new Vector3(b.center.x, b.max.y, b.center.z);
            _boundsCached = !first;
        }

        void Build()
        {
            if (_canvasGo != null) return;
            // Strip stale arrow canvases from prior ExecuteAlways passes.
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var c = transform.GetChild(i);
                if (c != null && c.name == "[ArrowCanvas]")
                {
                    if (Application.isPlaying) Destroy(c.gameObject);
                    else DestroyImmediate(c.gameObject);
                }
            }
            _canvasGo = new GameObject("[ArrowCanvas]");
            _canvasGo.transform.SetParent(transform, false);
            var canvas = _canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 10;
            var rt = (RectTransform)_canvasGo.transform;
            rt.sizeDelta = new Vector2(280, 200);
            _canvasGo.transform.localScale = Vector3.one * 0.003f;
            _cg = _canvasGo.AddComponent<CanvasGroup>();

            var arrowGO = new GameObject("Arrow", typeof(RectTransform));
            arrowGO.transform.SetParent(_canvasGo.transform, false);
            _arrowText = arrowGO.AddComponent<TextMeshProUGUI>();
            _arrowText.text = "▼";
            _arrowText.fontSize = 140;
            _arrowText.fontStyle = FontStyles.Bold;
            _arrowText.alignment = TextAlignmentOptions.Center;
            _arrowText.color = arrowColor;
            _arrowText.textWrappingMode = TextWrappingModes.NoWrap;
            var art = (RectTransform)arrowGO.transform;
            art.anchorMin = new Vector2(0, 0.35f); art.anchorMax = new Vector2(1, 1);
            art.offsetMin = Vector2.zero; art.offsetMax = Vector2.zero;

            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(_canvasGo.transform, false);
            _labelText = labelGO.AddComponent<TextMeshProUGUI>();
            _labelText.text = label;
            _labelText.fontSize = 44;
            _labelText.fontStyle = FontStyles.Bold;
            _labelText.alignment = TextAlignmentOptions.Center;
            _labelText.color = arrowColor;
            _labelText.textWrappingMode = TextWrappingModes.NoWrap;
            var lrt = (RectTransform)labelGO.transform;
            lrt.anchorMin = new Vector2(0, 0); lrt.anchorMax = new Vector2(1, 0.35f);
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
        }

        void LateUpdate()
        {
            if (_canvasGo == null) Build();
            if (useSelfPosition) _cachedAnchor = transform.position;
            else if (!_boundsCached && target != null) CacheAnchor();

            float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
            _canvasGo.transform.position = _cachedAnchor + new Vector3(0f, heightAboveTarget + bob, 0f);
            float s = Mathf.Max(0.05f, scale);
            _canvasGo.transform.localScale = Vector3.one * 0.003f * s;

            if (_camTf == null) ResolveCamera();
            if (_camTf != null)
            {
                Vector3 toCam = _camTf.position - _canvasGo.transform.position;
                toCam.y = 0f;
                if (toCam.sqrMagnitude > 0.0001f)
                    _canvasGo.transform.rotation = Quaternion.LookRotation(-toCam.normalized, Vector3.up);
            }

            // Hide when the target is bought.
            bool showing = target != null && !target.IsOwned;
            if (_cg != null)
            {
                bool shownBool = _cg.alpha > 0.5f;
                if (showing != shownBool) _cg.alpha = showing ? 1f : 0f;
            }

            if (_arrowText != null && _shownArrowColor != arrowColor)
            {
                _shownArrowColor = arrowColor;
                _arrowText.color = arrowColor;
                if (_labelText != null) _labelText.color = arrowColor;
            }
            if (_labelText != null && !ReferenceEquals(_shownLabel, label))
            {
                _shownLabel = label;
                _labelText.text = label;
            }
        }
    }
}
