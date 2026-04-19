using UnityEngine;

namespace HackKU.Core
{
    // Plays a quiet ambient pad loop in the background. Auto-spawns a single persistent
    // instance at scene load and keeps playing across scene reloads.
    public class AmbientMusicPlayer : MonoBehaviour
    {
        [Tooltip("Resources/SFX clip name for the ambient loop.")]
        public string clipName = "ambient_pad";

        [Tooltip("Target volume (0-1). Kept quiet so it fills silence without intruding.")]
        [Range(0f, 1f)] public float volume = 0.12f;

        [Tooltip("Seconds to fade in from silence to target volume on start.")]
        public float fadeInSeconds = 2.5f;

        AudioSource _src;
        float _t;

        void Awake()
        {
            var clip = Resources.Load<AudioClip>("SFX/" + clipName);
            if (clip == null) { enabled = false; return; }
            _src = gameObject.AddComponent<AudioSource>();
            _src.clip = clip;
            _src.loop = true;
            _src.spatialBlend = 0f;
            _src.volume = 0f;
            _src.playOnAwake = false;
            _src.priority = 64;
            _src.Play();
        }

        void Update()
        {
            if (_src == null) return;
            if (_t < fadeInSeconds)
            {
                _t += Time.unscaledDeltaTime;
                _src.volume = Mathf.Lerp(0f, volume, Mathf.Clamp01(_t / Mathf.Max(0.01f, fadeInSeconds)));
            }
            else if (!Mathf.Approximately(_src.volume, volume))
            {
                _src.volume = volume;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoSpawn()
        {
            if (FindFirstObjectByType<AmbientMusicPlayer>() != null) return;
            var go = new GameObject("[AmbientMusic]");
            DontDestroyOnLoad(go);
            go.AddComponent<AmbientMusicPlayer>();
        }
    }
}
