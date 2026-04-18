using System.IO;
using HackKU.Core;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace HackKU.EditorTools
{
    // Builds the GroceryBox prefab: a carryable brown cardboard box that splits into
    // individual grocery items when dropped on the floor inside the house.
    public static class GroceryBoxBuilder
    {
        const string PrefabPath = "Assets/Data/Prefabs/GroceryBox.prefab";
        const string MatFolder = "Assets/Materials/Exterior";

        [MenuItem("HackKU/Build/Grocery Box Prefab")]
        public static void Build()
        {
            EnsureFolder("Assets/Data");
            EnsureFolder("Assets/Data/Prefabs");

            var root = new GameObject("GroceryBox");

            // Cardboard body.
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(0.45f, 0.35f, 0.35f);
            body.GetComponent<MeshRenderer>().sharedMaterial =
                GetOrMake(MatFolder + "/Cardboard.mat", new Color(0.78f, 0.58f, 0.38f), 0f, 0.15f);
            Object.DestroyImmediate(body.GetComponent<Collider>());

            // Packing-tape stripe on top.
            var tape = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tape.name = "Tape";
            tape.transform.SetParent(root.transform, false);
            tape.transform.localPosition = new Vector3(0f, 0.176f, 0f);
            tape.transform.localScale = new Vector3(0.45f, 0.005f, 0.09f);
            tape.GetComponent<MeshRenderer>().sharedMaterial =
                GetOrMake(MatFolder + "/Tape.mat", new Color(0.92f, 0.89f, 0.70f), 0f, 0.25f);
            Object.DestroyImmediate(tape.GetComponent<Collider>());

            // Floating billboard UI above the box — shows "xN" quantity + food name.
            var floating = new GameObject("Floating");
            floating.transform.SetParent(root.transform, false);
            floating.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            var faceCam = floating.AddComponent<HackKU.Core.TextFaceCamera>();
            faceCam.anchor = root.transform;
            faceCam.worldOffset = new Vector3(0f, 0.4f, 0f);

            // Dark background card so text is readable against any wall/floor.
            var bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bg.name = "BG";
            bg.transform.SetParent(floating.transform, false);
            bg.transform.localPosition = new Vector3(0f, 0f, 0.01f);
            bg.transform.localScale = new Vector3(0.42f, 0.2f, 1f);
            bg.GetComponent<MeshRenderer>().sharedMaterial =
                GetOrMake(MatFolder + "/BoxInfoBG.mat", new Color(0.06f, 0.06f, 0.08f, 0.9f), 0f, 0.2f);
            Object.DestroyImmediate(bg.GetComponent<Collider>());

            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(floating.transform, false);
            labelGO.transform.localPosition = Vector3.zero;
            var rt = labelGO.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0.4f, 0.2f);
            var tmp = labelGO.AddComponent<TextMeshPro>();
            tmp.text = "<b>x1</b>";
            tmp.fontSize = 1.4f;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.richText = true;
            tmp.textWrappingMode = TextWrappingModes.Normal;

            // Physics.
            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 2.5f;
            rb.linearDamping = 1.2f;
            rb.angularDamping = 2f;

            // Solid collider for the box so it rests on floor / furniture.
            var solid = root.AddComponent<BoxCollider>();
            solid.size = new Vector3(0.45f, 0.35f, 0.35f);
            solid.isTrigger = false;

            // XR grab so the player can carry it inside.
            var grab = root.AddComponent<XRGrabInteractable>();
            grab.movementType = XRBaseInteractable.MovementType.Instantaneous;

            // Behavior last so references are ready.
            var gb = root.AddComponent<GroceryBox>();
            gb.quantityLabel = tmp;

            var saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log("[GroceryBoxBuilder] saved " + PrefabPath);

            // Wire into FoodOrderController if present.
            var foc = Object.FindFirstObjectByType<HackKU.AI.FoodOrderController>();
            if (foc != null)
            {
                var so = new SerializedObject(foc);
                var p = so.FindProperty("groceryBoxPrefab");
                if (p != null) { p.objectReferenceValue = saved; so.ApplyModifiedProperties(); }
                EditorUtility.SetDirty(foc);
                Debug.Log("[GroceryBoxBuilder] wired into FoodOrderController.");
            }
            AssetDatabase.SaveAssets();
        }

        static Material GetOrMake(string path, Color c, float metallic, float smoothness)
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
