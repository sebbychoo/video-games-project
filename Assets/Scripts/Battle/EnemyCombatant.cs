using System.Collections.Generic;
using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// Result of an enemy executing an action. The caller (BattleManager) uses this
    /// to apply damage to the player, apply status effects, etc.
    /// </summary>
    public struct EnemyActionResult
    {
        public EnemyActionType ActionType;
        public int DamageValue;
        public string StatusEffectId;
        public int StatusDuration;
        public int StatusValue;
        public EnemyBuffType BuffType;
        public int BuffDuration;
        public bool WasSkipped;
    }

    /// <summary>
    /// Result of applying damage to an enemy, indicating whether the enemy
    /// parried the attack or took the damage.
    /// </summary>
    public struct TakeDamageResult
    {
        public bool WasParried;
        public int DamageDealt;
    }

    /// <summary>
    /// MonoBehaviour wrapping EnemyCombatantData for a single enemy in an encounter.
    /// Tracks HP (via Health component), attack pattern index, and provides
    /// action execution, damage handling, and intent display.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class EnemyCombatant : MonoBehaviour
    {
        private EnemyCombatantData _data;
        private BlockSystem _blockSystem;
        private StatusEffectSystem _statusEffectSystem;
        private int _patternIndex;
        private Health _health;

        public EnemyCombatantData Data => _data;
        public bool IsAlive => _health != null && _health.currentHealth > 0;
        public int CurrentHP => _health != null ? _health.currentHealth : 0;
        public int MaxHP => _health != null ? _health.maxHealth : 0;

        /// <summary>Chance (0–1) that this enemy parries a player's Attack card.</summary>
        public float EnemyParryChance => _data != null ? _data.enemyParryChance : 0f;

        /// <summary>Hours currency awarded when this enemy is defeated.</summary>
        public int HoursReward => _data != null ? _data.hoursReward : 0;

        /// <summary>
        /// Returns the next action in the pattern for intent display.
        /// Evaluates conditions to show the actual action that will execute.
        /// </summary>
        public EnemyAction CurrentIntent
        {
            get
            {
                if (_data == null || _data.attackPattern == null || _data.attackPattern.Count == 0)
                    return default;

                // Walk through the pattern starting at current index to find the
                // first action whose condition is met (or has no condition).
                int patternLength = _data.attackPattern.Count;
                for (int i = 0; i < patternLength; i++)
                {
                    int idx = (_patternIndex + i) % patternLength;
                    EnemyAction action = _data.attackPattern[idx];
                    if (IsConditionMet(action.condition))
                        return action;
                }

                // Fallback: return the action at current index regardless
                return _data.attackPattern[_patternIndex % patternLength];
            }
        }

        /// <summary>
        /// Initialize this enemy combatant with data and system references.
        /// </summary>
        public void Initialize(EnemyCombatantData data, BlockSystem blockSystem, StatusEffectSystem statusEffectSystem)
        {
            _data = data;
            _blockSystem = blockSystem;
            _statusEffectSystem = statusEffectSystem;
            _patternIndex = 0;

            _health = GetComponent<Health>();
            if (_health != null)
            {
                _health.maxHealth = data.maxHP;
                _health.currentHealth = data.maxHP;
                _health.suppressSceneLoad = true;
            }
        }

        /// <summary>
        /// Execute the current action in the attack pattern.
        /// Returns an EnemyActionResult describing what happened so the caller
        /// can apply damage to the player, status effects, etc.
        /// </summary>
        public EnemyActionResult ExecuteAction()
        {
            if (_data == null || _data.attackPattern == null || _data.attackPattern.Count == 0)
                return new EnemyActionResult { WasSkipped = true };

            int patternLength = _data.attackPattern.Count;

            // Find the next action whose condition is met
            EnemyAction action = default;
            bool found = false;
            int stepsChecked = 0;

            while (stepsChecked < patternLength)
            {
                action = _data.attackPattern[_patternIndex % patternLength];
                _patternIndex++;
                stepsChecked++;

                if (IsConditionMet(action.condition))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                // No valid action found this cycle — skip turn
                return new EnemyActionResult { WasSkipped = true };
            }

            EnemyActionResult result = new EnemyActionResult
            {
                ActionType = action.actionType,
                WasSkipped = false
            };

            switch (action.actionType)
            {
                case EnemyActionType.DealDamage:
                    result.DamageValue = action.value;
                    break;

                case EnemyActionType.ApplyStatus:
                    result.StatusEffectId = action.statusEffectId;
                    result.StatusDuration = action.statusDuration;
                    result.StatusValue = action.value;
                    break;

                case EnemyActionType.Defend:
                    // Add Block to self
                    if (_blockSystem != null)
                        _blockSystem.AddBlock(action.value, gameObject);
                    break;

                case EnemyActionType.Buff:
                    // Placeholder: return buff info for caller to handle
                    result.BuffType = action.buffType;
                    result.BuffDuration = action.buffDuration;
                    break;

                case EnemyActionType.Special:
                    // Placeholder for special enemy actions
                    Debug.Log($"{_data.enemyName} performs a special action.");
                    break;
            }

            return result;
        }

        /// <summary>
        /// Apply damage from a player Attack card. Evaluates Enemy_Parry_Chance first;
        /// if the enemy parries, damage is canceled. Otherwise applies Bleed bonus,
        /// Block absorption, then remaining damage to HP. Raises DamageEvent.
        /// </summary>
        public TakeDamageResult TakeDamageFromAttack(int damage, GameObject source)
        {
            if (!IsAlive)
                return new TakeDamageResult { WasParried = false, DamageDealt = 0 };

            // Evaluate Enemy_Parry_Chance (Req 6.11)
            float parryChance = _data != null ? _data.enemyParryChance : 0f;
            if (parryChance > 0f && Random.value < parryChance)
            {
                // Enemy parried — cancel all damage
                Debug.Log($"{(_data != null ? _data.enemyName : name)} parried the attack!");
                return new TakeDamageResult { WasParried = true, DamageDealt = 0 };
            }

            // Parry failed — apply damage normally
            int dealt = ApplyDamageInternal(damage, source);
            return new TakeDamageResult { WasParried = false, DamageDealt = dealt };
        }

        /// <summary>
        /// Apply damage to this enemy (non-parryable). Used for status effect ticks,
        /// environmental damage, and other sources that bypass enemy parry chance.
        /// Applies Bleed bonus, Block absorption, then remaining damage to HP.
        /// Raises DamageEvent.
        /// </summary>
        public void TakeDamage(int damage, GameObject source)
        {
            if (!IsAlive) return;
            ApplyDamageInternal(damage, source);
        }

        /// <summary>
        /// Internal damage pipeline: Bleed bonus → Block absorption → HP reduction → DamageEvent.
        /// Returns the total damage dealt (before Block absorption).
        /// </summary>
        private int ApplyDamageInternal(int damage, GameObject source)
        {
            // Apply Bleed bonus
            int bleedBonus = _statusEffectSystem != null
                ? _statusEffectSystem.GetBleedBonus(gameObject)
                : 0;
            int totalDamage = damage + bleedBonus;

            // Absorb through Block
            int remaining = _blockSystem != null
                ? _blockSystem.AbsorbDamage(totalDamage, gameObject)
                : totalDamage;

            // Apply to HP
            if (remaining > 0 && _health != null)
                _health.TakeDamage(remaining);

            // Raise DamageEvent
            if (BattleEventBus.Instance != null)
            {
                BattleEventBus.Instance.Raise(new DamageEvent
                {
                    Source = source,
                    Target = gameObject,
                    Amount = totalDamage
                });
            }

            return totalDamage;
        }

        // ── Condition evaluation ────────────────────────────────────────────

        private bool IsConditionMet(EnemyActionCondition condition)
        {
            switch (condition)
            {
                case EnemyActionCondition.None:
                    return true;

                case EnemyActionCondition.HPBelow50:
                    return _health != null && _health.currentHealth < _health.maxHealth * 0.5f;

                case EnemyActionCondition.HPBelow25:
                    return _health != null && _health.currentHealth < _health.maxHealth * 0.25f;

                case EnemyActionCondition.PlayerLowHP:
                    if (BattleManager.Instance == null) return false;
                    // Check if the player's HP is below 25% of max
                    return BattleManager.Instance.PlayerHP < BattleManager.Instance.PlayerMaxHP * 0.25f;

                default:
                    return true;
            }
        }
    }
}
