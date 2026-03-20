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

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (BattleManager.Instance == null) return;
            if (BattleManager.Instance.CurrentTurn != TurnState.PlayerTurn) return;

            // If a card is already selected, don't do anything on hover
            if (CardTargetingManager.Instance != null
                && CardTargetingManager.Instance.HasSelectedCard)
                return;

            Card.IsHovered = true;
            CardTargetingManager.Instance?.SetHoveredCard(Card);

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
            if (BattleManager.Instance == null) return;
            if (BattleManager.Instance.CurrentTurn != TurnState.PlayerTurn) return;

            // If a card is selected, ignore hover exit
            if (CardTargetingManager.Instance != null
                && CardTargetingManager.Instance.HasSelectedCard)
                return;

            Card.IsHovered = false;
            CardTargetingManager.Instance?.ClearHoveredCard(Card);

            if (HandManager != null)
                HandManager.OnCardHoverExit(Card);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (BattleManager.Instance == null) return;
            if (BattleManager.Instance.CurrentTurn != TurnState.PlayerTurn) return;

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
