using System;
using HackKU.TTS;

namespace HackKU.AI
{
    // Static config for Groq API. Reads GROQ_API_KEY from EnvLoader (same path ElevenLabs uses).
    // Key is resolved lazily; first access throws if the key is missing.
    public static class GroqConfig
    {
        public const string ApiKeyEnvVar = "GROQ_API_KEY";

        private static string _apiKey;
        private static bool _resolved;

        public static string ApiKey
        {
            get
            {
                if (_resolved) return _apiKey;
                EnvLoader.TryGet(ApiKeyEnvVar, out _apiKey);
                _resolved = true;
                if (string.IsNullOrEmpty(_apiKey))
                {
                    throw new InvalidOperationException(
                        $"[GroqConfig] '{ApiKeyEnvVar}' not found. Set it in the process env or in <projectRoot>/.env (or StreamingAssets/.env for shipping builds).");
                }
                return _apiKey;
            }
        }

        public static string ChatModel => "llama-3.1-8b-instant";
        public static string WhisperModel => "whisper-large-v3-turbo";

        public static string BaseUrl => "https://api.groq.com/openai/v1";
        public static string ChatUrl => BaseUrl + "/chat/completions";
        public static string TranscriptionsUrl => BaseUrl + "/audio/transcriptions";

        // For tests / re-resolve after setting env at runtime.
        public static void ResetCache()
        {
            _apiKey = null;
            _resolved = false;
        }
    }
}
