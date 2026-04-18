using HackKU.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;

namespace HackKU.EditorTools
{
    public static class ThirdPolish
    {
        [MenuItem("HackKU/Fix/Third Polish (Turn + CardFollow + Wrist)")]
        public static void Run()
        {
            InstallSimpleTurn();
            AttachCardFollower();
            FixWristBillboard();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("[ThirdPolish] done.");
        }

        static void InstallSimpleTurn()
        {
            // Disable both XRI turn providers to avoid double-turn.
            foreach (var c in Object.FindObjectsByType<ContinuousTurnProvider>(FindObjectsSortMode.None))
            {
                c.enabled = false;
                EditorUtility.SetDirty(c);
            }
            foreach (var s in Object.FindObjectsByType<SnapTurnProvider>(FindObjectsSortMode.None))
            {
                s.enabled = false;
                EditorUtility.SetDirty(s);
            }

            var rig = GameObject.Find("XR Origin (XR Rig)");
            if (rig == null) { Debug.LogError("[ThirdPolish] no XR Origin"); return; }
            var st = rig.GetComponent<SimpleSmoothTurn>();
            if (st == null) st = rig.AddComponent<SimpleSmoothTurn>();
            st.target = rig.transform;
            st.turnSpeed = 90f;
            st.deadZone = 0.2f;
            EditorUtility.SetDirty(st);
            Debug.Log("[ThirdPolish] SimpleSmoothTurn installed on XR Origin");
        }

        static void AttachCardFollower()
        {
            var select = GameObject.Find("CharacterSelect");
            if (select == null) { Debug.LogWarning("[ThirdPolish] no CharacterSelect"); return; }
            var follower = select.GetComponent<CardFanFollower>();
            if (follower == null) follower = select.AddComponent<CardFanFollower>();
            follower.distance = 2.2f;
            follower.eyeHeight = 1.55f;
            follower.followSpeed = 4f;
            EditorUtility.SetDirty(follower);
            Debug.Log("[ThirdPolish] CardFanFollower attached");
        }

        static void FixWristBillboard()
        {
            var wrist = GameObject.Find("WristCanvas");
            if (wrist == null) return;

            var bill = wrist.GetComponent<WristBillboardFace>();
            if (bill == null) bill = wrist.AddComponent<WristBillboardFace>();
            bill.tiltDegrees = 20f;
            // Reset transform so the billboard drives it from scratch.
            wrist.transform.localPosition = new Vector3(0f, 0.02f, -0.05f);
            wrist.transform.localRotation = Quaternion.identity;
            wrist.transform.localScale = Vector3.one * 0.0009f;
            EditorUtility.SetDirty(bill);
            Debug.Log("[ThirdPolish] wrist billboard attached");
        }
    }
}
