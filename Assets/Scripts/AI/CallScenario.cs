using UnityEngine;
using HackKU.TTS;

namespace HackKU.AI
{
    /// <summary>
    /// Designer-authored description of one phone call: who is calling, what they want,
    /// how the LLM should behave, and the safety caps for the conversation loop.
    /// </summary>
    [CreateAssetMenu(menuName = "HackKU/Call Scenario", fileName = "CallScenario")]
    public class CallScenario : ScriptableObject
    {
        [Tooltip("Stable string id used for logs, analytics, save data.")]
        public string scenarioId;

        [Tooltip("Display name of the caller (e.g. 'Mom').")]
        public string callerName;

        [Tooltip("Voice profile assigned to the NPCVoice component while this scenario plays.")]
        public NPCVoiceProfile voiceProfile;

        [TextArea(3, 8)]
        [Tooltip("Human-readable synopsis for designers. Also injected into the LLM prompt as situation context.")]
        public string situation;

        [TextArea(6, 20)]
        [Tooltip("Full LLM system prompt. Must instruct the model to stay in character AND to emit a structured tool call 'apply_outcome' when the conversation reaches a decision.")]
        public string systemPrompt;

        [TextArea(2, 4)]
        [Tooltip("The caller's first spoken line. Player may interrupt or reply.")]
        public string openingLine;

        [Tooltip("Hard safety cap on a call's total wall-clock duration (seconds).")]
        public float maxConversationSeconds = 60f;

        [Tooltip("Hard safety cap on the number of user<->assistant exchanges.")]
        public int maxTurns = 6;
    }
}
