using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CardBattle;

namespace CardBattle.Tests
{
    /// <summary>
    /// Property-based tests for Bathroom Shop purchases and toilet card removal.
    /// Validates Properties 27, 28, 37, 38, 39.
    /// </summary>
    [TestFixture]
    public class ShopPropertyTests
    {
        private const int Iterations = 200;

        private GameObject _saveGo;
        private SaveManager _saveManager;
        private GameObject _shopGo;
        private BathroomShop _shop;
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

            _shopGo = new GameObject("TestShop");
            _shop = _shopGo.AddComponent<BathroomShop>();
            var so = new UnityEditor.SerializedObject(_shop);
            so.FindProperty("gameConfig").objectReferenceValue = _config;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            if (_shopGo != null) Object.DestroyImmediate(_shopGo);
            if (_saveGo != null) Object.DestroyImmediate(_saveGo);
            if (_config != null) Object.DestroyImmediate(_config);
        }

        #region Property 27: Shop Purchase Deducts Currency and Adds Item

        /// <summary>
        /// Property 27: For any player with h Hours and item costing c where c &lt;= h,
        /// after purchase Hours = h - c and item in inventory.
        /// If c &gt; h, purchase rejected, Hours and inventory unchanged.
        /// **Validates: Requirements 22.3, 22.4**
        /// </summary>
        // Feature: card-battle-system, Property 27: Shop Purchase Deducts Currency and Adds Item
        [Test]
        public void Property27_CardPurchase_DeductsCurrency_AddsItem()
        {
            var rng = new System.Random(42);
            var rarities = new[] { CardRarity.Common, CardRarity.Rare, CardRarity.Legendary, CardRarity.Unknown };

            for (int i = 0; i < Iterations; i++)
            {
                // Fresh shop each iteration
                if (_shopGo != null) Object.DestroyImmediate(_shopGo);
                _shopGo = new GameObject("TestShop");
                _shop = _shopGo.AddComponent<BathroomShop>();
                var so = new UnityEditor.SerializedObject(_shop);
                so.FindProperty("gameConfig").objectReferenceValue = _config;
                so.ApplyModifiedPropertiesWithoutUndo();

                int hours = rng.Next(0, 501);
                _saveManager.CurrentRun.hours = hours;
                _saveManager.CurrentRun.deckCardIds = new List<string> { "starter" };
                _saveManager.CurrentRun.toolIds = new List<string>();
                _saveManager.CurrentRun.currentFloor = 1;

                _shop.Initialize();

                if (_shop.CardEntries.Count == 0) continue;

                int idx = rng.Next(0, _shop.CardEntries.Count);
                var entry = _shop.CardEntries[idx];
                int price = entry.price;
                int hoursBefore = _saveManager.CurrentRun.hours;
                int deckBefore = _saveManager.CurrentRun.deckCardIds.Count;

                bool result = _shop.PurchaseCard(idx);

                if (price <= hoursBefore)
                {
                    Assert.IsTrue(result,
                        $"[Iter {i}] Purchase should succeed: hours={hoursBefore}, price={price}");
                    Assert.AreEqual(hoursBefore - price, _saveManager.CurrentRun.hours,
                        $"[Iter {i}] Hours should be {hoursBefore - price} after purchase");
                    Assert.AreEqual(deckBefore + 1, _saveManager.CurrentRun.deckCardIds.Count,
                        $"[Iter {i}] Deck should grow by 1");
                    Assert.IsTrue(_saveManager.CurrentRun.deckCardIds.Contains(entry.cardId),
                        $"[Iter {i}] Deck should contain purchased card");
                }
                else
                {
                    Assert.IsFalse(result,
                        $"[Iter {i}] Purchase should be rejected: hours={hoursBefore}, price={price}");
                    Assert.AreEqual(hoursBefore, _saveManager.CurrentRun.hours,
                        $"[Iter {i}] Hours should be unchanged after rejection");
                    Assert.AreEqual(deckBefore, _saveManager.CurrentRun.deckCardIds.Count,
                        $"[Iter {i}] Deck should be unchanged after rejection");
                }
            }
        }

