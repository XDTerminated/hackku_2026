using System.IO;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.Management;

namespace HackKU.EditorTools
{
    public static class XRSetupWizard
    {
        const string SettingsPath = "Assets/XR/XRGeneralSettings.asset";

        [MenuItem("HackKU/Setup XR/Enable OpenXR (Standalone)")]
        public static void EnableOpenXRStandalone()
        {
            AssignLoader(BuildTargetGroup.Standalone);
        }

        [MenuItem("HackKU/Setup XR/Enable OpenXR (Android)")]
        public static void EnableOpenXRAndroid()
        {
            AssignLoader(BuildTargetGroup.Android);
        }

        [MenuItem("HackKU/Setup XR/Enable OpenXR (Both)")]
        public static void EnableBoth()
        {
            AssignLoader(BuildTargetGroup.Standalone);
            AssignLoader(BuildTargetGroup.Android);
            ReportLoaders();
        }

        static XRGeneralSettingsPerBuildTarget GetOrCreateContainer()
        {
            XRGeneralSettingsPerBuildTarget container;
            EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out container);
            if (container == null)
            {
                Directory.CreateDirectory("Assets/XR");
                container = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(SettingsPath);
                if (container == null)
                {
                    container = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                    AssetDatabase.CreateAsset(container, SettingsPath);
                }
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, container, true);
            }
            return container;
        }

        static void AssignLoader(BuildTargetGroup group)
        {
            var container = GetOrCreateContainer();
            container.CreateDefaultSettingsForBuildTarget(group);
            var settings = container.SettingsForBuildTarget(group);
            if (settings == null)
            {
                Debug.LogError("[XRSetupWizard] null settings for " + group);
                return;
            }
            if (settings.Manager == null)
            {
                var manager = ScriptableObject.CreateInstance<XRManagerSettings>();
                manager.name = "XRManagerSettings_" + group;
                AssetDatabase.AddObjectToAsset(manager, container);
                settings.Manager = manager;
            }
            bool ok = XRPackageMetadataStore.AssignLoader(settings.Manager, "UnityEngine.XR.OpenXR.OpenXRLoader", group);
            settings.InitManagerOnStart = true;
            EditorUtility.SetDirty(settings);
            EditorUtility.SetDirty(container);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[XRSetupWizard] " + group + " assign=" + ok + " init=" + settings.InitManagerOnStart + " loaders=" + settings.Manager.activeLoaders.Count);
        }

        [MenuItem("HackKU/Setup XR/Report Loaders")]
        public static void ReportLoaders()
        {
            XRGeneralSettingsPerBuildTarget container;
            EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out container);
            if (container == null)
            {
                Debug.Log("[XRSetupWizard] no container");
                return;
            }
            foreach (var grp in new[] { BuildTargetGroup.Standalone, BuildTargetGroup.Android })
            {
                var s = container.SettingsForBuildTarget(grp);
                if (s == null || s.Manager == null)
                {
                    Debug.Log("[XRSetupWizard] " + grp + ": none");
                    continue;
                }
                Debug.Log("[XRSetupWizard] " + grp + ": init=" + s.InitManagerOnStart + " loaders=" + s.Manager.activeLoaders.Count);
                for (int i = 0; i < s.Manager.activeLoaders.Count; i++)
                {
                    var loader = s.Manager.activeLoaders[i];
                    Debug.Log("  - " + (loader != null ? loader.GetType().FullName : "null"));
                }
            }
        }
    }
}
