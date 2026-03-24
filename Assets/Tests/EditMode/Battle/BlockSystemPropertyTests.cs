using System;
using NUnit.Framework;
using UnityEngine;
using CardBattle;

namespace CardBattle.Tests
{
    /// <summary>
    /// Property-based tests for BlockSystem.
    /// Uses randomized inputs across many iterations to verify correctness properties.
    /// </summary>
    [TestFixture]
    public class BlockSystemPropertyTests
    {
        private const int Iterations = 200;

        private GameObject _go;
        private BlockSystem _blockSystem;
        private GameObject _target;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("TestBlockSystem");
            _blockSystem = _go.AddComponent<BlockSystem>();
            _blockSystem.Initialize();
            _target = new GameObject("TestTarget");
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_target);
            UnityEngine.Object.DestroyImmediate(_go);
        }

        #region Property 10: Block Absorbs Damage Before HP

        /// <summary>
        /// Property 10: For any damage d, block b, and HP hp:
        ///   new Block = max(0, b - d)
        ///   remaining damage to HP = max(0, d - b)
        ///   new HP = hp - max(0, d - b)
        /// Validates: Requirements 6.1, 6.2, 6.3, 6.5, 9.2
        /// </summary>
        [Test]
        public void Property10_BlockAbsorbsDamageBeforeHP()
        {
            var rng = new System.Random(42);

            for (int i = 0; i < Iterations; i++)
            {
                int block = rng.Next(0, 50);
                int damage = rng.Next(0, 80);
                int hp = rng.Next(1, 200);

                _blockSystem.Initialize();
                if (block > 0)
                    _blockSystem.AddBlock(block, _target);

                Assert.AreEqual(block, _blockSystem.GetBlock(_target),
                    $"[Iter {i}] Setup: expected Block={block}");

                int remainingDamage = _blockSystem.AbsorbDamage(damage, _target);
                int newBlock = _blockSystem.GetBlock(_target);
                int newHP = hp - remainingDamage;

                int expectedBlock = Math.Max(0, block - damage);
                int expectedRemainingDmg = Math.Max(0, damage - block);
                int expectedHP = hp - expectedRemainingDmg;

                Assert.AreEqual(expectedBlock, newBlock,
                    $"[Iter {i}] Block after absorb: block={block}, damage={damage}, expected={expectedBlock}");
                Assert.AreEqual(expectedRemainingDmg, remainingDamage,
                    $"[Iter {i}] Remaining damage: block={block}, damage={damage}, expected={expectedRemainingDmg}");
                Assert.AreEqual(expectedHP, newHP,
                    $"[Iter {i}] HP after absorb: hp={hp}, remainingDmg={expectedRemainingDmg}, expected={expectedHP}");
            }
        }

        [Test]
        public void Property10_ZeroBlock_AllDamagePassesThrough()
        {
            var rng = new System.Random(99);

            for (int i = 0; i < 50; i++)
            {
                int damage = rng.Next(1, 100);
                int hp = rng.Next(damage, 300);

                _blockSystem.Initialize();
                // No block added — block is 0

                int remaining = _blockSystem.AbsorbDamage(damage, _target);

                Assert.AreEqual(damage, remaining,
                    $"[Iter {i}] With zero block, all damage should pass through");
                Assert.AreEqual(0, _blockSystem.GetBlock(_target),
                    $"[Iter {i}] Block should remain 0");
            }
        }

        [Test]
        public void Property10_BlockExceedsDamage_NoDamageToHP()
        {
            var rng = new System.Random(77);

            for (int i = 0; i < 50; i++)
            {
                int damage = rng.Next(1, 50);
                int block = damage + rng.Next(0, 50); // block >= damage

                _blockSystem.Initialize();
                _blockSystem.AddBlock(block, _target);

                int remaining = _blockSystem.AbsorbDamage(damage, _target);

                Assert.AreEqual(0, remaining,
                    $"[Iter {i}] No damage should pass through when block({block}) >= damage({damage})");
                Assert.AreEqual(block - damage, _blockSystem.GetBlock(_target),
                    $"[Iter {i}] Block should decrease by damage amount");
            }
        }

        [Test]
        public void Property10_AppliesToEnemiesIdentically()
        {
            var rng = new System.Random(55);
            var enemy = new GameObject("TestEnemy");

            try
            {
                for (int i = 0; i < Iterations; i++)
                {
                    int block = rng.Next(0, 50);
                    int damage = rng.Next(0, 80);

                    _blockSystem.Initialize();
                    if (block > 0)
                        _blockSystem.AddBlock(block, enemy);

                    int remaining = _blockSystem.AbsorbDamage(damage, enemy);
                    int newBlock = _blockSystem.GetBlock(enemy);

                    int expectedBlock = Math.Max(0, block - damage);
                    int expectedRemaining = Math.Max(0, damage - block);

                    Assert.AreEqual(expectedBlock, newBlock,
                        $"[Iter {i}] Enemy block after absorb: block={block}, damage={damage}");
                    Assert.AreEqual(expectedRemaining, remaining,
                        $"[Iter {i}] Enemy remaining damage: block={block}, damage={damage}");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemy);
            }
        }

        #endregion

        #region Property 11: Block Resets Each Turn

        /// <summary>
        /// Property 11: Block resets to 0 at the start of a new player turn,
        /// regardless of its previous value.
        /// Validates: Requirements 6.4
        /// </summary>
        [Test]
        public void Property11_BlockResetsToZeroOnNewTurn()
        {
            var rng = new System.Random(123);

            for (int i = 0; i < Iterations; i++)
            {
                int block = rng.Next(0, 100);

                _blockSystem.Initialize();
                if (block > 0)
                    _blockSystem.AddBlock(block, _target);

                Assert.AreEqual(block, _blockSystem.GetBlock(_target),
                    $"[Iter {i}] Setup: expected Block={block}");

                // Simulate new turn: reset block
                _blockSystem.Reset(_target);

                Assert.AreEqual(0, _blockSystem.GetBlock(_target),
                    $"[Iter {i}] Block should be 0 after reset, was {block} before");
            }
        }

        [Test]
        public void Property11_ResetOnlyAffectsSpecifiedTarget()
        {
            var rng = new System.Random(456);
            var enemy = new GameObject("TestEnemy");

            try
            {
                for (int i = 0; i < 50; i++)
                {
                    int playerBlock = rng.Next(1, 50);
                    int enemyBlock = rng.Next(1, 50);

                    _blockSystem.Initialize();
                    _blockSystem.AddBlock(playerBlock, _target);
                    _blockSystem.AddBlock(enemyBlock, enemy);

                    // Reset only the player's block
                    _blockSystem.Reset(_target);

                    Assert.AreEqual(0, _blockSystem.GetBlock(_target),
                        $"[Iter {i}] Player block should be 0 after reset");
                    Assert.AreEqual(enemyBlock, _blockSystem.GetBlock(enemy),
                        $"[Iter {i}] Enemy block should be unchanged after player reset");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemy);
            }
        }

        [Test]
        public void Property11_DoubleResetIsIdempotent()
        {
            _blockSystem.Initialize();
            _blockSystem.AddBlock(25, _target);

            _blockSystem.Reset(_target);
            Assert.AreEqual(0, _blockSystem.GetBlock(_target));

            // Second reset should also leave it at 0
            _blockSystem.Reset(_target);
            Assert.AreEqual(0, _blockSystem.GetBlock(_target),
                "Double reset should still be 0");
        }

        #endregion

        #region Property 12: Defense Card Adds Block

        /// <summary>
        /// Property 12: Playing a Defense card with blockValue sets Block to b + blockValue.
        /// Validates: Requirements 5.3
        /// </summary>
        [Test]
        public void Property12_DefenseCardAddsBlock()
        {
            var rng = new System.Random(789);

            for (int i = 0; i < Iterations; i++)
            {
                int existingBlock = rng.Next(0, 50);
                int blockValue = rng.Next(1, 30);

                _blockSystem.Initialize();
                if (existingBlock > 0)
                    _blockSystem.AddBlock(existingBlock, _target);

                // Simulate playing a Defense card: add blockValue
                _blockSystem.AddBlock(blockValue, _target);

                int expected = existingBlock + blockValue;
                Assert.AreEqual(expected, _blockSystem.GetBlock(_target),
                    $"[Iter {i}] Block after Defense card: existing={existingBlock}, blockValue={blockValue}, expected={expected}");
            }
        }

        [Test]
        public void Property12_MultipleDefenseCardsStack()
        {
            var rng = new System.Random(321);

            for (int i = 0; i < 50; i++)
            {
                int numCards = rng.Next(1, 6);
                int totalExpected = 0;

                _blockSystem.Initialize();

                for (int c = 0; c < numCards; c++)
                {
                    int blockValue = rng.Next(1, 20);
                    totalExpected += blockValue;
                    _blockSystem.AddBlock(blockValue, _target);
                }

                Assert.AreEqual(totalExpected, _blockSystem.GetBlock(_target),
                    $"[Iter {i}] Block after {numCards} Defense cards should be sum of all blockValues");
            }
        }

        [Test]
        public void Property12_DefenseCardOnlyAffectsTarget()
        {
            var rng = new System.Random(654);
            var enemy = new GameObject("TestEnemy");

            try
            {
                for (int i = 0; i < 50; i++)
                {
                    int blockValue = rng.Next(1, 30);

                    _blockSystem.Initialize();

                    // Add block to player only
                    _blockSystem.AddBlock(blockValue, _target);

                    Assert.AreEqual(blockValue, _blockSystem.GetBlock(_target),
                        $"[Iter {i}] Player should have block={blockValue}");
                    Assert.AreEqual(0, _blockSystem.GetBlock(enemy),
                        $"[Iter {i}] Enemy should have no block");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemy);
            }
        }

        #endregion
    }
}
