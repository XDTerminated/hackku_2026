using UnityEngine;

namespace HackKU.Core
{
    // Fades the wrist canvas in/out based on proximity to the player's head.
    // Loosely models the "raise your wrist to check your watch" interaction — when the
    // wrist comes within a short distance of the camera, the watch lights up.
    [RequireComponent(typeof(CanvasGroup))]
    public class WristVisibilityController : MonoBehaviour
    {
        public Transform head;
        public float showDistance = 0.85f;
        public float hideDistance = 1.25f;
        public float fadeSpeed = 10f;

        CanvasGroup _cg;
        bool _on;

        void Awake()
        {
            _cg = GetComponent<CanvasGroup>();
            _cg.alpha = 0f;
        }

        void Update()
        {
            if (head == null)
            {
                var cam = Camera.main;
                if (cam != null) head = cam.transform;
                if (head == null) { _cg.alpha = 1f; return; } // no head — always show
            }

            float dist = Vector3.Distance(transform.position, head.position);
            if (_on) { if (dist > hideDistance) _on = false; }
            else     { if (dist < showDistance) _on = true; }

            float target = _on ? 1f : 0f;
            _cg.alpha = Mathf.MoveTowards(_cg.alpha, target, fadeSpeed * Time.deltaTime);
            _cg.interactable = _cg.alpha > 0.5f;
            _cg.blocksRaycasts = _on;
        }
    }
}
