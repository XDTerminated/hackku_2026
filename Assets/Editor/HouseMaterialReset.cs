using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HackKU.EditorTools
{
    // Clears per-instance material overrides on the scene's House GameObject so the FBX
    // import's own material-remap table takes over. Fixes the "pink house" that shows up
    // when stale instance overrides reference deleted materials.
    public static class HouseMaterialReset
    {
        [MenuItem("HackKU/Fix/Reset House Material Overrides")]
        public static void Reset()
        {
            var house = GameObject.Find("House");
            if (house == null)
            {
                Debug.LogError("[HouseMaterialReset] No 'House' GameObject in scene.");
                return;
            }

            int reset = 0;
            foreach (var mr in house.GetComponentsInChildren<MeshRenderer>(true))
            {
                var so = new SerializedObject(mr);
                var matsProp = so.FindProperty("m_Materials");
                if (matsProp != null && matsProp.isArray)
                {
                    // RevertPropertyOverride reverts this property to its prefab source (the FBX),
                    // which now correctly maps each embedded material name via externalObjects.
                    PrefabUtility.RevertPropertyOverride(matsProp, InteractionMode.AutomatedAction);
                    reset++;
                }
            }

            EditorSceneManager.MarkSceneDirty(house.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[HouseMaterialReset] reverted material overrides on " + reset + " renderers");
        }
    }
}
