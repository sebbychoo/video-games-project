using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using CardBattle;

namespace CardBattle.Tests
{
    /// <summary>
    /// Property-based tests for DeckManager.
    /// Uses randomized inputs across many iterations to verify correctness properties
    /// for card conservation, deck cycling, and draw phase hand size.
    /// </summary>
    [TestFixture]
    public class DeckManagerPropertyTests
    {
        private const int Iterations = 200;
        private const int DefaultHandSize = 5;

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
            UnityEngine.Object.DestroyImmediate(_go);
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

        #region Property 1: Card Count Conservation

        /// <summary>
        /// Property 1: For any encounter state and any sequence of operations (draw, play,
        /// discard, shuffle), the total number of cards across Draw_Pile, Hand, and Discard_Pile
        /// shall remain constant and equal to the deck size at encounter start.
        /// Validates: Requirements 7.4, 5.5, 19.7
        /// </summary>
        [Test]
        public void Property1_CardCountConservation_AcrossRandomOperations()
        {
            var rng = new System.Random(42);

            for (int i = 0; i < Iterations; i++)
            {
                int deckSize = rng.Next(1, 30);
                var deck = CreateDeck(deckSize);
                _deckManager.Initialize(deck);

                var hand = new List<CardData>();
                int totalExpected = deckSize;

                // Perform a random sequence of draw/discard operations
                int ops = rng.Next(5, 40);
                for (int op = 0; op < ops; op++)
                {
                    int action = rng.Next(0, 3);

                    if (action == 0 || action == 1) // Draw
                    {
                        var card = _deckManager.Draw();
                        if (card != null)
                            hand.Add(card);
                    }
                    else // Discard from hand
                    {
                        if (hand.Count > 0)
                        {
                            int idx = rng.Next(0, hand.Count);
                            _deckManager.Discard(hand[idx]);
                            hand.RemoveAt(idx);
                        }
                    }

                    int total = _deckManager.DeckCount + hand.Count + _deckManager.DiscardCount;
                    Assert.AreEqual(totalExpected, total,
                        $"[Iter {i}, Op {op}] Card count mismatch: draw={_deckManager.DeckCount}, " +
                        $"hand={hand.Count}, discard={_deckManager.DiscardCount}, expected={totalExpected}");
                }

                // Clean up ScriptableObjects
                foreach (var c in deck)
                    UnityEngine.Object.DestroyImmediate(c);
            }
        }

        [Test]
        public void Property1_CardCountConservation_AfterFullCycle()
        {
            var rng = new System.Random(77);

            for (int i = 0; i < Iterations; i++)
            {
                int deckSize = rng.Next(1, 20);
                var deck = CreateDeck(deckSize);
                _deckManager.Initialize(deck);

                int totalExpected = deckSize;

                // Draw all cards
                var hand = new List<CardData>();
                for (int d = 0; d < deckSize + 5; d++)
                {
                    var card = _deckManager.Draw();
                    if (card != null) hand.Add(card);
                }

                Assert.AreEqual(totalExpected, hand.Count + _deckManager.DeckCount + _deckManager.DiscardCount,
                    $"[Iter {i}] After drawing all: conservation violated");

                // Discard all
                foreach (var card in hand)
                    _deckManager.Discard(card);
                hand.Clear();

                Assert.AreEqual(totalExpected, _deckManager.DeckCount + _deckManager.DiscardCount,
                    $"[Iter {i}] After discarding all: conservation violated");

                // Draw again (triggers reshuffle)
                for (int d = 0; d < deckSize; d++)
                {
                    var card = _deckManager.Draw();
                    if (card != null) hand.Add(card);
                }

                Assert.AreEqual(totalExpected, hand.Count + _deckManager.DeckCount + _deckManager.DiscardCount,
                    $"[Iter {i}] After reshuffle cycle: conservation violated");

                foreach (var c in deck)
                    UnityEngine.Object.DestroyImmediate(c);
            }
        }

        [Test]
        public void Property1_CardCountConservation_WithAddCard()
        {
            var rng = new System.Random(101);

            for (int i = 0; i < Iterations; i++)
            {
                int deckSize = rng.Next(1, 15);
                var deck = CreateDeck(deckSize);
                _deckManager.Initialize(deck);

                int totalExpected = deckSize;

                // Draw some cards
                var hand = new List<CardData>();
                int drawCount = rng.Next(0, deckSize + 1);
                for (int d = 0; d < drawCount; d++)
                {
                    var card = _deckManager.Draw();
                    if (card != null) hand.Add(card);
                }

                // Add new cards mid-run
                int addCount = rng.Next(1, 5);
                for (int a = 0; a < addCount; a++)
                {
                    var newCard = CreateCard($"Added_{i}_{a}");
                    _deckManager.AddCard(newCard);
                    totalExpected++;
                }

                int total = _deckManager.DeckCount + hand.Count + _deckManager.DiscardCount;
                Assert.AreEqual(totalExpected, total,
                    $"[Iter {i}] After AddCard: draw={_deckManager.DeckCount}, " +
                    $"hand={hand.Count}, discard={_deckManager.DiscardCount}, expected={totalExpected}");

                foreach (var c in deck)
                    UnityEngine.Object.DestroyImmediate(c);
            }
        }

