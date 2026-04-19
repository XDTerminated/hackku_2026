namespace HackKU.Core
{
    public static class HappinessMultiplierStack
    {
        public static float TotalBonus { get; private set; }
        public static float TotalMultiplier => 1f + TotalBonus;

        public static void Add(float bonus)
        {
            if (bonus <= 0f) return;
            TotalBonus += bonus;
        }

        public static float ApplyToGain(float rawGain)
        {
            if (rawGain <= 0f) return rawGain;
            return rawGain * TotalMultiplier;
        }

        public static void ResetAll() { TotalBonus = 0f; }
    }
}
