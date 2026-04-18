using System;

namespace HackKU.Core
{
    [Serializable]
    public struct StatsSnapshot
    {
        public float money;
        public float happiness;
        public int year;
        public string lastReason;
    }
}
