using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CardBattle
{
    /// <summary>
    /// Carousel UI for selecting a starting deck set at the beginning of a new run.
    /// Displays the current deck set name at top, all 8 cards with full details below,
    /// left/right arrow buttons to browse between sets, and a Select button at bottom-center.
    /// </summary>
    public class StartingDeckCarousel : MonoBehaviour
    {
        [Header("Deck Sets")]
        [SerializeField] List<StartingDeckSet> deckSets;

        [Header("UI References")]
        [SerializeField] TextMeshProUGUI deckSetNameText;
        [SerializeField] TextMeshProUGUI deckSetDescriptionText;
        [SerializeField] Transform cardDisplayParent;
        [SerializeField] GameObject cardEntryPrefab;

        [Header("Navigation")]
        [SerializeField] Button leftArrowButton;
        [SerializeField] Button rightArrowButton;
        [SerializeField] Button selectButton;

        /// <summary>Fired when the player selects a deck set. Listeners can proceed with run start.</summary>
        public event Action OnDeckSelected;

        private int _currentIndex;
        private readonly List<GameObject> _spawnedEntries = new List<GameObject>();

        private void OnEnable()
        {
            if (leftArrowButton != null)
                leftArrowButton.onClick.AddListener(BrowseLeft);
            if (rightArrowButton != null)
                rightArrowButton.onClick.AddListener(BrowseRight);
            if (selectButton != null)
                selectButton.onClick.AddListener(SelectCurrentDeck);
        }

        private void OnDisable()
        {
            if (leftArrowButton != null)
                leftArrowButton.onClick.RemoveListener(BrowseLeft);
            if (rightArrowButton != null)
                rightArrowButton.onClick.RemoveListener(BrowseRight);
            if (selectButton != null)
                selectButton.onClick.RemoveListener(SelectCurrentDeck);
        }

        /// <summary>
        /// Initialize the carousel. Call this to show the UI with available deck sets.
        /// Defaults to the first set.
        /// </summary>
        public void Initialize()
        {
            if (deckSets == null || deckSets.Count == 0)
            {
                Debug.LogWarning("StartingDeckCarousel: No deck sets assigned.");
                return;
            }

            _currentIndex = 0;
            RefreshDisplay();
        }

        /// <summary>Navigate to the previous deck set (wraps around).</summary>
        public void BrowseLeft()
        {
            if (deckSets == null || deckSets.Count == 0) return;
            _currentIndex = (_currentIndex - 1 + deckSets.Count) % deckSets.Count;
            RefreshDisplay();
        }

        /// <summary>Navigate to the next deck set (wraps around).</summary>
        public void BrowseRight()
        {
            if (deckSets == null || deckSets.Count == 0) return;
            _currentIndex = (_currentIndex + 1) % deckSets.Count;
            RefreshDisplay();
        }

        /// <summary>
        /// Confirm the currently displayed deck set. Stores the set ID in RunState,
        /// initializes the Draw_Pile via DeckManager, and fires OnDeckSelected.
        /// </summary>
        public void SelectCurrentDeck()
        {
            if (deckSets == null || deckSets.Count == 0) return;

            StartingDeckSet chosen = deckSets[_currentIndex];
            if (chosen == null || chosen.cards == null || chosen.cards.Count == 0)
            {
                Debug.LogWarning("StartingDeckCarousel: Selected deck set is empty.");
                return;
            }

            // Store the chosen deck set ID in RunState
            if (SaveManager.Instance != null && SaveManager.Instance.CurrentRun != null)
            {
                SaveManager.Instance.CurrentRun.startingDeckSetId = chosen.setName;

                // Populate deckCardIds so BattleManager can load them
                SaveManager.Instance.CurrentRun.deckCardIds = new List<string>();
                foreach (CardData card in chosen.cards)
                {
                    if (card != null)
                        SaveManager.Instance.CurrentRun.deckCardIds.Add(card.name);
                }
            }

            // Initialize the DeckManager draw pile if BattleManager is available
            if (BattleManager.Instance != null)
            {
                DeckManager deckManager = BattleManager.Instance.GetComponentInChildren<DeckManager>();
                if (deckManager != null)
                    deckManager.Initialize(new List<CardData>(chosen.cards));
            }

            OnDeckSelected?.Invoke();
        }

        /// <summary>The currently displayed StartingDeckSet, or null if none.</summary>
        public StartingDeckSet CurrentDeckSet
        {
            get
            {
                if (deckSets == null || deckSets.Count == 0) return null;
                return deckSets[_currentIndex];
            }
        }

        // ── Private ──────────────────────────────────────────────────────────

        private void RefreshDisplay()
        {
            StartingDeckSet current = deckSets[_currentIndex];

            // Update header text
            if (deckSetNameText != null)
                deckSetNameText.text = current != null ? current.setName : "";

            if (deckSetDescriptionText != null)
                deckSetDescriptionText.text = current != null ? current.description : "";

            // Rebuild card entries
            ClearCardEntries();

            if (current == null || current.cards == null) return;

            foreach (CardData card in current.cards)
            {
                if (card == null || cardDisplayParent == null) continue;
                SpawnCardEntry(card);
            }

            // Update arrow button interactability (always interactable since we wrap)
            if (leftArrowButton != null)
                leftArrowButton.interactable = deckSets.Count > 1;
            if (rightArrowButton != null)
                rightArrowButton.interactable = deckSets.Count > 1;
        }

        private void SpawnCardEntry(CardData card)
        {
            if (cardEntryPrefab == null || cardDisplayParent == null) return;

            GameObject entry = Instantiate(cardEntryPrefab, cardDisplayParent);
            entry.SetActive(true);

            // Try to initialize via CardInstance + CardVisual (full card prefab)
            CardInstance ci = entry.GetComponent<CardInstance>();
            if (ci != null)
                ci.Data = card;

            CardVisual cv = entry.GetComponent<CardVisual>();
            if (cv != null)
                cv.Refresh();

            // Disable interaction on carousel cards
            CardInteractionHandler handler = entry.GetComponent<CardInteractionHandler>();
            if (handler != null)
                handler.enabled = false;

            // Fallback: populate TextMeshPro fields by name if no CardVisual
            if (cv == null)
                PopulateTextEntry(entry, card);

            _spawnedEntries.Add(entry);
        }

        /// <summary>
        /// Fallback text population for a card entry prefab that uses named
        /// TextMeshProUGUI children instead of CardVisual.
        /// </summary>
        private void PopulateTextEntry(GameObject entry, CardData card)
        {
            SetChildText(entry, "CardName", card.cardName);
            SetChildText(entry, "OvertimeCost", card.overtimeCost.ToString());
            SetChildText(entry, "CardType", card.cardType.ToString());
            SetChildText(entry, "EffectValue", card.effectValue.ToString());
            SetChildText(entry, "Description", card.description);
        }

        private static void SetChildText(GameObject parent, string childName, string value)
        {
            Transform child = parent.transform.Find(childName);
            if (child == null) return;
            TextMeshProUGUI tmp = child.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
                tmp.text = value;
        }

        private void ClearCardEntries()
        {
            foreach (GameObject entry in _spawnedEntries)
            {
                if (entry != null)
                    Destroy(entry);
            }
            _spawnedEntries.Clear();
        }
    }
}
