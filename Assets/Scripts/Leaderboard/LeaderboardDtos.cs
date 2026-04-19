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
        public int hunger;
        public int hygiene;
        public int debt;
        public int starting_debt;
        public int invested;
        public int year;
        public int composite_score;
    }

    [Serializable]
    public class LeaderboardEntry
    {
        public string display_name;
        public int money;
        public int happiness;
        public int hunger;
        public int hygiene;
        public int debt;
        public int starting_debt;
        public int invested;
        public int year;
        public int composite_score;
        public int rank;
    }

    [Serializable]
    public class LeaderboardResponse
    {
        public LeaderboardEntry[] entries;
    }

    [Serializable]
    public struct MetricsSample
    {
        public int money;
        public int happiness;
        public int hunger;
        public int hygiene;
        public int debt;
        public int startingDebt;
        public int invested;
        public int year;
        public int compositeScore;
    }
}
