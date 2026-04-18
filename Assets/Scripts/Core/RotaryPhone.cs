using System;
using UnityEngine;

namespace HackKU.Core
{
    public class RotaryPhone : MonoBehaviour
    {
        public AudioClip ringClip;
        public Light glowLight;
        public HandsetController handset;

        public bool IsRinging { get; private set; }
        public bool IsHandsetOnCradle => handset != null && handset.IsOnCradle;

        public event Action OnAnswered;
        public event Action OnHungUp;
        public event Action OnDialOutRequested;
        public event Action OnHandsetDropped;

        // True iff the handset is in a state where we can receive a new incoming call.
        // (On cradle, not ringing, not being held right now.)
        public bool CanReceiveCall => handset != null && handset.IsOnCradle && !IsRinging && !handset.IsHeld;

        AudioSource ringSource;
        float glowPhase;

        void Awake()
        {
            if (handset == null) handset = GetComponentInChildren<HandsetController>();
            if (handset != null)
            {
                handset.Phone = this;
            }
            ringSource = gameObject.AddComponent<AudioSource>();
            ringSource.playOnAwake = false;
            ringSource.loop = true;
            ringSource.spatialBlend = 1f;
            ringSource.volume = 0.6f;
        }

        void Update()
        {
            if (IsRinging && glowLight != null)
            {
                glowPhase += Time.deltaTime * 6f;
                glowLight.intensity = 2f + Mathf.Sin(glowPhase) * 1.5f;
            }
        }

        public void StartRinging()
        {
            if (IsRinging) return;
            IsRinging = true;
            if (ringClip != null)
            {
                ringSource.clip = ringClip;
                ringSource.Play();
            }
            if (glowLight != null) glowLight.enabled = true;
        }

        public void StopRinging()
        {
            IsRinging = false;
            if (ringSource.isPlaying) ringSource.Stop();
            if (glowLight != null) glowLight.enabled = false;
        }

        public void NotifyHandsetLifted()
        {
            if (IsRinging)
            {
                StopRinging();
                OnAnswered?.Invoke();
            }
            else
            {
                OnDialOutRequested?.Invoke();
            }
        }

        public void NotifyHandsetPlacedBack()
        {
            OnHungUp?.Invoke();
        }

        // Called by HandsetController when the player drops the handset somewhere that isn't the cradle.
        public void NotifyHandsetDropped()
        {
            if (IsRinging) StopRinging();
            OnHandsetDropped?.Invoke();
            // Treat a drop as a hangup too so the active call cleans up.
            OnHungUp?.Invoke();
        }
    }
}
