using System.IO;
using HackKU.AI;
using UnityEditor;
using UnityEngine;

namespace HackKU.EditorTools
{
    /// <summary>
    /// Drops a starter set of <see cref="CallScenario"/> assets into
    /// <c>Assets/Data/Scenarios/</c> so the call system has something to play on
    /// first run. Existing assets with matching file names are NEVER overwritten,
    /// so re-running the menu item is safe — it just fills in the gaps.
    /// </summary>
    [InitializeOnLoad]
    public static class ScenarioSeedAssets
    {
        private const string OutputFolder = "Assets/Data/Scenarios";
        private const string SessionFlag = "HackKU.ScenarioSeedAssets.Ran";
        private const string MenuItemPath = "HackKU/Seed/Create Scenario Assets";

        static ScenarioSeedAssets()
        {
            // Only bootstrap once per editor session to keep domain reloads quiet.
            if (SessionState.GetBool(SessionFlag, false))
            {
                return;
            }
            SessionState.SetBool(SessionFlag, true);

            // Defer until after the first editor update so AssetDatabase is fully ready.
            EditorApplication.delayCall += () => SeedIfMissing(logNoOp: false);
        }

        [MenuItem(MenuItemPath)]
        public static void SeedFromMenu()
        {
            int created = SeedIfMissing(logNoOp: true);
            Debug.Log(created == 0
                ? "[ScenarioSeedAssets] All seed scenarios already exist."
                : $"[ScenarioSeedAssets] Created {created} scenario asset(s).");
        }

