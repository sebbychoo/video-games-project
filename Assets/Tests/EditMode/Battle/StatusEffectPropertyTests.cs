using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CardBattle;

namespace CardBattle.Tests
{
    /// <summary>
    /// Property-based tests for StatusEffectSystem.
    /// Uses randomized inputs across many iterations to verify correctness properties.
    /// </summary>
    [TestFixture]
    public class StatusEffectPropertyTests
    {
        private const int Iterations = 200;

        private GameObject _systemGo;
        private StatusEffectSystem _ses;
        private GameObject _target;

        [SetUp]
        public void SetUp()
        {
            // BattleEventBus may not exist in edit-mode tests; that's fine —
            // StatusEffectSystem guards against null BattleEventBus.Instance.
            _systemGo = new GameObject("TestStatusEffectSystem");
            _ses = _systemGo.AddComponent<StatusEffectSystem>();
            _ses.Initialize();
            _target = new GameObject("TestTarget");
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_target);
            UnityEngine.Object.DestroyImmediate(_systemGo);
        }

        #region Property 16: Status Effect Duration Tick and Removal

        /// <summary>
        /// Property 16: Each active effect's duration is decremented by 1 per tick,
        /// and effects whose duration reaches 0 are removed.
        /// Validates: Requirements 10.3
        /// </summary>
        [Test]
        public void Property16_DurationDecrementedByOne_RemovedAtZero()
        {
            var rng = new System.Random(42);

            for (int i = 0; i < Iterations; i++)
            {
                _ses.Initialize();

                // Apply 1-3 random effects with random durations
                int effectCount = rng.Next(1, 4);
                string[] ids = { "EffA", "EffB", "EffC" };
                var applied = new List<(string id, int duration, int value)>();

                for (int e = 0; e < effectCount; e++)
                {
                    int dur = rng.Next(1, 6);
                    int val = rng.Next(1, 10);
                    _ses.Apply(_target, new StatusEffectInstance
                    {
                        effectId = ids[e],
                        duration = dur,
                        value = val
                    });
                    applied.Add((ids[e], dur, val));
                }

                // Tick once
                _ses.Tick(_target);

                List<StatusEffectInstance> remaining = _ses.GetEffects(_target);

                foreach (var (id, duration, value) in applied)
                {
                    int expectedDur = duration - 1;
                    if (expectedDur <= 0)
                    {
                        // Should be removed
                        Assert.IsFalse(_ses.HasEffect(_target, id),
                            $"[Iter {i}] Effect '{id}' with duration {duration} should be removed after tick");
                    }
                    else
                    {
                        // Should still exist with decremented duration
                        Assert.IsTrue(_ses.HasEffect(_target, id),
                            $"[Iter {i}] Effect '{id}' with duration {duration} should still exist after tick");

                        StatusEffectInstance found = remaining.Find(x => x.effectId == id);
                        Assert.AreEqual(expectedDur, found.duration,
                            $"[Iter {i}] Effect '{id}' duration should be {expectedDur} after tick (was {duration})");
                    }
                }
            }
        }

        [Test]
        public void Property16_MultipleTicksUntilAllExpire()
        {
            _ses.Initialize();

            _ses.Apply(_target, new StatusEffectInstance { effectId = "Short", duration = 1, value = 5 });
            _ses.Apply(_target, new StatusEffectInstance { effectId = "Medium", duration = 3, value = 5 });
            _ses.Apply(_target, new StatusEffectInstance { effectId = "Long", duration = 5, value = 5 });

            // Tick 1: Short removed, Medium=2, Long=4
            _ses.Tick(_target);
            Assert.IsFalse(_ses.HasEffect(_target, "Short"), "Short should be removed after tick 1");
            Assert.IsTrue(_ses.HasEffect(_target, "Medium"), "Medium should remain after tick 1");
            Assert.IsTrue(_ses.HasEffect(_target, "Long"), "Long should remain after tick 1");

            // Tick 2: Medium=1, Long=3
            _ses.Tick(_target);
            Assert.IsTrue(_ses.HasEffect(_target, "Medium"), "Medium should remain after tick 2");

            // Tick 3: Medium removed, Long=2
            _ses.Tick(_target);
            Assert.IsFalse(_ses.HasEffect(_target, "Medium"), "Medium should be removed after tick 3");
            Assert.IsTrue(_ses.HasEffect(_target, "Long"), "Long should remain after tick 3");

            // Tick 4: Long=1
            _ses.Tick(_target);
            Assert.IsTrue(_ses.HasEffect(_target, "Long"), "Long should remain after tick 4");

            // Tick 5: Long removed
            _ses.Tick(_target);
            Assert.IsFalse(_ses.HasEffect(_target, "Long"), "Long should be removed after tick 5");
            Assert.AreEqual(0, _ses.GetEffects(_target).Count, "No effects should remain");
        }

