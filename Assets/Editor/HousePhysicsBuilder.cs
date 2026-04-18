using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HackKU.EditorTools
{
    // Adds MeshColliders to every house mesh (so the player can't walk through furniture
    // or fall through floors) and TeleportationArea components to walkable surfaces
    // (ground floor, upper floor, stairs) so right-thumbstick-forward lets you reach
    // the second floor. Also places interior point lights so the house isn't pitch dark.
    public static class HousePhysicsBuilder
    {
        [MenuItem("HackKU/Build/House Physics + Lighting")]
        public static void Build()
        {
            var house = GameObject.Find("House");
            if (house == null) { Debug.LogError("[HousePhysicsBuilder] no House in scene"); return; }

            int colliderCount = AddColliders(house);
            int teleportCount = AddTeleportAreas(house);
            int lightCount = AddInteriorLights(house);

            EditorSceneManager.MarkSceneDirty(house.scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[HousePhysicsBuilder] colliders={colliderCount}, teleportAreas={teleportCount}, lights={lightCount}");
        }

        static int AddColliders(GameObject house)
        {
            int count = 0;
            foreach (var mr in house.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mf = mr.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;

                // Skip absurdly tiny cosmetic meshes (< 1 cm smallest axis) to avoid spam.
                var s = mr.bounds.size;
                if (Mathf.Min(s.x, s.y, s.z) < 0.01f) continue;

                var existing = mr.GetComponent<MeshCollider>();
                if (existing == null)
                {
                    existing = mr.gameObject.AddComponent<MeshCollider>();
                }
                existing.sharedMesh = mf.sharedMesh;
                existing.convex = false; // non-convex for static geometry
                EditorUtility.SetDirty(existing);
                count++;
            }
            return count;
        }

        static int AddTeleportAreas(GameObject house)
        {
            var teleportType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationArea, Unity.XR.Interaction.Toolkit");
            if (teleportType == null)
            {
                Debug.LogWarning("[HousePhysicsBuilder] TeleportationArea type not found (XRI missing?)");
                return 0;
            }

            int count = 0;
            // Walkable surfaces are identified by their material name.
            HashSet<string> walkableMaterials = new HashSet<string>
            {
                "pavimento_piano_terra",
                "pavimento_primo_piano",
                "gradini",
            };

            foreach (var mr in house.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (mr.sharedMaterial == null) continue;
                if (!walkableMaterials.Contains(mr.sharedMaterial.name)) continue;

                if (mr.GetComponent(teleportType) == null)
                {
                    mr.gameObject.AddComponent(teleportType);
                    count++;
                }
            }
            return count;
        }

        static int AddInteriorLights(GameObject house)
        {
            // Remove any prior interior lights this builder placed so re-runs don't dup.
            var prev = house.transform.Find("InteriorLights");
            if (prev != null) Object.DestroyImmediate(prev.gameObject);

            var lightRoot = new GameObject("InteriorLights");
            lightRoot.transform.SetParent(house.transform, worldPositionStays: true);
            lightRoot.transform.localPosition = Vector3.zero;

            Bounds bounds = default;
            bool first = true;
            foreach (var mr in house.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (first) { bounds = mr.bounds; first = false; }
                else bounds.Encapsulate(mr.bounds);
            }
            if (first) return 0;

            // Ground floor roughly occupies the lower half of the house bounds; upper floor the upper half.
            float groundY = bounds.min.y + bounds.size.y * 0.25f + 0.5f;
            float upperY = bounds.min.y + bounds.size.y * 0.70f + 0.5f;

            int count = 0;
            count += PlaceLight(lightRoot.transform, "Ground_Center", new Vector3(bounds.center.x, groundY, bounds.center.z), 5f, new Color(1f, 0.95f, 0.78f), 7f);
            count += PlaceLight(lightRoot.transform, "Ground_North",  new Vector3(bounds.center.x, groundY, bounds.max.z - 1.5f), 3.5f, new Color(1f, 0.92f, 0.7f), 5f);
            count += PlaceLight(lightRoot.transform, "Ground_South",  new Vector3(bounds.center.x, groundY, bounds.min.z + 1.5f), 3.5f, new Color(1f, 0.92f, 0.7f), 5f);
            count += PlaceLight(lightRoot.transform, "Upper_Center",  new Vector3(bounds.center.x, upperY, bounds.center.z), 5f, new Color(1f, 0.95f, 0.78f), 7f);
            count += PlaceLight(lightRoot.transform, "Upper_North",   new Vector3(bounds.center.x, upperY, bounds.max.z - 1.5f), 3.5f, new Color(1f, 0.92f, 0.7f), 5f);

            // Also boost ambient so the house isn't jet black where the point lights don't reach.
            RenderSettings.ambientLight = new Color(0.35f, 0.34f, 0.33f);

            return count;
        }

        static int PlaceLight(Transform parent, string name, Vector3 worldPos, float intensity, Color color, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.position = worldPos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.intensity = intensity;
            l.range = range;
            l.color = color;
            l.shadows = LightShadows.Soft;
            l.shadowStrength = 0.6f;
            return 1;
        }
    }
}
