using System;
using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// Manages the Draw → Play → Discard → Enemy turn phase state machine.
    /// Raises TurnPhaseChangedEvent on the BattleEventBus when the phase changes.
    /// Skips Play_Phase when the player is stunned (Draw → Discard → Enemy).
    /// Player always acts first on turn 1 (first phase is Draw).
    /// </summary>
    public class TurnPhaseController : MonoBehaviour
    {
        [SerializeField] private StatusEffectSystem statusEffectSystem;
        [SerializeField] private GameObject playerObject;

        /// <summary>The current turn phase.</summary>
        public TurnPhase CurrentPhase { get; private set; }

        /// <summary>The current turn number (starts at 1).</summary>
        public int TurnNumber { get; private set; }

        /// <summary>Fired whenever the phase changes. Passes the new phase.</summary>
        public event Action<TurnPhase> OnPhaseChanged;

        /// <summary>
        /// Initialize the controller for a new encounter.
        /// Sets turn 1, phase Draw (player acts first).
        /// </summary>
        public void Initialize()
        {
            TurnNumber = 1;
            CurrentPhase = TurnPhase.Draw;
            RaisePhaseChanged();
        }

        /// <summary>
        /// Initialize with explicit dependencies (useful for testing).
        /// </summary>
        public void Initialize(StatusEffectSystem ses, GameObject player)
        {
            statusEffectSystem = ses;
            playerObject = player;
            Initialize();
        }

        /// <summary>
        /// Advance to the next phase in the cycle.
        /// Draw → Play → Discard → Enemy → Draw (next turn).
        /// When the player is stunned, Play is skipped (Draw → Discard).
        /// </summary>
        public void AdvancePhase()
        {
            switch (CurrentPhase)
            {
                case TurnPhase.Draw:
                    if (IsPlayerStunned())
                    {
                        // Skip Play_Phase when stunned
                        CurrentPhase = TurnPhase.Discard;
                    }
                    else
                    {
                        CurrentPhase = TurnPhase.Play;
                    }
                    break;

                case TurnPhase.Play:
                    CurrentPhase = TurnPhase.Discard;
                    break;

                case TurnPhase.Discard:
                    CurrentPhase = TurnPhase.Enemy;
                    break;

                case TurnPhase.Enemy:
                    TurnNumber++;
                    CurrentPhase = TurnPhase.Draw;
                    break;
            }

            RaisePhaseChanged();
        }

        private bool IsPlayerStunned()
        {
            if (statusEffectSystem == null || playerObject == null)
                return false;

            return statusEffectSystem.IsStunned(playerObject);
        }

        private void RaisePhaseChanged()
        {
            OnPhaseChanged?.Invoke(CurrentPhase);

            if (BattleEventBus.Instance != null)
            {
                BattleEventBus.Instance.Raise(new TurnPhaseChangedEvent
                {
                    NewPhase = CurrentPhase,
                    TurnNumber = TurnNumber
                });
            }
        }
    }
}
