using HackKU.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HackKU.EditorTools
{
    // Adds rigid colliders to the phone base so the handset can physically rest on the cradle
    // (previously the builder stripped all colliders for aesthetic primitives, causing the
    // released handset to fall through the base). Also widens the docking magnet for a cleaner
    // "place phone back" feel during an active call.
    public static class ThirteenthPolish
    {
        const string PhonePrefabPath = "Assets/Data/Prefabs/RotaryPhone.prefab";
        const string HandsetPrefabPath = "Assets/Data/Prefabs/Handset.prefab";

        [MenuItem("HackKU/Fix/Thirteenth Polish (Phone Colliders + Dock Magnet)")]
        public static void Run()
        {
            PatchPhonePrefab();
            PatchHandsetPrefab();
            PatchSceneInstances();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("[ThirteenthPolish] done.");
        }

        static void PatchPhonePrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PhonePrefabPath);
            if (prefab == null) return;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                AddBoxColliderTo(instance, "Base");
                AddBoxColliderTo(instance, "BaseFront");
                AddBoxColliderTo(instance, "CradleL");
                AddBoxColliderTo(instance, "CradleR");
                PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);
            }
            finally { Object.DestroyImmediate(instance); }
            Debug.Log("[ThirteenthPolish] added colliders to RotaryPhone Base/BaseFront/CradleL/CradleR");
        }

        static void AddBoxColliderTo(GameObject root, string childName)
        {
            var t = FindDeepChild(root.transform, childName);
            if (t == null)
            {
                Debug.LogWarning("[ThirteenthPolish] no child: " + childName);
                return;
            }
            var col = t.GetComponent<BoxCollider>();
            if (col == null) col = t.gameObject.AddComponent<BoxCollider>();
            col.isTrigger = false;
            // Cube primitive's default BoxCollider auto-sizes to (1,1,1) local; fine since scale is on the transform.
            col.center = Vector3.zero;
            col.size = Vector3.one;
            EditorUtility.SetDirty(col);
        }

        static Transform FindDeepChild(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                var f = FindDeepChild(parent.GetChild(i), name);
                if (f != null) return f;
            }
            return null;
        }

        static void PatchHandsetPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HandsetPrefabPath);
            if (prefab == null) return;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                var hc = instance.GetComponent<HandsetController>();
                if (hc != null)
                {
                    hc.cradleProximity = 0.35f;   // more forgiving dock magnet
                    hc.dockBlendSeconds = 0.18f;
                }
                var rb = instance.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.mass = 0.5f;
                    rb.linearDamping = 1.5f;
                    rb.angularDamping = 2.5f;
                    rb.isKinematic = true;
                    rb.useGravity = false;
                    rb.interpolation = RigidbodyInterpolation.Interpolate;
                    rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                }
                PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);
            }
            finally { Object.DestroyImmediate(instance); }
            Debug.Log("[ThirteenthPolish] tuned handset cradleProximity=0.35, dockBlend=0.18");
        }

        static void PatchSceneInstances()
        {
            foreach (var hc in Object.FindObjectsByType<HandsetController>(FindObjectsSortMode.None))
            {
                hc.cradleProximity = 0.35f;
                hc.dockBlendSeconds = 0.18f;
                EditorUtility.SetDirty(hc);
            }
        }
    }
}
