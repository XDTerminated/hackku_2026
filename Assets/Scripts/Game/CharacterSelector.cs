using System;
using System.Collections.Generic;
using HackKU.Core;
using UnityEngine;

namespace HackKU.Game
{
    /// <summary>
    /// Spawns a row of <see cref="CharacterCardUI"/> instances in an arc in front
    /// of <see cref="cardAnchor"/>, one per profile in the supplied
    /// <see cref="CharacterCatalog"/>. When any card is clicked the selector
    /// raises <see cref="OnCharacterChosen"/> and tears the row down.
    /// </summary>
    public class CharacterSelector : MonoBehaviour
    {
        /// <summary>Static so boot/UI systems can subscribe without needing a direct reference.</summary>
        public static event Action<CharacterProfile> OnCharacterChosen;

        [Header("Data")]
        [SerializeField] private CharacterCatalog catalog;

        [Header("Scene Refs")]
        [Tooltip("Cards are laid out on an arc centered on this transform, facing its +Z.")]
        [SerializeField] private Transform cardAnchor;
        [Tooltip("Prefab that has a CharacterCardUI (and a UI Button) somewhere in it.")]
        [SerializeField] private GameObject cardPrefab;

        [Header("Arc Layout")]
        [Tooltip("Distance from anchor to each card.")]
        [SerializeField] private float radius = 1.2f;
        [Tooltip("Total arc width in degrees (spread of the fan).")]
        [SerializeField] private float arcDegrees = 60f;
        [Tooltip("Vertical offset applied to every card relative to the anchor.")]
        [SerializeField] private float heightOffset = 0f;

        [Header("Behaviour")]
        [SerializeField] private bool spawnOnStart = true;
        [SerializeField] private bool destroySelfWhenChosen = false;

        private readonly List<GameObject> spawnedCards = new List<GameObject>();
        private bool hasChosen;

        private void Start()
        {
            if (spawnOnStart)
            {
                Spawn();
            }
        }

        private void OnDestroy()
        {
            DetachCardListeners();
        }

        /// <summary>
        /// Force the card fan to (re)build. Safe to call multiple times — existing
        /// cards are cleared first.
        /// </summary>
        public void Spawn()
        {
            Clear();
            hasChosen = false;

            if (catalog == null || catalog.characters == null || catalog.characters.Length == 0)
            {
                Debug.LogWarning("[CharacterSelector] No catalog or empty character list; nothing to spawn.", this);
                return;
            }
            if (cardPrefab == null)
            {
                Debug.LogWarning("[CharacterSelector] cardPrefab not assigned.", this);
                return;
            }
            if (cardAnchor == null)
            {
                cardAnchor = transform;
            }

            int count = catalog.characters.Length;
            // For a single card we just place it dead center.
            for (int i = 0; i < count; i++)
            {
                CharacterProfile profile = catalog.characters[i];
                if (profile == null)
                {
                    continue;
                }

                float t = count == 1 ? 0.5f : (float)i / (count - 1);
                float angleDeg = Mathf.Lerp(-arcDegrees * 0.5f, arcDegrees * 0.5f, t);
                float rad = angleDeg * Mathf.Deg2Rad;

                Vector3 localOffset = new Vector3(Mathf.Sin(rad) * radius, heightOffset, Mathf.Cos(rad) * radius);
                Vector3 worldPos = cardAnchor.TransformPoint(localOffset);
                // Face the anchor — cards should look back toward the player.
                Quaternion worldRot = Quaternion.LookRotation(cardAnchor.position - worldPos, cardAnchor.up);

                GameObject cardGO = Instantiate(cardPrefab, worldPos, worldRot, cardAnchor);
                cardGO.name = $"Card_{profile.characterName}";

                CharacterCardUI ui = cardGO.GetComponent<CharacterCardUI>();
                if (ui == null)
                {
                    ui = cardGO.GetComponentInChildren<CharacterCardUI>(true);
                }
                if (ui != null)
                {
                    ui.Bind(profile);
                    ui.OnClicked += HandleCardClicked;
                }
                else
                {
                    Debug.LogWarning($"[CharacterSelector] Card prefab '{cardPrefab.name}' has no CharacterCardUI component.", this);
                }

                spawnedCards.Add(cardGO);
            }
        }

        private void HandleCardClicked(CharacterProfile profile)
        {
            if (hasChosen || profile == null)
            {
                return;
            }
            hasChosen = true;
            OnCharacterChosen?.Invoke(profile);
            Clear();
            if (destroySelfWhenChosen)
            {
                Destroy(gameObject);
            }
        }

        private void Clear()
        {
            DetachCardListeners();
            for (int i = 0; i < spawnedCards.Count; i++)
            {
                if (spawnedCards[i] != null)
                {
                    Destroy(spawnedCards[i]);
                }
            }
            spawnedCards.Clear();
        }

        private void DetachCardListeners()
        {
            for (int i = 0; i < spawnedCards.Count; i++)
            {
                if (spawnedCards[i] == null)
                {
                    continue;
                }
                CharacterCardUI ui = spawnedCards[i].GetComponent<CharacterCardUI>();
                if (ui == null)
                {
                    ui = spawnedCards[i].GetComponentInChildren<CharacterCardUI>(true);
                }
                if (ui != null)
                {
                    ui.OnClicked -= HandleCardClicked;
                }
            }
        }
    }
}
