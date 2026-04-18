using HackKU.TTS;
using UnityEditor;
using UnityEngine;

namespace HackKU.EditorTools
{
    public static class NpcVoiceParentCheck
    {
        [MenuItem("HackKU/Debug/NPCVoice Parent")]
        public static void Run()
        {
            var v = Object.FindFirstObjectByType<NPCVoice>();
            if (v == null) { Debug.Log("[NpcVoiceParentCheck] none"); return; }
            var parent = v.transform.parent;
            Debug.Log("[NpcVoiceParentCheck] parent=" + (parent != null ? parent.name : "(root)") + " localPos=" + v.transform.localPosition);
        }
    }
}
