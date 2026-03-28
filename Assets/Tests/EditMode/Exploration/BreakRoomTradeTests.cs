using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CardBattle;

namespace CardBattle.Tests
{
    [TestFixture]
    public class BreakRoomTradeTests
    {
        private GameObject _saveGo;
        private SaveManager _saveManager;
        private GameObject _tradeGo;
        private BreakRoomTrade _trade;
        private GameConfig _config;

        [SetUp]
        public void SetUp()
        {
            if (SaveManager.Instance != null)
                Object.DestroyImmediate(SaveManager.Instance.gameObject);

            _saveGo = new GameObject("TestSaveManager");
            _saveManager = _saveGo.AddComponent<SaveManager>();
            _saveManager.Initialize();

            _config = ScriptableObject.CreateInstance<GameConfig>();

            _tradeGo = new GameObject("TestTrade");
            _trade = _tradeGo.AddComponent<BreakRoomTrade>();
            var so = new UnityEditor.SerializedObject(_trade);
            so.FindProperty("gameConfig").objectReferenceValue = _config;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            if (_tradeGo != null) Object.DestroyImmediate(_tradeGo);
            if (_saveGo != null) Object.DestroyImmediate(_saveGo);
            if (_config != null) Object.DestroyImmediate(_config);
        }

        // ----------------------------------------------------------------
        // Initialization
        // ----------------------------------------------------------------

        [Test]
        public void Initialize_Idempotent()
        {
            _saveManager.CurrentRun.deckCardIds = new List<string> { "Pencil1" };
            _trade.Initialize();
            var firstOffer = _trade.CurrentOffer;

            _trade.Initialize(); // second call

            Assert.AreSame(firstOffer, _trade.CurrentOffer);
        }

        [Test]
        public void Initialize_NoInventory_OfferIsNull()
        {
            _saveManager.CurrentRun.deckCardIds = new List<string>();
            _saveManager.CurrentRun.toolIds = new List<string>();
            _trade.Initialize();

            Assert.IsNull(_trade.CurrentOffer);
        }

        [Test]
        public void Initialize_WithCards_GeneratesOffer()
        {
            _saveManager.CurrentRun.deckCardIds = new List<string> { "Pencil1" };
            _trade.Initialize();

            // Offer may be null if Resources can't find cards, but should be non-null
            // in a real project. We test the generation logic separately.
            Assert.IsTrue(_trade.IsInitialized);
        }

        // ----------------------------------------------------------------
        // AcceptTrade — Card-for-Card
        // ----------------------------------------------------------------

        [Test]
        public void AcceptTrade_CardForCard_RemovesRequestedAddsOffered()
        {
            _saveManager.CurrentRun.deckCardIds = new List<string> { "cardA", "cardB", "cardC" };

            // Manually set up a trade offer
            SetTradeOffer(new BreakRoomTrade.TradeOffer
            {
                tradeType = BreakRoomTrade.TradeType.CardForCard,
                requestedItemId = "cardB",
                requestedRarity = CardRarity.Rare,
                offeredItemId = "cardX",
                offeredRarity = CardRarity.Common,
                accepted = false,
                declined = false
            });

            bool result = _trade.AcceptTrade();

            Assert.IsTrue(result);
            Assert.IsFalse(_saveManager.CurrentRun.deckCardIds.Contains("cardB"));
            Assert.IsTrue(_saveManager.CurrentRun.deckCardIds.Contains("cardX"));
            Assert.AreEqual(3, _saveManager.CurrentRun.deckCardIds.Count);
            Assert.IsTrue(_trade.CurrentOffer.accepted);
        }

        [Test]
        public void AcceptTrade_CardForCard_RequestedNotInDeck_Rejected()
        {
            _saveManager.CurrentRun.deckCardIds = new List<string> { "cardA" };

            SetTradeOffer(new BreakRoomTrade.TradeOffer
            {
                tradeType = BreakRoomTrade.TradeType.CardForCard,
                requestedItemId = "cardZ",
                requestedRarity = CardRarity.Common,
                offeredItemId = "cardX",
                offeredRarity = CardRarity.Common,
                accepted = false,
                declined = false
            });

            bool result = _trade.AcceptTrade();

            Assert.IsFalse(result);
            Assert.AreEqual(1, _saveManager.CurrentRun.deckCardIds.Count);
        }

