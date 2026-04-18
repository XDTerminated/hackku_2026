using System;
using UnityEngine;

namespace HackKU.Core
{
    public class TimeManager : MonoBehaviour
    {
        public const float SECONDS_PER_YEAR = 45f;

        public static event Action<int> OnYearTick;

        public static TimeManager Instance { get; private set; }

        [SerializeField] private float elapsedSeconds;
        [SerializeField] private int currentYear;
        [SerializeField] private bool isRunning = true;

        public int CurrentYear => currentYear;

        public float YearProgress01
        {
            get
            {
                float into = elapsedSeconds - (currentYear * SECONDS_PER_YEAR);
                return Mathf.Clamp01(into / SECONDS_PER_YEAR);
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (!isRunning)
            {
                return;
            }

            elapsedSeconds += Time.deltaTime;

            int targetYear = Mathf.FloorToInt(elapsedSeconds / SECONDS_PER_YEAR);
            while (currentYear < targetYear)
            {
                currentYear++;
                OnYearTick?.Invoke(currentYear);
            }
        }

        public void Pause()
        {
            isRunning = false;
        }

        public void Resume()
        {
            isRunning = true;
        }

        public void ResetTime()
        {
            elapsedSeconds = 0f;
            currentYear = 0;
        }
    }
}
