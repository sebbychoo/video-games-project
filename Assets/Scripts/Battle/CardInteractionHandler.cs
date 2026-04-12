using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CardBattle
{
    public class CardInteractionHandler : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        /// <summary>Set by HandManager on spawn.</summary>
        public CardInstance Card { get; set; }

        /// <summary>Set by HandManager on spawn.</summary>
        public HandManager HandManager { get; set; }

        /// <summary>Optional tooltip reference — set by HandManager or found in scene.</summary>
        public CardEffectPreview EffectPreview { get; set; }

        /// <summary>How long to hover before switching from a selected card.</summary>
        private const float SwitchHoverDelay = 0.25f;
        private float _hoverTimer;
        private bool _waitingToSwitch;

        /// <summary>Delayed exit to prevent flickering between overlapping cards.</summary>
        private const float ExitDelay = 0.05f;
        private float _exitTimer;
        private bool _pendingExit;

        /// <summary>Tracks whether this card is actively hovered to prevent re-trigger flicker at screen edges.</summary>
        private bool _isActivelyHovered;

        private void Update()
        {
            // Handle delayed exit to prevent flicker between overlapping cards
            if (_pendingExit)
            {
                _exitTimer += Time.deltaTime;
                if (_exitTimer >= ExitDelay)
                {
                    _pendingExit = false;
                    ProcessHoverExit();
                }
            }

            // Keyboard shortcuts for parry (Alpha1–Alpha9) during active parry window
            if (BattleManager.Instance != null
                && BattleManager.Instance.ParrySystem != null
                && BattleManager.Instance.ParrySystem.IsParryWindowActive
                && BattleManager.Instance.HandManager != null)
            {
                for (int k = 0; k < 9; k++)
                {
                    if (Input.GetKeyDown(KeyCode.Alpha1 + k))
                    {
                        List<CardInstance> matching = BattleManager.Instance.ParrySystem
                            .GetMatchingCards(BattleManager.Instance.HandManager.Cards);
                        if (k < matching.Count)
                            BattleManager.Instance.TryParryWithCard(matching[k]);
                        break;
                    }
                }
            }

            if (!_waitingToSwitch) return;

            _hoverTimer += Time.deltaTime;
            if (_hoverTimer >= SwitchHoverDelay)
            {
                _waitingToSwitch = false;

                // Deselect the current card and hover this one instead
                CardTargetingManager.Instance?.CancelSelection();

                Card.IsHovered = true;
                CardTargetingManager.Instance?.SetHoveredCard(Card);

                if (EffectPreview != null)
                    EffectPreview.Show(Card);

                if (HandManager != null)
                {
                    IReadOnlyList<CardInstance> cards = HandManager.Cards;
                    int index = -1;
                    for (int i = 0; i < cards.Count; i++)
                        if (cards[i] == Card) { index = i; break; }
                    HandManager.OnCardHoverEnter(Card, index);
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // Cancel any pending delayed exit — pointer came back before the exit fired
            if (_pendingExit)
            {
                _pendingExit = false;
                // Card is still actively hovered, no need to re-trigger hover animations
                return;
            }

            // If already actively hovered (rapid re-entry at screen edges), skip
            if (_isActivelyHovered) return;

            if (BattleManager.Instance == null) return;

            bool isPlayPhase = BattleManager.Instance.CurrentTurn == TurnPhase.Play;
            bool isParryWindow = BattleManager.Instance.ParrySystem != null
                && BattleManager.Instance.ParrySystem.IsParryWindowActive;

            // During parry window, block hover on non-matching cards
            if (isParryWindow)
            {
                if (!IsMatchingCard()) return;
            }

            if (!isPlayPhase && !isParryWindow) return;

            // If a different card is selected, start the switch timer
            if (CardTargetingManager.Instance != null
                && CardTargetingManager.Instance.HasSelectedCard
                && CardTargetingManager.Instance.SelectedCard != Card)
            {
                _hoverTimer = 0f;
                _waitingToSwitch = true;
                return;
            }

            // If this card is the selected one, ignore hover
            if (CardTargetingManager.Instance != null
                && CardTargetingManager.Instance.SelectedCard == Card)
                return;

            Card.IsHovered = true;
            _isActivelyHovered = true;
            CardTargetingManager.Instance?.SetHoveredCard(Card);

            // Show description on hover
            CardVisual visual = Card.GetComponent<CardVisual>();
            if (visual != null) visual.ShowDescription();

            // Show effect preview tooltip
            if (EffectPreview != null)
                EffectPreview.Show(Card);

            if (HandManager != null)
            {
                IReadOnlyList<CardInstance> cards = HandManager.Cards;
                int index = -1;
                for (int i = 0; i < cards.Count; i++)
                    if (cards[i] == Card) { index = i; break; }
                HandManager.OnCardHoverEnter(Card, index);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // Cancel any pending switch
            _waitingToSwitch = false;

            // Always hide tooltip on exit, regardless of phase or selection state
            if (Card != null)
            {
                CardVisual visual = Card.GetComponent<CardVisual>();
                if (visual != null) visual.HideDescription();
            }

            if (BattleManager.Instance == null) return;

            bool isPlayPhase = BattleManager.Instance.CurrentTurn == TurnPhase.Play;
            bool isParryWindow = BattleManager.Instance.ParrySystem != null
                && BattleManager.Instance.ParrySystem.IsParryWindowActive;

            if (!isPlayPhase && !isParryWindow) return;

            // If a card is selected, ignore hover exit for card position
            if (CardTargetingManager.Instance != null
                && CardTargetingManager.Instance.HasSelectedCard)
                return;

            if (Card == null) return;

            // Delay the actual exit to prevent flickering between overlapping cards
            _pendingExit = true;
            _exitTimer = 0f;
        }

        /// <summary>Actually process the hover exit after the delay.</summary>
        private void ProcessHoverExit()
        {
            if (Card == null) return;
            Card.IsHovered = false;
            _isActivelyHovered = false;
            CardTargetingManager.Instance?.ClearHoveredCard(Card);

            // Hide effect preview tooltip
            if (EffectPreview != null)
                EffectPreview.Hide();

            if (HandManager != null)
                HandManager.OnCardHoverExit(Card);
        }

        /// <summary>
        /// Check whether this card is in the current parry window's matching cards list.
        /// Returns false if no parry window is active or the card is null.
        /// </summary>
        private bool IsMatchingCard()
        {
            if (Card == null || BattleManager.Instance == null) return false;
            ParrySystem ps = BattleManager.Instance.ParrySystem;
            if (ps == null || !ps.IsParryWindowActive) return false;
            HandManager hm = BattleManager.Instance.HandManager;
            if (hm == null) return false;
            List<CardInstance> matching = ps.GetMatchingCards(hm.Cards);
            return matching.Contains(Card);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (BattleManager.Instance == null) return;

            // During parry window, only left-click on matching cards triggers parry
            if (BattleManager.Instance.ParrySystem != null
                && BattleManager.Instance.ParrySystem.IsParryWindowActive)
            {
                if (eventData.button != PointerEventData.InputButton.Left) return;
                if (!IsMatchingCard()) return;
                BattleManager.Instance.TryParryWithCard(Card);
                return;
            }

            if (BattleManager.Instance.CurrentTurn != TurnPhase.Play) return;

            // Right click cancels selection
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                CardTargetingManager.Instance?.CancelSelection();
                return;
            }

            // Left click selects this card for targeting
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                CardTargetingManager.Instance?.SelectCard(Card);
            }
        }
    }
}
