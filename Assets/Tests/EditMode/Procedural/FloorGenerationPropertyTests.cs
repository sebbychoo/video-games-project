using System;
using NUnit.Framework;
using CardBattle;

namespace CardBattle.Tests
{
    /// <summary>
    /// Property-based tests for floor generation systems.
    /// Uses randomized inputs across many iterations to verify correctness properties.
    /// </summary>
    [TestFixture]
    public class FloorGenerationPropertyTests
    {
        private const int Iterations = 200;

        #region Property 26: Work Box Card Count by Size

        // Feature: card-battle-system, Property 26: Work Box Card Count by Size

        /// <summary>
        /// Property 26: For any WorkBoxSize, RollCardCount always returns a value
        /// within [min, max] from GetCardCountRange.
        /// Small → [1, 3], Big → [3, 5], Huge → [5, 7]
        /// Validates: Requirements 21.3
        /// </summary>
        [Test]
        public void Property26_RollCardCount_Small_AlwaysWithinRange()
        {
            var rng = new System.Random(42);

            WorkBox.GetCardCountRange(WorkBoxSize.Small, out int expectedMin, out int expectedMax);
            Assert.AreEqual(1, expectedMin, "Small min should be 1");
            Assert.AreEqual(3, expectedMax, "Small max should be 3");

            for (int i = 0; i < Iterations; i++)
            {
                int count = WorkBox.RollCardCount(WorkBoxSize.Small);

                Assert.GreaterOrEqual(count, expectedMin,
                    $"[Iter {i}] Small box card count {count} is below minimum {expectedMin}");
                Assert.LessOrEqual(count, expectedMax,
                    $"[Iter {i}] Small box card count {count} exceeds maximum {expectedMax}");
            }
        }

        [Test]
        public void Property26_RollCardCount_Big_AlwaysWithinRange()
        {
            var rng = new System.Random(99);

            WorkBox.GetCardCountRange(WorkBoxSize.Big, out int expectedMin, out int expectedMax);
            Assert.AreEqual(3, expectedMin, "Big min should be 3");
            Assert.AreEqual(5, expectedMax, "Big max should be 5");

            for (int i = 0; i < Iterations; i++)
            {
                int count = WorkBox.RollCardCount(WorkBoxSize.Big);

                Assert.GreaterOrEqual(count, expectedMin,
                    $"[Iter {i}] Big box card count {count} is below minimum {expectedMin}");
                Assert.LessOrEqual(count, expectedMax,
                    $"[Iter {i}] Big box card count {count} exceeds maximum {expectedMax}");
            }
        }

        [Test]
        public void Property26_RollCardCount_Huge_AlwaysWithinRange()
        {
            var rng = new System.Random(77);

            WorkBox.GetCardCountRange(WorkBoxSize.Huge, out int expectedMin, out int expectedMax);
            Assert.AreEqual(5, expectedMin, "Huge min should be 5");
            Assert.AreEqual(7, expectedMax, "Huge max should be 7");

            for (int i = 0; i < Iterations; i++)
            {
                int count = WorkBox.RollCardCount(WorkBoxSize.Huge);

                Assert.GreaterOrEqual(count, expectedMin,
                    $"[Iter {i}] Huge box card count {count} is below minimum {expectedMin}");
                Assert.LessOrEqual(count, expectedMax,
                    $"[Iter {i}] Huge box card count {count} exceeds maximum {expectedMax}");
            }
        }

        /// <summary>
        /// Property 26 (all sizes): For any WorkBoxSize enum value, RollCardCount
        /// always returns a value within the range from GetCardCountRange.
        /// Validates: Requirements 21.3
        /// </summary>
        [Test]
        public void Property26_RollCardCount_AllSizes_AlwaysWithinRange()
        {
            var sizes = (WorkBoxSize[])Enum.GetValues(typeof(WorkBoxSize));

            for (int i = 0; i < Iterations; i++)
            {
                foreach (var size in sizes)
                {
                    WorkBox.GetCardCountRange(size, out int min, out int max);
                    int count = WorkBox.RollCardCount(size);

                    Assert.GreaterOrEqual(count, min,
                        $"[Iter {i}] {size} box card count {count} is below minimum {min}");
                    Assert.LessOrEqual(count, max,
                        $"[Iter {i}] {size} box card count {count} exceeds maximum {max}");
                }
            }
        }

        #endregion
    }
}
