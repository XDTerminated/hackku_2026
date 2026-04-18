using System.IO;
using HackKU.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace HackKU.EditorTools
{
    public static class SixthPolish
    {
        const string HandsetPrefabPath = "Assets/Data/Prefabs/Handset.prefab";
        const string PhonePrefabPath = "Assets/Data/Prefabs/RotaryPhone.prefab";
        const string RingClipPath = "Assets/Data/Prefabs/PhoneRing.wav";

        [MenuItem("HackKU/Fix/Sixth Polish (Ring Tone + Handset)")]
        public static void Run()
        {
            RemoveProximityGate();
            var clip = EnsureRingClip();
            WireRingIntoPhones(clip);
            AddGlowLightToPhones();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("[SixthPolish] done.");
        }

        static void RemoveProximityGate()
        {
            RemoveGateFromPrefab(HandsetPrefabPath);
            RemoveGateFromPrefab(PhonePrefabPath);
            foreach (var gate in Object.FindObjectsByType<ProximityGrabGate>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(gate);
            }
        }

        static void RemoveGateFromPrefab(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                foreach (var gate in instance.GetComponentsInChildren<ProximityGrabGate>(true))
                {
                    var grab = gate.GetComponent<XRGrabInteractable>();
                    if (grab != null) grab.enabled = true;
                    Object.DestroyImmediate(gate);
                }
                PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);
            }
            finally { Object.DestroyImmediate(instance); }
        }

        static AudioClip EnsureRingClip()
        {
            if (!Directory.Exists("Assets/Data/Prefabs")) Directory.CreateDirectory("Assets/Data/Prefabs");
            var wavBytes = GenerateRingToneWav();
            File.WriteAllBytes(RingClipPath, wavBytes);
            AssetDatabase.ImportAsset(RingClipPath, ImportAssetOptions.ForceUpdate);
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(RingClipPath);
            Debug.Log("[SixthPolish] ring clip " + (clip != null ? "ready" : "FAILED") + " at " + RingClipPath);
            return clip;
        }

        // Generates a 2-second classic telephone ring: 20 Hz amplitude-modulated dual tone (440 + 480 Hz).
        // Pattern: 1s ring + 1s silence, x1 = 2 seconds total. Source loops it.
        static byte[] GenerateRingToneWav()
        {
            int sampleRate = 22050;
            float totalSeconds = 2.0f;
            int totalSamples = Mathf.RoundToInt(sampleRate * totalSeconds);

            var samples = new float[totalSamples];
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                bool ringing = t < 1.0f; // first second rings, second second is silence
                if (!ringing)
                {
                    samples[i] = 0f;
                    continue;
                }
                // Two classic ring frequencies + 20Hz warble to make it sound like a phone.
                float a = Mathf.Sin(2f * Mathf.PI * 440f * t);
                float b = Mathf.Sin(2f * Mathf.PI * 480f * t);
                float warble = 0.5f * (1f + Mathf.Sin(2f * Mathf.PI * 20f * t));
                float envelope = Mathf.SmoothStep(0f, 1f, Mathf.Min(t * 10f, (1f - t) * 10f, 1f));
                samples[i] = 0.4f * (a + b) * warble * envelope;
            }

            return EncodeWav16(samples, sampleRate, 1);
        }

        static byte[] EncodeWav16(float[] samples, int sampleRate, int channels)
        {
            int bytesPerSample = 2;
            int dataSize = samples.Length * bytesPerSample;
            int fileSize = 44 + dataSize;
            using var ms = new MemoryStream(fileSize);
            using var bw = new BinaryWriter(ms);
            bw.Write(new[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F' });
            bw.Write(fileSize - 8);
            bw.Write(new[] { (byte)'W', (byte)'A', (byte)'V', (byte)'E' });
            bw.Write(new[] { (byte)'f', (byte)'m', (byte)'t', (byte)' ' });
            bw.Write(16);
            bw.Write((short)1);
            bw.Write((short)channels);
            bw.Write(sampleRate);
            bw.Write(sampleRate * channels * bytesPerSample);
            bw.Write((short)(channels * bytesPerSample));
            bw.Write((short)(bytesPerSample * 8));
            bw.Write(new[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' });
            bw.Write(dataSize);
            for (int i = 0; i < samples.Length; i++)
            {
                float f = samples[i];
                if (f > 1f) f = 1f; else if (f < -1f) f = -1f;
                bw.Write((short)(f * short.MaxValue));
            }
            bw.Flush();
            return ms.ToArray();
        }

        static void WireRingIntoPhones(AudioClip clip)
        {
            if (clip == null) return;

            // Phone prefab.
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PhonePrefabPath);
            if (prefab != null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                try
                {
                    foreach (var phone in instance.GetComponentsInChildren<RotaryPhone>(true))
                    {
                        phone.ringClip = clip;
                    }
                    PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);
                }
                finally { Object.DestroyImmediate(instance); }
            }

            // Scene instances.
            foreach (var phone in Object.FindObjectsByType<RotaryPhone>(FindObjectsSortMode.None))
            {
                phone.ringClip = clip;
                EditorUtility.SetDirty(phone);
            }
        }

        static void AddGlowLightToPhones()
        {
            foreach (var phone in Object.FindObjectsByType<RotaryPhone>(FindObjectsSortMode.None))
            {
                if (phone.glowLight != null) continue;
                var lightGo = new GameObject("PhoneRingGlow");
                lightGo.transform.SetParent(phone.transform, false);
                lightGo.transform.localPosition = new Vector3(0f, 0.15f, 0f);
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, 0.85f, 0.3f);
                light.range = 1.8f;
                light.intensity = 2f;
                light.enabled = false;
                phone.glowLight = light;
                EditorUtility.SetDirty(phone);
                Debug.Log("[SixthPolish] added PhoneRingGlow light on " + phone.gameObject.name);
            }
        }
    }
}
