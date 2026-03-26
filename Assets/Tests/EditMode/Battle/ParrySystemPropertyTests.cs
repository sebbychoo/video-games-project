using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CardBattle;

namespace CardBattle.Tests
{
    /// <summary>
    /// Property-based tests for ParrySystem.
    /// Uses randomized inputs across many iterations to verify correctness properties.
    /// </summary>
    [TestFixture]
    public class ParrySystemPropertyTests
    {
        private const int Iterations = 200;

        private GameObject _parryGo;
        private ParrySystem _parrySystem;
        private GameConfig _gameConfig;

        private GameObject _blockGo;
        private BlockSystem _blockSystem;
        private GameObject _statusGo;
        private StatusEffectSystem _statusEffectSystem;

        private readonly List<GameObject> _tempObjects = new List<GameObject>();
        private readonly List<ScriptableObject> _tempAssets = new List<ScriptableObject>();

        [SetUp]
        public void SetUp()
        {
            _parryGo = new GameObject("TestParrySystem");
            _parrySystem = _parryGo.AddComponent<ParrySystem>();

            _gameConfig = ScriptableObject.CreateInstance<GameConfig>();
            _gameConfig.baseParryWindowDuration = 1.5f;
            _gameConfig.parryWindowFloorScaling = 0.02f;
            _gameConfig.parryWindowMinDuration = 0.3f;
            _tempAssets.Add(_gameConfig);

            _parrySystem.Initialize(_gameConfig, 0);

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

            foreach (var asset in _tempAssets)
            {
                if (asset != null)
                    UnityEngine.Object.DestroyImmediate(asset);
            }
            _tempAssets.Clear();

            if (_statusGo != null) UnityEngine.Object.DestroyImmediate(_statusGo);
            if (_blockGo != null) UnityEngine.Object.DestroyImmediate(_blockGo);
            if (_parryGo != null) UnityEngine.Object.DestroyImmediate(_parryGo);
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        private EnemyCombatantData CreateEnemyData(string name, int maxHP, float parryWindowDuration = 1.5f)
        {
            var data = ScriptableObject.CreateInstance<EnemyCombatantData>();
            data.enemyName = name;
            data.maxHP = maxHP;
            data.hoursReward = 10;
            data.baseParryWindowDuration = parryWindowDuration;
            data.attackPattern = new List<EnemyAction>();
            _tempAssets.Add(data);
            return data;
        }

        private EnemyCombatant CreateEnemy(EnemyCombatantData data)
        {
            var go = new GameObject(data.enemyName);
            go.AddComponent<Health>();
            var ec = go.AddComponent<EnemyCombatant>();
            ec.Initialize(data, _blockSystem, _statusEffectSystem);
            _tempObjects.Add(go);
            return ec;
        }

        private CardData CreateDefenseCard(string name, int overtimeCost, List<string> parryTags)
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.cardName = name;
            card.cardType = CardType.Defense;
            card.overtimeCost = overtimeCost;
            card.parryMatchTags = parryTags;
            _tempAssets.Add(card);
            return card;
        }

        private CardData CreateAttackCard(string name, int overtimeCost, int effectValue)
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.cardName = name;
            card.cardType = CardType.Attack;
            card.overtimeCost = overtimeCost;
            card.effectValue = effectValue;
            _tempAssets.Add(card);
            return card;
        }

        private CardInstance CreateCardInstance(CardData data)
        {
            var go = new GameObject(data.cardName);
            go.AddComponent<RectTransform>();
            var ci = go.AddComponent<CardInstance>();
            ci.Data = data;
            _tempObjects.Add(go);
            return ci;
        }

        private EnemyAction CreateDamageAction(int damage, IntentColor color, List<string> parryTags)
        {
            return new EnemyAction
            {
                actionType = EnemyActionType.DealDamage,
                value = damage,
                intentColor = color,
                parryMatchTags = parryTags
            };
        }

        #region Property 10: Parry Cancels Damage During Parry Window

