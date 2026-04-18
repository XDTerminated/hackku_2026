using HackKU.Core;
using UnityEngine;
using UnityEngine.Events;

namespace HackKU.Game
{
    /// <summary>
    /// Glues character selection to the rest of the sim. When
    /// <see cref="CharacterSelector.OnCharacterChosen"/> fires we seed the
    /// <see cref="StatsManager"/>, (optionally) reset + resume the
    /// <see cref="TimeManager"/>, and broadcast <see cref="OnGameStarted"/> so
    /// scene-specific systems (HUD, ambient audio, voice prompts) can hook in.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Time")]
        [Tooltip("Zero the simulation clock when a character is chosen.")]
        [SerializeField] private bool resetTimeOnStart = true;
        [Tooltip("Un-pause the simulation clock when a character is chosen.")]
        [SerializeField] private bool resumeTimeOnStart = true;
        [Tooltip("Pause the clock as soon as this bootstrap spins up (e.g. while menu is live).")]
        [SerializeField] private bool pauseTimeOnAwake = true;

        [Header("Events")]
        [Tooltip("Fired after the chosen profile has been applied to StatsManager.")]
        public UnityEvent<CharacterProfile> OnGameStarted;

        private CharacterProfile activeProfile;
        public CharacterProfile ActiveProfile => activeProfile;

        private void Awake()
        {
            if (pauseTimeOnAwake && TimeManager.Instance != null)
            {
                TimeManager.Instance.Pause();
            }
        }

        private void OnEnable()
        {
            CharacterSelector.OnCharacterChosen += HandleCharacterChosen;
        }

        private void OnDisable()
        {
            CharacterSelector.OnCharacterChosen -= HandleCharacterChosen;
        }

        private void Start()
        {
            // If the TimeManager wasn't alive yet in Awake, try again here.
            if (pauseTimeOnAwake && TimeManager.Instance != null)
            {
                TimeManager.Instance.Pause();
            }
        }

        private void HandleCharacterChosen(CharacterProfile profile)
        {
            if (profile == null)
            {
                Debug.LogWarning("[GameBootstrap] Null profile chosen; ignoring.", this);
                return;
            }

            activeProfile = profile;

            if (StatsManager.Instance != null)
            {
                StatsManager.Instance.Initialize(profile);
            }
            else
            {
                Debug.LogError("[GameBootstrap] StatsManager.Instance missing; cannot seed stats.", this);
            }

            if (TimeManager.Instance != null)
            {
                if (resetTimeOnStart)
                {
                    TimeManager.Instance.ResetTime();
                }
                if (resumeTimeOnStart)
                {
                    TimeManager.Instance.Resume();
                }
            }

            OnGameStarted?.Invoke(profile);
        }
    }
}
