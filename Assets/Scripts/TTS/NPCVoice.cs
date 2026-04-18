using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HackKU.TTS
{
    // Drop-in component for NPCs. Three-step integration:
    //   1) Create an NPCVoiceProfile asset (Assets/Create/HackKU/TTS/NPC Voice Profile).
    //   2) Add this component to your NPC GameObject and assign the profile.
    //   3) Call npc.GetComponent<NPCVoice>().Speak("Hello.") from anywhere.
    [RequireComponent(typeof(AudioSource))]
    public class NPCVoice : MonoBehaviour
    {
        [SerializeField] private NPCVoiceProfile voiceProfile;

        [Tooltip("Queue new lines instead of interrupting the current one.")]
        [SerializeField] private bool queueLines = true;

        [Tooltip("Sets AudioSource.spatialBlend to 1 so playback is positional.")]
        [SerializeField] private bool spatialize = true;

        public event Action<string> OnLineStarted;
        public event Action<string> OnLineFinished;
        public event Action<string, string> OnLineFailed;

        private AudioSource _audio;
        private readonly Queue<string> _pending = new();
        private bool _busy;

        public bool IsSpeaking => _busy || _audio != null && _audio.isPlaying;
        public NPCVoiceProfile VoiceProfile { get => voiceProfile; set => voiceProfile = value; }

        private void Awake()
        {
            _audio = GetComponent<AudioSource>();
            _audio.playOnAwake = false;
            if (spatialize) _audio.spatialBlend = 1f;
        }

        public void Speak(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            if (voiceProfile == null)
            {
                Debug.LogError($"[NPCVoice] '{name}' has no voice profile assigned.", this);
                return;
            }

            if (_busy || _audio.isPlaying)
            {
                if (queueLines) _pending.Enqueue(text);
                else
                {
                    _pending.Clear();
                    _pending.Enqueue(text);
                    Stop();
                }
                return;
            }

            StartCoroutine(SpeakRoutine(text));
        }

        public void Stop()
        {
            _pending.Clear();
            if (_audio != null && _audio.isPlaying) _audio.Stop();
            _busy = false;
        }

        private IEnumerator SpeakRoutine(string text)
        {
            _busy = true;
            AudioClip clip = null;
            string error = null;
            bool done = false;

            ElevenLabsClient.Instance.Synthesize(
                text,
                voiceProfile,
                c => { clip = c; done = true; },
                e => { error = e; done = true; });

            while (!done) yield return null;

            if (clip == null)
            {
                OnLineFailed?.Invoke(text, error ?? "unknown error");
                _busy = false;
                TryDequeueNext();
                yield break;
            }

            _audio.clip = clip;
            _audio.Play();
            OnLineStarted?.Invoke(text);

            while (_audio != null && _audio.isPlaying)
                yield return null;

            OnLineFinished?.Invoke(text);
            _busy = false;
            TryDequeueNext();
        }

        private void TryDequeueNext()
        {
            if (_pending.Count == 0) return;
            var next = _pending.Dequeue();
            StartCoroutine(SpeakRoutine(next));
        }
    }
}
