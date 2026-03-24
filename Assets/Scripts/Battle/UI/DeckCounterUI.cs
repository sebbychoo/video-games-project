using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CardBattle
{
    /// <summary>
    /// Displays draw pile and discard pile counts.
    /// Clicking the draw pile counter shows a scrollable alphabetical list of draw pile cards
    /// (sorted to avoid revealing draw order).
    /// Clicking the discard pile counter shows a scrollable list of discard pile cards.
    /// Subscribes to BattleEventBus events to refresh counts reactively.
    /// </summary>
    public class DeckCounterUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] DeckManager deckManager;

        [Header("Counter UI")]
        [SerializeField] TextMeshProUGUI drawPileCountText;
        [SerializeField] TextMeshProUGUI discardPileCountText;
        [SerializeField] Button drawPileButton;
        [SerializeField] Button discardPileButton;

        [Header("Inspection Panel")]
        [SerializeField] GameObject inspectionPanel;
        [SerializeField] TextMeshProUGUI inspectionTitleText;
        [SerializeField] Transform contentParent;
        [SerializeField] Button closeButton;

        [Header("Card Entry Prefab")]
        [SerializeField] TextMeshProUGUI cardEntryPrefab;

        private readonly List<GameObject> _spawnedEntries = new List<GameObject>();

        /// <summary>Initialize the UI with a DeckManager reference.</summary>
        public void Initialize(DeckManager manager)
        {
            deckManager = manager;
            RefreshCounts();
            HideInspectionPanel();
        }

        private void OnEnable()
        {
            if (drawPileButton != null)
                drawPileButton.onClick.AddListener(ShowDrawPile);
            if (discardPileButton != null)
                discardPileButton.onClick.AddListener(ShowDiscardPile);
            if (closeButton != null)
                closeButton.onClick.AddListener(HideInspectionPanel);

            if (BattleEventBus.Instance != null)
            {
                BattleEventBus.Instance.OnCardPlayed += HandleCardPlayed;
                BattleEventBus.Instance.OnTurnPhaseChanged += HandleTurnPhaseChanged;
            }
        }

        private void OnDisable()
        {
            if (drawPileButton != null)
                drawPileButton.onClick.RemoveListener(ShowDrawPile);
            if (discardPileButton != null)
                discardPileButton.onClick.RemoveListener(ShowDiscardPile);
            if (closeButton != null)
                closeButton.onClick.RemoveListener(HideInspectionPanel);

            if (BattleEventBus.Instance != null)
            {
                BattleEventBus.Instance.OnCardPlayed -= HandleCardPlayed;
                BattleEventBus.Instance.OnTurnPhaseChanged -= HandleTurnPhaseChanged;
            }
        }

        private void HandleCardPlayed(CardPlayedEvent e) => RefreshCounts();
        private void HandleTurnPhaseChanged(TurnPhaseChangedEvent e) => RefreshCounts();

        /// <summary>Refresh the draw and discard pile count labels.</summary>
        public void RefreshCounts()
        {
            if (deckManager == null) return;

            if (drawPileCountText != null)
                drawPileCountText.text = deckManager.DeckCount.ToString();
            if (discardPileCountText != null)
                discardPileCountText.text = deckManager.DiscardCount.ToString();
        }

        /// <summary>Show the draw pile contents sorted alphabetically (no order reveal).</summary>
        public void ShowDrawPile()
        {
            if (deckManager == null) return;

            var cardNames = deckManager.DrawPile
                .Where(c => c != null)
                .Select(c => c.cardName)
                .OrderBy(n => n)
                .ToList();

            PopulateInspectionPanel("Draw Pile", cardNames);
        }

        /// <summary>Show the discard pile contents.</summary>
        public void ShowDiscardPile()
        {
            if (deckManager == null) return;

            var cardNames = deckManager.DiscardPile
                .Where(c => c != null)
                .Select(c => c.cardName)
                .ToList();

            PopulateInspectionPanel("Discard Pile", cardNames);
        }

        /// <summary>Hide the inspection panel.</summary>
        public void HideInspectionPanel()
        {
            if (inspectionPanel != null)
                inspectionPanel.SetActive(false);
        }

        private void PopulateInspectionPanel(string title, List<string> cardNames)
        {
            ClearEntries();

            if (inspectionTitleText != null)
                inspectionTitleText.text = title;

            foreach (var name in cardNames)
            {
                if (cardEntryPrefab == null || contentParent == null) break;

                var entry = Instantiate(cardEntryPrefab, contentParent);
                entry.text = name;
                entry.gameObject.SetActive(true);
                _spawnedEntries.Add(entry.gameObject);
            }

            if (inspectionPanel != null)
                inspectionPanel.SetActive(true);
        }

        private void ClearEntries()
        {
            foreach (var entry in _spawnedEntries)
            {
                if (entry != null)
                    Destroy(entry);
            }
            _spawnedEntries.Clear();
        }
    }
}
