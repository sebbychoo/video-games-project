using NUnit.Framework;
using UnityEngine;
using CardBattle;

namespace CardBattle.Tests
{
    /// <summary>
    /// Property-based tests for TurnPhaseController.
    /// Property 4: Turn Phase Ordering — Phase cycles Draw → Play → Discard → Enemy,
    /// first phase is Draw, Stun skips Play.
    /// Validates: Requirements 1.2, 1.5, 1.6, 1.7, 10.7
    /// </summary>
    [TestFixture]
    public class TurnPhasePropertyTests
    {
        private const int Iterations = 200;

        private static readonly TurnPhase[] NormalCycle =
            { TurnPhase.Draw, TurnPhase.Play, TurnPhase.Discard, TurnPhase.Enemy };

        private static readonly TurnPhase[] StunnedCycle =
            { TurnPhase.Draw, TurnPhase.Discard, TurnPhase.Enemy };

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

        #region Property 4: Turn Phase Ordering

        /// <summary>
        /// Property 4a: For any number of complete turns N, the phase sequence is
        /// strictly Draw → Play → Discard → Enemy repeated N times, and the first
        /// phase after Initialize is always Draw (player acts first).
        /// Validates: Requirements 1.2, 1.5, 1.6, 1.7
        /// </summary>
        [Test]
        public void Property4_NormalCycle_StrictOrdering_AcrossRandomTurns()
        {
            var rng = new System.Random(42);

            for (int i = 0; i < Iterations; i++)
            {
                _ses.Initialize();
                _controller.Initialize(_ses, _player);

                int numTurns = rng.Next(1, 20);

                // First phase must always be Draw (Req 1.8 — player acts first)
                Assert.AreEqual(TurnPhase.Draw, _controller.CurrentPhase,
                    $"[Iter {i}] First phase must be Draw");
                Assert.AreEqual(1, _controller.TurnNumber,
                    $"[Iter {i}] First turn must be 1");

                for (int t = 0; t < numTurns; t++)
                {
                    // At the start of each turn we should be at Draw
                    Assert.AreEqual(TurnPhase.Draw, _controller.CurrentPhase,
                        $"[Iter {i}, Turn {t + 1}] Turn should start at Draw");

                    _controller.AdvancePhase();
                    Assert.AreEqual(TurnPhase.Play, _controller.CurrentPhase,
                        $"[Iter {i}, Turn {t + 1}] After Draw should be Play");

                    _controller.AdvancePhase();
                    Assert.AreEqual(TurnPhase.Discard, _controller.CurrentPhase,
                        $"[Iter {i}, Turn {t + 1}] After Play should be Discard");

                    _controller.AdvancePhase();
                    Assert.AreEqual(TurnPhase.Enemy, _controller.CurrentPhase,
                        $"[Iter {i}, Turn {t + 1}] After Discard should be Enemy");

                    _controller.AdvancePhase(); // Enemy → Draw (next turn)
                }

                // Turn number should have incremented by numTurns
                Assert.AreEqual(numTurns + 1, _controller.TurnNumber,
                    $"[Iter {i}] After {numTurns} complete turns, TurnNumber should be {numTurns + 1}");
            }
        }

        /// <summary>
        /// Property 4b: For any turn where the player is stunned, the phase sequence
        /// is Draw → Discard → Enemy (Play is skipped).
        /// Validates: Requirements 1.2, 1.5, 1.6, 10.7
        /// </summary>
        [Test]
        public void Property4_StunnedCycle_SkipsPlay_AcrossRandomTurns()
        {
            var rng = new System.Random(99);

            for (int i = 0; i < Iterations; i++)
            {
                _ses.Initialize();
                _controller.Initialize(_ses, _player);

                int numTurns = rng.Next(1, 15);
                // Apply stun with enough duration to last all turns
                _ses.Apply(_player, new StatusEffectInstance
                {
                    effectId = StatusEffectSystem.Stun,
                    duration = numTurns + 5,
                    value = 0
                });

                for (int t = 0; t < numTurns; t++)
                {
                    Assert.AreEqual(TurnPhase.Draw, _controller.CurrentPhase,
                        $"[Iter {i}, Turn {t + 1}] Stunned turn should start at Draw");

                    _controller.AdvancePhase();
                    // Play should be skipped — go directly to Discard
                    Assert.AreEqual(TurnPhase.Discard, _controller.CurrentPhase,
                        $"[Iter {i}, Turn {t + 1}] Stunned: after Draw should skip Play → Discard");

                    _controller.AdvancePhase();
                    Assert.AreEqual(TurnPhase.Enemy, _controller.CurrentPhase,
                        $"[Iter {i}, Turn {t + 1}] After Discard should be Enemy");

                    _controller.AdvancePhase(); // Enemy → Draw (next turn)
                }

                Assert.AreEqual(numTurns + 1, _controller.TurnNumber,
                    $"[Iter {i}] Turn number should still increment correctly when stunned");
            }
        }

