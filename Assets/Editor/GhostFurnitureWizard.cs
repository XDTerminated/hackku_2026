using HackKU.Core;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HackKU.EditorTools
{
    // Quick wizard to turn selected scene objects into GhostFurnitureItem items, and to
    // drop an InvestmentBoard ghost on the wall to the left of the phone.
    public static class GhostFurnitureWizard
    {
        // ----- Selection helpers ---------------------------------------------

        [MenuItem("HackKU/Ghosts/Make Selected a Ghost Couch ($200, +10%)")]
        public static void MakeCouch() => Apply("couch", "Couch", 200f, 0.10f);

        [MenuItem("HackKU/Ghosts/Make Selected a Ghost Table ($120, +5%)")]
        public static void MakeTable() => Apply("table", "Table", 120f, 0.05f);

        [MenuItem("HackKU/Ghosts/Make Selected a Ghost TV ($450, +15%)")]
        public static void MakeTV() => Apply("tv", "TV", 450f, 0.15f);

        [MenuItem("HackKU/Ghosts/Make Selected a Ghost Bed ($350, +12%)")]
        public static void MakeBed() => Apply("bed", "Bed", 350f, 0.12f);

        [MenuItem("HackKU/Ghosts/Make Selected a Ghost Shower ($400, +0%)")]
        public static void MakeShower() => Apply("shower", "Shower", 400f, 0f);

        static void Apply(string id, string name, float price, float bonus)
        {
            foreach (var go in Selection.gameObjects)
            {
                var g = go.GetComponent<GhostFurnitureItem>();
                if (g == null) g = go.AddComponent<GhostFurnitureItem>();
                g.itemId = id;
                g.displayName = name;
                g.price = price;
                g.happinessBonus = bonus;
                EditorUtility.SetDirty(g);
            }
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log($"[Ghosts] Tagged {Selection.gameObjects.Length} object(s) as {name}.");
        }

        // ----- Bulk auto-tag House furniture --------------------------------

        // Removes GhostFurnitureItem from objects named like floor/wall/door, or matching
        // pCube22..pCube37 and pCube133..pCube142 (the structural pieces the user flagged).
        [MenuItem("HackKU/Ghosts/Un-Ghost Structural (floor, wall, door, pCube ranges)")]
        public static void UntagStructuralSpecific()
        {
            var house = GameObject.Find("House");
            if (house == null) { Debug.LogError("[Ghosts] no House"); return; }

            var rangeBlock = new System.Collections.Generic.HashSet<string>();
            for (int i = 22; i <= 37; i++) rangeBlock.Add("pcube" + i);
            for (int i = 133; i <= 142; i++) rangeBlock.Add("pcube" + i);

            string[] nameContains = { "floor", "wall", "door" };

            int removed = 0;
            foreach (Transform child in house.transform)
            {
                string lower = child.name.ToLowerInvariant();
                bool hit = rangeBlock.Contains(lower);
                if (!hit) foreach (var s in nameContains) if (lower.Contains(s)) { hit = true; break; }
                if (!hit) continue;
                var g = child.GetComponent<GhostFurnitureItem>();
                if (g == null) continue;
                Object.DestroyImmediate(g);
                removed++;
            }
            EditorSceneManager.MarkSceneDirty(house.scene);
            Debug.Log($"[Ghosts] Un-ghosted {removed} structural pieces.");
        }

        // Renames every ghost-tagged object in the House using a bounds-based heuristic so
        // "pCube41" becomes "Chair" etc. Also refreshes the GhostFurnitureItem displayName.
        [MenuItem("HackKU/Ghosts/Rename Ghosts By Shape")]
        public static void RenameByShape()
        {
            var house = GameObject.Find("House");
            if (house == null) { Debug.LogError("[Ghosts] no House"); return; }
            int renamed = 0;
            foreach (var g in house.GetComponentsInChildren<GhostFurnitureItem>(true))
            {
                var label = ClassifyByBounds(g.transform);
                g.displayName = label;
                g.gameObject.name = label + "_" + g.GetInstanceID();
                EditorUtility.SetDirty(g);
                renamed++;
            }
            EditorSceneManager.MarkSceneDirty(house.scene);
            Debug.Log($"[Ghosts] Renamed {renamed} ghosts via shape heuristic.");
        }

        // Very rough classifier — look at the world-space bounding box.
        static string ClassifyByBounds(Transform t)
        {
            bool first = true;
            Bounds b = new Bounds(t.position, Vector3.zero);
            foreach (var r in t.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (r == null) continue;
                if (first) { b = r.bounds; first = false; }
                else b.Encapsulate(r.bounds);
            }
            Vector3 s = b.size;
            float w = s.x, h = s.y, d = s.z;
            float longest = Mathf.Max(w, Mathf.Max(h, d));
            float shortest = Mathf.Max(0.001f, Mathf.Min(w, Mathf.Min(h, d)));
            float aspect = longest / shortest;
            float volume = w * h * d;

            // Lamp: tall + narrow.
            if (h > 0.8f && Mathf.Max(w, d) < 0.35f) return "Lamp";
            // Very thin + wide → Rug or Painting.
            if (h < 0.12f && Mathf.Max(w, d) > 0.8f) return "Rug";
            if (d < 0.12f && h > 0.5f && w > 0.6f) return "Painting";
            // TV: wide + tall + shallow.
            if (w > 0.9f && h > 0.5f && d < 0.2f) return "TV";
            // Bed: very wide + moderately tall + deep.
            if (w > 1.6f && d > 1.2f && h < 1.0f) return "Bed";
            // Couch: wide + moderate depth + low.
            if (w > 1.4f && d > 0.6f && h < 1.0f) return "Couch";
            // Table / desk: medium wide + low-to-mid height.
            if (w > 0.8f && d > 0.5f && h < 0.9f) return "Table";
            // Chair / stool: small-to-medium cubic.
            if (h > 0.45f && h < 1.3f && w < 0.8f && d < 0.8f) return "Chair";
            // Shelf: narrow depth, tall.
            if (d < 0.5f && h > 1.2f && w > 0.5f) return "Shelf";
            // Cabinet: mid-size box.
            if (h > 0.6f && h < 1.8f && w > 0.5f && d > 0.3f) return "Cabinet";
            // Plant: small, moderately tall.
            if (volume < 0.3f && h > 0.25f) return "Plant";
            return "Furniture";
        }

        // Adds bouncing-arrow markers above the Investment Board and Shower ghosts so the
        // player is visually guided to the unlock-critical items first.
        // Drops a short "breadcrumb" trail of bouncing arrows between the XR Origin and the
        // shower ghost so the player knows which way to walk (up the stairs). Each breadcrumb
        // targets the same shower ghost, so all of them disappear the moment shower is bought.
        [MenuItem("HackKU/Ghosts/Build Path To Shower")]
        public static void BuildPathToShower()
        {
            var rig = GameObject.Find("XR Origin (XR Rig)") ?? GameObject.Find("XR Origin");
            if (rig == null) { Debug.LogError("[Ghosts] no XR Origin"); return; }
            HackKU.Core.GhostFurnitureItem shower = null;
            foreach (var g in Object.FindObjectsByType<HackKU.Core.GhostFurnitureItem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (g != null && (g.itemId ?? "").ToLowerInvariant() == "shower") { shower = g; break; }
            if (shower == null) { Debug.LogError("[Ghosts] no shower ghost in scene"); return; }

            // Wipe previous trail.
            var prev = GameObject.Find("[PathToShower]");
            if (prev != null) Object.DestroyImmediate(prev);

            var root = new GameObject("[PathToShower]");

            Vector3 start = rig.transform.position;
            Vector3 end = shower.transform.position;
            const int count = 10;
            for (int i = 1; i <= count; i++)
            {
                float t = i / (float)(count + 1);
                // Interpolate X/Z linearly but for Y we raycast DOWN to find the floor/stair
                // surface. That way the trail climbs real stair steps instead of clipping through.
                Vector3 lateral = new Vector3(Mathf.Lerp(start.x, end.x, t),
                                              Mathf.Lerp(start.y, end.y, t) + 3f,
                                              Mathf.Lerp(start.z, end.z, t));
                float groundY;
                if (Physics.Raycast(lateral, Vector3.down, out var hit, 20f))
                    groundY = hit.point.y;
                else
                    groundY = Mathf.Lerp(start.y, end.y, t);
                Vector3 p = new Vector3(lateral.x, groundY + 0.9f, lateral.z);

                var node = new GameObject("Breadcrumb_" + i);
                node.transform.SetParent(root.transform, true);
                node.transform.position = p;
                var m = node.AddComponent<HackKU.Core.ObjectiveMarker>();
                m.target = shower;
                m.label = "SHOWER";
                m.arrowColor = new Color(0.5f, 0.85f, 1f);
                m.bobAmplitude = 0.06f;
                m.bobSpeed = 3.5f;
                m.heightAboveTarget = 0.25f;
                m.useSelfPosition = true;
                m.scale = 0.45f;
            }
            EditorSceneManager.MarkSceneDirty(rig.scene);
            Debug.Log($"[Ghosts] Built {count}-step shower path.");
        }

        [MenuItem("HackKU/Ghosts/Add Objective Arrows (Board + Shower)")]
        public static void AddObjectiveArrows()
        {
            int added = 0;
            foreach (var g in Object.FindObjectsByType<GhostFurnitureItem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (g == null) continue;
                string id = (g.itemId ?? "").ToLowerInvariant();
                string label;
                if (id == "investment_board") label = "BUY STOCKS";
                else if (id == "shower") label = "BUY SHOWER";
                else continue;
                var marker = g.GetComponent<HackKU.Core.ObjectiveMarker>();
                if (marker == null) marker = g.gameObject.AddComponent<HackKU.Core.ObjectiveMarker>();
                marker.target = g;
                marker.label = label;
                marker.scale = (id == "shower") ? 0.35f : 1f;
                EditorUtility.SetDirty(marker);
                added++;
            }
            if (Object.FindObjectsByType<GhostFurnitureItem>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0)
                EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log($"[Ghosts] Added objective arrows to {added} critical items.");
        }

        [MenuItem("HackKU/Ghosts/Un-Ghost Selected Objects")]
        public static void UntagSelected()
        {
            int removed = 0;
            foreach (var go in Selection.gameObjects)
            {
                var g = go.GetComponent<GhostFurnitureItem>();
                if (g == null) continue;
                Object.DestroyImmediate(g);
                removed++;
            }
            if (Selection.gameObjects.Length > 0)
                EditorSceneManager.MarkSceneDirty(Selection.gameObjects[0].scene);
            Debug.Log($"[Ghosts] Un-ghosted {removed} selected object(s).");
        }

        [MenuItem("HackKU/Ghosts/Un-Tag Structural Pieces")]
        public static void UntagStructural()
        {
            var house = GameObject.Find("House");
            if (house == null) { Debug.LogError("[Ghosts] no House"); return; }
            string[] skipContains = {
                "wall", "ceiling", "floor", "flooring", "door", "window", "roof",
                "exteriorwalls", "stair", "stairs", "step", "banister", "railing",
                "pillar", "column", "beam", "balcony"
            };
            int removed = 0;
            foreach (Transform child in house.transform)
            {
                string lower = child.name.ToLowerInvariant();
                bool isStructural = false;
                foreach (var s in skipContains) if (lower.Contains(s)) { isStructural = true; break; }
                if (!isStructural) continue;
                var g = child.GetComponent<GhostFurnitureItem>();
                if (g == null) continue;
                Object.DestroyImmediate(g);
                removed++;
            }
            EditorSceneManager.MarkSceneDirty(house.scene);
            Debug.Log($"[Ghosts] Stripped GhostFurnitureItem from {removed} structural pieces.");
        }

        [MenuItem("HackKU/Ghosts/Auto-Tag All House Furniture")]
        public static void AutoTagHouseFurniture()
        {
            var house = GameObject.Find("House");
            if (house == null) { Debug.LogError("[Ghosts] no House in scene"); return; }

            // Skip things obviously not furniture (walls, ceiling, floor, exterior shell, stairs).
            string[] skipContains = {
                "wall", "ceiling", "floor", "flooring", "door", "window", "roof",
                "exteriorwalls", "stair", "stairs", "step", "banister", "railing",
                "pillar", "column", "beam", "balcony"
            };

            int tagged = 0, skipped = 0;
            foreach (Transform child in house.transform)
            {
                string lower = child.name.ToLowerInvariant();
                bool skip = false;
                foreach (var s in skipContains) if (lower.Contains(s)) { skip = true; break; }
                if (skip) { skipped++; continue; }

                // Special-case skip.
                if (child.GetComponent<HackKU.Core.RotaryPhone>() != null) continue;
                if (child.name == "InvestmentBoard") continue;
                // If already tagged, just refresh its friendly name + leave price/bonus alone.
                var existingGf = child.GetComponent<GhostFurnitureItem>();
                if (existingGf != null)
                {
                    existingGf.displayName = FriendlyName(child.name);
                    EditorUtility.SetDirty(existingGf);
                    tagged++;
                    continue;
                }

                // Need a visible mesh to be interesting.
                var renderers = child.GetComponentsInChildren<MeshRenderer>(true);
                if (renderers.Length == 0) { skipped++; continue; }

                // Bounds-based auto-pricing: bigger = pricier + bigger happiness bonus.
                Bounds b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
                float volume = b.size.x * b.size.y * b.size.z;
                float price = Mathf.Round(Mathf.Clamp(80f + volume * 2f, 60f, 600f));
                // Keep per-item bonuses small so buying lots doesn't break happiness.
                float bonus = Mathf.Clamp(0.01f + volume * 0.006f, 0.01f, 0.06f);

                var g = child.gameObject.AddComponent<GhostFurnitureItem>();
                g.itemId = "furniture_" + child.name.ToLowerInvariant().Replace(" ", "_");
                g.displayName = FriendlyName(child.name);
                g.price = price;
                g.happinessBonus = bonus;
                EditorUtility.SetDirty(g);
                tagged++;
            }
            EditorSceneManager.MarkSceneDirty(house.scene);
            Debug.Log($"[Ghosts] Auto-tagged {tagged} furniture pieces ({skipped} skipped).");
        }

        static readonly (string key, string label)[] _nameTable =
        {
            ("sofa","Sofa"), ("couch","Couch"), ("chair","Chair"), ("stool","Stool"),
            ("bench","Bench"), ("armchair","Armchair"), ("recliner","Recliner"),
            ("tv","TV"), ("television","TV"), ("screen","TV"), ("monitor","Monitor"),
            ("table","Table"), ("desk","Desk"), ("shelf","Shelf"), ("bookshelf","Bookshelf"),
            ("bed","Bed"), ("mattress","Bed"), ("nightstand","Nightstand"),
            ("lamp","Lamp"), ("light","Lamp"),
            ("rug","Rug"), ("carpet","Rug"),
            ("fridge","Fridge"), ("oven","Oven"), ("stove","Stove"), ("sink","Sink"),
            ("microwave","Microwave"), ("toaster","Toaster"),
            ("cabinet","Cabinet"), ("drawer","Drawer"), ("dresser","Dresser"),
            ("plant","Plant"), ("vase","Vase"), ("mirror","Mirror"), ("clock","Clock"),
            ("shower","Shower"), ("toilet","Toilet"), ("bathtub","Bathtub"), ("tub","Bathtub"),
            ("picture","Painting"), ("painting","Painting"), ("frame","Painting"),
            ("cushion","Cushion"), ("pillow","Pillow"),
        };

        static string FriendlyName(string raw)
        {
            string lower = raw.ToLowerInvariant();
            foreach (var pair in _nameTable) if (lower.Contains(pair.key)) return pair.label;
            // Fallback — strip digits and tidy up.
            string name = System.Text.RegularExpressions.Regex.Replace(raw, @"\d+$", "").Replace("_", " ").Trim();
            if (string.IsNullOrEmpty(name)) name = "Furniture";
            return char.ToUpper(name[0]) + name.Substring(1);
        }

        // ----- Investment Board ----------------------------------------------

        [MenuItem("HackKU/Ghosts/Build Investment Board Ghost")]
        public static void BuildInvestmentBoard()
        {
            var phone = GameObject.Find("RotaryPhone");
            if (phone == null) { Debug.LogError("[Ghosts] no RotaryPhone; can't anchor the board."); return; }

            // Place to the left of the phone along the same wall.
            Vector3 boardPos = phone.transform.position + phone.transform.up * 0.05f
                             + Vector3.forward * 1.4f;

            var existing = GameObject.Find("InvestmentBoard");
            if (existing != null) Object.DestroyImmediate(existing);

            var root = new GameObject("InvestmentBoard");
            root.transform.position = boardPos;
            root.transform.rotation = phone.transform.rotation;

            // --- Frame & screen panel ----------------------------------------
            var frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Frame";
            frame.transform.SetParent(root.transform, false);
            frame.transform.localScale = new Vector3(0.95f, 0.65f, 0.06f);
            frame.transform.localPosition = Vector3.zero;
            var frameMat = GetOrMake("Assets/Materials/Exterior/BoardFrame.mat", new Color(0.15f, 0.17f, 0.20f), 0.2f, 0.35f);
            frame.GetComponent<MeshRenderer>().sharedMaterial = frameMat;

            var screen = GameObject.CreatePrimitive(PrimitiveType.Cube);
            screen.name = "Screen";
            screen.transform.SetParent(root.transform, false);
            screen.transform.localScale = new Vector3(0.90f, 0.60f, 0.055f);
            screen.transform.localPosition = new Vector3(0f, 0f, 0.005f);
            var screenMat = GetOrMake("Assets/Materials/Exterior/BoardScreen.mat", new Color(0.04f, 0.07f, 0.10f), 0.1f, 0.6f);
            screen.GetComponent<MeshRenderer>().sharedMaterial = screenMat;
            Object.DestroyImmediate(screen.GetComponent<Collider>());

            // --- World-space UI canvas for the live display ------------------
            // The canvas is parented to a dedicated front-facing anchor whose world forward
            // is computed from the root's +Z (the room-facing side of the board).
            // Doing it this way dodges all the Y-rotation quirks that kept making the text
            // read backwards — we just slap the canvas on a transform that already points
            // toward the room and let TMP render forward from there.
            var canvasGo = new GameObject("DisplayCanvas", typeof(RectTransform));
            canvasGo.transform.SetParent(root.transform, false);
            canvasGo.transform.localPosition = new Vector3(0f, 0f, 0.035f);
            canvasGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var canvasRt = (RectTransform)canvasGo.transform;
            canvasRt.sizeDelta = new Vector2(900, 600);
            canvasGo.transform.localScale = Vector3.one * 0.001f;

            // Title row
            var title = AddTMP(canvasRt, "Title", "LIVE MARKET", 54f, FontStyles.Bold,
                               new Color(0.55f, 0.85f, 1f), TextAlignmentOptions.Center);
            PlaceRect(title.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -90), new Vector2(-20, -20));

            // Balance (big)
            var balance = AddTMP(canvasRt, "Balance", "$0", 130f, FontStyles.Bold,
                                 Color.white, TextAlignmentOptions.Center);
            PlaceRect(balance.rectTransform, new Vector2(0, 0.55f), new Vector2(1, 0.88f),
                      new Vector2(20, 0), new Vector2(-20, 0));

            // Delta
            var delta = AddTMP(canvasRt, "Delta", "—", 56f, FontStyles.Bold,
                               new Color(0.8f, 0.8f, 0.85f), TextAlignmentOptions.Center);
            PlaceRect(delta.rectTransform, new Vector2(0, 0.35f), new Vector2(1, 0.55f),
                      new Vector2(20, 0), new Vector2(-20, 0));

            // Ticker strip
            var ticker = AddTMP(canvasRt, "Ticker", "MKT FLAT", 32f, FontStyles.Bold,
                                new Color(0.85f, 0.85f, 0.9f), TextAlignmentOptions.Center);
            PlaceRect(ticker.rectTransform, new Vector2(0, 0.22f), new Vector2(1, 0.34f),
                      new Vector2(20, 0), new Vector2(-20, 0));

            // Sparkline area
            var sparkHolder = new GameObject("Sparkline", typeof(RectTransform), typeof(UnityEngine.UI.RectMask2D));
            sparkHolder.transform.SetParent(canvasRt, false);
            var sparkRt = (RectTransform)sparkHolder.transform;
            PlaceRect(sparkRt, new Vector2(0, 0), new Vector2(1, 0.2f),
                      new Vector2(40, 20), new Vector2(-40, 0));

            // --- Live display component --------------------------------------
            var disp = root.AddComponent<HackKU.Core.InvestmentBoardDisplay>();
            disp.balanceText = balance;
            disp.deltaText = delta;
            disp.tickerText = ticker;
            disp.sparklineParent = sparkRt;

            // --- Ghost component (gating) ------------------------------------
            var g = root.AddComponent<GhostFurnitureItem>();
            g.itemId = "investment_board";
            g.displayName = "Investment Board";
            g.price = 300f;
            g.happinessBonus = 0f;

            EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log("[Ghosts] Built InvestmentBoard (live tracker) at " + boardPos);
        }

        static TextMeshProUGUI AddTMP(Transform parent, string name, string text, float size,
                                      FontStyles style, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            t.alignment = align;
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Overflow;
            return t;
        }

        static void PlaceRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        // ----- Attach GhostPurchaseHand to XR controllers --------------------

        [MenuItem("HackKU/Ghosts/Attach Purchase Hand To Right Controller")]
        public static void AttachHand()
        {
            var rig = GameObject.Find("XR Origin (XR Rig)") ?? GameObject.Find("XR Origin");
            if (rig == null) { Debug.LogError("[Ghosts] no XR Origin"); return; }
            var right = rig.transform.Find("Camera Offset/Right Controller") ?? rig.transform.Find("RightHand Controller");
            if (right == null) { right = FindDeep(rig.transform, "Right Controller"); }
            if (right == null) { Debug.LogError("[Ghosts] couldn't locate Right Controller"); return; }
            if (right.GetComponent<GhostPurchaseHand>() == null) right.gameObject.AddComponent<GhostPurchaseHand>();
            EditorUtility.SetDirty(right);
            EditorSceneManager.MarkSceneDirty(right.gameObject.scene);
            Debug.Log("[Ghosts] GhostPurchaseHand attached to " + right.name);
        }

        static Transform FindDeep(Transform root, string nameContains)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name.Contains(nameContains)) return t;
            return null;
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
    }
}
