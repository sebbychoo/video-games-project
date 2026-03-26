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

        [Header("Card Display")]
        [SerializeField] TextMeshProUGUI cardEntryPrefab; // fallback text-only
        [SerializeField] GameObject cardPrefab;           // full card prefab (optional)
        [SerializeField] float inspectionCardScale = 0.5f; // scale for cards in inspection panel
        [SerializeField] Vector2 inspectionCardSize = new Vector2(150f, 200f); // fixed size per card in inspection

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

            SubscribeToBattleEvents();
        }

        private void Start()
        {
            SubscribeToBattleEvents();
        }

        private void SubscribeToBattleEvents()
        {
            if (BattleEventBus.Instance == null) return;
            BattleEventBus.Instance.OnCardPlayed -= HandleCardPlayed;
            BattleEventBus.Instance.OnTurnPhaseChanged -= HandleTurnPhaseChanged;
            BattleEventBus.Instance.OnCardPlayed += HandleCardPlayed;
            BattleEventBus.Instance.OnTurnPhaseChanged += HandleTurnPhaseChanged;
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
                drawPileCountText.text = $"INBOX {deckManager.DeckCount}";
            if (discardPileCountText != null)
                discardPileCountText.text = $"ARCHIVE {deckManager.DiscardCount}";
        }

        /// <summary>Show the draw pile contents sorted alphabetically (no order reveal).</summary>
        public void ShowDrawPile()
        {
            if (deckManager == null) return;

            var cards = deckManager.DrawPile
                .Where(c => c != null)
                .OrderBy(c => c.cardName)
                .ToList();

            PopulateInspectionPanel("INBOX", cards);
        }

        /// <summary>Show the discard pile contents.</summary>
        public void ShowDiscardPile()
        {
            if (deckManager == null) return;

            var cards = deckManager.DiscardPile
                .Where(c => c != null)
                .ToList();

            PopulateInspectionPanel("ARCHIVE", cards);
        }

        /// <summary>Hide the inspection panel.</summary>
        public void HideInspectionPanel()
        {
            if (inspectionPanel != null)
                inspectionPanel.SetActive(false);
        }

        private void PopulateInspectionPanel(string title, List<CardData> cards)
        {
            ClearEntries();

            if (inspectionTitleText != null)
                inspectionTitleText.text = title;

            foreach (var card in cards)
            {
                if (contentParent == null) break;

                if (cardPrefab != null)
                {
                    // Spawn full card prefab and initialize it
                    GameObject go = Instantiate(cardPrefab, contentParent);
                    go.SetActive(true);
                    go.transform.localScale = Vector3.one * inspectionCardScale;

                    // Force fixed size — prevent layout group from stretching
                    RectTransform cardRT = go.GetComponent<RectTransform>();
                    if (cardRT != null)
                    {
                        cardRT.anchorMin = new Vector2(0.5f, 0.5f);
                        cardRT.anchorMax = new Vector2(0.5f, 0.5f);
                        cardRT.pivot = new Vector2(0.5f, 0.5f);
                        cardRT.sizeDelta = inspectionCardSize;
                    }

                    // Add LayoutElement to override layout group sizing
                    LayoutElement le = go.GetComponent<LayoutElement>();
                    if (le == null) le = go.AddComponent<LayoutElement>();
                    le.preferredWidth = inspectionCardSize.x * inspectionCardScale;
                    le.preferredHeight = inspectionCardSize.y * inspectionCardScale;
                    le.flexibleWidth = 0;
                    le.flexibleHeight = 0;

                    // Disable interaction on inspection cards
                    CardInteractionHandler handler = go.GetComponent<CardInteractionHandler>();
                    if (handler != null) handler.enabled = false;

                    // Try to initialize via CardInstance if available
                    CardInstance ci = go.GetComponent<CardInstance>();
                    if (ci != null) ci.Data = card;

                    // Refresh visuals
                    CardVisual cv = go.GetComponent<CardVisual>();
                    if (cv != null) cv.Refresh();

                    _spawnedEntries.Add(go);
                }
                else if (cardEntryPrefab != null)
                {
                    // Fallback: text only
                    var entry = Instantiate(cardEntryPrefab, contentParent);
                    entry.text = card.cardName;
                    entry.gameObject.SetActive(true);
                    _spawnedEntries.Add(entry.gameObject);
                }
            }

            if (inspectionPanel != null)
            {
                // Stretch to full screen
                RectTransform rt = inspectionPanel.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                }
                inspectionPanel.SetActive(true);
            }
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
