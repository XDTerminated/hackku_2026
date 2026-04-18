using System;
using HackKU.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HackKU.Game
{
    /// <summary>
    /// Lives on a card prefab. Exposes <see cref="Bind"/> so the
    /// <see cref="CharacterSelector"/> can push profile data into named TMP_Text
    /// children, and raises <see cref="OnClicked"/> when the attached
    /// <see cref="UnityEngine.UI.Button"/> (or any Button in children) is pressed.
    /// </summary>
    [DisallowMultipleComponent]
    public class CharacterCardUI : MonoBehaviour
    {
        /// <summary>Fires when the player taps/clicks this card.</summary>
        public event Action<CharacterProfile> OnClicked;

        [SerializeField] private Button button;
        [SerializeField] private Image portraitImage;

        private CharacterProfile profile;
        public CharacterProfile Profile => profile;

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponentInChildren<Button>(true);
            }
            if (button != null)
            {
                button.onClick.AddListener(HandleClick);
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
            }
        }

        /// <summary>
        /// Populate this card's visible text/portrait from the supplied profile.
        /// Looks for TMP_Text children named "Name", "Gimmick", "Money",
        /// "Happiness" and "Description".
        /// </summary>
        public void Bind(CharacterProfile p)
        {
            profile = p;
            if (p == null)
            {
                return;
            }

            SetText("Name", p.characterName);
            SetText("Gimmick", string.IsNullOrEmpty(p.gimmickTag) ? "" : $"\"{p.gimmickTag}\"");
            SetText("Money", FormatMoney(p.startingMoney));
            SetText("Happiness", $"{Mathf.RoundToInt(p.startingHappiness)} / 100");
            SetText("Description", p.description);

            if (portraitImage != null && p.portrait != null)
            {
                portraitImage.sprite = p.portrait;
                portraitImage.enabled = true;
            }
        }

        private void HandleClick()
        {
            OnClicked?.Invoke(profile);
        }

        private void SetText(string childName, string value)
        {
            Transform child = FindChildRecursive(transform, childName);
            if (child == null)
            {
                return;
            }

            TMP_Text tmp = child.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                tmp.text = value;
                return;
            }

            Text legacy = child.GetComponent<Text>();
            if (legacy != null)
            {
                legacy.text = value;
            }
        }

        private static Transform FindChildRecursive(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChildRecursive(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        private static string FormatMoney(float amount)
        {
            string sign = amount < 0f ? "-" : "";
            return $"{sign}${Mathf.Abs(amount):N0}";
        }
    }
}