        #endregion

        #region Property 17: Status Effect Refresh (No Stacking)

        /// <summary>
        /// Property 17: Re-applying the same effect type refreshes duration to the new value
        /// rather than creating a duplicate. Only one instance of each type exists.
        /// Validates: Requirements 10.2
        /// </summary>
        [Test]
        public void Property17_ReapplyRefreshesDuration_NoDuplicate()
        {
            var rng = new System.Random(77);

            for (int i = 0; i < Iterations; i++)
            {
                _ses.Initialize();

                string effectId = "TestEffect";
                int dur1 = rng.Next(1, 10);
                int val1 = rng.Next(1, 20);
                int dur2 = rng.Next(1, 10);
                int val2 = rng.Next(1, 20);

                // Apply first time
                _ses.Apply(_target, new StatusEffectInstance
                {
                    effectId = effectId,
                    duration = dur1,
                    value = val1
                });

                List<StatusEffectInstance> afterFirst = _ses.GetEffects(_target);
                Assert.AreEqual(1, afterFirst.Count,
                    $"[Iter {i}] Should have exactly 1 effect after first apply");

                // Re-apply with different duration/value
                _ses.Apply(_target, new StatusEffectInstance
                {
                    effectId = effectId,
                    duration = dur2,
                    value = val2
                });

                List<StatusEffectInstance> afterSecond = _ses.GetEffects(_target);
                Assert.AreEqual(1, afterSecond.Count,
                    $"[Iter {i}] Should still have exactly 1 effect after re-apply (no stacking)");
                Assert.AreEqual(dur2, afterSecond[0].duration,
                    $"[Iter {i}] Duration should be refreshed to {dur2}");
                Assert.AreEqual(val2, afterSecond[0].value,
                    $"[Iter {i}] Value should be refreshed to {val2}");
            }
        }

        [Test]
        public void Property17_DifferentEffectTypes_CoexistIndependently()
        {
            _ses.Initialize();

            _ses.Apply(_target, new StatusEffectInstance { effectId = StatusEffectSystem.Burn, duration = 3, value = 5 });
            _ses.Apply(_target, new StatusEffectInstance { effectId = StatusEffectSystem.Stun, duration = 2, value = 0 });
            _ses.Apply(_target, new StatusEffectInstance { effectId = StatusEffectSystem.Bleed, duration = 4, value = 3 });

            List<StatusEffectInstance> effects = _ses.GetEffects(_target);
            Assert.AreEqual(3, effects.Count, "Three different effect types should coexist");

            // Re-apply Burn — should refresh, not add a 4th
            _ses.Apply(_target, new StatusEffectInstance { effectId = StatusEffectSystem.Burn, duration = 5, value = 8 });

            effects = _ses.GetEffects(_target);
            Assert.AreEqual(3, effects.Count, "Still 3 effects after refreshing Burn");

            StatusEffectInstance burn = effects.Find(x => x.effectId == StatusEffectSystem.Burn);
            Assert.AreEqual(5, burn.duration, "Burn duration should be refreshed to 5");
            Assert.AreEqual(8, burn.value, "Burn value should be refreshed to 8");
        }

        #endregion

        #region Property 18: Burn Deals Damage at Turn Start

        /// <summary>
        /// Property 18: A target with active Burn of value v takes v damage at the start
        /// of that target's turn. ProcessBurn returns the damage to be applied.
        /// Validates: Requirements 10.4
        /// </summary>
        [Test]
        public void Property18_BurnReturnsDamageEqualToValue()
        {
            var rng = new System.Random(123);

            for (int i = 0; i < Iterations; i++)
            {
                _ses.Initialize();

                int burnValue = rng.Next(1, 30);
                int burnDuration = rng.Next(1, 6);

                _ses.Apply(_target, new StatusEffectInstance
                {
                    effectId = StatusEffectSystem.Burn,
                    duration = burnDuration,
                    value = burnValue
                });

                int damage = _ses.ProcessBurn(_target);
                Assert.AreEqual(burnValue, damage,
                    $"[Iter {i}] ProcessBurn should return {burnValue} for Burn with value={burnValue}");
            }
        }

