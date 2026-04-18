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
            // Expressive settings: lower stability (more emotion/variation), higher style (stronger delivery).
            new VoiceSeed("MomVoice", "Mom (Lily)", "pFZP5JQG7iQjIQuC4Bku", 0.30f, 0.75f, 0.55f),
            new VoiceSeed("BossVoice", "Boss (Brian)", "nPczCjzI2devNBz1zQrb", 0.35f, 0.80f, 0.45f),
            new VoiceSeed("BuddyVoice", "Buddy (Liam)", "TX3LPaxmHKxFdv7VOQHJ", 0.30f, 0.70f, 0.60f),
            new VoiceSeed("SiblingVoice", "Sibling (Sarah)", "EXAVITQu4vr4xnSDxMaL", 0.28f, 0.75f, 0.55f),
            new VoiceSeed("GroceryClerkVoice", "Grocery Clerk (Daniel)", "onwK4e9ZLuTAKqWW03F9", 0.40f, 0.80f, 0.35f),
            new VoiceSeed("PizzaGuyVoice", "Pizza Guy (Callum)", "N2lVS1w4EtoT3dr4eOWO", 0.30f, 0.75f, 0.55f),
            new VoiceSeed("GymSalesVoice", "Gym Sales (Bill)", "pqHfZKP75CvOlQylNhV4", 0.25f, 0.75f, 0.65f),
            new VoiceSeed("DebtCollectorVoice", "Debt Collector (George)", "JBFqnCBsd6RMkjVDRZzb", 0.35f, 0.85f, 0.45f),
            new VoiceSeed("TherapistVoice", "Therapist (Alice)", "Xb7hH8MSUJpSbSDYk0k2", 0.45f, 0.75f, 0.40f),
            // Additional personalities for 12 total callers.
            // Landlord — Chris (American male, middle-aged). Stern, transactional.
            new VoiceSeed("LandlordVoice", "Landlord (Chris)", "iP95p4xoKVk53GoZ742B", 0.35f, 0.80f, 0.45f),
            // Dad — George (older British male). Gruff but caring.
            new VoiceSeed("DadVoice", "Dad (George)", "JBFqnCBsd6RMkjVDRZzb", 0.35f, 0.80f, 0.50f),
            // ScamCaller — Matilda (smooth female). Too-friendly phishing energy.
            new VoiceSeed("ScamVoice", "Scam Caller (Matilda)", "XrExE9yKIg1WjnnlVkGX", 0.30f, 0.80f, 0.55f),
            // Insurance — Jessica (bright female). Peppy upsell agent.
            new VoiceSeed("InsuranceVoice", "Insurance Agent (Jessica)", "cgSgspJ2msm6clMCkdW9", 0.30f, 0.75f, 0.55f),
            // Dentist — Charlotte (soft female). Clinical receptionist.
            new VoiceSeed("DentistVoice", "Dentist Office (Charlotte)", "XB0fDUnXU5powFXDhCwa", 0.45f, 0.75f, 0.35f),
            // FancyRestaurant — Laurent stand-in via Daniel (British male). Formal maitre d'.
            new VoiceSeed("FancyRestaurantVoice", "Fancy Restaurant (Daniel)", "onwK4e9ZLuTAKqWW03F9", 0.40f, 0.80f, 0.40f),
            // FastFood — Liam (young American male). Energetic cashier.
            new VoiceSeed("FastFoodVoice", "Fast Food (Liam)", "TX3LPaxmHKxFdv7VOQHJ", 0.25f, 0.70f, 0.65f),
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
