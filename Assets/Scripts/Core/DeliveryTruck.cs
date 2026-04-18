using System.Collections;
using UnityEngine;

namespace HackKU.Core
{
    public class DeliveryTruck : MonoBehaviour
    {
        [Tooltip("Local direction the truck drives away along (usually forward).")]
        public Vector3 driveDirection = Vector3.forward;

        [Tooltip("Seconds the truck spends driving away before hiding.")]
        public float driveDuration = 3.5f;

        [Tooltip("Top speed at end of drive (units/sec).")]
        public float maxSpeed = 8f;

        [Tooltip("Hide the GameObject after driving off.")]
        public bool hideWhenGone = true;

        bool _driving;

        public void DriveAway()
        {
            if (_driving || !gameObject.activeInHierarchy) return;
            _driving = true;
            StartCoroutine(DriveRoutine());
        }

        IEnumerator DriveRoutine()
        {
            Vector3 dirWorld = transform.TransformDirection(driveDirection.normalized);
            float t = 0f;
            while (t < driveDuration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / driveDuration);
                float speed = Mathf.Lerp(0f, maxSpeed, k * k);
                transform.position += dirWorld * speed * Time.deltaTime;
                yield return null;
            }
            if (hideWhenGone) gameObject.SetActive(false);
            _driving = false;
        }
    }
}