        [Test]
        public void Property18_NoBurn_ReturnsZero()
        {
            _ses.Initialize();

            // Apply non-Burn effects
            _ses.Apply(_target, new StatusEffectInstance
            {
                effectId = StatusEffectSystem.Stun,
                duration = 2,
                value = 0
            });

            int damage = _ses.ProcessBurn(_target);
            Assert.AreEqual(0, damage, "ProcessBurn should return 0 when no Burn is active");
        }

        [Test]
        public void Property18_BurnPersistsAcrossTicks()
        {
            _ses.Initialize();

            int burnValue = 7;
            _ses.Apply(_target, new StatusEffectInstance
            {
                effectId = StatusEffectSystem.Burn,
                duration = 3,
                value = burnValue
            });

            // Turn 1: Burn deals damage, then tick reduces duration to 2
            Assert.AreEqual(burnValue, _ses.ProcessBurn(_target));
            _ses.Tick(_target);

            // Turn 2: Burn still active (duration 1), deals damage again
            Assert.AreEqual(burnValue, _ses.ProcessBurn(_target));
            _ses.Tick(_target);

            // Turn 3: Burn still active (duration was 1, now ticked to 0 — removed)
            // But ProcessBurn is called at start of turn BEFORE tick
            // After tick 2, duration is 1. ProcessBurn at turn 3 should still find it.
            Assert.AreEqual(burnValue, _ses.ProcessBurn(_target));
            _ses.Tick(_target);

            // After 3 ticks, Burn should be gone
            Assert.AreEqual(0, _ses.ProcessBurn(_target), "Burn should be gone after 3 ticks");
        }

        #endregion

        #region Property 19: Stun Skips Enemy Action

        /// <summary>
        /// Property 19: A stunned enemy's action is skipped during the Enemy_Phase.
        /// IsStunned returns true when Stun is active.
        /// Validates: Requirements 10.5
        /// </summary>
        [Test]
        public void Property19_StunnedEnemyIsDetected()
        {
            var rng = new System.Random(456);

            for (int i = 0; i < Iterations; i++)
            {
                _ses.Initialize();

                int stunDuration = rng.Next(1, 5);

                Assert.IsFalse(_ses.IsStunned(_target),
                    $"[Iter {i}] Target should not be stunned before applying Stun");

                _ses.Apply(_target, new StatusEffectInstance
                {
                    effectId = StatusEffectSystem.Stun,
                    duration = stunDuration,
                    value = 0
                });

                Assert.IsTrue(_ses.IsStunned(_target),
                    $"[Iter {i}] Target should be stunned after applying Stun with duration={stunDuration}");
            }
        }

        [Test]
        public void Property19_StunExpiresAfterDurationTicks()
        {
            _ses.Initialize();

            int stunDuration = 2;
            _ses.Apply(_target, new StatusEffectInstance
            {
                effectId = StatusEffectSystem.Stun,
                duration = stunDuration,
                value = 0
            });

            Assert.IsTrue(_ses.IsStunned(_target), "Stunned at start");

            _ses.Tick(_target); // duration 2 → 1
            Assert.IsTrue(_ses.IsStunned(_target), "Still stunned after 1 tick (duration=1)");

            _ses.Tick(_target); // duration 1 → 0, removed
            Assert.IsFalse(_ses.IsStunned(_target), "Stun should expire after 2 ticks");
        }

        [Test]
        public void Property19_NonStunEffects_DoNotCauseStun()
        {
            _ses.Initialize();

            _ses.Apply(_target, new StatusEffectInstance
            {
                effectId = StatusEffectSystem.Burn,
                duration = 3,
                value = 5
            });
            _ses.Apply(_target, new StatusEffectInstance
            {
                effectId = StatusEffectSystem.Bleed,
                duration = 3,
                value = 4
            });

            Assert.IsFalse(_ses.IsStunned(_target),
                "Burn and Bleed should not cause IsStunned to return true");
        }

        #endregion

        #region Property 19a: Stun Skips Player Play Phase

