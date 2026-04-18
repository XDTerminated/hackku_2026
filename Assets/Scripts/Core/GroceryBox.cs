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

        void Start() { RefreshLabel(); }

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
