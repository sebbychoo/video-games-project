using System.Collections.Generic;
using UnityEngine;

namespace CardBattle
{
    public class DeckManager : MonoBehaviour
    {
        private readonly List<CardData> _deck    = new List<CardData>();
        private readonly List<CardData> _discard = new List<CardData>();

        public int DeckCount    => _deck.Count;
        public int DiscardCount => _discard.Count;

        /// <summary>Read-only view of the draw pile contents (for UI inspection).</summary>
        public IReadOnlyList<CardData> DrawPile => _deck;

        /// <summary>Read-only view of the discard pile contents (for UI inspection).</summary>
        public IReadOnlyList<CardData> DiscardPile => _discard;

        /// <summary>Copies cards into the internal deck and shuffles.</summary>
        public void Initialize(List<CardData> cards)
        {
            _deck.Clear();
            _discard.Clear();
            _deck.AddRange(cards);
            Shuffle(_deck);
        }

        /// <summary>
        /// Returns the top card of the deck.
        /// If the deck is empty, shuffles the discard pile into the deck first.
        /// Returns null if both piles are empty.
        /// </summary>
        public CardData Draw()
        {
            if (_deck.Count == 0)
            {
                if (_discard.Count == 0)
                    return null;

                ShuffleDiscardIntoDeck();
            }

            int last = _deck.Count - 1;
            CardData card = _deck[last];
            _deck.RemoveAt(last);
            return card;
        }

        /// <summary>Adds a card to the discard pile.</summary>
        public void Discard(CardData card)
        {
            _discard.Add(card);
        }

        /// <summary>
        /// Adds a new card to the discard pile for mid-run deck additions.
        /// The card enters the cycle naturally and will be shuffled into the draw pile
        /// when the discard is next reshuffled.
        /// </summary>
        public void AddCard(CardData card)
        {
            if (card == null) return;
            _discard.Add(card);
        }

        /// <summary>
        /// Attempts to add a card to the deck, respecting the maximum deck size limit.
        /// Returns true if the card was added, false if the deck is full.
        /// </summary>
        /// <param name="card">The card to add.</param>
        /// <param name="maxDeckSize">Maximum allowed deck size.</param>
        /// <returns>True if added, false if deck is at capacity.</returns>
        public bool TryAddCard(CardData card, int maxDeckSize)
        {
            if (card == null) return false;
            if (TotalCardCount >= maxDeckSize) return false;
            _discard.Add(card);
            return true;
        }

        /// <summary>Total cards across draw pile, hand (not tracked here), and discard pile.</summary>
        public int TotalCardCount => _deck.Count + _discard.Count;

        /// <summary>
        /// Retrieve N random cards from the discard pile and return them.
        /// Removes the retrieved cards from the discard pile. (Req 19.3)
        /// </summary>
        public List<CardData> RetrieveFromDiscard(int count)
        {
            var retrieved = new List<CardData>();
            if (count <= 0 || _discard.Count == 0) return retrieved;

            int actual = Mathf.Min(count, _discard.Count);

            // Fisher-Yates partial shuffle to pick random cards
            for (int i = 0; i < actual; i++)
            {
                int j = Random.Range(i, _discard.Count);
                CardData tmp = _discard[i];
                _discard[i] = _discard[j];
                _discard[j] = tmp;
            }

            // Take the first 'actual' cards
            for (int i = 0; i < actual; i++)
                retrieved.Add(_discard[i]);

            _discard.RemoveRange(0, actual);
            return retrieved;
        }

        /// <summary>
        /// Peek at the top N cards of the draw pile without removing them. (Req 19.4)
        /// Returns up to N cards from the top (end) of the draw pile.
        /// </summary>
        public List<CardData> PeekTop(int count)
        {
            var result = new List<CardData>();
            if (count <= 0 || _deck.Count == 0) return result;

            int actual = Mathf.Min(count, _deck.Count);
            for (int i = _deck.Count - 1; i >= _deck.Count - actual; i--)
                result.Add(_deck[i]);

            return result;
        }

        /// <summary>Moves all cards from the discard pile into the deck and shuffles.</summary>
        public void ShuffleDiscardIntoDeck()
        {
            _deck.AddRange(_discard);
            _discard.Clear();
            Shuffle(_deck);
        }

        // Fisher-Yates shuffle
        private static void Shuffle(List<CardData> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                CardData tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }
    }
}
