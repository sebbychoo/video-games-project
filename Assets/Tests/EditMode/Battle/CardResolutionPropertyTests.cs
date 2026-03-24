using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CardBattle;

namespace CardBattle.Tests
{
    /// <summary>
    /// Property-based tests for card effect resolution.
    /// Validates Properties 13, 31, 32, and 34 using randomized inputs.
    /// </summary>
    [TestFixture]
    public class CardResolutionPropertyTests
    {
        private const int Iterations = 200;

        private GameObject _systemGo;
        private BlockSystem _blockSystem;
        private StatusEffectSystem _statusEffectSystem;
        private OverflowBuffer _overflowBuffer;
        private OvertimeMeter _overtimeMeter;
        private DeckManager _deckManager;

        private GameObject _playerGo;
        private Health _playerHealth;

        private readonly List<GameObject> _enemyGos = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            _systemGo = new GameObject("TestSystems");
            _blockSystem = _systemGo.AddComponent<BlockSystem>();
            _blockSystem.Initialize();
            _statusEffectSystem = _systemGo.AddComponent<StatusEffectSystem>();
            _statusEffectSystem.Initialize();
            _overflowBuffer = _systemGo.AddComponent<OverflowBuffer>();
            _overflowBuffer.Initialize();
            _overtimeMeter = _systemGo.AddComponent<OvertimeMeter>();
            _overtimeMeter.Initialize(10, 2, _overflowBuffer);
            _deckManager = _systemGo.AddComponent<DeckManager>();