        /// <summary>
        /// Property 27 (tools): Same property for tool purchases.
        /// **Validates: Requirements 22.3, 22.4**
        /// </summary>
        // Feature: card-battle-system, Property 27: Shop Purchase Deducts Currency and Adds Item (Tools)
        [Test]
        public void Property27_ToolPurchase_DeductsCurrency_AddsItem()
        {
            var rng = new System.Random(99);

            for (int i = 0; i < Iterations; i++)
            {
                if (_shopGo != null) Object.DestroyImmediate(_shopGo);
                _shopGo = new GameObject("TestShop");
                _shop = _shopGo.AddComponent<BathroomShop>();
                var so = new UnityEditor.SerializedObject(_shop);
                so.FindProperty("gameConfig").objectReferenceValue = _config;
                so.ApplyModifiedPropertiesWithoutUndo();

                int hours = rng.Next(0, 501);
                _saveManager.CurrentRun.hours = hours;
                _saveManager.CurrentRun.deckCardIds = new List<string> { "starter" };
                _saveManager.CurrentRun.toolIds = new List<string>();
                // Boss floor to guarantee at least 1 tool
                _saveManager.CurrentRun.currentFloor = 3;

                _shop.Initialize();

                if (_shop.ToolEntries.Count == 0) continue;

                int idx = rng.Next(0, _shop.ToolEntries.Count);
                var entry = _shop.ToolEntries[idx];
                int price = entry.price;
                int hoursBefore = _saveManager.CurrentRun.hours;
                int toolsBefore = _saveManager.CurrentRun.toolIds.Count;

                bool result = _shop.PurchaseTool(idx);

                if (price <= hoursBefore)
                {
                    Assert.IsTrue(result,
                        $"[Iter {i}] Tool purchase should succeed: hours={hoursBefore}, price={price}");
                    Assert.AreEqual(hoursBefore - price, _saveManager.CurrentRun.hours,
                        $"[Iter {i}] Hours should be {hoursBefore - price} after tool purchase");
                    Assert.AreEqual(toolsBefore + 1, _saveManager.CurrentRun.toolIds.Count,
                        $"[Iter {i}] Tools should grow by 1");
                }
                else
                {
                    Assert.IsFalse(result,
                        $"[Iter {i}] Tool purchase should be rejected: hours={hoursBefore}, price={price}");
                    Assert.AreEqual(hoursBefore, _saveManager.CurrentRun.hours,
                        $"[Iter {i}] Hours should be unchanged after rejection");
                    Assert.AreEqual(toolsBefore, _saveManager.CurrentRun.toolIds.Count,
                        $"[Iter {i}] Tools should be unchanged after rejection");
                }
            }
        }

        #endregion

        #region Property 28: Card Removal via Toilet

