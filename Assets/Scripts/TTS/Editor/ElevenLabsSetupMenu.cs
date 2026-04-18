using System.IO;
using UnityEditor;
using UnityEngine;

namespace HackKU.TTS.Editor
{
    public static class ElevenLabsSetupMenu
    {
        private const string ConfigDir = "Assets/Resources";
        private const string ConfigPath = "Assets/Resources/ElevenLabsConfig.asset";

        [MenuItem("HackKU/TTS/Create Default Config Asset")]
        public static void CreateDefaultConfig()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                Directory.CreateDirectory(ConfigDir);
                AssetDatabase.Refresh();
            }

            var existing = AssetDatabase.LoadAssetAtPath<ElevenLabsConfig>(ConfigPath);
            if (existing != null)
            {
                EditorGUIUtility.PingObject(existing);
                Selection.activeObject = existing;
                Debug.Log($"[HackKU.TTS] Config already exists at {ConfigPath}.");
                return;
            }

            var asset = ScriptableObject.CreateInstance<ElevenLabsConfig>();
            AssetDatabase.CreateAsset(asset, ConfigPath);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(asset);
            Selection.activeObject = asset;
            Debug.Log($"[HackKU.TTS] Created {ConfigPath}.");
        }

        [MenuItem("HackKU/TTS/Test Env Loader")]
        public static void TestEnvLoader()
        {
            var config = AssetDatabase.LoadAssetAtPath<ElevenLabsConfig>(ConfigPath);
            var varName = config != null ? config.apiKeyEnvVar : "ELEVENLABS_API_KEY";

            if (EnvLoader.TryGet(varName, out var value))
            {
                var masked = value.Length <= 8 ? new string('*', value.Length) : value.Substring(0, 4) + new string('*', value.Length - 8) + value.Substring(value.Length - 4);
                Debug.Log($"[HackKU.TTS] EnvLoader found {varName} = {masked} (len={value.Length}).");
            }
            else
            {
                Debug.LogWarning($"[HackKU.TTS] EnvLoader could NOT find {varName}. Put it in <projectRoot>/.env or StreamingAssets/.env.");
            }
        }
    }
}
