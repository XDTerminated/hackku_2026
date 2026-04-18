using System.Reflection;
using HackKU.AI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HackKU.EditorTools
{
    public static class TenthPolish
    {
        [MenuItem("HackKU/Fix/Tenth Polish (Voice Activity Detection)")]
        public static void Run()
        {
            var callSystem = GameObject.Find("CallSystem");
            if (callSystem == null) { Debug.LogError("[TenthPolish] no CallSystem"); return; }

            var cc = callSystem.GetComponent<CallController>();
            var mic = callSystem.GetComponent<MicrophoneCapture>();
            if (cc == null || mic == null) { Debug.LogError("[TenthPolish] missing CallController or MicrophoneCapture"); return; }

            // Remove push-to-talk.
            var pst = callSystem.GetComponent<PlayerSpeechTrigger>();
            if (pst != null)
            {
                Object.DestroyImmediate(pst);
                Debug.Log("[TenthPolish] removed PlayerSpeechTrigger (push-to-talk)");
            }

            // Add VoiceActivityDetector and wire inspector refs via reflection (private [SerializeField]).
            var vad = callSystem.GetComponent<VoiceActivityDetector>();
            if (vad == null) vad = callSystem.AddComponent<VoiceActivityDetector>();
            SetPrivate(vad, "mic", mic);
            SetPrivate(vad, "callController", cc);

            EditorUtility.SetDirty(vad);
            EditorSceneManager.MarkSceneDirty(callSystem.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[TenthPolish] VAD installed on " + callSystem.name);
        }

        static void SetPrivate(Object target, string fieldName, object value)
        {
            var f = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (f == null) { Debug.LogWarning("[TenthPolish] field not found: " + fieldName); return; }
            f.SetValue(target, value);
        }
    }
}