        /// <summary>
        /// Property 28: For any deck containing card X and removal costing c Hours
        /// where player has sufficient Hours: after removal, deck no longer contains X,
        /// deck size -1, Hours -c.
        /// **Validates: Requirements 22.5, 22.6**
        /// </summary>
        // Feature: card-battle-system, Property 28: Card Removal via Toilet
        [Test]
        public void Property28_CardRemoval_RemovesCard_DeductsCost()
        {
            var rng = new System.Random(55);

            for (int i = 0; i < Iterations; i++)
            {
                // Fresh shop each iteration to reset _removedCardThisVisit
                if (_shopGo != null) Object.DestroyImmediate(_shopGo);
                _shopGo = new GameObject("TestShop");
                _shop = _shopGo.AddComponent<BathroomShop>();
                var so = new UnityEditor.SerializedObject(_shop);
                so.FindProperty("gameConfig").objectReferenceValue = _config;
                so.ApplyModifiedPropertiesWithoutUndo();

                int previousRemovals = rng.Next(0, 11);
                int removalCost = 25 + (previousRemovals * 10);

                // Ensure enough hours for removal
                int hours = removalCost + rng.Next(0, 200);
                int deckSize = rng.Next(2, 31); // at least 2 so removal is allowed

                var deck = new List<string>();
                for (int d = 0; d < deckSize; d++)
                    deck.Add($"card_{d}");

                _saveManager.CurrentRun.hours = hours;
                _saveManager.CurrentRun.deckCardIds = deck;
                _saveManager.CurrentRun.cardRemovalsThisRun = previousRemovals;

                // Pick a random card to remove
                int cardIdx = rng.Next(0, deckSize);
                string cardToRemove = deck[cardIdx];

                int hoursBefore = _saveManager.CurrentRun.hours;
                int deckBefore = _saveManager.CurrentRun.deckCardIds.Count;

                bool result = _shop.RemoveCard(cardToRemove);

                Assert.IsTrue(result,
                    $"[Iter {i}] Removal should succeed: hours={hoursBefore}, cost={removalCost}, deckSize={deckSize}");
                Assert.IsFalse(_saveManager.CurrentRun.deckCardIds.Contains(cardToRemove),
                    $"[Iter {i}] Deck should no longer contain removed card '{cardToRemove}'");
                Assert.AreEqual(deckBefore - 1, _saveManager.CurrentRun.deckCardIds.Count,
                    $"[Iter {i}] Deck size should decrease by 1");
                Assert.AreEqual(hoursBefore - removalCost, _saveManager.CurrentRun.hours,
                    $"[Iter {i}] Hours should decrease by removal cost {removalCost}");
            }
        }

        #endregion

        #region Property 37: Minimum Deck Size Enforced

        /// <summary>
        /// Property 37: Removal rejected if deck size &lt;= 1.
        /// **Validates: Requirements 22.8**
        /// </summary>
        // Feature: card-battle-system, Property 37: Minimum Deck Size Enforced
        [Test]
        public void Property37_MinimumDeckSize_RemovalRejected()
        {
            var rng = new System.Random(77);

            for (int i = 0; i < Iterations; i++)
            {
                if (_shopGo != null) Object.DestroyImmediate(_shopGo);
                _shopGo = new GameObject("TestShop");
                _shop = _shopGo.AddComponent<BathroomShop>();
                var so = new UnityEditor.SerializedObject(_shop);
                so.FindProperty("gameConfig").objectReferenceValue = _config;
                so.ApplyModifiedPropertiesWithoutUndo();

                // Deck size of exactly 1
                int hours = rng.Next(100, 501); // plenty of hours
                _saveManager.CurrentRun.hours = hours;
                _saveManager.CurrentRun.deckCardIds = new List<string> { $"onlyCard_{i}" };
                _saveManager.CurrentRun.cardRemovalsThisRun = rng.Next(0, 11);

                int hoursBefore = _saveManager.CurrentRun.hours;
                int deckBefore = _saveManager.CurrentRun.deckCardIds.Count;

                bool result = _shop.RemoveCard($"onlyCard_{i}");

                Assert.IsFalse(result,
                    $"[Iter {i}] Removal should be rejected when deck size is 1");
                Assert.AreEqual(1, _saveManager.CurrentRun.deckCardIds.Count,
                    $"[Iter {i}] Deck should remain at size 1");
                Assert.AreEqual(hoursBefore, _saveManager.CurrentRun.hours,
                    $"[Iter {i}] Hours should be unchanged");
            }
        }

        #endregion

        #region Property 38: One Toilet Flush Per Bathroom Visit

