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
        [Header("Student-loan debt (new core loop)")]
        [Tooltip("Starting debt balance. Win the run by getting this to 0.")]
        public float startingDebt;
        [Tooltip("Fraction of every paycheck automatically applied to debt principal. 0..1.")]
        [Range(0f, 1f)] public float paycheckDebtSplit = 0.2f;
    }
}
