using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CardBattle;

namespace CardBattle.Tests
{
    [TestFixture]
    public class BathroomShopTests
    {
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
            // Inject GameConfig via serialized field
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

        // ----------------------------------------------------------------
        // Pricing
        // ----------------------------------------------------------------

        [Test]
        public void GetCardPrice_Common_Returns10()
        {
            Assert.AreEqual(10, BathroomShop.GetCardPrice(CardRarity.Common));
        }

        [Test]
        public void GetCardPrice_Rare_Returns25()
        {
            Assert.AreEqual(25, BathroomShop.GetCardPrice(CardRarity.Rare));
        }

        [Test]
        public void GetCardPrice_Legendary_Returns100()
        {
            Assert.AreEqual(100, BathroomShop.GetCardPrice(CardRarity.Legendary));
        }

        [Test]
        public void GetCardPrice_Unknown_Returns150()
        {
            Assert.AreEqual(150, BathroomShop.GetCardPrice(CardRarity.Unknown));
        }

        [Test]
        public void GetToolPrice_Common_Returns30()
        {
            Assert.AreEqual(30, BathroomShop.GetToolPrice(CardRarity.Common));
        }

        [Test]
        public void GetToolPrice_Rare_Returns60()
        {
            Assert.AreEqual(60, BathroomShop.GetToolPrice(CardRarity.Rare));
        }

        [Test]
        public void GetToolPrice_Legendary_Returns200()
        {
            Assert.AreEqual(200, BathroomShop.GetToolPrice(CardRarity.Legendary));
        }

        // ----------------------------------------------------------------
        // Removal Cost
        // ----------------------------------------------------------------

        [Test]
        public void GetRemovalCost_ZeroPreviousRemovals_ReturnsBaseCost()
        {
            _saveManager.CurrentRun.cardRemovalsThisRun = 0;
            Assert.AreEqual(25, _shop.GetRemovalCost());
        }

        [Test]
        public void GetRemovalCost_TwoPreviousRemovals_Returns45()
        {
            _saveManager.CurrentRun.cardRemovalsThisRun = 2;
            Assert.AreEqual(45, _shop.GetRemovalCost());
        }

        [Test]
        public void GetRemovalCost_FivePreviousRemovals_Returns75()
        {
            _saveManager.CurrentRun.cardRemovalsThisRun = 5;
            Assert.AreEqual(75, _shop.GetRemovalCost());
        }

        // ----------------------------------------------------------------
        // Card Purchase
        // ----------------------------------------------------------------

        [Test]
        public void PurchaseCard_SufficientHours_DeductsAndAddsCard()
        {
            _saveManager.CurrentRun.hours = 100;
            _saveManager.CurrentRun.deckCardIds = new List<string> { "card1" };
            _shop.Initialize();

            // Find a card entry to purchase
            if (_shop.CardEntries.Count == 0) return; // skip if no cards generated

            var entry = _shop.CardEntries[0];
            int hoursBefore = _saveManager.CurrentRun.hours;
            int deckBefore = _saveManager.CurrentRun.deckCardIds.Count;

            bool result = _shop.PurchaseCard(0);

            Assert.IsTrue(result);
            Assert.AreEqual(hoursBefore - entry.price, _saveManager.CurrentRun.hours);
            Assert.AreEqual(deckBefore + 1, _saveManager.CurrentRun.deckCardIds.Count);
            Assert.IsTrue(entry.sold);
        }

        [Test]
        public void PurchaseCard_InsufficientHours_Rejected()
        {
            _saveManager.CurrentRun.hours = 0;
            _saveManager.CurrentRun.deckCardIds = new List<string> { "card1" };
            _shop.Initialize();

            if (_shop.CardEntries.Count == 0) return;

            int deckBefore = _saveManager.CurrentRun.deckCardIds.Count;
            bool result = _shop.PurchaseCard(0);

            Assert.IsFalse(result);
            Assert.AreEqual(0, _saveManager.CurrentRun.hours);
            Assert.AreEqual(deckBefore, _saveManager.CurrentRun.deckCardIds.Count);
        }

