using HackKU.Core;
using HackKU.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace HackKU.EditorTools
{
    public static class FourthPolish
    {
        const string CardPrefabPath = "Assets/Data/Prefabs/CharacterCard.prefab";

        [MenuItem("HackKU/Fix/Fourth Polish")]
        public static void Run()
        {
            System.IO.File.WriteAllText("Assets/fourth_polish_trace.txt", "entered Run at " + System.DateTime.Now);
            try
            {
                PatchCardPrefab();
                MakeFollowerPositionOnly();
                ResetSceneCards();
                EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                AssetDatabase.SaveAssets();
                Debug.Log("[FourthPolish] done.");
                System.IO.File.AppendAllText("Assets/fourth_polish_trace.txt", "\nfinished without throwing");
            }
            catch (System.Exception ex)
            {
                System.IO.File.AppendAllText("Assets/fourth_polish_trace.txt", "\nthrew: " + ex);
                throw;
            }
            AssetDatabase.Refresh();
        }

        static void PatchCardPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
            if (prefab == null) { Debug.LogError("[FourthPolish] no card prefab"); return; }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                // Billboard so each card always faces the player's head regardless of anchor rotation.
                if (instance.GetComponent<CardBillboard>() == null)
                {
                    instance.AddComponent<CardBillboard>();
                }

                // Ensure BoxCollider on root for XR physics selection.
                var rootCol = instance.GetComponent<BoxCollider>();
                if (rootCol == null)
                {
                    rootCol = instance.AddComponent<BoxCollider>();
                }
                var rt = (RectTransform)instance.transform;
                rootCol.center = Vector3.zero;
                rootCol.size = new Vector3(rt.rect.width, rt.rect.height, 10f);
                rootCol.isTrigger = false;

                // XRSimpleInteractable allows trigger-press to "click" without uGUI raycasting.
                if (instance.GetComponent<XRSimpleInteractable>() == null)
                {
                    instance.AddComponent<XRSimpleInteractable>();
                }

                // Bridge selection to Button.onClick.
                if (instance.GetComponent<CardXRClick>() == null)
                {
                    instance.AddComponent<CardXRClick>();
                }

                PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);
            }
            finally { Object.DestroyImmediate(instance); }
            Debug.Log("[FourthPolish] CharacterCard prefab patched (Billboard + Collider + XRSimpleInteractable + CardXRClick)");
        }

        static void MakeFollowerPositionOnly()
        {
            var follower = Object.FindFirstObjectByType<CardFanFollower>();
            if (follower == null) return;
            follower.followSpeed = 5f;
            follower.distance = 2.2f;
            follower.eyeHeight = 1.55f;
            EditorUtility.SetDirty(follower);
        }

        static void ResetSceneCards()
        {
            // If cards were already spawned with the old orientation, we don't need to do anything
            // special — each new instance of the patched prefab carries Billboard + XRSimpleInteractable.
            // Cards get respawned when CharacterSelector.Spawn() runs at scene start / Play.
            var select = GameObject.Find("CharacterSelect");
            if (select == null) return;
            for (int i = select.transform.childCount - 1; i >= 0; i--)
            {
                var child = select.transform.GetChild(i);
                if (child.name.StartsWith("Card_"))
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }
        }
    }
}