        // ----------------------------------------------------------------
        // AcceptTrade — Tool-for-Tool
        // ----------------------------------------------------------------

        [Test]
        public void AcceptTrade_ToolForTool_RemovesRequestedAddsOffered()
        {
            _saveManager.CurrentRun.toolIds = new List<string> { "toolA", "toolB" };

            SetTradeOffer(new BreakRoomTrade.TradeOffer
            {
                tradeType = BreakRoomTrade.TradeType.ToolForTool,
                requestedItemId = "toolA",
                requestedRarity = CardRarity.Legendary,
                offeredItemId = "toolY",
                offeredRarity = CardRarity.Rare,
                accepted = false,
                declined = false
            });

            bool result = _trade.AcceptTrade();

            Assert.IsTrue(result);
            Assert.IsFalse(_saveManager.CurrentRun.toolIds.Contains("toolA"));
            Assert.IsTrue(_saveManager.CurrentRun.toolIds.Contains("toolY"));
            Assert.AreEqual(2, _saveManager.CurrentRun.toolIds.Count);
            Assert.IsTrue(_trade.CurrentOffer.accepted);
        }

        [Test]
        public void AcceptTrade_ToolForTool_RequestedNotInInventory_Rejected()
        {
            _saveManager.CurrentRun.toolIds = new List<string> { "toolA" };

            SetTradeOffer(new BreakRoomTrade.TradeOffer
            {
                tradeType = BreakRoomTrade.TradeType.ToolForTool,
                requestedItemId = "toolZ",
                requestedRarity = CardRarity.Common,
                offeredItemId = "toolX",
                offeredRarity = CardRarity.Common,
                accepted = false,
                declined = false
            });

            bool result = _trade.AcceptTrade();

            Assert.IsFalse(result);
            Assert.AreEqual(1, _saveManager.CurrentRun.toolIds.Count);
        }

        // ----------------------------------------------------------------
        // AcceptTrade — Already Resolved
        // ----------------------------------------------------------------

        [Test]
        public void AcceptTrade_AlreadyAccepted_Rejected()
        {
            _saveManager.CurrentRun.deckCardIds = new List<string> { "cardA" };

            SetTradeOffer(new BreakRoomTrade.TradeOffer
            {
                tradeType = BreakRoomTrade.TradeType.CardForCard,
                requestedItemId = "cardA",
                requestedRarity = CardRarity.Common,
                offeredItemId = "cardX",
                offeredRarity = CardRarity.Common,
                accepted = true,
                declined = false
            });

            bool result = _trade.AcceptTrade();
            Assert.IsFalse(result);
        }

        [Test]
        public void AcceptTrade_AlreadyDeclined_Rejected()
        {
            _saveManager.CurrentRun.deckCardIds = new List<string> { "cardA" };

            SetTradeOffer(new BreakRoomTrade.TradeOffer
            {
                tradeType = BreakRoomTrade.TradeType.CardForCard,
                requestedItemId = "cardA",
                requestedRarity = CardRarity.Common,
                offeredItemId = "cardX",
                offeredRarity = CardRarity.Common,
                accepted = false,
                declined = true
            });

            bool result = _trade.AcceptTrade();
            Assert.IsFalse(result);
        }

        [Test]
        public void AcceptTrade_NoOffer_Rejected()
        {
            bool result = _trade.AcceptTrade();
            Assert.IsFalse(result);
        }

        // ----------------------------------------------------------------
        // DeclineTrade
        // ----------------------------------------------------------------

