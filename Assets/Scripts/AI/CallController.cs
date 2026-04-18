using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HackKU.Core;
using HackKU.TTS;
using UnityEngine;

namespace HackKU.AI
{
    /// <summary>
    /// Orchestrates a single phone call end-to-end: ring -> answer -> opening line ->
    /// listen / transcribe / reply loop -> apply_outcome tool call -> hang up.
    /// Exactly one call may be active at a time; <see cref="BeginIncomingCall"/> is a
    /// no-op while <see cref="IsCallActive"/> is true.
    /// </summary>
    [DisallowMultipleComponent]
    public class CallController : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private RotaryPhone phone;
        [SerializeField] private NPCVoice npcVoice;
        [SerializeField] private MicrophoneCapture mic;

        [Header("Debug")]
        [SerializeField] private bool verboseLogging = false;
        [Tooltip("If NPCVoice.IsSpeaking never flips true (e.g. TTS failed), stop waiting after this many seconds.")]
        [SerializeField] private float speakWatchdogSeconds = 15f;

        /// <summary>Fired after the LLM commits a decision via apply_outcome. Gives a chance to surface the outcome in UI.</summary>
        public event Action<CallScenario, CallOutcome> OnCallResolved;
        /// <summary>Fired whenever a call ends for any reason (resolved, hung up, timed out, errored).</summary>
        public event Action<CallScenario> OnCallEnded;

        public bool IsCallActive { get; private set; }
        public CallScenario ActiveScenario => _activeScenario;

        private CallScenario _activeScenario;
        private ConversationSession _session;
        private GroqClient _groq;
        private CancellationTokenSource _cts;
        private Coroutine _loopRoutine;

        // Mic/turn coordination. Trigger-to-end-speech is exposed via OnPlayerFinishedSpeaking();
        // the flow coroutine waits on _userTurnReady before shipping audio to Whisper.
        private bool _userTurnReady;
        private bool _eventsHooked;

        private void Awake()
        {
            _groq = new GroqClient { VerboseLogging = verboseLogging };
        }

        private void OnEnable()
        {
            HookPhoneEvents();
        }

        private void OnDisable()
        {
            UnhookPhoneEvents();
            AbortActiveCall("disabled");
        }

        private void HookPhoneEvents()
        {
            if (_eventsHooked || phone == null) return;
            phone.OnAnswered += HandleAnswered;
            phone.OnHungUp += HandleHungUp;
            _eventsHooked = true;
        }

        private void UnhookPhoneEvents()
        {
            if (!_eventsHooked || phone == null) return;
            phone.OnAnswered -= HandleAnswered;
            phone.OnHungUp -= HandleHungUp;
            _eventsHooked = false;
        }

        // ---------- Public entry points ----------

        /// <summary>
        /// Start an incoming call: ring the phone and wait for the player to answer.
        /// Sets the active scenario so <see cref="HandleAnswered"/> knows what to run.
        /// </summary>
        public void BeginIncomingCall(CallScenario s)
        {
            if (IsCallActive)
            {
                Debug.LogWarning("[CallController] Another call is already active — ignoring BeginIncomingCall.");
                return;
            }
            if (s == null)
            {
                Debug.LogError("[CallController] BeginIncomingCall called with null scenario.");
                return;
            }
            if (phone == null || npcVoice == null || mic == null)
            {
                Debug.LogError("[CallController] Missing a required reference (phone / npcVoice / mic).");
                return;
            }

            HookPhoneEvents();
            _activeScenario = s;
            IsCallActive = true;

            // Mic is pre-warmed by MicrophoneCapture.Start() at game boot; this call is
            // a safety net in case pre-warm was disabled or failed (e.g. permission denied).
            if (!mic.IsMicOpen) mic.InitializeRecording();

            if (verboseLogging) Debug.Log($"[CallController] Incoming call: {s.callerName} ({s.scenarioId})");
            phone.StartRinging();
        }

        /// <summary>
        /// Hook me up to the controller's "end-of-turn" button. Signals the dialogue loop
        /// that the player has finished speaking, so it can stop the mic and transcribe.
        /// </summary>
        public void OnPlayerFinishedSpeaking()
        {
            if (!IsCallActive)
            {
                if (verboseLogging) Debug.Log("[CallController] OnPlayerFinishedSpeaking ignored — no active call.");
                return;
            }
            _userTurnReady = true;
        }

