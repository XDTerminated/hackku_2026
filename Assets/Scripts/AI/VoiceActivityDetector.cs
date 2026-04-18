using System;
using UnityEngine;

namespace HackKU.AI
{
    // Watches MicrophoneCapture.CurrentLevel while a call is active.
    // Fires OnSpeechEnded after the user has spoken and then gone silent for `silenceToEndSeconds`.
    // Lets players talk naturally — no push-to-talk.
    public class VoiceActivityDetector : MonoBehaviour
    {
        [SerializeField] MicrophoneCapture mic;
        [SerializeField] CallController callController;

        [Tooltip("Mic RMS above this counts as speech.")]
        [Range(0f, 0.2f)]
        [SerializeField] float speakingThreshold = 0.02f;

        [Tooltip("Mic RMS below this counts as silence.")]
        [Range(0f, 0.2f)]
        [SerializeField] float silenceThreshold = 0.012f;

        [Tooltip("Continuous silence (in seconds, after speech) before we call the turn done.")]
        [SerializeField] float silenceToEndSeconds = 1.3f;

        [Tooltip("Minimum speech duration before an end-of-turn signal is allowed.")]
        [SerializeField] float minSpeechSeconds = 0.4f;

        [Tooltip("Hard cap on a single listen window. Ends the turn even if the user never spoke.")]
        [SerializeField] float maxListenSeconds = 15f;

        public event Action OnSpeechEnded;

        bool listening;
        bool hasSpoken;
        float speechStartTime;
        float lastAboveThresholdTime;
        float listenStartTime;

        void Update()
        {
            if (mic == null || callController == null) return;

            bool shouldListen = callController.IsCallActive && mic.IsRecording;
            if (shouldListen && !listening) BeginListen();
            else if (!shouldListen && listening) EndListen();

            if (!listening) return;

            float now = Time.unscaledTime;
            float level = mic.CurrentLevel;

            if (level > speakingThreshold)
            {
                if (!hasSpoken)
                {
                    hasSpoken = true;
                    speechStartTime = now;
                }
                lastAboveThresholdTime = now;
            }

            bool hardTimeout = now - listenStartTime > maxListenSeconds;

            if (hasSpoken
                && now - speechStartTime > minSpeechSeconds
                && level < silenceThreshold
                && now - lastAboveThresholdTime > silenceToEndSeconds)
            {
                FireEndOfTurn("silence after speech");
                return;
            }

            if (hardTimeout)
            {
                FireEndOfTurn(hasSpoken ? "max listen timeout" : "no speech timeout");
            }
        }

        void BeginListen()
        {
            listening = true;
            hasSpoken = false;
            listenStartTime = Time.unscaledTime;
            lastAboveThresholdTime = listenStartTime;
        }

        void EndListen()
        {
            listening = false;
            hasSpoken = false;
        }

        void FireEndOfTurn(string reason)
        {
            EndListen();
            OnSpeechEnded?.Invoke();
            callController?.OnPlayerFinishedSpeaking();
        }
    }
}
