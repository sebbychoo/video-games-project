using System.Collections;
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
    /// Manages card dimming, slide-forward animations, attack queue indicator,
    /// number key labels, perfect parry flash, and on-parry effect labels.
    /// Hides itself when no parry window is active.
    /// Requirements: 1.2–1.4, 2.1, 2.4, 6.1, 6.2, 6.8, 8.2–8.6, 9.5, 9.6, 10.3, 11.7, 11.8
    /// </summary>
    public class ParryWindowUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ParrySystem parrySystem;
        [SerializeField] private HandManager handManager;
        [SerializeField] private CardAnimator cardAnimator;

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

        [Header("Dimming")]
        [SerializeField] private float dimmedAlpha = 0.4f;

        [Header("Attack Queue")]
        [SerializeField] private TextMeshProUGUI attackQueueText;

        [Header("Perfect Parry")]
        [SerializeField] private GameObject perfectParryFlashRoot;
        [SerializeField] private TextMeshProUGUI perfectParryText;

        [Header("Card Labels")]
        [SerializeField] private GameObject numberKeyLabelPrefab;
        [SerializeField] private GameObject parryEffectLabelPrefab;

        // Cards currently highlighted so we can clear them when the window closes
        private readonly List<CardInstance> _highlightedCards = new List<CardInstance>();
        private readonly List<CardInstance> _dimmedCards = new List<CardInstance>();
        private readonly List<CardInstance> _slidForwardCards = new List<CardInstance>();
        private readonly List<GameObject> _numberKeyLabels = new List<GameObject>();
        private readonly List<GameObject> _effectLabels = new List<GameObject>();
        private bool _wasWindowActive;
        private Coroutine _perfectFlashCoroutine;

        private void Awake()
        {
            // Only hide the panel child — keep this MonoBehaviour active so Update() runs
            if (panelRoot != null && panelRoot != gameObject)
                panelRoot.SetActive(false);

            if (perfectParryFlashRoot != null)
                perfectParryFlashRoot.SetActive(false);

            if (attackQueueText != null)
                attackQueueText.gameObject.SetActive(false);
        }

        private void Start()
        {
            // Auto-locate dependencies if not wired in Inspector
            if (parrySystem == null)
                parrySystem = FindObjectOfType<ParrySystem>();
            if (handManager == null)
                handManager = FindObjectOfType<HandManager>();
            if (cardAnimator == null)
                cardAnimator = FindObjectOfType<CardAnimator>();

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
            if (cardAnimator == null)
                cardAnimator = FindObjectOfType<CardAnimator>();

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
            ApplyDimming();
            ApplySlideForward();
            ShowAttackQueueIndicator();
            ShowNumberKeyLabels();
            ShowOnParryEffectLabels();
        }

        private void OnWindowClosed()
        {
            Debug.Log("[ParryWindowUI] Window CLOSED");
            if (panelRoot != null)
                panelRoot.SetActive(false);

            ClearCardHighlights();
            ClearDimming();
            ClearSlideForward();
            HideAttackQueueIndicator();
            ClearNumberKeyLabels();
            ClearEffectLabels();
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

        // ── Card dimming (Req 2.1, 2.4) ─────────────────────────────────────

        /// <summary>
        /// Dim all non-matching cards by reducing their alpha.
        /// Called once when the parry window opens.
        /// </summary>
        private void ApplyDimming()
        {
            if (parrySystem == null || handManager == null) return;

            List<CardInstance> matching = parrySystem.GetMatchingCards(handManager.Cards);

            foreach (CardInstance card in handManager.Cards)
            {
                if (card == null) continue;
                if (matching.Contains(card)) continue;

                // Apply dimmed alpha via CanvasGroup (add one if missing)
                CanvasGroup cg = card.GetComponent<CanvasGroup>();
                if (cg == null)
                    cg = card.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = dimmedAlpha;

                _dimmedCards.Add(card);
            }
        }

        /// <summary>
        /// Restore all dimmed cards to full alpha.
        /// Called when the parry window closes.
        /// </summary>
        private void ClearDimming()
        {
            foreach (CardInstance card in _dimmedCards)
            {
                if (card == null) continue;
                CanvasGroup cg = card.GetComponent<CanvasGroup>();
                if (cg != null)
                    cg.alpha = 1f;
            }
            _dimmedCards.Clear();
        }

        // ── Slide-forward animation (Req 1.2, 1.3, 1.4) ─────────────────────

        /// <summary>
        /// Slide matching cards forward to indicate they are parry-eligible.
        /// Called once when the parry window opens.
        /// </summary>
        private void ApplySlideForward()
        {
            if (cardAnimator == null || parrySystem == null || handManager == null) return;

            List<CardInstance> matching = parrySystem.GetMatchingCards(handManager.Cards);

            foreach (CardInstance card in matching)
            {
                if (card == null) continue;
                cardAnimator.PlaySlideForward(card, card.ArcTarget);
                _slidForwardCards.Add(card);
            }
        }

        /// <summary>
        /// Return all slid-forward cards to their arc positions.
        /// Called when the parry window closes.
        /// </summary>
        private void ClearSlideForward()
        {
            if (cardAnimator == null) return;

            foreach (CardInstance card in _slidForwardCards)
            {
                if (card == null) continue;
                cardAnimator.PlaySlideBack(card, card.ArcTarget);
            }
            _slidForwardCards.Clear();
        }

        // ── Attack queue indicator (Req 8.2–8.6) ────────────────────────────

        /// <summary>
        /// Display "Attack X of Y" indicator when the parry window opens.
        /// </summary>
        private void ShowAttackQueueIndicator()
        {
            if (attackQueueText == null) return;

            if (BattleManager.Instance != null)
            {
                int current = BattleManager.Instance.AttackQueueCurrent;
                int total   = BattleManager.Instance.AttackQueueTotal;
                attackQueueText.text = $"Attack {current} of {total}";
            }

            attackQueueText.gameObject.SetActive(true);
        }

        /// <summary>
        /// Hide the attack queue indicator when the parry window closes.
        /// </summary>
        private void HideAttackQueueIndicator()
        {
            if (attackQueueText != null)
                attackQueueText.gameObject.SetActive(false);
        }

        // ── Number key labels (Req 9.5, 9.6) ────────────────────────────────

        /// <summary>
        /// Instantiate number key label overlays ("1", "2", etc.) on each matching card.
        /// Called once when the parry window opens.
        /// </summary>
        private void ShowNumberKeyLabels()
        {
            if (parrySystem == null || handManager == null) return;

            List<CardInstance> matching = parrySystem.GetMatchingCards(handManager.Cards);

            for (int i = 0; i < matching.Count && i < 9; i++)
            {
                CardInstance card = matching[i];
                if (card == null) continue;

                if (numberKeyLabelPrefab != null)
                {
                    GameObject label = Instantiate(numberKeyLabelPrefab, card.transform);
                    TextMeshProUGUI tmp = label.GetComponentInChildren<TextMeshProUGUI>();
                    if (tmp != null)
                        tmp.text = (i + 1).ToString();
                    _numberKeyLabels.Add(label);
                }
                else
                {
                    // Fallback: create a simple text label dynamically
                    GameObject labelGO = new GameObject($"KeyLabel_{i + 1}");
                    labelGO.transform.SetParent(card.transform, false);

                    RectTransform rt = labelGO.AddComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.5f, 1f);
                    rt.anchorMax = new Vector2(0.5f, 1f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(0f, 20f);
                    rt.sizeDelta = new Vector2(30f, 30f);

                    TextMeshProUGUI tmp = labelGO.AddComponent<TextMeshProUGUI>();
                    tmp.text = (i + 1).ToString();
                    tmp.fontSize = 18f;
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.color = Color.yellow;

                    _numberKeyLabels.Add(labelGO);
                }
            }
        }

        /// <summary>
        /// Destroy all number key label instances.
        /// Called when the parry window closes.
        /// </summary>
        private void ClearNumberKeyLabels()
        {
            foreach (GameObject label in _numberKeyLabels)
            {
                if (label != null)
                    Destroy(label);
            }
            _numberKeyLabels.Clear();
        }

        // ── Perfect parry flash (Req 10.3) ───────────────────────────────────

        /// <summary>
        /// Briefly display "PERFECT" text with a scale-punch animation.
        /// Auto-hides after ~0.5s.
        /// </summary>
        public void ShowPerfectParryFlash()
        {
            if (perfectParryFlashRoot == null) return;

            if (_perfectFlashCoroutine != null)
                StopCoroutine(_perfectFlashCoroutine);

            _perfectFlashCoroutine = StartCoroutine(PerfectParryFlashRoutine());
        }

        private IEnumerator PerfectParryFlashRoutine()
        {
            perfectParryFlashRoot.SetActive(true);

            if (perfectParryText != null)
                perfectParryText.text = "PERFECT";

            // Scale-punch: start at 0.5x, punch up to 1.3x, settle at 1x
            RectTransform rt = perfectParryFlashRoot.GetComponent<RectTransform>();
            if (rt != null)
            {
                float elapsed = 0f;
                float punchDuration = 0.15f;

                // Scale up
                while (elapsed < punchDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / punchDuration);
                    float scale = Mathf.Lerp(0.5f, 1.3f, t);
                    rt.localScale = new Vector3(scale, scale, 1f);
                    yield return null;
                }

                // Scale down to 1x
                elapsed = 0f;
                float settleDuration = 0.1f;
                while (elapsed < settleDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / settleDuration);
                    float scale = Mathf.Lerp(1.3f, 1f, t);
                    rt.localScale = new Vector3(scale, scale, 1f);
                    yield return null;
                }

                rt.localScale = Vector3.one;
            }

            // Hold visible briefly
            yield return new WaitForSeconds(0.25f);

            perfectParryFlashRoot.SetActive(false);
            _perfectFlashCoroutine = null;
        }

        // ── On-parry effect labels (Req 11.7, 11.8) ─────────────────────────

        /// <summary>
        /// Show on-parry effect descriptions on matching cards that have non-None effects.
        /// Called once when the parry window opens.
        /// </summary>
        private void ShowOnParryEffectLabels()
        {
            if (parrySystem == null || handManager == null) return;

            List<CardInstance> matching = parrySystem.GetMatchingCards(handManager.Cards);

            foreach (CardInstance card in matching)
            {
                if (card == null || card.Data == null) continue;
                if (card.Data.onParryEffect == ParryEffectType.None) continue;

                string effectText = FormatParryEffectText(card.Data.onParryEffect, card.Data.onParryEffectValue);

                if (parryEffectLabelPrefab != null)
                {
                    GameObject label = Instantiate(parryEffectLabelPrefab, card.transform);
                    TextMeshProUGUI tmp = label.GetComponentInChildren<TextMeshProUGUI>();
                    if (tmp != null)
                        tmp.text = effectText;
                    _effectLabels.Add(label);
                }
                else
                {
                    // Fallback: create a simple text label dynamically
                    GameObject labelGO = new GameObject("ParryEffectLabel");
                    labelGO.transform.SetParent(card.transform, false);

                    RectTransform rt = labelGO.AddComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.5f, 0f);
                    rt.anchorMax = new Vector2(0.5f, 0f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(0f, -20f);
                    rt.sizeDelta = new Vector2(120f, 24f);

                    TextMeshProUGUI tmp = labelGO.AddComponent<TextMeshProUGUI>();
                    tmp.text = effectText;
                    tmp.fontSize = 12f;
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.color = new Color(1f, 0.9f, 0.3f);

                    _effectLabels.Add(labelGO);
                }
            }
        }

        /// <summary>
        /// Format a human-readable description for an on-parry effect.
        /// </summary>
        private static string FormatParryEffectText(ParryEffectType effect, int value)
        {
            switch (effect)
            {
                case ParryEffectType.CounterDamage: return $"Counter: {value} DMG";
                case ParryEffectType.RestoreOT:     return $"+{value} OT";
                case ParryEffectType.DrawCard:      return $"Draw {value}";
                default:                            return "";
            }
        }

        /// <summary>
        /// Destroy all on-parry effect label instances.
        /// Called when the parry window closes.
        /// </summary>
        private void ClearEffectLabels()
        {
            foreach (GameObject label in _effectLabels)
            {
                if (label != null)
                    Destroy(label);
            }
            _effectLabels.Clear();
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
