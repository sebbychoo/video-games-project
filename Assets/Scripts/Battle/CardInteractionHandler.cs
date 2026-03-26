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

        private void Update()
        {
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
            if (BattleManager.Instance == null) return;

            bool isPlayPhase = BattleManager.Instance.CurrentTurn == TurnPhase.Play;
            bool isParryWindow = BattleManager.Instance.ParrySystem != null
                && BattleManager.Instance.ParrySystem.IsParryWindowActive
                && Card != null && Card.Data != null
                && Card.Data.cardType == CardType.Defense;

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
                && BattleManager.Instance.ParrySystem.IsParryWindowActive
                && Card != null && Card.Data != null
                && Card.Data.cardType == CardType.Defense;

            if (!isPlayPhase && !isParryWindow) return;

            // If a card is selected, ignore hover exit for card position
            if (CardTargetingManager.Instance != null
                && CardTargetingManager.Instance.HasSelectedCard)
                return;

            if (Card == null) return;
            Card.IsHovered = false;
            CardTargetingManager.Instance?.ClearHoveredCard(Card);

            // Hide effect preview tooltip
            if (EffectPreview != null)
                EffectPreview.Hide();

            if (HandManager != null)
                HandManager.OnCardHoverExit(Card);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (BattleManager.Instance == null) return;

            // During parry window, clicking a Defense card triggers parry
            if (BattleManager.Instance.ParrySystem != null
                && BattleManager.Instance.ParrySystem.IsParryWindowActive
                && Card != null && Card.Data != null
                && Card.Data.cardType == CardType.Defense
                && eventData.button == PointerEventData.InputButton.Left)
            {
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