        #endregion

        #region Property 2: Deck Cycle Round Trip

        /// <summary>
        /// Property 2: For any non-empty Discard_Pile and empty Draw_Pile, shuffling the discard
        /// into the draw pile shall produce a Draw_Pile containing exactly the same card set
        /// (by identity) as the original Discard_Pile, and the Discard_Pile shall be empty afterward.
        /// Validates: Requirements 7.1, 7.3
        /// </summary>
        [Test]
        public void Property2_ShuffleDiscardIntoDeck_ProducesSameCardSet()
        {
            var rng = new System.Random(88);

            for (int i = 0; i < Iterations; i++)
            {
                int deckSize = rng.Next(1, 25);
                var deck = CreateDeck(deckSize);
                _deckManager.Initialize(deck);

                // Draw all cards to empty the draw pile
                var hand = new List<CardData>();
                for (int d = 0; d < deckSize + 5; d++)
                {
                    var card = _deckManager.Draw();
                    if (card != null) hand.Add(card);
                }

                // Discard all to fill the discard pile
                foreach (var card in hand)
                    _deckManager.Discard(card);
                hand.Clear();

                Assert.AreEqual(0, _deckManager.DeckCount,
                    $"[Iter {i}] Draw pile should be empty before reshuffle");
                Assert.AreEqual(deckSize, _deckManager.DiscardCount,
                    $"[Iter {i}] Discard pile should have all cards before reshuffle");

                // Capture discard pile contents by reference before shuffle
                var discardBefore = new HashSet<CardData>(_deckManager.DiscardPile);

                _deckManager.ShuffleDiscardIntoDeck();

                Assert.AreEqual(0, _deckManager.DiscardCount,
                    $"[Iter {i}] Discard pile should be empty after reshuffle");
                Assert.AreEqual(deckSize, _deckManager.DeckCount,
                    $"[Iter {i}] Draw pile should have all cards after reshuffle");

                // Verify same card set by identity
                var drawAfter = new HashSet<CardData>(_deckManager.DrawPile);
                Assert.IsTrue(discardBefore.SetEquals(drawAfter),
                    $"[Iter {i}] Draw pile after reshuffle must contain exactly the same cards as discard pile before");

                foreach (var c in deck)
                    UnityEngine.Object.DestroyImmediate(c);
            }
        }

        [Test]
        public void Property2_ShuffleDiscardIntoDeck_DrawTriggeredReshuffle()
        {
            var rng = new System.Random(99);

            for (int i = 0; i < Iterations; i++)
            {
                int deckSize = rng.Next(2, 15);
                var deck = CreateDeck(deckSize);
                _deckManager.Initialize(deck);

                // Draw all and discard all
                var hand = new List<CardData>();
                for (int d = 0; d < deckSize; d++)
                {
                    var card = _deckManager.Draw();
                    if (card != null) hand.Add(card);
                }
                foreach (var card in hand)
                    _deckManager.Discard(card);
                hand.Clear();

                var discardSet = new HashSet<CardData>(_deckManager.DiscardPile);

                // Next draw triggers automatic reshuffle
                var drawn = _deckManager.Draw();
                Assert.IsNotNull(drawn,
                    $"[Iter {i}] Draw after discard-only state should succeed via reshuffle");
                Assert.AreEqual(0, _deckManager.DiscardCount,
                    $"[Iter {i}] Discard should be empty after auto-reshuffle");
                Assert.IsTrue(discardSet.Contains(drawn),
                    $"[Iter {i}] Drawn card must come from the original discard set");

                // Remaining draw pile + drawn card = original discard set
                var remaining = new HashSet<CardData>(_deckManager.DrawPile) { drawn };
                Assert.IsTrue(discardSet.SetEquals(remaining),
                    $"[Iter {i}] All cards accounted for after auto-reshuffle draw");

                foreach (var c in deck)
                    UnityEngine.Object.DestroyImmediate(c);
            }
        }

        [Test]
        public void Property2_EmptyDiscardAndDraw_DrawReturnsNull()
        {
            _deckManager.Initialize(new List<CardData>());

            var result = _deckManager.Draw();
            Assert.IsNull(result, "Draw from empty deck and empty discard should return null");
        }

        #endregion

        #region Property 3: Draw Phase Hand Size

