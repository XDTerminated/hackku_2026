using UnityEngine;

namespace HackKU.Core
{
    [CreateAssetMenu(menuName = "HackKU/Food Item")]
    public class FoodItem : ScriptableObject
    {
        public string itemId;
        public string displayName;
        public float price;
        public float hungerRestore;
        public Color accentColor = new Color(0.8f, 0.5f, 0.2f);
        public GameObject prefab;
    }
}
