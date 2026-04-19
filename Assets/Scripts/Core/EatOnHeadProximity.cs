using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace HackKU.Core
{
    // Attach to grabbable food items. When the player holds it (XRGrabInteractable selected)
    // and brings it within `proximityMeters` of the head camera for `dwellSeconds`, the food
    // is consumed — hunger restored and the GameObject destroyed.
    [RequireComponent(typeof(XRGrabInteractable))]
    public class EatOnHeadProximity : MonoBehaviour
    {
        [Tooltip("Distance from head camera that counts as 'bringing to mouth'.")]
        public float proximityMeters = 0.25f;

        [Tooltip("Continuous time at close range before consumption fires.")]
        public float dwellSeconds = 0.4f;

        public float hungerRestore = 40f;
        public string foodName = "Food";

        XRGrabInteractable _grab;
        Transform _head;
        float _dwell;

        void Awake()
        {
            _grab = GetComponent<XRGrabInteractable>();
        }

        void Update()
        {
            if (_head == null)
            {
                var cam = Camera.main;
                _head = cam != null ? cam.transform : null;
                if (_head == null) return;
            }

            if (_grab == null || !_grab.isSelected)
            {
                _dwell = 0f;
                return;
            }

            float d = Vector3.Distance(transform.position, _head.position);
            if (d <= proximityMeters)
            {
                _dwell += Time.deltaTime;
                if (_dwell >= dwellSeconds) Consume();
            }
            else
            {
                _dwell = 0f;
            }
        }

        void Consume()
        {
            if (HungerManager.Instance != null) HungerManager.Instance.ApplyDelta(hungerRestore, foodName);
            SfxHub.Instance.PlayAt("eat", transform.position, 0.9f);
            ToastHUD.Show("+" + Mathf.Round(hungerRestore), "Ate " + foodName, ToastKind.HungerUp);
            Destroy(gameObject);
        }
    }
}
