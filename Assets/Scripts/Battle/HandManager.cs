using System.Collections.Generic;
using UnityEngine;

namespace CardBattle
{
    public class HandManager : MonoBehaviour
    {
        [SerializeField] CardLayoutController layout;
        [SerializeField] CardAnimator         animator;
        [SerializeField] GameObject           cardPrefab;
        [SerializeField] CardEffectPreview    effectPreview;
        [SerializeField] Transform            handContainer;

        private readonly List<CardInstance> _cards = new List<CardInstance>();

        private void Awake()
        {
            if (layout == null) layout = GetComponent<CardLayoutController>();
            if (animator == null) animator = GetComponent<CardAnimator>();
        }

        /// <summary>The parent transform where cards are spawned. Falls back to HandContainer in scene.</summary>
        private Transform CardParent
        {
            get
            {
                if (handContainer != null) return handContainer;
                // Try to find HandContainer in the scene
                GameObject hc = GameObject.Find("HandContainer");
                if (hc == null) hc = GameObject.Find("HandContainer ");
                if (hc != null) handContainer = hc.transform;
                return handContainer != null ? handContainer : transform;
            }
        }

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
            if (animator != null) animator.StopSelectedIdle(card);
            _cards.Remove(card);
            Destroy(card.gameObject);
            // Animate remaining cards smoothly to new centered positions
            for (int i = 0; i < _cards.Count; i++)
            {
                CardTransformTarget target = layout.GetTargetTransform(i, _cards.Count);
                _cards[i].ArcTarget = target;
                _cards[i].IsSelected = false;
                if (animator != null) animator.PlayHoverExit(_cards[i], target);
            }
        }

        public void DiscardAll()
        {
            if (_cards.Count == 0) return;
            if (animator != null)
            {
                foreach (var card in _cards)
                    animator.StopSelectedIdle(card);
                animator.StopAll();
            }
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

        /// <summary>Called by CardTargetingManager when a card is clicked — pops bigger + shakes. Lifts first if not already hovered.</summary>
        public void OnCardSelected(CardInstance card)
        {
            card.IsSelected = true;
            int index = _cards.IndexOf(card);
            if (index < 0) return;
            CardTransformTarget target = layout.GetTargetTransform(index, _cards.Count);
            card.ArcTarget = target;

            if (card.IsHovered)
            {
                // Already lifted from hover — just pop
                if (animator != null) animator.PlaySelectPop(card);
            }
            else
            {
                // Not hovered — lift first, then pop
                if (animator != null) animator.PlayHoverEnter(card, target);
                StartCoroutine(DelayedSelectPop(card, 0.15f));
                layout.RefreshLayout(_cards, index);
            }
        }

        private System.Collections.IEnumerator DelayedSelectPop(CardInstance card, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (card != null && card.IsSelected)
                animator.PlaySelectPop(card);
        }

        /// <summary>Called by CardTargetingManager when selection is cancelled.</summary>
        public void OnCardDeselected(CardInstance card)
        {
            if (card == null) return;
            card.IsSelected = false;
            if (animator != null) animator.StopSelectedIdle(card);
            int index = _cards.IndexOf(card);
            if (index < 0) return;
            if (layout == null) return;
            CardTransformTarget target = layout.GetTargetTransform(index, _cards.Count);
            card.ArcTarget = target;
            if (animator != null) animator.PlayHoverExit(card, target);
            layout.RefreshLayout(_cards, -1);
        }

        // ── private ──────────────────────────────────────────────────────────

        private CardInstance SpawnCard(CardData data)
        {
            GameObject go = Instantiate(cardPrefab, CardParent);
            CardInstance instance = go.GetComponent<CardInstance>();
            instance.Data = data;

            CardInteractionHandler handler = go.GetComponent<CardInteractionHandler>();
            if (handler != null)
            {
                handler.Card        = instance;
                handler.HandManager = this;
                handler.EffectPreview = effectPreview;
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
