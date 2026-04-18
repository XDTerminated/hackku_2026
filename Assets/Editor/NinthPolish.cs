using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace HackKU.EditorTools
{
    public static class NinthPolish
    {
        [MenuItem("HackKU/Fix/Ninth Polish (Far Attach = Near)")]
        public static void Run()
        {
            int changed = 0;
            foreach (var nf in Object.FindObjectsByType<NearFarInteractor>(FindObjectsSortMode.None))
            {
                nf.farAttachMode = InteractorFarAttachMode.Near;
                nf.enableNearCasting = true;
                nf.enableFarCasting = true;
                nf.enableUIInteraction = true;
                EditorUtility.SetDirty(nf);
                Debug.Log("[NinthPolish] " + nf.gameObject.name + " farAttachMode=Near");
                changed++;
            }
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("[NinthPolish] done, " + changed + " interactors fixed.");
        }
    }
}