        /// <summary>Programmatic hang-up — mirror of the player putting the handset back.</summary>
        public void HangUp()
        {
            AbortActiveCall("player hang up");
        }

        // ---------- Phone events ----------

        private void HandleAnswered()
        {
            if (!IsCallActive || _activeScenario == null) return;
            if (_loopRoutine != null) return; // already running

            _cts = new CancellationTokenSource();
            _loopRoutine = StartCoroutine(RunCallFlow(_activeScenario, _cts.Token));
        }

        private void HandleHungUp()
        {
            AbortActiveCall("handset cradled");
        }

        // ---------- Main flow ----------

        private IEnumerator RunCallFlow(CallScenario scenario, CancellationToken ct)
        {
            // 1) Assign voice + build session.
            npcVoice.VoiceProfile = scenario.voiceProfile;
            _session = new ConversationSession(
                BuildSystemPrompt(scenario),
                new[] { ToolSchemas.ApplyOutcomeTool });

            // Prime the conversation with the opening line as an assistant message so the LLM
            // remembers what it already said on turn 1.
            if (!string.IsNullOrWhiteSpace(scenario.openingLine))
            {
                string opener = SanitizeForSpeech(scenario.openingLine);
                _session.AppendAssistant(new ChatMessage("assistant", opener));
                npcVoice.Speak(opener);
                yield return WaitForNpcToFinishSpeaking();
            }

            float callStartTime = Time.unscaledTime;
            int turn = 0;

            while (!ct.IsCancellationRequested && IsCallActive)
            {
                // Safety caps.
                if (turn >= Mathf.Max(1, scenario.maxTurns))
                {
                    if (verboseLogging) Debug.Log("[CallController] maxTurns reached, ending call.");
                    break;
                }
                if (Time.unscaledTime - callStartTime >= scenario.maxConversationSeconds)
                {
                    if (verboseLogging) Debug.Log("[CallController] maxConversationSeconds reached, ending call.");
                    break;
                }

                // 2) Listen phase — start mic, wait for OnPlayerFinishedSpeaking().
                _userTurnReady = false;
                mic.StartRecording();

                while (!ct.IsCancellationRequested && IsCallActive && !_userTurnReady)
                {
                    // Also honor wall-clock cap inside the wait.
                    if (Time.unscaledTime - callStartTime >= scenario.maxConversationSeconds) break;
                    yield return null;
                }

                if (ct.IsCancellationRequested || !IsCallActive) break;
                if (!_userTurnReady) break; // timed out

                byte[] wav = mic.StopRecordingAndGetWav();
                if (wav == null || wav.Length == 0)
                {
                    if (verboseLogging) Debug.LogWarning("[CallController] Empty recording — skipping turn.");
                    turn++;
                    continue;
                }

                // 3) Transcribe.
                var sttTask = SafeTranscribe(wav, ct);
                while (!sttTask.IsCompleted) yield return null;
                if (ct.IsCancellationRequested || !IsCallActive) break;

                string userText = sttTask.Result;
                if (string.IsNullOrWhiteSpace(userText))
                {
                    if (verboseLogging) Debug.Log("[CallController] STT returned empty text — skipping turn.");
                    turn++;
                    continue;
                }

                if (verboseLogging) Debug.Log($"[CallController] Player said: {userText}");
                _session.AppendUser(userText);

                // 4) Ask the LLM.
                var chatTask = SafeChat(ct);
                while (!chatTask.IsCompleted) yield return null;
                if (ct.IsCancellationRequested || !IsCallActive) break;

                ChatResponse response = chatTask.Result;
                ChatMessage assistantMsg = response?.choices != null && response.choices.Count > 0
                    ? response.choices[0].message
                    : null;

                if (assistantMsg == null)
                {
                    Debug.LogWarning("[CallController] Groq returned no message — ending call.");
                    break;
                }

                _session.AppendAssistant(assistantMsg);

                // 5) Did the model commit the outcome?
                if (TryExtractOutcome(assistantMsg, out CallOutcome outcome, out string toolCallId))
                {
                    ApplyOutcome(outcome);

                    // Let the model know the tool ran (keeps the history valid if we ever continue).
                    _session.AppendToolResult(toolCallId, "{\"ok\":true}");

                    // Speak any goodbye text that came alongside the tool call.
                    string goodbye = assistantMsg.content;

                    // If Groq returned ONLY a tool call with no text, ask for one short sign-off line.
                    if (string.IsNullOrWhiteSpace(goodbye))
                    {
                        _session.AppendUser("(say one short in-character goodbye now, then end the call)");
                        var goodbyeTask = SafeChat(ct);
                        while (!goodbyeTask.IsCompleted) yield return null;
                        if (!ct.IsCancellationRequested && IsCallActive)
                        {
                            var resp = goodbyeTask.Result;
                            if (resp?.choices != null && resp.choices.Count > 0 && resp.choices[0].message != null)
                            {
                                goodbye = resp.choices[0].message.content;
                            }
                        }
                    }

                    goodbye = SanitizeForSpeech(goodbye);
                    if (!ct.IsCancellationRequested && IsCallActive && !string.IsNullOrWhiteSpace(goodbye))
                    {
                        npcVoice.Speak(goodbye);
                        yield return WaitForNpcToFinishSpeaking();
                    }

                    // Do NOT auto-dock. The player has to physically place the handset back on
                    // the cradle before any new call can come in — this is what tells the director
                    // "I'm ready for another call".
                    OnCallResolved?.Invoke(scenario, outcome);
                    break;
                }

                // 6) Otherwise, speak the assistant text and loop back.
                string reply = SanitizeForSpeech(assistantMsg.content);
                if (!string.IsNullOrWhiteSpace(reply))
                {
                    npcVoice.Speak(reply);
                    yield return WaitForNpcToFinishSpeaking();
                }

                turn++;
            }

            FinishCall();
        }

