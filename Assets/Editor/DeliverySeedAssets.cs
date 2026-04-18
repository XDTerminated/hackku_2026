using System.IO;
using HackKU.Core;
using UnityEditor;
using UnityEngine;

namespace HackKU.EditorTools
{
    [InitializeOnLoad]
    public static class DeliverySeedAssets
    {
        const string ItemFolder = "Assets/Data/Deliveries";
        const string PrefabFolder = "Assets/Data/Prefabs";
        const string SessionFlag = "HackKU.DeliverySeedAssets.Ran";
        const string MenuItemPath = "HackKU/Seed/Create Delivery Items";

        static DeliverySeedAssets()
        {
            if (SessionState.GetBool(SessionFlag, false)) return;
            SessionState.SetBool(SessionFlag, true);
            EditorApplication.delayCall += () => SeedIfMissing(false);
        }

        [MenuItem(MenuItemPath)]
        public static void SeedFromMenu()
        {
            int created = SeedIfMissing(true);
            EditorUtility.DisplayDialog(
                "HackKU Delivery Seeds",
                created == 0 ? "All seed delivery items already exist." : $"Created {created} delivery asset(s).",
                "OK");
        }

        static int SeedIfMissing(bool logNoOp)
        {
            EnsureFolder(ItemFolder);
            EnsureFolder(PrefabFolder);

            var seeds = new[]
            {
                new Seed { id = "groceries", name = "Groceries", price = 45f, happy = 3f, color = new Color(0.4f, 0.8f, 0.4f) },
                new Seed { id = "pizza", name = "Pizza", price = 22f, happy = 4f, color = new Color(0.95f, 0.6f, 0.25f) },
                new Seed { id = "gym_membership", name = "Gym Membership", price = 80f, happy = 5f, color = new Color(0.3f, 0.55f, 0.95f) },
            };

            int created = 0;
            foreach (var s in seeds)
            {
                string prefabPath = $"{PrefabFolder}/Delivery_{s.id}.prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    prefab = CreateGrabbablePrefab(s, prefabPath);
                    created++;
                }

                string itemPath = $"{ItemFolder}/{s.id}.asset";
                var existing = AssetDatabase.LoadAssetAtPath<DeliveryItem>(itemPath);
                if (existing == null)
                {
                    var item = ScriptableObject.CreateInstance<DeliveryItem>();
                    item.itemId = s.id;
                    item.displayName = s.name;
                    item.price = s.price;
                    item.happinessOnUse = s.happy;
                    item.prefab = prefab;
                    AssetDatabase.CreateAsset(item, itemPath);
                    created++;
                    Debug.Log($"[DeliverySeedAssets] Created {itemPath}");
                }
            }

            if (created > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            else if (logNoOp)
            {
                Debug.Log("[DeliverySeedAssets] No missing seeds.");
            }

            return created;
        }

        static GameObject CreateGrabbablePrefab(Seed s, string prefabPath)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Delivery_" + s.id;
            go.transform.localScale = new Vector3(0.18f, 0.18f, 0.18f);

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            string matPath = $"{PrefabFolder}/Delivery_{s.id}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            mat.color = s.color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", s.color);
            EditorUtility.SetDirty(mat);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.6f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            // BoxCollider is already on the primitive; no action needed.

            var grabType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable, Unity.XR.Interaction.Toolkit");
            if (grabType != null) go.AddComponent(grabType);
            else Debug.LogWarning("[DeliverySeedAssets] XRGrabInteractable type not found; prop will spawn without grab.");

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            Debug.Log($"[DeliverySeedAssets] Created {prefabPath}");
            return prefab;
        }

        static void EnsureFolder(string assetFolderPath)
        {
            if (AssetDatabase.IsValidFolder(assetFolderPath)) return;
            string[] parts = assetFolderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
            string abs = Path.Combine(Directory.GetCurrentDirectory(), assetFolderPath);
            if (!Directory.Exists(abs)) Directory.CreateDirectory(abs);
        }

        struct Seed
        {
            public string id;
            public string name;
            public float price;
            public float happy;
            public Color color;
        }
    }
}
