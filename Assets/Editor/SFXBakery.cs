using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HackKU.EditorTools
{
    // Procedurally synthesizes short SFX and writes them as 16-bit mono WAVs to
    // Assets/Resources/SFX/ so SfxHub can load them via Resources.Load. Run via
    // menu `HackKU/Build/Bake SFX`. Overwrites existing files — if you swap in
    // real sound effects with the same file names they'll override these.
    public static class SFXBakery
    {
        const int SampleRate = 44100;
        const string OutDir = "Assets/Resources/SFX";

        [MenuItem("HackKU/Build/Bake SFX")]
        public static void BakeAll()
        {
            Directory.CreateDirectory(OutDir);

            WriteWav("door_open", BuildDoorOpen());
            WriteWav("door_close", BuildDoorClose());
            WriteWav("eat", BuildEat());
            WriteWav("pickup", BuildPickup());
            WriteWav("drop", BuildDrop());
            WriteWav("charge_start", BuildChargeStart());
            WriteWav("charge_cancel", BuildChargeCancel());
            WriteWav("phone_pickup", BuildPhonePickup());
            WriteWav("phone_hangup", BuildPhoneHangup());
            WriteWav("phone_wall_place", BuildPhoneWallPlace());
            WriteWav("footstep_1", BuildFootstep(1));
            WriteWav("footstep_2", BuildFootstep(2));
            WriteWav("footstep_3", BuildFootstep(3));
            WriteWav("ui_click", BuildUIClick());
            WriteWav("box_pickup", BuildBoxPickup());
            WriteWav("box_drop", BuildBoxDrop());
            WriteWav("van_engine", BuildVanEngine());
            WriteWav("cha_ching", BuildChaChing());
            WriteWav("ambient_pad", BuildAmbientPad());
            WriteWav("bill_drop", BuildBillDrop());
            WriteWav("bill_late", BuildBillLate());

            AssetDatabase.Refresh();
            Debug.Log("[SFXBakery] Baked SFX into " + OutDir);
        }

        // --- Sound designs -----------------------------------------------------------

        // Low woody creak rising slightly with a small opening thud at the head.
        static float[] BuildDoorOpen()
        {
            float dur = 0.55f;
            int n = (int)(dur * SampleRate);
            var buf = new float[n];
            var rng = new System.Random(42);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float env = Env(t, dur, 0.02f, 0.08f);
                float creakFreq = Mathf.Lerp(90f, 140f, t / dur);
                float creak = Mathf.Sin(2 * Mathf.PI * creakFreq * t);
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0) * 0.5f;
                float filtered = LowPass(noise, ref _lp1, 0.18f);
                float body = (creak * 0.55f + filtered * 0.45f);
                // Subtle wobble to feel like hinge resistance.
                body *= 1f + 0.15f * Mathf.Sin(2 * Mathf.PI * 7f * t);
                float thud = t < 0.05f ? Mathf.Sin(2 * Mathf.PI * 65f * t) * (1f - t / 0.05f) * 0.6f : 0f;
                buf[i] = (body * env + thud) * 0.55f;
            }
            _lp1 = 0f;
            return buf;
        }

        // Descending creak ending in a firm thud.
        static float[] BuildDoorClose()
        {
            float dur = 0.55f;
            int n = (int)(dur * SampleRate);
            var buf = new float[n];
            var rng = new System.Random(99);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float env = Env(t, dur, 0.03f, 0.12f);
                float creakFreq = Mathf.Lerp(140f, 85f, t / dur);
                float creak = Mathf.Sin(2 * Mathf.PI * creakFreq * t);
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0) * 0.45f;
                float filtered = LowPass(noise, ref _lp1, 0.16f);
                float body = (creak * 0.5f + filtered * 0.5f);
                body *= 1f + 0.1f * Mathf.Sin(2 * Mathf.PI * 9f * t);
                // Closing thud at the end.
                float thudT = t - (dur - 0.08f);
                float thud = (thudT > 0f && thudT < 0.12f)
                    ? Mathf.Sin(2 * Mathf.PI * 55f * thudT) * Mathf.Exp(-thudT * 22f) * 0.9f : 0f;
                buf[i] = (body * env * 0.45f + thud) * 0.6f;
            }
            _lp1 = 0f;
            return buf;
        }

        // Three quick munch/crunch bursts — bandpassed noise with chomp AM.
        static float[] BuildEat()
        {
            float dur = 0.42f;
            int n = (int)(dur * SampleRate);
            var buf = new float[n];
            var rng = new System.Random(17);
            float[] bursts = { 0.00f, 0.14f, 0.27f };
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float s = 0f;
                foreach (var b in bursts)
                {
                    float bt = t - b;
                    if (bt < 0f || bt > 0.11f) continue;
                    float benv = Mathf.Exp(-bt * 28f) * (bt < 0.005f ? bt / 0.005f : 1f);
                    float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                    float hp = HighPass(noise, ref _hp1, 0.25f);
                    float bp = LowPass(hp, ref _lp2, 0.35f);
                    s += bp * benv;
                }
                buf[i] = s * 0.7f;
            }
            _hp1 = _lp2 = 0f;
            return buf;
        }

        // Short rising chirp "pwip" — the universal "pick-up" cue.
        static float[] BuildPickup()
        {
            float dur = 0.14f;
            int n = (int)(dur * SampleRate);
            var buf = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float freq = Mathf.Lerp(320f, 880f, t / dur);
                float env = Mathf.Exp(-t * 9f) * (t < 0.01f ? t / 0.01f : 1f);
                float tone = Mathf.Sin(2 * Mathf.PI * freq * t);
                float harmonic = Mathf.Sin(2 * Mathf.PI * freq * 2f * t) * 0.3f;
                buf[i] = (tone + harmonic) * env * 0.55f;
            }
            return buf;
        }

        // Soft low thump — small item hitting ground.
        static float[] BuildDrop()
        {
            float dur = 0.22f;
            int n = (int)(dur * SampleRate);
            var buf = new float[n];
            var rng = new System.Random(7);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t * 18f) * (t < 0.004f ? t / 0.004f : 1f);
                float low = Mathf.Sin(2 * Mathf.PI * 82f * t);
                float tick = (t < 0.02f ? (float)(rng.NextDouble() * 2.0 - 1.0) * 0.6f : 0f);
                float tickLP = LowPass(tick, ref _lp1, 0.35f);
                buf[i] = (low * 0.75f + tickLP) * env * 0.7f;
            }
            _lp1 = 0f;
            return buf;
        }

        // Rising hum — plays once while player holds-to-buy.
        static float[] BuildChargeStart()
        {
            float dur = 1.0f;
            int n = (int)(dur * SampleRate);
            var buf = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Min(1f, t / 0.05f) * (t > dur - 0.05f ? (dur - t) / 0.05f : 1f);
                float freq = Mathf.Lerp(260f, 880f, Mathf.SmoothStep(0f, 1f, t / dur));
                float vib = 0.02f * Mathf.Sin(2 * Mathf.PI * 5f * t);
                float tone = Mathf.Sin(2 * Mathf.PI * freq * t + vib);
                float harm = Mathf.Sin(2 * Mathf.PI * freq * 1.5f * t) * 0.25f;
                float swell = Mathf.Lerp(0.35f, 0.85f, t / dur);
                buf[i] = (tone + harm) * env * swell * 0.5f;
            }
            return buf;
        }

        // Descending "aw" — signals release before the buy completes.
        static float[] BuildChargeCancel()
        {
            float dur = 0.22f;
            int n = (int)(dur * SampleRate);
            var buf = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t * 12f) * (t < 0.005f ? t / 0.005f : 1f);
                float freq = Mathf.Lerp(720f, 210f, t / dur);
                float tone = Mathf.Sin(2 * Mathf.PI * freq * t);
                buf[i] = tone * env * 0.55f;
            }
            return buf;
        }

        // Click + soft "off-hook" blip.
        static float[] BuildPhonePickup()
        {
            float dur = 0.18f;
            int n = (int)(dur * SampleRate);
            var buf = new float[n];
            var rng = new System.Random(3);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float click = (t < 0.018f) ? (float)(rng.NextDouble() * 2.0 - 1.0) * (1f - t / 0.018f) * 0.9f : 0f;
                float clickFiltered = HighPass(click, ref _hp1, 0.35f);
                float blipT = t - 0.03f;
                float blip = 0f;
                if (blipT > 0f && blipT < 0.12f)
                {
                    float blipEnv = Mathf.Exp(-blipT * 14f);
                    float blipFreq = Mathf.Lerp(440f, 520f, blipT / 0.12f);
                    blip = Mathf.Sin(2 * Mathf.PI * blipFreq * blipT) * blipEnv * 0.5f;
                }
                buf[i] = clickFiltered + blip;
            }
            _hp1 = 0f;
            return buf;
        }

        // Click + two short disconnect beeps — "call ended".
        static float[] BuildPhoneHangup()
        {
            float dur = 0.5f;
            int n = (int)(dur * SampleRate);
            var buf = new float[n];
            var rng = new System.Random(11);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float click = (t < 0.015f) ? (float)(rng.NextDouble() * 2.0 - 1.0) * (1f - t / 0.015f) * 0.9f : 0f;
                float clickF = HighPass(click, ref _hp1, 0.35f);
                float b1 = Beep(t - 0.06f, 0.11f, 620f);
                float b2 = Beep(t - 0.25f, 0.11f, 480f);
                buf[i] = clickF + (b1 + b2) * 0.4f;
            }
            _hp1 = 0f;
            return buf;
        }

        // Solid clunk — handset docking onto wall mount.
        static float[] BuildPhoneWallPlace()
        {
            float dur = 0.28f;
            int n = (int)(dur * SampleRate);
            var buf = new float[n];
            var rng = new System.Random(23);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t * 14f) * (t < 0.003f ? t / 0.003f : 1f);
                float thump = Mathf.Sin(2 * Mathf.PI * 95f * t);
                float tickN = (t < 0.03f ? (float)(rng.NextDouble() * 2.0 - 1.0) : 0f);
                float tick = HighPass(tickN, ref _hp1, 0.4f) * 0.55f;
                buf[i] = (thump * 0.8f + tick) * env * 0.8f;
            }
            _hp1 = 0f;
            return buf;
        }

        // Footstep — short filtered-noise thud with a quick tick. Seed varies between calls.
        static float[] BuildFootstep(int seedMul)
        {
            float dur = 0.14f;
            int n = (int)(dur * SampleRate);
            var buf = new float[n];
            var rng = new System.Random(200 + seedMul * 37);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t * 22f) * (t < 0.003f ? t / 0.003f : 1f);
                float thump = Mathf.Sin(2 * Mathf.PI * (110f + seedMul * 8f) * t);
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                float filt = LowPass(noise, ref _lp1, 0.22f);
                buf[i] = (thump * 0.55f + filt * 0.6f) * env * 0.7f;
            }
            _lp1 = 0f;
            return buf;
        }

        // Short crisp tick — UI click.
        static float[] BuildUIClick()
        {
            float dur = 0.07f;
            int n = (int)(dur * SampleRate);
            var buf = new float[n];
            var rng = new System.Random(1234);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t * 65f) * (t < 0.001f ? t / 0.001f : 1f);
                float tone = Mathf.Sin(2 * Mathf.PI * 1800f * t) * 0.5f;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0) * 0.4f;
                float hp = HighPass(noise, ref _hp1, 0.5f);
                buf[i] = (tone + hp) * env * 0.6f;
            }
            _hp1 = 0f;
            return buf;
        }

        // Cardboard rustle + low thump.
        static float[] BuildBoxPickup()
        {
            float dur = 0.35f;
            int n = (int)(dur * SampleRate);
            var buf = new float[n];
            var rng = new System.Random(501);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float thumpEnv = Mathf.Exp(-t * 14f) * (t < 0.005f ? t / 0.005f : 1f);
                float thump = Mathf.Sin(2 * Mathf.PI * 90f * t) * thumpEnv * 0.6f;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                float hp = HighPass(noise, ref _hp1, 0.45f);
                float rustleEnv = Mathf.Exp(-Mathf.Abs(t - 0.1f) * 10f) * 0.35f;
                buf[i] = thump + hp * rustleEnv;
            }
            _hp1 = 0f;
            return buf;
        }

        // Heavier drop — double thud simulating cardboard flex + settle.
        static float[] BuildBoxDrop()
        {
            float dur = 0.45f;
            int n = (int)(dur * SampleRate);
            var buf = new float[n];
            var rng = new System.Random(777);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float e1 = Mathf.Exp(-t * 10f) * (t < 0.004f ? t / 0.004f : 1f);
                float thud1 = Mathf.Sin(2 * Mathf.PI * 70f * t) * e1 * 0.9f;
                float t2 = t - 0.11f;
                float thud2 = 0f;
                if (t2 > 0f && t2 < 0.18f)
                {
                    float e2 = Mathf.Exp(-t2 * 16f);
                    thud2 = Mathf.Sin(2 * Mathf.PI * 95f * t2) * e2 * 0.45f;
                }
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                float hp = HighPass(noise, ref _hp1, 0.4f);
                float rustleEnv = Mathf.Exp(-Mathf.Abs(t - 0.03f) * 18f) * 0.3f;
                buf[i] = (thud1 + thud2 + hp * rustleEnv) * 0.75f;
            }
            _hp1 = 0f;
            return buf;
        }

        // Looping low rumble — delivery truck engine. 2s loop, tight so it loops seamlessly.
        static float[] BuildVanEngine()
        {
            float dur = 2.0f;
            int n = (int)(dur * SampleRate);
            var buf = new float[n];
            var rng = new System.Random(404);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float baseFreq = 75f + 4f * Mathf.Sin(2 * Mathf.PI * 0.5f * t);
                float rumble = Mathf.Sin(2 * Mathf.PI * baseFreq * t);
                float second = Mathf.Sin(2 * Mathf.PI * baseFreq * 2f * t) * 0.35f;
                float fourth = Mathf.Sin(2 * Mathf.PI * baseFreq * 4f * t) * 0.15f;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                float filt = LowPass(noise, ref _lp1, 0.08f);
                float body = rumble * 0.6f + second + fourth + filt * 0.25f;
                // Loop-safe envelope: fade in first 0.08s, fade out last 0.08s.
                float env = 1f;
                if (t < 0.08f) env = t / 0.08f;
                else if (t > dur - 0.08f) env = (dur - t) / 0.08f;
                buf[i] = body * env * 0.35f;
            }
            _lp1 = 0f;
            return buf;
        }

        // Classic cha-ching — register bell two-tone ding ding.
        static float[] BuildChaChing()
        {
            float dur = 0.55f;
            int n = (int)(dur * SampleRate);
            var buf = new float[n];
            var rng = new System.Random(909);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                // Cash-drawer click at the very start.
                float click = (t < 0.02f) ? (float)(rng.NextDouble() * 2.0 - 1.0) * (1f - t / 0.02f) * 0.55f : 0f;
                float clickHP = HighPass(click, ref _hp1, 0.45f);
                // Two bright bells ding-ding.
                float bell1 = Bell(t - 0.04f, 0.25f, 1200f);
                float bell2 = Bell(t - 0.22f, 0.32f, 1600f);
                buf[i] = clickHP + bell1 * 0.7f + bell2 * 0.75f;
            }
            _hp1 = 0f;
            return buf;
        }

        // Slow ambient pad — stacked sine drones in a minor-ish key, gentle chord swells.
        // 16s loop, smoothly fades in/out at the seams.
        static float[] BuildAmbientPad()
        {
            float dur = 16f;
            int n = (int)(dur * SampleRate);
            var buf = new float[n];
            // Notes: low drone root A2 (110Hz), fifth E3 (164.81), octave A3 (220), third C4 (261.63).
            float[] freqs = { 110f, 164.81f, 220f, 261.63f };
            float[] amps = { 0.55f, 0.35f, 0.28f, 0.18f };
            float[] lfoRates = { 0.06f, 0.08f, 0.13f, 0.11f };
            float[] lfoDepths = { 0.25f, 0.4f, 0.35f, 0.5f };
            float[] phases = { 0f, 1.2f, 2.4f, 3.6f };
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float s = 0f;
                for (int k = 0; k < freqs.Length; k++)
                {
                    // Slow amplitude LFO so voices breathe in and out against each other.
                    float lfo = 0.5f + 0.5f * Mathf.Sin(2 * Mathf.PI * lfoRates[k] * t + phases[k]);
                    float amp = amps[k] * Mathf.Lerp(1f - lfoDepths[k], 1f, lfo);
                    float tone = Mathf.Sin(2 * Mathf.PI * freqs[k] * t);
                    // Tiny harmonic shimmer from detuned partial.
                    float shimmer = Mathf.Sin(2 * Mathf.PI * freqs[k] * 2.005f * t) * 0.15f;
                    s += (tone + shimmer) * amp;
                }
                // Loop-safe fade for seamless looping — fade first and last 0.4s.
                float env = 1f;
                if (t < 0.4f) env = t / 0.4f;
                else if (t > dur - 0.4f) env = (dur - t) / 0.4f;
                buf[i] = s * env * 0.22f;
            }
            return buf;
        }

        // Mail-slot paper flutter + soft chime — bill just dropped.
        static float[] BuildBillDrop()
        {
            float dur = 0.55f;
            int n = (int)(dur * SampleRate);
            var buf = new float[n];
            var rng = new System.Random(321);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float flutterEnv = Mathf.Exp(-Mathf.Abs(t - 0.08f) * 9f);
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                float hp = HighPass(noise, ref _hp1, 0.45f);
                float flutter = hp * flutterEnv * 0.45f;
                // Two-tone chime (G5 + B5).
                float c1 = Beep(t - 0.2f, 0.22f, 783.99f);
                float c2 = Beep(t - 0.28f, 0.25f, 987.77f);
                buf[i] = flutter + (c1 + c2) * 0.22f;
            }
            _hp1 = 0f;
            return buf;
        }

        // Urgent two-note beep — bill crossed a late-fee tier.
        static float[] BuildBillLate()
        {
            float dur = 0.4f;
            int n = (int)(dur * SampleRate);
            var buf = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float a = Beep(t - 0.02f, 0.14f, 540f);
                float b = Beep(t - 0.2f, 0.18f, 420f);
                buf[i] = (a + b) * 0.45f;
            }
            return buf;
        }

        static float Bell(float bt, float len, float freq)
        {
            if (bt < 0f || bt > len) return 0f;
            float env = Mathf.Exp(-bt * 8f) * (bt < 0.005f ? bt / 0.005f : 1f);
            float f1 = Mathf.Sin(2 * Mathf.PI * freq * bt);
            float f2 = Mathf.Sin(2 * Mathf.PI * freq * 2.01f * bt) * 0.4f;
            float f3 = Mathf.Sin(2 * Mathf.PI * freq * 3.03f * bt) * 0.2f;
            return (f1 + f2 + f3) * env * 0.35f;
        }

        // --- DSP helpers -------------------------------------------------------------

        static float _lp1, _lp2, _hp1;
        static float LowPass(float x, ref float state, float a) { state += a * (x - state); return state; }
        static float HighPass(float x, ref float state, float a) { state = a * (state + x - state); return x - state; }

        // Attack-release envelope with flat sustain between.
        static float Env(float t, float dur, float atk, float rel)
        {
            if (t < atk) return t / atk;
            if (t > dur - rel) return Mathf.Max(0f, (dur - t) / rel);
            return 1f;
        }

        static float Beep(float bt, float blen, float freq)
        {
            if (bt < 0f || bt > blen) return 0f;
            float env = Mathf.Min(1f, bt / 0.01f) * (bt > blen - 0.015f ? (blen - bt) / 0.015f : 1f);
            return Mathf.Sin(2 * Mathf.PI * freq * bt) * env;
        }

        // --- WAV writer --------------------------------------------------------------

        static void WriteWav(string name, float[] samples)
        {
            string path = OutDir + "/" + name + ".wav";
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var bw = new BinaryWriter(fs))
            {
                int byteLen = samples.Length * 2;
                bw.Write(new[] { 'R', 'I', 'F', 'F' });
                bw.Write(36 + byteLen);
                bw.Write(new[] { 'W', 'A', 'V', 'E' });
                bw.Write(new[] { 'f', 'm', 't', ' ' });
                bw.Write(16);
                bw.Write((short)1);           // PCM
                bw.Write((short)1);           // mono
                bw.Write(SampleRate);
                bw.Write(SampleRate * 2);     // byte rate
                bw.Write((short)2);           // block align
                bw.Write((short)16);          // bits
                bw.Write(new[] { 'd', 'a', 't', 'a' });
                bw.Write(byteLen);
                for (int i = 0; i < samples.Length; i++)
                {
                    short s = (short)Mathf.Clamp(Mathf.RoundToInt(samples[i] * 32760f), short.MinValue, short.MaxValue);
                    bw.Write(s);
                }
            }
        }
    }
}
