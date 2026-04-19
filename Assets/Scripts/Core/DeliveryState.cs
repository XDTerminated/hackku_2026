namespace HackKU.Core
{
    // Lives in HackKU.Core so UI components here can watch it without referencing HackKU.AI.
    // FoodOrderController writes; DeliveryTimerUI reads.
    public static class DeliveryState
    {
        public static float Remaining;
        public static string Label;
        public static bool HasPending => Remaining > 0.01f;
    }
}