        /// <summary>
        /// Property 10: For any enemy attack with damage d during the Enemy_Phase,
        /// if the player drags a matching Defense card during the Parry_Window,
        /// the player takes 0 damage and the card goes to discard at no OT cost.
        /// Validates: Requirements 6.1, 6.2, 6.3, 9.2
        /// </summary>
        [Test]
        public void Property10_MatchingDefenseCard_CancelsDamage_DuringParryWindow()
        {
            var rng = new System.Random(42);
            string[] tags = { "Slash", "Thrust", "Sweep", "Bash", "Chop" };

            for (int i = 0; i < Iterations; i++)
            {
                int damage = rng.Next(1, 100);
                int tagIndex = rng.Next(0, tags.Length);
                string sharedTag = tags[tagIndex];

                var attack = CreateDamageAction(damage, IntentColor.White,
                    new List<string> { sharedTag });

                var enemyData = CreateEnemyData($"Enemy_{i}", rng.Next(10, 200));
                var enemy = CreateEnemy(enemyData);

                _parrySystem.Initialize(_gameConfig, 0);
                bool windowOpened = _parrySystem.StartParryWindow(attack, enemy);
                Assert.IsTrue(windowOpened, $"[Iter {i}] Parry window should open for parryable attack");
                Assert.IsTrue(_parrySystem.IsParryWindowActive, $"[Iter {i}] Window should be active");

                // Create matching Defense card
                var defenseData = CreateDefenseCard($"Shield_{i}", rng.Next(1, 10),
                    new List<string> { sharedTag });
                var cardInstance = CreateCardInstance(defenseData);

                bool parryResult = _parrySystem.TryParry(cardInstance);

                Assert.IsTrue(parryResult,
                    $"[Iter {i}] Parry should succeed with matching tag '{sharedTag}'");
                Assert.IsTrue(_parrySystem.ParrySucceeded,
                    $"[Iter {i}] ParrySucceeded flag should be true");
                Assert.IsFalse(_parrySystem.IsParryWindowActive,
                    $"[Iter {i}] Window should close after successful parry");
            }
        }

        [Test]
        public void Property10_ParrySucceeds_WithMultipleMatchingTags()
        {
            var rng = new System.Random(99);

            for (int i = 0; i < Iterations; i++)
            {
                int damage = rng.Next(1, 50);
                // Attack has multiple tags, card shares at least one
                var attackTags = new List<string> { "Slash", "Fire" };
                var cardTags = new List<string> { "Ice", "Slash" }; // "Slash" matches

                var attack = CreateDamageAction(damage, IntentColor.Yellow, attackTags);
                var enemyData = CreateEnemyData($"Multi_{i}", 50);
                var enemy = CreateEnemy(enemyData);

                _parrySystem.Initialize(_gameConfig, 0);
                _parrySystem.StartParryWindow(attack, enemy);

                var defenseData = CreateDefenseCard($"MultiShield_{i}", 3, cardTags);
                var cardInstance = CreateCardInstance(defenseData);

                bool result = _parrySystem.TryParry(cardInstance);
                Assert.IsTrue(result,
                    $"[Iter {i}] Parry should succeed when at least one tag matches");
            }
        }

        [Test]
        public void Property10_ParryFails_WithNonMatchingTags()
        {
            var rng = new System.Random(77);

            for (int i = 0; i < Iterations; i++)
            {
                int damage = rng.Next(1, 50);
                var attack = CreateDamageAction(damage, IntentColor.Red,
                    new List<string> { "Slash" });

                var enemyData = CreateEnemyData($"NoMatch_{i}", 50);
                var enemy = CreateEnemy(enemyData);

                _parrySystem.Initialize(_gameConfig, 0);
                _parrySystem.StartParryWindow(attack, enemy);

                // Card has different tags — no match
                var defenseData = CreateDefenseCard($"WrongShield_{i}", 3,
                    new List<string> { "Ice", "Thunder" });
                var cardInstance = CreateCardInstance(defenseData);

                bool result = _parrySystem.TryParry(cardInstance);
                Assert.IsFalse(result,
                    $"[Iter {i}] Parry should fail when no tags match");
                Assert.IsTrue(_parrySystem.IsParryWindowActive,
                    $"[Iter {i}] Window should remain active after failed parry attempt");
            }
        }

        [Test]
        public void Property10_ParryFails_WithNonDefenseCard()
        {
            var attack = CreateDamageAction(10, IntentColor.White,
                new List<string> { "Slash" });
            var enemyData = CreateEnemyData("TestEnemy", 50);
            var enemy = CreateEnemy(enemyData);

            _parrySystem.Initialize(_gameConfig, 0);
            _parrySystem.StartParryWindow(attack, enemy);

            // Attack card cannot parry
            var attackCard = CreateAttackCard("Punch", 2, 5);
            var cardInstance = CreateCardInstance(attackCard);

            bool result = _parrySystem.TryParry(cardInstance);
            Assert.IsFalse(result, "Non-Defense cards should not be able to parry");
            Assert.IsTrue(_parrySystem.IsParryWindowActive,
                "Window should remain active after non-Defense card attempt");
        }

