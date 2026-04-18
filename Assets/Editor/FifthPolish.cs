using HackKU.AI;
using HackKU.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace HackKU.EditorTools
{
    public static class FifthPolish
    {
        const string HandsetPrefabPath = "Assets/Data/Prefabs/Handset.prefab";
        const string PhonePrefabPath = "Assets/Data/Prefabs/RotaryPhone.prefab";

        [MenuItem("HackKU/Fix/Fifth Polish (Calls + Handset Proximity)")]
        public static void Run()
        {
            TuneCallDirector();
            AddProximityGateToHandset();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("[FifthPolish] done.");
        }

        static void TuneCallDirector()
        {
            foreach (var cd in Object.FindObjectsByType<CallDirector>(FindObjectsSortMode.None))
            {
                var so = new SerializedObject(cd);
                SetFloat(so, "minGapSeconds", 10f);
                SetFloat(so, "maxGapSeconds", 25f);
                SetFloat(so, "firstCallDelay", 8f);
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(cd);
                Debug.Log("[FifthPolish] CallDirector tuned (gap 10-25s, first 8s)");
            }
        }

        static void SetFloat(SerializedObject so, string name, float v)
        {
            var p = so.FindProperty(name);
            if (p != null) p.floatValue = v;
        }

        static void AddProximityGateToHandset()
        {
            AddGateToPrefab(HandsetPrefabPath);
            AddGateToPrefab(PhonePrefabPath);
            // Scene instance fallback — handsets may already be spawned in the open scene.
            foreach (var grab in Object.FindObjectsByType<XRGrabInteractable>(FindObjectsSortMode.None))
            {
                if (grab.gameObject.name != "Handset" && grab.gameObject.name != "Handset(Clone)") continue;
                EnsureGate(grab);
            }
        }

        static void AddGateToPrefab(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                foreach (var grab in instance.GetComponentsInChildren<XRGrabInteractable>(true))
                {
                    EnsureGate(grab);
                }
                PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);
            }
            finally { Object.DestroyImmediate(instance); }
            Debug.Log("[FifthPolish] ProximityGrabGate applied to " + path);
        }

        static void EnsureGate(XRGrabInteractable grab)
        {
            var gate = grab.GetComponent<ProximityGrabGate>();
            if (gate == null) gate = grab.gameObject.AddComponent<ProximityGrabGate>();
            gate.maxDistance = 0.4f;
            gate.checkInterval = 0.08f;
            EditorUtility.SetDirty(gate);
        }
    }
}
