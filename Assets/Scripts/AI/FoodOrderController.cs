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
        [SerializeField] NPCVoiceProfile brokerVoice;

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
            if (phone == null)
                phone = UnityEngine.Object.FindFirstObjectByType<HackKU.Core.RotaryPhone>();
            if (phone != null)
            {
                phone.OnDialOutRequested += HandleDialOut;
                phone.OnHungUp += HandleHungUp;
                Debug.Log($"[FoodOrder] subscribed to RotaryPhone events on '{phone.gameObject.name}' (id={phone.GetInstanceID()})");
            }
            else
            {
                Debug.LogError("[FoodOrder] No RotaryPhone found — cannot subscribe to dial-out events!");
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
            Debug.Log("[FoodOrder] HandleDialOut fired — player picked up handset for outgoing call.");
            if (IsOrderActive) { Debug.Log("[FoodOrder] ...ignored: order already active."); return; }
            var cc = UnityEngine.Object.FindFirstObjectByType<CallController>();
            if (cc != null && cc.IsCallActive) { Debug.Log("[FoodOrder] ...ignored: incoming call is active."); return; }
            if (npcVoice == null || mic == null) { Debug.LogError("[FoodOrderController] missing refs"); return; }

            IsOrderActive = true;
            _cts = new CancellationTokenSource();
            _flow = StartCoroutine(Run(_cts.Token));
        }

        void HandleHungUp()
        {
            Debug.Log($"[FoodOrder] HandleHungUp — phone was docked. IsOrderActive={IsOrderActive}");
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
            Debug.Log("[FoodOrder] Run() coroutine entered — waiting for dial tone.");
            if (!mic.IsMicOpen) mic.InitializeRecording();
            yield return new WaitForSeconds(dialToneSeconds);
            if (ct.IsCancellationRequested) { Debug.Log("[FoodOrder] cancelled during dial tone."); yield break; }

            Debug.Log("[FoodOrder] starting mic recording, listening...");
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
            Debug.Log($"[FoodOrder] VAD loop exited. IsOrderActive={IsOrderActive} ct.IsCancellationRequested={ct.IsCancellationRequested} wavBytes={(wav != null ? wav.Length : 0)}");
            if (wav == null || wav.Length == 0)
            {
                Debug.Log("[FoodOrder] Empty wav — speaking 'didnt catch you' and bailing.");
                SpeakLine("Sorry, I didn't catch you there. Give us a call back.", groceryVoice);
                yield return new WaitForSeconds(0.4f);
                while (npcVoice.IsSpeaking && !ct.IsCancellationRequested) yield return null;
                EndOrder();
                yield break;
            }

            Debug.Log("[FoodOrder] transcribing...");
            var sttTask = SafeTranscribe(wav, ct);
            while (!sttTask.IsCompleted) yield return null;
            if (ct.IsCancellationRequested) { Debug.Log("[FoodOrder] cancelled during STT."); EndOrder(); yield break; }

            string text = (sttTask.Result ?? string.Empty).Trim();
            Debug.Log($"[FoodOrder] STT heard: \"{text}\"");

            // Withdraw intent — pulling money out of the market back into Checking.
            // Evaluated before "invest" so phrases like "sell my stocks" don't get
            // swallowed by the broader invest matcher.
            if (IsWithdrawIntent(text))
            {
                yield return RunWithdrawCall(text, ct);
                EndOrder();
                yield break;
            }
            // Invest intent — routing to the brokerage flow.
            if (IsInvestIntent(text))
            {
                yield return RunInvestCall(text, ct);
                EndOrder();
                yield break;
            }
            // Bank intent — if the player's first utterance already contains an
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
                SpeakLine("Sorry, I didn't catch that. Try: groceries, pizza, dinner, fast food, say 'bank' to pay loans, 'invest' to buy stocks, or 'sell' to cash out.", groceryVoice);
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

            // --- COMMIT POINT -------------------------------------------------------
            // Match is valid + funds are sufficient. Deduct money and LAUNCH DELIVERY NOW
            // BEFORE any LLM speech, so hanging up mid-ack can never cancel the box spawn.
            string orderLabel = quantity > 1 ? ("Ordered " + quantity + "x " + picked.displayName) : ("Ordered " + picked.displayName);
            float happinessBump = 3f + Mathf.Min(4f, quantity - 1);
            if (StatsManager.Instance != null)
            {
                StatsManager.Instance.ApplyDelta(-totalPrice, happinessBump, orderLabel);
                ToastHUD.Show("-$" + Mathf.Round(totalPrice), orderLabel, ToastKind.Bill);
                ToastHUD.Show("+" + Mathf.RoundToInt(happinessBump) + "%", "Treat yourself", ToastKind.HappinessUp);
            }
            Debug.Log($"[FoodOrder] money deducted; launching detached delivery coroutine for {quantity}x {picked.displayName}");
            DeliveryRunner.Run(DeliverAfterDelay(picked, quantity, deliveryDelaySeconds));
            // ------------------------------------------------------------------------

            // Now speak the ack. Cancellation (handset docked) simply cuts the speech short;
            // delivery is already on its way regardless.
            string ack = null;
            var ackTask = SafeOneShot(BuildAckPrompt(picked, quantity), ct);
            while (!ackTask.IsCompleted) yield return null;
            if (!ct.IsCancellationRequested) ack = ackTask.Result;
            if (string.IsNullOrWhiteSpace(ack))
                ack = "Your " + picked.displayName.ToLowerInvariant() + " will be there soon. Bye!";

            if (!ct.IsCancellationRequested && IsOrderActive)
            {
                SpeakLine(Sanitize(ack), voice);
                yield return new WaitForSeconds(0.4f);
                while (npcVoice.IsSpeaking && !ct.IsCancellationRequested) yield return null;
            }
            EndOrder();
        }

        IEnumerator DeliverAfterDelay(FoodItem item, int quantity, float delay)
        {
            HackKU.Core.DeliveryState.Label =
                (quantity > 1 ? quantity + "x " : "") + (item != null ? item.displayName : "Delivery");
            HackKU.Core.DeliveryState.Remaining = delay;
            while (HackKU.Core.DeliveryState.Remaining > 0f)
            {
                HackKU.Core.DeliveryState.Remaining -= Time.unscaledDeltaTime;
                yield return null;
            }
            HackKU.Core.DeliveryState.Remaining = 0f;
            HackKU.Core.DeliveryState.Label = null;

            // If a DeliveryTruck is in the scene (even hidden from a previous cycle), it pulls
            // up to the curb and the box spawns on its pause callback; otherwise spawn directly.
            var trucks = UnityEngine.Object.FindObjectsByType<HackKU.Core.DeliveryTruck>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            var truck = trucks != null && trucks.Length > 0 ? trucks[0] : null;
            if (truck != null) truck.DeliverCycle(() => SpawnDeliveryNow(item, quantity));
            else SpawnDeliveryNow(item, quantity);
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
            Debug.Log($"[FoodOrder][spawn] ENTRY item={(item!=null?item.displayName:"null")} qty={quantity} groceryBoxPrefab={(groceryBoxPrefab!=null?groceryBoxPrefab.name:"NULL")}");
            if (item == null) { Debug.LogWarning("[FoodOrder] spawn aborted: item null"); return; }
            if (item.prefab == null) { Debug.LogWarning("[FoodOrder] spawn aborted: item.prefab null for " + item.displayName); return; }

            Vector3 dropPos = ResolveDeliveryPosition();
            Debug.Log($"[FoodOrder][spawn] resolved dropPos={dropPos}");
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
            Debug.Log($"[FoodOrder] Spawning {count}x {item.displayName} box at {boxPos}");
            var box = Instantiate(groceryBoxPrefab, boxPos, Quaternion.identity);
            if (box == null) Debug.LogError("[FoodOrder] Instantiate returned null!");
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
            string foodWord = item.displayName.ToLowerInvariant();
            return "Respond AS " + persona + ". The player just ordered " + foodWord + ". " +
                "Say ONE short declarative line following this exact pattern: say their " + foodWord +
                " will be there soon, then a brief goodbye. That's it. " +
                "DO NOT mention dollar amounts, quantity, specific times, dates, or minutes. " +
                "DO NOT say things like 'a dozen items' or 'by 7pm' or 'in 20 minutes'. " +
                "DO NOT ask questions or offer upgrades. " +
                "Example good line: 'Your " + foodWord + " will be there soon, take care!'. " +
                "No asterisks, stage directions, emojis, or quotes. Plain spoken English only.";
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

        // "loan / debt / pay down / pay off / student" — banker should skip the loan-or-invest
        // menu and go straight to loan payment.
        static bool MentionsLoan(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            string t = text.ToLowerInvariant();
            return t.Contains("loan") || t.Contains("loans") || t.Contains("debt") ||
                   t.Contains("pay off") || t.Contains("pay down") || t.Contains("pay my loan") ||
                   t.Contains("student");
        }

        static bool IsBankShortTrigger(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            string t = text.ToLowerInvariant().Trim();
            return t == "bank" || t == "the bank" || t.StartsWith("call the bank") || t.StartsWith("bank please");
        }

        static bool IsInvestIntent(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            string t = text.ToLowerInvariant();
            return t.Contains("invest") || t.Contains("stocks") || t.Contains("stock market") ||
                   t.Contains("brokerage") || t.Contains("broker") || t.Contains("mutual fund") ||
                   t.Contains("index fund") || t.Contains("buy shares");
        }

        static bool IsWithdrawIntent(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            string t = text.ToLowerInvariant();
            // "sell" / "cash out" / "liquidate" are unambiguous; "withdraw" only counts
            // when paired with something market-flavored so it doesn't steal bank calls.
            if (t.Contains("cash out") || t.Contains("cash in") || t.Contains("liquidate") ||
                t.Contains("sell my") || t.Contains("sell everything") || t.Contains("sell all") ||
                t.Contains("sell stocks") || t.Contains("sell the stocks") || t.Contains("pull out"))
                return true;
            // "take out" / "withdraw" / "pull" count when combined with something market-flavored.
            bool marketish = t.Contains("invest") || t.Contains("stock") || t.Contains("broker") ||
                             t.Contains("market") || t.Contains("portfolio") || t.Contains("shares");
            if (marketish && (t.Contains("take out") || t.Contains("withdraw") || t.Contains("pull") ||
                              t.Contains("take some") || t.Contains("get some") || t.Contains("get out")))
                return true;
            if (t.Contains("withdraw") && t.Contains("all")) return true;
            return false;
        }

        static bool MentionsAll(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            string t = text.ToLowerInvariant();
            return System.Text.RegularExpressions.Regex.IsMatch(
                t, @"\b(all|everything|entire|max|maximum|whole)\b");
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

            // If the player's first utterance clearly mentions investing, route to the
            // broker flow so "call the bank, invest $500" works as a single call.
            if (IsInvestIntent(firstText) || IsWithdrawIntent(firstText))
            {
                yield return IsWithdrawIntent(firstText) ? RunWithdrawCall(firstText, ct)
                                                        : RunInvestCall(firstText, ct);
                yield break;
            }

            // If no explicit intent, ask up front which the player wants — loan or invest.
            bool routedToInvest = false;
            if (!MentionsLoan(firstText))
            {
                string greeting = sm.Debt > 0f
                    ? "Hello, this is the bank. Would you like to pay down your loan, invest money, or cash out some of your investments today?"
                    : "Hello, this is the bank. Looks like your loan is already paid off — would you like to invest, or cash out some of your investments today?";
                SpeakLine(greeting, voice);
                while (npcVoice.IsSpeaking && !ct.IsCancellationRequested) yield return null;
                yield return new WaitForSeconds(0.2f);

                string reply = null;
                yield return ListenOnce(ct, 8f, r => reply = r);
                if (ct.IsCancellationRequested) yield break;
                reply = reply ?? string.Empty;

                if (IsWithdrawIntent(reply)) { yield return RunWithdrawCall(reply, ct); yield break; }
                if (IsInvestIntent(reply)) { yield return RunInvestCall(reply, ct); yield break; }
                // No invest keywords + debt is zero → nothing to do.
                if (sm.Debt <= 0f)
                {
                    SpeakLine("Alright, your loan is already clear. Give us a ring when you'd like to invest.", voice);
                    while (npcVoice.IsSpeaking && !ct.IsCancellationRequested) yield return null;
                    yield break;
                }
                firstText = reply; // reuse whatever they said as the loan-amount seed
            }

            if (sm.Debt <= 0f)
            {
                SpeakLine("Good afternoon — looks like you're already paid off. Nothing more to do here. Have a good one.", voice);
                while (npcVoice.IsSpeaking && !ct.IsCancellationRequested) yield return null;
                yield break;
            }

            // Step 1: did the player's utterance already include an amount?
            float amount = ParseDollarAmount(firstText);
            _ = routedToInvest;

            // Step 2: if not, greet + listen. If we don't catch an amount, ask again up to 3 times
            // before giving up. Player should never have to redial just because we misheard.
            if (amount <= 0f)
            {
                string[] prompts =
                {
                    "Hello, this is the bank. How much would you like to pay on your loan today?",
                    "Sorry, I didn't catch a dollar amount — how much would you like to pay?",
                    "One more time, please — how many dollars toward your loan today?",
                };

                for (int attempt = 0; attempt < prompts.Length && amount <= 0f; attempt++)
                {
                    SpeakLine(prompts[attempt], voice);
                    while (npcVoice.IsSpeaking && !ct.IsCancellationRequested) yield return null;
                    if (ct.IsCancellationRequested) yield break;
                    yield return new WaitForSeconds(0.2f);

                    string reply = null;
                    yield return ListenOnce(ct, 8f, r => reply = r);
                    if (ct.IsCancellationRequested) yield break;
                    amount = ParseDollarAmount(reply ?? "");
                }

                if (amount <= 0f)
                {
                    SpeakLine("Alright, no problem — give us a call back when you've decided. Take care.", voice);
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

        // --- Brokerage (invest) flow ---------------------------------------------------

        IEnumerator RunInvestCall(string firstText, CancellationToken ct)
        {
            var sm = StatsManager.Instance;
            var im = InvestmentManager.Instance;
            NPCVoiceProfile voice = brokerVoice != null ? brokerVoice : (bankVoice != null ? bankVoice : groceryVoice);

            if (sm == null || im == null)
            {
                SpeakLine("Sorry, the market desk is down. Please call back.", voice);
                while (npcVoice.IsSpeaking && !ct.IsCancellationRequested) yield return null;
                yield break;
            }
            // Gated: player must have purchased the Investment Board before the broker will
            // take any new deposits. Existing holdings still tick & can be withdrawn.
            if (!HackKU.Core.InvestmentManager.CanInvest)
            {
                SpeakLine("Looks like you don't have a trading desk set up yet. Get yourself an investment board and give us a ring back.", voice);
                while (npcVoice.IsSpeaking && !ct.IsCancellationRequested) yield return null;
                yield break;
            }

            bool wantsAll = MentionsAll(firstText);
            float amount = wantsAll ? sm.Money : ParseDollarAmount(firstText);

            if (amount <= 0f && !wantsAll)
            {
                string greeting = "Hi, this is the brokerage. Your checking balance is about " +
                                  SpokenDollars(sm.Money) + ". How much would you like to invest today?";
                SpeakLine(greeting, voice);
                while (npcVoice.IsSpeaking && !ct.IsCancellationRequested) yield return null;
                yield return new WaitForSeconds(0.2f);

                string reply = null;
                yield return ListenOnce(ct, 8f, r => reply = r);
                if (ct.IsCancellationRequested) yield break;
                wantsAll = MentionsAll(reply ?? "");
                amount = wantsAll ? sm.Money : ParseDollarAmount(reply ?? "");

                if (amount <= 0f)
                {
                    SpeakLine("No dollar amount came through. Give us a ring back when you're ready.", voice);
                    while (npcVoice.IsSpeaking && !ct.IsCancellationRequested) yield return null;
                    yield break;
                }
            }

            float applied = im.Deposit(amount);
            if (applied <= 0f)
            {
                SpeakLine("Funds didn't clear, I'm afraid. Check your checking balance and call us back.", voice);
                while (npcVoice.IsSpeaking && !ct.IsCancellationRequested) yield return null;
                yield break;
            }

            ToastHUD.Show("-$" + Mathf.Round(applied), "Invested", ToastKind.Bill);

            string ackPrompt = "Respond as a friendly brokerage agent named Riley. The customer just invested $" +
                               Mathf.Round(applied) + " in the market. Their invested balance is now about $" +
                               Mathf.RoundToInt(im.Invested) + ". Confirm the trade in one warm sentence, " +
                               "hint that values fluctuate, and sign off. Plain spoken English only.";
            string ack = null;
            var ackTask = SafeOneShot(ackPrompt, ct);
            while (!ackTask.IsCompleted) yield return null;
            if (!ct.IsCancellationRequested) ack = ackTask.Result;
            if (string.IsNullOrWhiteSpace(ack))
                ack = "Got it, $" + Mathf.Round(applied) + " invested. Your portfolio is about $" +
                      Mathf.RoundToInt(im.Invested) + ". Markets move — we'll be here when you want to sell.";

            SpeakLine(Sanitize(ack), voice);
            while (npcVoice.IsSpeaking && !ct.IsCancellationRequested) yield return null;
        }

        IEnumerator RunWithdrawCall(string firstText, CancellationToken ct)
        {
            var sm = StatsManager.Instance;
            var im = InvestmentManager.Instance;
            NPCVoiceProfile voice = brokerVoice != null ? brokerVoice : (bankVoice != null ? bankVoice : groceryVoice);

            if (sm == null || im == null)
            {
                SpeakLine("Sorry, the market desk is down. Please call back.", voice);
                while (npcVoice.IsSpeaking && !ct.IsCancellationRequested) yield return null;
                yield break;
            }

            if (im.Invested <= 0f)
            {
                SpeakLine("Looks like you don't hold any positions right now. Nothing to sell.", voice);
                while (npcVoice.IsSpeaking && !ct.IsCancellationRequested) yield return null;
                yield break;
            }

            bool wantsAll = MentionsAll(firstText);
            float amount = wantsAll ? im.Invested : ParseDollarAmount(firstText);

            if (amount <= 0f && !wantsAll)
            {
                string greeting = "Brokerage here. Your portfolio sits at about " +
                                  SpokenDollars(im.Invested) + ". How much would you like to cash out?";
                SpeakLine(greeting, voice);
                while (npcVoice.IsSpeaking && !ct.IsCancellationRequested) yield return null;
                yield return new WaitForSeconds(0.2f);

                string reply = null;
                yield return ListenOnce(ct, 8f, r => reply = r);
                if (ct.IsCancellationRequested) yield break;
                wantsAll = MentionsAll(reply ?? "");
                amount = wantsAll ? im.Invested : ParseDollarAmount(reply ?? "");

                if (amount <= 0f)
                {
                    SpeakLine("Didn't catch a dollar amount. Call back when you're ready to sell.", voice);
                    while (npcVoice.IsSpeaking && !ct.IsCancellationRequested) yield return null;
                    yield break;
                }
            }

            float applied = im.Withdraw(amount);
            if (applied <= 0f)
            {
                SpeakLine("Couldn't complete that — nothing to sell on this end.", voice);
                while (npcVoice.IsSpeaking && !ct.IsCancellationRequested) yield return null;
                yield break;
            }

            ToastHUD.Show("+$" + Mathf.Round(applied), "Withdrew investment", ToastKind.Income);

            string ackPrompt = "Respond as a friendly brokerage agent named Riley. The customer just cashed out $" +
                               Mathf.Round(applied) + " from their portfolio. Remaining invested balance is about $" +
                               Mathf.RoundToInt(im.Invested) + ". Confirm the sale in one warm sentence and sign off. " +
                               "Plain spoken English only.";
            string ack = null;
            var ackTask = SafeOneShot(ackPrompt, ct);
            while (!ackTask.IsCompleted) yield return null;
            if (!ct.IsCancellationRequested) ack = ackTask.Result;
            if (string.IsNullOrWhiteSpace(ack))
                ack = "Done — $" + Mathf.Round(applied) + " is back in your checking. Portfolio is about $" +
                      Mathf.RoundToInt(im.Invested) + ". Talk soon.";

            SpeakLine(Sanitize(ack), voice);
            while (npcVoice.IsSpeaking && !ct.IsCancellationRequested) yield return null;
        }

        static string SpokenDollars(float v) => "$" + Mathf.RoundToInt(v);

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
