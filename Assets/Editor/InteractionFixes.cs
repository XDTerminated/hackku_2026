using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HackKU.EditorTools
{
    public static class InteractionFixes
    {
        [MenuItem("HackKU/Debug/Dump Rig + Card Diagnostics")]
        public static void Diagnose()
        {
            var sb = new StringBuilder();

            var rig = GameObject.Find("XR Origin (XR Rig)");
            sb.AppendLine("=== XR Origin components ===");
            if (rig != null)
            {
                foreach (var c in rig.GetComponents<Component>())
                    sb.AppendLine("  " + (c == null ? "NULL" : c.GetType().FullName));

                sb.AppendLine("=== Descendants with Locomotion / MoveProvider / Teleport in name ===");
                foreach (var t in rig.GetComponentsInChildren<Transform>(true))
                {
                    foreach (var c in t.GetComponents<Component>())
                    {
                        if (c == null) continue;
                        var n = c.GetType().FullName;
                        if (n.Contains("Locomotion") || n.Contains("Move") || n.Contains("Teleport") || n.Contains("SnapTurn") || n.Contains("Continuous"))
                            sb.AppendLine("  " + t.name + " -> " + n);
                    }
                }
            }

            sb.AppendLine("=== Character Card prefab ===");
            var card = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Data/Prefabs/CharacterCard.prefab");
            if (card != null)
            {
                foreach (var c in card.GetComponents<Component>())
                    sb.AppendLine("  root: " + (c == null ? "NULL" : c.GetType().FullName));
                foreach (var t in card.GetComponentsInChildren<Transform>(true))
                {
                    foreach (var c in t.GetComponents<Component>())
                    {
                        if (c == null) continue;
                        var n = c.GetType().Name;
                        if (n.Contains("Button") || n.Contains("Raycaster") || n.Contains("Canvas") || n.Contains("CharacterCard"))
                            sb.AppendLine("  " + t.name + " -> " + c.GetType().FullName);
                    }
                }
            }

            var path = "Assets/rig_card_diag.txt";
            File.WriteAllText(path, sb.ToString());
            AssetDatabase.Refresh();
            Debug.Log("[InteractionFixes] wrote " + path);
        }

        [MenuItem("HackKU/Fix/Interactions + Locomotion")]
        public static void ApplyFixes()
        {
            AddTeleportAreaToGround();
            AddTrackedRaycasterToCard();
            AddTrackedRaycasterToWristCanvas();
            AddTrackedRaycasterToSceneCanvas();
            AddContinuousLocomotion();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[InteractionFixes] applied.");
        }

        static void AddTeleportAreaToGround()
        {
            var ground = GameObject.Find("Ground");
            if (ground == null) { Debug.LogWarning("[InteractionFixes] no Ground"); return; }
            var teleportType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationArea, Unity.XR.Interaction.Toolkit");
            if (teleportType == null)
            {
                Debug.LogWarning("[InteractionFixes] TeleportationArea type not found");
                return;
            }
            if (ground.GetComponent(teleportType) == null)
            {
                ground.AddComponent(teleportType);
                Debug.Log("[InteractionFixes] TeleportationArea added to Ground");
            }
        }

        static void AddTrackedRaycasterToCard()
        {
            var cardPath = "Assets/Data/Prefabs/CharacterCard.prefab";
            var card = AssetDatabase.LoadAssetAtPath<GameObject>(cardPath);
            if (card == null) { Debug.LogWarning("[InteractionFixes] no CharacterCard prefab"); return; }
            var raycasterType = FindTrackedRaycasterType();
            if (raycasterType == null) { Debug.LogWarning("[InteractionFixes] TrackedDeviceGraphicRaycaster type not found"); return; }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(card);
            try
            {
                if (instance.GetComponent(raycasterType) == null)
                {
                    instance.AddComponent(raycasterType);
                }
                PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);
            }
            finally { Object.DestroyImmediate(instance); }
            Debug.Log("[InteractionFixes] TrackedDeviceGraphicRaycaster ensured on CharacterCard prefab");
        }

        static void AddTrackedRaycasterToWristCanvas()
        {
            var wrist = GameObject.Find("WristCanvas");
            if (wrist == null) return;
            var raycasterType = FindTrackedRaycasterType();
            if (raycasterType == null) return;
            if (wrist.GetComponent(raycasterType) == null) wrist.AddComponent(raycasterType);
        }

        static void AddTrackedRaycasterToSceneCanvas()
        {
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (canvas == null) continue;
                if (canvas.renderMode != RenderMode.WorldSpace) continue;
                var raycasterType = FindTrackedRaycasterType();
                if (raycasterType == null) return;
                if (canvas.gameObject.GetComponent(raycasterType) == null)
                    canvas.gameObject.AddComponent(raycasterType);
            }
        }

        static System.Type FindTrackedRaycasterType()
        {
            string[] candidates =
            {
                "UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit",
                "UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster",
            };
            foreach (var c in candidates)
            {
                var t = System.Type.GetType(c);
                if (t != null) return t;
            }
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster", false);
                if (t != null) return t;
            }
            return null;
        }

        static void AddContinuousLocomotion()
        {
            var rig = GameObject.Find("XR Origin (XR Rig)");
            if (rig == null) { Debug.LogWarning("[InteractionFixes] no XR Origin"); return; }

            var locomotionChild = rig.transform.Find("Locomotion");
            GameObject locomotionGo;
            if (locomotionChild == null)
            {
                locomotionGo = new GameObject("Locomotion");
                locomotionGo.transform.SetParent(rig.transform, false);
            }
            else
            {
                locomotionGo = locomotionChild.gameObject;
            }

            var mediatorType = FindType("UnityEngine.XR.Interaction.Toolkit.Locomotion.LocomotionMediator");
            var moveType = FindType("UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement.ContinuousMoveProvider");
            var turnType = FindType("UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning.SnapTurnProvider");
            var teleportLocoType = FindType("UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider");

            if (mediatorType != null && locomotionGo.GetComponent(mediatorType) == null) locomotionGo.AddComponent(mediatorType);
            if (moveType != null && locomotionGo.GetComponent(moveType) == null) locomotionGo.AddComponent(moveType);
            if (turnType != null && locomotionGo.GetComponent(turnType) == null) locomotionGo.AddComponent(turnType);
            if (teleportLocoType != null && locomotionGo.GetComponent(teleportLocoType) == null) locomotionGo.AddComponent(teleportLocoType);

            Debug.Log("[InteractionFixes] Locomotion providers ensured: mediator=" + (mediatorType != null) + " move=" + (moveType != null) + " turn=" + (turnType != null) + " teleport=" + (teleportLocoType != null));
        }

        static System.Type FindType(string fullName)
        {
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(fullName, false);
                if (t != null) return t;
            }
            return null;
        }
    }
}
