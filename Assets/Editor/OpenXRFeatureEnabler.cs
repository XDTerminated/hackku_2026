using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

namespace HackKU.EditorTools
{
    public static class OpenXRFeatureEnabler
    {
        [MenuItem("HackKU/Fix/Enable OpenXR Controller Profiles")]
        public static void EnableAll()
        {
            EditorUserBuildSettings.selectedBuildTargetGroup = BuildTargetGroup.Standalone;
            SettingsService.OpenProjectSettings("Project/XR Plug-in Management/OpenXR");
            EditorApplication.delayCall += () =>
            {
                EnableForActive(BuildTargetGroup.Standalone);
                EditorUserBuildSettings.selectedBuildTargetGroup = BuildTargetGroup.Android;
                SettingsService.OpenProjectSettings("Project/XR Plug-in Management/OpenXR");
                EditorApplication.delayCall += () =>
                {
                    EnableForActive(BuildTargetGroup.Android);
                    EditorUserBuildSettings.selectedBuildTargetGroup = BuildTargetGroup.Standalone;
                    AssetDatabase.SaveAssets();
                };
            };
        }

        [MenuItem("HackKU/Debug/List OpenXR Features")]
        public static void List()
        {
            EditorUserBuildSettings.selectedBuildTargetGroup = BuildTargetGroup.Standalone;
            SettingsService.OpenProjectSettings("Project/XR Plug-in Management/OpenXR");
            EditorApplication.delayCall += () => ListActive(BuildTargetGroup.Standalone);
        }

        static void EnableForActive(BuildTargetGroup group)
        {
            string[] wanted =
            {
                "OculusTouchControllerProfile",
                "MetaQuestTouchProControllerProfile",
                "MetaQuestTouchPlusControllerProfile",
                "MetaXRFeature",
            };

            var features = GetFeaturesViaActive();
            if (features == null || features.Count == 0) { Debug.LogWarning("no features for " + group); return; }
            int enabled = 0;
            foreach (var f in features)
            {
                if (f == null) continue;
                var n = f.GetType().Name;
                bool match = false;
                for (int i = 0; i < wanted.Length; i++) if (n.Contains(wanted[i])) { match = true; break; }
                if (!match) continue;
                f.enabled = true;
                EditorUtility.SetDirty(f);
                enabled++;
                Debug.Log("[OpenXRFeatureEnabler] " + group + " enabled: " + n);
            }
            Debug.Log("[OpenXRFeatureEnabler] " + group + " total enabled: " + enabled);
            AssetDatabase.SaveAssets();
        }

        static void ListActive(BuildTargetGroup group)
        {
            var features = GetFeaturesViaActive();
            Debug.Log("[OpenXRFeatureEnabler] " + group + " features (" + (features == null ? 0 : features.Count) + "):");
            if (features == null) return;
            foreach (var f in features)
            {
                if (f == null) continue;
                Debug.Log("  " + f.GetType().Name + " enabled=" + f.enabled);
            }
        }

        static List<OpenXRFeature> GetFeaturesViaActive()
        {
            var active = OpenXRSettings.ActiveBuildTargetInstance;
            if (active == null) return null;
            var list = new List<OpenXRFeature>();
            active.GetFeatures(list);
            return list;
        }
    }
}
