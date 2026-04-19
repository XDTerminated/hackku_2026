# hackku_2026

A VR debt-payoff simulator built for HackKU 2026. You live in a low-poly house, get phone calls from pushy salespeople / family / collectors, order food and pay bills over a rotary phone, and try to pay off student loans before hunger, hygiene, or happiness tank.

Every phone conversation is voice-driven end-to-end: **your microphone → Groq Whisper (STT) → Groq Llama 3.1 (LLM) → ElevenLabs (TTS) → NPC voice in your ear**. Each NPC has an authored personality and pre-authored yes/no stat outcomes, so the LLM can drive the conversation without being trusted to decide how much money you lose.

Built for Meta Quest over OpenXR; also runs in the Editor with the XR Device Simulator.

---

## Tech stack

- **Engine:** Unity `6000.0.73f1` with URP `17.0.4`
- **VR:** OpenXR `1.17` + Meta XR `2.5`, XR Interaction Toolkit `3.4.1`, XR Hands `1.7.3`
- **AI:** Groq (chat + Whisper), ElevenLabs (TTS)
- **Backend:** Node + Express + `@neondatabase/serverless` → Neon Postgres (leaderboard)
- **Platform:** Windows Editor / Meta Quest standalone

---

## Project layout

```
Assets/
  Audio/                  in-game SFX
  CompositionLayers/      XR composition layer runtime settings
  Data/                   ScriptableObjects — Characters, Voices, Scenarios, Deliveries, Events
  Editor/                 custom editor tooling (builders, seeders, setup wizards)
  Materials/              URP Lit materials (House, Exterior, Yard, Foods, Textures)
  Models/                 FBX meshes (House, DeliveryTruck, Foods)
  Resources/              runtime-loaded configs (ElevenLabsConfig, GhostMat)
  Samples/                XRI + XR Hands package samples (don't move — Unity owns this path)
  Scenes/                 SampleScene.unity is the single gameplay scene
  Scripts/                game code, one folder per asmdef
  Settings/               URP pipeline assets, Volume profiles, Input Actions
  Shaders/                custom UIOverlay shader
  TextMesh Pro/           TMP essentials
  XR/                     XR Plugin Management settings
  XRI/                    XR Interaction Toolkit runtime settings
Packages/                 manifest.json
ProjectSettings/          Unity project settings
server/                   Express + Neon leaderboard service
docs/                     developer notes and scene dumps
screenshots/              dev screenshots (not imported by Unity)
```

## Assemblies

| Assembly | Path | Purpose |
|---|---|---|
| `HackKU.TTS` | `Assets/Scripts/TTS/` | ElevenLabs client, env loader, NPCVoice component, PCM WAV builder |
| `HackKU.AI` | `Assets/Scripts/AI/` | Groq client + Whisper, CallController, CallDirector, FoodOrderController, microphone + VAD |
| `HackKU.Core` | `Assets/Scripts/Core/` | Stats / hunger / hygiene / time / investment / finance, phone + delivery + food + ghost-furniture gameplay, wrist UI |
| `HackKU.Game` | `Assets/Scripts/Game/` | Game bootstrap + character-select card fan |
| `HackKU.Leaderboard` | `Assets/Scripts/Leaderboard/` | Throttled HTTP client for the Neon leaderboard |

Dependency flow: `HackKU.TTS` ← `HackKU.AI` ← `HackKU.Core` ← `HackKU.Game`. `HackKU.Leaderboard` depends on `HackKU.TTS` only (shares the env loader).

---

## Getting started

### Prerequisites

