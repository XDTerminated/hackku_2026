using System.Collections.Generic;
using UnityEngine;

namespace HackKU.Core
{
    public class GhostFurnitureItem : MonoBehaviour
    {
        [Header("Identity")]
        public string itemId = "";
        public string displayName = "Furniture";

        [Header("Economy")]
        public float price = 200f;
        public float happinessBonus = 0.10f;

        [Header("Purchase UX")]
        public float holdSecondsToBuy = 1.5f;

        [Header("Ghost visual")]
        public Material ghostMaterial;

        public bool IsOwned { get; private set; }
        public float HoldProgress01 { get; private set; }

        readonly Dictionary<Renderer, Material[]> _origMats = new Dictionary<Renderer, Material[]>();
        readonly List<Collider> _solids = new List<Collider>();
        BoxCollider _hoverTrigger;

        void Awake()
        {
            if (string.IsNullOrEmpty(itemId)) itemId = displayName.ToLowerInvariant().Replace(" ", "_");
            if (GhostRegistry.IsOwned(itemId)) { IsOwned = true; return; }
            EnsureGhostMat();
            CacheAndGhost();
            BuildHoverTrigger();
        }

        void OnEnable()
        {
            if (IsOwned || _origMats.Count == 0) return;
            PaintGhost();
            ToggleSolids(false);
        }

        void EnsureGhostMat()
        {
            if (ghostMaterial == null) ghostMaterial = Resources.Load<Material>("GhostMat");
            if (ghostMaterial == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                ghostMaterial = new Material(shader);
                Color c = new Color(0.6f, 0.75f, 0.9f, 0.3f);
                if (ghostMaterial.HasProperty("_BaseColor")) ghostMaterial.SetColor("_BaseColor", c);
                if (ghostMaterial.HasProperty("_Color")) ghostMaterial.SetColor("_Color", c);
            }
            ConfigureTransparency(ghostMaterial);
        }

        static void ConfigureTransparency(Material m)
        {
            if (m == null) return;
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
            if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 0f);
            if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
            if (m.HasProperty("_AlphaClip")) m.SetFloat("_AlphaClip", 0f);
            m.DisableKeyword("_SURFACE_TYPE_OPAQUE");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            m.DisableKeyword("_ALPHABLEND_ON");
            m.renderQueue = 3000;
        }

        void CacheAndGhost()
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (r == null || r is ParticleSystemRenderer) continue;
                _origMats[r] = r.sharedMaterials;
            }
            foreach (var c in GetComponentsInChildren<Collider>(true))
            {
                if (c == null || c.isTrigger) continue;
                _solids.Add(c);
            }
            PaintGhost();
            ToggleSolids(false);
        }

        void PaintGhost()
        {
            foreach (var kv in _origMats)
            {
                if (kv.Key == null) continue;
                var arr = new Material[kv.Value.Length];
                for (int i = 0; i < arr.Length; i++) arr[i] = ghostMaterial;
                kv.Key.sharedMaterials = arr;
            }
        }

        void ToggleSolids(bool on)
        {
            foreach (var c in _solids) if (c != null) c.enabled = on;
        }

        // Adds a box trigger matching the item's world bounds so the XR aim-raycast has
        // something to detect while the real colliders are disabled. Player walks through
        // triggers freely, so it doesn't interfere with "walk through the ghost".
        void BuildHoverTrigger()
        {
            var b = GetWorldBounds();
            if (b.size.sqrMagnitude < 0.0001f) return;
            _hoverTrigger = gameObject.AddComponent<BoxCollider>();
            _hoverTrigger.isTrigger = true;
            // BoxCollider center/size are in local space, so convert world bounds.
            Vector3 localCenter = transform.InverseTransformPoint(b.center);
            Vector3 localSize = transform.InverseTransformVector(b.size);
            _hoverTrigger.center = localCenter;
            _hoverTrigger.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
        }

        Bounds GetWorldBounds()
        {
            bool first = true;
            Bounds b = new Bounds(transform.position, Vector3.zero);
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;
                if (first) { b = r.bounds; first = false; }
                else b.Encapsulate(r.bounds);
            }
            return b;
        }

        public bool BeginHold() { if (IsOwned) return false; HoldProgress01 = 0f; return true; }

        public bool TickHold(float dt)
        {
            if (IsOwned) return false;
            HoldProgress01 = Mathf.Clamp01(HoldProgress01 + dt / Mathf.Max(0.1f, holdSecondsToBuy));
            if (HoldProgress01 >= 1f) { TryPurchase(); return true; }
            return false;
        }

        public void CancelHold() { HoldProgress01 = 0f; }

        public bool TryPurchase()
        {
            if (IsOwned) return false;
            var sm = StatsManager.Instance;
            if (sm == null) return false;
            if (sm.Money < price)
            {
                ToastHUD.Show("Not enough money", displayName + " costs $" + Mathf.Round(price), ToastKind.Bill);
                HoldProgress01 = 0f;
                return false;
            }
            sm.ApplyDelta(-price, 0f, "Bought " + displayName);
            HappinessMultiplierStack.Add(happinessBonus);
            GhostRegistry.MarkOwned(itemId);
            IsOwned = true;
            foreach (var kv in _origMats) if (kv.Key != null) kv.Key.sharedMaterials = kv.Value;
            ToggleSolids(true);
            if (_hoverTrigger != null) Destroy(_hoverTrigger);
            ToastHUD.Show("-$" + Mathf.Round(price), "Bought " + displayName + " (+" + Mathf.RoundToInt(happinessBonus * 100f) + "% happiness)", ToastKind.HappinessUp);
            return true;
        }
    }
}
