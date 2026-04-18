using System.IO;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.Management;

namespace HackKU.EditorTools
{
    [InitializeOnLoad]
    public static class XRAutoSetup
    {
        const string SettingsPath = "Assets/XR/XRGeneralSettings.asset";
        const string MarkerKey = "HackKU.XRAutoSetup.Ran";

        static XRAutoSetup()
        {
            if (SessionState.GetBool(MarkerKey, false)) return;
            SessionState.SetBool(MarkerKey, true);
            EditorApplication.delayCall += RunOnce;
        }

        static void RunOnce()
        {
            var container = GetOrCreateContainer();
            AssignLoader(container, BuildTargetGroup.Standalone);
            AssignLoader(container, BuildTargetGroup.Android);
            Report(container);
        }

        [MenuItem("HackKU/Setup XR/Run Again")]
        public static void RunAgain()
        {
            var container = GetOrCreateContainer();
            AssignLoader(container, BuildTargetGroup.Standalone);
            AssignLoader(container, BuildTargetGroup.Android);
            Report(container);
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

        static void AssignLoader(XRGeneralSettingsPerBuildTarget container, BuildTargetGroup group)
        {
            container.CreateDefaultSettingsForBuildTarget(group);
            var settings = container.SettingsForBuildTarget(group);
            if (settings == null) { Debug.LogError("[XRAutoSetup] null settings for " + group); return; }
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
            Debug.Log("[XRAutoSetup] " + group + " assign=" + ok + " init=" + settings.InitManagerOnStart + " loaders=" + settings.Manager.activeLoaders.Count);
        }

        static void Report(XRGeneralSettingsPerBuildTarget container)
        {
            foreach (var grp in new[] { BuildTargetGroup.Standalone, BuildTargetGroup.Android })
            {
                var s = container.SettingsForBuildTarget(grp);
                if (s == null || s.Manager == null) { Debug.Log("[XRAutoSetup] " + grp + ": none"); continue; }
                Debug.Log("[XRAutoSetup] " + grp + " REPORT: init=" + s.InitManagerOnStart + " loaders=" + s.Manager.activeLoaders.Count);
                for (int i = 0; i < s.Manager.activeLoaders.Count; i++)
                {
                    var ld = s.Manager.activeLoaders[i];
                    Debug.Log("  - " + (ld != null ? ld.GetType().FullName : "null"));
                }
            }
        }
    }
}
