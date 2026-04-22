using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CardBattle;

namespace CardBattle.Tests
{
    /// <summary>
    /// Unit tests for Hub Office upgrade purchase logic and HubUpgradeApplier.
    /// Validates: Requirements 28.1–28.13
    /// </summary>
    [TestFixture]
    public class HubUpgradeTests
    {
        private SaveManager _saveManager;

        [SetUp]
        public void SetUp()
        {
            var go = new GameObject("TestSaveManager");
            _saveManager = go.AddComponent<SaveManager>();
            _saveManager.Initialize();
            _saveManager.CurrentMeta.hubUpgradeLevels = new List<StringIntPair>();
            _saveManager.CurrentMeta.badReviews = 100;
        }

        [TearDown]
        public void TearDown()
        {
            if (_saveManager != null)
                Object.DestroyImmediate(_saveManager.gameObject);
        }

        // ── GetUpgradeLevel ────────────────────────────────────────────────

        [Test]
        public void GetUpgradeLevel_ReturnsZero_WhenNoUpgrades()
        {
            int level = HubOffice.GetUpgradeLevel("Computer");
            Assert.AreEqual(0, level);
        }

        [Test]
        public void GetUpgradeLevel_ReturnsCorrectLevel_WhenUpgradeExists()
        {
            _saveManager.CurrentMeta.hubUpgradeLevels.Add(
                new StringIntPair { key = "Computer", value = 3 });

            int level = HubOffice.GetUpgradeLevel("Computer");
            Assert.AreEqual(3, level);
        }

        [Test]
        public void GetUpgradeLevel_ReturnsZero_ForDifferentUpgradeId()
        {
            _saveManager.CurrentMeta.hubUpgradeLevels.Add(
                new StringIntPair { key = "Computer", value = 2 });

            int level = HubOffice.GetUpgradeLevel("CoffeeMachine");
            Assert.AreEqual(0, level);
        }

        // ── HubUpgradeApplier.GetModifierValue ────────────────────────────

        [Test]
        public void GetModifierValue_ReturnsZero_WhenNoUpgrades()
        {
            int value = HubUpgradeApplier.GetModifierValue(ToolModifierType.OvertimeRegen);
            Assert.AreEqual(0, value);
        }

        [Test]
        public void GetModifierValue_ReturnsZero_WhenUpgradeLevelsNull()
        {
            _saveManager.CurrentMeta.hubUpgradeLevels = null;
            int value = HubUpgradeApplier.GetModifierValue(ToolModifierType.MaxHP);
            Assert.AreEqual(0, value);
        }

        // ── HubUpgradeApplier.GetEffectiveBaseHP ──────────────────────────

        [Test]
        public void GetEffectiveBaseHP_ReturnsBaseHP_WhenNoUpgrades()
        {
            var config = ScriptableObject.CreateInstance<GameConfig>();
            config.playerBaseHP = 80;

            int hp = HubUpgradeApplier.GetEffectiveBaseHP(config);
            Assert.AreEqual(80, hp);

            Object.DestroyImmediate(config);
        }

        [Test]
        public void GetEffectiveBaseHP_ReturnsDefault_WhenConfigNull()
        {
            int hp = HubUpgradeApplier.GetEffectiveBaseHP(null);
            Assert.AreEqual(80, hp); // default fallback
        }

        // ── HubUpgradeApplier.GetEffectiveHandSize ───────────────────────

        [Test]
        public void GetEffectiveHandSize_ReturnsBaseHandSize_WhenNoUpgrades()
        {
            var config = ScriptableObject.CreateInstance<GameConfig>();
            config.baseHandSize = 5;

            int handSize = HubUpgradeApplier.GetEffectiveHandSize(config);
            Assert.AreEqual(5, handSize);

            Object.DestroyImmediate(config);
        }

        // ── HubUpgradeApplier.GetHealPerFloor ─────────────────────────────

        [Test]
        public void GetHealPerFloor_ReturnsZero_WhenNoUpgrades()
        {
            int heal = HubUpgradeApplier.GetHealPerFloor();
            Assert.AreEqual(0, heal);
        }

        // ── HubUpgradeApplier.GetTechCardDamageBonus ──────────────────────

        [Test]
        public void GetTechCardDamageBonus_ReturnsZero_WhenNoUpgrades()
        {
            int bonus = HubUpgradeApplier.GetTechCardDamageBonus();
            Assert.AreEqual(0, bonus);
        }

        // ── HubUpgradeApplier.GetHubParryWindowBonus ──────────────────────

        [Test]
        public void GetHubParryWindowBonus_ReturnsZero_WhenNoUpgrades()
        {
            float bonus = HubUpgradeApplier.GetHubParryWindowBonus();
            Assert.AreEqual(0f, bonus);
        }

        // ── CardEffectResolver TechCardDamageBonus ────────────────────────

        [Test]
        public void CardEffectResolver_TechCardDamageBonus_DefaultsToZero()
        {
            var go = new GameObject("TestResolver");
            var resolver = go.AddComponent<CardEffectResolver>();
            resolver.ResetModifiers();

            Assert.AreEqual(0, resolver.TechCardDamageBonus);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void CardEffectResolver_ApplyTechCardDamageBonus_Accumulates()
        {
            var go = new GameObject("TestResolver");
            var resolver = go.AddComponent<CardEffectResolver>();
            resolver.ResetModifiers();

            resolver.ApplyTechCardDamageBonus(2);
            resolver.ApplyTechCardDamageBonus(3);

            Assert.AreEqual(5, resolver.TechCardDamageBonus);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void CardEffectResolver_ResetModifiers_ClearsTechBonus()
        {
            var go = new GameObject("TestResolver");
            var resolver = go.AddComponent<CardEffectResolver>();

            resolver.ApplyTechCardDamageBonus(5);
            resolver.ResetModifiers();

            Assert.AreEqual(0, resolver.TechCardDamageBonus);

            Object.DestroyImmediate(go);
        }
    }
}
