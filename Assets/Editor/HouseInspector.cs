using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace HackKU.EditorTools
{
    public static class HouseInspector
    {
        [MenuItem("HackKU/Debug/Dump House Hierarchy")]
        public static void Dump()
        {
            var house = GameObject.Find("House");
            if (house == null) { Debug.LogError("[HouseInspector] no House"); return; }
            var sb = new StringBuilder();
            sb.AppendLine("[HouseInspector] dump:");
            foreach (var t in house.GetComponentsInChildren<Transform>(true))
            {
                sb.Append(new string(' ', Depth(t, house.transform) * 2));
                sb.Append(t.name);
                sb.Append("  pos=").Append(t.position.ToString("F2"));
                var mr = t.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    var b = mr.bounds;
                    sb.Append("  bounds=").Append(b.size.ToString("F2"));
                }
                sb.AppendLine();
            }
            var path = "Assets/house_hierarchy.txt";
            System.IO.File.WriteAllText(path, sb.ToString());
            AssetDatabase.Refresh();
            Debug.Log("[HouseInspector] wrote " + sb.Length + " chars to " + path);
        }

        static int Depth(Transform t, Transform root)
        {
            int d = 0;
            while (t != null && t != root) { t = t.parent; d++; }
            return d;
        }
    }
}
