using System;
using UnityEngine;

namespace HackKU.Core
{
    public class DeliveryService : MonoBehaviour
    {
        public static DeliveryService Instance { get; private set; }

        public static event Action<DeliveryItem> OnOrderCompleted;
        public static event Action<string> OnOrderFailed;

        public Transform spawnPoint;

        [Tooltip("Plays the moment a delivery arrives (fires at spawnPoint if set, else at this transform).")]
        public AudioClip deliveryReadyClip;

        [Range(0f, 1f)] public float deliveryReadyVolume = 0.8f;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void DeliverItem(DeliveryItem item)
        {
            if (item == null)
            {
                OnOrderFailed?.Invoke("No item");
                return;
            }

            var stats = StatsManager.Instance;
            if (stats == null)
            {
                OnOrderFailed?.Invoke("Stats unavailable");
                return;
            }

            if (stats.Money < item.price)
            {
                OnOrderFailed?.Invoke("Insufficient funds");
                return;
            }

            stats.ApplyDelta(-item.price, 0f, "Ordered " + item.displayName);

            if (item.prefab != null && spawnPoint != null)
            {
                Vector3 jitter = new Vector3(
                    UnityEngine.Random.Range(-0.15f, 0.15f),
                    0f,
                    UnityEngine.Random.Range(-0.15f, 0.15f));
                Instantiate(item.prefab, spawnPoint.position + jitter, spawnPoint.rotation);
            }

            if (deliveryReadyClip != null)
            {
                Vector3 at = spawnPoint != null ? spawnPoint.position : transform.position;
                AudioSource.PlayClipAtPoint(deliveryReadyClip, at, deliveryReadyVolume);
            }

            OnOrderCompleted?.Invoke(item);
        }
    }
}