        /// <summary>
        /// Property 4c: For any random sequence of stunned and non-stunned turns,
        /// the phase ordering is correct per-turn: normal cycle when not stunned,
        /// skipped Play when stunned.
        /// Validates: Requirements 1.2, 1.5, 1.6, 1.7, 10.7
        /// </summary>
        [Test]
        public void Property4_MixedStunAndNormal_CorrectPerTurnOrdering()
        {
            var rng = new System.Random(777);

            for (int i = 0; i < Iterations; i++)
            {
                _ses.Initialize();
                _controller.Initialize(_ses, _player);

                int numTurns = rng.Next(2, 15);

                for (int t = 0; t < numTurns; t++)
                {
                    bool stunThisTurn = rng.Next(2) == 0;

                    // Apply or clear stun before the Draw → Play transition
                    if (stunThisTurn)
                    {
                        _ses.Apply(_player, new StatusEffectInstance
                        {
                            effectId = StatusEffectSystem.Stun,
                            duration = 2,
                            value = 0
                        });
                    }
                    else
                    {
                        _ses.Remove(_player, StatusEffectSystem.Stun);
                    }

                    Assert.AreEqual(TurnPhase.Draw, _controller.CurrentPhase,
                        $"[Iter {i}, Turn {t + 1}] Should start at Draw");

                    _controller.AdvancePhase();

                    if (stunThisTurn)
                    {
                        Assert.AreEqual(TurnPhase.Discard, _controller.CurrentPhase,
                            $"[Iter {i}, Turn {t + 1}] Stunned: should skip Play → Discard");
                    }
                    else
                    {
                        Assert.AreEqual(TurnPhase.Play, _controller.CurrentPhase,
                            $"[Iter {i}, Turn {t + 1}] Not stunned: should go to Play");
                        _controller.AdvancePhase(); // Play → Discard
                        Assert.AreEqual(TurnPhase.Discard, _controller.CurrentPhase,
                            $"[Iter {i}, Turn {t + 1}] After Play should be Discard");
                    }

                    _controller.AdvancePhase(); // Discard → Enemy
                    Assert.AreEqual(TurnPhase.Enemy, _controller.CurrentPhase,
                        $"[Iter {i}, Turn {t + 1}] After Discard should be Enemy");

                    _controller.AdvancePhase(); // Enemy → Draw (next turn)
                }
            }
        }

        /// <summary>
        /// Property 4d: The first phase of the first turn is always Draw,
        /// regardless of initial conditions. Player always acts before enemies on turn 1.
        /// Validates: Requirements 1.7
        /// </summary>
        [Test]
        public void Property4_FirstPhaseAlwaysDraw_PlayerActsFirst()
        {
            var rng = new System.Random(123);

            for (int i = 0; i < Iterations; i++)
            {
                _ses.Initialize();

                // Randomly decide if player starts stunned
                bool startStunned = rng.Next(2) == 0;
                if (startStunned)
                {
                    _ses.Apply(_player, new StatusEffectInstance
                    {
                        effectId = StatusEffectSystem.Stun,
                        duration = rng.Next(1, 5),
                        value = 0
                    });
                }

                _controller.Initialize(_ses, _player);

                // Regardless of stun, first phase is Draw (player acts first)
                Assert.AreEqual(TurnPhase.Draw, _controller.CurrentPhase,
                    $"[Iter {i}] First phase must be Draw even when stunned={startStunned}");
                Assert.AreEqual(1, _controller.TurnNumber,
                    $"[Iter {i}] First turn must be 1");
            }
        }

