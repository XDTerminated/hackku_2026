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

            OnOrderCompleted?.Invoke(item);
        }
    }
}
