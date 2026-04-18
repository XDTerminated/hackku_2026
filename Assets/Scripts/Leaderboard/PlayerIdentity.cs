using System;
using UnityEngine;

namespace HackKU.Leaderboard
{
    // Persists a stable per-install player UUID + display name in PlayerPrefs.
    // UUID lets the server upsert without ever seeing the player's machine/name change.
    public static class PlayerIdentity
    {
        private const string IdKey = "hackku.leaderboard.playerId";
        private const string NameKey = "hackku.leaderboard.displayName";
        private const string DefaultName = "Player";

        public static string GetOrCreateId()
        {
            var id = PlayerPrefs.GetString(IdKey, string.Empty);
            if (string.IsNullOrEmpty(id))
            {
                id = Guid.NewGuid().ToString();
                PlayerPrefs.SetString(IdKey, id);
                PlayerPrefs.Save();
            }
            return id;
        }

        public static string DisplayName
        {
            get
            {
                var name = PlayerPrefs.GetString(NameKey, string.Empty);
                return string.IsNullOrWhiteSpace(name) ? DefaultName : name;
            }
            set
            {
                var clean = string.IsNullOrWhiteSpace(value) ? DefaultName : value.Trim();
                if (clean.Length > 64) clean = clean.Substring(0, 64);
                PlayerPrefs.SetString(NameKey, clean);
                PlayerPrefs.Save();
            }
        }
    }
}
