using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI;
using UnityEngine;

namespace HackKU.EditorTools
{
    public static class XRIStarterImporter
    {
        [MenuItem("HackKU/Setup XR/Import XRI Starter Assets")]
        public static void ImportStarter()
        {
            ImportSample("com.unity.xr.interaction.toolkit", "Starter Assets");
            ImportSample("com.unity.xr.interaction.toolkit", "XR Device Simulator");
            ImportSample("com.unity.xr.interaction.toolkit", "Hands Interaction Demo");
            AssetDatabase.Refresh();
            Debug.Log("[XRIStarterImporter] Sample import requests issued.");
        }

        static void ImportSample(string packageName, string sampleName)
        {
            var samples = Sample.FindByPackage(packageName, string.Empty);
            if (samples == null)
            {
                Debug.LogWarning("[XRIStarterImporter] No samples found for " + packageName);
                return;
            }
            var sample = samples.FirstOrDefault(s => s.displayName == sampleName);
            if (sample.displayName == null)
            {
                Debug.LogWarning("[XRIStarterImporter] Sample not found: " + sampleName);
                return;
            }
            bool imported = sample.Import(Sample.ImportOptions.OverridePreviousImports);
            Debug.Log("[XRIStarterImporter] " + sampleName + " import triggered: " + imported + " (path=" + sample.importPath + ")");
        }
    }
}
