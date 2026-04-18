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
        Transform[] interactorPoints;
        float nextCheck;

        void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();
        }

        void OnEnable()
        {
            RefreshInteractorCache();
        }

        void RefreshInteractorCache()
        {
            var nfs = Object.FindObjectsByType<NearFarInteractor>(FindObjectsSortMode.None);
            interactorPoints = new Transform[nfs.Length];
            for (int i = 0; i < nfs.Length; i++) interactorPoints[i] = nfs[i].transform;
        }

        void Update()
        {
            if (Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + checkInterval;

            if (interactorPoints == null || interactorPoints.Length == 0) RefreshInteractorCache();
            if (grab.isSelected) return; // already held, don't toggle

            float nearest = float.MaxValue;
            for (int i = 0; i < interactorPoints.Length; i++)
            {
                if (interactorPoints[i] == null) continue;
                float d = Vector3.Distance(interactorPoints[i].position, transform.position);
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
