using System.Text;
using UnityEditor;
using UnityEngine;

namespace HackKU.EditorTools
{
    public static class HouseInspectHierarchy
    {
        [MenuItem("HackKU/Debug/Dump House Renderers + Materials")]
        public static void Dump()
        {
            var house = GameObject.Find("House");
            if (house == null) { Debug.LogError("no house"); return; }
            var sb = new StringBuilder();
            Bounds? combined = null;
            foreach (var mr in house.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mf = mr.GetComponent<MeshFilter>();
                string meshName = mf != null && mf.sharedMesh != null ? mf.sharedMesh.name : "-";
                string matName = mr.sharedMaterial != null ? mr.sharedMaterial.name : "-";
                var b = mr.bounds;
                sb.AppendFormat("{0} | mesh={1} | mat={2} | size=({3:F2},{4:F2},{5:F2}) | center=({6:F2},{7:F2},{8:F2})\n",
                    mr.name, meshName, matName, b.size.x, b.size.y, b.size.z, b.center.x, b.center.y, b.center.z);
                if (combined == null) combined = b; else { var cc = combined.Value; cc.Encapsulate(b); combined = cc; }
            }
            if (combined.HasValue)
            {
                var c = combined.Value;
                sb.Insert(0, string.Format("House bounds center=({0:F2},{1:F2},{2:F2}) size=({3:F2},{4:F2},{5:F2})\n\n",
                    c.center.x, c.center.y, c.center.z, c.size.x, c.size.y, c.size.z));
            }
            System.IO.File.WriteAllText("Assets/house_dump.txt", sb.ToString());
            AssetDatabase.Refresh();
            Debug.Log("[HouseInspectHierarchy] wrote Assets/house_dump.txt (" + sb.Length + " chars)");
        }
    }
}
