using System.Collections.Generic;
using HackKU.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace HackKU.EditorTools
{
    public static class SeventhPolish
    {
        const string HandsetPrefabPath = "Assets/Data/Prefabs/Handset.prefab";
        const string PhonePrefabPath = "Assets/Data/Prefabs/RotaryPhone.prefab";

        [MenuItem("HackKU/Fix/Seventh Polish (Handset Pickup)")]
        public static void Run()
        {
            PatchPrefab(HandsetPrefabPath);
            PatchPrefab(PhonePrefabPath);
            PatchSceneInstances();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("[SeventhPolish] done.");
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
                    ConfigureGrab(grab);
                }
                PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);
            }
            finally { Object.DestroyImmediate(instance); }
            Debug.Log("[SeventhPolish] patched " + path);
        }

        static void PatchSceneInstances()
        {
            foreach (var grab in Object.FindObjectsByType<XRGrabInteractable>(FindObjectsSortMode.None))
            {
                // Only touch our phone/handset, not every interactable in the scene.
                if (!grab.gameObject.name.Contains("Handset") && !grab.gameObject.name.Contains("RotaryPhone")) continue;
                ConfigureGrab(grab);
                EditorUtility.SetDirty(grab);
            }
        }

        static void ConfigureGrab(XRGrabInteractable grab)
        {
            grab.enabled = true;

            // Ensure interaction layer includes the "Default" (bit 0) so both near and far casters can see it.
            grab.interactionLayers = InteractionLayerMask.GetMask("Default");

            // Populate colliders explicitly — auto-discovery can miss when m_Colliders was saved empty.
            var colliders = new List<Collider>(grab.GetComponentsInChildren<Collider>(true));
            var colProp = new SerializedObject(grab).FindProperty("m_Colliders");
            if (colProp != null)
            {
                colProp.arraySize = colliders.Count;
                for (int i = 0; i < colliders.Count; i++)
                    colProp.GetArrayElementAtIndex(i).objectReferenceValue = colliders[i];
                colProp.serializedObject.ApplyModifiedProperties();
            }

            // Instantaneous snap-to-hand.
            grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
            grab.trackPosition = true;
            grab.trackRotation = true;
            grab.throwOnDetach = false;
            grab.smoothPosition = false;
            grab.smoothRotation = false;
            grab.retainTransformParent = true;

            // Make sure Rigidbody is set up for kinematic pickup.
            var rb = grab.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                EditorUtility.SetDirty(rb);
            }

            EditorUtility.SetDirty(grab);
            Debug.Log("[SeventhPolish] configured grab on " + grab.gameObject.name + " colliders=" + colliders.Count + " layers=" + grab.interactionLayers.value);
        }
    }
}
