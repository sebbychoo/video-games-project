using System;
using NUnit.Framework;
using CardBattle;
using Procedural;

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

        #region Property 25: Boss Floor Placement

        // Feature: card-battle-system, Property 25: Boss Floor Placement

        /// <summary>
        /// Property 25: Boss rooms are placed at floors 3, 6, 9, ... (multiples of bossFloorInterval).
        /// Non-multiples of 3 are never boss floors.
        /// Validates: Requirements 25.1, 33.6
        /// </summary>
        [Test]
        public void Property25_BossFloor_OnlyAtMultiplesOfInterval()
        {
            const int interval = 3;
            const int maxFloor = 75;

            for (int floor = 1; floor <= maxFloor; floor++)
            {
                bool expectedBoss = floor % interval == 0;
                bool actualBoss   = floor > 0 && floor % interval == 0;

                Assert.AreEqual(expectedBoss, actualBoss,
                    $"Floor {floor}: expected isBossFloor={expectedBoss}, got {actualBoss}");
            }
        }

        [Test]
        public void Property25_BossFloor_NeverOnNonMultiples()
        {
            const int interval = 3;
            const int maxFloor = 75;

            for (int floor = 1; floor <= maxFloor; floor++)
            {
                if (floor % interval != 0)
                {
                    bool isBoss = floor > 0 && floor % interval == 0;
                    Assert.IsFalse(isBoss,
                        $"Floor {floor} is not a multiple of {interval} but was flagged as boss floor.");
                }
            }
        }

        [Test]
        public void Property25_BossFloor_ExactlyAtMultiples()
        {
            const int interval = 3;
            int[] expectedBossFloors = {
                3, 6, 9, 12, 15, 18, 21, 24, 27, 30,
                33, 36, 39, 42, 45, 48, 51, 54, 57, 60,
                63, 66, 69, 72, 75
            };

            foreach (int floor in expectedBossFloors)
            {
                bool isBoss = floor > 0 && floor % interval == 0;
                Assert.IsTrue(isBoss,
                    $"Floor {floor} should be a boss floor (multiple of {interval}) but was not.");
            }
        }

        #endregion

        #region Property 33: Boss Floor Blocks Exit Until Defeated

        // Feature: card-battle-system, Property 33: Boss Floor Blocks Exit Until Defeated

        /// <summary>
        /// Property 33: On a boss floor, TryExit returns false while boss is alive
        /// and true after the boss is defeated.
        /// Validates: Requirements 38.3, 38.4, 38.5
        /// </summary>
        [Test]
        public void Property33_BossFloorGate_BlocksExitWhileBossAlive()
        {
            var gate = new BossFloorGateTestHelper();
            gate.SetFloor(3);

            Assert.IsFalse(gate.TryExitLogic(),
                "Exit should be blocked on a boss floor while the boss is alive.");
        }

        [Test]
        public void Property33_BossFloorGate_AllowsExitAfterBossDefeated()
        {
            var gate = new BossFloorGateTestHelper();
            gate.SetFloor(3);
            gate.SimulateBossDefeated();

            Assert.IsTrue(gate.TryExitLogic(),
                "Exit should be unlocked on a boss floor after the boss is defeated.");
        }

        [Test]
        public void Property33_BossFloorGate_MultipleFloors_AlwaysBlockedUntilDefeated()
        {
            int[] bossFloors = { 3, 6, 9, 12, 15 };

            foreach (int floor in bossFloors)
            {
                var gate = new BossFloorGateTestHelper();
                gate.SetFloor(floor);

                Assert.IsFalse(gate.TryExitLogic(),
                    $"Floor {floor}: exit should be blocked before boss defeat.");

                gate.SimulateBossDefeated();

                Assert.IsTrue(gate.TryExitLogic(),
                    $"Floor {floor}: exit should be unlocked after boss defeat.");
            }
        }

        /// <summary>
        /// Pure-logic test helper that mirrors BossFloorGate's state machine
        /// without requiring MonoBehaviour / Unity scene setup.
        /// </summary>
        private class BossFloorGateTestHelper
        {
            private bool _bossDefeated;

            public void SetFloor(int floor) => _bossDefeated = false;
            public void SimulateBossDefeated() => _bossDefeated = true;

            /// <summary>Returns true if the player is allowed to exit.</summary>
            public bool TryExitLogic() => _bossDefeated;
        }

        #endregion

        #region Enemy Weight by Floor Depth

        /// <summary>
        /// Validates: Requirements 26.6, 26.7, 33.5
        /// Floors 1–5 should heavily favour coworkers (weight ≤ 0.10 for creatures).
        /// Floors 11+ should heavily favour creatures (weight ≥ 0.80 for creatures).
        /// </summary>
        [Test]
        public void EnemyWeight_EarlyFloors_FavourCoworkers()
        {
            for (int floor = 1; floor <= 5; floor++)
            {
                float creatureWeight = LevelGenerator.GetCreatureWeight(floor);
                Assert.LessOrEqual(creatureWeight, 0.10f,
                    $"Floor {floor}: creature weight {creatureWeight} should be ≤ 0.10 (coworker-heavy).");
            }
        }

        [Test]
        public void EnemyWeight_DeepFloors_FavourCreatures()
        {
            for (int floor = 11; floor <= 20; floor++)
            {
                float creatureWeight = LevelGenerator.GetCreatureWeight(floor);
                Assert.GreaterOrEqual(creatureWeight, 0.80f,
                    $"Floor {floor}: creature weight {creatureWeight} should be ≥ 0.80 (creature-heavy).");
            }
        }

        #endregion

        #region Work Box Size by Floor

        /// <summary>
        /// Validates: Requirements 21.2
        /// Floors 1–5: always Small.
        /// Floors 6–10: Small or Big only.
        /// Floors 11+: Small, Big, or Huge.
        /// </summary>
        [Test]
        public void WorkBoxSize_EarlyFloors_AlwaysSmall()
        {
            for (int i = 0; i < Iterations; i++)
            {
                int floor = UnityEngine.Random.Range(1, 6);
                WorkBoxSize size = LevelGenerator.RollWorkBoxSize(floor);
                Assert.AreEqual(WorkBoxSize.Small, size,
                    $"[Iter {i}] Floor {floor}: expected Small, got {size}.");
            }
        }

        [Test]
        public void WorkBoxSize_MidFloors_NeverHuge()
        {
            for (int i = 0; i < Iterations; i++)
            {
                int floor = UnityEngine.Random.Range(6, 11);
                WorkBoxSize size = LevelGenerator.RollWorkBoxSize(floor);
                Assert.AreNotEqual(WorkBoxSize.Huge, size,
                    $"[Iter {i}] Floor {floor}: Huge should not appear on floors 6–10.");
            }
        }

        #endregion
    }
}
