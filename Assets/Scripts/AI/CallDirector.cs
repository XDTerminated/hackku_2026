using System.Collections.Generic;
using HackKU.Core;
using UnityEngine;

namespace HackKU.AI
{
    // Time-based call scheduler. Fires the next incoming call a random interval
    // after the previous one ends (or after `firstCallDelay` from enable).
    // Default cadence aims at ~1-3 calls per in-game year (45s = 1 year).
    public class CallDirector : MonoBehaviour
    {
        [SerializeField] CallController callController;
        [SerializeField] RotaryPhone phone;
        [SerializeField] CallScenario[] scenarios;

        [Header("Cadence (real seconds)")]
        [Tooltip("Shortest gap between calls (after the previous one ends).")]
        [SerializeField] float minGapSeconds = 10f;

        [Tooltip("Longest gap between calls.")]
        [SerializeField] float maxGapSeconds = 25f;

        [Tooltip("Delay before the very first call after Enable.")]
        [SerializeField] float firstCallDelay = 8f;

        float nextCallTime = -1f;
        bool wasActiveLastFrame;
        readonly Queue<int> scenarioOrder = new();

        void OnEnable()
        {
            ScheduleFromNow(firstCallDelay);
        }

        void Update()
        {
            if (callController == null || scenarios == null || scenarios.Length == 0) return;

            if (callController.IsCallActive)
            {
                wasActiveLastFrame = true;
                return;
            }

            if (wasActiveLastFrame)
            {
                // Call just ended — queue the next one.
                wasActiveLastFrame = false;
                ScheduleFromNow(Random.Range(minGapSeconds, maxGapSeconds));
                return;
            }

            if (Time.time < nextCallTime) return;

            // Gate: only ring when the handset is actually on the cradle and ready to be answered.
            // If the player dropped it on the floor, the phone can't ring until it's put back.
            if (phone == null)
            {
                phone = callController != null ? callController.GetComponentInChildren<RotaryPhone>() : null;
                if (phone == null) phone = Object.FindFirstObjectByType<RotaryPhone>();
            }
            if (phone != null && !phone.CanReceiveCall)
            {
                ScheduleFromNow(2f); // check again soon
                return;
            }

            var scenario = PickScenario();
            if (scenario == null)
            {
                ScheduleFromNow(minGapSeconds);
                return;
            }
            callController.BeginIncomingCall(scenario);
            // nextCallTime stays stale until this call ends; the `wasActiveLastFrame` branch handles rescheduling.
            nextCallTime = float.MaxValue;
        }

        void ScheduleFromNow(float seconds)
        {
            nextCallTime = Time.time + seconds;
        }

        CallScenario PickScenario()
        {
            if (scenarios.Length == 0) return null;
            if (scenarioOrder.Count == 0)
            {
                var indices = new List<int>();
                for (int i = 0; i < scenarios.Length; i++) if (scenarios[i] != null) indices.Add(i);
                for (int i = indices.Count - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);
                    (indices[i], indices[j]) = (indices[j], indices[i]);
                }
                foreach (var idx in indices) scenarioOrder.Enqueue(idx);
            }
            return scenarioOrder.Count > 0 ? scenarios[scenarioOrder.Dequeue()] : null;
        }

        public void ForceCall(int scenarioIndex)
        {
            if (scenarios == null || scenarioIndex < 0 || scenarioIndex >= scenarios.Length) return;
            if (callController == null) return;
            if (callController.IsCallActive) return;
            callController.BeginIncomingCall(scenarios[scenarioIndex]);
        }
    }
}