- Unity Hub + Unity `6000.0.73f1`
- Node 18+ (for the leaderboard server)
- A Groq API key ([groq.com](https://groq.com))
- An ElevenLabs API key ([elevenlabs.io](https://elevenlabs.io))
- (Optional) A Neon Postgres database ([neon.tech](https://neon.tech))
- (Optional) Meta Quest + Link cable or Air Link for on-device testing

### 1. Clone and configure env

```bash
git clone https://github.com/XDTerminated/hackku_2026
cd hackku_2026
cp .env.example .env
```

Fill in `.env` at the repo root:

```
ELEVENLABS_API_KEY=sk_...
GROQ_API_KEY=gsk_...
LEADERBOARD_WRITE_KEY=pick-any-long-random-string
```

The Unity client loads these at runtime via `HackKU.TTS.EnvLoader` — in the Editor it reads the repo-root `.env`, in builds it falls back to `StreamingAssets/.env`.

### 2. Open the project

1. Unity Hub → Add → pick the `hackku_2026` folder → open.
2. First import takes several minutes while Unity rebuilds the Library cache.
3. Open `Assets/Scenes/SampleScene.unity`.

### 3. (Optional) Spin up the leaderboard server

Only needed if you want player ranks to persist between sessions. See [`server/README.md`](server/README.md) for Neon setup and deploy options.

```bash
cd server
npm install
cp .env.example .env  # fill DATABASE_URL + WRITE_KEY (must match root .env's LEADERBOARD_WRITE_KEY)
npm start
```

Point Unity at it via `Assets/Resources/LeaderboardConfig.asset` → set `baseUrl` to `http://localhost:3000` (or your deployed URL).

### 4. Run

Hit Play in the Editor. The XR Device Simulator works without a headset (WASD + mouse for the head, left/right Ctrl+hand keys for controllers). With a Quest connected via Link, it should just work over OpenXR.

---

## How it plays

1. You pick a character from a fan of cards floating in front of you — each sets starting money, happiness, and student-loan debt.
2. Time ticks in real-time (1 in-game year per ~45 real seconds). Paychecks arrive twice a year. Hunger and hygiene decay continuously.
3. The **rotary phone** is the main interaction point:
   - **Incoming calls** — pick up the handset, talk to the NPC. Your words are transcribed by Groq Whisper, the LLM decides whether your answer is a commitment, and pre-authored stat deltas get applied when you say yes or no.
   - **Outgoing calls** — pick up the handset when it isn't ringing, dial-tone plays, say what you want ("I'll take a pizza", "transfer $500 to my loan", "invest $200"). The intent is parsed, money is deducted, and the delivery truck spawns at the front door.
4. Walk around, grab food from the grocery box that drops at your doorstep, bring it to your head to eat (refills hunger). Stand in the shower zone if you own the shower. Aim at ghost furniture and hold trigger to purchase permanent happiness-multiplier boosts.
5. **Win:** pay off your debt. **Lose:** money stays below $0 for 45 seconds, or happiness below 20 for 45 seconds.

---

## Controls (Meta Quest / XR Device Simulator)

- **Move:** left stick
- **Smooth turn:** right stick X
- **Grab:** grip (both hands)
- **Interact / buy ghost furniture:** right trigger (hold)
- **End your speech turn manually:** right A button (also editor `Space`)
- **Buy ghost (keyboard shortcut):** `B`

Voice activity detection handles most speech endings automatically — the button is a fallback.

---

## Runtime requirements

Before pressing Play, verify the scene has exactly one of each:
- `StatsManager`, `TimeManager`, `HungerManager`, `HygieneManager`, `FinanceScheduler`, `InvestmentManager`
- `ToastHUD`
- `RotaryPhone` with a `HandsetController` child
- `CallController` + `CallDirector` + `FoodOrderController`
- `MicrophoneCapture` + `VoiceActivityDetector`
- `GameBootstrap` + `CharacterSelector`
- An XR Origin rig with `XR Origin (XR Rig)` component

The wrist canvas (`WristCanvas`) should be parented to the right-hand controller with `WristWatchUI`, `WristBillboardFace`, `WristVisibilityController`, `SessionTimerUI`, `DeliveryTimerUI`.

---

## Configs (ScriptableObjects)

- `Assets/Resources/ElevenLabsConfig.asset` — voice model defaults. Menu: `HackKU/TTS/Create Default Config Asset`.
- `Assets/Resources/LeaderboardConfig.asset` — leaderboard base URL + write-key env var name. Menu: `HackKU/Leaderboard/Create Default Config Asset`.
- `Assets/Data/Characters/*.asset` — `CharacterProfile` starting stats + portraits.
- `Assets/Data/Voices/*.asset` — `NPCVoiceProfile` per voice (voice_id + TTS settings).
- `Assets/Data/Events/*.asset` — `CallScenario` authored incoming-call blueprints.
- `Assets/Data/Deliveries/*.asset`, `Foods/*.asset` — orderable items.

---

## Scripts worth knowing

| File | What it does |
|---|---|
| `Assets/Scripts/AI/CallController.cs` | Incoming-call state machine: ring → answer → listen → Whisper → Groq chat → apply outcome → goodbye |
| `Assets/Scripts/AI/FoodOrderController.cs` | Dial-out flow: intent routing → buy food / pay loan / invest / withdraw |
| `Assets/Scripts/AI/MicrophoneCapture.cs` | Persistent 16 kHz mic, WAV slicing per turn |
| `Assets/Scripts/Core/StatsManager.cs` | Canonical stats state, event emitter, win/lose detection |
| `Assets/Scripts/Core/TimeManager.cs` | Game clock, `OnYearTick` |
| `Assets/Scripts/Core/FinanceScheduler.cs` | Monthly heartbeat: paychecks, debt interest, happiness drift |
| `Assets/Scripts/Core/GhostFurnitureItem.cs` | Hold-to-buy furniture upgrades |
| `Assets/Scripts/Core/RotaryPhone.cs` + `HandsetController.cs` | Grab-and-dock phone handset with XR grab interactable |
| `Assets/Scripts/Core/WristWatchUI.cs` | Wrist HUD: year/money/happiness/hunger/hygiene/debt/invested |

---

## Performance notes

The project was profiled and tuned in April 2026; see `docs/` for the pass notes. Summary:

- Mic pre-warm on Play is disabled by default (`MicrophoneCapture.preWarmOnStart = false`) — the mic opens on the first call instead.
- All wrist-HUD text writes are guarded by last-value comparisons to avoid per-frame TMP mesh rebuilds.
- Billboard scripts cache `Camera.main` at start rather than per-frame.
- `GhostPurchaseHand` physics queries are throttled to every 3rd frame.
- URP Mobile pipeline targets Quest: HDR off, MSAA 4x, render scale 1.0, Bloom disabled.

If you notice new lag, open Profiler → Deep Profile and watch `WristWatchUI.Update`, `ObjectiveMarker.LateUpdate`, `GhostPurchaseHand.Update`.

---

## Troubleshooting

**"No API key configured"** — confirm `.env` sits at the repo root and contains `GROQ_API_KEY` / `ELEVENLABS_API_KEY`. The Editor reads the repo-root file; builds read `StreamingAssets/.env`.

**Microphone returns empty WAV** — check Windows Privacy → Microphone → allow desktop apps, and that Unity appears in the list. On Quest, confirm the mic permission popped.

**Leaderboard always errors** — make sure the server is actually running (`curl http://localhost:3000/api/health`) and `LeaderboardConfig.baseUrl` matches.

**NPC voice sounds wrong or missing** — the `NPCVoiceProfile` referenced by the `CallScenario` must have a valid `voice_id`. ElevenLabs voice IDs are per-account.

**Phone doesn't ring** — `CallDirector` waits for character selection before arming its timer. Pick a character first.

**Scene references broken after pulling** — Unity tracks assets by GUID in `.meta` files. If someone moved a file without its `.meta`, references break. Always `git pull` both the file and its meta sibling.

---

## Layout conventions

- One folder per asmdef under `Assets/Scripts/`. The folder name matches the assembly name.
- ScriptableObject data lives under `Assets/Data/` (per subfolder by type).
- Runtime-loaded configs go in `Assets/Resources/` — kept minimal because `Resources/` forces a single serialized bundle.
- Don't move or rename anything under `Assets/Samples/`, `Assets/TextMesh Pro/`, or `Assets/XR/` — those paths are owned by Package Manager.
- `.env` is gitignored; `.env.example` is committed.

---

## Credits

- Built by the HackKU 2026 team.
- Voice synthesis: [ElevenLabs](https://elevenlabs.io).
- LLM + speech-to-text: [Groq](https://groq.com).
- Database: [Neon](https://neon.tech).
- 3D house: `LowPolyHouseInterior.fbx` (see model license).
- Unity MCP: [CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp).
