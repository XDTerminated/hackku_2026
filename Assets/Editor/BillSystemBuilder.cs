using System.IO;
using HackKU.Core;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace HackKU.EditorTools
{
    // Builds the BillPaper prefab (paper mesh + TMP label + XR grab) and drops a
    // BillSpawner + spawn point into the scene near the front door, so physical bills
    // drop into the house on a timer.
    public static class BillSystemBuilder
    {
        const string PrefabPath = "Assets/Data/Prefabs/BillPaper.prefab";
        const string MatFolder = "Assets/Materials/Exterior";

        [MenuItem("HackKU/Build/Bill System (Prefab + Spawner)")]
        public static void Build()
        {
            EnsureFolder("Assets/Data");
            EnsureFolder("Assets/Data/Prefabs");

            var prefab = BuildPrefab();
            WireSpawner(prefab);

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("[BillSystemBuilder] done — BillPaper prefab at " + PrefabPath);
        }

        static BillPaper BuildPrefab()
        {
            // Build the paper in scene, save as prefab, then delete the scene instance.
            var root = new GameObject("BillPaper");

            // Paper mesh — thin quad-like cube.
            var paper = GameObject.CreatePrimitive(PrimitiveType.Cube);
            paper.name = "PaperMesh";
            paper.transform.SetParent(root.transform, false);
            paper.transform.localScale = new Vector3(0.21f, 0.015f, 0.28f);
            var paperMat = GetOrMakeMat(MatFolder + "/BillPaper.mat", new Color(0.98f, 0.97f, 0.90f), 0f, 0.1f);
            paper.GetComponent<MeshRenderer>().sharedMaterial = paperMat;
            Object.DestroyImmediate(paper.GetComponent<Collider>());

            // Red accent stripe so the paper reads as a bill.
            var stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripe.name = "Stripe";
            stripe.transform.SetParent(root.transform, false);
            stripe.transform.localPosition = new Vector3(0f, 0.008f, 0.12f);
            stripe.transform.localScale = new Vector3(0.21f, 0.004f, 0.03f);
            stripe.GetComponent<MeshRenderer>().sharedMaterial = GetOrMakeMat(MatFolder + "/BillStripe.mat", new Color(0.75f, 0.1f, 0.1f), 0f, 0.3f);
            Object.DestroyImmediate(stripe.GetComponent<Collider>());

            // World-space billboarded UI above the paper. Stays a child so the prefab captures it,
            // but TextFaceCamera's anchor+worldOffset keep it upright even when the paper tumbles.
            var billboardRoot = new GameObject("FloatingInfo");
            billboardRoot.transform.SetParent(root.transform, false);
            billboardRoot.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            var face = billboardRoot.AddComponent<TextFaceCamera>();
            face.anchor = root.transform;
            face.worldOffset = new Vector3(0f, 0.4f, 0f);

            // Background card so the text is readable over any floor.
            var bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bg.name = "BG";
            bg.transform.SetParent(billboardRoot.transform, false);
            bg.transform.localPosition = new Vector3(0f, 0f, 0.01f);
            bg.transform.localScale = new Vector3(0.34f, 0.18f, 1f);
            bg.GetComponent<MeshRenderer>().sharedMaterial = GetOrMakeMat(MatFolder + "/BillInfoBG.mat", new Color(0.08f, 0.08f, 0.08f, 0.85f), 0f, 0.2f);
            Object.DestroyImmediate(bg.GetComponent<Collider>());

            var textGO = new GameObject("Label");
            textGO.transform.SetParent(billboardRoot.transform, false);
            textGO.transform.localPosition = Vector3.zero;
            var rect = textGO.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0.32f, 0.16f);
            var tmp = textGO.AddComponent<TextMeshPro>();
            tmp.text = "Bill\n$0";
            tmp.fontSize = 0.7f;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.richText = true;

            // Rigidbody so the paper can be thrown / dropped.
            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 0.05f;
            rb.linearDamping = 1.5f;
            rb.angularDamping = 2f;

            // Box trigger for auto-commit if the player walks into the paper.
            var trig = root.AddComponent<BoxCollider>();
            trig.size = new Vector3(0.25f, 0.15f, 0.32f);
            trig.center = new Vector3(0f, 0.05f, 0f);
            trig.isTrigger = true;

            // Solid collider so the paper has physics.
            var solid = root.AddComponent<BoxCollider>();
            solid.size = new Vector3(0.21f, 0.015f, 0.28f);
            solid.isTrigger = false;

            // XR grab so the player can pick it up with a controller.
            var grab = root.AddComponent<XRGrabInteractable>();
            grab.movementType = XRBaseInteractable.MovementType.Instantaneous;

            // BillPaper component last so references are ready.
            var bp = root.AddComponent<BillPaper>();
            bp.label3D = tmp;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            return prefab.GetComponent<BillPaper>();
        }

        static void WireSpawner(BillPaper prefab)
        {
            var existing = GameObject.Find("BillSpawner");
            if (existing != null) Object.DestroyImmediate(existing);

            var spawnerGO = new GameObject("BillSpawner");
            // Position the spawn point just inside the front door so bills drop through the mail slot.
            // Front door is at X=-2.2, Z=-1.75. Put the spawn ~0.6m inside the house.
            var spawnPoint = new GameObject("SpawnPoint");
            spawnPoint.transform.SetParent(spawnerGO.transform, false);
            spawnPoint.transform.position = new Vector3(-2.2f, 1.2f, -1.1f);
            spawnPoint.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            var spawner = spawnerGO.AddComponent<BillSpawner>();
            var so = new SerializedObject(spawner);
            so.FindProperty("billPrefab").objectReferenceValue = prefab;
            so.FindProperty("spawnPoint").objectReferenceValue = spawnPoint.transform;
            so.ApplyModifiedProperties();

            var house = GameObject.Find("House");
            if (house != null) spawnerGO.transform.SetParent(house.transform, true);

            EditorUtility.SetDirty(spawnerGO);
        }

        static Material GetOrMakeMat(string path, Color c, float metallic, float smoothness)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.shader = shader;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(m);
            return m;
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
