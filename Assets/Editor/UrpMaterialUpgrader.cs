using System.IO;
using UnityEditor;
using UnityEngine;

namespace HackKU.EditorTools
{
    // Walks every .mat under Assets/Materials/House/ and swaps the Standard shader (built-in RP)
    // to Universal Render Pipeline/Lit, preserving each material's base color so the pink
    // "missing shader" look goes away.
    public static class UrpMaterialUpgrader
    {
        [MenuItem("HackKU/Fix/Upgrade House Materials to URP")]
        public static void Upgrade()
        {
            var urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null)
            {
                Debug.LogError("[UrpMaterialUpgrader] Universal Render Pipeline/Lit shader not found.");
                return;
            }

            int count = 0;
            var guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Materials" });
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;
                if (mat.shader == urpLit) continue;

                Color baseColor = Color.white;
                if (mat.HasProperty("_Color")) baseColor = mat.GetColor("_Color");
                if (mat.HasProperty("_BaseColor")) baseColor = mat.GetColor("_BaseColor");
                float metallic = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : 0f;
                float smoothness = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness")
                                : mat.HasProperty("_Smoothness") ? mat.GetFloat("_Smoothness") : 0.3f;

                mat.shader = urpLit;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseColor);
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
                EditorUtility.SetDirty(mat);
                count++;
                Debug.Log("[UrpMaterialUpgrader] upgraded " + Path.GetFileName(path));
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[UrpMaterialUpgrader] Upgraded " + count + " materials to URP/Lit.");
        }
    }
}