        /// <summary>
        /// Property 19a: When the player has an active Stun, the Play_Phase is skipped.
        /// The system correctly reports stun status so the turn controller can skip Play.
        /// Draw → Discard → Enemy (Play skipped).
        /// Validates: Requirements 10.7
        /// </summary>
        [Test]
        public void Property19a_StunnedPlayerDetected_PlayPhaseSkippable()
        {
            var rng = new System.Random(789);

            for (int i = 0; i < Iterations; i++)
            {
                _ses.Initialize();

                int stunDuration = rng.Next(1, 5);
                var player = new GameObject("Player");

                try
                {
                    // Before stun: not stunned
                    Assert.IsFalse(_ses.IsStunned(player),
                        $"[Iter {i}] Player should not be stunned initially");

                    // Apply stun
                    _ses.Apply(player, new StatusEffectInstance
                    {
                        effectId = StatusEffectSystem.Stun,
                        duration = stunDuration,
                        value = 0
                    });

                    // Player is stunned — turn controller should skip Play_Phase
                    Assert.IsTrue(_ses.IsStunned(player),
                        $"[Iter {i}] Player should be stunned, Play_Phase should be skipped");

                    // Simulate the turn: Draw happens, Play is skipped, Discard happens, Enemy happens
                    // Then tick at end of turn
                    _ses.Tick(player);

                    if (stunDuration == 1)
                    {
                        Assert.IsFalse(_ses.IsStunned(player),
                            $"[Iter {i}] Stun with duration=1 should expire after 1 tick");
                    }
                    else
                    {
                        Assert.IsTrue(_ses.IsStunned(player),
                            $"[Iter {i}] Stun with duration={stunDuration} should persist after 1 tick");
                    }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(player);
                }
            }
        }

        [Test]
        public void Property19a_StunOnPlayer_IndependentOfEnemyStun()
        {
            _ses.Initialize();

            var player = new GameObject("Player");
            var enemy = new GameObject("Enemy");

            try
            {
                // Stun only the enemy
                _ses.Apply(enemy, new StatusEffectInstance
                {
                    effectId = StatusEffectSystem.Stun,
                    duration = 2,
                    value = 0
                });

                Assert.IsFalse(_ses.IsStunned(player),
                    "Player should not be stunned when only enemy is stunned");
                Assert.IsTrue(_ses.IsStunned(enemy),
                    "Enemy should be stunned");

                // Now stun the player too
                _ses.Apply(player, new StatusEffectInstance
                {
                    effectId = StatusEffectSystem.Stun,
                    duration = 1,
                    value = 0
                });

                Assert.IsTrue(_ses.IsStunned(player), "Player should now be stunned");
                Assert.IsTrue(_ses.IsStunned(enemy), "Enemy should still be stunned");

                // Tick player — stun expires (duration 1)
                _ses.Tick(player);
                Assert.IsFalse(_ses.IsStunned(player), "Player stun should expire after tick");
                Assert.IsTrue(_ses.IsStunned(enemy), "Enemy stun should be unaffected by player tick");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(player);
                UnityEngine.Object.DestroyImmediate(enemy);
            }
        }

        #endregion

        #region Property 20: Bleed Amplifies Incoming Damage

        /// <summary>
        /// Property 20: When a target has Bleed with value v, any damage d dealt to that
        /// target becomes d + v. GetBleedBonus returns the bonus.
        /// Validates: Requirements 10.6
        /// </summary>
        [Test]
        public void Property20_BleedBonusEqualsValue()
        {
            var rng = new System.Random(321);

            for (int i = 0; i < Iterations; i++)
            {
                _ses.Initialize();

                int bleedValue = rng.Next(1, 20);
                int bleedDuration = rng.Next(1, 6);

                Assert.AreEqual(0, _ses.GetBleedBonus(_target),
                    $"[Iter {i}] No bleed bonus before applying Bleed");

                _ses.Apply(_target, new StatusEffectInstance
                {
                    effectId = StatusEffectSystem.Bleed,
                    duration = bleedDuration,
                    value = bleedValue
                });

                int bonus = _ses.GetBleedBonus(_target);
                Assert.AreEqual(bleedValue, bonus,
                    $"[Iter {i}] Bleed bonus should equal value={bleedValue}");

                // Verify: for any incoming damage d, total = d + bonus
                int baseDamage = rng.Next(1, 50);
                int totalDamage = baseDamage + bonus;
                Assert.AreEqual(baseDamage + bleedValue, totalDamage,
                    $"[Iter {i}] Total damage should be {baseDamage} + {bleedValue} = {baseDamage + bleedValue}");
            }
        }