        // ---------- Helpers ----------

        // Safety net: strips stage-direction style noise from LLM output so the TTS voice
        // doesn't read "*sighs*", "[crying]", "(pause)", etc. aloud. The system prompt already
        // forbids these, but models occasionally slip.
        private static readonly System.Text.RegularExpressions.Regex _stageDirectionRx =
            new System.Text.RegularExpressions.Regex(
                @"\*[^*]+\*|\[[^\]]+\]|\(\s*(?:pauses?|sighs?|chuckles?|laughs?|giggles?|whispers?|groans?|sobs?|crying|nervous|quietly|softly|softly cries|clears throat|beat|pause|long pause)[^)]*\)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        static string SanitizeForSpeech(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            string cleaned = _stageDirectionRx.Replace(s, string.Empty);
            // Collapse any doubled whitespace / dangling punctuation left over.
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s{2,}", " ");
            cleaned = cleaned.Replace(" ,", ",").Replace(" .", ".").Replace(" ?", "?").Replace(" !", "!");
            return cleaned.Trim();
        }

        private string BuildSystemPrompt(CallScenario scenario)
        {
            // Inject the designer-facing situation into the prompt so the LLM has a one-liner
            // summary even if the full systemPrompt is sparse.
            if (string.IsNullOrWhiteSpace(scenario.situation))
                return scenario.systemPrompt ?? string.Empty;

            return (scenario.systemPrompt ?? string.Empty).TrimEnd()
                + "\n\n[Situation]\n" + scenario.situation.Trim()
                + "\n\n[Caller]\n" + (string.IsNullOrEmpty(scenario.callerName) ? "Unknown" : scenario.callerName);
        }

        private IEnumerator WaitForNpcToFinishSpeaking()
        {
            // Give TTS a moment to kick in; IsSpeaking may be false for a frame while the clip fetches.
            float deadline = Time.unscaledTime + speakWatchdogSeconds;
            float startedAt = Time.unscaledTime;
            while (!npcVoice.IsSpeaking && Time.unscaledTime - startedAt < 2f)
            {
                yield return null;
            }

            // While the NPC is talking, listen for the player interrupting.
            // If the mic picks up sustained speech (~150 ms above threshold), cut the NPC off.
            const float interruptLevelThreshold = 0.03f;
            const float interruptSustainSeconds = 0.15f;
            float speechStreak = 0f;

            while (npcVoice.IsSpeaking && Time.unscaledTime < deadline)
            {
                if (mic != null && mic.IsMicOpen && mic.CurrentLevel > interruptLevelThreshold)
                {
                    speechStreak += Time.unscaledDeltaTime;
                    if (speechStreak >= interruptSustainSeconds)
                    {
                        if (verboseLogging) Debug.Log("[CallController] Player interrupted NPC.");
                        try { npcVoice.Stop(); } catch { /* ignore */ }
                        break;
                    }
                }
                else
                {
                    // Decay streak so brief noise doesn't accumulate falsely.
                    speechStreak = Mathf.Max(0f, speechStreak - Time.unscaledDeltaTime * 0.5f);
                }
                yield return null;
            }
        }

