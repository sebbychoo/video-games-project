using NUnit.Framework;
using UnityEngine;
using CardBattle;

namespace CardBattle.Tests
{
    /// <summary>
    /// Unit tests for TurnPhaseController.
    /// Validates the Draw → Play → Discard → Enemy cycle,
    /// player-first on turn 1, stun skipping Play_Phase, and event firing.
    /// </summary>
    [TestFixture]
    public class TurnPhaseControllerTests
    {
        private GameObject _controllerGo;
        private TurnPhaseController _controller;
        private GameObject _sesGo;
        private StatusEffectSystem _ses;
        private GameObject _player;

        [SetUp]
        public void SetUp()
        {
            _controllerGo = new GameObject("TurnPhaseController");
            _controller = _controllerGo.AddComponent<TurnPhaseController>();

            _sesGo = new GameObject("StatusEffectSystem");
            _ses = _sesGo.AddComponent<StatusEffectSystem>();
            _ses.Initialize();

            _player = new GameObject("Player");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_player);
            Object.DestroyImmediate(_sesGo);
            Object.DestroyImmediate(_controllerGo);
        }

        // --- Requirement 1.8: Player always acts first on turn 1 ---

        [Test]
        public void Initialize_SetsPhaseToDrawAndTurnTo1()
        {
            _controller.Initialize(_ses, _player);

            Assert.AreEqual(TurnPhase.Draw, _controller.CurrentPhase);
            Assert.AreEqual(1, _controller.TurnNumber);
        }

        // --- Requirements 1.2, 1.5, 1.6, 1.7: Strict phase cycle ---

        [Test]
        public void AdvancePhase_DrawToPlay()
        {
            _controller.Initialize(_ses, _player);
            Assert.AreEqual(TurnPhase.Draw, _controller.CurrentPhase);

            _controller.AdvancePhase();
            Assert.AreEqual(TurnPhase.Play, _controller.CurrentPhase);
        }

        [Test]
        public void AdvancePhase_PlayToDiscard()
        {
            _controller.Initialize(_ses, _player);
            _controller.AdvancePhase(); // Draw → Play

            _controller.AdvancePhase(); // Play → Discard
            Assert.AreEqual(TurnPhase.Discard, _controller.CurrentPhase);
        }

        [Test]
        public void AdvancePhase_DiscardToEnemy()
        {
            _controller.Initialize(_ses, _player);
            _controller.AdvancePhase(); // Draw → Play
            _controller.AdvancePhase(); // Play → Discard

            _controller.AdvancePhase(); // Discard → Enemy
            Assert.AreEqual(TurnPhase.Enemy, _controller.CurrentPhase);
        }

        [Test]
        public void AdvancePhase_EnemyToDrawIncrementsTurn()
        {
            _controller.Initialize(_ses, _player);
            _controller.AdvancePhase(); // Draw → Play
            _controller.AdvancePhase(); // Play → Discard
            _controller.AdvancePhase(); // Discard → Enemy

            Assert.AreEqual(1, _controller.TurnNumber);
            _controller.AdvancePhase(); // Enemy → Draw (turn 2)
            Assert.AreEqual(TurnPhase.Draw, _controller.CurrentPhase);
            Assert.AreEqual(2, _controller.TurnNumber);
        }

        [Test]
        public void FullCycle_DrawPlayDiscardEnemyDraw()
        {
            _controller.Initialize(_ses, _player);

            var expected = new[]
            {
                TurnPhase.Draw,  // initial
                TurnPhase.Play,
                TurnPhase.Discard,
                TurnPhase.Enemy,
                TurnPhase.Draw   // turn 2
            };

            Assert.AreEqual(expected[0], _controller.CurrentPhase);
            for (int i = 1; i < expected.Length; i++)
            {
                _controller.AdvancePhase();
                Assert.AreEqual(expected[i], _controller.CurrentPhase,
                    $"Phase mismatch at step {i}");
            }
        }

        // --- Requirement 10.7: Skip Play_Phase when player is stunned ---

        [Test]
        public void AdvancePhase_SkipsPlayWhenStunned()
        {
            _controller.Initialize(_ses, _player);
            Assert.AreEqual(TurnPhase.Draw, _controller.CurrentPhase);

            // Apply stun to player
            _ses.Apply(_player, new StatusEffectInstance
            {
                effectId = StatusEffectSystem.Stun,
                duration = 2,
                value = 0
            });

            _controller.AdvancePhase(); // Draw → should skip Play → Discard
            Assert.AreEqual(TurnPhase.Discard, _controller.CurrentPhase);
        }