        /// <summary>
        /// Property 3: After the Draw_Phase completes, the hand size shall equal
        /// min(baseHandSize, D + S) — drawing up to the base hand size, reshuffling
        /// discard if needed, and stopping only when both piles are exhausted.
        /// Validates: Requirements 1.1, 7.1, 7.2
        /// </summary>
        [Test]
        public void Property3_DrawPhaseHandSize_EqualsMinOfHandSizeAndAvailable()
        {
            var rng = new System.Random(200);

            for (int i = 0; i < Iterations; i++)
            {
                int deckSize = rng.Next(0, 20);
                int handSize = rng.Next(1, 10);
                var deck = CreateDeck(deckSize);
                _deckManager.Initialize(deck);

                // Simulate a draw phase: draw up to handSize cards
                var hand = new List<CardData>();
                int availableBefore = _deckManager.DeckCount + _deckManager.DiscardCount;

                for (int d = 0; d < handSize; d++)
                {
                    var card = _deckManager.Draw();
                    if (card != null)
                        hand.Add(card);
                    else
                        break; // Both piles exhausted
                }

                int expectedHandSize = Math.Min(handSize, availableBefore);
                Assert.AreEqual(expectedHandSize, hand.Count,
                    $"[Iter {i}] Hand size after draw phase: deckSize={deckSize}, " +
                    $"handSize={handSize}, expected={expectedHandSize}, got={hand.Count}");

                foreach (var c in deck)
                    UnityEngine.Object.DestroyImmediate(c);
            }
        }

        [Test]
        public void Property3_DrawPhaseHandSize_WithPartialDiscardReshuffle()
        {
            var rng = new System.Random(300);

            for (int i = 0; i < Iterations; i++)
            {
                int deckSize = rng.Next(2, 15);
                var deck = CreateDeck(deckSize);
                _deckManager.Initialize(deck);

                // Draw some cards and discard them to create a split state
                int firstDraw = rng.Next(1, deckSize + 1);
                var firstHand = new List<CardData>();
                for (int d = 0; d < firstDraw; d++)
                {
                    var card = _deckManager.Draw();
                    if (card != null) firstHand.Add(card);
                }

                // Discard a random subset
                int discardCount = rng.Next(0, firstHand.Count + 1);
                for (int d = 0; d < discardCount; d++)
                {
                    _deckManager.Discard(firstHand[d]);
                }
                // Cards still in hand (not discarded)
                var stillInHand = firstHand.Skip(discardCount).ToList();

                // Now simulate a new draw phase
                int drawPileSize = _deckManager.DeckCount;
                int discardPileSize = _deckManager.DiscardCount;
                int available = drawPileSize + discardPileSize;

                var newHand = new List<CardData>();
                for (int d = 0; d < DefaultHandSize; d++)
                {
                    var card = _deckManager.Draw();
                    if (card != null)
                        newHand.Add(card);
                    else
                        break;
                }

                int expectedNewHand = Math.Min(DefaultHandSize, available);
                Assert.AreEqual(expectedNewHand, newHand.Count,
                    $"[Iter {i}] Draw phase with split piles: draw={drawPileSize}, " +
                    $"discard={discardPileSize}, expected hand={expectedNewHand}");

                // Conservation still holds
                int totalCards = _deckManager.DeckCount + newHand.Count + stillInHand.Count + _deckManager.DiscardCount;
                Assert.AreEqual(deckSize, totalCards,
                    $"[Iter {i}] Conservation after partial reshuffle draw");

                foreach (var c in deck)
                    UnityEngine.Object.DestroyImmediate(c);
            }
        }

        [Test]
        public void Property3_DrawPhaseHandSize_EmptyDeck_DrawsNothing()
        {
            _deckManager.Initialize(new List<CardData>());

            var hand = new List<CardData>();
            for (int d = 0; d < DefaultHandSize; d++)
            {
                var card = _deckManager.Draw();
                if (card != null)
                    hand.Add(card);
                else
                    break;
            }

            Assert.AreEqual(0, hand.Count, "Empty deck should draw 0 cards");
        }

        [Test]
        public void Property3_DrawPhaseHandSize_FewerCardsThanHandSize()
        {
            var rng = new System.Random(400);

            for (int i = 0; i < 100; i++)
            {
                int handSize = rng.Next(3, 10);
                int deckSize = rng.Next(1, handSize); // Fewer cards than hand size
                var deck = CreateDeck(deckSize);
                _deckManager.Initialize(deck);

                var hand = new List<CardData>();
                for (int d = 0; d < handSize; d++)
                {
                    var card = _deckManager.Draw();
                    if (card != null)
                        hand.Add(card);
                    else
                        break;
                }

                Assert.AreEqual(deckSize, hand.Count,
                    $"[Iter {i}] When deck ({deckSize}) < handSize ({handSize}), " +
                    $"should draw all available cards");

                foreach (var c in deck)
                    UnityEngine.Object.DestroyImmediate(c);
            }
        }

        #endregion
    }
}
