using UnityEngine;

namespace HackKU.Core
{
    [CreateAssetMenu(menuName = "HackKU/Delivery Item", fileName = "DeliveryItem")]
    public class DeliveryItem : ScriptableObject
    {
        public string itemId;
        public string displayName;
        public float price;
        public float happinessOnUse;
        public GameObject prefab;
    }
}
