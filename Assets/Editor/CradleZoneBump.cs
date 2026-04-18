using HackKU.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HackKU.EditorTools
{
    public static class CradleZoneBump
    {
        [MenuItem("HackKU/Fix/Bump Cradle Zone (Lenient Dock)")]
        public static void Run()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Data/Prefabs/Handset.prefab");
            if (prefab != null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                try
                {
                    var hc = instance.GetComponent<HandsetController>();
                    if (hc != null)
                    {
                        hc.cradleProximity = 0.6f;
                        hc.cradleZoneHalfExtents = new Vector3(0.6f, 0.8f, 0.5f);
                    }
                    PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);
                }
                finally { Object.DestroyImmediate(instance); }
            }

            foreach (var hc in Object.FindObjectsByType<HandsetController>(FindObjectsSortMode.None))
            {
                hc.cradleProximity = 0.6f;
                hc.cradleZoneHalfExtents = new Vector3(0.6f, 0.8f, 0.5f);
                EditorUtility.SetDirty(hc);
            }
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("[CradleZoneBump] cradleProximity=0.6, cradleZoneHalfExtents=(0.6,0.8,0.5)");
        }
    }
}
