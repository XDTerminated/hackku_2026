using System.Collections.Generic;
using UnityEngine;

namespace HackKU.Core
{
    // Central sound-effect player. Loads WAVs from Resources/SFX on demand and plays
    // them via a pool of AudioSources at a world position (for spatial) or 2D (for UI).
    // Auto-spawns a persistent instance on first access so callers never have to wire it.
    public class SfxHub : MonoBehaviour
    {
        public static SfxHub Instance
        {
            get
            {
                if (_inst == null)
                {
                    var go = new GameObject("[SfxHub]");
                    DontDestroyOnLoad(go);
                    _inst = go.AddComponent<SfxHub>();
                }
                return _inst;
            }
        }
        static SfxHub _inst;

        readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        readonly List<AudioSource> _pool = new List<AudioSource>();
        readonly Dictionary<string, AudioSource> _tracked = new Dictionary<string, AudioSource>();
        const int PoolSize = 12;

        AudioClip Get(string name)
        {
            if (_clips.TryGetValue(name, out var c) && c != null) return c;
            c = Resources.Load<AudioClip>("SFX/" + name);
            _clips[name] = c;
            return c;
        }

        AudioSource RentSource()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i] != null && !_pool[i].isPlaying) return _pool[i];
            }
            if (_pool.Count >= PoolSize) return _pool[0];
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            _pool.Add(src);
            return src;
        }

        public void Play(string name, float volume = 1f, float pitch = 1f)
        {
            var clip = Get(name);
            if (clip == null) return;
            var src = RentSource();
            src.spatialBlend = 0f;
            src.pitch = pitch;
            src.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        public void PlayAt(string name, Vector3 worldPos, float volume = 1f, float pitch = 1f)
        {
            var clip = Get(name);
            if (clip == null) return;
            AudioSource.PlayClipAtPoint(clip, worldPos, Mathf.Clamp01(volume));
            _ = pitch;
        }

        // Start a tracked sound (e.g. the hold-to-buy charge). Calling Stop with the
        // same key halts it. Re-calling while playing restarts at position 0.
        public void StartLoopedOrOneShot(string key, string clipName, float volume = 1f, bool loop = false, float pitch = 1f)
        {
            var clip = Get(clipName);
            if (clip == null) return;
            if (_tracked.TryGetValue(key, out var existing) && existing != null)
            {
                existing.Stop();
            }
            else
            {
                existing = gameObject.AddComponent<AudioSource>();
                existing.playOnAwake = false;
                existing.spatialBlend = 0f;
                _tracked[key] = existing;
            }
            existing.clip = clip;
            existing.loop = loop;
            existing.volume = Mathf.Clamp01(volume);
            existing.pitch = pitch;
            existing.time = 0f;
            existing.Play();
        }

        public void StopTracked(string key)
        {
            if (_tracked.TryGetValue(key, out var src) && src != null && src.isPlaying)
            {
                src.Stop();
            }
        }

        public bool IsTrackedPlaying(string key)
        {
            return _tracked.TryGetValue(key, out var src) && src != null && src.isPlaying;
        }
    }
}
