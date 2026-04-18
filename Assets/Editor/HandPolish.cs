using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HackKU.EditorTools
{
    // Fixes the XRI Starter Assets controller-visual rotation (they ship with a 180° Y flip
    // that makes the controller model face backward in-simulator) and lifts the wrist UI
    // up & away from the controller mesh so it doesn't peek through.
    public static class HandPolish
    {
        [MenuItem("HackKU/Fix/Hand Visuals + Wrist Placement")]
        public static void Run()
        {
            FixControllerVisuals();
            FixWristPlacement();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("[HandPolish] done.");
        }

        static void FixControllerVisuals()
        {
            // XRI Starter Assets ships the controller mesh modeled facing -Z, so a 180° Y flip
            // is required to make it point in the same direction as the interactor ray (+Z).
            // Right controller mirrors via negative X scale.
            string[] names = { "Left Controller Visual", "Right Controller Visual" };
            foreach (var name in names)
            {
                var go = GameObject.Find(name);
                if (go == null) { Debug.LogWarning("[HandPolish] no " + name); continue; }

                var t = go.transform;
                bool isRight = name.StartsWith("Right");
                t.localRotation = Quaternion.Euler(0f, 180f, 0f);
                t.localPosition = new Vector3(0f, 0f, -0.05f);
                t.localScale = isRight ? new Vector3(-1f, 1f, 1f) : Vector3.one;
                EditorUtility.SetDirty(t);
                Debug.Log("[HandPolish] set " + name + " to Y=180°");
            }
        }

        static void FixWristPlacement()
        {
            var wrist = GameObject.Find("WristCanvas");
            if (wrist == null) { Debug.LogWarning("[HandPolish] no WristCanvas"); return; }

            // Lift the wrist canvas above the back of the controller so it doesn't intersect
            // the mesh. Billboard still rotates it to face head, so only position matters.
            wrist.transform.localPosition = new Vector3(0f, 0.06f, -0.08f);
            wrist.transform.localRotation = Quaternion.identity;
            wrist.transform.localScale = Vector3.one * 0.001f;
            EditorUtility.SetDirty(wrist.transform);

            // Nudge the canvas's sort order so it draws on top when it does intersect.
            var canvas = wrist.GetComponent<UnityEngine.Canvas>();
            if (canvas != null)
            {
                canvas.sortingOrder = 10;
                EditorUtility.SetDirty(canvas);
            }
        }
    }
}
