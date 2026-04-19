using System.Collections.Generic;
using HackKU.Core;
using UnityEngine;

namespace HackKU.AI
{
    // Time-based call scheduler. Fires the next incoming call a random interval
    // after the previous one ends (or after `firstCallDelay` from enable).
    public class CallDirector : MonoBehaviour
    {
        [SerializeField] CallController callController;
        [SerializeField] RotaryPhone phone;
        [SerializeField] CallScenario[] scenarios;

        [Header("Cadence (real seconds)")]
        [SerializeField] float minGapSeconds = 10f;
        [SerializeField] float maxGapSeconds = 20f;
        [SerializeField] float firstCallDelay = 7f;

        bool _characterArmed;

        [SerializeField] bool verboseLogging = true;

        float nextCallTime = -1f;
        bool wasActiveLastFrame;
        float lastGateLogTime = -10f;
        readonly Queue<int> scenarioOrder = new Queue<int>();

        void OnEnable()
        {
            // Don't schedule anything yet — wait until a character is picked and StatsManager
            // is initialized, then arm the timer with firstCallDelay seconds of grace.
            _characterArmed = false;
            nextCallTime = float.MaxValue;
            if (verboseLogging) Debug.Log("[CallDirector] Enabled — waiting for character selection.");
        }

        void Update()
        {
            if (callController == null || scenarios == null || scenarios.Length == 0) return;

            // Wait for the player to pick a character (StatsManager gets an ActiveProfile).
            if (!_characterArmed)
            {
                var sm = HackKU.Core.StatsManager.Instance;
                if (sm == null || sm.ActiveProfile == null) return;
                _characterArmed = true;
                ScheduleFromNow(firstCallDelay);
                if (verboseLogging) Debug.Log("[CallDirector] Character picked — first call in " + firstCallDelay + "s.");
            }

            bool active = callController.IsCallActive;

            // Detect state transitions.
            if (active != wasActiveLastFrame)
            {
                wasActiveLastFrame = active;
                if (active)
                {
                    // Just started.
                    nextCallTime = float.MaxValue;
                }
                else
                {
                    // Just ended — queue next call.
                    float gap = Random.Range(minGapSeconds, maxGapSeconds);
                    ScheduleFromNow(gap);
                    if (verboseLogging) Debug.Log("[CallDirector] Call ended. Next call in " + gap.ToString("F1") + "s.");
                }
                return;
            }

            if (active) return;

            // Bulletproof: if somehow nextCallTime is stuck at MaxValue but no call is active,
            // reset to a sane near-future value so we don't hang forever.
            if (nextCallTime > Time.time + maxGapSeconds + 5f)
            {
                ScheduleFromNow(Random.Range(minGapSeconds, maxGapSeconds));
                if (verboseLogging) Debug.LogWarning("[CallDirector] nextCallTime was unreachable; resetting.");
            }

            if (Time.time < nextCallTime) return;

            if (phone == null)
            {
                phone = callController != null ? callController.GetComponentInChildren<RotaryPhone>() : null;
                if (phone == null) phone = Object.FindFirstObjectByType<RotaryPhone>();
            }

            if (phone != null && !phone.CanReceiveCall)
            {
                ScheduleFromNow(2f);
                if (verboseLogging && Time.time - lastGateLogTime > 3f)
                {
                    lastGateLogTime = Time.time;
                    Debug.Log("[CallDirector] Phone not ready. IsOnCradle=" + phone.IsHandsetOnCradle +
                              " IsRinging=" + phone.IsRinging +
                              " IsHeld=" + (phone.handset != null && phone.handset.IsHeld) +
                              ". Retrying in 2s.");
                }
                return;
            }

            var scenario = PickScenario();
            if (scenario == null)
            {
                ScheduleFromNow(minGapSeconds);
                return;
            }
            if (verboseLogging) Debug.Log("[CallDirector] Starting call: " + scenario.callerName);
            callController.BeginIncomingCall(scenario);
            nextCallTime = float.MaxValue;
        }

        void ScheduleFromNow(float seconds)
        {
            nextCallTime = Time.time + seconds;
        }

        CallScenario PickScenario()
        {
            if (scenarios.Length == 0) return null;

            // Try up to scenarios.Length * 2 dequeues to find an eligible one.
            // Refills and shuffles whenever the queue is empty.
            int tries = scenarios.Length * 2;
            while (tries-- > 0)
            {
                if (scenarioOrder.Count == 0) RefillQueue();
                if (scenarioOrder.Count == 0) return null;

                int idx = scenarioOrder.Dequeue();
                var s = scenarios[idx];
                if (IsEligible(s)) return s;
                if (verboseLogging) Debug.Log("[CallDirector] Skipped ineligible scenario: " + (s != null ? s.scenarioId : "(null)"));
            }
            return null;
        }

        void RefillQueue()
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

        bool IsEligible(CallScenario s)
        {
            if (s == null) return false;
            var sm = StatsManager.Instance;
            string id = s.scenarioId ?? "";

            // (debt_collector was retired — it was a pure money-loss punish call with no trade.)

            // Gym sales: only call if happiness is below 70 (they're targeting unhappy people).
            // (Optional — feel free to loosen if too rare.)
            // if (id == "gym_upsell" && sm != null && sm.Happiness > 80f) return false;

            return true;
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
