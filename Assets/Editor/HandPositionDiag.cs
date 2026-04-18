using System.Text;
using UnityEditor;
using UnityEngine;

namespace HackKU.EditorTools
{
    public static class HandPositionDiag
    {
        [MenuItem("HackKU/Debug/Dump Hand Transforms")]
        public static void Dump()
        {
            var sb = new StringBuilder();
            LogTransform(sb, GameObject.Find("XR Origin (XR Rig)"));
            LogTransform(sb, GameObject.Find("Camera Offset"));
            LogTransform(sb, GameObject.Find("Main Camera"));
            LogTransform(sb, GameObject.Find("Left Controller"));
            LogTransform(sb, GameObject.Find("Right Controller"));
            LogTransform(sb, GameObject.Find("Left Controller Visual"));
            LogTransform(sb, GameObject.Find("Right Controller Visual"));
            LogTransform(sb, GameObject.Find("WristCanvas"));
            LogTransform(sb, GameObject.Find("ToastHUD"));
            System.IO.File.WriteAllText("Assets/hand_transforms.txt", sb.ToString());
            AssetDatabase.Refresh();
            Debug.Log("[HandPositionDiag] wrote hand_transforms.txt");
        }

        static void LogTransform(StringBuilder sb, GameObject go)
        {
            if (go == null) { sb.AppendLine("(null)"); return; }
            var t = go.transform;
            string parent = t.parent != null ? t.parent.name : "(root)";
            sb.AppendLine(go.name + "  parent=" + parent);
            sb.AppendLine("  localPos=" + t.localPosition.ToString("F3") + "  localRot=" + t.localEulerAngles.ToString("F1") + "  localScale=" + t.localScale.ToString("F3"));
            sb.AppendLine("  worldPos=" + t.position.ToString("F3") + "  worldRot=" + t.eulerAngles.ToString("F1"));
        }
    }
}
