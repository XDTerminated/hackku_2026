using UnityEngine;

namespace HackKU.Leaderboard
{
    [CreateAssetMenu(menuName = "HackKU/Leaderboard/Config", fileName = "LeaderboardConfig")]
    public class LeaderboardConfig : ScriptableObject
    {
        [Header("API")]
        [Tooltip("Base URL of the leaderboard server, e.g. http://localhost:3000 or https://<your-host>.")]
        public string baseUrl = "http://localhost:3000";

        [Tooltip("Env var (read from .env) holding the shared write key. Must match WRITE_KEY on the server.")]
        public string writeKeyEnvVar = "LEADERBOARD_WRITE_KEY";

        [Header("Throttling")]
        [Tooltip("Minimum seconds between upsert requests. Rapid changes are coalesced into one POST.")]
        [Min(0f)] public float throttleSeconds = 1.0f;

        [Header("Networking")]
        [Min(1)] public int timeoutSeconds = 15;

        [Header("Debug")]
        public bool verboseLogging = false;

        private const string DefaultResourcesName = "LeaderboardConfig";
        private static LeaderboardConfig _cached;

        public static LeaderboardConfig Load()
        {
            if (_cached != null) return _cached;
            _cached = Resources.Load<LeaderboardConfig>(DefaultResourcesName);
            if (_cached == null)
            {
                Debug.LogError(
                    $"[LeaderboardConfig] Missing 'Assets/Resources/{DefaultResourcesName}.asset'. " +
                    "Create one via HackKU/Leaderboard/Create Default Config Asset.");
            }
            return _cached;
        }
    }
}