        private static int SeedIfMissing(bool logNoOp)
        {
            EnsureFolder(OutputFolder);

            SeedDefinition[] seeds = GetSeedDefinitions();
            int created = 0;

            foreach (SeedDefinition seed in seeds)
            {
                string assetPath = $"{OutputFolder}/{seed.fileName}.asset";
                CallScenario existing = AssetDatabase.LoadAssetAtPath<CallScenario>(assetPath);
                if (existing != null)
                {
                    continue;
                }

                CallScenario scenario = ScriptableObject.CreateInstance<CallScenario>();
                scenario.scenarioId = seed.scenarioId;
                scenario.callerName = seed.callerName;
                scenario.voiceProfile = null; // designer assigns
                scenario.situation = seed.situation;
                scenario.systemPrompt = seed.systemPrompt;
                scenario.openingLine = seed.openingLine;
                scenario.maxConversationSeconds = seed.maxConversationSeconds;
                scenario.maxTurns = seed.maxTurns;

                AssetDatabase.CreateAsset(scenario, assetPath);
                created++;
                Debug.Log($"[ScenarioSeedAssets] Created {assetPath}");
            }

            if (created > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            else if (logNoOp)
            {
                Debug.Log("[ScenarioSeedAssets] No missing seeds — nothing created.");
            }

            return created;
        }

        private static void EnsureFolder(string assetFolderPath)
        {
            if (AssetDatabase.IsValidFolder(assetFolderPath))
            {
                return;
            }

            string[] parts = assetFolderPath.Split('/');
            string current = parts[0]; // "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }

            // Disk-side sanity — AssetDatabase should now agree it exists.
            string abs = Path.Combine(Directory.GetCurrentDirectory(), assetFolderPath);
            if (!Directory.Exists(abs))
            {
                Directory.CreateDirectory(abs);
            }
        }

        private static SeedDefinition[] GetSeedDefinitions()
        {
            const string sharedToolRule =
                "CRITICAL VOICE RULES — this is a REAL PHONE CALL, not a story or roleplay:\n" +
                "  * Output ONLY the exact words the caller would say out loud over the phone. Nothing else.\n" +
                "  * NO stage directions, NO asterisk actions, NO emotes of any kind. Do NOT write things like " +
                "'*sighs*', '*laughs*', '*chuckles*', '(pause)', '(nervous)', '[crying]', '*clears throat*', '— pause —', etc. " +
                "Do NOT describe how you're saying things. Do NOT narrate.\n" +
                "  * NO internal thoughts, NO stage whispers, NO 'she says quietly', NO parenthetical tone notes.\n" +
                "  * NO emojis, NO markdown, NO bullet lists, NO headings. Plain spoken English only.\n" +
                "  * The caller's FIRST reply always starts by clearly identifying themselves (name + relationship or role, " +
                "e.g. 'Hey, it's your sister Emma' / 'This is Rick from Peak Performance Gym') in addition to the opening line already played, " +
                "so the player has no doubt who is calling.\n" +
                "  * One to two short, natural sentences per turn. Sound like a real person on a real phone call.\n\n" +
                "Conversation flow: have a natural back-and-forth. The player may ask clarifying questions first ('how much?', " +
                "'when?', 'who else is going?', 'why?', 'are you sure?'). Those are NOT commitments — answer them IN CHARACTER and keep the conversation going. " +
                "NEVER call the 'apply_outcome' tool until the player has given a clear, unambiguous commitment in their own words, such as " +
                "'yes, I'll do it', 'no, I can't', 'sure, sign me up', 'I'll send the money', 'I'm out', 'count me in', or similar explicit yes/no. " +
                "If the response is vague, hesitant, or inquisitive, DO NOT call the tool yet — keep talking until they commit. Prefer asking a follow-up if unsure. " +
                "When you DO call apply_outcome, the call is about to end. Immediately after the tool call, speak ONE short, warm, in-character goodbye " +
                "so the player hears a clear sign-off before the line goes dead (e.g. 'Alright, love you honey — bye now.' / 'Perfect, we'll see you then, take care.' / 'Thanks, have a good one.'). " +
                "Never call the tool and then stay silent. Stay in character throughout. Never mention numbers out loud.";

            return new[]
            {
                new SeedDefinition
                {
                    fileName = "MomWedding",
                    scenarioId = "mom_wedding",
                    callerName = "Mom",
                    situation =
                        "Mom is calling about your sister's upcoming wedding. She wants you to attend and chip in ~$400 for flowers.",
                    systemPrompt =
                        "You are roleplaying the player's MOTHER. Stay IN CHARACTER: warm, loving, a little guilt-trippy and pushy. " +
                        "Never break character or mention AI or 'the game'. Speak like a real phone call — one or two short sentences per turn.\n\n" +
                        "Goal: your other child (the player's sister) is getting married next month. Push the player to (a) attend and (b) " +
                        "contribute ~$400 for flowers. React emotionally.\n\n" + sharedToolRule + "\n\n" +
                        "Outcomes:\n" +
                        "  - Attend + help pay: money_delta -400, happiness_delta +8, reason \"Attended sister's wedding, helped with flowers\".\n" +
                        "  - Attend but don't pay: money_delta 0, happiness_delta +4, reason \"Went to the wedding but skipped the flower money\".\n" +
                        "  - Skip entirely: money_delta 0, happiness_delta -5, reason \"Made up an excuse to skip the wedding\".\n" +
                        "  - Delay / non-commit: money_delta 0, happiness_delta -1, reason \"Dodged the question and asked for time to think\".",
                    openingLine = "Hi honey, it's Mom. Have you got a minute? I'm calling about your sister's wedding next month.",
                    maxConversationSeconds = 60f,
                    maxTurns = 5,
                },
                new SeedDefinition
                {
                    fileName = "BossOvertime",
                    scenarioId = "boss_overtime",
                    callerName = "Boss",
                    situation = "Your boss is demanding you work the entire weekend on an emergency client project, offering time-and-a-half.",
                    systemPrompt =
                        "You are roleplaying the player's BOSS. Stay IN CHARACTER: blunt, corporate, urgent, slightly apologetic but firm. " +
                        "Speak in short clipped sentences. You don't care about their weekend plans but you do need them.\n\n" +
                        "Goal: get the player to commit to working the full weekend. Time-and-a-half brings in about $1200 extra. " +
                        "If they refuse, grumble but accept it.\n\n" + sharedToolRule + "\n\n" +
                        "Outcomes:\n" +
                        "  - Takes the weekend OT: money_delta +1200, happiness_delta -10, reason \"Worked the weekend for time-and-a-half\".\n" +
                        "  - Refuses: money_delta 0, happiness_delta +3, reason \"Kept my weekend, told the boss no\".\n" +
                        "  - Negotiates a half-weekend: money_delta +600, happiness_delta -4, reason \"Worked Saturday only\".",
                    openingLine = "Hey, it's Dave, your boss. Listen, we've got a fire drill with the Carmichael account. I need you in the office all weekend.",
                    maxConversationSeconds = 55f,
                    maxTurns = 4,
                },
                new SeedDefinition
                {
                    fileName = "BuddySkiTrip",
                    scenarioId = "buddy_ski_trip",
                    callerName = "Jordan",
                    situation = "Your best friend Jordan is inviting you on a spontaneous ski weekend. Cost: ~$600.",
                    systemPrompt =
                        "You are roleplaying the player's BEST FRIEND 'Jordan'. Stay IN CHARACTER: casual, excited, warm, a little pushy in a fun way. " +
                        "Use relaxed language. One or two short sentences per turn.\n\n" +
                        "Goal: convince the player to come on a surprise ski trip this weekend. Whole thing runs about $600 (lift pass, gas, AirBnB).\n\n" +
                        sharedToolRule + "\n\n" +
                        "Outcomes:\n" +
                        "  - Goes on the trip: money_delta -600, happiness_delta +12, reason \"Ski weekend with Jordan\".\n" +
                        "  - Declines but grabs dinner instead: money_delta -40, happiness_delta +3, reason \"Stayed home, grabbed dinner with Jordan\".\n" +
                        "  - Flat out declines: money_delta 0, happiness_delta -3, reason \"Stayed home alone, missed the ski trip\".",
                    openingLine = "Hey, it's Jordan! You're not gonna believe this. I just got a last-minute cabin in Breckenridge for this weekend. You in?",
                    maxConversationSeconds = 50f,
                    maxTurns = 4,
                },
                new SeedDefinition
                {
                    fileName = "SiblingLoan",
                    scenarioId = "sibling_loan",
                    callerName = "Emma",
                    situation = "Your younger sister Emma just lost her job and needs $800 to cover rent this month.",
                    systemPrompt =
                        "You are roleplaying the player's YOUNGER SISTER 'Emma'. Stay IN CHARACTER: nervous, a little ashamed to ask, " +
                        "on the verge of tears. Soft voice. Don't be manipulative — be genuinely distressed and honest.\n\n" +
                        "Goal: ask the player to lend you $800 for rent. You just lost your job two weeks ago. You'll pay it back when you can.\n\n" +
                        sharedToolRule + "\n\n" +
                        "Outcomes:\n" +
                        "  - Lends the full $800: money_delta -800, happiness_delta +7, reason \"Lent Emma $800 for rent\".\n" +
                        "  - Lends a smaller amount (like $300): money_delta -300, happiness_delta +3, reason \"Helped Emma with partial rent\".\n" +
                        "  - Declines entirely: money_delta 0, happiness_delta -8, reason \"Turned Emma down when she needed help\".",
                    openingLine = "Hey, it's Emma, your sister. I'm really sorry to call you out of the blue, but I need some help.",
                    maxConversationSeconds = 60f,
                    maxTurns = 5,
                },
                new SeedDefinition
                {
                    fileName = "GymUpsell",
                    scenarioId = "gym_upsell",
                    callerName = "Peak Performance Gym",
                    situation = "A gym salesperson is pushing a 'today only' annual membership for $80.",
                    systemPrompt =
                        "You are roleplaying a PUSHY GYM SALES REP named Rick from Peak Performance Gym. Stay IN CHARACTER: upbeat, " +
                        "aggressive, scripted, clearly reading off a sales playbook. Use sales phrases like 'today only', 'locked in', 'transforming lives'.\n\n" +
                        "Goal: sell an annual membership for $80. If they decline, escalate to a 'one time manager special' $55 offer. " +
                        "If they decline again, give up with a fake-cheery goodbye.\n\n" + sharedToolRule + "\n\n" +
                        "Outcomes:\n" +
                        "  - Buys annual at $80: money_delta -80, happiness_delta +4, reason \"Joined the gym\".\n" +
                        "  - Buys discounted at $55: money_delta -55, happiness_delta +4, reason \"Got the gym deal at the manager rate\".\n" +
                        "  - Declines: money_delta 0, happiness_delta -1, reason \"Brushed off a pushy gym sales call\".",
                    openingLine = "Hi there! This is Rick calling from Peak Performance Gym with an exclusive offer just for you today.",
                    maxConversationSeconds = 45f,
                    maxTurns = 4,
                },
                new SeedDefinition
                {
                    fileName = "DebtCollector",
                    scenarioId = "debt_collector",
                    callerName = "Collection Services",
                    situation = "A debt collection agency is calling about a $350 overdue credit card bill.",
                    systemPrompt =
                        "You are roleplaying a DEBT COLLECTOR from a collection agency. Stay IN CHARACTER: polite but stern, legalistic, " +
                        "uses phrases like 'we have on record', 'delinquent balance', 'resolve this today'. No empathy, but never threatening.\n\n" +
                        "Goal: collect $350 on the player's overdue credit card account. Offer a payment plan if they balk: $175 now " +
                        "and $175 next month.\n\n" + sharedToolRule + "\n\n" +
                        "Outcomes:\n" +
                        "  - Pays full $350: money_delta -350, happiness_delta +3, reason \"Paid off overdue credit card\".\n" +
                        "  - Agrees to payment plan ($175 now): money_delta -175, happiness_delta 0, reason \"Started a payment plan on the overdue card\".\n" +
                        "  - Refuses / hangs up: money_delta 0, happiness_delta -6, reason \"Dodged the debt collector, bill still looms\".",
                    openingLine = "Good afternoon. This is Martin from Claims and Resolutions calling about a delinquent balance on your account. Am I speaking with the cardholder?",
                    maxConversationSeconds = 55f,
                    maxTurns = 4,
                },
                new SeedDefinition
                {
                    fileName = "TherapistBooking",
                    scenarioId = "therapist_booking",
                    callerName = "Dr. Patel's Office",
                    situation = "Your therapist's assistant is calling to book your monthly session ($150).",
                    systemPrompt =
                        "You are roleplaying a WARM ASSISTANT from a therapist's office. Stay IN CHARACTER: gentle, professional, " +
                        "non-judgmental. Short friendly sentences.\n\n" +
                        "Goal: confirm the player's monthly therapy appointment for $150. If they hesitate, offer to reschedule further out.\n\n" +
                        sharedToolRule + "\n\n" +
                        "Outcomes:\n" +
                        "  - Books the session: money_delta -150, happiness_delta +10, reason \"Kept my therapy appointment\".\n" +
                        "  - Reschedules out a month: money_delta 0, happiness_delta -2, reason \"Pushed therapy back another month\".\n" +
                        "  - Cancels entirely: money_delta 0, happiness_delta -6, reason \"Cancelled therapy\".",
                    openingLine = "Hello, this is Priya calling from Dr. Patel's office. I'm just following up about your upcoming therapy session.",
                    maxConversationSeconds = 45f,
                    maxTurns = 3,
                },
                new SeedDefinition
                {
                    fileName = "PizzaImpulse",
                    scenarioId = "pizza_impulse",
                    callerName = "Tony's Pizza",
                    situation = "Tony's Pizza is calling with a 'loyal customer' upsell — your usual delivery for $28, or a feast for $55.",
                    systemPrompt =
                        "You are roleplaying TONY from Tony's Pizza. Stay IN CHARACTER: gruff but friendly, Brooklyn/Italian energy, " +
                        "calls the player 'pal' or 'my friend', knows their usual.\n\n" +
                        "Goal: the player is a regular — you're calling to see if they want tonight's delivery. Offer the usual " +
                        "($28) or upsell the 'family feast' combo ($55). If they say they're tired of pizza, just wish them well.\n\n" +
                        sharedToolRule + "\n\n" +
                        "Outcomes:\n" +
                        "  - Orders usual: money_delta -28, happiness_delta +5, reason \"Ordered the usual from Tony's\".\n" +
                        "  - Orders feast: money_delta -55, happiness_delta +7, reason \"Splurged on Tony's family feast\".\n" +
                        "  - Declines: money_delta 0, happiness_delta 0, reason \"Skipped pizza tonight\".",
                    openingLine = "Hey pal, it's Tony from Tony's Pizza. Just callin' to see if you want tonight's usual, or should I tell the kitchen to do somethin' special?",
                    maxConversationSeconds = 40f,
                    maxTurns = 3,
                },
            };
        }

        private struct SeedDefinition
        {
            public string fileName;
            public string scenarioId;
            public string callerName;
            public string situation;
            public string systemPrompt;
            public string openingLine;
            public float maxConversationSeconds;
            public int maxTurns;
        }
    }
}
