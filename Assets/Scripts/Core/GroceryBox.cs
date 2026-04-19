using TMPro;
using UnityEngine;

namespace HackKU.Core
{
    // A sealed delivery box. Carry it inside the house, drop it on the floor — it splits into
    // the ordered quantity of individual grocery items. Outside, it's inert so the player
    // can pick it up and bring it in.
    public class GroceryBox : MonoBehaviour
    {
        public GameObject itemPrefab;
        [Tooltip("If set, Split() picks a random prefab from this list per item. Falls back to itemPrefab when empty.")]
        public GameObject[] itemPrefabPool;
        public int quantity = 1;
        public string foodName;
        public float hungerRestore;
        [Tooltip("Billboarded TMP text that shows the quantity (set by the builder).")]
        public TMP_Text quantityLabel;

        void Start()
        {
            RefreshLabel();
            var grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            if (grab != null)
            {
                grab.selectEntered.AddListener(_ => SfxHub.Instance.PlayAt("box_pickup", transform.position, 0.85f));
                grab.selectExited.AddListener(_ => SfxHub.Instance.PlayAt("box_drop", transform.position, 0.7f));
            }
        }

        public void RefreshLabel()
        {
            if (quantityLabel == null) return;
            string title = string.IsNullOrEmpty(foodName) ? "Delivery" : foodName;
            quantityLabel.text = "<b>x" + Mathf.Max(1, quantity) + "</b>\n<size=60%>" + title + "</size>";
        }

        [Tooltip("Interior house X range (min, max).")]
        public Vector2 interiorX = new Vector2(-4.5f, 1.1f);
        [Tooltip("Interior house Z range (min, max) — Z must be past the front wall.")]
        public Vector2 interiorZ = new Vector2(-1.6f, 9.3f);
        [Tooltip("Max Y at which a floor impact counts (so bounces off tall furniture don't trigger).")]
        public float maxImpactY = 0.9f;

        [Tooltip("Vertical spread when spawning items out of the box.")]
        public float itemSpread = 0.35f;

        [Tooltip("Multiplier applied to each spawned food item's localScale. 50 lands them around hand-sized in VR.")]
        public float itemScaleMultiplier = 50f;

        bool _split;

        void OnCollisionEnter(Collision col)
        {
            if (_split) return;
            if (col == null || col.contacts == null || col.contacts.Length == 0) return;
            // Only split on low, downward landings — i.e. it actually touched the floor.
            var contact = col.contacts[0];
            if (contact.point.y > maxImpactY) return;

            Vector3 p = transform.position;
            if (p.x < interiorX.x || p.x > interiorX.y) return;
            if (p.z < interiorZ.x || p.z > interiorZ.y) return;

            Split();
        }

        // Per-prefab color tint so every food item reads as what it should be even though
        // they're all untextured OBJ meshes. Matched on the prefab name (lowercased).
        static readonly System.Collections.Generic.Dictionary<string, Color> _foodTints =
            new System.Collections.Generic.Dictionary<string, Color>
            {
                { "bread_and_cream",  new Color(0.90f, 0.78f, 0.55f) }, // tan
                { "breakfast_cereal", new Color(0.95f, 0.68f, 0.25f) }, // orange
                { "chocolate_bar",    new Color(0.30f, 0.18f, 0.10f) }, // dark brown
                { "flan",             new Color(0.98f, 0.85f, 0.50f) }, // custard yellow
                { "hazelnut_cream",   new Color(0.55f, 0.35f, 0.20f) }, // hazelnut brown
                { "ice_cream",        new Color(0.98f, 0.85f, 0.88f) }, // pale pink
                { "juice_box",        new Color(0.95f, 0.55f, 0.25f) }, // orange juice
                { "milk_box",         new Color(0.97f, 0.97f, 0.94f) }, // off-white
                { "nachos",           new Color(0.95f, 0.78f, 0.35f) }, // cheesy yellow
                { "pancake_syrup",    new Color(0.35f, 0.20f, 0.08f) }, // maple
                { "peanut_butter",    new Color(0.75f, 0.55f, 0.30f) }, // pb tan
                { "soda_bottle",      new Color(0.80f, 0.15f, 0.15f) }, // red cola
                { "water_bottle",     new Color(0.55f, 0.80f, 0.95f) }, // light blue
            };

        static void TintByName(GameObject go, string prefabName)
        {
            if (go == null || string.IsNullOrEmpty(prefabName)) return;
            string key = prefabName.ToLowerInvariant().Replace("(clone)", "").Trim();
            if (!_foodTints.TryGetValue(key, out var color)) return;

            foreach (var mr in go.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mats = mr.materials; // instanced copies, safe to mutate
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    if (mats[i].HasProperty("_BaseColor")) mats[i].SetColor("_BaseColor", color);
                    if (mats[i].HasProperty("_Color")) mats[i].SetColor("_Color", color);
                }
                mr.materials = mats;
            }
        }

        GameObject PickPrefab()
        {
            if (itemPrefabPool != null && itemPrefabPool.Length > 0)
            {
                for (int tries = 0; tries < 4; tries++)
                {
                    var p = itemPrefabPool[Random.Range(0, itemPrefabPool.Length)];
                    if (p != null) return p;
                }
            }
            return itemPrefab;
        }

        void Split()
        {
            _split = true;
            SfxHub.Instance.PlayAt("box_drop", transform.position, 0.9f);
            var hasAny = itemPrefab != null || (itemPrefabPool != null && itemPrefabPool.Length > 0);
            if (!hasAny || quantity <= 0)
            {
                Destroy(gameObject);
                return;
            }

            int n = Mathf.Clamp(quantity, 1, 10);
            for (int i = 0; i < n; i++)
            {
                var prefab = PickPrefab();
                if (prefab == null) continue;
                Vector3 jitter = new Vector3(
                    Random.Range(-itemSpread, itemSpread), 0.15f + i * 0.05f,
                    Random.Range(-itemSpread, itemSpread));
                var go = Instantiate(prefab, transform.position + jitter, Quaternion.identity);
                if (itemScaleMultiplier > 0f && !Mathf.Approximately(itemScaleMultiplier, 1f))
                    go.transform.localScale = go.transform.localScale * itemScaleMultiplier;
                TintByName(go, prefab.name);
                var eat = go.GetComponent<EatOnHeadProximity>();
                if (eat != null)
                {
                    if (!string.IsNullOrEmpty(foodName)) eat.foodName = foodName;
                    if (hungerRestore > 0f) eat.hungerRestore = hungerRestore;
                }
                var rb = go.GetComponent<Rigidbody>();
                if (rb != null) rb.AddForce(jitter * 2f, ForceMode.VelocityChange);
            }
            ToastHUD.Show(n + "x " + (string.IsNullOrEmpty(foodName) ? "groceries" : foodName),
                          "Unpacked", ToastKind.Info);
            Destroy(gameObject);
        }
    }
}
