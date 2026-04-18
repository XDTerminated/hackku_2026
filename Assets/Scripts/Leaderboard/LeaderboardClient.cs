using System;
using System.Collections;
using System.Text;
using HackKU.TTS;
using UnityEngine;
using UnityEngine.Networking;

namespace HackKU.Leaderboard
{
    [DisallowMultipleComponent]
    public class LeaderboardClient : MonoBehaviour
    {
        private static LeaderboardClient _instance;
        public static LeaderboardClient Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var go = new GameObject("[LeaderboardClient]");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<LeaderboardClient>();
                return _instance;
            }
        }

        private LeaderboardConfig _config;
        private string _writeKey;
        private bool _keyResolved;

        private int _pendingMoney;
        private int _pendingHappiness;
        private bool _dirty;
        private bool _inFlight;
        private float _nextAllowedSendTime;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        // Fire-and-forget. Rapid calls are coalesced: within throttleSeconds,
        // only the latest money/happiness values will be sent.
        public void UpsertStats(int money, int happiness)
        {
            _pendingMoney = money;
            _pendingHappiness = happiness;
            _dirty = true;
            TrySendPending();
        }

        // Force-send immediately, bypassing the throttle. Useful on quit/save-point.
        public void UpsertStatsNow(int money, int happiness, Action onSuccess = null, Action<string> onError = null)
        {
            StartCoroutine(SendOnce(money, happiness, onSuccess, onError));
        }

        public void GetLeaderboard(int limit, Action<LeaderboardEntry[]> onSuccess, Action<string> onError = null)
        {
            StartCoroutine(GetLeaderboardRoutine(limit, onSuccess, onError));
        }

        private void TrySendPending()
        {
            if (!_dirty || _inFlight) return;
            if (Time.unscaledTime < _nextAllowedSendTime)
            {
                StartCoroutine(WaitThenTrySend(_nextAllowedSendTime - Time.unscaledTime));
                return;
            }
            var money = _pendingMoney;
            var happiness = _pendingHappiness;
            _dirty = false;
            StartCoroutine(SendOnce(money, happiness, null, null));
        }

        private IEnumerator WaitThenTrySend(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            TrySendPending();
        }

        private IEnumerator SendOnce(int money, int happiness, Action onSuccess, Action<string> onError)
        {
            if (!EnsureReady(out var err))
            {
                Report(err, onError);
                yield break;
            }

            _inFlight = true;

            var body = new UpsertRequest
            {
                player_id = PlayerIdentity.GetOrCreateId(),
                display_name = PlayerIdentity.DisplayName,
                money = money,
                happiness = happiness,
            };
            var json = JsonUtility.ToJson(body);
            var payload = Encoding.UTF8.GetBytes(json);
            var url = $"{_config.baseUrl.TrimEnd('/')}/api/stats";

            using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            req.uploadHandler = new UploadHandlerRaw(payload) { contentType = "application/json" };
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Accept", "application/json");
            if (!string.IsNullOrEmpty(_writeKey)) req.SetRequestHeader("x-write-key", _writeKey);
            req.timeout = _config.timeoutSeconds;

            if (_config.verboseLogging) Debug.Log($"[Leaderboard] POST {url} money={money} happiness={happiness}");

            yield return req.SendWebRequest();

            _inFlight = false;
            _nextAllowedSendTime = Time.unscaledTime + Mathf.Max(0f, _config.throttleSeconds);

            if (req.result != UnityWebRequest.Result.Success)
            {
                var msg = $"HTTP {(int)req.responseCode} {req.error}";
                if (!string.IsNullOrEmpty(req.downloadHandler?.text) && req.downloadHandler.text.Length < 1024)
                    msg += " | " + req.downloadHandler.text;
                Report(msg, onError);
                // If more changes queued up during the request, send again.
                TrySendPending();
                yield break;
            }

            if (_config.verboseLogging) Debug.Log($"[Leaderboard] ok {req.downloadHandler.text}");
            onSuccess?.Invoke();

            // Trailing-edge: if the user mutated stats mid-request, send again.
            TrySendPending();
        }

        private IEnumerator GetLeaderboardRoutine(int limit, Action<LeaderboardEntry[]> onSuccess, Action<string> onError)
        {
            if (!EnsureReady(out var err))
            {
                Report(err, onError);
                yield break;
            }

            var clamped = Mathf.Clamp(limit, 1, 100);
            var url = $"{_config.baseUrl.TrimEnd('/')}/api/leaderboard?limit={clamped}";

            using var req = UnityWebRequest.Get(url);
            req.SetRequestHeader("Accept", "application/json");
            req.timeout = _config.timeoutSeconds;

            if (_config.verboseLogging) Debug.Log($"[Leaderboard] GET {url}");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                var msg = $"HTTP {(int)req.responseCode} {req.error}";
                if (!string.IsNullOrEmpty(req.downloadHandler?.text) && req.downloadHandler.text.Length < 1024)
                    msg += " | " + req.downloadHandler.text;
                Report(msg, onError);
                yield break;
            }

            LeaderboardResponse parsed;
            try
            {
                parsed = JsonUtility.FromJson<LeaderboardResponse>(req.downloadHandler.text);
            }
            catch (Exception ex)
            {
                Report("Failed to parse leaderboard response: " + ex.Message, onError);
                yield break;
            }

            var entries = parsed?.entries ?? Array.Empty<LeaderboardEntry>();
            if (_config.verboseLogging) Debug.Log($"[Leaderboard] received {entries.Length} entries");
            onSuccess?.Invoke(entries);
        }

        private bool EnsureReady(out string error)
        {
            _config ??= LeaderboardConfig.Load();
            if (_config == null)
            {
                error = "LeaderboardConfig not found under Resources/.";
                return false;
            }
            if (string.IsNullOrEmpty(_config.baseUrl))
            {
                error = "LeaderboardConfig.baseUrl is empty.";
                return false;
            }
            if (!_keyResolved)
            {
                if (!string.IsNullOrEmpty(_config.writeKeyEnvVar))
                    EnvLoader.TryGet(_config.writeKeyEnvVar, out _writeKey);
                _keyResolved = true;
            }
            error = null;
            return true;
        }

        private static void Report(string message, Action<string> onError)
        {
            Debug.LogError("[Leaderboard] " + message);
            onError?.Invoke(message);
        }
    }
}
