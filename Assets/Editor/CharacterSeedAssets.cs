using System.IO;
using HackKU.Core;
using UnityEditor;
using UnityEngine;

namespace HackKU.EditorTools
{
    /// <summary>
    /// Drops a starter roster of <see cref="CharacterProfile"/> assets into
    /// <c>Assets/Data/Characters/</c> so the Character Select screen has
    /// something to show on first run of the project.
    ///
    /// The seeder will NEVER overwrite an existing asset — if a profile with
    /// the same file name is already on disk it's left alone. Re-run the menu
    /// item to add freshly-added seeds without stomping designer tweaks.
    /// </summary>
    [InitializeOnLoad]
    public static class CharacterSeedAssets
    {
        private const string OutputFolder = "Assets/Data/Characters";
        private const string SessionFlag = "HackKU.CharacterSeedAssets.Ran";
        private const string MenuItemPath = "HackKU/Seed/Create Character Assets";

        static CharacterSeedAssets()
        {
            // Only bootstrap once per editor session to keep domain reloads quiet.
            if (SessionState.GetBool(SessionFlag, false))
            {
                return;
            }
            SessionState.SetBool(SessionFlag, true);

            // Defer until the editor finishes its first update so AssetDatabase
            // is fully ready (important right after a domain reload).
            EditorApplication.delayCall += () => SeedIfMissing(logNoOp: false);
        }

        [MenuItem(MenuItemPath)]
        public static void SeedFromMenu()
        {
            int created = SeedIfMissing(logNoOp: true);
            EditorUtility.DisplayDialog(
                "HackKU Character Seeds",
                created == 0
                    ? "All seed characters already exist on disk. Nothing to do."
                    : $"Created {created} character profile asset(s) under {OutputFolder}/.",
                "OK");
        }

        private static int SeedIfMissing(bool logNoOp)
        {
            EnsureFolder(OutputFolder);

            SeedDefinition[] seeds = GetSeedDefinitions();
            int created = 0;

            foreach (SeedDefinition seed in seeds)
            {
                string assetPath = $"{OutputFolder}/{seed.fileName}.asset";
                CharacterProfile existing = AssetDatabase.LoadAssetAtPath<CharacterProfile>(assetPath);
                if (existing != null)
                {
                    continue;
                }

                CharacterProfile profile = ScriptableObject.CreateInstance<CharacterProfile>();
                profile.characterName = seed.characterName;
                profile.description = seed.description;
                profile.startingMoney = seed.startingMoney;
                profile.startingHappiness = seed.startingHappiness;
                profile.yearlyIncome = seed.yearlyIncome;
                profile.yearlyExpenses = seed.yearlyExpenses;
                profile.yearlyHappinessRegen = seed.yearlyHappinessRegen;
                profile.gimmickTag = seed.gimmickTag;

                AssetDatabase.CreateAsset(profile, assetPath);
                created++;
                Debug.Log($"[CharacterSeedAssets] Created {assetPath}");
            }

            if (created > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            else if (logNoOp)
            {
                Debug.Log("[CharacterSeedAssets] No missing seeds — nothing created.");
            }

            return created;
        }

        private static void EnsureFolder(string assetFolderPath)
        {
            if (AssetDatabase.IsValidFolder(assetFolderPath))
            {
                return;
            }

            string[] parts = assetFolderPath.Split('/');
            string current = parts[0]; // "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }

            // Disk-side sanity — AssetDatabase should now agree it exists.
            string abs = Path.Combine(Directory.GetCurrentDirectory(), assetFolderPath);
            if (!Directory.Exists(abs))
            {
                Directory.CreateDirectory(abs);
            }
        }

        private static SeedDefinition[] GetSeedDefinitions()
        {
            return new[]
            {
                new SeedDefinition
                {
                    fileName = "CorporateClimber",
                    characterName = "The Corporate Climber",
                    gimmickTag = "Golden Handcuffs",
                    description =
                        "Big paycheck, bigger obligations. You already own the suit, the lease, and the espresso machine. " +
                        "The money flows — as long as you keep grinding. The joy? That's on backorder.",
                    startingMoney = 8200f,
                    startingHappiness = 34f,
                    yearlyIncome = 76000f,
                    yearlyExpenses = 18500f,
                    yearlyHappinessRegen = -2f,
                },
                new SeedDefinition
                {
                    fileName = "EasygoingBarista",
                    characterName = "The Easygoing Barista",
                    gimmickTag = "Zen & Tips",
                    description =
                        "Rent is covered, latte art is crisp, regulars tip in dollars and smiles. " +
                        "You won't get rich pulling espresso, but you're probably the happiest person at the register.",
                    startingMoney = 1500f,
                    startingHappiness = 74f,
                    yearlyIncome = 32000f,
                    yearlyExpenses = 20000f,
                    yearlyHappinessRegen = 3f,
                },
                new SeedDefinition
                {
                    fileName = "GradStudent",
                    characterName = "The Grad Student",
                    gimmickTag = "Ramen & Dreams",
                    description =
                        "Tuition ate the savings, the stipend barely covers rent, and the dissertation is always 'almost done.' " +
                        "Still — the library is free, friends are close, and the future feels possible.",
                    startingMoney = -1200f,
                    startingHappiness = 55f,
                    yearlyIncome = 24500f,
                    yearlyExpenses = 19000f,
                    yearlyHappinessRegen = 1f,
                },
            };
        }

        private struct SeedDefinition
        {
            public string fileName;
            public string characterName;
            public string gimmickTag;
            public string description;
            public float startingMoney;
            public float startingHappiness;
            public float yearlyIncome;
            public float yearlyExpenses;
            public float yearlyHappinessRegen;
        }
    }
}