        /// <summary>
        /// Property 38: Second removal rejected in same visit.
        /// **Validates: Requirements 22.7**
        /// </summary>
        // Feature: card-battle-system, Property 38: One Toilet Flush Per Bathroom Visit
        [Test]
        public void Property38_OneFlushPerVisit_SecondRemovalRejected()
        {
            var rng = new System.Random(88);

            for (int i = 0; i < Iterations; i++)
            {
                if (_shopGo != null) Object.DestroyImmediate(_shopGo);
                _shopGo = new GameObject("TestShop");
                _shop = _shopGo.AddComponent<BathroomShop>();
                var so = new UnityEditor.SerializedObject(_shop);
                so.FindProperty("gameConfig").objectReferenceValue = _config;
                so.ApplyModifiedPropertiesWithoutUndo();

                int deckSize = rng.Next(3, 20); // at least 3 so we can attempt 2 removals
                var deck = new List<string>();
                for (int d = 0; d < deckSize; d++)
                    deck.Add($"card_{i}_{d}");

                _saveManager.CurrentRun.hours = 500;
                _saveManager.CurrentRun.deckCardIds = deck;
                _saveManager.CurrentRun.cardRemovalsThisRun = rng.Next(0, 11);

                // First removal should succeed
                string firstCard = deck[0];
                bool firstResult = _shop.RemoveCard(firstCard);
                Assert.IsTrue(firstResult,
                    $"[Iter {i}] First removal should succeed");

                // Second removal should be rejected
                string secondCard = _saveManager.CurrentRun.deckCardIds[0];
                int hoursAfterFirst = _saveManager.CurrentRun.hours;
                int deckAfterFirst = _saveManager.CurrentRun.deckCardIds.Count;

                bool secondResult = _shop.RemoveCard(secondCard);

                Assert.IsFalse(secondResult,
                    $"[Iter {i}] Second removal should be rejected in same visit");
                Assert.AreEqual(hoursAfterFirst, _saveManager.CurrentRun.hours,
                    $"[Iter {i}] Hours should be unchanged after rejected second removal");
                Assert.AreEqual(deckAfterFirst, _saveManager.CurrentRun.deckCardIds.Count,
                    $"[Iter {i}] Deck should be unchanged after rejected second removal");
            }
        }

        #endregion

        #region Property 39: Escalating Card Removal Cost

        /// <summary>
        /// Property 39: Cost = 25 + (r * 10) where r = previous removals this run.
        /// **Validates: Requirements 43.5**
        /// </summary>
        // Feature: card-battle-system, Property 39: Escalating Card Removal Cost
        [Test]
        public void Property39_EscalatingRemovalCost()
        {
            var rng = new System.Random(39);

            for (int i = 0; i < Iterations; i++)
            {
                if (_shopGo != null) Object.DestroyImmediate(_shopGo);
                _shopGo = new GameObject("TestShop");
                _shop = _shopGo.AddComponent<BathroomShop>();
                var so = new UnityEditor.SerializedObject(_shop);
                so.FindProperty("gameConfig").objectReferenceValue = _config;
                so.ApplyModifiedPropertiesWithoutUndo();

                int previousRemovals = rng.Next(0, 11);
                _saveManager.CurrentRun.cardRemovalsThisRun = previousRemovals;

                int expectedCost = 25 + (previousRemovals * 10);
                int actualCost = _shop.GetRemovalCost();

                Assert.AreEqual(expectedCost, actualCost,
                    $"[Iter {i}] Removal cost with {previousRemovals} previous removals " +
                    $"should be {expectedCost} but got {actualCost}");
            }
        }

        /// <summary>
        /// Property 39 (monotonicity): Cost increases with each removal performed.
        /// **Validates: Requirements 43.5**
        /// </summary>
        // Feature: card-battle-system, Property 39: Escalating Card Removal Cost (Monotonicity)
        [Test]
        public void Property39_CostIncreasesMonotonically()
        {
            var rng = new System.Random(139);

            for (int i = 0; i < Iterations; i++)
            {
                if (_shopGo != null) Object.DestroyImmediate(_shopGo);
                _shopGo = new GameObject("TestShop");
                _shop = _shopGo.AddComponent<BathroomShop>();
                var so = new UnityEditor.SerializedObject(_shop);
                so.FindProperty("gameConfig").objectReferenceValue = _config;
                so.ApplyModifiedPropertiesWithoutUndo();

                int r1 = rng.Next(0, 10);
                int r2 = r1 + rng.Next(1, 5); // r2 > r1

                _saveManager.CurrentRun.cardRemovalsThisRun = r1;
                int cost1 = _shop.GetRemovalCost();

                _saveManager.CurrentRun.cardRemovalsThisRun = r2;
                int cost2 = _shop.GetRemovalCost();

                Assert.Greater(cost2, cost1,
                    $"[Iter {i}] Cost with {r2} removals ({cost2}) should be greater " +
                    $"than cost with {r1} removals ({cost1})");
            }
        }