        [Test]
        public void AdvancePhase_StunnedCycle_DrawDiscardEnemy()
        {
            _controller.Initialize(_ses, _player);

            _ses.Apply(_player, new StatusEffectInstance
            {
                effectId = StatusEffectSystem.Stun,
                duration = 3,
                value = 0
            });

            _controller.AdvancePhase(); // Draw → Discard (skip Play)
            Assert.AreEqual(TurnPhase.Discard, _controller.CurrentPhase);

            _controller.AdvancePhase(); // Discard → Enemy
            Assert.AreEqual(TurnPhase.Enemy, _controller.CurrentPhase);

            _controller.AdvancePhase(); // Enemy → Draw (turn 2)
            Assert.AreEqual(TurnPhase.Draw, _controller.CurrentPhase);
            Assert.AreEqual(2, _controller.TurnNumber);
        }

        [Test]
        public void AdvancePhase_NotStunned_DoesNotSkipPlay()
        {
            _controller.Initialize(_ses, _player);

            // No stun applied
            _controller.AdvancePhase(); // Draw → Play
            Assert.AreEqual(TurnPhase.Play, _controller.CurrentPhase);
        }

        [Test]
        public void AdvancePhase_StunExpires_PlayNotSkipped()
        {
            _controller.Initialize(_ses, _player);

            // Apply stun with duration 1
            _ses.Apply(_player, new StatusEffectInstance
            {
                effectId = StatusEffectSystem.Stun,
                duration = 1,
                value = 0
            });

            // Turn 1: stunned → skip Play
            _controller.AdvancePhase(); // Draw → Discard
            Assert.AreEqual(TurnPhase.Discard, _controller.CurrentPhase);

            _controller.AdvancePhase(); // Discard → Enemy
            _controller.AdvancePhase(); // Enemy → Draw (turn 2)

            // Tick the stun so it expires
            _ses.Tick(_player);

            // Turn 2: no longer stunned → Play not skipped
            _controller.AdvancePhase(); // Draw → Play
            Assert.AreEqual(TurnPhase.Play, _controller.CurrentPhase);
        }

        // --- OnPhaseChanged event ---

        [Test]
        public void OnPhaseChanged_FiredOnInitialize()
        {
            TurnPhase? received = null;
            _controller.OnPhaseChanged += phase => received = phase;

            _controller.Initialize(_ses, _player);

            Assert.IsNotNull(received);
            Assert.AreEqual(TurnPhase.Draw, received.Value);
        }

        [Test]
        public void OnPhaseChanged_FiredOnEachAdvance()
        {
            _controller.Initialize(_ses, _player);

            var phases = new System.Collections.Generic.List<TurnPhase>();
            _controller.OnPhaseChanged += phase => phases.Add(phase);

            _controller.AdvancePhase(); // Play
            _controller.AdvancePhase(); // Discard
            _controller.AdvancePhase(); // Enemy
            _controller.AdvancePhase(); // Draw (turn 2)

            Assert.AreEqual(4, phases.Count);
            Assert.AreEqual(TurnPhase.Play, phases[0]);
            Assert.AreEqual(TurnPhase.Discard, phases[1]);
            Assert.AreEqual(TurnPhase.Enemy, phases[2]);
            Assert.AreEqual(TurnPhase.Draw, phases[3]);
        }

        // --- Multiple turns ---

        [Test]
        public void MultipleTurns_TurnNumberIncrementsCorrectly()
        {
            _controller.Initialize(_ses, _player);
            Assert.AreEqual(1, _controller.TurnNumber);

            // Complete turn 1
            _controller.AdvancePhase(); // Play
            _controller.AdvancePhase(); // Discard
            _controller.AdvancePhase(); // Enemy
            _controller.AdvancePhase(); // Draw (turn 2)
            Assert.AreEqual(2, _controller.TurnNumber);

            // Complete turn 2
            _controller.AdvancePhase(); // Play
            _controller.AdvancePhase(); // Discard
            _controller.AdvancePhase(); // Enemy
            _controller.AdvancePhase(); // Draw (turn 3)
            Assert.AreEqual(3, _controller.TurnNumber);
        }

        // --- Edge: no StatusEffectSystem or playerObject ---

        [Test]
        public void AdvancePhase_NullDependencies_DoesNotSkipPlay()
        {
            _controller.Initialize(null, null);

            _controller.AdvancePhase(); // Draw → Play (no stun check possible)
            Assert.AreEqual(TurnPhase.Play, _controller.CurrentPhase);
        }
    }
}
