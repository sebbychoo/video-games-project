using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using CardBattle;

namespace CardBattle.Tests
{
    [TestFixture]
    public class DeckManagerTests
    {
        private GameObject _go;
        private DeckManager _deckManager;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("TestDeckManager");
            _deckManager = _go.AddComponent<DeckManager>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        private CardData CreateCard(string name)
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.cardName = name;
            return card;
        }

        private List<CardData> CreateDeck(int count)
        {
            var cards = new List<CardData>();
            for (int i = 0; i < count; i++)
                cards.Add(CreateCard($"Card_{i}"));
            return cards;
        }

        #region AddCard

        [Test]
        public void AddCard_AddsToDiscardPile()
        {
            _deckManager.Initialize(new List<CardData>());
            var card = CreateCard("NewCard");

            _deckManager.AddCard(card);

            Assert.AreEqual(1, _deckManager.DiscardCount);
            Assert.AreEqual(0, _deckManager.DeckCount);
            Assert.IsTrue(_deckManager.DiscardPile.Contains(card));
        }

        [Test]
        public void AddCard_NullCard_DoesNothing()
        {
            _deckManager.Initialize(new List<CardData>());

            _deckManager.AddCard(null);

            Assert.AreEqual(0, _deckManager.DiscardCount);
        }

        [Test]
        public void AddCard_CardEntersCycleOnReshuffle()
        {
            var deck = CreateDeck(2);
            _deckManager.Initialize(deck);

            // Draw all cards to empty the draw pile
            var drawn1 = _deckManager.Draw();
            var drawn2 = _deckManager.Draw();
            Assert.AreEqual(0, _deckManager.DeckCount);

            // Discard them and add a new card
            _deckManager.Discard(drawn1);
            _deckManager.Discard(drawn2);
            var newCard = CreateCard("AddedCard");
            _deckManager.AddCard(newCard);

            Assert.AreEqual(3, _deckManager.DiscardCount);

            // Draw triggers reshuffle — all 3 cards should be in the draw pile
            var drawn3 = _deckManager.Draw();
            Assert.IsNotNull(drawn3);
            // Total across draw + discard should be 2 (3 in discard, reshuffled to draw, drew 1)
            Assert.AreEqual(2, _deckManager.DeckCount);
            Assert.AreEqual(0, _deckManager.DiscardCount);
        }

        #endregion

        #region ReadOnly Pile Access

        [Test]
        public void DrawPile_ReturnsCurrentDrawPileContents()
        {
            var deck = CreateDeck(3);
            _deckManager.Initialize(deck);

            Assert.AreEqual(3, _deckManager.DrawPile.Count);
            // All original cards should be present (order may differ due to shuffle)
            foreach (var card in deck)
                Assert.IsTrue(_deckManager.DrawPile.Contains(card));
        }

        [Test]
        public void DiscardPile_ReturnsCurrentDiscardPileContents()
        {
            var deck = CreateDeck(2);
            _deckManager.Initialize(deck);

            var drawn = _deckManager.Draw();
            _deckManager.Discard(drawn);

            Assert.AreEqual(1, _deckManager.DiscardPile.Count);
            Assert.AreEqual(drawn, _deckManager.DiscardPile[0]);
        }

        #endregion

        #region Draw with Reshuffle

        [Test]
        public void Draw_EmptyDeck_ReshufflesDiscard()
        {
            var deck = CreateDeck(2);
            _deckManager.Initialize(deck);

            // Draw all and discard
            var d1 = _deckManager.Draw();
            var d2 = _deckManager.Draw();
            _deckManager.Discard(d1);
            _deckManager.Discard(d2);

            Assert.AreEqual(0, _deckManager.DeckCount);
            Assert.AreEqual(2, _deckManager.DiscardCount);

            // Next draw should trigger reshuffle
            var drawn = _deckManager.Draw();
            Assert.IsNotNull(drawn);
            Assert.AreEqual(1, _deckManager.DeckCount);
            Assert.AreEqual(0, _deckManager.DiscardCount);
        }

        [Test]
        public void Draw_BothPilesEmpty_ReturnsNull()
        {
            _deckManager.Initialize(new List<CardData>());

            var result = _deckManager.Draw();

            Assert.IsNull(result);
        }

        [Test]
        public void Draw_EmptyDeckEmptyDiscard_AfterDrawingAll_ReturnsNull()
        {
            var deck = CreateDeck(1);
            _deckManager.Initialize(deck);

            var drawn = _deckManager.Draw();
            Assert.IsNotNull(drawn);

            // Don't discard — both piles empty
            var result = _deckManager.Draw();
            Assert.IsNull(result);
        }

        #endregion

        #region Card Conservation

        [Test]
        public void CardConservation_TotalCountPreservedAcrossOperations()
        {
            var deck = CreateDeck(5);
            _deckManager.Initialize(deck);
            int totalCards = 5;

            // Draw some cards (simulating hand)
            var hand = new List<CardData>();
            for (int i = 0; i < 3; i++)
            {
                var card = _deckManager.Draw();
                if (card != null) hand.Add(card);
            }

            // Total = draw + hand + discard
            Assert.AreEqual(totalCards, _deckManager.DeckCount + hand.Count + _deckManager.DiscardCount);

            // Discard hand
            foreach (var card in hand)
                _deckManager.Discard(card);
            hand.Clear();

            Assert.AreEqual(totalCards, _deckManager.DeckCount + _deckManager.DiscardCount);

            // Draw again (may trigger reshuffle)
            for (int i = 0; i < 2; i++)
            {
                var card = _deckManager.Draw();
                if (card != null) hand.Add(card);
            }

            Assert.AreEqual(totalCards, _deckManager.DeckCount + hand.Count + _deckManager.DiscardCount);
        }

        [Test]
        public void CardConservation_AddCardIncreasesTotal()
        {
            var deck = CreateDeck(3);
            _deckManager.Initialize(deck);

            var newCard = CreateCard("Extra");
            _deckManager.AddCard(newCard);

            // Total should now be 4 (3 in draw + 1 in discard)
            Assert.AreEqual(4, _deckManager.DeckCount + _deckManager.DiscardCount);
        }

        #endregion
    }
}
