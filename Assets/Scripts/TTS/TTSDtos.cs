using System;

namespace HackKU.TTS
{
    [Serializable]
    public class VoiceSettings
    {
        public float stability = 0.5f;
        public float similarity_boost = 0.75f;
        public float style = 0.0f;
        public bool use_speaker_boost = true;

        public VoiceSettings Clone() => new()
        {
            stability = stability,
            similarity_boost = similarity_boost,
            style = style,
            use_speaker_boost = use_speaker_boost,
        };

        public string StableHash()
        {
            return $"s={stability:F3};sb={similarity_boost:F3};st={style:F3};usb={(use_speaker_boost ? 1 : 0)}";
        }
    }

    [Serializable]
    public class TTSRequestBody
    {
        public string text;
        public string model_id;
        public VoiceSettings voice_settings;
    }
}
