using HackKU.Core;
using UnityEngine;

namespace HackKU.Game
{
    /// <summary>
    /// Container asset listing every <see cref="CharacterProfile"/> the Character
    /// Select screen will present to the player. Drop the profile assets into the
    /// <see cref="characters"/> array in the inspector in display order.
    /// </summary>
    [CreateAssetMenu(menuName = "HackKU/Character Catalog", fileName = "CharacterCatalog")]
    public class CharacterCatalog : ScriptableObject
    {
        [Tooltip("Characters shown on the selection screen, in the order they should appear.")]
        public CharacterProfile[] characters;
    }
}
