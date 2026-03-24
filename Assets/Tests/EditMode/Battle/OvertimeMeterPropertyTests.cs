using System;
using NUnit.Framework;
using UnityEngine;
using CardBattle;

namespace CardBattle.Tests
{
    /// <summary>
    /// Property-based tests for OvertimeMeter.
    /// Uses randomized inputs across many iterations to verify correctness properties.
    /// </summary>
    [TestFixture]
    public class OvertimeMeterPropertyTests
    {
        private const int Iterations = 200;
        private const int DefaultMax = 10;
        private const int DefaultRegen = 2;

        private GameObject _go;
        private OvertimeMeter _meter;
        private OverflowBuffer _overflow;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("TestOvertimeMeter");
            _meter = _go.AddComponent<OvertimeMeter>();
            _overflow = _go.AddComponent<OverflowBuffer>();
            _overflow.Initialize();
            _meter.Initialize(DefaultMax, DefaultRegen, _overflow);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_go);
        }

        #region Property 5: Overtime Spend Correctness

        /// <summary>
        /// Property 5: For any meter value and card cost, spend succeeds iff cost &lt;= current,
        /// and meter decreases by exactly cost on success.
        /// Validates: Requirements 2.3, 2.4
        /// </summary>
        [Test]
        public void Property5_SpendSucceeds_WhenCostLessOrEqualCurrent()
        {
            var rng = new System.Random(42);

            for (int i = 0; i < Iterations; i++)
            {
                int max = rng.Next(1, 30);
                int initial = rng.Next(0, max + 1);
                int cost = rng.Next(0, max + 5);

                _overflow.Initialize();
                _meter.Initialize(max, DefaultRegen, _overflow);
                // Set meter to desired initial value by spending from full
                int spendToReach = max - initial;
                if (spendToReach > 0)
                    _meter.Spend(spendToReach);

                Assert.AreEqual(initial, _meter.Current,
                    $"[Iter {i}] Setup failed: expected Current={initial}");

                int beforeSpend = _meter.Current;
                bool result = _meter.Spend(cost);

                if (cost <= initial)
                {
                    Assert.IsTrue(result,
                        $"[Iter {i}] Spend({cost}) should succeed when Current={initial}");
                    Assert.AreEqual(initial - cost, _meter.Current,
                        $"[Iter {i}] After Spend({cost}) from {initial}, expected {initial - cost}");
                }
                else
                {
                    Assert.IsFalse(result,
                        $"[Iter {i}] Spend({cost}) should fail when Current={initial}");
                    Assert.AreEqual(initial, _meter.Current,
                        $"[Iter {i}] Meter should be unchanged after rejected Spend({cost})");
                }
            }
        }

        [Test]
        public void Property5_SpendZero_AlwaysSucceeds()
        {
            var rng = new System.Random(99);

            for (int i = 0; i < 50; i++)
            {
                int max = rng.Next(1, 20);
                int initial = rng.Next(0, max + 1);

                _meter.Initialize(max, DefaultRegen, _overflow);
                int spendToReach = max - initial;
                if (spendToReach > 0)
                    _meter.Spend(spendToReach);

                bool result = _meter.Spend(0);
                Assert.IsTrue(result, $"[Iter {i}] Spend(0) should always succeed");
                Assert.AreEqual(initial, _meter.Current,
                    $"[Iter {i}] Spend(0) should not change meter");
            }
        }

        #endregion

        #region Property 6: Overtime Regeneration Capped

        /// <summary>
        /// Property 6: Regen sets meter to min(v + regenAmount, max).
        /// Validates: Requirements 2.2
        /// </summary>
        [Test]
        public void Property6_RegenerationCappedAtMax()
        {
            var rng = new System.Random(123);

            for (int i = 0; i < Iterations; i++)
            {
                int max = rng.Next(1, 30);
                int regen = rng.Next(1, 10);
                int initial = rng.Next(0, max + 1);

                _overflow.Initialize();
                _meter.Initialize(max, regen, _overflow);
                int spendToReach = max - initial;
                if (spendToReach > 0)
                    _meter.Spend(spendToReach);

                Assert.AreEqual(initial, _meter.Current,
                    $"[Iter {i}] Setup: expected Current={initial}");

                _meter.Regenerate();

                int expected = Math.Min(initial + regen, max);
                Assert.AreEqual(expected, _meter.Current,
                    $"[Iter {i}] After Regen from {initial} with regen={regen}, max={max}, expected {expected}");
            }
        }

        [Test]
        public void Property6_RegenerationOverflowRoutedToBuffer()
        {
            var rng = new System.Random(456);

            for (int i = 0; i < Iterations; i++)
            {
                int max = rng.Next(1, 20);
                int regen = rng.Next(1, 10);
                // Start near max so regen overflows
                int initial = rng.Next(Math.Max(0, max - regen + 1), max + 1);

                _overflow.Initialize();
                _meter.Initialize(max, regen, _overflow);
                int spendToReach = max - initial;
                if (spendToReach > 0)
                    _meter.Spend(spendToReach);

                int overflowBefore = _overflow.Current;
                _meter.Regenerate();

                int rawNew = initial + regen;
                int expectedOverflow = rawNew > max ? rawNew - max : 0;

                Assert.AreEqual(Math.Min(rawNew, max), _meter.Current,
                    $"[Iter {i}] Meter should be capped at max={max}");
                Assert.AreEqual(overflowBefore + expectedOverflow, _overflow.Current,
                    $"[Iter {i}] Overflow should receive excess: initial={initial}, regen={regen}, max={max}");
            }
        }

        [Test]
        public void Property6_RegenerationWithToolModifier()
        {
            var rng = new System.Random(789);

            for (int i = 0; i < 100; i++)
            {
                int max = rng.Next(5, 20);
                int baseRegen = rng.Next(1, 5);
                int modifier = rng.Next(-1, 5);
                int initial = rng.Next(0, max + 1);

                _overflow.Initialize();
                _meter.Initialize(max, baseRegen, _overflow);
                _meter.ApplyRegenModifier(modifier);
                int spendToReach = max - initial;
                if (spendToReach > 0)
                    _meter.Spend(spendToReach);

                int effectiveRegen = baseRegen + modifier;
                int beforeCurrent = _meter.Current;
                _meter.Regenerate();

                if (effectiveRegen <= 0)
                {
                    Assert.AreEqual(beforeCurrent, _meter.Current,
                        $"[Iter {i}] No regen when effective regen <= 0");
                }
                else
                {
                    int expected = Math.Min(beforeCurrent + effectiveRegen, max);
                    Assert.AreEqual(expected, _meter.Current,
                        $"[Iter {i}] Regen with modifier: base={baseRegen}, mod={modifier}, from={beforeCurrent}, max={max}");
                }
            }
        }

        #endregion

        #region Property 7: Damage-to-Overtime Gain with Overflow Routing

        /// <summary>
        /// Property 7: Gain = floor(hpLost / maxHP * 10), capped at max, excess to overflow.
        /// Validates: Requirements 2.5, 2.6
        /// </summary>
        [Test]
        public void Property7_DamageGainFormula_CorrectAndCapped()
        {
            var rng = new System.Random(321);

            for (int i = 0; i < Iterations; i++)
            {
                int meterMax = rng.Next(1, 20);
                int meterInitial = rng.Next(0, meterMax + 1);
                int maxHP = rng.Next(1, 200);
                int hpLost = rng.Next(0, maxHP + 1);

                _overflow.Initialize();
                _meter.Initialize(meterMax, DefaultRegen, _overflow);
                int spendToReach = meterMax - meterInitial;
                if (spendToReach > 0)
                    _meter.Spend(spendToReach);

                int overflowBefore = _overflow.Current;
                _meter.GainFromDamage(hpLost, maxHP);

                int expectedGain = Mathf.FloorToInt((float)hpLost / maxHP * 10f);
                if (hpLost <= 0 || maxHP <= 0) expectedGain = 0;

                int rawNew = meterInitial + expectedGain;
                int expectedMeter = Math.Min(rawNew, meterMax);
                int expectedOverflow = rawNew > meterMax ? rawNew - meterMax : 0;

                Assert.AreEqual(expectedMeter, _meter.Current,
                    $"[Iter {i}] Meter after damage gain: initial={meterInitial}, gain={expectedGain}, max={meterMax}");
                Assert.AreEqual(overflowBefore + expectedOverflow, _overflow.Current,
                    $"[Iter {i}] Overflow after damage gain: excess should route to buffer");
            }
        }

        [Test]
        public void Property7_ZeroDamage_NoGain()
        {
            _meter.Initialize(DefaultMax, DefaultRegen, _overflow);
            _meter.Spend(5); // Current = 5

            int before = _meter.Current;
            _meter.GainFromDamage(0, 100);

            Assert.AreEqual(before, _meter.Current, "Zero damage should not change meter");
            Assert.AreEqual(0, _overflow.Current, "Zero damage should not add overflow");
        }

        [Test]
        public void Property7_FullDamage_GainsMaxPoints()
        {
            // hpLost == maxHP → gain = floor(1.0 * 10) = 10
            _overflow.Initialize();
            _meter.Initialize(10, DefaultRegen, _overflow);
            _meter.Spend(10); // Current = 0

            _meter.GainFromDamage(100, 100);

            Assert.AreEqual(10, _meter.Current, "Full HP loss should fill meter to max");
            Assert.AreEqual(0, _overflow.Current, "No overflow when meter exactly fills");
        }

        [Test]
        public void Property7_OverflowRouting_WhenMeterNearFull()
        {
            _overflow.Initialize();
            _meter.Initialize(10, DefaultRegen, _overflow);
            // Current = 10 (full), take 50% HP damage → gain = 5
            _meter.GainFromDamage(50, 100);

            Assert.AreEqual(10, _meter.Current, "Meter should stay at max");
            Assert.AreEqual(5, _overflow.Current, "All 5 gained points should overflow");
        }

        #endregion

        #region Property 35: Status Effect OT Gain Capped at 1 Per Tick

        /// <summary>
        /// Property 35: Status effect damage grants at most 1 OT per tick,
        /// regardless of the damage amount.
        /// Validates: Requirements 2.6
        /// </summary>
        [Test]
        public void Property35_StatusTickGainCappedAtOne()
        {
            var rng = new System.Random(555);

            for (int i = 0; i < Iterations; i++)
            {
                int meterMax = rng.Next(2, 20);
                int meterInitial = rng.Next(0, meterMax);
                int maxHP = rng.Next(10, 200);
                // Large damage that would normally grant many OT points
                int hpLost = rng.Next(1, maxHP + 1);

                _overflow.Initialize();
                _meter.Initialize(meterMax, DefaultRegen, _overflow);
                int spendToReach = meterMax - meterInitial;
                if (spendToReach > 0)
                    _meter.Spend(spendToReach);

                int before = _meter.Current;
                _meter.GainFromDamage(hpLost, maxHP, isStatusTick: true);

                int normalGain = Mathf.FloorToInt((float)hpLost / maxHP * 10f);
                int cappedGain = Math.Min(normalGain, 1);
                if (hpLost <= 0) cappedGain = 0;

                int expectedMeter = Math.Min(before + cappedGain, meterMax);
                int expectedOverflow = (before + cappedGain) > meterMax
                    ? (before + cappedGain) - meterMax : 0;

                Assert.AreEqual(expectedMeter, _meter.Current,
                    $"[Iter {i}] Status tick gain should be at most 1: hpLost={hpLost}, maxHP={maxHP}, normalGain={normalGain}");

                // The key property: gain is at most 1
                int actualGain = _meter.Current - before + _overflow.Current;
                Assert.LessOrEqual(actualGain, 1,
                    $"[Iter {i}] Total gain (meter + overflow) from status tick must be <= 1");
            }
        }

        [Test]
        public void Property35_StatusTick_LargeDamage_StillCappedAtOne()
        {
            _overflow.Initialize();
            _meter.Initialize(10, DefaultRegen, _overflow);
            _meter.Spend(5); // Current = 5

            // 100% HP damage would normally grant 10 OT, but status tick caps at 1
            _meter.GainFromDamage(100, 100, isStatusTick: true);

            Assert.AreEqual(6, _meter.Current, "Should gain exactly 1 from status tick");
            Assert.AreEqual(0, _overflow.Current, "No overflow from capped status tick");
        }

        [Test]
        public void Property35_StatusTick_VsNormalDamage_Comparison()
        {
            // Normal damage: 50% HP → gain 5
            _overflow.Initialize();
            _meter.Initialize(10, DefaultRegen, _overflow);
            _meter.Spend(10); // Current = 0
            _meter.GainFromDamage(50, 100, isStatusTick: false);
            int normalResult = _meter.Current;

            // Status tick: same damage → gain capped at 1
            _overflow.Initialize();
            _meter.Initialize(10, DefaultRegen, _overflow);
            _meter.Spend(10); // Current = 0
            _meter.GainFromDamage(50, 100, isStatusTick: true);
            int statusResult = _meter.Current;

            Assert.AreEqual(5, normalResult, "Normal damage should grant 5 OT");
            Assert.AreEqual(1, statusResult, "Status tick should grant only 1 OT");
        }

        [Test]
        public void Property35_StatusTick_AtFullMeter_OverflowsCappedGain()
        {
            _overflow.Initialize();
            _meter.Initialize(10, DefaultRegen, _overflow);
            // Meter is full (10/10), status tick with big damage

            _meter.GainFromDamage(80, 100, isStatusTick: true);

            Assert.AreEqual(10, _meter.Current, "Meter stays at max");
            Assert.AreEqual(1, _overflow.Current,
                "Capped gain of 1 should overflow when meter is full");
        }

        #endregion
    }
}
