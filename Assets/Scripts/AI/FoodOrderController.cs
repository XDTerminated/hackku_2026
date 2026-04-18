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
        [SerializeField] NPCVoiceProfile bankVoice;

        [Header("Grocery box (spawns as 1 box that splits on floor impact)")]
        [SerializeField] GameObject groceryBoxPrefab;

        [Header("Timing")]
        [SerializeField] float dialToneSeconds = 1.0f;
        [SerializeField] float deliveryDelaySeconds = 10f;

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

            // Bank intent first — if the player's first utterance already contains an
            // amount we skip straight to payment; otherwise we greet them and listen again.
            if (IsBankIntent(text))
            {
                yield return RunBankCall(text, ct);
                EndOrder();
                yield break;
            }
            // Short utterances like just "bank" — treat as bank intent too.
            if (IsBankShortTrigger(text))
            {
                yield return RunBankCall("", ct);
                EndOrder();
                yield break;
            }

            FoodItem picked = MatchFood(text);
            int quantity = Mathf.Clamp(ParseQuantity(text), 1, 10);
            NPCVoiceProfile voice = PickVoice(picked);

            if (picked == null)
            {
                SpeakLine("Sorry, I didn't catch that. Try: groceries, pizza, dinner, fast food, or say 'bank' to pay loans.", groceryVoice);
                yield return new WaitForSeconds(0.4f);
                while (npcVoice.IsSpeaking && !ct.IsCancellationRequested) yield return null;
                EndOrder();
                yield break;
            }

            float totalPrice = picked.price * quantity;
            if (StatsManager.Instance != null && StatsManager.Instance.Money < totalPrice)
            {
                SpeakLine("Sorry friend, card got declined. Call us back when you've got the funds.", voice);
                yield return new WaitForSeconds(0.4f);
                while (npcVoice.IsSpeaking && !ct.IsCancellationRequested) yield return null;
                EndOrder();
                yield break;
            }

            string ack = null;
            var ackTask = SafeOneShot(BuildAckPrompt(picked, quantity), ct);
            while (!ackTask.IsCompleted) yield return null;
            if (!ct.IsCancellationRequested) ack = ackTask.Result;
            if (string.IsNullOrWhiteSpace(ack))
                ack = (quantity > 1 ? quantity + " " : "") + picked.displayName + " on the way.";

            SpeakLine(Sanitize(ack), voice);

            string orderLabel = quantity > 1 ? ("Ordered " + quantity + "x " + picked.displayName) : ("Ordered " + picked.displayName);
            // Ordering food gives a small happiness bump — treating yourself.
            float happinessBump = 3f + Mathf.Min(4f, quantity - 1);
            if (StatsManager.Instance != null)
            {
                StatsManager.Instance.ApplyDelta(-totalPrice, happinessBump, orderLabel);
                ToastHUD.Show("-$" + Mathf.Round(totalPrice), orderLabel, ToastKind.Bill);
                ToastHUD.Show("+" + Mathf.RoundToInt(happinessBump) + "%", "Treat yourself", ToastKind.HappinessUp);
            }

            yield return new WaitForSeconds(0.4f);
            while (npcVoice.IsSpeaking && !ct.IsCancellationRequested) yield return null;

            // Wait the delivery delay inline so EndOrder can't cancel the spawn coroutine.
            float waitStart = Time.unscaledTime;
            while (!ct.IsCancellationRequested && Time.unscaledTime - waitStart < deliveryDelaySeconds)
                yield return null;
            if (!ct.IsCancellationRequested) SpawnDeliveryNow(picked, quantity);
            EndOrder();
        }

        // Always resolve the drop point from the scene at spawn-time so a missing/destroyed
        // serialized reference can't silently eat deliveries. Prefers the FrontDoorPivot
        // position offset a bit outside, falls back to the DeliverySpawn transform.
        Vector3 ResolveDeliveryPosition()
        {
            var pivot = GameObject.Find("FrontDoorPivot");
            if (pivot != null)
            {
                // Pivot is at the hinge on the front wall; drop the box 1.4m outside (negative Z).
                return pivot.transform.position + new Vector3(0.6f, 0.4f, -1.4f);
            }
            if (deliverySpawn != null) return deliverySpawn.position;
            var fallback = GameObject.Find("DeliverySpawn");
            if (fallback != null) return fallback.transform.position;
            Debug.LogWarning("[FoodOrder] No FrontDoorPivot or DeliverySpawn found; dropping at origin.");
            return Vector3.up * 0.5f;
        }

        void SpawnDeliveryNow(FoodItem item, int quantity)
        {
            if (item == null) { Debug.LogWarning("[FoodOrder] spawn aborted: item null"); return; }
            if (item.prefab == null) { Debug.LogWarning("[FoodOrder] spawn aborted: item.prefab null for " + item.displayName); return; }

            Vector3 dropPos = ResolveDeliveryPosition();
            int count = Mathf.Clamp(quantity, 1, 10);

            // Every delivery — groceries, pizza, fancy, fast food — arrives as ONE sealed box
            // outside the door. The box splits into `count` individual items when it lands on
            // the floor inside the house.
            if (groceryBoxPrefab == null)
            {
                Debug.LogWarning("[FoodOrder] groceryBoxPrefab not wired; can't deliver.");
                return;
            }
            Vector3 boxPos = dropPos + Vector3.up * 0.2f;
            var box = Instantiate(groceryBoxPrefab, boxPos, Quaternion.identity);
            var gb = box.GetComponent<HackKU.Core.GroceryBox>();
            if (gb != null)
            {
                gb.itemPrefab = item.prefab;
                gb.quantity = count;
                gb.foodName = item.displayName;
                gb.hungerRestore = item.hungerRestore;
                gb.RefreshLabel();
            }
            ToastHUD.Show("x" + count + " " + item.displayName, "Left on the doorstep — bring it inside", ToastKind.Info);
        }

        void SpeakLine(string line, NPCVoiceProfile voice)
        {
            if (voice != null) npcVoice.VoiceProfile = voice;
            npcVoice.Speak(Sanitize(line));
        }

        string BuildAckPrompt(FoodItem item, int quantity)
        {
            string persona =
                item.itemId == "pizza" ? "a Brooklyn Italian pizza-shop owner named Tony" :
                item.itemId == "groceries" ? "a warm British grocery clerk named Priya" :
                item.itemId == "fancy" ? "a fine-dining French maitre d named Laurent" :
                item.itemId == "fast_food" ? "an energetic American fast-food cashier named Mike" :
                "a friendly shopkeeper";
            string qtyPart = quantity > 1 ? quantity + " of '" + item.displayName + "'" : "'" + item.displayName + "'";
            return "Respond AS " + persona + ". The player just ordered " + qtyPart +
                " for $" + Mathf.Round(item.price * quantity) + " total. Say a single natural short phone-call line (1-2 short sentences) " +
                "confirming the order and giving a delivery time. Mention the quantity if it's more than one. Do NOT use asterisks, stage directions, emojis, or quotes. " +
                "Plain spoken English only. End with a brief goodbye.";
        }

        static readonly System.Collections.Generic.Dictionary<string, int> _numberWords =
            new System.Collections.Generic.Dictionary<string, int>
            {
                { "one", 1 }, { "a", 1 }, { "an", 1 }, { "two", 2 }, { "couple", 2 }, { "three", 3 },
                { "four", 4 }, { "five", 5 }, { "six", 6 }, { "seven", 7 }, { "eight", 8 }, { "nine", 9 }, { "ten", 10 },
            };

        int ParseQuantity(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 1;
            string t = text.ToLowerInvariant();
            // Numeric digit first.
            var m = System.Text.RegularExpressions.Regex.Match(t, @"\b(\d{1,2})\b");
            if (m.Success && int.TryParse(m.Groups[1].Value, out int n) && n > 0) return n;
            // Fallback: word-number scan.
            foreach (var kv in _numberWords)
                if (System.Text.RegularExpressions.Regex.IsMatch(t, @"\b" + kv.Key + @"\b")) return kv.Value;
            return 1;
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

        IEnumerator SpawnDeliveryAfter(FoodItem item, int quantity, float delay)
        {
            Debug.Log("[FoodOrder] scheduling delivery of " + quantity + "x " + (item != null ? item.displayName : "null") + " in " + delay + "s");
            yield return new WaitForSeconds(delay);

            if (item == null) { Debug.LogWarning("[FoodOrder] spawn aborted: item null"); yield break; }
            if (item.prefab == null) { Debug.LogWarning("[FoodOrder] spawn aborted: item.prefab null for " + item.displayName); yield break; }
            if (deliverySpawn == null) { Debug.LogWarning("[FoodOrder] spawn aborted: deliverySpawn null"); yield break; }

            int count = Mathf.Clamp(quantity, 1, 10);

            // Groceries arrive as ONE big delivery box that splits into N items when it lands
            // on the floor inside the house. Other deliveries drop N separate items outside.
            if (item.itemId == "groceries" && groceryBoxPrefab != null)
            {
                Vector3 boxPos = deliverySpawn.position + Vector3.up * 0.6f;
                var box = Instantiate(groceryBoxPrefab, boxPos, deliverySpawn.rotation);
                var gb = box.GetComponent<HackKU.Core.GroceryBox>();
                if (gb != null)
                {
                    gb.itemPrefab = item.prefab;
                    gb.quantity = count;
                    gb.foodName = item.displayName;
                    gb.hungerRestore = item.hungerRestore;
                }
                ToastHUD.Show("Grocery box", "Left on the doorstep — bring it inside", ToastKind.Info);
                yield break;
            }

            for (int i = 0; i < count; i++)
            {
                Vector3 jitter = new Vector3(UnityEngine.Random.Range(-0.25f, 0.25f), 0f, UnityEngine.Random.Range(-0.25f, 0.25f));
                Vector3 pos = deliverySpawn.position + jitter + Vector3.up * (0.6f + i * 0.12f);
                var go = Instantiate(item.prefab, pos, deliverySpawn.rotation);
                var eat = go.GetComponent<HackKU.Core.EatOnHeadProximity>();
                if (eat != null)
                {
                    eat.foodName = item.displayName;
                    eat.hungerRestore = item.hungerRestore;
                }
            }
            ToastHUD.Show((count > 1 ? count + "x " : "") + item.displayName, "Delivered at the door", ToastKind.Info);
        }

        // --- Bank call flow -------------------------------------------------------------

        static bool IsBankIntent(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            string t = text.ToLowerInvariant();
            return t.Contains("bank") || t.Contains("loan") || t.Contains("loans") ||
                   t.Contains("pay off") || t.Contains("pay down") || t.Contains("pay my");
        }

        static bool IsBankShortTrigger(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            string t = text.ToLowerInvariant().Trim();
            return t == "bank" || t == "the bank" || t.StartsWith("call the bank") || t.StartsWith("bank please");
        }

        IEnumerator RunBankCall(string firstText, CancellationToken ct)
        {
            var sm = StatsManager.Instance;
            NPCVoiceProfile voice = bankVoice != null ? bankVoice : groceryVoice;

            if (sm == null)
            {
                SpeakLine("Sorry, systems down. Please call back later.", voice);
                while (npcVoice.IsSpeaking && !ct.IsCancellationRequested) yield return null;
                yield break;
            }

            if (sm.Debt <= 0f)
            {
                SpeakLine("Good afternoon — looks like you're already paid off. Nothing more to do here. Have a good one.", voice);
                while (npcVoice.IsSpeaking && !ct.IsCancellationRequested) yield return null;
                yield break;
            }

            // Step 1: did the player's first utterance already include an amount?
            float amount = ParseDollarAmount(firstText);

            // Step 2: if not, greet them and listen for the amount.
            if (amount <= 0f)
            {
                string greeting = "Hello, this is the bank. How much would you like to pay on your loan today?";
                SpeakLine(greeting, voice);
                while (npcVoice.IsSpeaking && !ct.IsCancellationRequested) yield return null;
                yield return new WaitForSeconds(0.2f);

                // Listen for the reply.
                string reply = null;
                yield return ListenOnce(ct, 8f, r => reply = r);
                if (ct.IsCancellationRequested) yield break;
                amount = ParseDollarAmount(reply ?? "");

                if (amount <= 0f)
                {
                    SpeakLine("Sorry, I didn't catch a dollar amount. Let's try that again — call back when you're ready.", voice);
                    while (npcVoice.IsSpeaking && !ct.IsCancellationRequested) yield return null;
                    yield break;
                }
            }

            float applied = Mathf.Min(amount, Mathf.Min(sm.Money, sm.Debt));
            if (applied <= 0f)
            {
                SpeakLine("Your card got declined, I'm afraid. Call back when you've got the funds.", voice);
                while (npcVoice.IsSpeaking && !ct.IsCancellationRequested) yield return null;
                yield break;
            }

            sm.ApplyDelta(-applied, 0f, "Loan payment");
            sm.ApplyDebtDelta(-applied, "Loan payment");
            float happinessGain = Mathf.Clamp(applied / 500f, 1f, 8f);
            sm.ApplyDelta(0f, happinessGain, "Loan relief");

            ToastHUD.Show("-$" + Mathf.Round(applied), "Loan payment", ToastKind.Bill);
            ToastHUD.Show("+" + Mathf.RoundToInt(happinessGain) + "%", "Debt relief", ToastKind.HappinessUp);

            string ackPrompt = "Respond as a calm professional loan servicer named Morgan. The customer just paid $" +
                               Mathf.Round(applied) + " toward their student loan. Remaining balance is about $" +
                               Mathf.RoundToInt(sm.Debt) + ". Confirm the payment in one warm sentence, mention the " +
                               "remaining balance briefly, and sign off. Plain spoken English only.";
            string ack = null;
            var ackTask = SafeOneShot(ackPrompt, ct);
            while (!ackTask.IsCompleted) yield return null;
            if (!ct.IsCancellationRequested) ack = ackTask.Result;
            if (string.IsNullOrWhiteSpace(ack))
                ack = "Got it, $" + Mathf.Round(applied) + " applied. Remaining balance is about $" +
                      Mathf.RoundToInt(sm.Debt) + ". Have a great day.";

            SpeakLine(Sanitize(ack), voice);
            while (npcVoice.IsSpeaking && !ct.IsCancellationRequested) yield return null;
        }

        // Opens the mic, waits for VAD end-of-speech (or timeout), transcribes, returns result.
        IEnumerator ListenOnce(CancellationToken ct, float maxListen, System.Action<string> onResult)
        {
            if (!mic.IsMicOpen) mic.InitializeRecording();
            mic.StartRecording();

            float start = Time.unscaledTime;
            const float minSpeech = 0.5f;
            const float silenceToEnd = 1.2f;
            const float speakingRms = 0.02f;
            const float silenceRms = 0.012f;
            bool spoke = false;
            float spokeAt = 0f;
            float lastAbove = Time.unscaledTime;

            while (!ct.IsCancellationRequested)
            {
                float now = Time.unscaledTime;
                float level = mic.CurrentLevel;
                if (level > speakingRms)
                {
                    if (!spoke) { spoke = true; spokeAt = now; }
                    lastAbove = now;
                }
                if (spoke && now - spokeAt > minSpeech && level < silenceRms && now - lastAbove > silenceToEnd) break;
                if (now - start > maxListen) break;
                yield return null;
            }

            byte[] wav = mic.StopRecordingAndGetWav();
            if (wav == null || wav.Length == 0) { onResult?.Invoke(""); yield break; }

            var sttTask = SafeTranscribe(wav, ct);
            while (!sttTask.IsCompleted) yield return null;
            onResult?.Invoke(ct.IsCancellationRequested ? "" : (sttTask.Result ?? ""));
        }

        static readonly System.Collections.Generic.Dictionary<string, int> _scalarWords =
            new System.Collections.Generic.Dictionary<string, int>
            {
                {"one",1},{"two",2},{"three",3},{"four",4},{"five",5},{"six",6},{"seven",7},{"eight",8},{"nine",9},
                {"ten",10},{"eleven",11},{"twelve",12},{"thirteen",13},{"fourteen",14},{"fifteen",15},
                {"sixteen",16},{"seventeen",17},{"eighteen",18},{"nineteen",19},{"twenty",20},
                {"thirty",30},{"forty",40},{"fifty",50},{"sixty",60},{"seventy",70},{"eighty",80},{"ninety",90},
            };

        // Parses "$500", "500 dollars", "five hundred", "two thousand", "1.5k" — best effort.
        static float ParseDollarAmount(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0f;
            string t = text.ToLowerInvariant();

            // Numeric forms first.
            var numMatch = System.Text.RegularExpressions.Regex.Match(
                t, @"\$?\s*(\d{1,3}(?:[,]\d{3})*|\d+)(?:\.(\d+))?\s*(k|thousand|grand)?");
            if (numMatch.Success)
            {
                string whole = numMatch.Groups[1].Value.Replace(",", "");
                string frac = numMatch.Groups[2].Value;
                string suffix = numMatch.Groups[3].Value;
                if (float.TryParse(whole, out float w))
                {
                    float val = w;
                    if (!string.IsNullOrEmpty(frac) && float.TryParse("0." + frac, out float f)) val += f;
                    if (suffix == "k" || suffix == "thousand" || suffix == "grand") val *= 1000f;
                    return val;
                }
            }

            // Word form — "five hundred", "two thousand five hundred".
            float total = 0f, current = 0f;
            foreach (var tok in System.Text.RegularExpressions.Regex.Split(t, @"[^a-z]+"))
            {
                if (string.IsNullOrEmpty(tok)) continue;
                if (_scalarWords.TryGetValue(tok, out int n)) { current += n; continue; }
                if (tok == "hundred") { current = (current == 0 ? 1 : current) * 100; continue; }
                if (tok == "thousand") { total += (current == 0 ? 1 : current) * 1000; current = 0; continue; }
                if (tok == "million") { total += (current == 0 ? 1 : current) * 1000000; current = 0; continue; }
            }
            return total + current;
        }

        string Sanitize(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            s = System.Text.RegularExpressions.Regex.Replace(s, @"\*[^*]+\*|\[[^\]]+\]", "");
            // Convert "$500" and "1,200 dollars" to spoken-word form so TTS doesn't glitch.
            s = HackKU.Core.SpeechUtils.SpeakifyMoney(s);
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