        /// <summary>
        /// Property 4e: Turn number increments exactly once per complete cycle,
        /// regardless of whether Play was skipped due to stun.
        /// Validates: Requirements 1.7
        /// </summary>
        [Test]
        public void Property4_TurnNumberIncrementsOncePerCycle()
        {
            var rng = new System.Random(456);

            for (int i = 0; i < Iterations; i++)
            {
                _ses.Initialize();
                _controller.Initialize(_ses, _player);

                int numTurns = rng.Next(1, 25);

                for (int t = 0; t < numTurns; t++)
                {
                    int expectedTurn = t + 1;
                    Assert.AreEqual(expectedTurn, _controller.TurnNumber,
                        $"[Iter {i}, Turn {t + 1}] TurnNumber should be {expectedTurn}");

                    bool stunned = rng.Next(3) == 0; // ~33% chance stunned
                    if (stunned)
                        _ses.Apply(_player, new StatusEffectInstance
                        {
                            effectId = StatusEffectSystem.Stun,
                            duration = 2,
                            value = 0
                        });
                    else
                        _ses.Remove(_player, StatusEffectSystem.Stun);

                    // Advance through the full cycle
                    _controller.AdvancePhase(); // Draw → Play or Discard
                    if (!stunned)
                        _controller.AdvancePhase(); // Play → Discard
                    _controller.AdvancePhase(); // Discard → Enemy
                    _controller.AdvancePhase(); // Enemy → Draw (next turn)
                }

                Assert.AreEqual(numTurns + 1, _controller.TurnNumber,
                    $"[Iter {i}] Final TurnNumber should be {numTurns + 1}");
            }
        }

        /// <summary>
        /// Property 4f: OnPhaseChanged event fires for every phase transition,
        /// and the reported phase matches CurrentPhase.
        /// Validates: Requirements 1.2
        /// </summary>
        [Test]
        public void Property4_OnPhaseChangedEvent_MatchesCurrentPhase()
        {
            var rng = new System.Random(321);

            for (int i = 0; i < 50; i++)
            {
                _ses.Initialize();
                _controller.Initialize(_ses, _player);

                var reportedPhases = new System.Collections.Generic.List<TurnPhase>();
                _controller.OnPhaseChanged += phase => reportedPhases.Add(phase);

                int numTurns = rng.Next(1, 10);
                bool stunned = rng.Next(2) == 0;

                if (stunned)
                    _ses.Apply(_player, new StatusEffectInstance
                    {
                        effectId = StatusEffectSystem.Stun,
                        duration = numTurns + 5,
                        value = 0
                    });

                for (int t = 0; t < numTurns; t++)
                {
                    reportedPhases.Clear();

                    if (stunned)
                    {
                        // Draw → Discard → Enemy → Draw
                        _controller.AdvancePhase();
                        _controller.AdvancePhase();
                        _controller.AdvancePhase();

                        Assert.AreEqual(3, reportedPhases.Count,
                            $"[Iter {i}, Turn {t + 1}] Stunned cycle should fire 3 events");
                        Assert.AreEqual(TurnPhase.Discard, reportedPhases[0]);
                        Assert.AreEqual(TurnPhase.Enemy, reportedPhases[1]);
                        Assert.AreEqual(TurnPhase.Draw, reportedPhases[2]);
                    }
                    else
                    {
                        // Draw → Play → Discard → Enemy → Draw
                        _controller.AdvancePhase();
                        _controller.AdvancePhase();
                        _controller.AdvancePhase();
                        _controller.AdvancePhase();

                        Assert.AreEqual(4, reportedPhases.Count,
                            $"[Iter {i}, Turn {t + 1}] Normal cycle should fire 4 events");
                        Assert.AreEqual(TurnPhase.Play, reportedPhases[0]);
                        Assert.AreEqual(TurnPhase.Discard, reportedPhases[1]);
                        Assert.AreEqual(TurnPhase.Enemy, reportedPhases[2]);
                        Assert.AreEqual(TurnPhase.Draw, reportedPhases[3]);
                    }
                }

                // Unsubscribe to avoid leaking across iterations
                _controller.OnPhaseChanged -= phase => reportedPhases.Add(phase);
            }
        }

        #endregion
    }
}