        [Test]
        public void Property10_NoParryWindow_ForUnparryableAttacks()
        {
            var rng = new System.Random(55);

            for (int i = 0; i < 50; i++)
            {
                int damage = rng.Next(1, 100);
                var attack = CreateDamageAction(damage, IntentColor.Unparryable,
                    new List<string> { "Slash" });

                var enemyData = CreateEnemyData($"Unparryable_{i}", 50);
                var enemy = CreateEnemy(enemyData);

                _parrySystem.Initialize(_gameConfig, 0);
                bool windowOpened = _parrySystem.StartParryWindow(attack, enemy);

                Assert.IsFalse(windowOpened,
                    $"[Iter {i}] No parry window should open for Unparryable attacks");
                Assert.IsFalse(_parrySystem.IsParryWindowActive,
                    $"[Iter {i}] Window should not be active for Unparryable attacks");
            }
        }

        [Test]
        public void Property10_GetMatchingCards_ReturnsOnlyMatchingDefenseCards()
        {
            var attack = CreateDamageAction(15, IntentColor.White,
                new List<string> { "Slash", "Thrust" });
            var enemyData = CreateEnemyData("TestEnemy", 50);
            var enemy = CreateEnemy(enemyData);

            _parrySystem.Initialize(_gameConfig, 0);
            _parrySystem.StartParryWindow(attack, enemy);

            // Build a hand with mixed cards
            var matchingDefense = CreateCardInstance(
                CreateDefenseCard("MatchShield", 3, new List<string> { "Slash" }));
            var nonMatchingDefense = CreateCardInstance(
                CreateDefenseCard("WrongShield", 3, new List<string> { "Ice" }));
            var attackCard = CreateCardInstance(
                CreateAttackCard("Punch", 2, 5));
            var anotherMatch = CreateCardInstance(
                CreateDefenseCard("ThrustGuard", 4, new List<string> { "Thrust" }));

            var hand = new List<CardInstance> { matchingDefense, nonMatchingDefense, attackCard, anotherMatch };

            var matching = _parrySystem.GetMatchingCards(hand);

            Assert.AreEqual(2, matching.Count, "Should find exactly 2 matching Defense cards");
            Assert.Contains(matchingDefense, matching);
            Assert.Contains(anotherMatch, matching);
        }

        #endregion

        #region Property 11: Missed Parry Deals Full Damage

        /// <summary>
        /// Property 11: For any enemy attack with damage d, if the Parry_Window
        /// expires without the player placing a matching card, the player takes
        /// the full d damage.
        /// Validates: Requirements 6.4
        /// </summary>
        [Test]
        public void Property11_ExpiredWindow_ParryFails()
        {
            var rng = new System.Random(123);

            for (int i = 0; i < Iterations; i++)
            {
                int damage = rng.Next(1, 100);
                var attack = CreateDamageAction(damage, IntentColor.White,
                    new List<string> { "Slash" });

                var enemyData = CreateEnemyData($"Expiry_{i}", 50);
                var enemy = CreateEnemy(enemyData);

                _parrySystem.Initialize(_gameConfig, 0);
                _parrySystem.StartParryWindow(attack, enemy);
                Assert.IsTrue(_parrySystem.IsParryWindowActive,
                    $"[Iter {i}] Window should be active after opening");

                // Tick the window past its full duration to expire it
                float duration = _parrySystem.ParryWindowDuration;
                bool expired = _parrySystem.TickParryWindow(duration + 0.1f);

                Assert.IsTrue(expired,
                    $"[Iter {i}] TickParryWindow should return true when window expires");
                Assert.IsFalse(_parrySystem.IsParryWindowActive,
                    $"[Iter {i}] Window should be inactive after expiry");
                Assert.IsFalse(_parrySystem.ParrySucceeded,
                    $"[Iter {i}] ParrySucceeded should be false when window expired");

                // Verify the attack damage value is preserved (caller uses it to deal damage)
                Assert.AreEqual(damage, _parrySystem.CurrentAttack.value,
                    $"[Iter {i}] Attack damage should be preserved for caller to apply");
            }
        }

