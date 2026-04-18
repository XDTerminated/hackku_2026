using System.IO;
using System.Reflection;
using HackKU.AI;
using HackKU.Core;
using HackKU.TTS;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HackKU.EditorTools
{
    public static class FinalSceneBootstrap
    {
        [MenuItem("HackKU/Build/Final Scene Wiring")]
        public static void Run()
        {
            EnsureElevenLabsConfig();
            var momVoice = EnsureMomVoice();
            AttachMomVoiceToScenario(momVoice);

            var phoneGo = GameObject.Find("RotaryPhone") ?? GameObject.Find("RotaryPhone(Clone)");
            if (phoneGo == null) { Debug.LogError("[FinalSceneBootstrap] no RotaryPhone in scene"); return; }

            var phone = phoneGo.GetComponentInChildren<RotaryPhone>(true);
            if (phone == null) { Debug.LogError("[FinalSceneBootstrap] no RotaryPhone component"); return; }

            var npcVoiceGo = new GameObject("NPCVoice", typeof(AudioSource));
            npcVoiceGo.transform.SetParent(phoneGo.transform, false);
            var npcVoice = npcVoiceGo.AddComponent<NPCVoice>();
            npcVoice.VoiceProfile = momVoice;

            var controllerGo = new GameObject("CallSystem");
            controllerGo.transform.position = Vector3.zero;

            var mic = controllerGo.AddComponent<MicrophoneCapture>();
            var callController = controllerGo.AddComponent<CallController>();
            SetField(callController, "phone", phone);
            SetField(callController, "npcVoice", npcVoice);
            SetField(callController, "mic", mic);

            var callDirector = controllerGo.AddComponent<CallDirector>();
            var momScenario = AssetDatabase.LoadAssetAtPath<CallScenario>("Assets/Data/Scenarios/MomWedding.asset");
            SetField(callDirector, "callController", callController);
            SetField(callDirector, "scenarios", new[] { momScenario });

            var trigger = controllerGo.AddComponent<PlayerSpeechTrigger>();
            SetField(trigger, "callController", callController);
            SetField(trigger, "mic", mic);

            var deliveryGo = new GameObject("DeliveryService");
            var deliveryService = deliveryGo.AddComponent<DeliveryService>();
            var spawn = GameObject.Find("DeliverySpawn");
            if (spawn != null) deliveryService.spawnPoint = spawn.transform;

            var outgoingGo = phoneGo;
            var outgoing = outgoingGo.GetComponent<OutgoingCallMenu>() ?? outgoingGo.AddComponent<OutgoingCallMenu>();
            WireOutgoingMenu(outgoing, phone);

            EditorSceneManager.MarkSceneDirty(phoneGo.scene);
            Debug.Log("[FinalSceneBootstrap] wiring done.");
        }

        static void WireOutgoingMenu(OutgoingCallMenu menu, RotaryPhone phone)
        {
            var groceries = AssetDatabase.LoadAssetAtPath<DeliveryItem>("Assets/Data/Deliveries/groceries.asset");
            var pizza = AssetDatabase.LoadAssetAtPath<DeliveryItem>("Assets/Data/Deliveries/pizza.asset");
            var gym = AssetDatabase.LoadAssetAtPath<DeliveryItem>("Assets/Data/Deliveries/gym_membership.asset");
            var field = menu.GetType().GetField("items", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValue(menu, new DeliveryItem[] { groceries, pizza, gym });
            }
            menu.SetPhone(phone);
        }

        static void EnsureElevenLabsConfig()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            var cfg = AssetDatabase.LoadAssetAtPath<ElevenLabsConfig>("Assets/Resources/ElevenLabsConfig.asset");
            if (cfg == null)
            {
                cfg = ScriptableObject.CreateInstance<ElevenLabsConfig>();
                AssetDatabase.CreateAsset(cfg, "Assets/Resources/ElevenLabsConfig.asset");
            }
            EditorUtility.SetDirty(cfg);
            AssetDatabase.SaveAssets();
        }

        static NPCVoiceProfile EnsureMomVoice()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data/Voices"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
                AssetDatabase.CreateFolder("Assets/Data", "Voices");
            }
            var path = "Assets/Data/Voices/MomVoice.asset";
            var v = AssetDatabase.LoadAssetAtPath<NPCVoiceProfile>(path);
            if (v == null)
            {
                v = ScriptableObject.CreateInstance<NPCVoiceProfile>();
                v.displayName = "Mom";
                v.voiceId = "EXAVITQu4vr4xnSDxMaL";
                AssetDatabase.CreateAsset(v, path);
            }
            EditorUtility.SetDirty(v);
            AssetDatabase.SaveAssets();
            return v;
        }

        static void AttachMomVoiceToScenario(NPCVoiceProfile momVoice)
        {
            var scenario = AssetDatabase.LoadAssetAtPath<CallScenario>("Assets/Data/Scenarios/MomWedding.asset");
            if (scenario == null) return;
            var so = new SerializedObject(scenario);
            var prop = so.FindProperty("voiceProfile");
            if (prop != null)
            {
                prop.objectReferenceValue = momVoice;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(scenario);
                AssetDatabase.SaveAssets();
            }
        }

        static void SetField(object target, string name, object value)
        {
            var f = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f == null)
            {
                Debug.LogWarning("[FinalSceneBootstrap] field not found: " + target.GetType().Name + "." + name);
                return;
            }
            f.SetValue(target, value);
        }
    }
}
