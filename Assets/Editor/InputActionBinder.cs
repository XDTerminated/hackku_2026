using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

namespace HackKU.EditorTools
{
    public static class InputActionBinder
    {
        const string XRIActions = "Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/XRI Default Input Actions.inputactions";

        [MenuItem("HackKU/Fix/Bind InputActionManager")]
        public static void Bind()
        {
            var rig = GameObject.Find("XR Origin (XR Rig)");
            if (rig == null) { Debug.LogError("[InputActionBinder] no XR Origin"); return; }
            var iam = rig.GetComponent<InputActionManager>();
            if (iam == null)
            {
                iam = rig.AddComponent<InputActionManager>();
                Debug.Log("[InputActionBinder] added InputActionManager");
            }

            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(XRIActions);
            if (asset == null)
            {
                Debug.LogError("[InputActionBinder] couldn't load " + XRIActions);
                return;
            }

            var so = new SerializedObject(iam);
            SerializedProperty arr = null;
            var it = so.GetIterator();
            while (it.NextVisible(true))
            {
                if (it.isArray && (it.name.Contains("ction") && it.name.Contains("sset")))
                {
                    arr = it.Copy();
                    break;
                }
            }
            if (arr == null)
            {
                var list = new List<InputActionAsset> { asset };
                iam.actionAssets = list;
                EditorUtility.SetDirty(iam);
                Debug.Log("[InputActionBinder] used property setter for actionAssets");
            }
            else
            {
                arr.arraySize = 1;
                arr.GetArrayElementAtIndex(0).objectReferenceValue = asset;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(iam);
                Debug.Log("[InputActionBinder] set via SerializedObject field " + arr.name);
            }
            EditorSceneManager.MarkSceneDirty(rig.scene);
            Debug.Log("[InputActionBinder] bound XRI Default Input Actions to InputActionManager");

            DumpLocomotionBindings();
        }

        static void DumpLocomotionBindings()
        {
            var rig = GameObject.Find("XR Origin (XR Rig)");
            if (rig == null) return;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== Locomotion provider field dump ===");
            foreach (var c in rig.GetComponentsInChildren<Component>(true))
            {
                if (c == null) continue;
                var n = c.GetType().FullName;
                if (!n.Contains("MoveProvider") && !n.Contains("TurnProvider") && !n.Contains("TeleportationProvider") && !n.Contains("DynamicMove")) continue;
                sb.AppendLine(c.gameObject.name + " -> " + n);
                var so = new SerializedObject(c);
                var it = so.GetIterator();
                while (it.NextVisible(true))
                {
                    if (it.propertyType == SerializedPropertyType.ObjectReference && it.objectReferenceValue != null)
                    {
                        sb.AppendLine("  " + it.propertyPath + " = " + it.objectReferenceValue.name + " (" + it.objectReferenceValue.GetType().Name + ")");
                    }
                }
            }
            File.WriteAllText("Assets/locomotion_diag.txt", sb.ToString());
            AssetDatabase.Refresh();
        }
    }
}