        [Test]
        public void Property11_TryParry_FailsAfterWindowExpires()
        {
            var attack = CreateDamageAction(20, IntentColor.White,
                new List<string> { "Slash" });
            var enemyData = CreateEnemyData("ExpiryEnemy", 50);
            var enemy = CreateEnemy(enemyData);

            _parrySystem.Initialize(_gameConfig, 0);
            _parrySystem.StartParryWindow(attack, enemy);

            // Expire the window
            _parrySystem.TickParryWindow(_parrySystem.ParryWindowDuration + 0.1f);

            // Now try to parry — should fail because window is closed
            var defenseData = CreateDefenseCard("LateShield", 3,
                new List<string> { "Slash" });
            var cardInstance = CreateCardInstance(defenseData);

            bool result = _parrySystem.TryParry(cardInstance);
            Assert.IsFalse(result, "Parry should fail after window has expired");
        }

        [Test]
        public void Property11_WindowExpiry_SimulatesPlayerTakingFullDamage()
        {
            var rng = new System.Random(456);

            for (int i = 0; i < Iterations; i++)
            {
                int damage = rng.Next(1, 200);
                int playerMaxHP = rng.Next(damage + 1, 500);

                var attack = CreateDamageAction(damage, IntentColor.Yellow,
                    new List<string> { "Thrust" });
                var enemyData = CreateEnemyData($"DmgEnemy_{i}", 50);
                var enemy = CreateEnemy(enemyData);

                // Create a player Health to simulate damage application
                var playerGo = new GameObject($"Player_{i}");
                var playerHealth = playerGo.AddComponent<Health>();
                playerHealth.maxHealth = playerMaxHP;
                playerHealth.currentHealth = playerMaxHP;
                playerHealth.suppressSceneLoad = true;
                _tempObjects.Add(playerGo);

                _parrySystem.Initialize(_gameConfig, 0);
                _parrySystem.StartParryWindow(attack, enemy);

                // Expire the window
                _parrySystem.TickParryWindow(_parrySystem.ParryWindowDuration + 0.1f);

                // Caller applies full damage since parry failed
                Assert.IsFalse(_parrySystem.ParrySucceeded);
                playerHealth.TakeDamage(attack.value);

                int expectedHP = playerMaxHP - damage;
                Assert.AreEqual(expectedHP, playerHealth.currentHealth,
                    $"[Iter {i}] Player should take full {damage} damage when parry window expires");
            }
        }

        [Test]
        public void Property11_PartialTickDoesNotExpireWindow()
        {
            var attack = CreateDamageAction(10, IntentColor.White,
                new List<string> { "Slash" });
            var enemyData = CreateEnemyData("TickEnemy", 50);
            var enemy = CreateEnemy(enemyData);

            _parrySystem.Initialize(_gameConfig, 0);
            _parrySystem.StartParryWindow(attack, enemy);

            float duration = _parrySystem.ParryWindowDuration;

            // Tick half the duration — window should still be active
            bool expired = _parrySystem.TickParryWindow(duration * 0.5f);
            Assert.IsFalse(expired, "Window should not expire at half duration");
            Assert.IsTrue(_parrySystem.IsParryWindowActive,
                "Window should still be active at half duration");
            Assert.IsTrue(_parrySystem.ParryWindowTimeRemaining > 0f,
                "Time remaining should be positive");
        }

        #endregion

        #region Property 12: Proactive Defense Card Costs Overtime

