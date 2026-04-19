using System.IO;
using HackKU.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HackKU.EditorTools
{
    // Builds a proper-looking front door (panel + frame + handle), replaces the prior gray cube,
    // adds four exterior walls closing off the house (with a door-sized gap in the front), and
    // repositions DeliverySpawn so food lands clearly outside the door.
    public static class HouseExteriorBuilder
    {
        [MenuItem("HackKU/Build/House Exterior (Walls + Door + Spawn)")]
        public static void Build()
        {
            EnsureMaterials();
            BuildDoor();
            BuildExteriorWalls();
            BuildCeiling();
            PlaceDeliverySpawn();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("[HouseExteriorBuilder] done.");
        }

        const string MatFolder = "Assets/Materials/Exterior";
        static Material _woodDoor;
        static Material _doorTrim;
        static Material _doorHandle;
        static Material _wallStucco;

        // House footprint — derived from the FBX's world bounds.
        // The house mesh spans roughly X[-4.55..1.17] Z[-1.57..9.37], Y[0..6.8].
        // We'll frame walls just outside those bounds.
        static readonly float MinX = -4.7f;
        static readonly float MaxX = 1.3f;
        static readonly float MinZ = -1.75f;
        static readonly float MaxZ = 9.5f;
        static readonly float WallHeight = 6.9f;
        static readonly float WallThickness = 0.12f;
        // Walls sink slightly below y=0 so there's no seam where wall meets floor.
        static readonly float WallFootSink = 0.8f;

        // Door cutout on the front (south) wall.
        // Centered on the front wall (which spans MinX..MaxX = -4.7..1.3 → midpoint -1.7).
        static readonly float DoorCenterX = -1.7f;
        static readonly float DoorWidth = 1.1f;
        static readonly float DoorHeight = 2.1f;

        static Material _ceiling;

        static void EnsureMaterials()
        {
            EnsureFolder(MatFolder);
            // Upstairs-interior door: lighter painted finish with darker frame.
            _woodDoor  = GetOrMake(MatFolder + "/Door_Wood.mat",   new Color(0.92f, 0.89f, 0.82f), 0f, 0.25f);
            _doorTrim  = GetOrMake(MatFolder + "/Door_Trim.mat",   new Color(0.35f, 0.22f, 0.14f), 0f, 0.3f);
            _doorHandle= GetOrMake(MatFolder + "/Door_Handle.mat", new Color(0.85f, 0.72f, 0.38f), 0.9f, 0.75f);
            _wallStucco= GetOrMake(MatFolder + "/Wall_Stucco.mat", new Color(0.92f, 0.90f, 0.84f), 0f, 0.15f);
            _ceiling   = GetOrMake(MatFolder + "/Ceiling.mat",     new Color(0.96f, 0.95f, 0.92f), 0f, 0.10f);
        }

        static void BuildCeiling()
        {
            var prev = GameObject.Find("ExteriorCeiling");
            if (prev != null) Object.DestroyImmediate(prev);

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "ExteriorCeiling";
            float t = WallThickness;
            go.transform.position = new Vector3(
                (MinX + MaxX) * 0.5f,
                WallHeight + t * 0.5f,
                (MinZ + MaxZ) * 0.5f);
            go.transform.localScale = new Vector3(
                (MaxX - MinX) + t * 2f,
                t,
                (MaxZ - MinZ) + t * 2f);
            go.GetComponent<MeshRenderer>().sharedMaterial = _ceiling;

            var house = GameObject.Find("House");
            if (house != null) go.transform.SetParent(house.transform, worldPositionStays: true);
        }

        static void BuildDoor()
        {
            var existingOld = GameObject.Find("FrontDoorPivot");
            if (existingOld != null) Object.DestroyImmediate(existingOld);

            // Pivot at left edge of the door opening (so it swings around its hinge).
            var pivot = new GameObject("FrontDoorPivot");
            pivot.transform.position = new Vector3(DoorCenterX - DoorWidth * 0.5f, 0f, MinZ);
            pivot.transform.rotation = Quaternion.identity;

            // Panel: child of pivot, offset so the free edge is opposite the hinge.
            var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = "DoorPanel";
            panel.transform.SetParent(pivot.transform, false);
            panel.transform.localPosition = new Vector3(DoorWidth * 0.5f, DoorHeight * 0.5f, 0f);
            panel.transform.localScale = new Vector3(DoorWidth - 0.04f, DoorHeight - 0.04f, 0.05f);
            panel.GetComponent<MeshRenderer>().sharedMaterial = _woodDoor;

            // Trim (darker band around the panel edge).
            var trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trim.name = "DoorTrim";
            trim.transform.SetParent(pivot.transform, false);
            trim.transform.localPosition = new Vector3(DoorWidth * 0.5f, DoorHeight * 0.5f, 0f);
            trim.transform.localScale = new Vector3(DoorWidth, DoorHeight, 0.02f);
            trim.GetComponent<MeshRenderer>().sharedMaterial = _doorTrim;
            // Kill the trim's collider — the panel collider is the thing that pushes the player.
            Object.DestroyImmediate(trim.GetComponent<Collider>());

            // Six-panel interior-door insets (classic upstairs residential look):
            // two rows of two vertical panels each — top pair taller than bottom pair.
            float panelInsetZ = 0.031f;
            float panelW = DoorWidth * 0.38f;
            float panelLeftX = DoorWidth * 0.5f - DoorWidth * 0.22f;
            float panelRightX = DoorWidth * 0.5f + DoorWidth * 0.22f;
            // Top (tall) row.
            MakePanelInset(pivot.transform, "Panel_TL", new Vector3(panelLeftX,  DoorHeight * 0.72f, panelInsetZ), new Vector3(panelW, DoorHeight * 0.34f, 0.01f));
            MakePanelInset(pivot.transform, "Panel_TR", new Vector3(panelRightX, DoorHeight * 0.72f, panelInsetZ), new Vector3(panelW, DoorHeight * 0.34f, 0.01f));
            // Middle row.
            MakePanelInset(pivot.transform, "Panel_ML", new Vector3(panelLeftX,  DoorHeight * 0.40f, panelInsetZ), new Vector3(panelW, DoorHeight * 0.18f, 0.01f));
            MakePanelInset(pivot.transform, "Panel_MR", new Vector3(panelRightX, DoorHeight * 0.40f, panelInsetZ), new Vector3(panelW, DoorHeight * 0.18f, 0.01f));
            // Bottom row.
            MakePanelInset(pivot.transform, "Panel_BL", new Vector3(panelLeftX,  DoorHeight * 0.17f, panelInsetZ), new Vector3(panelW, DoorHeight * 0.18f, 0.01f));
            MakePanelInset(pivot.transform, "Panel_BR", new Vector3(panelRightX, DoorHeight * 0.17f, panelInsetZ), new Vector3(panelW, DoorHeight * 0.18f, 0.01f));

            // Lever-style handle on the free edge of the door, mirrored to both faces
            // so the handle is visible and looks correct whether the player approaches from
            // inside or outside. Handle height is ~1.0m (upstairs-door knob height).
            float handleY = 1.0f;
            float handleX = DoorWidth - 0.09f;                // near free edge (hinge is at x=0)
            BuildLeverHandle(pivot.transform, "Handle_Front", new Vector3(handleX, handleY,  0.03f), +1f);
            BuildLeverHandle(pivot.transform, "Handle_Back",  new Vector3(handleX, handleY, -0.03f), -1f);

            // Door opens via trigger zone + OpenableDoor component on the pivot.
            var openable = pivot.AddComponent<OpenableDoor>();
            openable.hinge = pivot.transform;
            openable.openAngle = 95f;
            openable.duration = 0.5f;
            openable.autoOpenOnTriggerEnter = true;

            var trigger = pivot.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(DoorWidth * 0.5f, DoorHeight * 0.5f, 0.4f);
            trigger.size = new Vector3(DoorWidth * 1.8f, DoorHeight, 1.4f);

            EditorUtility.SetDirty(pivot);
        }

        static void BuildExteriorWalls()
        {
            var prev = GameObject.Find("ExteriorWalls");
            if (prev != null) Object.DestroyImmediate(prev);

            var root = new GameObject("ExteriorWalls");
            var house = GameObject.Find("House");
            if (house != null) root.transform.SetParent(house.transform, worldPositionStays: true);

            // Corner overlap: extend each wall by one thickness on each end so adjacent
            // walls meet with overlap at every corner (no hairline gaps at the seams).
            float t = WallThickness;
            float frontZ = MinZ - t * 0.5f;          // outside face of front wall at Z=MinZ
            float backZ  = MaxZ + t * 0.5f;          // outside face of back wall at Z=MaxZ
            float leftX  = MinX - t * 0.5f;
            float rightX = MaxX + t * 0.5f;

            // --- Front wall (south) — split into two segments around the door gap.
            float doorLeft = DoorCenterX - DoorWidth * 0.5f;
            float doorRight = DoorCenterX + DoorWidth * 0.5f;

            // Extend front-wall segments outward by t so they overlap with the side walls.
            float frontLeftStart = MinX - t;
            float frontRightEnd  = MaxX + t;

            float wallCenterY = (WallHeight - WallFootSink) * 0.5f;
            float wallTotalH = WallHeight + WallFootSink;
            MakeWallSegment(root.transform, "Front_Left",
                new Vector3((frontLeftStart + doorLeft) * 0.5f, wallCenterY, frontZ),
                new Vector3(Mathf.Max(0.1f, doorLeft - frontLeftStart), wallTotalH, t));
            MakeWallSegment(root.transform, "Front_Right",
                new Vector3((doorRight + frontRightEnd) * 0.5f, wallCenterY, frontZ),
                new Vector3(Mathf.Max(0.1f, frontRightEnd - doorRight), wallTotalH, t));
            // Front lintel above door so the gap isn't floor-to-ceiling.
            MakeWallSegment(root.transform, "Front_Lintel",
                new Vector3(DoorCenterX, DoorHeight + (WallHeight - DoorHeight) * 0.5f, frontZ),
                new Vector3(DoorWidth, WallHeight - DoorHeight, t));

            // --- Back wall (north) — extended past both sides to overlap corners.
            MakeWallSegment(root.transform, "Back",
                new Vector3((MinX + MaxX) * 0.5f, wallCenterY, backZ),
                new Vector3((MaxX - MinX) + t * 2f, wallTotalH, t));

            // --- Left wall — full Z span, centered on outer face at leftX.
            MakeWallSegment(root.transform, "Left",
                new Vector3(leftX, wallCenterY, (MinZ + MaxZ) * 0.5f),
                new Vector3(t, wallTotalH, (MaxZ - MinZ) + t * 2f));

            // --- Right wall.
            MakeWallSegment(root.transform, "Right",
                new Vector3(rightX, wallCenterY, (MinZ + MaxZ) * 0.5f),
                new Vector3(t, wallTotalH, (MaxZ - MinZ) + t * 2f));
        }

        // Builds a small backplate (rosette) + a horizontal lever on one face of the door.
        // faceDir = +1 for the outward face, -1 for the inward face.
        static void BuildLeverHandle(Transform parent, string namePrefix, Vector3 rootLocal, float faceDir)
        {
            // Rosette / backplate — a thin disc pressed against the door face.
            var plate = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            plate.name = namePrefix + "_Plate";
            plate.transform.SetParent(parent, false);
            plate.transform.localPosition = rootLocal;
            plate.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // cylinder axis along local Z
            plate.transform.localScale = new Vector3(0.065f, 0.008f, 0.065f);
            plate.GetComponent<MeshRenderer>().sharedMaterial = _doorHandle;
            Object.DestroyImmediate(plate.GetComponent<Collider>());

            // Lever arm — a small horizontal box sticking out perpendicular to the door face.
            // Extends in +X (toward the free edge) so it reads as a pushable lever.
            var lever = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lever.name = namePrefix + "_Lever";
            lever.transform.SetParent(parent, false);
            lever.transform.localPosition = rootLocal + new Vector3(-0.05f, 0f, faceDir * 0.04f);
            lever.transform.localScale = new Vector3(0.11f, 0.022f, 0.022f);
            lever.GetComponent<MeshRenderer>().sharedMaterial = _doorHandle;
            Object.DestroyImmediate(lever.GetComponent<Collider>());

            // Neck — tiny stub connecting the plate to the lever so it reads as one piece.
            var neck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            neck.name = namePrefix + "_Neck";
            neck.transform.SetParent(parent, false);
            neck.transform.localPosition = rootLocal + new Vector3(0f, 0f, faceDir * 0.02f);
            neck.transform.localScale = new Vector3(0.03f, 0.03f, 0.028f);
            neck.GetComponent<MeshRenderer>().sharedMaterial = _doorHandle;
            Object.DestroyImmediate(neck.GetComponent<Collider>());
        }

        static void MakePanelInset(Transform parent, string name, Vector3 localPos, Vector3 localScale)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name;
            g.transform.SetParent(parent, false);
            g.transform.localPosition = localPos;
            g.transform.localScale = localScale;
            g.GetComponent<MeshRenderer>().sharedMaterial = _doorTrim;
            Object.DestroyImmediate(g.GetComponent<Collider>());
        }

        static GameObject MakeWallSegment(Transform parent, string name, Vector3 position, Vector3 size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, worldPositionStays: true);
            go.transform.position = position;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = _wallStucco;
            return go;
        }

        static void PlaceDeliverySpawn()
        {
            var spawn = GameObject.Find("DeliverySpawn");
            if (spawn == null)
            {
                spawn = new GameObject("DeliverySpawn");
            }
            // Outside the front door, on the doorstep. Deliveries arrive here; the player
            // opens the door and carries / gathers what was dropped off.
            spawn.transform.position = new Vector3(DoorCenterX, 0.4f, MinZ - 1.4f);
            spawn.transform.rotation = Quaternion.identity;
            EditorUtility.SetDirty(spawn);

            // Heal the FoodOrderController's deliverySpawn reference — when we Destroy+Create
            // DeliverySpawn the serialized reference on the controller becomes a "missing" link
            // and SpawnDeliveryNow silently early-returns. Rewire every time.
            var foc = Object.FindFirstObjectByType<HackKU.AI.FoodOrderController>();
            if (foc != null)
            {
                var so = new SerializedObject(foc);
                var p = so.FindProperty("deliverySpawn");
                if (p != null) { p.objectReferenceValue = spawn.transform; so.ApplyModifiedProperties(); }
                EditorUtility.SetDirty(foc);
            }
        }

        static Material GetOrMake(string path, Color color, float metallic, float smoothness)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            var leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
