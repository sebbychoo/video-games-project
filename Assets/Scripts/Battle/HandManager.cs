using System.Collections.Generic;
using UnityEngine;

namespace CardBattle
{
    public class HandManager : MonoBehaviour
    {
        [SerializeField] CardLayoutController layout;
        [SerializeField] CardAnimator         animator;
        [SerializeField] GameObject           cardPrefab;

        private readonly List<CardInstance> _cards = new List<CardInstance>();

        public IReadOnlyList<CardInstance> Cards => _cards;

        /// <summary>Add a batch of cards at once so layout is calculated with the final count.</summary>
        public void AddCards(List<CardData> cards)
        {
            var newInstances = new List<CardInstance>();
            foreach (CardData data in cards)
            {
                CardInstance instance = SpawnCard(data);
                newInstances.Add(instance);
            }

            layout.RefreshLayout(_cards);
            for (int i = 0; i < _cards.Count; i++)
            {
                CardTransformTarget target = layout.GetTargetTransform(i, _cards.Count);
                _cards[i].ArcTarget = target;
                float delay = i * 0.08f;
                animator.PlayEntrance(_cards[i], target, delay);
            }
        }

        /// <summary>Add a single card (e.g. drawn mid-turn). Recalculates full layout.</summary>
        public void AddCard(CardData data)
        {
            CardInstance instance = SpawnCard(data);

            layout.RefreshLayout(_cards);
            // Snap existing cards to new positions, animate only the new one
            for (int i = 0; i < _cards.Count - 1; i++)
            {
                CardTransformTarget t = layout.GetTargetTransform(i, _cards.Count);
                _cards[i].ArcTarget = t;
            }
            int newIndex = _cards.Count - 1;
            CardTransformTarget target = layout.GetTargetTransform(newIndex, _cards.Count);
            instance.ArcTarget = target;
            animator.PlayEntrance(instance, target, 0f);
        }

        public void RemoveCard(CardInstance card)
        {
            _cards.Remove(card);
            Destroy(card.gameObject);
            RefreshAllArcTargets();
        }

        public void DiscardAll()
        {
            if (_cards.Count == 0) return;
            for (int i = _cards.Count - 1; i >= 0; i--)
                Destroy(_cards[i].gameObject);
            _cards.Clear();
        }

        public void OnCardHoverEnter(CardInstance card, int index)
        {
            // Don't lift if already selected
            if (card.IsSelected) return;

            CardTransformTarget target = layout.GetTargetTransform(index, _cards.Count);
            card.ArcTarget = target;
            animator.PlayHoverEnter(card, target);
            layout.RefreshLayout(_cards, index);
        }

        public void OnCardHoverExit(CardInstance card)
        {
            // If selected, keep it lifted — don't return to arc
            if (card.IsSelected) return;

            int index = _cards.IndexOf(card);
            if (index < 0) return;
            CardTransformTarget target = layout.GetTargetTransform(index, _cards.Count);
            card.ArcTarget = target;
            animator.PlayHoverExit(card, target);
            layout.RefreshLayout(_cards, -1);
        }

        /// <summary>Called by CardTargetingManager when a card is selected — keeps it lifted.</summary>
        public void OnCardSelected(CardInstance card)
        {
            card.IsSelected = true;
            // Keep the card at its hovered (lifted) position — no animation needed
        }

        /// <summary>Called by CardTargetingManager when selection is cancelled.</summary>
        public void OnCardDeselected(CardInstance card)
        {
            card.IsSelected = false;
            int index = _cards.IndexOf(card);
            if (index < 0) return;
            CardTransformTarget target = layout.GetTargetTransform(index, _cards.Count);
            card.ArcTarget = target;
            animator.PlayHoverExit(card, target);
            layout.RefreshLayout(_cards, -1);
        }

        // ── private ──────────────────────────────────────────────────────────

        private CardInstance SpawnCard(CardData data)
        {
            GameObject go = Instantiate(cardPrefab, transform);
            CardInstance instance = go.GetComponent<CardInstance>();
            instance.Data = data;

            CardInteractionHandler handler = go.GetComponent<CardInteractionHandler>();
            if (handler != null)
            {
                handler.Card        = instance;
                handler.HandManager = this;
            }
            _cards.Add(instance);
            return instance;
        }

        private void RefreshAllArcTargets()
        {
            layout.RefreshLayout(_cards);
            for (int i = 0; i < _cards.Count; i++)
                _cards[i].ArcTarget = layout.GetTargetTransform(i, _cards.Count);
        }
    }
}
