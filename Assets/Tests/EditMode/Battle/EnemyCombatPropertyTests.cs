using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CardBattle;

namespace CardBattle.Tests
{
    /// <summary>
    /// Property-based tests for enemy combat mechanics.
    /// Uses randomized inputs across many iterations to verify correctness properties.
    /// </summary>
    [TestFixture]
    public class EnemyCombatPropertyTests
    {
        private const int Iterations = 200;

        private GameObject _blockGo;
        private BlockSystem _blockSystem;
        private GameObject _statusGo;
        private StatusEffectSystem _statusEffectSystem;
        private readonly List<GameObject> _tempObjects = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            _blockGo = new GameObject("TestBlockSystem");
            _blockSystem = _blockGo.AddComponent<BlockSystem>();
            _blockSystem.Initialize();

            _statusGo = new GameObject("TestStatusEffectSystem");
            _statusEffectSystem = _statusGo.AddComponent<StatusEffectSystem>();
            _statusEffectSystem.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _tempObjects)
            {
                if (obj != null)
                    UnityEngine.Object.DestroyImmediate(obj);
            }
            _tempObjects.Clear();

            if (_statusGo != null) UnityEngine.Object.DestroyImmediate(_statusGo);
            if (_blockGo != null) UnityEngine.Object.DestroyImmediate(_blockGo);
        }

        /// <summary>
        /// Helper: create an EnemyCombatantData ScriptableObject with the given pattern.
        /// </summary>
        private EnemyCombatantData CreateEnemyData(string name, int maxHP, List<EnemyAction> pattern)
        {
            var data = ScriptableObject.CreateInstance<EnemyCombatantData>();
            data.enemyName = name;
            data.maxHP = maxHP;
            data.hoursReward = 10;
            data.attackPattern = pattern;
            return data;
        }

        /// <summary>
        /// Helper: create a fully initialized EnemyCombatant GameObject.
        /// </summary>
        private EnemyCombatant CreateEnemy(EnemyCombatantData data)
        {
            var go = new GameObject(data.enemyName);
            go.AddComponent<Health>();
            var ec = go.AddComponent<EnemyCombatant>();
            ec.Initialize(data, _blockSystem, _statusEffectSystem);
            _tempObjects.Add(go);
            return ec;
        }

        #region Property 14: Dead Enemies Are Removed and Skipped

        /// <summary>
        /// Property 14: When an EnemyCombatant's HP reaches 0, IsAlive returns false.
        /// Dead enemies should be excluded from Enemy_Phase action execution,
        /// and when all enemies are dead the encounter ends as victory.
        /// Validates: Requirements 8.5, 8.6, 8.7
        /// </summary>
        [Test]
        public void Property14_DeadEnemyIsNotAlive()
        {
            var rng = new System.Random(42);

            for (int i = 0; i < Iterations; i++)
            {
                int maxHP = rng.Next(1, 100);
                var data = CreateEnemyData($"Enemy_{i}", maxHP, new List<EnemyAction>
                {
                    new EnemyAction { actionType = EnemyActionType.DealDamage, value = 5, condition = EnemyActionCondition.None }
                });
                var enemy = CreateEnemy(data);

                Assert.IsTrue(enemy.IsAlive, $"[Iter {i}] Enemy should be alive at full HP");

                // Deal lethal damage
                int overkill = maxHP + rng.Next(0, 50);
                enemy.TakeDamage(overkill, _blockGo);

                Assert.IsFalse(enemy.IsAlive,
                    $"[Iter {i}] Enemy with maxHP={maxHP} should be dead after taking {overkill} damage");
                Assert.LessOrEqual(enemy.CurrentHP, 0,
                    $"[Iter {i}] Dead enemy HP should be <= 0");
            }
        }

        [Test]
        public void Property14_DeadEnemiesSkippedInEnemyPhase()
        {
            var rng = new System.Random(99);

            for (int i = 0; i < 50; i++)
            {
                int enemyCount = rng.Next(2, 5); // 2-4 enemies
                var enemies = new List<EnemyCombatant>();

                for (int e = 0; e < enemyCount; e++)
                {
                    int maxHP = rng.Next(10, 80);
                    var data = CreateEnemyData($"Enemy_{i}_{e}", maxHP, new List<EnemyAction>
                    {
                        new EnemyAction { actionType = EnemyActionType.DealDamage, value = 5, condition = EnemyActionCondition.None }
                    });
                    enemies.Add(CreateEnemy(data));
                }

                // Kill a random subset of enemies
                int killCount = rng.Next(1, enemyCount);
                for (int k = 0; k < killCount; k++)
                {
                    enemies[k].TakeDamage(enemies[k].MaxHP + 10, _blockGo);
                }

                // Simulate enemy phase: only living enemies execute actions
                var executedActions = new List<EnemyCombatant>();
                foreach (var enemy in enemies)
                {
                    if (enemy.IsAlive)
                    {
                        var result = enemy.ExecuteAction();
                        if (!result.WasSkipped)
                            executedActions.Add(enemy);
                    }
                }

                // Verify dead enemies did not execute
                for (int k = 0; k < killCount; k++)
                {
                    Assert.IsFalse(executedActions.Contains(enemies[k]),
                        $"[Iter {i}] Dead enemy {k} should not have executed an action");
                }

                // Verify living enemies did execute
                for (int e = killCount; e < enemyCount; e++)
                {
                    Assert.IsTrue(executedActions.Contains(enemies[e]),
                        $"[Iter {i}] Living enemy {e} should have executed an action");
                }
            }
        }

        [Test]
        public void Property14_AllDeadMeansVictory()
        {
            var rng = new System.Random(77);

            for (int i = 0; i < 50; i++)
            {
                int enemyCount = rng.Next(1, 5); // 1-4 enemies
                var enemies = new List<EnemyCombatant>();

                for (int e = 0; e < enemyCount; e++)
                {
                    int maxHP = rng.Next(5, 60);
                    var data = CreateEnemyData($"Enemy_{i}_{e}", maxHP, new List<EnemyAction>
                    {
                        new EnemyAction { actionType = EnemyActionType.DealDamage, value = 3, condition = EnemyActionCondition.None }
                    });
                    enemies.Add(CreateEnemy(data));
                }

                // Kill all enemies
                foreach (var enemy in enemies)
                {
                    enemy.TakeDamage(enemy.MaxHP + 10, _blockGo);
                }

                // Check victory condition: all enemies dead
                bool allDead = true;
                foreach (var enemy in enemies)
                {
                    if (enemy.IsAlive)
                    {
                        allDead = false;
                        break;
                    }
                }

                Assert.IsTrue(allDead,
                    $"[Iter {i}] All {enemyCount} enemies should be dead → victory condition met");
            }
        }

        [Test]
        public void Property14_PartialKillDoesNotTriggerVictory()
        {
            var rng = new System.Random(55);

            for (int i = 0; i < 50; i++)
            {
                int enemyCount = rng.Next(2, 5);
                var enemies = new List<EnemyCombatant>();

                for (int e = 0; e < enemyCount; e++)
                {
                    int maxHP = rng.Next(10, 80);
                    var data = CreateEnemyData($"Enemy_{i}_{e}", maxHP, new List<EnemyAction>
                    {
                        new EnemyAction { actionType = EnemyActionType.DealDamage, value = 5, condition = EnemyActionCondition.None }
                    });
                    enemies.Add(CreateEnemy(data));
                }

                // Kill all but the last enemy
                for (int k = 0; k < enemyCount - 1; k++)
                {
                    enemies[k].TakeDamage(enemies[k].MaxHP + 10, _blockGo);
                }

                bool allDead = true;
                foreach (var enemy in enemies)
                {
                    if (enemy.IsAlive) { allDead = false; break; }
                }

                Assert.IsFalse(allDead,
                    $"[Iter {i}] With 1 enemy still alive, victory should NOT be triggered");
            }
        }

        #endregion

        #region Property 15: Player Defeat at Zero HP

        /// <summary>
        /// Property 15: When the player's HP reaches 0, the encounter should
        /// immediately end as a player defeat.
        /// Validates: Requirements 9.5
        /// </summary>
        [Test]
        public void Property15_PlayerAtZeroHPIsDefeated()
        {
            var rng = new System.Random(42);

            for (int i = 0; i < Iterations; i++)
            {
                int maxHP = rng.Next(1, 200);
                int damage = maxHP + rng.Next(0, 50); // lethal or overkill

                var playerGo = new GameObject($"Player_{i}");
                var playerHealth = playerGo.AddComponent<Health>();
                playerHealth.maxHealth = maxHP;
                playerHealth.currentHealth = maxHP;
                playerHealth.suppressSceneLoad = true;
                _tempObjects.Add(playerGo);

                // Apply lethal damage through block system
                _blockSystem.Initialize();
                int remaining = _blockSystem.AbsorbDamage(damage, playerGo);
                playerHealth.TakeDamage(remaining);

                Assert.LessOrEqual(playerHealth.currentHealth, 0,
                    $"[Iter {i}] Player with maxHP={maxHP} should have HP<=0 after {damage} damage");

                // Defeat condition: HP <= 0
                bool isDefeated = playerHealth.currentHealth <= 0;
                Assert.IsTrue(isDefeated,
                    $"[Iter {i}] Player should be defeated at HP={playerHealth.currentHealth}");
            }
        }

        [Test]
        public void Property15_PlayerAtExactlyZeroHPIsDefeated()
        {
            var rng = new System.Random(123);

            for (int i = 0; i < 50; i++)
            {
                int maxHP = rng.Next(1, 200);

                var playerGo = new GameObject($"Player_exact_{i}");
                var playerHealth = playerGo.AddComponent<Health>();
                playerHealth.maxHealth = maxHP;
                playerHealth.currentHealth = maxHP;
                playerHealth.suppressSceneLoad = true;
                _tempObjects.Add(playerGo);

                // Deal exactly maxHP damage — should reach exactly 0
                playerHealth.TakeDamage(maxHP);

                Assert.AreEqual(0, playerHealth.currentHealth,
                    $"[Iter {i}] Player HP should be exactly 0 after taking maxHP={maxHP} damage");

                bool isDefeated = playerHealth.currentHealth <= 0;
                Assert.IsTrue(isDefeated,
                    $"[Iter {i}] Player at exactly 0 HP should be defeated");
            }
        }

        [Test]
        public void Property15_PlayerAboveZeroHPIsNotDefeated()
        {
            var rng = new System.Random(456);

            for (int i = 0; i < Iterations; i++)
            {
                int maxHP = rng.Next(2, 200);
                int damage = rng.Next(0, maxHP); // non-lethal

                var playerGo = new GameObject($"Player_alive_{i}");
                var playerHealth = playerGo.AddComponent<Health>();
                playerHealth.maxHealth = maxHP;
                playerHealth.currentHealth = maxHP;
                playerHealth.suppressSceneLoad = true;
                _tempObjects.Add(playerGo);

                playerHealth.TakeDamage(damage);

                Assert.Greater(playerHealth.currentHealth, 0,
                    $"[Iter {i}] Player with maxHP={maxHP} taking {damage} damage should still be alive");

                bool isDefeated = playerHealth.currentHealth <= 0;
                Assert.IsFalse(isDefeated,
                    $"[Iter {i}] Player at HP={playerHealth.currentHealth} should NOT be defeated");
            }
        }

        [Test]
        public void Property15_PlayerDefeatDuringEnemyPhase()
        {
            var rng = new System.Random(789);

            for (int i = 0; i < 50; i++)
            {
                int playerMaxHP = rng.Next(10, 100);
                int enemyDamage = playerMaxHP + rng.Next(0, 30); // lethal

                var playerGo = new GameObject($"Player_phase_{i}");
                var playerHealth = playerGo.AddComponent<Health>();
                playerHealth.maxHealth = playerMaxHP;
                playerHealth.currentHealth = playerMaxHP;
                playerHealth.suppressSceneLoad = true;
                _tempObjects.Add(playerGo);

                // Create an enemy that deals lethal damage
                var data = CreateEnemyData($"Killer_{i}", 50, new List<EnemyAction>
                {
                    new EnemyAction { actionType = EnemyActionType.DealDamage, value = enemyDamage, condition = EnemyActionCondition.None }
                });
                var enemy = CreateEnemy(data);

                // Execute enemy action
                var result = enemy.ExecuteAction();
                Assert.AreEqual(EnemyActionType.DealDamage, result.ActionType);

                // Apply damage to player (simulating BattleManager logic)
                _blockSystem.Initialize();
                int remaining = _blockSystem.AbsorbDamage(result.DamageValue, playerGo);
                if (remaining > 0)
                    playerHealth.TakeDamage(remaining);

                // Check immediate defeat
                Assert.LessOrEqual(playerHealth.currentHealth, 0,
                    $"[Iter {i}] Player should be defeated after enemy deals {enemyDamage} damage to {playerMaxHP} HP");
            }
        }

        #endregion

        #region Property 21: Enemy Attack Pattern Cycling

        /// <summary>
        /// Property 21: For an enemy with attack pattern of length N,
        /// the action executed on turn t (0-indexed) is pattern[t % N].
        /// Validates: Requirements 26.2
        /// </summary>
        [Test]
        public void Property21_PatternCyclesCorrectly()
        {
            var rng = new System.Random(42);

            for (int i = 0; i < Iterations; i++)
            {
                int patternLength = rng.Next(1, 6); // 1-5 actions
                var pattern = new List<EnemyAction>();

                for (int p = 0; p < patternLength; p++)
                {
                    pattern.Add(new EnemyAction
                    {
                        actionType = EnemyActionType.DealDamage,
                        value = (p + 1) * 10, // distinct values: 10, 20, 30, ...
                        condition = EnemyActionCondition.None
                    });
                }

                var data = CreateEnemyData($"Cycler_{i}", 100, pattern);
                var enemy = CreateEnemy(data);

                // Execute multiple turns (2-3 full cycles)
                int totalTurns = patternLength * rng.Next(2, 4);
                for (int t = 0; t < totalTurns; t++)
                {
                    var result = enemy.ExecuteAction();
                    int expectedIndex = t % patternLength;
                    int expectedDamage = (expectedIndex + 1) * 10;

                    Assert.AreEqual(EnemyActionType.DealDamage, result.ActionType,
                        $"[Iter {i}, Turn {t}] Action type should be DealDamage");
                    Assert.AreEqual(expectedDamage, result.DamageValue,
                        $"[Iter {i}, Turn {t}] Expected pattern[{expectedIndex}].value={expectedDamage}, got {result.DamageValue}");
                }
            }
        }

        [Test]
        public void Property21_SingleActionPatternRepeats()
        {
            var rng = new System.Random(99);

            for (int i = 0; i < 50; i++)
            {
                int damageValue = rng.Next(1, 50);
                var pattern = new List<EnemyAction>
                {
                    new EnemyAction
                    {
                        actionType = EnemyActionType.DealDamage,
                        value = damageValue,
                        condition = EnemyActionCondition.None
                    }
                };

                var data = CreateEnemyData($"Repeater_{i}", 100, pattern);
                var enemy = CreateEnemy(data);

                int turns = rng.Next(3, 10);
                for (int t = 0; t < turns; t++)
                {
                    var result = enemy.ExecuteAction();
                    Assert.AreEqual(damageValue, result.DamageValue,
                        $"[Iter {i}, Turn {t}] Single-action pattern should always deal {damageValue}");
                }
            }
        }

        [Test]
        public void Property21_MixedActionTypesInPattern()
        {
            var rng = new System.Random(77);

            for (int i = 0; i < 50; i++)
            {
                // Create a pattern with mixed action types
                var pattern = new List<EnemyAction>
                {
                    new EnemyAction { actionType = EnemyActionType.DealDamage, value = 15, condition = EnemyActionCondition.None },
                    new EnemyAction { actionType = EnemyActionType.Defend, value = 8, condition = EnemyActionCondition.None },
                    new EnemyAction { actionType = EnemyActionType.DealDamage, value = 25, condition = EnemyActionCondition.None }
                };

                var data = CreateEnemyData($"Mixed_{i}", 100, pattern);
                var enemy = CreateEnemy(data);

                int totalTurns = 3 * rng.Next(2, 4); // multiple full cycles
                for (int t = 0; t < totalTurns; t++)
                {
                    var result = enemy.ExecuteAction();
                    int expectedIndex = t % 3;
                    EnemyAction expectedAction = pattern[expectedIndex];

                    Assert.AreEqual(expectedAction.actionType, result.ActionType,
                        $"[Iter {i}, Turn {t}] Expected action type {expectedAction.actionType} at pattern index {expectedIndex}");

                    if (expectedAction.actionType == EnemyActionType.DealDamage)
                    {
                        Assert.AreEqual(expectedAction.value, result.DamageValue,
                            $"[Iter {i}, Turn {t}] Expected damage {expectedAction.value} at pattern index {expectedIndex}");
                    }
                }
            }
        }

        [Test]
        public void Property21_PatternIndexWrapsAtBoundary()
        {
            // Verify the exact boundary where pattern wraps from last to first
            var pattern = new List<EnemyAction>
            {
                new EnemyAction { actionType = EnemyActionType.DealDamage, value = 10, condition = EnemyActionCondition.None },
                new EnemyAction { actionType = EnemyActionType.DealDamage, value = 20, condition = EnemyActionCondition.None },
                new EnemyAction { actionType = EnemyActionType.DealDamage, value = 30, condition = EnemyActionCondition.None }
            };

            var data = CreateEnemyData("Wrapper", 100, pattern);
            var enemy = CreateEnemy(data);

            // Execute exactly N actions to reach the boundary
            Assert.AreEqual(10, enemy.ExecuteAction().DamageValue, "Turn 0: pattern[0]");
            Assert.AreEqual(20, enemy.ExecuteAction().DamageValue, "Turn 1: pattern[1]");
            Assert.AreEqual(30, enemy.ExecuteAction().DamageValue, "Turn 2: pattern[2]");

            // Should wrap back to pattern[0]
            Assert.AreEqual(10, enemy.ExecuteAction().DamageValue, "Turn 3: should wrap to pattern[0]");
            Assert.AreEqual(20, enemy.ExecuteAction().DamageValue, "Turn 4: pattern[1]");
            Assert.AreEqual(30, enemy.ExecuteAction().DamageValue, "Turn 5: pattern[2]");

            // Second wrap
            Assert.AreEqual(10, enemy.ExecuteAction().DamageValue, "Turn 6: should wrap to pattern[0] again");
        }

        #endregion
    }
}
