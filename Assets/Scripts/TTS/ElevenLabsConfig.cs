using UnityEngine;

namespace HackKU.TTS
{
    [CreateAssetMenu(menuName = "HackKU/TTS/ElevenLabs Config", fileName = "ElevenLabsConfig")]
    public class ElevenLabsConfig : ScriptableObject
    {
        [Header("API")]
        public string baseUrl = "https://api.elevenlabs.io/v1";

        [Tooltip("Name of the env var to read the API key from (in .env or process env).")]
        public string apiKeyEnvVar = "ELEVENLABS_API_KEY";

        [Header("Model")]
        [Tooltip("Default model. 'eleven_multilingual_v2' for quality, 'eleven_flash_v2_5' for low latency.")]
        public string defaultModelId = "eleven_multilingual_v2";

        [Header("Audio")]
        [Tooltip("ElevenLabs output_format query param. Must be a PCM variant (e.g. pcm_44100, pcm_22050).")]
        public string outputFormat = "pcm_44100";

        [Tooltip("Must match the numeric sample rate in outputFormat.")]
        public int sampleRate = 44100;

        [Header("Networking")]
        public int timeoutSeconds = 30;

        [Header("Cache")]
        public bool enableMemoryCache = true;
        [Min(1)] public int maxCacheEntries = 64;

        [Header("Debug")]
        public bool verboseLogging = false;

        private const string DefaultResourcesName = "ElevenLabsConfig";
        private static ElevenLabsConfig _cached;

        public static ElevenLabsConfig Load()
        {
            if (_cached != null) return _cached;
            _cached = Resources.Load<ElevenLabsConfig>(DefaultResourcesName);
            if (_cached == null)
            {
                Debug.LogError(
                    $"[ElevenLabsConfig] Missing 'Assets/Resources/{DefaultResourcesName}.asset'. " +
                    "Create one via Assets/Create/HackKU/TTS/ElevenLabs Config and place it under Resources/.");
            }
            return _cached;
        }
    }
}
