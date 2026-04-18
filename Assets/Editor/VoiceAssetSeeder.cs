using System.IO;
using HackKU.TTS;
using UnityEditor;
using UnityEngine;

namespace HackKU.EditorTools
{
    // Seeds a set of NPCVoiceProfile assets using well-known ElevenLabs public voice IDs.
    // Chosen so each caller archetype in the game has a distinct voice.
    public static class VoiceAssetSeeder
    {
        const string VoiceFolder = "Assets/Data/Voices";

        // All voice IDs below are from ElevenLabs' PREMADE (free-tier) set — they work without a paid plan.
        static readonly VoiceSeed[] Seeds = new[]
        {
            // Mom — Lily (British female, middle-aged, soft). Warm caring-mom energy.
            new VoiceSeed("MomVoice", "Mom (Lily)", "pFZP5JQG7iQjIQuC4Bku", 0.5f, 0.75f, 0.2f),
            // Boss — Brian (American male, middle-aged, deep). Corporate authority.
            new VoiceSeed("BossVoice", "Boss (Brian)", "nPczCjzI2devNBz1zQrb", 0.4f, 0.8f, 0.0f),
            // Buddy — Liam (American male, young, articulate). Casual friend vibe.
            new VoiceSeed("BuddyVoice", "Buddy (Liam)", "TX3LPaxmHKxFdv7VOQHJ", 0.55f, 0.65f, 0.3f),
            // Sibling — Sarah (American female, young, soft).
            new VoiceSeed("SiblingVoice", "Sibling (Sarah)", "EXAVITQu4vr4xnSDxMaL", 0.55f, 0.7f, 0.25f),
            // GroceryClerk — Daniel (British male, middle-aged, authoritative).
            new VoiceSeed("GroceryClerkVoice", "Grocery Clerk (Daniel)", "onwK4e9ZLuTAKqWW03F9", 0.45f, 0.8f, 0.0f),
            // PizzaGuy — Callum (American male, middle-aged, hoarse). Late-night pizza energy.
            new VoiceSeed("PizzaGuyVoice", "Pizza Guy (Callum)", "N2lVS1w4EtoT3dr4eOWO", 0.5f, 0.7f, 0.15f),
            // GymSales — Bill (American male, old, strong). Pushy upsell energy.
            new VoiceSeed("GymSalesVoice", "Gym Sales (Bill)", "pqHfZKP75CvOlQylNhV4", 0.35f, 0.75f, 0.4f),
            // DebtCollector — George (British male, middle-aged, authoritative). Serious bill-collector vibe.
            new VoiceSeed("DebtCollectorVoice", "Debt Collector (George)", "JBFqnCBsd6RMkjVDRZzb", 0.4f, 0.85f, 0.0f),
            // Therapist — Alice (British female, middle-aged, pleasant). Calm measured therapist.
            new VoiceSeed("TherapistVoice", "Therapist (Alice)", "Xb7hH8MSUJpSbSDYk0k2", 0.65f, 0.7f, 0.15f),
        };

        [MenuItem("HackKU/Seed/Create/Refresh All Voices")]
        public static void RunSeed()
        {
            EnsureFolder(VoiceFolder);
            foreach (var seed in Seeds) UpsertVoice(seed);
            AttachMomVoiceToScenario();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[VoiceAssetSeeder] seeded " + Seeds.Length + " voice profiles.");
        }

        static void UpsertVoice(VoiceSeed seed)
        {
            var path = VoiceFolder + "/" + seed.FileName + ".asset";
            var v = AssetDatabase.LoadAssetAtPath<NPCVoiceProfile>(path);
            if (v == null)
            {
                v = ScriptableObject.CreateInstance<NPCVoiceProfile>();
                AssetDatabase.CreateAsset(v, path);
            }
            v.displayName = seed.DisplayName;
            v.voiceId = seed.VoiceId;
            v.voiceSettings.stability = seed.Stability;
            v.voiceSettings.similarity_boost = seed.SimilarityBoost;
            v.voiceSettings.style = seed.Style;
            EditorUtility.SetDirty(v);
        }

        static void AttachMomVoiceToScenario()
        {
            var scenario = AssetDatabase.LoadAssetAtPath<HackKU.AI.CallScenario>("Assets/Data/Scenarios/MomWedding.asset");
            var momVoice = AssetDatabase.LoadAssetAtPath<NPCVoiceProfile>(VoiceFolder + "/MomVoice.asset");
            if (scenario == null || momVoice == null) return;
            var so = new SerializedObject(scenario);
            var prop = so.FindProperty("voiceProfile");
            if (prop != null)
            {
                prop.objectReferenceValue = momVoice;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(scenario);
            }
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        readonly struct VoiceSeed
        {
            public readonly string FileName;
            public readonly string DisplayName;
            public readonly string VoiceId;
            public readonly float Stability;
            public readonly float SimilarityBoost;
            public readonly float Style;

            public VoiceSeed(string fileName, string displayName, string voiceId, float stability, float similarityBoost, float style)
            {
                FileName = fileName; DisplayName = displayName; VoiceId = voiceId;
                Stability = stability; SimilarityBoost = similarityBoost; Style = style;
            }
        }
    }
}
