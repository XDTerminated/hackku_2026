using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace HackKU.Leaderboard.Editor
{
    public static class LeaderboardSetupMenu
    {
        private const string ConfigDir = "Assets/Resources";
        private const string ConfigPath = "Assets/Resources/LeaderboardConfig.asset";

        [MenuItem("HackKU/Leaderboard/Create Default Config Asset")]
        public static void CreateDefaultConfig()
        {
            if (!AssetDatabase.IsValidFolder(ConfigDir))
            {
                Directory.CreateDirectory(ConfigDir);
                AssetDatabase.Refresh();
            }

            var existing = AssetDatabase.LoadAssetAtPath<LeaderboardConfig>(ConfigPath);
            if (existing != null)
            {
                EditorGUIUtility.PingObject(existing);
                Selection.activeObject = existing;
                Debug.Log($"[HackKU.Leaderboard] Config already exists at {ConfigPath}.");
                return;
            }

            var asset = ScriptableObject.CreateInstance<LeaderboardConfig>();
            AssetDatabase.CreateAsset(asset, ConfigPath);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(asset);
            Selection.activeObject = asset;
            Debug.Log($"[HackKU.Leaderboard] Created {ConfigPath}.");
        }

        [MenuItem("HackKU/Leaderboard/Test Server Health")]
        public static void TestHealth()
        {
            var config = AssetDatabase.LoadAssetAtPath<LeaderboardConfig>(ConfigPath);
            if (config == null || string.IsNullOrEmpty(config.baseUrl))
            {
                Debug.LogWarning("[HackKU.Leaderboard] No config asset or baseUrl is empty.");
                return;
            }

            var url = config.baseUrl.TrimEnd('/') + "/api/health";
            EditorCoroutine(HealthRoutine(url));
        }

        private static IEnumerator HealthRoutine(string url)
        {
            using var req = UnityWebRequest.Get(url);
            req.timeout = 10;
            var op = req.SendWebRequest();
            while (!op.isDone) yield return null;

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[HackKU.Leaderboard] health check failed: HTTP {(int)req.responseCode} {req.error} — {url}");
            }
            else
            {
                Debug.Log($"[HackKU.Leaderboard] health OK: {req.downloadHandler.text} ({url})");
            }
        }

        // Minimal editor coroutine driver so we don't need a dependency.
        private static void EditorCoroutine(IEnumerator routine)
        {
            EditorApplication.CallbackFunction step = null;
            step = () =>
            {
                try
                {
                    if (!routine.MoveNext())
                    {
                        EditorApplication.update -= step;
                    }
                }
                catch
                {
                    EditorApplication.update -= step;
                    throw;
                }
            };
            EditorApplication.update += step;
        }
    }
}
