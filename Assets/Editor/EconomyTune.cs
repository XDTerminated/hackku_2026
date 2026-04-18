using HackKU.AI;
using HackKU.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HackKU.EditorTools
{
    public static class EconomyTune
    {
        // Per-profile income tune — drops from the old ~$32-75k range to something where
        // a single bill or call decision actually moves the needle.
        static readonly (string asset, float newIncome, float newExpenses, float newStartingMoney)[] ProfileTune = new[]
        {
            ("Assets/Data/Characters/CorporateClimber.asset",   20000f,  6000f,  4000f),
            ("Assets/Data/Characters/EasygoingBarista.asset",   12000f,  5000f,  1500f),
            ("Assets/Data/Characters/GradStudent.asset",         8000f,  4000f,  -800f),
        };

        [MenuItem("HackKU/Fix/Tune Economy + Call Cadence")]
        public static void Run()
        {
            TuneProfiles();
            TuneCallDirector();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("[EconomyTune] done.");
        }

        static void TuneProfiles()
        {
            foreach (var p in ProfileTune)
            {
                var profile = AssetDatabase.LoadAssetAtPath<CharacterProfile>(p.asset);
                if (profile == null) { Debug.LogWarning("[EconomyTune] missing " + p.asset); continue; }
                profile.yearlyIncome = p.newIncome;
                profile.yearlyExpenses = p.newExpenses;
                profile.startingMoney = p.newStartingMoney;
                EditorUtility.SetDirty(profile);
                Debug.Log("[EconomyTune] " + profile.characterName + " -> income $" + p.newIncome + ", expenses $" + p.newExpenses + ", start $" + p.newStartingMoney);
            }
        }

        static void TuneCallDirector()
        {
            var dir = Object.FindFirstObjectByType<CallDirector>();
            if (dir == null) { Debug.LogWarning("[EconomyTune] no CallDirector"); return; }
            var so = new SerializedObject(dir);
            Set(so, "minGapSeconds", 3f);
            Set(so, "maxGapSeconds", 10f);
            Set(so, "firstCallDelay", 5f);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(dir);
            Debug.Log("[EconomyTune] CallDirector: gap 3-10s, firstCall 5s");
        }

        static void Set(SerializedObject so, string name, float v)
        {
            var p = so.FindProperty(name);
            if (p != null) p.floatValue = v;
        }
    }
}
