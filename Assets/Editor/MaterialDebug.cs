using System.Text;
using UnityEditor;
using UnityEngine;

namespace HackKU.EditorTools
{
    public static class MaterialDebug
    {
        [MenuItem("HackKU/Debug/Dump House Material Shaders")]
        public static void Dump()
        {
            var sb = new StringBuilder();
            var guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Materials" });
            sb.AppendLine("Found " + guids.Length + " materials.");
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) { sb.AppendLine(path + ": LOAD FAILED"); continue; }
                sb.Append(path).Append("  shader=").Append(mat.shader ? mat.shader.name : "NULL");
                if (mat.shader != null) sb.Append("  supportedOnCurrentRP=" + (mat.shader.isSupported ? "yes" : "NO"));
                sb.AppendLine();
            }

            // Also check active render pipeline
            var rp = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline;
            sb.AppendLine("---");
            sb.AppendLine("defaultRenderPipeline = " + (rp != null ? rp.GetType().FullName + " (" + rp.name + ")" : "NULL"));
            var currentPipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            sb.AppendLine("currentRenderPipeline = " + (currentPipeline != null ? currentPipeline.GetType().FullName + " (" + currentPipeline.name + ")" : "NULL"));

            System.IO.File.WriteAllText("Assets/material_shader_dump.txt", sb.ToString());
            AssetDatabase.Refresh();
            Debug.Log("[MaterialDebug] wrote Assets/material_shader_dump.txt");
        }
    }
}