        [Test]
        public void PurchaseCard_AlreadySold_Rejected()
        {
            _saveManager.CurrentRun.hours = 500;
            _saveManager.CurrentRun.deckCardIds = new List<string> { "card1" };
            _shop.Initialize();

            if (_shop.CardEntries.Count == 0) return;

            _shop.PurchaseCard(0); // buy it
            bool result = _shop.PurchaseCard(0); // try again

            Assert.IsFalse(result);
        }

        [Test]
        public void PurchaseCard_InvalidIndex_Rejected()
        {
            _saveManager.CurrentRun.hours = 500;
            _saveManager.CurrentRun.deckCardIds = new List<string>();
            _shop.Initialize();

            Assert.IsFalse(_shop.PurchaseCard(-1));
            Assert.IsFalse(_shop.PurchaseCard(999));
        }

        // ----------------------------------------------------------------
        // Tool Purchase
        // ----------------------------------------------------------------

        [Test]
        public void PurchaseTool_SufficientHours_DeductsAndAddsTool()
        {
            _saveManager.CurrentRun.hours = 500;
            _saveManager.CurrentRun.toolIds = new List<string>();
            // Force boss floor to guarantee at least 1 tool
            _saveManager.CurrentRun.currentFloor = 3;
            _shop.Initialize();

            if (_shop.ToolEntries.Count == 0) return;

            var entry = _shop.ToolEntries[0];
            int hoursBefore = _saveManager.CurrentRun.hours;

            bool result = _shop.PurchaseTool(0);

            Assert.IsTrue(result);
            Assert.AreEqual(hoursBefore - entry.price, _saveManager.CurrentRun.hours);
            Assert.AreEqual(1, _saveManager.CurrentRun.toolIds.Count);
            Assert.IsTrue(entry.sold);
        }

        [Test]
        public void PurchaseTool_InsufficientHours_Rejected()
        {
            _saveManager.CurrentRun.hours = 0;
            _saveManager.CurrentRun.toolIds = new List<string>();
            _saveManager.CurrentRun.currentFloor = 3;
            _shop.Initialize();

            if (_shop.ToolEntries.Count == 0) return;

            bool result = _shop.PurchaseTool(0);

            Assert.IsFalse(result);
            Assert.AreEqual(0, _saveManager.CurrentRun.toolIds.Count);
        }

        // ----------------------------------------------------------------
        // Toilet Card Removal
        // ----------------------------------------------------------------

        [Test]
        public void RemoveCard_ValidCard_RemovesAndDeductsCost()
        {
            _saveManager.CurrentRun.hours = 100;
            _saveManager.CurrentRun.deckCardIds = new List<string> { "cardA", "cardB", "cardC" };
            _saveManager.CurrentRun.cardRemovalsThisRun = 0;

            bool result = _shop.RemoveCard("cardB");

            Assert.IsTrue(result);
            Assert.AreEqual(75, _saveManager.CurrentRun.hours); // 100 - 25
            Assert.AreEqual(2, _saveManager.CurrentRun.deckCardIds.Count);
            Assert.IsFalse(_saveManager.CurrentRun.deckCardIds.Contains("cardB"));
            Assert.AreEqual(1, _saveManager.CurrentRun.cardRemovalsThisRun);
            Assert.IsTrue(_shop.RemovedCardThisVisit);
        }

        [Test]
        public void RemoveCard_SecondRemoval_Rejected()
        {
            _saveManager.CurrentRun.hours = 200;
            _saveManager.CurrentRun.deckCardIds = new List<string> { "a", "b", "c" };
            _saveManager.CurrentRun.cardRemovalsThisRun = 0;

            _shop.RemoveCard("a");
            bool result = _shop.RemoveCard("b");

            Assert.IsFalse(result);
        }

        [Test]
        public void RemoveCard_DeckAtMinimumSize_Rejected()
        {
            _saveManager.CurrentRun.hours = 100;
            _saveManager.CurrentRun.deckCardIds = new List<string> { "onlyCard" };
            _saveManager.CurrentRun.cardRemovalsThisRun = 0;

            bool result = _shop.RemoveCard("onlyCard");

            Assert.IsFalse(result);
            Assert.AreEqual(1, _saveManager.CurrentRun.deckCardIds.Count);
        }

