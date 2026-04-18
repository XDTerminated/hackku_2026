using System.IO;
using System.Reflection;
using HackKU.AI;
using HackKU.Core;
using HackKU.TTS;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace HackKU.EditorTools
{
    public static class FoodSystemBuilder
    {
        const string FoodFolder = "Assets/Data/Foods";
        const string PrefabFolder = "Assets/Data/Prefabs";
        const string MatFolder = "Assets/Materials/Foods";

        [MenuItem("HackKU/Build/Food + Hunger + Toast System")]
        public static void Build()
        {
            EnsureFolder(FoodFolder);
            EnsureFolder(PrefabFolder);
            EnsureFolder(MatFolder);

            var items = BuildFoodItems();
            AttachHungerManager();
            AttachFinanceScheduler();
            AttachToastHUD();
            AttachHungerDebuffToRig();
            AddHungerRowToWristWatch();
            WireFoodOrderController(items);

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("[FoodSystemBuilder] done.");
        }

        static FoodItem[] BuildFoodItems()
        {
            var specs = new (string id, string name, float price, float hunger, Color color)[]
            {
                ("groceries", "Groceries", 45f, 40f, new Color(0.45f, 0.70f, 0.35f)),
                ("pizza",     "Pizza",     22f, 25f, new Color(0.85f, 0.25f, 0.15f)),
                ("fancy",     "Gourmet",   75f, 60f, new Color(0.85f, 0.65f, 0.20f)),
                ("fast_food", "Fast Food", 12f, 15f, new Color(0.95f, 0.80f, 0.30f)),
            };

            var items = new FoodItem[specs.Length];
            for (int i = 0; i < specs.Length; i++)
            {
                var s = specs[i];
                var itemPath = FoodFolder + "/" + s.id + ".asset";
                var item = AssetDatabase.LoadAssetAtPath<FoodItem>(itemPath);
                if (item == null)
                {
                    item = ScriptableObject.CreateInstance<FoodItem>();
                    AssetDatabase.CreateAsset(item, itemPath);
                }
                item.itemId = s.id;
                item.displayName = s.name;
                item.price = s.price;
                item.hungerRestore = s.hunger;
                item.accentColor = s.color;
                item.prefab = BuildFoodPrefab(s.id, s.name, s.hunger, s.color);
                EditorUtility.SetDirty(item);
                items[i] = item;
            }
            AssetDatabase.SaveAssets();
            return items;
        }

        static GameObject BuildFoodPrefab(string id, string displayName, float hungerRestore, Color color)
        {
            var path = PrefabFolder + "/Food_" + id + ".prefab";
            var mat = GetOrCreateMaterial(MatFolder + "/Food_" + id + ".mat", color);

            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Food_" + id;
            cube.transform.localScale = new Vector3(0.25f, 0.18f, 0.22f);
            cube.GetComponent<MeshRenderer>().sharedMaterial = mat;

            var col = cube.GetComponent<BoxCollider>();
            col.isTrigger = false;

            var rb = cube.AddComponent<Rigidbody>();
            rb.mass = 0.5f;
            rb.useGravity = true;
            rb.linearDamping = 1f;
            rb.angularDamping = 1f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var grab = cube.AddComponent<XRGrabInteractable>();
            grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
            grab.trackPosition = true;
            grab.trackRotation = true;
            grab.throwOnDetach = false;
            grab.smoothPosition = false;
            grab.smoothRotation = false;

            var eat = cube.AddComponent<EatOnHeadProximity>();
            eat.foodName = displayName;
            eat.hungerRestore = hungerRestore;

            var savedPrefab = PrefabUtility.SaveAsPrefabAsset(cube, path);
            Object.DestroyImmediate(cube);
            return savedPrefab;
        }

        static Material GetOrCreateMaterial(string path, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.3f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void AttachHungerManager()
        {
            var host = FindOrCreate("GameManager");
            if (host.GetComponent<HungerManager>() == null) host.AddComponent<HungerManager>();
        }

        static void AttachFinanceScheduler()
        {
            var host = FindOrCreate("GameManager");
            if (host.GetComponent<FinanceScheduler>() == null) host.AddComponent<FinanceScheduler>();
        }

        static void AttachToastHUD()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("[FoodSystemBuilder] No main camera — ToastHUD not attached. Ensure XR Origin has Main Camera tagged.");
                return;
            }

            // Clean up any prior instance.
            var old = cam.transform.Find("ToastHUD");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var go = new GameObject("ToastHUD");
            go.transform.SetParent(cam.transform, false);
            go.transform.localPosition = new Vector3(0.32f, 0.22f, 0.9f);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one * 0.0012f;

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();

            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(300, 500);
            rt.pivot = new Vector2(1f, 1f);

            var stack = new GameObject("Stack", typeof(RectTransform));
            var srt = (RectTransform)stack.transform;
            srt.SetParent(go.transform, false);
            srt.anchorMin = new Vector2(0, 0);
            srt.anchorMax = new Vector2(1, 1);
            srt.offsetMin = Vector2.zero;
            srt.offsetMax = Vector2.zero;

            var toast = go.AddComponent<ToastHUD>();
            var so = new SerializedObject(toast);
            var p = so.FindProperty("stack");
            if (p != null) { p.objectReferenceValue = srt; so.ApplyModifiedProperties(); }
            EditorUtility.SetDirty(toast);
        }

        static void AttachHungerDebuffToRig()
        {
            var rig = GameObject.Find("XR Origin (XR Rig)");
            if (rig == null) { Debug.LogWarning("[FoodSystemBuilder] no XR Origin"); return; }
            if (rig.GetComponent<HungerMovementDebuff>() == null) rig.AddComponent<HungerMovementDebuff>();
        }

        static void AddHungerRowToWristWatch()
        {
            // WristWatchBuilder now owns the wrist layout (TMP-based). Just wire the existing
            // Hunger TMP_Text if it's there; do nothing otherwise.
            var canvas = GameObject.Find("WristCanvas");
            if (canvas == null) return;
            var watch = canvas.GetComponent<WristWatchUI>();
            if (watch == null) return;

            var existing = canvas.transform.Find("Hunger");
            if (existing != null)
            {
                var tmp = existing.GetComponent<TMPro.TMP_Text>();
                if (tmp != null) watch.hungerText = tmp;
                EditorUtility.SetDirty(watch);
            }
        }

        static void WireFoodOrderController(FoodItem[] items)
        {
            var callSystem = GameObject.Find("CallSystem");
            if (callSystem == null)
            {
                Debug.LogWarning("[FoodSystemBuilder] no CallSystem — creating one");
                callSystem = new GameObject("CallSystem");
            }

            var foc = callSystem.GetComponent<FoodOrderController>();
            if (foc == null) foc = callSystem.AddComponent<FoodOrderController>();

            var phone = Object.FindFirstObjectByType<RotaryPhone>();
            var npcVoice = Object.FindFirstObjectByType<NPCVoice>();
            var mic = callSystem.GetComponent<MicrophoneCapture>();
            if (mic == null) mic = Object.FindFirstObjectByType<MicrophoneCapture>();
            var deliverySpawn = GameObject.Find("DeliverySpawn");

            var so = new SerializedObject(foc);
            SetObj(so, "phone", phone);
            SetObj(so, "npcVoice", npcVoice);
            SetObj(so, "mic", mic);
            SetObj(so, "deliverySpawn", deliverySpawn != null ? deliverySpawn.transform : null);

            var menuProp = so.FindProperty("menu");
            if (menuProp != null)
            {
                menuProp.arraySize = items.Length;
                for (int i = 0; i < items.Length; i++)
                    menuProp.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }

            var groceryVoice = AssetDatabase.LoadAssetAtPath<NPCVoiceProfile>("Assets/Data/Voices/GroceryClerkVoice.asset");
            var pizzaVoice = AssetDatabase.LoadAssetAtPath<NPCVoiceProfile>("Assets/Data/Voices/PizzaGuyVoice.asset");
            var therapistVoice = AssetDatabase.LoadAssetAtPath<NPCVoiceProfile>("Assets/Data/Voices/TherapistVoice.asset");
            var gymVoice = AssetDatabase.LoadAssetAtPath<NPCVoiceProfile>("Assets/Data/Voices/GymSalesVoice.asset");
            SetObj(so, "groceryVoice", groceryVoice);
            SetObj(so, "pizzaVoice", pizzaVoice);
            SetObj(so, "fancyVoice", therapistVoice); // calm British voice reads as maitre d
            SetObj(so, "fastFoodVoice", gymVoice);     // peppy voice reads as fast-food cashier

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(foc);
        }

        static void SetObj(SerializedObject so, string path, Object value)
        {
            var p = so.FindProperty(path);
            if (p == null) return;
            p.objectReferenceValue = value;
        }

        static GameObject FindOrCreate(string name)
        {
            var go = GameObject.Find(name);
            if (go == null) go = new GameObject(name);
            return go;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            var leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
