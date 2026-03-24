using System;
using NUnit.Framework;
using UnityEngine;
using CardBattle;

namespace CardBattle.Tests
{
    /// <summary>
    /// Property-based tests for RageBurstCalculator and overflow consumption.
    /// Uses randomized inputs across many iterations to verify correctness properties.
    /// </summary>
    [TestFixture]
    public class RageBurstPropertyTests
    {
        private const int Iterations = 200;

        private GameObject _go;
        private OverflowBuffer _overflow;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("TestRageBurst");
            // BattleEventBus needed by OverflowBuffer.Add and TryConsume
            _go.AddComponent<BattleEventBus>();
            _overflow = _go.AddComponent<OverflowBuffer>();
            _overflow.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_go);
        }

        #region Property 8: Rage Burst Formula

        /// <summary>
        /// Property 8: Piecewise linear interpolation produces exact values at
        /// GDD reference points (1→20%, 5→80%, 10→120%, 20→140%).
        /// Validates: Requirements 3.3
        /// </summary>
        [Test]
        public void Property8_ExactValuesAtReferencePoints()
        {
            Assert.AreEqual(20f, RageBurstCalculator.GetBonusPercent(1), 0.001f,
                "1 overflow → 20%");
            Assert.AreEqual(80f, RageBurstCalculator.GetBonusPercent(5), 0.001f,
                "5 overflow → 80%");
            Assert.AreEqual(120f, RageBurstCalculator.GetBonusPercent(10), 0.001f,
                "10 overflow → 120%");
            Assert.AreEqual(140f, RageBurstCalculator.GetBonusPercent(20), 0.001f,
                "20 overflow → 140%");
        }

        [Test]
        public void Property8_ClampedAt140_ForValuesAbove20()
        {
            var rng = new System.Random(42);

            for (int i = 0; i < Iterations; i++)
            {
                int overflow = rng.Next(21, 1000);
                float bonus = RageBurstCalculator.GetBonusPercent(overflow);

                Assert.AreEqual(140f, bonus, 0.001f,
                    $"[Iter {i}] Overflow={overflow} should clamp at 140%");
            }
        }

        [Test]
        public void Property8_ZeroOrNegative_ReturnsZero()
        {
            Assert.AreEqual(0f, RageBurstCalculator.GetBonusPercent(0), 0.001f,
                "0 overflow → 0%");
            Assert.AreEqual(0f, RageBurstCalculator.GetBonusPercent(-1), 0.001f,
                "-1 overflow → 0%");
            Assert.AreEqual(0f, RageBurstCalculator.GetBonusPercent(-100), 0.001f,
                "-100 overflow → 0%");
        }

        [Test]
        public void Property8_MonotonicallyIncreasing()
        {
            // For any two overflow values a < b where both > 0, bonus(a) <= bonus(b)
            var rng = new System.Random(77);

            for (int i = 0; i < Iterations; i++)
            {
                int a = rng.Next(1, 25);
                int b = rng.Next(a + 1, 30);

                float bonusA = RageBurstCalculator.GetBonusPercent(a);
                float bonusB = RageBurstCalculator.GetBonusPercent(b);

                Assert.LessOrEqual(bonusA, bonusB,
                    $"[Iter {i}] Bonus should be monotonically increasing: " +
                    $"overflow {a}→{bonusA}% should be <= overflow {b}→{bonusB}%");
            }
        }

        [Test]
        public void Property8_InterpolationBetweenReferencePoints()
        {
            var rng = new System.Random(123);

            // Reference points: (1,20), (5,80), (10,120), (20,140)
            int[] refX = { 1, 5, 10, 20 };
            float[] refY = { 20f, 80f, 120f, 140f };

            for (int i = 0; i < Iterations; i++)
            {
                // Pick a random segment
                int seg = rng.Next(0, 3);
                int x0 = refX[seg];
                int x1 = refX[seg + 1];
                float y0 = refY[seg];
                float y1 = refY[seg + 1];

                // Pick a random integer in the segment
                int x = rng.Next(x0, x1 + 1);
                float t = (float)(x - x0) / (x1 - x0);
                float expected = Mathf.Lerp(y0, y1, t);

                float actual = RageBurstCalculator.GetBonusPercent(x);

                Assert.AreEqual(expected, actual, 0.01f,
                    $"[Iter {i}] Interpolation at overflow={x} in segment [{x0},{x1}]: " +
                    $"expected {expected}%, got {actual}%");
            }
        }

        [Test]
        public void Property8_BonusDamageCalculation()
        {
            var rng = new System.Random(456);

            for (int i = 0; i < Iterations; i++)
            {
                int baseDamage = rng.Next(1, 100);
                int overflow = rng.Next(1, 25);

                float percent = RageBurstCalculator.GetBonusPercent(overflow);
                int expectedBonus = Mathf.FloorToInt(baseDamage * percent / 100f);
                int actualBonus = RageBurstCalculator.CalculateBonusDamage(baseDamage, overflow);

                Assert.AreEqual(expectedBonus, actualBonus,
                    $"[Iter {i}] BonusDamage for base={baseDamage}, overflow={overflow}: " +
                    $"expected {expectedBonus}, got {actualBonus}");
            }
        }

        #endregion

        #region Property 9: Rage Burst Consumption on Attack Only

        /// <summary>
        /// Property 9: Overflow consumed only for Attack cards (buffer reset to 0,
        /// bonus applied). For all other card types, buffer unchanged, no bonus.
        /// Validates: Requirements 3.2, 3.4, 3.5
        /// </summary>
        [Test]
        public void Property9_AttackCard_ConsumesOverflowAndReturnsBonusDamage()
        {
            var rng = new System.Random(789);

            for (int i = 0; i < Iterations; i++)
            {
                int overflowAmount = rng.Next(1, 50);
                int baseDamage = rng.Next(1, 100);

                _overflow.Initialize();
                _overflow.Add(overflowAmount);

                float expectedPercent = RageBurstCalculator.GetBonusPercent(overflowAmount);
                int expectedBonus = Mathf.FloorToInt(baseDamage * expectedPercent / 100f);

                int bonus = RageBurstCalculator.TryConsume(_overflow, CardType.Attack, baseDamage);

                Assert.AreEqual(expectedBonus, bonus,
                    $"[Iter {i}] Attack should return bonus: overflow={overflowAmount}, base={baseDamage}");
                Assert.AreEqual(0, _overflow.Current,
                    $"[Iter {i}] Overflow should be 0 after Attack consumption");
            }
        }

        [Test]
        public void Property9_NonAttackCards_DoNotConsumeOverflow()
        {
            var rng = new System.Random(321);
            CardType[] nonAttackTypes = { CardType.Defense, CardType.Effect, CardType.Utility, CardType.Special };

            for (int i = 0; i < Iterations; i++)
            {
                int overflowAmount = rng.Next(1, 50);
                int baseDamage = rng.Next(1, 100);
                CardType cardType = nonAttackTypes[rng.Next(0, nonAttackTypes.Length)];

                _overflow.Initialize();
                _overflow.Add(overflowAmount);

                int bonus = RageBurstCalculator.TryConsume(_overflow, cardType, baseDamage);

                Assert.AreEqual(0, bonus,
                    $"[Iter {i}] {cardType} card should return 0 bonus");
                Assert.AreEqual(overflowAmount, _overflow.Current,
                    $"[Iter {i}] Overflow should be unchanged ({overflowAmount}) after {cardType} card");
            }
        }

        [Test]
        public void Property9_ZeroOverflow_NoBonusForAnyCardType()
        {
            var rng = new System.Random(654);

            for (int i = 0; i < 50; i++)
            {
                int baseDamage = rng.Next(1, 100);
                CardType cardType = (CardType)rng.Next(0, 5);

                _overflow.Initialize(); // Current = 0

                int bonus = RageBurstCalculator.TryConsume(_overflow, cardType, baseDamage);

                Assert.AreEqual(0, bonus,
                    $"[Iter {i}] No bonus when overflow is 0, regardless of card type ({cardType})");
                Assert.AreEqual(0, _overflow.Current,
                    $"[Iter {i}] Overflow should remain 0");
            }
        }

        [Test]
        public void Property9_NullBuffer_ReturnsZero()
        {
            int bonus = RageBurstCalculator.TryConsume(null, CardType.Attack, 50);
            Assert.AreEqual(0, bonus, "Null buffer should return 0 bonus");
        }

        #endregion
    }
}
