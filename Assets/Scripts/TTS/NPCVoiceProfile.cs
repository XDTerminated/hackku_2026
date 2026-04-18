using UnityEngine;

namespace HackKU.TTS
{
    [CreateAssetMenu(menuName = "HackKU/TTS/NPC Voice Profile", fileName = "NPCVoiceProfile")]
    public class NPCVoiceProfile : ScriptableObject
    {
        [Tooltip("ElevenLabs voice ID (e.g. '21m00Tcm4TlvDq8ikWAM' for Rachel).")]
        public string voiceId;

        [Tooltip("Label shown in the Inspector — no effect on the request.")]
        public string displayName;

        [Tooltip("Optional per-NPC model override. Leave empty to use ElevenLabsConfig.defaultModelId.")]
        public string modelIdOverride;

        public VoiceSettings voiceSettings = new();

        public string ResolveModelId(ElevenLabsConfig config)
        {
            return string.IsNullOrEmpty(modelIdOverride) ? config.defaultModelId : modelIdOverride;
        }
    }
}