        [Test]
        public void RemoveCard_InsufficientHours_Rejected()
        {
            _saveManager.CurrentRun.hours = 10; // less than 25 base cost
            _saveManager.CurrentRun.deckCardIds = new List<string> { "a", "b" };
            _saveManager.CurrentRun.cardRemovalsThisRun = 0;

            bool result = _shop.RemoveCard("a");

            Assert.IsFalse(result);
            Assert.AreEqual(2, _saveManager.CurrentRun.deckCardIds.Count);
        }

        [Test]
        public void RemoveCard_CardNotInDeck_Rejected()
        {
            _saveManager.CurrentRun.hours = 100;
            _saveManager.CurrentRun.deckCardIds = new List<string> { "a", "b" };
            _saveManager.CurrentRun.cardRemovalsThisRun = 0;

            bool result = _shop.RemoveCard("nonexistent");

            Assert.IsFalse(result);
        }

        [Test]
        public void RemoveCard_EscalatingCost_CorrectlyApplied()
        {
            _saveManager.CurrentRun.hours = 200;
            _saveManager.CurrentRun.deckCardIds = new List<string> { "a", "b", "c" };
            _saveManager.CurrentRun.cardRemovalsThisRun = 3; // 3 previous removals

            // Cost should be 25 + (3 * 10) = 55
            int expectedCost = 55;
            int hoursBefore = _saveManager.CurrentRun.hours;

            bool result = _shop.RemoveCard("a");

            Assert.IsTrue(result);
            Assert.AreEqual(hoursBefore - expectedCost, _saveManager.CurrentRun.hours);
            Assert.AreEqual(4, _saveManager.CurrentRun.cardRemovalsThisRun);
        }

        // ----------------------------------------------------------------
        // ResetVisit
        // ----------------------------------------------------------------

        [Test]
        public void ResetVisit_AllowsNewRemoval()
        {
            _saveManager.CurrentRun.hours = 200;
            _saveManager.CurrentRun.deckCardIds = new List<string> { "a", "b", "c" };
            _saveManager.CurrentRun.cardRemovalsThisRun = 0;

            _shop.RemoveCard("a");
            Assert.IsTrue(_shop.RemovedCardThisVisit);

            _shop.ResetVisit();
            Assert.IsFalse(_shop.RemovedCardThisVisit);

            bool result = _shop.RemoveCard("b");
            Assert.IsTrue(result);
        }

        // ----------------------------------------------------------------
        // Initialization
        // ----------------------------------------------------------------

        [Test]
        public void Initialize_Idempotent()
        {
            _saveManager.CurrentRun.currentFloor = 1;
            _shop.Initialize();
            int cardCount = _shop.CardEntries.Count;
            int toolCount = _shop.ToolEntries.Count;

            _shop.Initialize(); // second call

            Assert.AreEqual(cardCount, _shop.CardEntries.Count);
            Assert.AreEqual(toolCount, _shop.ToolEntries.Count);
        }

        [Test]
        public void Initialize_CardCountInRange()
        {
            _saveManager.CurrentRun.currentFloor = 1;
            _shop.Initialize();

            Assert.GreaterOrEqual(_shop.CardEntries.Count, 3);
            Assert.LessOrEqual(_shop.CardEntries.Count, 5);
        }

        [Test]
        public void Initialize_BossFloor_GuaranteesAtLeastOneTool()
        {
            _saveManager.CurrentRun.currentFloor = 3; // boss floor
            _shop.Initialize();

            Assert.GreaterOrEqual(_shop.ToolEntries.Count, 1);
        }

        // ----------------------------------------------------------------
        // RollToolRarity (excludes Unknown)
        // ----------------------------------------------------------------

        [Test]
        public void RollToolRarity_NeverReturnsUnknown()
        {
            // Run many iterations on a high floor where Unknown is possible
            for (int i = 0; i < 100; i++)
            {
                CardRarity rarity = BathroomShop.RollToolRarity(25);
                Assert.AreNotEqual(CardRarity.Unknown, rarity);
            }
        }
    }
}
