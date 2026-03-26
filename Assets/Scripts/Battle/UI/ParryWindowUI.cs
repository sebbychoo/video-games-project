using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CardBattle
{
    /// <summary>
    /// Displays the active parry window state during Enemy_Phase attacks.
    /// Shows a countdown timer, the intent color (parry difficulty), and
    /// highlights matching Defense cards in the player's hand.
    /// Hides itself when no parry window is active.
    /// Requirements: 6.1, 6.2, 6.8
    /// </summary>
    public class ParryWindowUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ParrySystem parrySystem;
        [SerializeField] private HandManager handManager;

        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;

        [Header("Timer")]
        [SerializeField] private Image timerFillImage;
        [SerializeField] private TextMeshProUGUI timerText;

        [Header("Intent Color Indicator")]
        [SerializeField] private Image intentColorImage;
        [SerializeField] private TextMeshProUGUI intentLabel;

        [Header("Intent Colors")]
        [SerializeField] private Color whiteIntentColor  = new Color(0.9f, 0.9f, 0.9f);
        [SerializeField] private Color yellowIntentColor = new Color(1f,   0.85f, 0f);
        [SerializeField] private Color redIntentColor    = new Color(0.9f, 0.1f,  0.1f);

        [Header("Card Highlight Colors")]
        [SerializeField] private Color matchingCardHighlight  = new Color(0.2f, 1f, 0.3f);
        [SerializeField] private Color timerFillNormal        = new Color(0.2f, 1f, 0.3f);
        [SerializeField] private Color timerFillUrgent        = new Color(1f,   0.3f, 0.1f);
        [SerializeField] private float urgentThreshold        = 0.3f; // fraction of window remaining

        // Cards currently highlighted so we can clear them when the window closes
        private readonly List<CardInstance> _highlightedCards = new List<CardInstance>();
        private bool _wasWindowActive;

        private void Awake()
        {
            // Only hide the panel child — keep this MonoBehaviour active so Update() runs
            if (panelRoot != null && panelRoot != gameObject)
                panelRoot.SetActive(false);
        }

        private void Start()
        {
            // Auto-locate dependencies if not wired in Inspector
            if (parrySystem == null)
                parrySystem = FindObjectOfType<ParrySystem>();
            if (handManager == null)
                handManager = FindObjectOfType<HandManager>();

            SubscribeToEvents();
        }

        private void OnEnable()  => SubscribeToEvents();
        private void OnDisable() => UnsubscribeFromEvents();

        private void SubscribeToEvents()
        {
            if (BattleEventBus.Instance == null) return;
            BattleEventBus.Instance.OnParry          -= OnParry;
            BattleEventBus.Instance.OnTurnPhaseChanged -= OnTurnPhaseChanged;
            BattleEventBus.Instance.OnParry          += OnParry;
            BattleEventBus.Instance.OnTurnPhaseChanged += OnTurnPhaseChanged;
        }

        private void UnsubscribeFromEvents()
        {
            if (BattleEventBus.Instance == null) return;
            BattleEventBus.Instance.OnParry            -= OnParry;
            BattleEventBus.Instance.OnTurnPhaseChanged -= OnTurnPhaseChanged;
        }

        private void Update()
        {
            if (parrySystem == null)
            {
                // Try to find it again in case it was initialized late
                parrySystem = FindObjectOfType<ParrySystem>();
                if (parrySystem == null) return;
            }
            if (handManager == null)
                handManager = FindObjectOfType<HandManager>();

            bool isActive = parrySystem.IsParryWindowActive;

            if (isActive)
            {
                if (!_wasWindowActive)
                    OnWindowOpened();

                RefreshTimer();
                RefreshCardHighlights();
            }
            else if (_wasWindowActive)
            {
                OnWindowClosed();
            }

            _wasWindowActive = isActive;
        }

        // ── Window lifecycle ─────────────────────────────────────────────────

        private void OnWindowOpened()
        {
            Debug.Log($"[ParryWindowUI] Window OPENED — panelRoot: {(panelRoot != null ? panelRoot.name : "NULL")}");
            if (panelRoot != null)
                panelRoot.SetActive(true);

            RefreshIntentColor();
            RefreshTimer();
            RefreshCardHighlights();
        }

        private void OnWindowClosed()
        {
            Debug.Log("[ParryWindowUI] Window CLOSED");
            if (panelRoot != null)
                panelRoot.SetActive(false);

            ClearCardHighlights();
        }

        // ── Timer display ────────────────────────────────────────────────────

        private void RefreshTimer()
        {
            if (parrySystem == null) return;

            float remaining = parrySystem.ParryWindowTimeRemaining;
            float total     = parrySystem.ParryWindowDuration;
            float fraction  = total > 0f ? Mathf.Clamp01(remaining / total) : 0f;

            if (timerFillImage != null)
            {
                timerFillImage.fillAmount = fraction;
                timerFillImage.color = fraction <= urgentThreshold ? timerFillUrgent : timerFillNormal;
            }

            if (timerText != null)
                timerText.text = remaining.ToString("F1");
        }

        // ── Intent color display (Req 6.8) ───────────────────────────────────

        private void RefreshIntentColor()
        {
            if (parrySystem == null) return;

            IntentColor intent = parrySystem.CurrentAttack.intentColor;
            Color uiColor;
            string label;

            switch (intent)
            {
                case IntentColor.White:
                    uiColor = whiteIntentColor;
                    label   = "EASY";
                    break;
                case IntentColor.Yellow:
                    uiColor = yellowIntentColor;
                    label   = "MEDIUM";
                    break;
                case IntentColor.Red:
                    uiColor = redIntentColor;
                    label   = "HARD";
                    break;
                default:
                    // Unparryable — window should never open for these, but guard anyway
                    uiColor = Color.grey;
                    label   = "";
                    break;
            }

            if (intentColorImage != null)
                intentColorImage.color = uiColor;

            if (intentLabel != null)
                intentLabel.text = label;
        }

        // ── Card highlighting (Req 6.2) ──────────────────────────────────────

        private void RefreshCardHighlights()
        {
            if (parrySystem == null || handManager == null) return;

            List<CardInstance> matching = parrySystem.GetMatchingCards(handManager.Cards);

            // Update highlight on all matching cards every frame (for pulse animation)
            foreach (CardInstance card in matching)
            {
                if (card == null) continue;
                SetCardHighlight(card, true);
                if (!_highlightedCards.Contains(card))
                    _highlightedCards.Add(card);
            }

            // Remove highlight from cards no longer matching
            for (int i = _highlightedCards.Count - 1; i >= 0; i--)
            {
                CardInstance card = _highlightedCards[i];
                if (card == null || !matching.Contains(card))
                {
                    if (card != null) SetCardHighlight(card, false);
                    _highlightedCards.RemoveAt(i);
                }
            }
        }

        private void ClearCardHighlights()
        {
            foreach (CardInstance card in _highlightedCards)
            {
                if (card != null) SetCardHighlight(card, false);
            }
            _highlightedCards.Clear();
        }

        /// <summary>
        /// Apply or remove a pulsing green highlight on a card's graphic.
        /// Uses the card's Image component (the card face) for the tint.
        /// Pulses between white and green for visibility.
        /// </summary>
        private void SetCardHighlight(CardInstance card, bool highlighted)
        {
            if (card == null) return;

            Color color;
            if (highlighted)
            {
                // Pulse between white and green
                float pulse = Mathf.PingPong(Time.time * 4f, 1f);
                color = Color.Lerp(Color.white, matchingCardHighlight, pulse);
            }
            else
            {
                color = Color.white;
            }

            // Try a dedicated Graphic component on the card root first
            var graphic = card.GetComponent<Graphic>();
            if (graphic != null)
            {
                graphic.color = color;
                return;
            }

            // Fallback: tint the first Image found on the card
            var image = card.GetComponentInChildren<Image>();
            if (image != null)
                image.color = color;
        }

        // ── Event handlers ───────────────────────────────────────────────────

        private void OnParry(ParryEvent e)
        {
            // Successful parry — close the UI immediately
            if (e.Success)
                OnWindowClosed();
        }

        private void OnTurnPhaseChanged(TurnPhaseChangedEvent e)
        {
            // If we leave Enemy phase, ensure the UI is hidden and highlights cleared
            if (e.NewPhase != TurnPhase.Enemy)
            {
                OnWindowClosed();
                _wasWindowActive = false;
            }
        }
    }
}
