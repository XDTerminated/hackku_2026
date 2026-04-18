using System.Collections.Generic;
using HackKU.Core;
using HackKU.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;

namespace HackKU.EditorTools
{
    public static class SecondPolish
    {
        [MenuItem("HackKU/Fix/Second Polish (Turn + Cards + Handset)")]
        public static void Run()
        {
            MakeTurnContinuous();
            RepositionAndTuneCards();
            EnsureCardInteractable();
            EnableUIOnInteractors();
            FixHandsetSnap();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("[SecondPolish] done.");
        }

        static void MakeTurnContinuous()
        {
            foreach (var snap in Object.FindObjectsByType<SnapTurnProvider>(FindObjectsSortMode.None))
            {
                snap.enabled = false;
                EditorUtility.SetDirty(snap);
                Debug.Log("[SecondPolish] disabled SnapTurnProvider on " + snap.name);
            }
            foreach (var cont in Object.FindObjectsByType<ContinuousTurnProvider>(FindObjectsSortMode.None))
            {
                cont.enabled = true;
                cont.turnSpeed = 90f;
                EditorUtility.SetDirty(cont);
                Debug.Log("[SecondPolish] enabled ContinuousTurnProvider on " + cont.name + " turnSpeed=90");
            }
        }

        static void RepositionAndTuneCards()
        {
            var select = GameObject.Find("CharacterSelect");
            if (select != null)
            {
                // Move cards much further away — player spawn is at z=-2, put anchor at z=0.8 so cards are ~2.8m in front.
                select.transform.position = new Vector3(0f, 1.45f, 0.8f);
                var selector = select.GetComponent<CharacterSelector>();
                if (selector != null)
                {
                    var so = new SerializedObject(selector);
                    var rad = so.FindProperty("radius"); if (rad != null) rad.floatValue = 0.9f;
                    var arc = so.FindProperty("arcDegrees"); if (arc != null) arc.floatValue = 70f;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(selector);
                }
            }

            // Shrink card prefab.
            var card = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Data/Prefabs/CharacterCard.prefab");
            if (card != null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(card);
                try
                {
                    instance.transform.localScale = Vector3.one * 0.0009f;
                    PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);
                }
                finally { Object.DestroyImmediate(instance); }
            }
        }

        static void EnsureCardInteractable()
        {
            // Belt-and-suspenders: add a BoxCollider so the Near-Far Interactor's physics ray also has a target.
            var card = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Data/Prefabs/CharacterCard.prefab");
            if (card == null) return;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(card);
            try
            {
                var bg = instance.transform.Find("Background");
                if (bg != null)
                {
                    var col = bg.GetComponent<BoxCollider>();
                    if (col == null) col = bg.gameObject.AddComponent<BoxCollider>();
                    var rt = (RectTransform)bg.transform;
                    col.size = new Vector3(rt.rect.width, rt.rect.height, 10f);
                    col.center = Vector3.zero;
                    col.isTrigger = false;
                }
                PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);
            }
            finally { Object.DestroyImmediate(instance); }
        }

        static void EnableUIOnInteractors()
        {
            foreach (var nf in Object.FindObjectsByType<NearFarInteractor>(FindObjectsSortMode.None))
            {
                nf.enableUIInteraction = true;
                nf.enableFarCasting = true;
                EditorUtility.SetDirty(nf);
                Debug.Log("[SecondPolish] NearFarInteractor UI/far enabled on " + nf.gameObject.name);
            }
        }

        static void FixHandsetSnap()
        {
            var handsetPath = "Assets/Data/Prefabs/Handset.prefab";
            var phonePath = "Assets/Data/Prefabs/RotaryPhone.prefab";
            TuneGrabInteractableInPrefab(handsetPath);
            TuneGrabInteractableInPrefab(phonePath);

            // Also fix any existing scene instances of the handset.
            var scene = GameObject.Find("Handset");
            if (scene != null) TuneGrabOnGameObject(scene);
            // Try finding Handset under the phone in scene too.
            var phones = Object.FindObjectsByType<RotaryPhone>(FindObjectsSortMode.None);
            foreach (var p in phones)
            {
                var h = p.GetComponentInChildren<HandsetController>(true);
                if (h != null) TuneGrabOnGameObject(h.gameObject);
            }
        }

        static void TuneGrabInteractableInPrefab(string prefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                foreach (var grab in instance.GetComponentsInChildren<XRGrabInteractable>(true))
                {
                    ApplySnapSettings(grab);
                }
                PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);
            }
            finally { Object.DestroyImmediate(instance); }
        }

        static void TuneGrabOnGameObject(GameObject go)
        {
            foreach (var grab in go.GetComponents<XRGrabInteractable>())
            {
                ApplySnapSettings(grab);
                EditorUtility.SetDirty(grab);
            }
        }

        static void ApplySnapSettings(XRGrabInteractable grab)
        {
            grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
            grab.trackPosition = true;
            grab.trackRotation = true;
            grab.throwOnDetach = false;
            grab.smoothPosition = false;
            grab.smoothRotation = false;

            // Ensure an Attach Transform so the handset snaps to a predictable grip point.
            Transform attach = grab.attachTransform;
            if (attach == null)
            {
                var existing = grab.transform.Find("GrabAttach");
                if (existing != null)
                {
                    attach = existing;
                }
                else
                {
                    var go = new GameObject("GrabAttach");
                    go.transform.SetParent(grab.transform, false);
                    go.transform.localPosition = new Vector3(0f, 0f, 0f);
                    go.transform.localRotation = Quaternion.Euler(70f, 90f, 0f);
                    attach = go.transform;
                }
                grab.attachTransform = attach;
            }
            Debug.Log("[SecondPolish] tuned XRGrabInteractable on " + grab.gameObject.name + " movementType=Instantaneous attach=" + attach.name);
        }
    }
}
