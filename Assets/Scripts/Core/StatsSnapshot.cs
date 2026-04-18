using System;

namespace HackKU.Core
{
    [Serializable]
    public struct StatsSnapshot
    {
        public float money;
        public float happiness;
        public float debt;
        public float startingDebt;
        public int year;
        public string lastReason;
    }
}
