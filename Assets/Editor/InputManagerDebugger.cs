using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

namespace HackKU.EditorTools
{
    public static class InputManagerDebugger
    {
        [MenuItem("HackKU/Debug/Dump InputActionManager")]
        public static void Dump()
        {
            var rig = GameObject.Find("XR Origin (XR Rig)");
            if (rig == null) { Debug.LogError("no XR Origin"); return; }
            var iam = rig.GetComponent<InputActionManager>();
            var sb = new StringBuilder();
            sb.AppendLine("InputActionManager present: " + (iam != null));
            if (iam != null)
            {
                sb.AppendLine("actionAssets count: " + (iam.actionAssets == null ? -1 : iam.actionAssets.Count));
                if (iam.actionAssets != null)
                    foreach (var a in iam.actionAssets)
                        sb.AppendLine("  - " + (a == null ? "NULL" : a.name));
            }
            File.WriteAllText("Assets/iam_dump.txt", sb.ToString());
            AssetDatabase.Refresh();
            Debug.Log("[InputManagerDebugger] wrote Assets/iam_dump.txt");
        }
    }
}
