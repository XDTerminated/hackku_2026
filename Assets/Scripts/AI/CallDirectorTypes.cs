using System;

namespace HackKU.AI
{
    /// <summary>
    /// Parsed payload from the LLM's 'apply_outcome' tool call. Fields are laid out to match
    /// the JSON schema exposed by <see cref="ToolSchemas.ApplyOutcomeTool"/>; JsonUtility
    /// deserializes the snake_case arguments into these camelCase fields via a bridge DTO
    /// in <see cref="CallController"/>.
    /// </summary>
    [Serializable]
    public struct CallOutcome
    {
        public float moneyDelta;
        public float happinessDelta;
        public float hygieneDelta;
        public string reason;
    }
}
