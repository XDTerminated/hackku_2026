using System.Collections.Generic;
using HackKU.AI;
using HackKU.TTS;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HackKU.EditorTools
{
    // Rewires every CallScenario asset to an appropriate NPCVoiceProfile, then
    // assigns the full scenario set to each CallDirector in the open scene.
    // Food-service scenarios are excluded on purpose — those are outgoing-only.
    public static class CallDirectorRewire
    {
        const string ScenariosFolder = "Assets/Data/Scenarios";
        const string VoicesFolder = "Assets/Data/Voices";

        [MenuItem("HackKU/Fix/Rewire Call Director Scenarios & Voices")]
        public static void Run()
        {
            var idToVoice = new Dictionary<string, string>
            {
                { "mom_wedding", "MomVoice" },
                { "buddy_ski_trip", "BuddyVoice" },
                { "sibling_loan", "SiblingVoice" },
                { "therapist_booking", "TherapistVoice" },
                { "dad_ball_game", "DadVoice" },
                { "concert_friend", "BuddyVoice" },
                { "nephew_birthday", "TherapistVoice" },
                { "cooking_class", "SiblingVoice" },
                { "weekend_trip_partner", "SiblingVoice" },
            };

            var guids = AssetDatabase.FindAssets("t:CallScenario", new[] { ScenariosFolder });
            var scenarios = new List<CallScenario>();
            foreach (var g in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                var s = AssetDatabase.LoadAssetAtPath<CallScenario>(p);
                if (s == null) continue;
                if (!idToVoice.TryGetValue(s.scenarioId ?? "", out var voiceName))
                {
                    Debug.LogWarning("[CallDirectorRewire] unknown scenarioId: " + s.scenarioId + " — skipped.");
                    continue;
                }
                var voice = AssetDatabase.LoadAssetAtPath<NPCVoiceProfile>(VoicesFolder + "/" + voiceName + ".asset");
                if (voice == null)
                {
                    Debug.LogWarning("[CallDirectorRewire] missing voice asset: " + voiceName);
                }
                var so = new SerializedObject(s);
                var vp = so.FindProperty("voiceProfile");
                if (vp != null) { vp.objectReferenceValue = voice; so.ApplyModifiedProperties(); }
                EditorUtility.SetDirty(s);
                scenarios.Add(s);
            }

            // Assign scenarios[] on every CallDirector in the active scene.
            int wired = 0;
            foreach (var dir in Object.FindObjectsByType<CallDirector>(FindObjectsSortMode.None))
            {
                var so = new SerializedObject(dir);
                var arr = so.FindProperty("scenarios");
                arr.arraySize = scenarios.Count;
                for (int i = 0; i < scenarios.Count; i++)
                    arr.GetArrayElementAtIndex(i).objectReferenceValue = scenarios[i];
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(dir);
                wired++;
            }

            EditorSceneManager.MarkAllScenesDirty();
            AssetDatabase.SaveAssets();
            Debug.Log($"[CallDirectorRewire] wired {scenarios.Count} scenarios into {wired} CallDirector(s).");
        }
    }
}
