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
    // When the player lifts the handset while no call is active (dial out), this controller
    // runs a short voice flow: the player speaks "I want groceries" / "pizza" / etc.,
    // Groq Whisper transcribes, a keyword-match picks a FoodItem, the shopkeeper voice
    // says a short unique ack line via ElevenLabs, money is deducted, and the item spawns
    // at the DeliverySpawn point after a short delay.
    public class FoodOrderController : MonoBehaviour
    {
        [SerializeField] HackKU.Core.RotaryPhone phone;
        [SerializeField] NPCVoice npcVoice;
        [SerializeField] MicrophoneCapture mic;
        [SerializeField] Transform deliverySpawn;

        [Header("Menu")]
        [SerializeField] FoodItem[] menu;

        [Header("Voices (pick based on item)")]
        [SerializeField] NPCVoiceProfile groceryVoice;
        [SerializeField] NPCVoiceProfile pizzaVoice;
        [SerializeField] NPCVoiceProfile fancyVoice;
        [SerializeField] NPCVoiceProfile fastFoodVoice;

        [Header("Timing")]
        [SerializeField] float dialToneSeconds = 1.0f;
        [SerializeField] float deliveryDelaySeconds = 4f;

        public bool IsOrderActive { get; private set; }

        GroqClient _groq;
        CancellationTokenSource _cts;
        Coroutine _flow;

        void Awake()
        {
            _groq = new GroqClient();
        }

        void OnEnable()
        {
            if (phone != null)
            {
                phone.OnDialOutRequested += HandleDialOut;
                phone.OnHungUp += HandleHungUp;
            }
        }

        void OnDisable()
        {
            if (phone != null)
            {
                phone.OnDialOutRequested -= HandleDialOut;
                phone.OnHungUp -= HandleHungUp;
            }
            EndOrder();
        }

        void HandleDialOut()
        {
            if (IsOrderActive) return;
            var cc = UnityEngine.Object.FindFirstObjectByType<CallController>();
            if (cc != null && cc.IsCallActive) return;
            if (npcVoice == null || mic == null) { Debug.LogError("[FoodOrderController] missing refs"); return; }

            IsOrderActive = true;
            _cts = new CancellationTokenSource();
            _flow = StartCoroutine(Run(_cts.Token));
        }

        void HandleHungUp()
        {
            if (!IsOrderActive) return;
            EndOrder();
        }

        void EndOrder()
        {
            IsOrderActive = false;
            try { if (_cts != null) _cts.Cancel(); } catch { }
            _cts = null;
            if (_flow != null) { StopCoroutine(_flow); _flow = null; }
            if (npcVoice != null && npcVoice.IsSpeaking) try { npcVoice.Stop(); } catch { }
            if (mic != null && mic.IsRecording) try { mic.StopRecordingAndGetWav(); } catch { }
        }

        IEnumerator Run(CancellationToken ct)
        {
            if (!mic.IsMicOpen) mic.InitializeRecording();
            yield return new WaitForSeconds(dialToneSeconds);
            if (ct.IsCancellationRequested) yield break;

            mic.StartRecording();
            float listenStart = Time.unscaledTime;
            const float maxListen = 8f;
            const float minSpeech = 0.5f;
            const float silenceToEnd = 1.2f;
            const float speakingRms = 0.02f;
            const float silenceRms = 0.012f;
            bool spoke = false;
            float spokeAt = 0f;
            float lastAbove = Time.unscaledTime;

            while (!ct.IsCancellationRequested && IsOrderActive)
            {
                float now = Time.unscaledTime;
                float level = mic.CurrentLevel;
                if (level > speakingRms)
                {
                    if (!spoke) { spoke = true; spokeAt = now; }
                    lastAbove = now;
                }
                if (spoke && now - spokeAt > minSpeech && level < silenceRms && now - lastAbove > silenceToEnd) break;
                if (now - listenStart > maxListen) break;
                yield return null;
            }

            byte[] wav = mic.StopRecordingAndGetWav();
            if (wav == null || wav.Length == 0)
            {
                SpeakLine("Sorry, I didn't catch you there. Give us a call back.", groceryVoice);
                yield return new WaitForSeconds(0.4f);
                while (npcVoice.IsSpeaking && !ct.IsCancellationRequested) yield return null;
                EndOrder();
                yield break;
            }

            var sttTask = SafeTranscribe(wav, ct);
            while (!sttTask.IsCompleted) yield return null;
            if (ct.IsCancellationRequested) { EndOrder(); yield break; }

            string text = (sttTask.Result ?? string.Empty).Trim();
            FoodItem picked = MatchFood(text);
            NPCVoiceProfile voice = PickVoice(picked);

            if (picked == null)
            {
                SpeakLine("Sorry, I didn't catch that. Try: groceries, pizza, dinner, or fast food.", groceryVoice);
                yield return new WaitForSeconds(0.4f);
                while (npcVoice.IsSpeaking && !ct.IsCancellationRequested) yield return null;
                EndOrder();
                yield break;
            }

            if (StatsManager.Instance != null && StatsManager.Instance.Money < picked.price)
            {
                SpeakLine("Sorry friend, card got declined. Call us back when you've got the funds.", voice);
                yield return new WaitForSeconds(0.4f);
                while (npcVoice.IsSpeaking && !ct.IsCancellationRequested) yield return null;
                EndOrder();
                yield break;
            }

            string ack = null;
            var ackTask = SafeOneShot(BuildAckPrompt(picked), ct);
            while (!ackTask.IsCompleted) yield return null;
            if (!ct.IsCancellationRequested) ack = ackTask.Result;
            if (string.IsNullOrWhiteSpace(ack)) ack = "Got it, " + picked.displayName + " on the way.";

            SpeakLine(Sanitize(ack), voice);

            if (StatsManager.Instance != null)
            {
                StatsManager.Instance.ApplyDelta(-picked.price, 0f, "Ordered " + picked.displayName);
                ToastHUD.Show("-$" + Mathf.Round(picked.price), "Ordered " + picked.displayName, ToastKind.Bill);
            }

            yield return new WaitForSeconds(0.4f);
            while (npcVoice.IsSpeaking && !ct.IsCancellationRequested) yield return null;

            StartCoroutine(SpawnDeliveryAfter(picked, deliveryDelaySeconds));
            EndOrder();
        }

        void SpeakLine(string line, NPCVoiceProfile voice)
        {
            if (voice != null) npcVoice.VoiceProfile = voice;
            npcVoice.Speak(Sanitize(line));
        }

        string BuildAckPrompt(FoodItem item)
        {
            string persona =
                item.itemId == "pizza" ? "a Brooklyn Italian pizza-shop owner named Tony" :
                item.itemId == "groceries" ? "a warm British grocery clerk named Priya" :
                item.itemId == "fancy" ? "a fine-dining French maitre d named Laurent" :
                item.itemId == "fast_food" ? "an energetic American fast-food cashier named Mike" :
                "a friendly shopkeeper";
            return "Respond AS " + persona + ". The player just ordered '" + item.displayName +
                "' for $" + Mathf.Round(item.price) + ". Say a single natural short phone-call line (1-2 short sentences) " +
                "confirming the order and giving a delivery time. Do NOT use asterisks, stage directions, emojis, or quotes. " +
                "Plain spoken English only. End with a brief goodbye.";
        }

        FoodItem MatchFood(string userText)
        {
            if (menu == null || menu.Length == 0) return null;
            string t = userText.ToLowerInvariant();
            var patterns = new (string[] keys, string itemId)[]
            {
                (new[] { "grocery", "groceries", "produce", "vegetables", "supermarket" }, "groceries"),
                (new[] { "pizza", "slice", "pie" }, "pizza"),
                (new[] { "fancy", "dinner", "steak", "gourmet", "wine", "reservation" }, "fancy"),
                (new[] { "fast food", "burger", "burgers", "fries", "drive through", "drive-through" }, "fast_food"),
            };
            foreach (var p in patterns)
            {
                foreach (var k in p.keys)
                {
                    if (t.Contains(k))
                    {
                        for (int i = 0; i < menu.Length; i++)
                            if (menu[i] != null && menu[i].itemId == p.itemId) return menu[i];
                    }
                }
            }
            if (t.Contains("food") || t.Contains("hungry") || t.Contains("eat") || t.Contains("order"))
            {
                FoodItem best = null;
                for (int i = 0; i < menu.Length; i++)
                    if (menu[i] != null && (best == null || menu[i].price < best.price)) best = menu[i];
                return best;
            }
            return null;
        }

        NPCVoiceProfile PickVoice(FoodItem item)
        {
            if (item == null) return groceryVoice;
            if (item.itemId == "pizza") return pizzaVoice != null ? pizzaVoice : groceryVoice;
            if (item.itemId == "fancy") return fancyVoice != null ? fancyVoice : groceryVoice;
            if (item.itemId == "fast_food") return fastFoodVoice != null ? fastFoodVoice : groceryVoice;
            return groceryVoice;
        }

        IEnumerator SpawnDeliveryAfter(FoodItem item, float delay)
        {
            Debug.Log("[FoodOrder] scheduling delivery of " + (item != null ? item.displayName : "null") + " in " + delay + "s");
            yield return new WaitForSeconds(delay);

            if (item == null) { Debug.LogWarning("[FoodOrder] spawn aborted: item null"); yield break; }
            if (item.prefab == null) { Debug.LogWarning("[FoodOrder] spawn aborted: item.prefab null for " + item.displayName); yield break; }
            if (deliverySpawn == null) { Debug.LogWarning("[FoodOrder] spawn aborted: deliverySpawn null"); yield break; }

            Vector3 jitter = new Vector3(UnityEngine.Random.Range(-0.15f, 0.15f), 0f, UnityEngine.Random.Range(-0.15f, 0.15f));
            Vector3 pos = deliverySpawn.position + jitter + Vector3.up * 0.6f;
            var go = Instantiate(item.prefab, pos, deliverySpawn.rotation);
            Debug.Log("[FoodOrder] spawned " + item.displayName + " at " + pos);

            var eat = go.GetComponent<HackKU.Core.EatOnHeadProximity>();
            if (eat != null)
            {
                eat.foodName = item.displayName;
                eat.hungerRestore = item.hungerRestore;
            }
            ToastHUD.Show(item.displayName, "Delivered at the door", ToastKind.Info);
        }

        string Sanitize(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            s = System.Text.RegularExpressions.Regex.Replace(s, @"\*[^*]+\*|\[[^\]]+\]", "");
            s = System.Text.RegularExpressions.Regex.Replace(s, @"\s{2,}", " ");
            return s.Trim();
        }

        async Task<string> SafeTranscribe(byte[] wav, CancellationToken ct)
        {
            try { return await _groq.TranscribeAsync(wav, ct); }
            catch { return string.Empty; }
        }

        async Task<string> SafeOneShot(string userMsg, CancellationToken ct)
        {
            try
            {
                var msgs = new List<ChatMessage>
                {
                    new ChatMessage("system", "You play short phone-call NPCs for a video game. Output a SINGLE short phone-call line of in-character dialogue. No stage directions, no emojis, no markdown."),
                    new ChatMessage("user", userMsg)
                };
                var resp = await _groq.SendChatAsync(msgs, new List<ToolDef>(), ct);
                if (resp != null && resp.choices != null && resp.choices.Count > 0)
                {
                    var m = resp.choices[0].message;
                    if (m != null) return m.content;
                }
            }
            catch { }
            return null;
        }
    }
}
