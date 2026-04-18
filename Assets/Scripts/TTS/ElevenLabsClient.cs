using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace HackKU.TTS
{
    [DisallowMultipleComponent]
    public class ElevenLabsClient : MonoBehaviour
    {
        private static ElevenLabsClient _instance;
        public static ElevenLabsClient Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var go = new GameObject("[ElevenLabsClient]");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<ElevenLabsClient>();
                return _instance;
            }
        }

        private ElevenLabsConfig _config;
        private string _apiKey;
        private bool _keyResolved;

        private readonly Dictionary<string, AudioClip> _cache = new();
        private readonly LinkedList<string> _cacheOrder = new();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        public void Synthesize(
            string text,
            NPCVoiceProfile profile,
            Action<AudioClip> onSuccess,
            Action<string> onError = null)
        {
            SynthesizeCoroutine(text, profile, onSuccess, onError);
        }

        public Coroutine SynthesizeCoroutine(
            string text,
            NPCVoiceProfile profile,
            Action<AudioClip> onSuccess,
            Action<string> onError = null)
        {
            return StartCoroutine(SynthesizeRoutine(text, profile, onSuccess, onError));
        }

        private IEnumerator SynthesizeRoutine(
            string text,
            NPCVoiceProfile profile,
            Action<AudioClip> onSuccess,
            Action<string> onError)
        {
            if (!EnsureReady(out var error))
            {
                Report(error, onError);
                yield break;
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                Report("Text is empty.", onError);
                yield break;
            }
            if (profile == null || string.IsNullOrEmpty(profile.voiceId))
            {
                Report("NPCVoiceProfile or voiceId is missing.", onError);
                yield break;
            }

            var config = _config;
            var modelId = profile.ResolveModelId(config);
            var settings = profile.voiceSettings ?? new VoiceSettings();
            var cacheKey = BuildCacheKey(text, profile.voiceId, modelId, settings, config.outputFormat);

            if (config.enableMemoryCache && _cache.TryGetValue(cacheKey, out var cached) && cached != null)
            {
                if (config.verboseLogging) Debug.Log($"[ElevenLabs] cache HIT '{Preview(text)}'");
                TouchCacheEntry(cacheKey);
                onSuccess?.Invoke(cached);
                yield break;
            }

            var body = new TTSRequestBody
            {
                text = text,
                model_id = modelId,
                voice_settings = settings.Clone(),
            };
            var json = JsonUtility.ToJson(body);
            var payload = Encoding.UTF8.GetBytes(json);

            // Use the /stream endpoint so audio starts arriving token-by-token.
            // Combined with DownloadHandlerAudioClip.streamAudio=true, playback can begin
            // before the full download finishes. No temp-file I/O on the main thread.
            var url = $"{config.baseUrl}/text-to-speech/{UnityWebRequest.EscapeURL(profile.voiceId)}/stream?output_format={UnityWebRequest.EscapeURL(config.outputFormat)}";

            bool isMp3 = config.outputFormat.StartsWith("mp3");
            bool isPcm = config.outputFormat.StartsWith("pcm");

            AudioClip clip = null;

            if (isMp3)
            {
                using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
                req.uploadHandler = new UploadHandlerRaw(payload) { contentType = "application/json" };
                var audioHandler = new DownloadHandlerAudioClip(url, AudioType.MPEG);
                audioHandler.streamAudio = true;
                req.downloadHandler = audioHandler;
                req.SetRequestHeader("xi-api-key", _apiKey);
                req.SetRequestHeader("Content-Type", "application/json");
                req.SetRequestHeader("Accept", "audio/mpeg");
                req.timeout = config.timeoutSeconds;

                if (config.verboseLogging) Debug.Log($"[ElevenLabs] POST-stream {url} — '{Preview(text)}'");

                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    // ElevenLabs encodes error reason in the response body JSON; surface it
                    // so we can tell "quota_exceeded" vs "free-tier can't stream" vs "voice paid-only".
                    string errBody = TryReadErrorBody(req);
                    Report($"HTTP {(int)req.responseCode} {req.error} | {errBody}", onError);
                    yield break;
                }

                clip = DownloadHandlerAudioClip.GetContent(req);
                if (clip != null) clip.name = $"TTS_{profile.voiceId}_{cacheKey.Substring(0, 8)}";
            }
            else if (isPcm)
            {
                using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
                req.uploadHandler = new UploadHandlerRaw(payload) { contentType = "application/json" };
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("xi-api-key", _apiKey);
                req.SetRequestHeader("Content-Type", "application/json");
                req.SetRequestHeader("Accept", "audio/pcm");
                req.timeout = config.timeoutSeconds;

                if (config.verboseLogging) Debug.Log($"[ElevenLabs] POST {url} — '{Preview(text)}'");

                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    string errBody = TryReadErrorBody(req);
                    Report($"HTTP {(int)req.responseCode} {req.error} | {errBody}", onError);
                    yield break;
                }

                var pcm = req.downloadHandler.data;
                clip = PcmAudioClipBuilder.FromPcm16(pcm, config.sampleRate, 1, $"TTS_{profile.voiceId}_{cacheKey.Substring(0, 8)}");
            }

            if (clip == null)
            {
                Report("Failed to decode audio response (format=" + config.outputFormat + ").", onError);
                yield break;
            }

            if (config.enableMemoryCache)
                StoreInCache(cacheKey, clip, config.maxCacheEntries);

            if (config.verboseLogging) Debug.Log($"[ElevenLabs] ok, clip len={clip.length:F2}s samples={clip.samples}");
            onSuccess?.Invoke(clip);
        }

        private bool EnsureReady(out string error)
        {
            _config ??= ElevenLabsConfig.Load();
            if (_config == null)
            {
                error = "ElevenLabsConfig not found under Resources/.";
                return false;
            }

            if (!_keyResolved)
            {
                EnvLoader.TryGet(_config.apiKeyEnvVar, out _apiKey);
                _keyResolved = true;
            }

            if (string.IsNullOrEmpty(_apiKey))
            {
                error = $"API key '{_config.apiKeyEnvVar}' not found in env or .env.";
                return false;
            }

            error = null;
            return true;
        }

        public void ClearCache()
        {
            _cache.Clear();
            _cacheOrder.Clear();
        }

        private void TouchCacheEntry(string key)
        {
            if (_cacheOrder.Find(key) is { } node)
            {
                _cacheOrder.Remove(node);
                _cacheOrder.AddLast(node);
            }
        }

        private void StoreInCache(string key, AudioClip clip, int maxEntries)
        {
            if (_cache.ContainsKey(key))
            {
                _cache[key] = clip;
                TouchCacheEntry(key);
                return;
            }
            _cache[key] = clip;
            _cacheOrder.AddLast(key);
            while (_cacheOrder.Count > maxEntries)
            {
                var oldest = _cacheOrder.First;
                _cacheOrder.RemoveFirst();
                _cache.Remove(oldest.Value);
            }
        }

        private static string BuildCacheKey(string text, string voiceId, string modelId, VoiceSettings settings, string format)
        {
            var input = $"{voiceId}|{modelId}|{format}|{settings.StableHash()}|{text}";
            using var sha = SHA1.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        private static string Preview(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Length <= 40 ? text : text.Substring(0, 40) + "...";
        }

        private static void Report(string message, Action<string> onError)
        {
            Debug.LogError("[ElevenLabs] " + message);
            onError?.Invoke(message);
        }

        // ElevenLabs returns a JSON error body like
        //   {"detail":{"status":"quota_exceeded","message":"..."}}
        // Surface whatever fragment we can.
        private static string TryReadErrorBody(UnityWebRequest req)
        {
            try
            {
                if (req?.downloadHandler == null) return "";
                var text = req.downloadHandler.text;
                if (string.IsNullOrEmpty(text))
                {
                    var data = req.downloadHandler.data;
                    if (data != null && data.Length > 0) text = Encoding.UTF8.GetString(data);
                }
                if (string.IsNullOrEmpty(text)) return "";
                return text.Length > 300 ? text.Substring(0, 300) : text;
            }
            catch { return ""; }
        }
    }
}
