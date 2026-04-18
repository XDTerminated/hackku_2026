using UnityEngine;

namespace HackKU.TTS
{
    public static class PcmAudioClipBuilder
    {
        public static AudioClip FromPcm16(byte[] pcm, int sampleRate, int channels, string name)
        {
            if (pcm == null || pcm.Length < 2)
            {
                Debug.LogError("[PcmAudioClipBuilder] PCM buffer is empty.");
                return null;
            }

            int sampleCount = pcm.Length / 2;
            var samples = new float[sampleCount];
            const float scale = 1f / 32768f;
            for (int i = 0; i < sampleCount; i++)
            {
                short s = (short)(pcm[i * 2] | (pcm[i * 2 + 1] << 8));
                samples[i] = s * scale;
            }

            var clip = AudioClip.Create(name, sampleCount / channels, channels, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
