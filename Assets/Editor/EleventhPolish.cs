using System.Collections.Generic;
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
    // Wires voice profiles into every CallScenario, registers all scenarios with CallDirector,
    // and relocates the NPCVoice GameObject from the phone base onto the handset so TTS
    // audio emits from the earpiece the player holds.
    public static class EleventhPolish
    {
        [MenuItem("HackKU/Fix/Eleventh Polish (Audio on Handset + All Scenarios)")]
        public static void Run()
        {
            // Ensure scenarios exist (re-seed any missing).
            ScenarioSeedAssets.SeedFromMenu();

            WireScenarioVoices();
            RegisterAllScenariosWithDirector();
            MoveNpcVoiceToHandset();

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("[EleventhPolish] done.");
        }

        // Pair each CallScenario file to the NPCVoiceProfile it should use.
        static readonly (string scenarioFile, string voiceFile)[] ScenarioVoicePairs =
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

        static void WireScenarioVoices()
        {
            foreach (var (scenarioFile, voiceFile) in ScenarioVoicePairs)
            {
                var scenarioPath = "Assets/Data/Scenarios/" + scenarioFile + ".asset";
                var voicePath = "Assets/Data/Voices/" + voiceFile + ".asset";
                var scenario = AssetDatabase.LoadAssetAtPath<CallScenario>(scenarioPath);
                var voice = AssetDatabase.LoadAssetAtPath<NPCVoiceProfile>(voicePath);
                if (scenario == null || voice == null)
                {
                    Debug.LogWarning("[EleventhPolish] missing scenario or voice: " + scenarioPath + " / " + voicePath);
                    continue;
                }
                var so = new SerializedObject(scenario);
                var prop = so.FindProperty("voiceProfile");
                if (prop == null) continue;
                if (prop.objectReferenceValue == voice) continue;
                prop.objectReferenceValue = voice;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(scenario);
                Debug.Log("[EleventhPolish] wired " + voiceFile + " -> " + scenarioFile);
            }
        }

        static void RegisterAllScenariosWithDirector()
        {
            var director = Object.FindFirstObjectByType<CallDirector>();
            if (director == null) { Debug.LogWarning("[EleventhPolish] no CallDirector in scene"); return; }

            var list = new List<CallScenario>();
            foreach (var (scenarioFile, _) in ScenarioVoicePairs)
            {
                var s = AssetDatabase.LoadAssetAtPath<CallScenario>("Assets/Data/Scenarios/" + scenarioFile + ".asset");
                if (s != null) list.Add(s);
            }

            var so = new SerializedObject(director);
            var arr = so.FindProperty("scenarios");
            if (arr == null) { Debug.LogWarning("[EleventhPolish] no 'scenarios' field on CallDirector"); return; }
            arr.arraySize = list.Count;
            for (int i = 0; i < list.Count; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(director);
            Debug.Log("[EleventhPolish] registered " + list.Count + " scenarios with CallDirector");
        }

        static void MoveNpcVoiceToHandset()
        {
            var npcVoice = Object.FindFirstObjectByType<NPCVoice>();
            if (npcVoice == null) { Debug.LogWarning("[EleventhPolish] no NPCVoice in scene"); return; }

            var handset = Object.FindFirstObjectByType<HandsetController>();
            if (handset == null) { Debug.LogWarning("[EleventhPolish] no Handset in scene"); return; }

            if (npcVoice.transform.parent != handset.transform)
            {
                npcVoice.transform.SetParent(handset.transform, worldPositionStays: false);
                // Earpiece is at local (-0.1, 0.01, 0) on the handset. Put the AudioSource there so sound emits from it.
                npcVoice.transform.localPosition = new Vector3(-0.1f, 0.01f, 0f);
                npcVoice.transform.localRotation = Quaternion.identity;
                Debug.Log("[EleventhPolish] moved NPCVoice onto Handset earpiece");
            }

            // Keep spatial blend on and shrink the audible range so it really does sound like "from the handset".
            var audio = npcVoice.GetComponent<AudioSource>();
            if (audio != null)
            {
                audio.spatialBlend = 1f;
                audio.rolloffMode = AudioRolloffMode.Linear;
                audio.minDistance = 0.05f;
                audio.maxDistance = 2.5f;
                audio.dopplerLevel = 0f;
                EditorUtility.SetDirty(audio);
            }
            EditorUtility.SetDirty(npcVoice);
        }
    }
}
