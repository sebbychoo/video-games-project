using NUnit.Framework;
using CardBattle;

namespace CardBattle.Tests
{
    [TestFixture]
    public class WorkBoxTests
    {
        // ----------------------------------------------------------------
        // GetSpawnRates
        // ----------------------------------------------------------------

        [Test]
        public void GetSpawnRates_Floor1_Returns100PercentSmall()
        {
            var rates = WorkBox.GetSpawnRates(1);
            Assert.AreEqual(1.0f, rates.smallRate, 0.001f);
            Assert.AreEqual(0f, rates.bigRate, 0.001f);
            Assert.AreEqual(0f, rates.hugeRate, 0.001f);
        }

        [Test]
        public void GetSpawnRates_Floor5_Returns100PercentSmall()
        {
            var rates = WorkBox.GetSpawnRates(5);
            Assert.AreEqual(1.0f, rates.smallRate, 0.001f);
            Assert.AreEqual(0f, rates.bigRate, 0.001f);
            Assert.AreEqual(0f, rates.hugeRate, 0.001f);
        }

        [Test]
        public void GetSpawnRates_Floor6_Returns90Small10Big()
        {
            var rates = WorkBox.GetSpawnRates(6);
            Assert.AreEqual(0.9f, rates.smallRate, 0.001f);
            Assert.AreEqual(0.1f, rates.bigRate, 0.001f);
            Assert.AreEqual(0f, rates.hugeRate, 0.001f);
        }

        [Test]
        public void GetSpawnRates_Floor10_Returns90Small10Big()
        {
            var rates = WorkBox.GetSpawnRates(10);
            Assert.AreEqual(0.9f, rates.smallRate, 0.001f);
            Assert.AreEqual(0.1f, rates.bigRate, 0.001f);
            Assert.AreEqual(0f, rates.hugeRate, 0.001f);
        }

        [Test]
        public void GetSpawnRates_Floor11_Returns70Small25Big5Huge()
        {
            var rates = WorkBox.GetSpawnRates(11);
            Assert.AreEqual(0.7f, rates.smallRate, 0.001f);
            Assert.AreEqual(0.25f, rates.bigRate, 0.001f);
            Assert.AreEqual(0.05f, rates.hugeRate, 0.001f);
        }

        // ----------------------------------------------------------------
        // GetCardCountRange
        // ----------------------------------------------------------------

        [Test]
        public void GetCardCountRange_Small_Returns1To3()
        {
            WorkBox.GetCardCountRange(WorkBoxSize.Small, out int min, out int max);
            Assert.AreEqual(1, min);
            Assert.AreEqual(3, max);
        }

        [Test]
        public void GetCardCountRange_Big_Returns3To5()
        {
            WorkBox.GetCardCountRange(WorkBoxSize.Big, out int min, out int max);
            Assert.AreEqual(3, min);
            Assert.AreEqual(5, max);
        }

        [Test]
        public void GetCardCountRange_Huge_Returns5To7()
        {
            WorkBox.GetCardCountRange(WorkBoxSize.Huge, out int min, out int max);
            Assert.AreEqual(5, min);
            Assert.AreEqual(7, max);
        }

        // ----------------------------------------------------------------
        // GetRarityWeights
        // ----------------------------------------------------------------

        [Test]
        public void GetRarityWeights_Floor1_MatchesTable()
        {
            WorkBox.GetRarityWeights(1, out float c, out float r, out float l, out float u);
            Assert.AreEqual(0.72f, c, 0.001f);
            Assert.AreEqual(0.25f, r, 0.001f);
            Assert.AreEqual(0.03f, l, 0.001f);
            Assert.AreEqual(0f, u, 0.001f);
        }

        [Test]
        public void GetRarityWeights_Floor3_MatchesFirstTier()
        {
            WorkBox.GetRarityWeights(3, out float c, out float r, out float l, out float u);
            Assert.AreEqual(0.72f, c, 0.001f);
            Assert.AreEqual(0.25f, r, 0.001f);
        }

        [Test]
        public void GetRarityWeights_Floor4_MatchesSecondTier()
        {
            WorkBox.GetRarityWeights(4, out float c, out float r, out float l, out float u);
            Assert.AreEqual(0.52f, c, 0.001f);
            Assert.AreEqual(0.38f, r, 0.001f);
            Assert.AreEqual(0.10f, l, 0.001f);
            Assert.AreEqual(0f, u, 0.001f);
        }

        [Test]
        public void GetRarityWeights_Floor7_MatchesThirdTier()
        {
            WorkBox.GetRarityWeights(7, out float c, out float r, out float l, out float u);
            Assert.AreEqual(0.33f, c, 0.001f);
            Assert.AreEqual(0.45f, r, 0.001f);
            Assert.AreEqual(0.21f, l, 0.001f);
            Assert.AreEqual(0.01f, u, 0.001f);
        }

        [Test]
        public void GetRarityWeights_Floor25Plus_MatchesFinalTier()
        {
            WorkBox.GetRarityWeights(30, out float c, out float r, out float l, out float u);
            Assert.AreEqual(0f, c, 0.001f);
            Assert.AreEqual(0.01f, r, 0.001f);
            Assert.AreEqual(0.69f, l, 0.001f);
            Assert.AreEqual(0.30f, u, 0.001f);
        }

        [TestCase(1)]
        [TestCase(5)]
        [TestCase(7)]
        [TestCase(15)]
        [TestCase(25)]
        public void GetRarityWeights_SumToOne(int floor)
        {
            WorkBox.GetRarityWeights(floor, out float c, out float r, out float l, out float u);
            Assert.AreEqual(1.0f, c + r + l + u, 0.01f);
        }

        [TestCase(1)]
        [TestCase(5)]
        [TestCase(11)]
        public void GetSpawnRates_SumToOne(int floor)
        {
            var rates = WorkBox.GetSpawnRates(floor);
            Assert.AreEqual(1.0f, rates.smallRate + rates.bigRate + rates.hugeRate, 0.01f);
        }

        // ----------------------------------------------------------------
        // Rarity Reveal Sequence
        // ----------------------------------------------------------------

        [Test]
        public void GetTrueRarityRevealState_Common_ReturnsFullReveal()
        {
            Assert.AreEqual(WorkBox.RevealState.FullReveal, WorkBox.GetTrueRarityRevealState(CardRarity.Common));
        }

        [Test]
        public void GetTrueRarityRevealState_Rare_ReturnsYellow()
        {
            Assert.AreEqual(WorkBox.RevealState.Yellow, WorkBox.GetTrueRarityRevealState(CardRarity.Rare));
        }

        [Test]
        public void GetTrueRarityRevealState_Legendary_ReturnsRed()
        {
            Assert.AreEqual(WorkBox.RevealState.Red, WorkBox.GetTrueRarityRevealState(CardRarity.Legendary));
        }

        [Test]
        public void GetTrueRarityRevealState_Unknown_ReturnsBlack()
        {
            Assert.AreEqual(WorkBox.RevealState.Black, WorkBox.GetTrueRarityRevealState(CardRarity.Unknown));
        }
    }
}
