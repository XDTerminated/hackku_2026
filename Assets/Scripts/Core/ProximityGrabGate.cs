using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace HackKU.Core
{
    // Only allows the attached XRGrabInteractable to be selected when a controller
    // (XR Interactor) is physically within `maxDistance`. Prevents far-ray grabs so
    // the player must physically reach the object.
    [RequireComponent(typeof(XRGrabInteractable))]
    public class ProximityGrabGate : MonoBehaviour
    {
        [Tooltip("Maximum distance from any controller origin at which this object can be grabbed.")]
        public float maxDistance = 0.35f;

        [Tooltip("Re-check frequency in seconds. Small enough to feel responsive, large enough to be cheap.")]
        public float checkInterval = 0.1f;

        XRGrabInteractable grab;
        static Transform[] s_interactorPoints;
        static float s_nextRescanAt;
        float nextCheck;

        void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();
        }

        void OnEnable()
        {
            EnsureInteractorCache();
        }

        // Shared across all ProximityGrabGate instances so we don't run
        // FindObjectsByType per-interactable. Rescanned at most once per 5s to pick up
        // a late XR-rig spawn.
        static void EnsureInteractorCache()
        {
            if (s_interactorPoints != null && s_interactorPoints.Length > 0 && Time.unscaledTime < s_nextRescanAt) return;
            var nfs = Object.FindObjectsByType<NearFarInteractor>(FindObjectsSortMode.None);
            s_interactorPoints = new Transform[nfs.Length];
            for (int i = 0; i < nfs.Length; i++) s_interactorPoints[i] = nfs[i].transform;
            s_nextRescanAt = Time.unscaledTime + 5f;
        }

        void Update()
        {
            if (Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + checkInterval;

            if (s_interactorPoints == null || s_interactorPoints.Length == 0) EnsureInteractorCache();
            if (grab.isSelected) return; // already held, don't toggle

            var points = s_interactorPoints;
            float nearest = float.MaxValue;
            Vector3 me = transform.position;
            for (int i = 0; i < points.Length; i++)
            {
                if (points[i] == null) continue;
                float d = Vector3.Distance(points[i].position, me);
                if (d < nearest) nearest = d;
            }

            bool inRange = nearest <= maxDistance;
            if (grab.enabled != inRange)
            {
                grab.enabled = inRange;
            }
        }
    }
}