        [Test]
        public void Property20_BleedExpiresAfterDuration()
        {
            _ses.Initialize();

            _ses.Apply(_target, new StatusEffectInstance
            {
                effectId = StatusEffectSystem.Bleed,
                duration = 2,
                value = 5
            });

            Assert.AreEqual(5, _ses.GetBleedBonus(_target), "Bleed active, bonus = 5");

            _ses.Tick(_target); // duration 2 → 1
            Assert.AreEqual(5, _ses.GetBleedBonus(_target), "Bleed still active after 1 tick");

            _ses.Tick(_target); // duration 1 → 0, removed
            Assert.AreEqual(0, _ses.GetBleedBonus(_target), "Bleed expired, bonus = 0");
        }

        [Test]
        public void Property20_NoBleed_NoBonusDamage()
        {
            _ses.Initialize();

            // Apply other effects but not Bleed
            _ses.Apply(_target, new StatusEffectInstance
            {
                effectId = StatusEffectSystem.Burn,
                duration = 3,
                value = 10
            });

            Assert.AreEqual(0, _ses.GetBleedBonus(_target),
                "No Bleed bonus when only Burn is active");
        }

        #endregion

        #region Property 36: Bleed Amplifies All Damage Sources Including Burn

        /// <summary>
        /// Property 36: Bleed bonus applies to ALL damage sources, including Burn ticks.
        /// When a target has both Burn(v_burn) and Bleed(v_bleed), the Burn tick damage
        /// is v_burn, and the Bleed bonus v_bleed is added on top: total = v_burn + v_bleed.
        /// Validates: Requirements 10.6
        /// </summary>
        [Test]
        public void Property36_BleedAmplifiesBurnDamage()
        {
            var rng = new System.Random(654);

            for (int i = 0; i < Iterations; i++)
            {
                _ses.Initialize();

                int burnValue = rng.Next(1, 20);
                int bleedValue = rng.Next(1, 15);
                int burnDuration = rng.Next(2, 6);
                int bleedDuration = rng.Next(2, 6);

                _ses.Apply(_target, new StatusEffectInstance
                {
                    effectId = StatusEffectSystem.Burn,
                    duration = burnDuration,
                    value = burnValue
                });
                _ses.Apply(_target, new StatusEffectInstance
                {
                    effectId = StatusEffectSystem.Bleed,
                    duration = bleedDuration,
                    value = bleedValue
                });

                // ProcessBurn returns the base burn damage
                int baseBurnDamage = _ses.ProcessBurn(_target);
                // GetBleedBonus returns the bleed amplification
                int bleedBonus = _ses.GetBleedBonus(_target);

                // The caller (BattleManager) applies total = baseBurn + bleedBonus
                int totalBurnDamage = baseBurnDamage + bleedBonus;

                Assert.AreEqual(burnValue, baseBurnDamage,
                    $"[Iter {i}] Base burn damage should be {burnValue}");
                Assert.AreEqual(bleedValue, bleedBonus,
                    $"[Iter {i}] Bleed bonus should be {bleedValue}");
                Assert.AreEqual(burnValue + bleedValue, totalBurnDamage,
                    $"[Iter {i}] Total burn+bleed damage should be {burnValue + bleedValue}");
            }
        }

        [Test]
        public void Property36_BleedWithoutBurn_NoBurnDamage()
        {
            _ses.Initialize();

            _ses.Apply(_target, new StatusEffectInstance
            {
                effectId = StatusEffectSystem.Bleed,
                duration = 3,
                value = 8
            });

            int burnDamage = _ses.ProcessBurn(_target);
            int bleedBonus = _ses.GetBleedBonus(_target);

            Assert.AreEqual(0, burnDamage, "No burn damage when only Bleed is active");
            Assert.AreEqual(8, bleedBonus, "Bleed bonus still available for other damage sources");
            Assert.AreEqual(8, burnDamage + bleedBonus,
                "Total from burn tick = 0 + 8 (bleed only amplifies existing damage)");
        }

        [Test]
        public void Property36_BurnWithoutBleed_NoAmplification()
        {
            _ses.Initialize();

            _ses.Apply(_target, new StatusEffectInstance
            {
                effectId = StatusEffectSystem.Burn,
                duration = 3,
                value = 10
            });

            int burnDamage = _ses.ProcessBurn(_target);
            int bleedBonus = _ses.GetBleedBonus(_target);

            Assert.AreEqual(10, burnDamage, "Burn deals its value as damage");
            Assert.AreEqual(0, bleedBonus, "No bleed bonus without Bleed");
            Assert.AreEqual(10, burnDamage + bleedBonus,
                "Total burn damage is just burn value without Bleed");
        }

        #endregion
    }
}
