using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HackKU.EditorTools
{
    // Builds the outdoor environment around the house: grass, perimeter fence, a paved road
    // running perpendicular to the house front, a sidewalk connecting road to the front door,
    // and a curb-side mailbox. The road is where a delivery driver will pull up later.
    public static class YardBuilder
    {
        const string MatFolder = "Assets/Materials/Yard";

        // House footprint (must match HouseExteriorBuilder).
        const float HouseMinX = -4.7f;
        const float HouseMaxX =  1.3f;
        const float HouseMinZ = -1.75f;
        const float HouseMaxZ =  9.5f;
        const float DoorX     = -1.7f;  // centered on the front wall (mirrors HouseExteriorBuilder)

        // Yard envelope.
        const float YardMinX = -30f;
        const float YardMaxX =  30f;
        const float YardMinZ = -15f;
        const float YardMaxZ =  20f;

        // Road perpendicular to the house front (runs along X axis), south of the house.
        const float RoadCenterZ = -8f;
        const float RoadHalfWidth = 3.5f;
        const float SidewalkHalfWidth = 0.9f;

        static Material _grass, _asphalt, _roadLine, _sidewalk, _fenceWood, _mailboxRed, _mailboxPost, _dirt;

        [MenuItem("HackKU/Build/Yard (Grass + Fence + Road + Sidewalk)")]
        public static void Build()
        {
            EnsureMaterials();

            // Rescue manually-placed bushes before we nuke the old Yard so rebuilds don't
            // clobber the player's arrangement. The rescued group is re-parented at the end.
            GameObject rescuedBushes = null;
            var prev = GameObject.Find("Yard");
            if (prev != null)
            {
                var oldBushes = prev.transform.Find("Bushes");
                if (oldBushes != null)
                {
                    rescuedBushes = oldBushes.gameObject;
                    rescuedBushes.transform.SetParent(null, true);
                }
                Object.DestroyImmediate(prev);
            }

            var yard = new GameObject("Yard");
            var house = GameObject.Find("House");
            if (house != null) yard.transform.SetParent(house.transform.parent, true);

            BuildGrass(yard.transform);
            BuildRoad(yard.transform);
            BuildSidewalk(yard.transform);
            BuildFence(yard.transform);
            BuildMailbox(yard.transform);
            BuildBushes(yard.transform);
            BuildPlayerBarriers(yard.transform);

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("[YardBuilder] done.");
        }

        static void EnsureMaterials()
        {
            EnsureFolder(MatFolder);
            _grass       = GetOrMake(MatFolder + "/Grass.mat",        new Color(0.33f, 0.55f, 0.25f), 0f, 0.1f);
            _dirt        = GetOrMake(MatFolder + "/Dirt.mat",         new Color(0.42f, 0.30f, 0.18f), 0f, 0.05f);
            _asphalt     = GetOrMake(MatFolder + "/Asphalt.mat",      new Color(0.18f, 0.18f, 0.19f), 0f, 0.15f);
            _roadLine    = GetOrMake(MatFolder + "/RoadLine.mat",     new Color(0.95f, 0.85f, 0.15f), 0f, 0.25f);
            _sidewalk    = GetOrMake(MatFolder + "/Sidewalk.mat",     new Color(0.75f, 0.75f, 0.72f), 0f, 0.15f);
            _fenceWood   = GetOrMake(MatFolder + "/FenceWood.mat",    new Color(0.55f, 0.40f, 0.22f), 0f, 0.3f);
            _mailboxRed  = GetOrMake(MatFolder + "/MailboxRed.mat",   new Color(0.70f, 0.12f, 0.12f), 0.1f, 0.4f);
            _mailboxPost = GetOrMake(MatFolder + "/MailboxPost.mat",  new Color(0.35f, 0.25f, 0.14f), 0f, 0.3f);
        }

        // ---------- grass ----------
        static void BuildGrass(Transform parent)
        {
            // Big flat slab covering the whole yard area, sits just below y=0.
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = "Grass";
            g.transform.SetParent(parent, false);
            g.transform.position = new Vector3(
                (YardMinX + YardMaxX) * 0.5f, -0.05f, (YardMinZ + YardMaxZ) * 0.5f);
            g.transform.localScale = new Vector3(
                YardMaxX - YardMinX, 0.1f, YardMaxZ - YardMinZ);
            g.GetComponent<MeshRenderer>().sharedMaterial = _grass;
        }

        // ---------- road ----------
        static void BuildRoad(Transform parent)
        {
            // Asphalt slab running along X axis at RoadCenterZ.
            var road = GameObject.CreatePrimitive(PrimitiveType.Cube);
            road.name = "Road";
            road.transform.SetParent(parent, false);
            road.transform.position = new Vector3(
                (YardMinX + YardMaxX) * 0.5f, 0.005f, RoadCenterZ);
            road.transform.localScale = new Vector3(
                YardMaxX - YardMinX, 0.06f, RoadHalfWidth * 2f);
            road.GetComponent<MeshRenderer>().sharedMaterial = _asphalt;

            // Center double-yellow line (two dashes).
            int dashCount = 16;
            float roadSpan = YardMaxX - YardMinX;
            float dashLen = roadSpan / (dashCount * 2f);
            for (int i = 0; i < dashCount; i++)
            {
                float cx = YardMinX + (i * 2 + 1) * dashLen;
                var d = GameObject.CreatePrimitive(PrimitiveType.Cube);
                d.name = "LaneDash_" + i;
                d.transform.SetParent(parent, false);
                d.transform.position = new Vector3(cx, 0.04f, RoadCenterZ);
                d.transform.localScale = new Vector3(dashLen * 0.75f, 0.02f, 0.15f);
                d.GetComponent<MeshRenderer>().sharedMaterial = _roadLine;
                Object.DestroyImmediate(d.GetComponent<Collider>());
            }

            // Curbs — thin raised sidewalk slabs along both sides of the road.
            for (int side = -1; side <= 1; side += 2)
            {
                var curb = GameObject.CreatePrimitive(PrimitiveType.Cube);
                curb.name = "Curb_" + (side < 0 ? "S" : "N");
                curb.transform.SetParent(parent, false);
                float cz = RoadCenterZ + side * (RoadHalfWidth + 0.45f);
                curb.transform.position = new Vector3((YardMinX + YardMaxX) * 0.5f, 0.05f, cz);
                curb.transform.localScale = new Vector3(YardMaxX - YardMinX, 0.1f, 0.9f);
                curb.GetComponent<MeshRenderer>().sharedMaterial = _sidewalk;
            }
        }

        // ---------- sidewalk from road to front door ----------
        static void BuildSidewalk(Transform parent)
        {
            float walkZStart = RoadCenterZ + RoadHalfWidth + 0.9f;  // just past the curb
            float walkZEnd   = HouseMinZ;                           // front wall
            float walkCenterZ = (walkZStart + walkZEnd) * 0.5f;
            float walkLenZ = walkZEnd - walkZStart;

            var walk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            walk.name = "Sidewalk";
            walk.transform.SetParent(parent, false);
            walk.transform.position = new Vector3(DoorX, 0.02f, walkCenterZ);
            walk.transform.localScale = new Vector3(SidewalkHalfWidth * 2f, 0.06f, Mathf.Max(0.5f, walkLenZ));
            walk.GetComponent<MeshRenderer>().sharedMaterial = _sidewalk;
        }

        // ---------- perimeter fence with a gap over the sidewalk ----------
        static void BuildFence(Transform parent)
        {
            float fenceMinX = YardMinX + 1f;
            float fenceMaxX = YardMaxX - 1f;
            float fenceSouthZ = RoadCenterZ + RoadHalfWidth + 1.6f;   // just past the curb
            float fenceNorthZ = YardMaxZ - 1f;
            float postSpacing = 2.0f;
            float postHeight = 1.1f;
            float postWidth = 0.12f;

            // South fence (between road and front yard) — split around the sidewalk gap.
            float gapHalf = SidewalkHalfWidth + 0.3f;
            BuildFenceRun(parent, "Fence_S_Left",  fenceMinX, DoorX - gapHalf, fenceSouthZ, postSpacing, postHeight, postWidth, true);
            BuildFenceRun(parent, "Fence_S_Right", DoorX + gapHalf, fenceMaxX, fenceSouthZ, postSpacing, postHeight, postWidth, true);

            // North fence (back of yard).
            BuildFenceRun(parent, "Fence_N", fenceMinX, fenceMaxX, fenceNorthZ, postSpacing, postHeight, postWidth, true);

            // East / West fences — axis along Z, walk with fenceRun oriented along Z.
            BuildFenceRunZ(parent, "Fence_W", fenceMinX, fenceSouthZ, fenceNorthZ, postSpacing, postHeight, postWidth);
            BuildFenceRunZ(parent, "Fence_E", fenceMaxX, fenceSouthZ, fenceNorthZ, postSpacing, postHeight, postWidth);
        }

        // Posts + two horizontal rails running along X from startX to endX at given Z.
        static void BuildFenceRun(Transform parent, string name, float startX, float endX, float z,
                                  float postSpacing, float postHeight, float postWidth, bool includeRails)
        {
            float len = endX - startX;
            if (len <= 0.1f) return;
            var run = new GameObject(name);
            run.transform.SetParent(parent, false);

            int postCount = Mathf.Max(2, Mathf.CeilToInt(len / postSpacing) + 1);
            for (int i = 0; i < postCount; i++)
            {
                float t = postCount == 1 ? 0f : (float)i / (postCount - 1);
                float px = Mathf.Lerp(startX, endX, t);
                var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
                p.name = "Post_" + i;
                p.transform.SetParent(run.transform, false);
                p.transform.position = new Vector3(px, postHeight * 0.5f, z);
                p.transform.localScale = new Vector3(postWidth, postHeight, postWidth);
                p.GetComponent<MeshRenderer>().sharedMaterial = _fenceWood;
            }
            if (includeRails)
            {
                foreach (float yOff in new[] { 0.35f, 0.80f })
                {
                    var r = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    r.name = "Rail_" + yOff.ToString("0.00");
                    r.transform.SetParent(run.transform, false);
                    r.transform.position = new Vector3((startX + endX) * 0.5f, yOff, z);
                    r.transform.localScale = new Vector3(len, 0.06f, 0.04f);
                    r.GetComponent<MeshRenderer>().sharedMaterial = _fenceWood;
                }
            }
        }

        // Fence run along the Z axis at given X.
        static void BuildFenceRunZ(Transform parent, string name, float x, float startZ, float endZ,
                                   float postSpacing, float postHeight, float postWidth)
        {
            float len = endZ - startZ;
            if (len <= 0.1f) return;
            var run = new GameObject(name);
            run.transform.SetParent(parent, false);

            int postCount = Mathf.Max(2, Mathf.CeilToInt(len / postSpacing) + 1);
            for (int i = 0; i < postCount; i++)
            {
                float t = postCount == 1 ? 0f : (float)i / (postCount - 1);
                float pz = Mathf.Lerp(startZ, endZ, t);
                var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
                p.name = "Post_" + i;
                p.transform.SetParent(run.transform, false);
                p.transform.position = new Vector3(x, postHeight * 0.5f, pz);
                p.transform.localScale = new Vector3(postWidth, postHeight, postWidth);
                p.GetComponent<MeshRenderer>().sharedMaterial = _fenceWood;
            }
            foreach (float yOff in new[] { 0.35f, 0.80f })
            {
                var r = GameObject.CreatePrimitive(PrimitiveType.Cube);
                r.name = "Rail_" + yOff.ToString("0.00");
                r.transform.SetParent(run.transform, false);
                r.transform.position = new Vector3(x, yOff, (startZ + endZ) * 0.5f);
                r.transform.localScale = new Vector3(0.04f, 0.06f, len);
                r.GetComponent<MeshRenderer>().sharedMaterial = _fenceWood;
            }
        }

        // ---------- mailbox by the sidewalk ----------
        static void BuildMailbox(Transform parent)
        {
            var root = new GameObject("Mailbox");
            root.transform.SetParent(parent, false);
            float postX = DoorX + SidewalkHalfWidth + 0.35f;
            float postZ = RoadCenterZ + RoadHalfWidth + 1.3f;
            root.transform.position = new Vector3(postX, 0f, postZ);

            var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.name = "Post";
            post.transform.SetParent(root.transform, false);
            post.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            post.transform.localScale = new Vector3(0.08f, 1.1f, 0.08f);
            post.GetComponent<MeshRenderer>().sharedMaterial = _mailboxPost;

            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "Box";
            box.transform.SetParent(root.transform, false);
            box.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            box.transform.localScale = new Vector3(0.22f, 0.18f, 0.34f);
            box.GetComponent<MeshRenderer>().sharedMaterial = _mailboxRed;

            // Little flag.
            var flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flag.name = "Flag";
            flag.transform.SetParent(root.transform, false);
            flag.transform.localPosition = new Vector3(0.13f, 1.25f, 0.12f);
            flag.transform.localScale = new Vector3(0.04f, 0.1f, 0.08f);
            flag.GetComponent<MeshRenderer>().sharedMaterial = _mailboxRed;
        }

        // ---------- scattering a few bushes for life ----------
        static void BuildBushes(Transform parent)
        {
            // If the user has already placed bushes manually, leave them alone. We look for
            // a stray "Bushes" group anywhere in the scene (since the previous Yard was just
            // destroyed, any preserved Bushes would be unparented / re-parented on disk).
            var existing = GameObject.Find("Bushes");
            if (existing != null)
            {
                existing.transform.SetParent(parent, true);
                Debug.Log("[YardBuilder] existing Bushes preserved; skipping procedural generation.");
                return;
            }

            var bushes = new GameObject("Bushes");
            bushes.transform.SetParent(parent, false);
            var bushMat = GetOrMake(MatFolder + "/Bush.mat", new Color(0.22f, 0.42f, 0.18f), 0f, 0.2f);

            void Bush(float x, float z, float r)
            {
                var b = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                b.name = "Bush";
                b.transform.SetParent(bushes.transform, false);
                b.transform.position = new Vector3(x, r * 0.9f, z);
                b.transform.localScale = new Vector3(r * 2f, r * 1.6f, r * 2f);
                b.GetComponent<MeshRenderer>().sharedMaterial = bushMat;
                Object.DestroyImmediate(b.GetComponent<Collider>());
            }

            // Flanking the front door.
            Bush(HouseMinX - 0.3f, HouseMinZ - 0.6f, 0.45f);
            Bush(HouseMaxX + 0.3f, HouseMinZ - 0.6f, 0.45f);

            // Sprinkle on either side of the sidewalk.
            Bush(DoorX - 2.2f, RoadCenterZ + RoadHalfWidth + 2.6f, 0.4f);
            Bush(DoorX + 2.2f, RoadCenterZ + RoadHalfWidth + 2.6f, 0.5f);
            Bush(DoorX - 3.5f, RoadCenterZ + RoadHalfWidth + 4.6f, 0.6f);
            Bush(DoorX + 3.5f, RoadCenterZ + RoadHalfWidth + 4.6f, 0.5f);

            // A small grove back corner of yard.
            Bush(YardMinX + 4f, YardMaxZ - 3f, 0.7f);
            Bush(YardMinX + 5.2f, YardMaxZ - 4.2f, 0.55f);
            Bush(YardMaxX - 4f, YardMaxZ - 3f, 0.65f);
        }

        // ---------- invisible player barriers ----------
        // Confines the player to the house interior + a small porch area directly outside
        // the front door — enough to step out and grab the delivery box, but blocks them from
        // walking onto the road or wandering into the grass.
        static void BuildPlayerBarriers(Transform parent)
        {
            var root = new GameObject("PlayerBarriers");
            root.transform.SetParent(parent, false);

            // Porch corridor just in front of the door.
            float porchMinX = DoorX - 1.6f;   // slightly wider than door
            float porchMaxX = DoorX + 1.6f;
            float porchFarZ = -4.5f;   // how far south the player can step out
            float wallTop   = 3.0f;    // height of invisible wall
            float thick     = 0.3f;

            // Front stop wall (south of the porch).
            MakeBarrier(root.transform, "Porch_S",
                new Vector3((porchMinX + porchMaxX) * 0.5f, wallTop * 0.5f, porchFarZ),
                new Vector3(porchMaxX - porchMinX + thick, wallTop, thick));

            // Left side wall of porch — from porchFarZ up to the house front wall.
            MakeBarrier(root.transform, "Porch_W",
                new Vector3(porchMinX, wallTop * 0.5f, (porchFarZ + HouseMinZ) * 0.5f),
                new Vector3(thick, wallTop, HouseMinZ - porchFarZ));

            // Right side wall of porch.
            MakeBarrier(root.transform, "Porch_E",
                new Vector3(porchMaxX, wallTop * 0.5f, (porchFarZ + HouseMinZ) * 0.5f),
                new Vector3(thick, wallTop, HouseMinZ - porchFarZ));

            // Seal off the rest of the front wall (outside the door frame) so the player can't
            // scoot around the porch edges along the south face of the house.
            MakeBarrier(root.transform, "FrontLeft_Outer",
                new Vector3((HouseMinX + porchMinX) * 0.5f - 0.2f, wallTop * 0.5f, HouseMinZ - 0.3f),
                new Vector3(porchMinX - HouseMinX + 1.5f, wallTop, thick));
            MakeBarrier(root.transform, "FrontRight_Outer",
                new Vector3((HouseMaxX + porchMaxX) * 0.5f + 0.2f, wallTop * 0.5f, HouseMinZ - 0.3f),
                new Vector3(HouseMaxX - porchMaxX + 1.5f, wallTop, thick));
        }

        static void MakeBarrier(Transform parent, string name, Vector3 position, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = size;
            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = false;
            // No MeshRenderer — barrier is invisible. Collider still blocks the CharacterController.
        }

        static Material GetOrMake(string path, Color c, float metallic, float smoothness)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.shader = shader;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(m);
            return m;
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