        #endregion

        #region Property 29: Trade Conserves Inventory

        private GameObject _tradeGo;
        private BreakRoomTrade _trade;

        private void SetUpTrade()
        {
            _tradeGo = new GameObject("TestTrade");
            _trade = _tradeGo.AddComponent<BreakRoomTrade>();
            var so = new UnityEditor.SerializedObject(_trade);
            so.FindProperty("gameConfig").objectReferenceValue = _config;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private void TearDownTrade()
        {
            if (_tradeGo != null) Object.DestroyImmediate(_tradeGo);
        }

        private void SetTradeOffer(BreakRoomTrade.TradeOffer offer)
        {
            var field = typeof(BreakRoomTrade).GetField("_currentOffer",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(_trade, offer);
            var initField = typeof(BreakRoomTrade).GetField("_initialized",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            initField.SetValue(_trade, true);
        }

        /// <summary>
        /// Property 29 (Card-for-Card Accept): After accepting a card trade,
        /// deck contains offered card, does not contain requested card, deck size unchanged.
        /// **Validates: Requirements 23.5, 23.6**
        /// </summary>
        // Feature: card-battle-system, Property 29: Trade Conserves Inventory
        [Test]
        public void Property29_CardForCard_Accept_ConservesInventory()
        {
            var rng = new System.Random(29);

            for (int i = 0; i < Iterations; i++)
            {
                SetUpTrade();
                try
                {
                    int deckSize = rng.Next(2, 21);
                    var deck = new List<string>();
                    for (int d = 0; d < deckSize; d++)
                        deck.Add($"card_{i}_{d}");

                    _saveManager.CurrentRun.deckCardIds = deck;
                    _saveManager.CurrentRun.toolIds = new List<string>();

                    int requestedIdx = rng.Next(0, deckSize);
                    string requestedId = deck[requestedIdx];
                    string offeredId = $"offered_card_{i}";

                    SetTradeOffer(new BreakRoomTrade.TradeOffer
                    {
                        tradeType = BreakRoomTrade.TradeType.CardForCard,
                        requestedItemId = requestedId,
                        requestedRarity = CardRarity.Common,
                        offeredItemId = offeredId,
                        offeredRarity = CardRarity.Common,
                        accepted = false,
                        declined = false
                    });

                    int sizeBefore = _saveManager.CurrentRun.deckCardIds.Count;

                    bool result = _trade.AcceptTrade();

                    Assert.IsTrue(result,
                        $"[Iter {i}] Card trade accept should succeed");
                    Assert.IsTrue(_saveManager.CurrentRun.deckCardIds.Contains(offeredId),
                        $"[Iter {i}] Deck should contain offered card '{offeredId}'");
                    Assert.IsFalse(_saveManager.CurrentRun.deckCardIds.Contains(requestedId),
                        $"[Iter {i}] Deck should not contain requested card '{requestedId}'");
                    Assert.AreEqual(sizeBefore, _saveManager.CurrentRun.deckCardIds.Count,
                        $"[Iter {i}] Deck size should be unchanged after card trade");
                }
                finally
                {
                    TearDownTrade();
                }
            }
        }

        /// <summary>
        /// Property 29 (Tool-for-Tool Accept): After accepting a tool trade,
        /// tools contain offered tool, do not contain requested tool, tools count unchanged.
        /// **Validates: Requirements 23.5, 23.6**
        /// </summary>
        // Feature: card-battle-system, Property 29: Trade Conserves Inventory
        [Test]
        public void Property29_ToolForTool_Accept_ConservesInventory()
        {
            var rng = new System.Random(129);

            for (int i = 0; i < Iterations; i++)
            {
                SetUpTrade();
                try
                {
                    int toolCount = rng.Next(1, 11);
                    var tools = new List<string>();
                    for (int t = 0; t < toolCount; t++)
                        tools.Add($"tool_{i}_{t}");

                    _saveManager.CurrentRun.deckCardIds = new List<string> { "starter" };
                    _saveManager.CurrentRun.toolIds = tools;

                    int requestedIdx = rng.Next(0, toolCount);
                    string requestedId = tools[requestedIdx];
                    string offeredId = $"offered_tool_{i}";

                    SetTradeOffer(new BreakRoomTrade.TradeOffer
                    {
                        tradeType = BreakRoomTrade.TradeType.ToolForTool,
                        requestedItemId = requestedId,
                        requestedRarity = CardRarity.Rare,
                        offeredItemId = offeredId,
                        offeredRarity = CardRarity.Common,
                        accepted = false,
                        declined = false
                    });

                    int countBefore = _saveManager.CurrentRun.toolIds.Count;

                    bool result = _trade.AcceptTrade();

                    Assert.IsTrue(result,
                        $"[Iter {i}] Tool trade accept should succeed");
                    Assert.IsTrue(_saveManager.CurrentRun.toolIds.Contains(offeredId),
                        $"[Iter {i}] Tools should contain offered tool '{offeredId}'");
                    Assert.IsFalse(_saveManager.CurrentRun.toolIds.Contains(requestedId),
                        $"[Iter {i}] Tools should not contain requested tool '{requestedId}'");
                    Assert.AreEqual(countBefore, _saveManager.CurrentRun.toolIds.Count,
                        $"[Iter {i}] Tool count should be unchanged after tool trade");
                }
                finally
                {
                    TearDownTrade();
                }
            }
        }

        /// <summary>
        /// Property 29 (Decline): Declining a trade leaves deck and tools completely unchanged.
        /// **Validates: Requirements 23.5, 23.6**
        /// </summary>
        // Feature: card-battle-system, Property 29: Trade Conserves Inventory
        [Test]
        public void Property29_Decline_InventoryUnchanged()
        {
            var rng = new System.Random(229);

            for (int i = 0; i < Iterations; i++)
            {
                SetUpTrade();
                try
                {
                    int deckSize = rng.Next(1, 16);
                    var deck = new List<string>();
                    for (int d = 0; d < deckSize; d++)
                        deck.Add($"card_{i}_{d}");

                    int toolCount = rng.Next(0, 6);
                    var tools = new List<string>();
                    for (int t = 0; t < toolCount; t++)
                        tools.Add($"tool_{i}_{t}");

                    _saveManager.CurrentRun.deckCardIds = deck;
                    _saveManager.CurrentRun.toolIds = tools;

                    var deckSnapshot = new List<string>(deck);
                    var toolsSnapshot = new List<string>(tools);

                    bool isCardTrade = rng.Next(0, 2) == 0;
                    SetTradeOffer(new BreakRoomTrade.TradeOffer
                    {
                        tradeType = isCardTrade
                            ? BreakRoomTrade.TradeType.CardForCard
                            : BreakRoomTrade.TradeType.ToolForTool,
                        requestedItemId = isCardTrade ? deck[0] : (toolCount > 0 ? tools[0] : "none"),
                        requestedRarity = CardRarity.Common,
                        offeredItemId = $"offered_{i}",
                        offeredRarity = CardRarity.Common,
                        accepted = false,
                        declined = false
                    });

                    _trade.DeclineTrade();

                    Assert.AreEqual(deckSnapshot.Count, _saveManager.CurrentRun.deckCardIds.Count,
                        $"[Iter {i}] Deck count should be unchanged after decline");
                    CollectionAssert.AreEqual(deckSnapshot, _saveManager.CurrentRun.deckCardIds,
                        $"[Iter {i}] Deck contents should be identical after decline");
                    Assert.AreEqual(toolsSnapshot.Count, _saveManager.CurrentRun.toolIds.Count,
                        $"[Iter {i}] Tool count should be unchanged after decline");
                    CollectionAssert.AreEqual(toolsSnapshot, _saveManager.CurrentRun.toolIds,
                        $"[Iter {i}] Tool contents should be identical after decline");
                }
                finally
                {
                    TearDownTrade();
                }
            }
        }

        #endregion
    }
}