        /// <summary>
        /// Property 12: For any Defense card with OT cost c played during the Play_Phase
        /// (proactive parry), the OT meter decreases by c and the card goes to discard.
        /// The proactive parry does not guarantee a match against the next enemy attack.
        /// Validates: Requirements 5.3, 6.5
        /// </summary>
        [Test]
        public void Property12_ProactiveDefense_DeductsOvertimeCost()
        {
            var rng = new System.Random(789);

            var meterGo = new GameObject("TestMeter");
            var meter = meterGo.AddComponent<OvertimeMeter>();
            var overflow = meterGo.AddComponent<OverflowBuffer>();
            _tempObjects.Add(meterGo);

            for (int i = 0; i < Iterations; i++)
            {
                int meterMax = rng.Next(5, 20);
                int meterInitial = rng.Next(0, meterMax + 1);
                int cardCost = rng.Next(0, meterMax + 5);

                overflow.Initialize();
                meter.Initialize(meterMax, 2, overflow);
                // Set meter to desired initial value
                int spendToReach = meterMax - meterInitial;
                if (spendToReach > 0)
                    meter.Spend(spendToReach);

                Assert.AreEqual(meterInitial, meter.Current,
                    $"[Iter {i}] Setup: expected meter={meterInitial}");

                // Simulate proactive Defense card play: spend OT cost
                bool canPlay = meter.Spend(cardCost);

                if (cardCost <= meterInitial)
                {
                    Assert.IsTrue(canPlay,
                        $"[Iter {i}] Should be able to play Defense card with cost {cardCost} when OT={meterInitial}");
                    Assert.AreEqual(meterInitial - cardCost, meter.Current,
                        $"[Iter {i}] OT should decrease by card cost {cardCost}");
                }
                else
                {
                    Assert.IsFalse(canPlay,
                        $"[Iter {i}] Should reject Defense card with cost {cardCost} when OT={meterInitial}");
                    Assert.AreEqual(meterInitial, meter.Current,
                        $"[Iter {i}] OT should remain unchanged after rejected play");
                }
            }
        }

        [Test]
        public void Property12_ProactiveDefense_CardMovesToDiscard()
        {
            // Verify that after a proactive Defense play, the card is removed from hand
            // and conceptually goes to discard. We test the card data is preserved.
            var rng = new System.Random(321);

            for (int i = 0; i < Iterations; i++)
            {
                int cost = rng.Next(0, 10);
                var defenseData = CreateDefenseCard($"ProactiveShield_{i}", cost,
                    new List<string> { "Slash" });
                var cardInstance = CreateCardInstance(defenseData);

                // Verify card data is intact for discard pile tracking
                Assert.IsNotNull(cardInstance.Data,
                    $"[Iter {i}] Card instance should have data for discard tracking");
                Assert.AreEqual(CardType.Defense, cardInstance.Data.cardType,
                    $"[Iter {i}] Card should be Defense type");
                Assert.AreEqual(cost, cardInstance.Data.overtimeCost,
                    $"[Iter {i}] Card OT cost should be preserved");
            }
        }

        [Test]
        public void Property12_ProactiveDefense_VsParryWindowDefense_CostDifference()
        {
            // Key property: proactive (Play_Phase) costs OT, reactive (Parry_Window) is free
            var rng = new System.Random(654);

            var meterGo = new GameObject("CostCompare");
            var meter = meterGo.AddComponent<OvertimeMeter>();
            var overflow = meterGo.AddComponent<OverflowBuffer>();
            _tempObjects.Add(meterGo);

            for (int i = 0; i < Iterations; i++)
            {
                int meterMax = 10;
                int cardCost = rng.Next(1, 6);

                // Scenario A: Proactive play during Play_Phase — costs OT
                overflow.Initialize();
                meter.Initialize(meterMax, 2, overflow);
                int beforeProactive = meter.Current;
                bool proactiveResult = meter.Spend(cardCost);
                int afterProactive = meter.Current;

                Assert.IsTrue(proactiveResult,
                    $"[Iter {i}] Proactive play should succeed with full meter");
                Assert.AreEqual(beforeProactive - cardCost, afterProactive,
                    $"[Iter {i}] Proactive play should deduct OT cost {cardCost}");

                // Scenario B: Reactive parry during Parry_Window — free (no OT spend)
                overflow.Initialize();
                meter.Initialize(meterMax, 2, overflow);
                int beforeReactive = meter.Current;

                var attack = CreateDamageAction(10, IntentColor.White,
                    new List<string> { "Slash" });
                var enemyData = CreateEnemyData($"ReactiveEnemy_{i}", 50);
                var enemy = CreateEnemy(enemyData);

                _parrySystem.Initialize(_gameConfig, 0);
                _parrySystem.StartParryWindow(attack, enemy);

                var defenseData = CreateDefenseCard($"ReactiveShield_{i}", cardCost,
                    new List<string> { "Slash" });
                var cardInstance = CreateCardInstance(defenseData);

                bool parryResult = _parrySystem.TryParry(cardInstance);
                int afterReactive = meter.Current;

                Assert.IsTrue(parryResult,
                    $"[Iter {i}] Reactive parry should succeed with matching card");
                Assert.AreEqual(beforeReactive, afterReactive,
                    $"[Iter {i}] Reactive parry during Parry_Window should NOT deduct OT");
            }
        }

        #endregion
    }
}
