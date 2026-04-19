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
            SetText("Debt", FormatMoney(p.startingDebt));
            SetText("Happiness", $"{Mathf.RoundToInt(p.startingHappiness)}%");
            SetText("Description", p.description);
            SetText("Letter", GetInitial(p.characterName));

            var accent = AccentForName(p.characterName);
            TintChild("AccentStripe", accent);

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

        private static string GetInitial(string s)
        {
            if (string.IsNullOrEmpty(s)) return "?";
            string trimmed = s.Trim();
            // Skip "The " / "A " prefixes so class-style names grab the distinctive letter.
            if (trimmed.Length >= 4 && trimmed.StartsWith("the ", System.StringComparison.InvariantCultureIgnoreCase))
                trimmed = trimmed.Substring(4).TrimStart();
            if (trimmed.Length >= 2 && trimmed.StartsWith("a ", System.StringComparison.InvariantCultureIgnoreCase))
                trimmed = trimmed.Substring(2).TrimStart();
            if (trimmed.Length == 0) return "?";
            return char.ToUpperInvariant(trimmed[0]).ToString();
        }

        // Deterministic per-character accent color. Known names pick a curated palette;
        // fall back to a hash-derived hue so new profiles still look distinct.
        private static Color AccentForName(string name)
        {
            if (string.IsNullOrEmpty(name)) return new Color(0.7f, 0.85f, 1f);
            string key = name.ToLowerInvariant();
            if (key.Contains("student") || key.Contains("scholar") || key.Contains("grad")) return new Color(0.72f, 0.58f, 1f); // lavender
            if (key.Contains("barista") || key.Contains("brewer")) return new Color(1f, 0.72f, 0.45f);  // warm orange
            if (key.Contains("shark") || key.Contains("corporate")) return new Color(0.45f, 0.88f, 0.78f); // teal
            int h = 0;
            for (int i = 0; i < name.Length; i++) h = (h * 31 + name[i]) & 0x7fffffff;
            float hue = (h % 360) / 360f;
            return Color.HSVToRGB(hue, 0.45f, 1f);
        }

        private void TintChild(string childName, Color color)
        {
            Transform child = FindChildRecursive(transform, childName);
            if (child == null) return;
            var img = child.GetComponent<Image>();
            if (img != null) img.color = color;
        }
    }
}