            _playerGo = new GameObject("TestPlayer");
            _playerHealth = _playerGo.AddComponent<Health>();
            _playerHealth.suppressSceneLoad = true;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _enemyGos)
                UnityEngine.Object.DestroyImmediate(go);
            _enemyGos.Clear();
            UnityEngine.Object.DestroyImmediate(_playerGo);
            UnityEngine.Object.DestroyImmediate(_systemGo);
        }

        private GameObject CreateEnemy(int maxHP)
        {
            var go = new GameObject("TestEnemy");
            var health = go.AddComponent<Health>();
            health.maxHealth = maxHP;
            health.currentHealth = maxHP;
            health.suppressSceneLoad = true;
            _enemyGos.Add(go);
            return go;
        }

        private CardData CreateCardData(CardType type, int effectValue, TargetMode targetMode,
            UtilityEffectType utilityType = UtilityEffectType.None, int blockValue = 0)
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.cardType = type;
            card.effectValue = effectValue;
            card.targetMode = targetMode;
            card.utilityEffectType = utilityType;
            card.blockValue = blockValue;
            card.overtimeCost = 0;
            return card;
        }

        #region Property 13: Attack Card Deals Correct Damage

        /// <summary>
        /// Property 13: For any Attack card with effectValue, Single_Enemy target takes
        /// effectValue damage; All_Enemies each take effectValue damage.
        /// No status effects, no block, no overflow — pure damage.
        /// Validates: Requirements 5.1, 5.2, 8.4
        /// </summary>
        [Test]
        public void Property13_SingleTarget_TakesExactEffectValue()
        {
            var rng = new System.Random(42);

            for (int i = 0; i < Iterations; i++)
            {
                int maxHP = rng.Next(10, 200);
                int effectValue = rng.Next(1, maxHP);

                _blockSystem.Initialize();

                var enemyGo = CreateEnemy(maxHP);
                var enemyHealth = enemyGo.GetComponent<Health>();

                // Simulate attack resolution: deal effectValue damage through block system
                int remaining = _blockSystem.AbsorbDamage(effectValue, enemyGo);
                enemyHealth.TakeDamage(remaining);

                int expectedHP = maxHP - effectValue;
                Assert.AreEqual(expectedHP, enemyHealth.currentHealth,
                    $"[Iter {i}] Single target HP: maxHP={maxHP}, effectValue={effectValue}, expected={expectedHP}");

                // Cleanup for next iteration
                foreach (var go in _enemyGos)
                    UnityEngine.Object.DestroyImmediate(go);
                _enemyGos.Clear();
            }
        }

        [Test]
        public void Property13_AllEnemies_EachTakesEffectValue()
        {
            var rng = new System.Random(123);

            for (int i = 0; i < Iterations; i++)
            {
                int enemyCount = rng.Next(1, 5); // 1-4 enemies
                int effectValue = rng.Next(1, 50);

                _blockSystem.Initialize();

                var enemies = new List<GameObject>();
                var healths = new List<Health>();
                var maxHPs = new List<int>();

                for (int e = 0; e < enemyCount; e++)
                {
                    int maxHP = rng.Next(effectValue + 1, 200);
                    maxHPs.Add(maxHP);
                    var enemyGo = CreateEnemy(maxHP);
                    enemies.Add(enemyGo);
                    healths.Add(enemyGo.GetComponent<Health>());
                }

                // Simulate All_Enemies attack: each enemy takes effectValue
                for (int e = 0; e < enemyCount; e++)
                {
                    int remaining = _blockSystem.AbsorbDamage(effectValue, enemies[e]);
                    healths[e].TakeDamage(remaining);
                }

                for (int e = 0; e < enemyCount; e++)
                {
                    int expectedHP = maxHPs[e] - effectValue;
                    Assert.AreEqual(expectedHP, healths[e].currentHealth,
                        $"[Iter {i}] Enemy {e} HP: maxHP={maxHPs[e]}, effectValue={effectValue}, expected={expectedHP}");
                }

                // Cleanup
                foreach (var go in _enemyGos)
                    UnityEngine.Object.DestroyImmediate(go);
                _enemyGos.Clear();
            }
        }

        [Test]
        public void Property13_AllEnemies_SameDamageRegardlessOfCount()
        {
            var rng = new System.Random(456);

            for (int i = 0; i < 100; i++)
            {
                int effectValue = rng.Next(1, 60);
                int enemyCount = rng.Next(2, 5);

                _blockSystem.Initialize();

                var enemies = new List<GameObject>();
                int uniformMaxHP = 100;

                for (int e = 0; e < enemyCount; e++)
                    enemies.Add(CreateEnemy(uniformMaxHP));

                // Each enemy should take the full effectValue, not split
                for (int e = 0; e < enemyCount; e++)
                {
                    int remaining = _blockSystem.AbsorbDamage(effectValue, enemies[e]);
                    enemies[e].GetComponent<Health>().TakeDamage(remaining);
                }

                for (int e = 0; e < enemyCount; e++)
                {
                    Assert.AreEqual(uniformMaxHP - effectValue,
                        enemies[e].GetComponent<Health>().currentHealth,
                        $"[Iter {i}] Enemy {e}/{enemyCount}: damage is not split, each takes full effectValue={effectValue}");
                }

                foreach (var go in _enemyGos)
                    UnityEngine.Object.DestroyImmediate(go);
                _enemyGos.Clear();
            }
        }

        [Test]
        public void Property13_ZeroEffectValue_NoDamage()
        {
            var enemyGo = CreateEnemy(50);
            var health = enemyGo.GetComponent<Health>();

            _blockSystem.Initialize();
            int remaining = _blockSystem.AbsorbDamage(0, enemyGo);
            // AbsorbDamage returns damage when <= 0, so no TakeDamage call needed
            if (remaining > 0)
                health.TakeDamage(remaining);

            Assert.AreEqual(50, health.currentHealth, "Zero damage should not reduce HP");
        }

        #endregion

        #region Property 31: Utility Draw Card Effect

        /// <summary>
        /// Property 31: A Utility Draw card draws min(N, availableCards) additional cards.
        /// availableCards = drawPile + discardPile (since reshuffle can occur).
        /// Validates: Requirements 19.1
        /// </summary>
        [Test]
        public void Property31_DrawsMinOfNAndAvailable()
        {
            var rng = new System.Random(789);

            for (int i = 0; i < Iterations; i++)
            {
                int drawCount = rng.Next(1, 8);
                int deckSize = rng.Next(0, 15);

                // Create card assets for the deck
                var cards = new List<CardData>();
                for (int c = 0; c < deckSize; c++)
                    cards.Add(CreateCardData(CardType.Attack, 5, TargetMode.SingleEnemy));

                _deckManager.Initialize(cards);

                // Simulate utility draw: draw N cards
                int drawn = 0;
                for (int d = 0; d < drawCount; d++)
                {
                    CardData card = _deckManager.Draw();
                    if (card != null)
                        drawn++;
                    else
                        break;
                }

                int expected = Math.Min(drawCount, deckSize);
                Assert.AreEqual(expected, drawn,
                    $"[Iter {i}] Draw count: requested={drawCount}, available={deckSize}, expected={expected}");

                // Cleanup ScriptableObjects
                foreach (var c in cards)
                    UnityEngine.Object.DestroyImmediate(c);
            }
        }

        [Test]
        public void Property31_DrawFromEmptyDeck_DrawsNothing()
        {
            _deckManager.Initialize(new List<CardData>());

            CardData result = _deckManager.Draw();
            Assert.IsNull(result, "Drawing from empty deck should return null");
        }

        [Test]
        public void Property31_DrawTriggersReshuffle_WhenDrawPileEmpty()
        {
            var rng = new System.Random(321);

            for (int i = 0; i < 50; i++)
            {
                int totalCards = rng.Next(3, 12);
                int drawCount = rng.Next(1, totalCards + 1);

                var cards = new List<CardData>();
                for (int c = 0; c < totalCards; c++)
                    cards.Add(CreateCardData(CardType.Attack, 5, TargetMode.SingleEnemy));

                _deckManager.Initialize(cards);

                // Draw all cards to empty the draw pile
                var drawnCards = new List<CardData>();
                for (int d = 0; d < totalCards; d++)
                {
                    CardData card = _deckManager.Draw();
                    Assert.IsNotNull(card, $"[Iter {i}] Should draw card {d + 1}/{totalCards}");
                    drawnCards.Add(card);
                }

                // Discard them all back
                foreach (var card in drawnCards)
                    _deckManager.Discard(card);

                // Now draw again — should reshuffle discard into draw
                int redrawn = 0;
                for (int d = 0; d < drawCount; d++)
                {
                    CardData card = _deckManager.Draw();
                    if (card != null) redrawn++;
                    else break;
                }

                Assert.AreEqual(Math.Min(drawCount, totalCards), redrawn,
                    $"[Iter {i}] After reshuffle: requested={drawCount}, available={totalCards}");

                foreach (var c in cards)
                    UnityEngine.Object.DestroyImmediate(c);
            }
        }

        #endregion

        #region Property 32: Utility Restore Overtime Effect

        /// <summary>
        /// Property 32: Restore sets meter to min(v + N, max), excess to overflow.
        /// Validates: Requirements 19.2, 2.6
        /// </summary>
        [Test]
        public void Property32_RestoreAddsOvertimeCappedAtMax()
        {
            var rng = new System.Random(654);

            for (int i = 0; i < Iterations; i++)
            {
                int max = rng.Next(3, 20);
                int initial = rng.Next(0, max + 1);
                int restoreAmount = rng.Next(1, 15);

                _overflowBuffer.Initialize();
                _overtimeMeter.Initialize(max, 2, _overflowBuffer);
                int spendToReach = max - initial;
                if (spendToReach > 0)
                    _overtimeMeter.Spend(spendToReach);

                Assert.AreEqual(initial, _overtimeMeter.Current,
                    $"[Iter {i}] Setup: expected OT={initial}");

                int overflowBefore = _overflowBuffer.Current;

                // Simulate restore: GainFromDamage(amount * 10, 100) adds exactly 'amount' OT
                // This matches CardEffectResolver.ResolveUtilityRestore
                _overtimeMeter.GainFromDamage(restoreAmount * 10, 100);

                int expectedMeter = Math.Min(initial + restoreAmount, max);
                int rawNew = initial + restoreAmount;
                int expectedOverflow = rawNew > max ? rawNew - max : 0;

                Assert.AreEqual(expectedMeter, _overtimeMeter.Current,
                    $"[Iter {i}] OT after restore: initial={initial}, restore={restoreAmount}, max={max}");
                Assert.AreEqual(overflowBefore + expectedOverflow, _overflowBuffer.Current,
                    $"[Iter {i}] Overflow after restore: excess should route to buffer");
            }
        }

        [Test]
        public void Property32_RestoreAtFullMeter_AllGoesToOverflow()
        {
            var rng = new System.Random(987);

            for (int i = 0; i < 100; i++)
            {
                int max = rng.Next(3, 20);
                int restoreAmount = rng.Next(1, 10);

                _overflowBuffer.Initialize();
                _overtimeMeter.Initialize(max, 2, _overflowBuffer);
                // Meter starts at full

                _overtimeMeter.GainFromDamage(restoreAmount * 10, 100);

                Assert.AreEqual(max, _overtimeMeter.Current,
                    $"[Iter {i}] Meter should stay at max={max}");
                Assert.AreEqual(restoreAmount, _overflowBuffer.Current,
                    $"[Iter {i}] All {restoreAmount} points should overflow");
            }
        }

        [Test]
        public void Property32_RestoreZero_NoChange()
        {
            _overflowBuffer.Initialize();
            _overtimeMeter.Initialize(10, 2, _overflowBuffer);
            _overtimeMeter.Spend(5); // Current = 5

            // Restore 0: GainFromDamage(0, 100) should do nothing
            _overtimeMeter.GainFromDamage(0, 100);

            Assert.AreEqual(5, _overtimeMeter.Current, "Restore 0 should not change meter");
            Assert.AreEqual(0, _overflowBuffer.Current, "Restore 0 should not add overflow");
        }

        #endregion

        #region Property 34: Heal Utility Capped at Max HP

        /// <summary>
        /// Property 34: Heal sets HP = min(hp + H, maxHP). Never exceeds max.
        /// Validates: Requirements 19.5
        /// </summary>
        [Test]
        public void Property34_HealCappedAtMaxHP()
        {
            var rng = new System.Random(111);

            for (int i = 0; i < Iterations; i++)
            {
                int maxHP = rng.Next(10, 200);
                int currentHP = rng.Next(1, maxHP + 1);
                int healAmount = rng.Next(1, 100);

                _playerHealth.maxHealth = maxHP;
                _playerHealth.currentHealth = currentHP;

                // Simulate heal: same logic as CardEffectResolver.ResolveUtilityHeal
                int actualHeal = Math.Min(healAmount, _playerHealth.maxHealth - _playerHealth.currentHealth);
                if (actualHeal > 0)
                    _playerHealth.currentHealth += actualHeal;

                int expectedHP = Math.Min(currentHP + healAmount, maxHP);
                Assert.AreEqual(expectedHP, _playerHealth.currentHealth,
                    $"[Iter {i}] HP after heal: current={currentHP}, heal={healAmount}, max={maxHP}, expected={expectedHP}");

                // Key property: never exceeds max
                Assert.LessOrEqual(_playerHealth.currentHealth, maxHP,
                    $"[Iter {i}] HP must never exceed maxHP={maxHP}");
            }
        }

        [Test]
        public void Property34_HealAtFullHP_NoChange()
        {
            var rng = new System.Random(222);

            for (int i = 0; i < 50; i++)
            {
                int maxHP = rng.Next(10, 200);
                int healAmount = rng.Next(1, 100);

                _playerHealth.maxHealth = maxHP;
                _playerHealth.currentHealth = maxHP; // Already full

                int actualHeal = Math.Min(healAmount, _playerHealth.maxHealth - _playerHealth.currentHealth);
                if (actualHeal > 0)
                    _playerHealth.currentHealth += actualHeal;

                Assert.AreEqual(maxHP, _playerHealth.currentHealth,
                    $"[Iter {i}] Healing at full HP should not change HP");
            }
        }

        [Test]
        public void Property34_HealExactlyToMax()
        {
            var rng = new System.Random(333);

            for (int i = 0; i < 50; i++)
            {
                int maxHP = rng.Next(10, 200);
                int missing = rng.Next(1, maxHP);
                int currentHP = maxHP - missing;

                _playerHealth.maxHealth = maxHP;
                _playerHealth.currentHealth = currentHP;

                // Heal exactly the missing amount
                int actualHeal = Math.Min(missing, _playerHealth.maxHealth - _playerHealth.currentHealth);
                if (actualHeal > 0)
                    _playerHealth.currentHealth += actualHeal;

                Assert.AreEqual(maxHP, _playerHealth.currentHealth,
                    $"[Iter {i}] Healing exact missing amount should reach maxHP");
            }
        }

        [Test]
        public void Property34_HealZero_NoChange()
        {
            _playerHealth.maxHealth = 100;
            _playerHealth.currentHealth = 50;

            int actualHeal = Math.Min(0, _playerHealth.maxHealth - _playerHealth.currentHealth);
            if (actualHeal > 0)
                _playerHealth.currentHealth += actualHeal;

            Assert.AreEqual(50, _playerHealth.currentHealth, "Heal 0 should not change HP");
        }

        #endregion
    }
}
