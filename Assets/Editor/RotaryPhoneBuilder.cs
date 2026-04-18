using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HackKU.EditorTools
{
    public static class RotaryPhoneBuilder
    {
        const string PhonePrefabPath = "Assets/Data/Prefabs/RotaryPhone.prefab";
        const string HandsetPrefabPath = "Assets/Data/Prefabs/Handset.prefab";

        [MenuItem("HackKU/Build/Rotary Phone")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data/Prefabs"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
                AssetDatabase.CreateFolder("Assets/Data", "Prefabs");
            }

            var bakelite = GetOrCreateMaterial("Assets/Data/Prefabs/Bakelite.mat", new Color(0.08f, 0.07f, 0.08f), 0.0f, 0.6f);
            var gold = GetOrCreateMaterial("Assets/Data/Prefabs/Gold.mat", new Color(0.9f, 0.75f, 0.35f), 0.8f, 0.7f);
            var phone = new GameObject("RotaryPhone");

            var baseGo = MakeCube("Base", phone.transform, new Vector3(0, 0.04f, 0), new Vector3(0.22f, 0.08f, 0.17f), bakelite);
            MakeCube("BaseFront", phone.transform, new Vector3(0, 0.025f, 0.07f), new Vector3(0.2f, 0.03f, 0.03f), bakelite);

            var dialHub = MakeCylinder("DialHub", phone.transform, new Vector3(0, 0.085f, -0.025f), new Vector3(0.11f, 0.005f, 0.11f), bakelite);
            dialHub.transform.rotation = Quaternion.Euler(0, 0, 0);

            for (int i = 0; i < 10; i++)
            {
                float ang = (i * 36f - 170f) * Mathf.Deg2Rad;
                var hole = MakeSphere("Hole" + i, dialHub.transform, new Vector3(Mathf.Sin(ang) * 0.04f, 0.005f, Mathf.Cos(ang) * 0.04f), Vector3.one * 0.018f, gold);
            }

            var cradleL = MakeCube("CradleL", phone.transform, new Vector3(-0.08f, 0.09f, 0.05f), new Vector3(0.035f, 0.02f, 0.05f), bakelite);
            var cradleR = MakeCube("CradleR", phone.transform, new Vector3(0.08f, 0.09f, 0.05f), new Vector3(0.035f, 0.02f, 0.05f), bakelite);

            var handset = new GameObject("Handset");
            handset.transform.SetParent(phone.transform, false);
            handset.transform.localPosition = new Vector3(0, 0.12f, 0.05f);

            var bar = MakeCube("Bar", handset.transform, new Vector3(0, 0, 0), new Vector3(0.22f, 0.025f, 0.03f), bakelite);

            var earpiece = MakeSphere("Earpiece", handset.transform, new Vector3(-0.1f, 0.01f, 0), new Vector3(0.06f, 0.05f, 0.06f), bakelite);
            var mouthpiece = MakeSphere("Mouthpiece", handset.transform, new Vector3(0.1f, 0.01f, 0), new Vector3(0.06f, 0.05f, 0.06f), bakelite);

            var handsetRb = handset.AddComponent<Rigidbody>();
            handsetRb.useGravity = false;
            handsetRb.isKinematic = true;
            handsetRb.interpolation = RigidbodyInterpolation.Interpolate;

            var handsetCollider = handset.AddComponent<BoxCollider>();
            handsetCollider.center = Vector3.zero;
            handsetCollider.size = new Vector3(0.24f, 0.07f, 0.07f);

            var grabType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable, Unity.XR.Interaction.Toolkit");
            if (grabType != null) handset.AddComponent(grabType);

            var handsetGrab = handset.AddComponent<HackKU.Core.HandsetController>();

            var phoneBase = phone.AddComponent<HackKU.Core.RotaryPhone>();

            var handsetPrefab = PrefabUtility.SaveAsPrefabAsset(handset, HandsetPrefabPath);

            var phonePrefab = PrefabUtility.SaveAsPrefabAsset(phone, PhonePrefabPath);
            Object.DestroyImmediate(phone);

            Debug.Log("[RotaryPhoneBuilder] built " + PhonePrefabPath);
            AssetDatabase.SaveAssets();
        }

        static Material GetOrCreateMaterial(string path, Color color, float metallic, float smoothness)
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
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssetIfDirty(mat);
            return mat;
        }

        [MenuItem("HackKU/Build/Place Rotary Phone In Scene")]
        public static void PlaceInScene()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PhonePrefabPath);
            if (prefab == null) { Debug.LogError("[RotaryPhoneBuilder] build phone first"); return; }

            var existing = GameObject.Find("RotaryPhone");
            if (existing != null) Object.DestroyImmediate(existing);

            var table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = "PhoneTable";
            table.transform.position = new Vector3(2.5f, 0.4f, 0);
            table.transform.localScale = new Vector3(0.6f, 0.8f, 0.4f);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = new Vector3(2.5f, 0.82f, 0);
            instance.transform.rotation = Quaternion.Euler(0, -90, 0);
            EditorSceneManager.MarkSceneDirty(instance.scene);
            Debug.Log("[RotaryPhoneBuilder] phone placed in scene");
        }

        static GameObject MakeCube(string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static GameObject MakeCylinder(string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static GameObject MakeSphere(string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }
    }
}
