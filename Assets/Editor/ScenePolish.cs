using HackKU.Core;
using HackKU.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HackKU.EditorTools
{
    public static class ScenePolish
    {
        [MenuItem("HackKU/Build/Polish Scene")]
        public static void Polish()
        {
            PositionWristCanvas();
            TuneCharacterSelect();
            RepositionPhone();
            DockDoorToHouse();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[ScenePolish] done.");
        }

        static void PositionWristCanvas()
        {
            var canvas = GameObject.Find("WristCanvas");
            if (canvas == null) return;
            canvas.transform.localPosition = new Vector3(0f, 0.03f, -0.08f);
            canvas.transform.localRotation = Quaternion.Euler(75f, 180f, 0f);
            canvas.transform.localScale = Vector3.one * 0.0008f;
            var rt = (RectTransform)canvas.transform;
            rt.sizeDelta = new Vector2(240, 140);
        }

        static void TuneCharacterSelect()
        {
            var select = GameObject.Find("CharacterSelect");
            if (select == null) return;
            select.transform.position = new Vector3(0f, 1.4f, -1f);
            var selector = select.GetComponent<CharacterSelector>();
            if (selector != null)
            {
                var so = new SerializedObject(selector);
                var rad = so.FindProperty("radius"); if (rad != null) rad.floatValue = 1.0f;
                var arc = so.FindProperty("arcDegrees"); if (arc != null) arc.floatValue = 50f;
                so.ApplyModifiedProperties();
            }
            var cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Data/Prefabs/CharacterCard.prefab");
            if (cardPrefab != null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(cardPrefab);
                instance.transform.localScale = Vector3.one * 0.0015f;
                PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);
                Object.DestroyImmediate(instance);
            }
        }

        static void RepositionPhone()
        {
            var phone = GameObject.Find("RotaryPhone(Clone)") ?? GameObject.Find("RotaryPhone");
            var table = GameObject.Find("PhoneTable");
            if (phone == null) return;
            phone.transform.position = new Vector3(1.5f, 0.82f, -1.2f);
            phone.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            if (table != null)
            {
                table.transform.position = new Vector3(1.5f, 0.4f, -1.2f);
                table.transform.localScale = new Vector3(0.6f, 0.8f, 0.5f);
            }
        }

        static void DockDoorToHouse()
        {
            var door = GameObject.Find("FrontDoorPivot");
            var spawn = GameObject.Find("DeliverySpawn");
            if (door != null)
            {
                door.transform.position = new Vector3(-3.5f, 0f, -2f);
                door.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            }
            if (spawn != null)
            {
                spawn.transform.position = new Vector3(-5.5f, 0f, -2f);
            }
        }
    }
}
