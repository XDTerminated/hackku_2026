using System.Text;
using UnityEditor;
using UnityEngine;

namespace HackKU.EditorTools
{
    public static class RigInspector
    {
        [MenuItem("HackKU/Debug/Dump XR Rig")]
        public static void Dump()
        {
            var rig = GameObject.Find("XR Origin (XR Rig)");
            if (rig == null) { Debug.LogError("[RigInspector] no XR Origin"); return; }
            var sb = new StringBuilder();
            sb.AppendLine("[RigInspector]");
            Walk(rig.transform, sb, 0);
            var path = "Assets/xr_rig.txt";
            System.IO.File.WriteAllText(path, sb.ToString());
            AssetDatabase.Refresh();
            Debug.Log("[RigInspector] wrote " + path);
        }

        static void Walk(Transform t, StringBuilder sb, int d)
        {
            sb.Append(new string(' ', d * 2));
            sb.Append(t.name);
            foreach (var c in t.GetComponents<Component>())
            {
                if (c == null || c is Transform) continue;
                sb.Append(" [").Append(c.GetType().Name).Append("]");
            }
            sb.AppendLine();
            for (int i = 0; i < t.childCount; i++) Walk(t.GetChild(i), sb, d + 1);
        }
    }
}
