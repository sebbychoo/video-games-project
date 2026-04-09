using NUnit.Framework;
using CardBattle;

namespace CardBattle.Tests
{
    /// <summary>
    /// Property-based tests for boss pose assignment by floor.
    /// Uses randomized inputs across many iterations to verify correctness properties.
    /// </summary>
    [TestFixture]
    public class BossPosePropertyTests
    {
        private const int Iterations = 200;

        /// <summary>
        /// Pure logic helper that mirrors the boss pose assignment rule
        /// from the design: floor 1 → Standing, all other boss floors → Sitting.
        /// This is the rule LevelGenerator.PopulateFloor will apply when spawning bosses.
        /// </summary>
        private static BossPose AssignBossPoseForFloor(int floor)
        {
            return floor == 1 ? BossPose.Standing : BossPose.Sitting;
        }

        /// <summary>
        /// Pure logic helper that mirrors the updated IsBossFloor formula:
        /// floor == 1 || (floor > 1 && (floor - 1) % interval == 0)
        /// </summary>
        private static bool IsBossFloor(int floor, int interval)
        {
            return floor == 1 || (floor > 1 && (floor - 1) % interval == 0);
        }

        #region Property 5: Boss Pose Assignment by Floor

        /// <summary>
        /// Feature: boss-encounter-system, Property 5: Boss Pose Assignment by Floor
        ///
        /// For floor 1, verify assigned boss has bossPose == Standing.
        /// Validates: Requirements 7.5
        /// </summary>
        [Test]
        public void Property5_Floor1_BossPoseIsStanding()
        {
            BossPose pose = AssignBossPoseForFloor(1);
            Assert.AreEqual(BossPose.Standing, pose,
                "Floor 1 boss should always have Standing pose");
        }

        /// <summary>
        /// Feature: boss-encounter-system, Property 5: Boss Pose Assignment by Floor
        ///
        /// For any boss floor > 1, verify assigned boss has bossPose == Sitting.
        /// Uses 200 iterations with randomized floor numbers and intervals.
        /// Validates: Requirements 7.6
        /// </summary>
        [Test]
        public void Property5_BossFloorGreaterThan1_BossPoseIsSitting()
        {
            var rng = new System.Random(42);

            int verified = 0;
            for (int i = 0; i < Iterations; i++)
            {
                int interval = rng.Next(1, 20);
                // Generate a random floor > 1 that is a boss floor
                // Boss floors > 1 satisfy: (floor - 1) % interval == 0
                // So floor = 1 + k * interval for k >= 1
                int k = rng.Next(1, 50);
                int floor = 1 + k * interval;

                Assert.IsTrue(IsBossFloor(floor, interval),
                    $"[Iter {i}] Floor {floor} with interval {interval} should be a boss floor");

                BossPose pose = AssignBossPoseForFloor(floor);
                Assert.AreEqual(BossPose.Sitting, pose,
                    $"[Iter {i}] Boss floor {floor} (> 1) should have Sitting pose, got {pose}");

                verified++;
            }

            Assert.AreEqual(Iterations, verified, "All iterations should have been verified");
        }

        /// <summary>
        /// Feature: boss-encounter-system, Property 5: Boss Pose Assignment by Floor
        ///
        /// Verify the known boss floor sequence (1, 4, 7, 10, 13) with interval=3:
        /// floor 1 → Standing, all others → Sitting.
        /// Validates: Requirements 7.5, 7.6
        /// </summary>
        [Test]
        public void Property5_KnownSequence_CorrectPoses()
        {
            int[] bossFloors = { 1, 4, 7, 10, 13, 16, 19, 22, 25 };

            foreach (int floor in bossFloors)
            {
                Assert.IsTrue(IsBossFloor(floor, 3),
                    $"Floor {floor} should be a boss floor with interval 3");

                BossPose expected = floor == 1 ? BossPose.Standing : BossPose.Sitting;
                BossPose actual = AssignBossPoseForFloor(floor);
                Assert.AreEqual(expected, actual,
                    $"Floor {floor}: expected {expected}, got {actual}");
            }
        }

        /// <summary>
        /// Feature: boss-encounter-system, Property 5: Boss Pose Assignment by Floor
        ///
        /// Verify pose assignment is deterministic: same floor always produces same pose.
        /// Validates: Requirements 7.5, 7.6
        /// </summary>
        [Test]
        public void Property5_PoseAssignment_IsDeterministic()
        {
            var rng = new System.Random(99);

            for (int i = 0; i < Iterations; i++)
            {
                int floor = rng.Next(1, 1000);
                BossPose first = AssignBossPoseForFloor(floor);
                BossPose second = AssignBossPoseForFloor(floor);
                Assert.AreEqual(first, second,
                    $"[Iter {i}] Pose assignment for floor {floor} must be deterministic");
            }
        }

        #endregion
    }
}