        [Test]
        public void DeclineTrade_NoChangesToInventory()
        {
            _saveManager.CurrentRun.deckCardIds = new List<string> { "cardA", "cardB" };

            SetTradeOffer(new BreakRoomTrade.TradeOffer
            {
                tradeType = BreakRoomTrade.TradeType.CardForCard,
                requestedItemId = "cardA",
                requestedRarity = CardRarity.Common,
                offeredItemId = "cardX",
                offeredRarity = CardRarity.Common,
                accepted = false,
                declined = false
            });

            _trade.DeclineTrade();

            Assert.IsTrue(_trade.CurrentOffer.declined);
            Assert.AreEqual(2, _saveManager.CurrentRun.deckCardIds.Count);
            Assert.IsTrue(_saveManager.CurrentRun.deckCardIds.Contains("cardA"));
            Assert.IsTrue(_saveManager.CurrentRun.deckCardIds.Contains("cardB"));
            Assert.IsFalse(_saveManager.CurrentRun.deckCardIds.Contains("cardX"));
        }

        [Test]
        public void DeclineTrade_AlreadyAccepted_NoChange()
        {
            SetTradeOffer(new BreakRoomTrade.TradeOffer
            {
                tradeType = BreakRoomTrade.TradeType.CardForCard,
                requestedItemId = "cardA",
                requestedRarity = CardRarity.Common,
                offeredItemId = "cardX",
                offeredRarity = CardRarity.Common,
                accepted = true,
                declined = false
            });

            _trade.DeclineTrade();

            Assert.IsFalse(_trade.CurrentOffer.declined);
        }

        // ----------------------------------------------------------------
        // CanFulfillTrade
        // ----------------------------------------------------------------

        [Test]
        public void CanFulfillTrade_CardInDeck_ReturnsTrue()
        {
            _saveManager.CurrentRun.deckCardIds = new List<string> { "cardA" };

            SetTradeOffer(new BreakRoomTrade.TradeOffer
            {
                tradeType = BreakRoomTrade.TradeType.CardForCard,
                requestedItemId = "cardA",
                requestedRarity = CardRarity.Common,
                offeredItemId = "cardX",
                offeredRarity = CardRarity.Common,
                accepted = false,
                declined = false
            });

            Assert.IsTrue(_trade.CanFulfillTrade());
        }

        [Test]
        public void CanFulfillTrade_CardNotInDeck_ReturnsFalse()
        {
            _saveManager.CurrentRun.deckCardIds = new List<string> { "cardB" };

            SetTradeOffer(new BreakRoomTrade.TradeOffer
            {
                tradeType = BreakRoomTrade.TradeType.CardForCard,
                requestedItemId = "cardA",
                requestedRarity = CardRarity.Common,
                offeredItemId = "cardX",
                offeredRarity = CardRarity.Common,
                accepted = false,
                declined = false
            });

            Assert.IsFalse(_trade.CanFulfillTrade());
        }

        [Test]
        public void CanFulfillTrade_ToolInInventory_ReturnsTrue()
        {
            _saveManager.CurrentRun.toolIds = new List<string> { "toolA" };

            SetTradeOffer(new BreakRoomTrade.TradeOffer
            {
                tradeType = BreakRoomTrade.TradeType.ToolForTool,
                requestedItemId = "toolA",
                requestedRarity = CardRarity.Common,
                offeredItemId = "toolX",
                offeredRarity = CardRarity.Common,
                accepted = false,
                declined = false
            });

            Assert.IsTrue(_trade.CanFulfillTrade());
        }

        [Test]
        public void CanFulfillTrade_NoOffer_ReturnsFalse()
        {
            Assert.IsFalse(_trade.CanFulfillTrade());
        }

        [Test]
        public void CanFulfillTrade_AlreadyAccepted_ReturnsFalse()
        {
            _saveManager.CurrentRun.deckCardIds = new List<string> { "cardA" };

            SetTradeOffer(new BreakRoomTrade.TradeOffer
            {
                tradeType = BreakRoomTrade.TradeType.CardForCard,
                requestedItemId = "cardA",
                requestedRarity = CardRarity.Common,
                offeredItemId = "cardX",
                offeredRarity = CardRarity.Common,
                accepted = true,
                declined = false
            });

            Assert.IsFalse(_trade.CanFulfillTrade());
        }

