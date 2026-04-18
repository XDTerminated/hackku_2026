using System.Reflection;
using HackKU.AI;
using HackKU.Core;
using HackKU.TTS;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HackKU.EditorTools
{
    // Overwrites ALL existing CallScenario assets with the latest seed definitions
    // (so the stricter "don't apply_outcome until explicit commitment + always say goodbye"
    // prompt takes effect on already-created scenarios). Also re-wires voice profiles
    // and the phone reference on CallDirector.
    public static class TwelfthPolish
    {
        [MenuItem("HackKU/Fix/Twelfth Polish (Re-seed + Cradle Physics)")]
        public static void Run()
        {
            ForceReseedScenarios();
            EleventhPolish_Run();
            WirePhoneOnDirector();
            PhysicsPrimeHandset();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("[TwelfthPolish] done.");
        }

        static void ForceReseedScenarios()
        {
            var m = typeof(ScenarioSeedAssets).GetMethod("GetSeedDefinitions", BindingFlags.NonPublic | BindingFlags.Static);
            var arr = m?.Invoke(null, null) as System.Array;
            if (arr == null)
            {
                Debug.LogError("[TwelfthPolish] couldn't read seed definitions");
                return;
            }
            for (int i = 0; i < arr.Length; i++)
            {
                var seedBoxed = arr.GetValue(i);
                var t = seedBoxed.GetType();
                string fileName = (string)t.GetField("fileName").GetValue(seedBoxed);
                string scenarioId = (string)t.GetField("scenarioId").GetValue(seedBoxed);
                string callerName = (string)t.GetField("callerName").GetValue(seedBoxed);
                string situation = (string)t.GetField("situation").GetValue(seedBoxed);
                string systemPrompt = (string)t.GetField("systemPrompt").GetValue(seedBoxed);
                string openingLine = (string)t.GetField("openingLine").GetValue(seedBoxed);
                float maxConversationSeconds = (float)t.GetField("maxConversationSeconds").GetValue(seedBoxed);
                int maxTurns = (int)t.GetField("maxTurns").GetValue(seedBoxed);

                string path = "Assets/Data/Scenarios/" + fileName + ".asset";
                var scenario = AssetDatabase.LoadAssetAtPath<CallScenario>(path);
                bool isNew = scenario == null;
                if (isNew) scenario = ScriptableObject.CreateInstance<CallScenario>();

                scenario.scenarioId = scenarioId;
                scenario.callerName = callerName;
                scenario.situation = situation;
                scenario.systemPrompt = systemPrompt;
                scenario.openingLine = openingLine;
                scenario.maxConversationSeconds = maxConversationSeconds;
                scenario.maxTurns = maxTurns;

                if (isNew) AssetDatabase.CreateAsset(scenario, path);
                EditorUtility.SetDirty(scenario);
                Debug.Log("[TwelfthPolish] " + (isNew ? "created" : "updated") + " " + path);
            }
            AssetDatabase.SaveAssets();
        }

        // Keep this matching EleventhPolish behaviour so voice profiles stay correctly wired after re-seed.
        static void EleventhPolish_Run()
        {
            (string scenarioFile, string voiceFile)[] pairs =
            {
                ("MomWedding", "MomVoice"),
                ("BossOvertime", "BossVoice"),
                ("BuddySkiTrip", "BuddyVoice"),
                ("SiblingLoan", "SiblingVoice"),
                ("GymUpsell", "GymSalesVoice"),
                ("DebtCollector", "DebtCollectorVoice"),
                ("TherapistBooking", "TherapistVoice"),
                ("PizzaImpulse", "PizzaGuyVoice"),
            };
            foreach (var (s, v) in pairs)
            {
                var scen = AssetDatabase.LoadAssetAtPath<CallScenario>("Assets/Data/Scenarios/" + s + ".asset");
                var voice = AssetDatabase.LoadAssetAtPath<NPCVoiceProfile>("Assets/Data/Voices/" + v + ".asset");
                if (scen == null || voice == null) continue;
                var so = new SerializedObject(scen);
                var p = so.FindProperty("voiceProfile");
                if (p != null) { p.objectReferenceValue = voice; so.ApplyModifiedProperties(); EditorUtility.SetDirty(scen); }
            }
        }

        static void WirePhoneOnDirector()
        {
            var dir = Object.FindFirstObjectByType<CallDirector>();
            var phone = Object.FindFirstObjectByType<RotaryPhone>();
            if (dir == null || phone == null) return;
            var so = new SerializedObject(dir);
            var p = so.FindProperty("phone");
            if (p != null)
            {
                p.objectReferenceValue = phone;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(dir);
                Debug.Log("[TwelfthPolish] phone ref wired on CallDirector");
            }
        }

        static void PhysicsPrimeHandset()
        {
            // Ensure handset has a BoxCollider trimesh-ish enough to land flat on a floor.
            var handsetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Data/Prefabs/Handset.prefab");
            if (handsetPrefab == null) return;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(handsetPrefab);
            try
            {
                var hc = instance.GetComponent<HandsetController>();
                if (hc != null)
                {
                    hc.cradleProximity = 0.25f;
                    hc.dockBlendSeconds = 0.2f;
                    hc.dropSettleSeconds = 0.8f;
                }
                var rb = instance.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.mass = 0.6f;
                    rb.linearDamping = 1.0f;
                    rb.angularDamping = 1.5f;
                    rb.interpolation = RigidbodyInterpolation.Interpolate;
                    rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                    rb.useGravity = false;
                    rb.isKinematic = true;
                }
                PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);
            }
            finally { Object.DestroyImmediate(instance); }
            Debug.Log("[TwelfthPolish] Handset physics primed (gravity off until released far from cradle)");
        }
    }
}
