using UnityEngine;

namespace HackKU.TTS
{
    // Optional: attach alongside NPCVoice to play a test line on Start.
    [RequireComponent(typeof(NPCVoice))]
    public class NPCVoiceSmokeTest : MonoBehaviour
    {
        [TextArea(2, 5)]
        public string line = "Hello from ElevenLabs.";

        public bool playOnStart = true;

        [Tooltip("If >0, repeats the line after this delay (tests the memory cache).")]
        public float repeatAfterSeconds = 0f;

        private NPCVoice _voice;

        private void Start()
        {
            _voice = GetComponent<NPCVoice>();
            if (playOnStart) _voice.Speak(line);
            if (repeatAfterSeconds > 0f) Invoke(nameof(SpeakAgain), repeatAfterSeconds);
        }

        private void SpeakAgain()
        {
            _voice.Speak(line);
        }
    }
}
