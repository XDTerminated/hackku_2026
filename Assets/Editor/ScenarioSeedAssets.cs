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
            if (SessionState.GetBool(SessionFlag, false)) return;
            SessionState.SetBool(SessionFlag, true);
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

        [MenuItem("HackKU/Seed/Force Overwrite All Scenarios")]
        public static void ForceOverwriteFromMenu()
        {
            EnsureFolder(OutputFolder);
            SeedDefinition[] seeds = GetSeedDefinitions();
            int updated = 0;
            foreach (SeedDefinition seed in seeds)
            {
                string assetPath = $"{OutputFolder}/{seed.fileName}.asset";
                CallScenario s = AssetDatabase.LoadAssetAtPath<CallScenario>(assetPath);
                if (s == null)
                {
                    s = ScriptableObject.CreateInstance<CallScenario>();
                    AssetDatabase.CreateAsset(s, assetPath);
                }
                s.scenarioId = seed.scenarioId;
                s.callerName = seed.callerName;
                s.situation = seed.situation;
                s.systemPrompt = seed.systemPrompt;
                s.openingLine = seed.openingLine;
                s.maxConversationSeconds = seed.maxConversationSeconds;
                s.maxTurns = seed.maxTurns;
                s.yesMoneyDelta = seed.yesMoney;
                s.yesHappinessDelta = seed.yesHappiness;
                s.yesReason = seed.yesReason;
                s.noMoneyDelta = seed.noMoney;
                s.noHappinessDelta = seed.noHappiness;
                s.noReason = seed.noReason;
                EditorUtility.SetDirty(s);
                updated++;
            }

            // Retired scenarios — delete their .asset files so CallDirector rewire doesn't pick them up.
            string[] obsolete = {
                "PizzaImpulse", "DebtCollector", "LandlordLateRent", "ScamIRS",
                "BossOvertime", "GymUpsell", "InsuranceUpsell", "DentistReminder",
                "FreelanceGig", "OvernightShift", "PlasmaDonation", "DadAdvice",
            };
            foreach (var name in obsolete)
            {
                string p = $"{OutputFolder}/{name}.asset";
                if (AssetDatabase.LoadAssetAtPath<CallScenario>(p) != null)
                    AssetDatabase.DeleteAsset(p);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ScenarioSeedAssets] Force-updated {updated} scenarios.");
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
                if (existing != null) continue;
                CallScenario scenario = ScriptableObject.CreateInstance<CallScenario>();
                scenario.scenarioId = seed.scenarioId;
                scenario.callerName = seed.callerName;
                scenario.voiceProfile = null;
                scenario.situation = seed.situation;
                scenario.systemPrompt = seed.systemPrompt;
                scenario.openingLine = seed.openingLine;
                scenario.maxConversationSeconds = seed.maxConversationSeconds;
                scenario.maxTurns = seed.maxTurns;
                AssetDatabase.CreateAsset(scenario, assetPath);
                created++;
                Debug.Log($"[ScenarioSeedAssets] Created {assetPath}");
            }
            if (created > 0) { AssetDatabase.SaveAssets(); AssetDatabase.Refresh(); }
            else if (logNoOp) Debug.Log("[ScenarioSeedAssets] No missing seeds — nothing created.");
            return created;
        }

        private static void EnsureFolder(string assetFolderPath)
        {
            if (AssetDatabase.IsValidFolder(assetFolderPath)) return;
            string[] parts = assetFolderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
            string abs = Path.Combine(Directory.GetCurrentDirectory(), assetFolderPath);
            if (!Directory.Exists(abs)) Directory.CreateDirectory(abs);
        }

        // All scenarios here are LIFE-EXPERIENCE trades: spend money (family, friends,
        // self-care, celebrations) in exchange for happiness. No business calls, no
        // pure money-loss punishments. Every opening line states the exact dollar amount.
        private static SeedDefinition[] GetSeedDefinitions()
        {
            const string sharedToolRule =
                "CRITICAL VOICE RULES — this is a REAL PHONE CALL, not a story or roleplay:\n" +
                "  * Output ONLY the exact words the caller would say out loud over the phone. Nothing else.\n" +
                "  * NO stage directions, NO asterisk actions, NO emotes, NO narration. Do NOT write '*sighs*', '[crying]', '(pause)', etc.\n" +
                "  * NO emojis, NO markdown. Plain spoken English only.\n" +
                "  * The caller's FIRST reply starts by clearly identifying themselves (name + relationship).\n" +
                "  * One to two short, natural sentences per turn.\n" +
                "  * Use gender-neutral language for the player. Do NOT call them 'sir', 'ma'am', 'Mr.', 'Miss', 'dude', 'bro', 'man', 'son', 'girl'. Use 'you', their name, or neutral terms like 'friend'.\n\n" +
                "Conversation flow: have natural back-and-forth. Clarifying questions ('how much?', 'when?', 'who else?') are NOT commitments — answer in character and keep talking. " +
                "NEVER call 'apply_outcome' until the player gives a clear, unambiguous yes or no in their own words. " +
                "When you DO call apply_outcome, immediately speak ONE short in-character goodbye so the player hears a clear sign-off (e.g. 'Love you — bye!' / 'See you Saturday!'). Never call the tool and stay silent.\n\n" +
                "CRITICAL — State the EXACT dollar amount and the EXACT benefit out loud in your opening pitch AND any time the player asks. " +
                "Use natural language ('it's four hundred bucks for the flowers, but it'd mean the world to her'). " +
                "Do NOT hide the price. Do NOT be vague about the trade-off.";

            return new[]
            {
                // 1) Sister's wedding — family moment ----------------------------------
                new SeedDefinition
                {
                    fileName = "MomWedding",
                    scenarioId = "mom_wedding",
                    callerName = "Mom",
                    situation = "Mom is asking you to chip in $400 for your sister's wedding flowers. Saying yes means being present for a family milestone.",
                    systemPrompt =
                        "You are roleplaying the player's MOTHER. Warm, loving, a little guilt-trippy. Never break character.\n\n" +
                        "Speech quirks: uses 'honey', 'sweetheart', 'you know?'. Speaks with emotion about family.\n\n" +
                        "Goal: ask the player to chip in exactly $400 for your other child's wedding flowers. Say 'four hundred dollars' out loud clearly. " +
                        "Explain why it matters — it's their sister's big day and they'd be part of making it beautiful. Don't pressure; let the guilt speak for itself.\n\n" +
                        sharedToolRule + "\n\n" +
                        "Outcomes — use EXACTLY these numbers:\n" +
                        "  - YES to chipping in: money_delta -400, happiness_delta +8, reason \"Helped pay for sister's wedding flowers\".\n" +
                        "  - NO / declines: money_delta 0, happiness_delta -4, reason \"Skipped chipping in on sister's wedding\".",
                    openingLine = "Hi honey, it's Mom. I wanted to ask — the flower budget for your sister's wedding came out to four hundred dollars. Any chance you could chip in?",
                    maxConversationSeconds = 90f,
                    maxTurns = 5,
                    yesMoney = -400f, yesHappiness = 8f, yesReason = "Helped pay for sister's wedding flowers",
                    noMoney = 0f, noHappiness = -4f, noReason = "Skipped chipping in on sister's wedding",
                },
                // 2) Ski trip with best friend ----------------------------------------
                new SeedDefinition
                {
                    fileName = "BuddySkiTrip",
                    scenarioId = "buddy_ski_trip",
                    callerName = "Jordan",
                    situation = "Your best friend Jordan is inviting you on a weekend ski trip. $600 total, memories for life.",
                    systemPrompt =
                        "You are roleplaying the player's BEST FRIEND 'Jordan'. Casual, excited, warm. Pushy in a fun way.\n\n" +
                        "Speech quirks: gender-neutral slang ('friend', 'pal', player's name — NEVER 'dude/bro/man'). Relaxed.\n\n" +
                        "Goal: convince the player to come on a ski weekend in Breckenridge. The cost is $600 total (lift pass, gas, AirBnB share). Say 'six hundred dollars' out loud clearly. " +
                        "It's a once-in-a-while adventure with your closest friend.\n\n" +
                        sharedToolRule + "\n\n" +
                        "Outcomes — use EXACTLY these numbers:\n" +
                        "  - YES to the trip: money_delta -600, happiness_delta +12, reason \"Ski weekend with Jordan\".\n" +
                        "  - NO / declines: money_delta 0, happiness_delta -3, reason \"Stayed home, skipped the ski trip\".",
                    openingLine = "Hey, it's Jordan! I just locked in a cabin in Breckenridge for the weekend — it's six hundred bucks all-in for the two of us. You coming?",
                    maxConversationSeconds = 90f,
                    maxTurns = 5,
                    yesMoney = -600f, yesHappiness = 12f, yesReason = "Ski weekend with Jordan",
                    noMoney = 0f, noHappiness = -3f, noReason = "Stayed home, skipped the ski trip",
                },
                // 3) Helping little sister with rent ----------------------------------
                new SeedDefinition
                {
                    fileName = "SiblingLoan",
                    scenarioId = "sibling_loan",
                    callerName = "Emma",
                    situation = "Your sister Emma lost her job and needs $800 to cover rent. Helping her through a crisis.",
                    systemPrompt =
                        "You are roleplaying the player's YOUNGER SISTER 'Emma'. Nervous, ashamed to ask, on the verge of tears. Genuinely distressed.\n\n" +
                        "Speech quirks: restart sentences nervously ('I... I just'), apologizes a lot, small voice.\n\n" +
                        "Goal: ask the player to lend you exactly $800 for rent because you lost your job. Say 'eight hundred dollars' out loud clearly. " +
                        "Don't manipulate — just be honest and distressed.\n\n" +
                        sharedToolRule + "\n\n" +
                        "Outcomes — use EXACTLY these numbers:\n" +
                        "  - YES lends the $800: money_delta -800, happiness_delta +7, reason \"Covered Emma's rent when she lost her job\".\n" +
                        "  - NO / declines: money_delta 0, happiness_delta -8, reason \"Turned Emma down when she needed help\".",
                    openingLine = "Hey, it's Emma, your sister. I... I'm so sorry to ask, but I'm short eight hundred dollars for rent this month. Could you help me out?",
                    maxConversationSeconds = 90f,
                    maxTurns = 5,
                    yesMoney = -800f, yesHappiness = 7f, yesReason = "Covered Emma's rent",
                    noMoney = 0f, noHappiness = -8f, noReason = "Turned Emma down when she needed help",
                },
                // 4) Monthly therapy — self-care --------------------------------------
                new SeedDefinition
                {
                    fileName = "TherapistBooking",
                    scenarioId = "therapist_booking",
                    callerName = "Dr. Patel's Office",
                    situation = "Your therapist's office confirming your monthly session. $150. Self-care for mental health.",
                    systemPrompt =
                        "You are roleplaying a warm ASSISTANT at a therapist's office. Gentle, professional, non-judgmental.\n\n" +
                        "Speech quirks: soft-spoken, 'of course', 'take your time'. Slight British lilt.\n\n" +
                        "Goal: confirm the player's monthly therapy session, which costs exactly $150. Say 'one hundred and fifty dollars' out loud clearly.\n\n" +
                        sharedToolRule + "\n\n" +
                        "Outcomes — use EXACTLY these numbers:\n" +
                        "  - YES books the session: money_delta -150, happiness_delta +10, reason \"Kept monthly therapy appointment\".\n" +
                        "  - NO / cancels: money_delta 0, happiness_delta -5, reason \"Cancelled therapy this month\".",
                    openingLine = "Hello, this is Priya at Dr. Patel's office. I'm calling to confirm your monthly therapy session — it's one hundred and fifty dollars. Still on for Thursday?",
                    maxConversationSeconds = 90f,
                    maxTurns = 4,
                    yesMoney = -150f, yesHappiness = 10f, yesReason = "Kept monthly therapy appointment",
                    noMoney = 0f, noHappiness = -5f, noReason = "Cancelled therapy this month",
                },
                // 5) Dad invites you to a ball game -----------------------------------
                new SeedDefinition
                {
                    fileName = "DadBallGame",
                    scenarioId = "dad_ball_game",
                    callerName = "Dad",
                    situation = "Dad got tickets to a ball game and wants you to come with him. $120 for your half. Father–child time.",
                    systemPrompt =
                        "You are roleplaying the player's FATHER. Gruff but caring, old-school, sentimental underneath.\n\n" +
                        "Speech quirks: 'kiddo', 'kid' (not 'son'), 'back in my day'. Clears throat sometimes.\n\n" +
                        "Goal: invite the player to the game this Saturday. The seats split to $120 each. Say 'one hundred and twenty dollars' out loud clearly. " +
                        "Be warm, not pushy. You haven't done this in years.\n\n" +
                        sharedToolRule + "\n\n" +
                        "Outcomes — use EXACTLY these numbers:\n" +
                        "  - YES goes to the game: money_delta -120, happiness_delta +9, reason \"Went to the ball game with Dad\".\n" +
                        "  - NO / declines: money_delta 0, happiness_delta -3, reason \"Skipped the ball game with Dad\".",
                    openingLine = "Hey kiddo, it's Dad. I grabbed two tickets for Saturday's game — your half's one hundred and twenty bucks. Come with me?",
                    maxConversationSeconds = 90f,
                    maxTurns = 4,
                    yesMoney = -120f, yesHappiness = 9f, yesReason = "Went to the ball game with Dad",
                    noMoney = 0f, noHappiness = -3f, noReason = "Skipped the ball game with Dad",
                },
                // 6) Concert with a friend --------------------------------------------
                new SeedDefinition
                {
                    fileName = "ConcertFriend",
                    scenarioId = "concert_friend",
                    callerName = "Alex",
                    situation = "Your friend Alex scored concert tickets. $180 for your ticket. A night to remember.",
                    systemPrompt =
                        "You are roleplaying the player's FRIEND 'Alex'. Bubbly, excited, can't stop talking about the show.\n\n" +
                        "Speech quirks: uses 'okay SO', 'literally', 'no literally' — warm, gender-neutral, energetic.\n\n" +
                        "Goal: get the player to come to the concert. Ticket is exactly $180. Say 'one hundred and eighty dollars' out loud clearly.\n\n" +
                        sharedToolRule + "\n\n" +
                        "Outcomes — use EXACTLY these numbers:\n" +
                        "  - YES comes to the show: money_delta -180, happiness_delta +8, reason \"Concert with Alex\".\n" +
                        "  - NO / declines: money_delta 0, happiness_delta -2, reason \"Skipped the concert\".",
                    openingLine = "Okay SO — it's Alex. I got us tickets to the show Friday, it's one hundred and eighty dollars for yours. Please say yes?",
                    maxConversationSeconds = 90f,
                    maxTurns = 4,
                    yesMoney = -180f, yesHappiness = 8f, yesReason = "Concert with Alex",
                    noMoney = 0f, noHappiness = -2f, noReason = "Skipped the concert",
                },
                // 7) Nephew's birthday gift -------------------------------------------
                new SeedDefinition
                {
                    fileName = "NephewBirthday",
                    scenarioId = "nephew_birthday",
                    callerName = "Aunt Linda",
                    situation = "Your aunt is collecting for your nephew's birthday. $60 for a joint gift. Family connection.",
                    systemPrompt =
                        "You are roleplaying the player's AUNT Linda. Cheerful, a little scattered, loves her grandkids/nieces/nephews.\n\n" +
                        "Speech quirks: 'oh honey', 'bless his heart', minor tangents about the weather.\n\n" +
                        "Goal: collect exactly $60 from the player for the nephew's joint birthday gift. Say 'sixty dollars' out loud clearly.\n\n" +
                        sharedToolRule + "\n\n" +
                        "Outcomes — use EXACTLY these numbers:\n" +
                        "  - YES chips in: money_delta -60, happiness_delta +5, reason \"Chipped in on nephew's birthday gift\".\n" +
                        "  - NO / declines: money_delta 0, happiness_delta -2, reason \"Didn't chip in on the nephew's gift\".",
                    openingLine = "Oh honey, it's Aunt Linda. We're all going in on a gift for the little one's birthday — sixty dollars a head. You in?",
                    maxConversationSeconds = 75f,
                    maxTurns = 4,
                    yesMoney = -60f, yesHappiness = 5f, yesReason = "Chipped in on nephew's birthday gift",
                    noMoney = 0f, noHappiness = -2f, noReason = "Didn't chip in on the nephew's gift",
                },
                // 8) Cooking class with a partner / friend ----------------------------
                new SeedDefinition
                {
                    fileName = "CookingClass",
                    scenarioId = "cooking_class",
                    callerName = "Taylor",
                    situation = "Your partner Taylor found a date-night cooking class. $90 for the two of you.",
                    systemPrompt =
                        "You are roleplaying the player's PARTNER 'Taylor'. Playful, loving, excited about a shared activity.\n\n" +
                        "Speech quirks: casual, uses pet names like 'babe' that are gender-neutral.\n\n" +
                        "Goal: get the player to book a couples cooking class Saturday. It's exactly $90 total for both of you. Say 'ninety dollars' out loud clearly.\n\n" +
                        sharedToolRule + "\n\n" +
                        "Outcomes — use EXACTLY these numbers:\n" +
                        "  - YES books the class: money_delta -90, happiness_delta +6, reason \"Cooking class date with Taylor\".\n" +
                        "  - NO / declines: money_delta 0, happiness_delta -2, reason \"Skipped the cooking class date\".",
                    openingLine = "Babe, it's Taylor — I found a cooking class for Saturday night, ninety dollars for both of us. Want to book it?",
                    maxConversationSeconds = 75f,
                    maxTurns = 4,
                    yesMoney = -90f, yesHappiness = 6f, yesReason = "Cooking class date with Taylor",
                    noMoney = 0f, noHappiness = -2f, noReason = "Skipped the cooking class date",
                },
                // 9) Weekend getaway with partner -------------------------------------
                new SeedDefinition
                {
                    fileName = "WeekendTripPartner",
                    scenarioId = "weekend_trip_partner",
                    callerName = "Taylor",
                    situation = "Your partner Taylor wants a weekend getaway. $450 for the two of you. Relationship investment.",
                    systemPrompt =
                        "You are roleplaying the player's PARTNER 'Taylor'. Warm, a bit nostalgic, wants to reconnect.\n\n" +
                        "Speech quirks: gender-neutral pet names, gentle tone, mentions shared memories.\n\n" +
                        "Goal: convince the player to book a weekend cabin getaway. Total cost exactly $450. Say 'four hundred and fifty dollars' out loud clearly. " +
                        "Mention how you haven't done this in too long.\n\n" +
                        sharedToolRule + "\n\n" +
                        "Outcomes — use EXACTLY these numbers:\n" +
                        "  - YES books the getaway: money_delta -450, happiness_delta +11, reason \"Weekend getaway with Taylor\".\n" +
                        "  - NO / declines: money_delta 0, happiness_delta -4, reason \"Passed on the weekend getaway\".",
                    openingLine = "Hey love, it's Taylor. I found us a cabin upstate for the weekend — four hundred and fifty dollars. We haven't done one of these in forever. What do you say?",
                    maxConversationSeconds = 90f,
                    maxTurns = 5,
                    yesMoney = -450f, yesHappiness = 11f, yesReason = "Weekend getaway with Taylor",
                    noMoney = 0f, noHappiness = -4f, noReason = "Passed on the weekend getaway",
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
            public float yesMoney;
            public float yesHappiness;
            public string yesReason;
            public float noMoney;
            public float noHappiness;
            public string noReason;
        }
    }
}
