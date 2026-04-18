namespace HackKU.AI
{
    /// <summary>
    /// Static repository of <see cref="ToolDef"/>s we expose to the LLM. Because
    /// <c>parametersJson</c> is a raw JSON-Schema string spliced into the request
    /// verbatim (see <see cref="GroqClient.BuildChatRequestJson"/>), we build the
    /// schema by hand here instead of trying to serialize nested objects.
    /// </summary>
    public static class ToolSchemas
    {
        /// <summary>
        /// LLM-callable function that commits a decision made during a phone call.
        /// The model should invoke this exactly once, when the player has committed
        /// to a choice (or declined / delayed). Arguments are delta values that get
        /// applied directly to <c>StatsManager.Instance.ApplyDelta</c>.
        /// </summary>
        public static readonly ToolDef ApplyOutcomeTool = new ToolDef
        {
            type = "function",
            function = new ToolFunctionDef
            {
                name = "apply_outcome",
                description =
                    "Apply the outcome of the phone call to the player's stats. Call this exactly once, " +
                    "after the player has committed to a decision (or refused / delayed). Provide signed " +
                    "deltas for money and happiness plus a one-sentence reason for the journal.",
                parametersJson =
                    "{" +
                        "\"type\":\"object\"," +
                        "\"properties\":{" +
                            "\"money_delta\":{\"type\":\"number\",\"description\":\"Change in money (positive or negative)\"}," +
                            "\"happiness_delta\":{\"type\":\"number\",\"description\":\"Change in happiness (-100..100)\"}," +
                            "\"reason\":{\"type\":\"string\",\"description\":\"Short reason for the outcome, 1 sentence\"}" +
                        "}," +
                        "\"required\":[\"money_delta\",\"happiness_delta\",\"reason\"]" +
                    "}",
            },
        };
    }
}
