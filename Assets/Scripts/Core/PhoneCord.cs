using UnityEngine;

namespace HackKU.Core
{
    // Draws a helical / spiral telephone cord between a fixed cradle anchor and the
    // handset, via a LineRenderer. The spiral tightens when the cord is slack and
    // stretches flat as the handset is pulled to full length.
    [RequireComponent(typeof(LineRenderer))]
    public class PhoneCord : MonoBehaviour
    {
        [Tooltip("Where the cord enters the phone body (wall-mounted cradle).")]
        public Transform cradleAnchor;

        [Tooltip("Where the cord attaches to the handset (usually handset.transform).")]
        public Transform handsetAnchor;

        [Tooltip("Resolution of the cord. More = smoother spiral, more draw calls.")]
        [Range(8, 120)] public int segments = 48;

        [Tooltip("Spiral turns when the cord is fully slack (handset on cradle).")]
        public int slackTurns = 10;

        [Tooltip("Radius of the spiral when fully slack.")]
        public float slackRadius = 0.035f;

        [Tooltip("Full cord length. Beyond this the cord is taut and the player's tether kicks in.")]
        public float maxLength = 2.5f;

        [Tooltip("When set, the cord is hidden while the handset is resting on the cradle and only appears once the player picks it up.")]
        public HandsetController handset;

        LineRenderer _lr;

        void Awake()
        {
            _lr = GetComponent<LineRenderer>();
            _lr.useWorldSpace = true;
            _lr.positionCount = segments + 1;
        }

        void LateUpdate()
        {
            // Only show the cord while the handset is lifted / held.
            if (handset != null)
            {
                bool shouldShow = handset.IsHeld || !handset.IsOnCradle;
                if (_lr.enabled != shouldShow) _lr.enabled = shouldShow;
                if (!shouldShow) return;
            }

            if (cradleAnchor == null || handsetAnchor == null) return;

            Vector3 a = cradleAnchor.position;
            Vector3 b = handsetAnchor.position;
            Vector3 ab = b - a;
            float len = ab.magnitude;
            if (len < 0.001f) { for (int i = 0; i <= segments; i++) _lr.SetPosition(i, a); return; }

            Vector3 dir = ab / len;
            // Build two perpendicular axes in the plane orthogonal to the cord direction.
            Vector3 perp1 = Vector3.Cross(dir, Vector3.up);
            if (perp1.sqrMagnitude < 0.001f) perp1 = Vector3.Cross(dir, Vector3.right);
            perp1.Normalize();
            Vector3 perp2 = Vector3.Cross(dir, perp1).normalized;

            // tautness 0 = slack → full spiral, 1 = fully stretched → spiral flattens.
            float tautness = Mathf.Clamp01(len / Mathf.Max(0.01f, maxLength));
            float radius = slackRadius * (1f - 0.85f * tautness);
            // Fewer turns when taut so the curl stretches out like a real extended spring cord.
            float turns = Mathf.Lerp(slackTurns, slackTurns * 0.35f, tautness);
            float totalAngle = turns * 2f * Mathf.PI;

            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                // Damp the spiral near the two endpoints so the cord cleanly meets each anchor.
                float endFalloff = Mathf.Sin(t * Mathf.PI); // 0 at ends, 1 at middle
                float r = radius * endFalloff;
                float angle = t * totalAngle;
                Vector3 axisPoint = Vector3.Lerp(a, b, t);
                Vector3 offset = perp1 * Mathf.Cos(angle) * r + perp2 * Mathf.Sin(angle) * r;
                _lr.SetPosition(i, axisPoint + offset);
            }
        }
    }
}