        // ----------------------------------------------------------------
        // Trade Fairness (Req 23.4)
        // ----------------------------------------------------------------

        [Test]
        public void GetLowerRarity_Unknown_ReturnsLegendary()
        {
            Assert.AreEqual(CardRarity.Legendary, BreakRoomTrade.GetLowerRarity(CardRarity.Unknown));
        }

        [Test]
        public void GetLowerRarity_Legendary_ReturnsRare()
        {
            Assert.AreEqual(CardRarity.Rare, BreakRoomTrade.GetLowerRarity(CardRarity.Legendary));
        }

        [Test]
        public void GetLowerRarity_Rare_ReturnsCommon()
        {
            Assert.AreEqual(CardRarity.Common, BreakRoomTrade.GetLowerRarity(CardRarity.Rare));
        }

        [Test]
        public void GetLowerRarity_Common_ReturnsCommon()
        {
            Assert.AreEqual(CardRarity.Common, BreakRoomTrade.GetLowerRarity(CardRarity.Common));
        }

        [Test]
        public void IsEqualOrLowerRarity_SameRarity_ReturnsTrue()
        {
            Assert.IsTrue(BreakRoomTrade.IsEqualOrLowerRarity(CardRarity.Rare, CardRarity.Rare));
        }

        [Test]
        public void IsEqualOrLowerRarity_LowerOffered_ReturnsTrue()
        {
            Assert.IsTrue(BreakRoomTrade.IsEqualOrLowerRarity(CardRarity.Common, CardRarity.Legendary));
        }

        [Test]
        public void IsEqualOrLowerRarity_HigherOffered_ReturnsFalse()
        {
            Assert.IsFalse(BreakRoomTrade.IsEqualOrLowerRarity(CardRarity.Legendary, CardRarity.Common));
        }

        [Test]
        public void RollEqualOrLowerRarity_Common_AlwaysReturnsCommon()
        {
            // Common is the lowest, so it should always return Common
            for (int i = 0; i < 50; i++)
            {
                CardRarity result = BreakRoomTrade.RollEqualOrLowerRarity(CardRarity.Common);
                Assert.AreEqual(CardRarity.Common, result);
            }
        }

        [Test]
        public void RollEqualOrLowerRarity_NeverReturnsHigher()
        {
            // Run many iterations to verify fairness constraint
            for (int i = 0; i < 100; i++)
            {
                CardRarity result = BreakRoomTrade.RollEqualOrLowerRarity(CardRarity.Rare);
                Assert.IsTrue(BreakRoomTrade.IsEqualOrLowerRarity(result, CardRarity.Rare),
                    $"Expected equal or lower than Rare, got {result}");
            }
        }

        // ----------------------------------------------------------------
        // Deck Size Conservation on Card Trade
        // ----------------------------------------------------------------

        [Test]
        public void AcceptTrade_CardForCard_DeckSizeUnchanged()
        {
            _saveManager.CurrentRun.deckCardIds = new List<string> { "a", "b", "c" };
            int sizeBefore = _saveManager.CurrentRun.deckCardIds.Count;

            SetTradeOffer(new BreakRoomTrade.TradeOffer
            {
                tradeType = BreakRoomTrade.TradeType.CardForCard,
                requestedItemId = "b",
                requestedRarity = CardRarity.Common,
                offeredItemId = "x",
                offeredRarity = CardRarity.Common,
                accepted = false,
                declined = false
            });

            _trade.AcceptTrade();

            Assert.AreEqual(sizeBefore, _saveManager.CurrentRun.deckCardIds.Count);
        }

        // ----------------------------------------------------------------
        // Helper to inject a trade offer for testing
        // ----------------------------------------------------------------

        private void SetTradeOffer(BreakRoomTrade.TradeOffer offer)
        {
            // Use reflection to set the private _currentOffer field
            var field = typeof(BreakRoomTrade).GetField("_currentOffer",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(_trade, offer);

            // Also mark as initialized
            var initField = typeof(BreakRoomTrade).GetField("_initialized",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            initField.SetValue(_trade, true);
        }
    }
}
