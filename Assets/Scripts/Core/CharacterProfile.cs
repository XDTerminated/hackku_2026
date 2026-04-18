using UnityEngine;

namespace HackKU.Core
{
    [CreateAssetMenu(menuName = "HackKU/Character Profile")]
    public class CharacterProfile : ScriptableObject
    {
        public string characterName;
        [TextArea] public string description;
        public Sprite portrait;
        public float startingMoney;
        public float startingHappiness;
        public float yearlyIncome;
        public float yearlyExpenses;
        public float yearlyHappinessRegen;
        public string gimmickTag;
    }
}
