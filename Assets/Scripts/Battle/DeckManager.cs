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
