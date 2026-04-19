using HackKU.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HackKU.EditorTools
{
    // Wall-mounts the RotaryPhone: moves it to the west interior wall, rotates to face
    // into the room, builds a plain wall-phone mounting plate, sets up a spiral cord
    // LineRenderer between cradle and handset, and tethers the player so they can't
    // walk past the cord's max length while holding the handset.
    public static class WallPhoneBuilder
    {
        // Phone position/rotation is set externally (via MCP / inspector). This builder only
        // adds the wall-phone parts: plate, cord anchors, spiral LineRenderer, player tether.
        const float CordMaxLength = 4.0f;

        [MenuItem("HackKU/Build/Convert Phone to Wall Phone")]
        public static void Build()
        {
            var phoneGO = GameObject.Find("RotaryPhone");
            if (phoneGO == null) { Debug.LogError("[WallPhoneBuilder] no RotaryPhone in scene"); return; }

            // Phone transform is positioned outside this script. We just wire up the parts.

            // 2. Find the handset child.
            var handsetCtrl = phoneGO.GetComponentInChildren<HandsetController>();
            if (handsetCtrl == null) { Debug.LogError("[WallPhoneBuilder] no HandsetController on phone"); return; }

            // 3. Ensure a cradle anchor exists — a small empty on the phone body where the cord enters.
            var cradleAnchor = phoneGO.transform.Find("CordAnchor_Cradle");
            if (cradleAnchor == null)
            {
                var a = new GameObject("CordAnchor_Cradle");
                a.transform.SetParent(phoneGO.transform, false);
                // Slightly offset from the phone body — roughly where a real wall-phone cord attaches.
                a.transform.localPosition = new Vector3(0f, -0.04f, 0.08f);
                cradleAnchor = a.transform;
            }

            // 4. Ensure a handset anchor exists — small empty on the handset where the cord meets it.
            var handsetAnchor = handsetCtrl.transform.Find("CordAnchor_Handset");
            if (handsetAnchor == null)
            {
                var a = new GameObject("CordAnchor_Handset");
                a.transform.SetParent(handsetCtrl.transform, false);
                a.transform.localPosition = new Vector3(0f, -0.04f, 0f);
                handsetAnchor = a.transform;
            }

            // 5. Add or reuse a "Cord" child with LineRenderer + PhoneCord script.
            var cordGO = phoneGO.transform.Find("Cord")?.gameObject;
            if (cordGO == null)
            {
                cordGO = new GameObject("Cord");
                cordGO.transform.SetParent(phoneGO.transform, false);
            }
            var lr = cordGO.GetComponent<LineRenderer>();
            if (lr == null) lr = cordGO.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.startWidth = 0.012f;
            lr.endWidth = 0.012f;
            lr.numCornerVertices = 2;
            lr.numCapVertices = 2;
            lr.material = GetOrMakeCordMaterial();
            lr.startColor = Color.black;
            lr.endColor = Color.black;

            var cord = cordGO.GetComponent<PhoneCord>();
            if (cord == null) cord = cordGO.AddComponent<PhoneCord>();
            cord.cradleAnchor = cradleAnchor;
            cord.handsetAnchor = handsetAnchor;
            cord.maxLength = CordMaxLength;
            cord.handset = handsetCtrl;

            // 6. Player tether — add on the phone (cheap place to host it; it auto-finds XR Origin).
            var tether = phoneGO.GetComponent<PhoneCordTether>();
            if (tether == null) tether = phoneGO.AddComponent<PhoneCordTether>();
            tether.cradleAnchor = cradleAnchor;
            tether.handset = handsetCtrl;
            tether.maxDistance = CordMaxLength;

            // 7. Hide any pre-existing old rotary-phone visual children so the new wall-phone
            //    model doesn't overlap them. Preserve anything functional (Handset, Cord,
            //    anchors, previous WallPlate, new WallPhoneVisual).
            foreach (Transform child in phoneGO.transform)
            {
                string n = child.name;
                if (n.StartsWith("Handset")) continue;
                if (n == "Cord" || n.StartsWith("CordAnchor")) continue;
                if (n == "WallPhoneVisual" || n == "WallPlate") continue;
                foreach (var r in child.GetComponentsInChildren<MeshRenderer>(true))
                    r.enabled = false;
            }

            // 8. Build the new wall-phone visual — body + keypad + earpiece cradle hook.
            var oldVis = phoneGO.transform.Find("WallPhoneVisual");
            if (oldVis != null) Object.DestroyImmediate(oldVis.gameObject);
            BuildWallPhoneVisual(phoneGO.transform);

            // Reposition the cradle cord-anchor to the bottom of the new body.
            cradleAnchor.localPosition = new Vector3(0f, -0.22f, 0.05f);

            EditorUtility.SetDirty(phoneGO);
            EditorSceneManager.MarkSceneDirty(phoneGO.scene);
            Debug.Log("[WallPhoneBuilder] phone wall-mounted with spiral cord + player tether.");
        }

        static void BuildWallPhoneVisual(Transform phone)
        {
            var root = new GameObject("WallPhoneVisual");
            root.transform.SetParent(phone, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;

            var bodyMat    = GetOrMakeBodyMaterial();
            var dialFaceMat = GetOrMakeButtonMaterial();  // cream dial face
            var dialHoleMat = GetOrMakeDialHoleMaterial(); // dark finger holes
            var hookMat    = GetOrMakeHookMaterial();

            // Rectangular wall body — shorter and wider than a desk phone, like a classic
            // wall rotary mounted plate. Local +Z points out from the wall.
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 0f, 0.02f);
            body.transform.localScale = new Vector3(0.28f, 0.42f, 0.09f);
            body.GetComponent<MeshRenderer>().sharedMaterial = bodyMat;
            Object.DestroyImmediate(body.GetComponent<Collider>());

            // Twin ringer bells on top — classic rotary look.
            for (int side = -1; side <= 1; side += 2)
            {
                var bell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bell.name = "Bell_" + (side < 0 ? "L" : "R");
                bell.transform.SetParent(root.transform, false);
                bell.transform.localPosition = new Vector3(side * 0.075f, 0.23f, 0.04f);
                bell.transform.localScale = new Vector3(0.085f, 0.07f, 0.085f);
                bell.GetComponent<MeshRenderer>().sharedMaterial = hookMat;
                Object.DestroyImmediate(bell.GetComponent<Collider>());
            }

            // Rotary dial — flat disc face on the front of the body, slightly recessed.
            var dialFace = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dialFace.name = "DialFace";
            dialFace.transform.SetParent(root.transform, false);
            dialFace.transform.localPosition = new Vector3(0f, -0.01f, 0.075f);
            dialFace.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            dialFace.transform.localScale = new Vector3(0.20f, 0.01f, 0.20f);
            dialFace.GetComponent<MeshRenderer>().sharedMaterial = dialFaceMat;
            Object.DestroyImmediate(dialFace.GetComponent<Collider>());

            // Center hub of the dial.
            var dialHub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dialHub.name = "DialHub";
            dialHub.transform.SetParent(root.transform, false);
            dialHub.transform.localPosition = new Vector3(0f, -0.01f, 0.09f);
            dialHub.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            dialHub.transform.localScale = new Vector3(0.05f, 0.008f, 0.05f);
            dialHub.GetComponent<MeshRenderer>().sharedMaterial = hookMat;
            Object.DestroyImmediate(dialHub.GetComponent<Collider>());

            // 10 finger holes around the dial: start from roughly 1-o'clock and step clockwise.
            int holeCount = 10;
            float holeRingRadius = 0.072f;
            for (int i = 0; i < holeCount; i++)
            {
                // Sweep from about 300° down to 60° (skipping the 60-300 bottom arc, like a real dial).
                float angle = Mathf.Lerp(-60f, -300f, i / (float)(holeCount - 1));
                float rad = angle * Mathf.Deg2Rad;
                float x = Mathf.Cos(rad) * holeRingRadius;
                float y = Mathf.Sin(rad) * holeRingRadius - 0.01f;

                var hole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                hole.name = "DialHole_" + i;
                hole.transform.SetParent(root.transform, false);
                hole.transform.localPosition = new Vector3(x, y, 0.082f);
                hole.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                hole.transform.localScale = new Vector3(0.025f, 0.005f, 0.025f);
                hole.GetComponent<MeshRenderer>().sharedMaterial = dialHoleMat;
                Object.DestroyImmediate(hole.GetComponent<Collider>());
            }

            // Finger stop — small nub on the lower-right of the dial.
            var stop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stop.name = "FingerStop";
            stop.transform.SetParent(root.transform, false);
            stop.transform.localPosition = new Vector3(0.085f, -0.08f, 0.082f);
            stop.transform.localScale = new Vector3(0.015f, 0.015f, 0.015f);
            stop.transform.localRotation = Quaternion.Euler(0f, 0f, 30f);
            stop.GetComponent<MeshRenderer>().sharedMaterial = hookMat;
            Object.DestroyImmediate(stop.GetComponent<Collider>());

            // Cradle hook at the BOTTOM of the body — two prongs where the handset rests
            // horizontally across. Visual only; actual dock logic is unchanged.
            var hookBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hookBar.name = "CradleBar";
            hookBar.transform.SetParent(root.transform, false);
            hookBar.transform.localPosition = new Vector3(0f, -0.235f, 0.09f);
            hookBar.transform.localScale = new Vector3(0.22f, 0.02f, 0.05f);
            hookBar.GetComponent<MeshRenderer>().sharedMaterial = hookMat;
            Object.DestroyImmediate(hookBar.GetComponent<Collider>());

            for (int side = -1; side <= 1; side += 2)
            {
                var prong = GameObject.CreatePrimitive(PrimitiveType.Cube);
                prong.name = "CradleProng_" + (side < 0 ? "L" : "R");
                prong.transform.SetParent(root.transform, false);
                prong.transform.localPosition = new Vector3(side * 0.09f, -0.21f, 0.11f);
                prong.transform.localScale = new Vector3(0.025f, 0.05f, 0.025f);
                prong.GetComponent<MeshRenderer>().sharedMaterial = hookMat;
                Object.DestroyImmediate(prong.GetComponent<Collider>());
            }
        }

        static Material GetOrMakeDialHoleMaterial()
        {
            const string path = "Assets/Materials/Exterior/WallPhoneDialHole.mat";
            return MakeMat(path, new Color(0.05f, 0.05f, 0.06f), 0f, 0.2f);
        }

        static Material GetOrMakeBodyMaterial()
        {
            const string path = "Assets/Materials/Exterior/WallPhoneBody.mat";
            return MakeMat(path, new Color(0.08f, 0.09f, 0.10f), 0.1f, 0.4f);
        }
        static Material GetOrMakeButtonMaterial()
        {
            const string path = "Assets/Materials/Exterior/WallPhoneButton.mat";
            return MakeMat(path, new Color(0.85f, 0.82f, 0.75f), 0f, 0.3f);
        }
        static Material GetOrMakeHookMaterial()
        {
            const string path = "Assets/Materials/Exterior/WallPhoneHook.mat";
            return MakeMat(path, new Color(0.7f, 0.7f, 0.72f), 0.8f, 0.6f);
        }
        static Material GetOrMakeScreenMaterial()
        {
            const string path = "Assets/Materials/Exterior/WallPhoneScreen.mat";
            return MakeMat(path, new Color(0.2f, 0.5f, 0.45f), 0.3f, 0.8f);
        }
        static Material MakeMat(string path, Color c, float metallic, float smoothness)
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

        static Material GetOrMakeCordMaterial()
        {
            const string path = "Assets/Materials/Exterior/PhoneCord.mat";
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.shader = shader;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", new Color(0.04f, 0.04f, 0.04f));
            if (m.HasProperty("_Color")) m.SetColor("_Color", new Color(0.04f, 0.04f, 0.04f));
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.35f);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material GetOrMakePlateMaterial()
        {
            const string path = "Assets/Materials/Exterior/WallPhonePlate.mat";
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.shader = shader;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", new Color(0.85f, 0.82f, 0.76f));
            if (m.HasProperty("_Color")) m.SetColor("_Color", new Color(0.85f, 0.82f, 0.76f));
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.25f);
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
