using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace HackKU.EditorTools
{
    public static class EighthPolish
    {
        const string HandsetPrefabPath = "Assets/Data/Prefabs/Handset.prefab";
        const string PhonePrefabPath = "Assets/Data/Prefabs/RotaryPhone.prefab";

        [MenuItem("HackKU/Fix/Eighth Polish (Handset Grip Pose)")]
        public static void Run()
        {
            PatchPrefab(HandsetPrefabPath);
            PatchPrefab(PhonePrefabPath);
            PatchSceneInstances();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("[EighthPolish] done.");
        }

        static void PatchPrefab(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                foreach (var grab in instance.GetComponentsInChildren<XRGrabInteractable>(true))
                {
                    ConfigureAttach(grab);
                }
                PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);
            }
            finally { Object.DestroyImmediate(instance); }
            Debug.Log("[EighthPolish] patched " + path);
        }

        static void PatchSceneInstances()
        {
            foreach (var grab in Object.FindObjectsByType<XRGrabInteractable>(FindObjectsSortMode.None))
            {
                if (!grab.gameObject.name.Contains("Handset") && !grab.gameObject.name.Contains("RotaryPhone")) continue;
                ConfigureAttach(grab);
                EditorUtility.SetDirty(grab);
            }
        }

        static void ConfigureAttach(XRGrabInteractable grab)
        {
            // Find or create the GrabAttach child.
            var attach = grab.transform.Find("GrabAttach");
            if (attach == null)
            {
                var go = new GameObject("GrabAttach");
                go.transform.SetParent(grab.transform, false);
                attach = go.transform;
            }

            // Pose: center of the handset bar; rotated so the earpiece points UP when held
            // in the controller's default forward pose (Z+ forward, Y+ up).
            // Handset local X is the bar-length axis (earpiece at -X, mouthpiece at +X).
            // Rotating -90 around Z puts handset X along world -Y, i.e. mouthpiece DOWN and earpiece UP.
            attach.localPosition = Vector3.zero;
            attach.localRotation = Quaternion.Euler(0f, 0f, -90f);
            attach.localScale = Vector3.one;

            grab.attachTransform = attach;
            grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
            grab.trackPosition = true;
            grab.trackRotation = true;
            grab.smoothPosition = false;
            grab.smoothRotation = false;
            grab.throwOnDetach = false;
            grab.useDynamicAttach = false;

            EditorUtility.SetDirty(attach);
            EditorUtility.SetDirty(grab);
            Debug.Log("[EighthPolish] attachTransform set on " + grab.gameObject.name + " pos=(0,0,0) rot=(0,0,-90)");
        }
    }
}
