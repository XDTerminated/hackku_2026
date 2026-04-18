using System;

namespace HackKU.Leaderboard
{
    [Serializable]
    public class UpsertRequest
    {
        public string player_id;
        public string display_name;
        public int money;
        public int happiness;
    }

    [Serializable]
    public class LeaderboardEntry
    {
        public string display_name;
        public int money;
        public int happiness;
        public int score;
        public int rank;
    }

    [Serializable]
    public class LeaderboardResponse
    {
        public LeaderboardEntry[] entries;
    }
}