        private async Task<string> SafeTranscribe(byte[] wav, CancellationToken ct)
        {
            try { return await _groq.TranscribeAsync(wav, ct); }
            catch (OperationCanceledException) { return string.Empty; }
            catch (Exception ex)
            {
                Debug.LogError("[CallController] STT failed: " + ex.Message);
                return string.Empty;
            }
        }

        private async Task<ChatResponse> SafeChat(CancellationToken ct)
        {
            try
            {
                var msgs = _session.BuildRequestMessages();
                return await _groq.SendChatAsync(msgs, new List<ToolDef>(_session.Tools), ct);
            }
            catch (OperationCanceledException) { return null; }
            catch (Exception ex)
            {
                Debug.LogError("[CallController] Chat failed: " + ex.Message);
                return null;
            }
        }

        /// <summary>JsonUtility bridge matching the snake_case arg payload from apply_outcome.</summary>
        [Serializable]
        private class ApplyOutcomeArgs
        {
            public float money_delta;
            public float happiness_delta;
            public string reason;
        }

        private static bool TryExtractOutcome(ChatMessage assistantMsg, out CallOutcome outcome, out string toolCallId)
        {
            outcome = default;
            toolCallId = null;

            if (assistantMsg?.tool_calls == null) return false;

            for (int i = 0; i < assistantMsg.tool_calls.Count; i++)
            {
                var tc = assistantMsg.tool_calls[i];
                if (tc?.function == null) continue;
                if (tc.function.name != "apply_outcome") continue;
                if (string.IsNullOrEmpty(tc.function.arguments)) continue;

                ApplyOutcomeArgs parsed;
                try { parsed = JsonUtility.FromJson<ApplyOutcomeArgs>(tc.function.arguments); }
                catch (Exception ex)
                {
                    Debug.LogError("[CallController] Failed to parse apply_outcome args: " + ex.Message + "\n" + tc.function.arguments);
                    return false;
                }
                if (parsed == null) return false;

                outcome = new CallOutcome
                {
                    moneyDelta = parsed.money_delta,
                    happinessDelta = parsed.happiness_delta,
                    reason = parsed.reason ?? string.Empty,
                };
                toolCallId = tc.id ?? string.Empty;
                return true;
            }
            return false;
        }

        private void ApplyOutcome(CallOutcome outcome)
        {
            var stats = StatsManager.Instance;
            if (stats == null)
            {
                Debug.LogWarning("[CallController] No StatsManager.Instance — outcome not applied.");
                return;
            }
            stats.ApplyDelta(outcome.moneyDelta, outcome.happinessDelta, outcome.reason);
            if (verboseLogging)
                Debug.Log($"[CallController] Applied outcome: money {outcome.moneyDelta:+0.##;-0.##}, happiness {outcome.happinessDelta:+0.##;-0.##} — {outcome.reason}");
        }

        private void AbortActiveCall(string reasonLog)
        {
            if (!IsCallActive && _loopRoutine == null && _cts == null) return;

            if (verboseLogging) Debug.Log("[CallController] Aborting call: " + reasonLog);

            try { _cts?.Cancel(); } catch { /* ignore */ }

            if (_loopRoutine != null)
            {
                StopCoroutine(_loopRoutine);
                _loopRoutine = null;
            }

            if (mic != null && mic.IsRecording)
            {
                try { mic.StopRecordingAndGetWav(); } catch { /* ignore */ }
            }
            if (mic != null)
            {
                try { mic.DisposeRecording(); } catch { /* ignore */ }
            }
            if (npcVoice != null && npcVoice.IsSpeaking)
            {
                try { npcVoice.Stop(); } catch { /* ignore */ }
            }
            if (phone != null && phone.IsRinging)
            {
                try { phone.StopRinging(); } catch { /* ignore */ }
            }

            FinishCall();
        }

        private void FinishCall()
        {
            CallScenario finished = _activeScenario;

            try { _cts?.Dispose(); } catch { /* ignore */ }
            _cts = null;
            _loopRoutine = null;
            _session = null;
            _userTurnReady = false;
            _activeScenario = null;
            IsCallActive = false;

            if (mic != null)
            {
                try { mic.DisposeRecording(); } catch { /* ignore */ }
            }

            if (finished != null) OnCallEnded?.Invoke(finished);
        }
    }
}
