using System;
using System.IO;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace HackKU.AI
{
    // Persistent microphone capture: opens the mic ONCE per call and keeps the buffer
    // looping. Individual turns mark start/end sample positions against the same live clip,
    // so there's no Microphone.Start() stall between turns (which was causing the
    // mid-call freeze / "loading bar" the user was seeing).
    [DisallowMultipleComponent]
    public class MicrophoneCapture : MonoBehaviour
    {
        public const int SampleRate = 16000;
        public const int MaxSeconds = 30;

        public event Action<byte[]> OnRecordingStopped;

        private AudioClip _clip;
        private string _deviceName;
        private bool _micOpen;
        private bool _turnActive;
        private int _turnStartSample;

        public bool IsMicOpen => _micOpen;
        public bool IsRecording => _turnActive;
        public float CurrentLevel { get; private set; }

        [Tooltip("If true, opens the mic device once on Start() so the first phone ring doesn't stall while Windows/Quest initializes WASAPI. Default off — the mic warms up on first Initialize call instead, so Play-mode entry isn't blocked by WASAPI init.")]
        [SerializeField] bool preWarmOnStart = false;

        private void Start()
        {
            if (preWarmOnStart) InitializeRecording();
        }

        // Scratch RMS buffer sized for a few ms of samples.
        private float[] _rmsScratch = new float[2048];
        private int _lastRmsSample;

        // Call once at the start of a call. Opens the mic and keeps it open.
        // Safe to call multiple times — no-op if already open.
        public void InitializeRecording()
        {
            if (_micOpen) return;

#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                Permission.RequestUserPermission(Permission.Microphone);
                Debug.LogWarning("[MicrophoneCapture] Requested mic permission. Retry after user grants.");
                return;
            }
#endif

            if (Microphone.devices == null || Microphone.devices.Length == 0)
            {
                Debug.LogError("[MicrophoneCapture] No microphone devices available.");
                return;
            }

            _deviceName = null;
            // loop:true so the buffer wraps continuously for the duration of the call.
            _clip = Microphone.Start(_deviceName, true, MaxSeconds, SampleRate);
            if (_clip == null)
            {
                Debug.LogError("[MicrophoneCapture] Microphone.Start returned null.");
                return;
            }
            _lastRmsSample = Microphone.GetPosition(_deviceName);
            CurrentLevel = 0f;
            _micOpen = true;
        }

        // Call when the call is over. No-op if the mic was pre-warmed at game start, to avoid
        // re-incurring the WASAPI freeze on the next call.
        public void DisposeRecording()
        {
            if (preWarmOnStart)
            {
                _turnActive = false;
                return;
            }
            ForceDispose();
        }

        private void ForceDispose()
        {
            if (!_micOpen) return;
            _turnActive = false;
            try { Microphone.End(_deviceName); } catch { }
            _micOpen = false;
            if (_clip != null)
            {
                UnityEngine.Object.Destroy(_clip);
                _clip = null;
            }
            CurrentLevel = 0f;
        }

        // Starts a "turn" — marks the current sample as the turn's start. Does NOT touch the mic.
        public void StartRecording()
        {
            if (!_micOpen) InitializeRecording();
            if (!_micOpen) return;
            _turnStartSample = Microphone.GetPosition(_deviceName);
            _turnActive = true;
        }

        // Ends the current turn, returns the WAV slice from turn-start to now.
        public byte[] StopRecordingAndGetWav()
        {
            if (!_turnActive || _clip == null)
            {
                Debug.LogWarning("[MicrophoneCapture] StopRecording called outside an active turn.");
                return Array.Empty<byte>();
            }
            int endSample = Microphone.GetPosition(_deviceName);
            _turnActive = false;

            int totalSamples = _clip.samples;
            int channels = _clip.channels;
            int recordedSamples = endSample - _turnStartSample;
            if (recordedSamples < 0) recordedSamples += totalSamples;
            if (recordedSamples <= 0)
            {
                var empty = Array.Empty<byte>();
                OnRecordingStopped?.Invoke(empty);
                return empty;
            }

            var floatBuffer = new float[recordedSamples * channels];
            _clip.GetData(floatBuffer, _turnStartSample);
            var wav = EncodeWav(floatBuffer, SampleRate, channels);
            OnRecordingStopped?.Invoke(wav);
            return wav;
        }

        private void Update()
        {
            if (!_micOpen || _clip == null) return;
            int pos = Microphone.GetPosition(_deviceName);
            int delta = pos - _lastRmsSample;
            if (delta < 0) delta += _clip.samples;
            // Stride: wait for ~32ms of samples at 16kHz (512) before recomputing RMS.
            // VAD thresholds don't need sub-frame accuracy and this halves the sum/sqrt work.
            if (delta < 512) return;
            int count = Mathf.Min(delta, _rmsScratch.Length);
            int start = pos - count;
            if (start < 0) start += _clip.samples;
            _clip.GetData(_rmsScratch, start);
            double sum = 0.0;
            for (int i = 0; i < count; i++) sum += _rmsScratch[i] * _rmsScratch[i];
            float rms = count > 0 ? (float)System.Math.Sqrt(sum / count) : 0f;
            CurrentLevel = Mathf.Lerp(CurrentLevel, rms, 0.4f);
            _lastRmsSample = pos;
        }

        private void OnDestroy()
        {
            ForceDispose();
        }

        // ---------- Inline WAV encoder (16-bit PCM, 44-byte RIFF header) ----------
        private static byte[] EncodeWav(float[] samples, int sampleRate, int channels)
        {
            int bytesPerSample = 2;
            int dataSize = samples.Length * bytesPerSample;
            int fileSize = 44 + dataSize;
            using var ms = new MemoryStream(fileSize);
            using var bw = new BinaryWriter(ms);
            bw.Write(new[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F' });
            bw.Write(fileSize - 8);
            bw.Write(new[] { (byte)'W', (byte)'A', (byte)'V', (byte)'E' });
            bw.Write(new[] { (byte)'f', (byte)'m', (byte)'t', (byte)' ' });
            bw.Write(16);
            bw.Write((short)1);
            bw.Write((short)channels);
            bw.Write(sampleRate);
            bw.Write(sampleRate * channels * bytesPerSample);
            bw.Write((short)(channels * bytesPerSample));
            bw.Write((short)(bytesPerSample * 8));
            bw.Write(new[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' });
            bw.Write(dataSize);
            for (int i = 0; i < samples.Length; i++)
            {
                float f = samples[i];
                if (f > 1f) f = 1f; else if (f < -1f) f = -1f;
                bw.Write((short)(f * short.MaxValue));
            }
            bw.Flush();
            return ms.ToArray();
        }
    }
}
