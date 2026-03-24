using System.Collections.Generic;
using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// Manages active status effects (Burn, Stun, Bleed) for the player and each enemy.
    /// - Refresh duration on re-application (no stacking).
    /// - Decrement durations at turn end; remove at 0.
    /// - Burn: deal damage at start of target's turn.
    /// - Stun: skip enemy action / skip player Play_Phase.
    /// - Bleed: bonus damage on every damage instance (queried externally).
    /// </summary>
    public class StatusEffectSystem : MonoBehaviour
    {
        public static readonly string Burn  = "Burn";
        public static readonly string Stun  = "Stun";
        public static readonly string Bleed = "Bleed";

        private readonly Dictionary<GameObject, List<StatusEffectInstance>> _effects
            = new Dictionary<GameObject, List<StatusEffectInstance>>();

        /// <summary>Initialize the system, clearing all tracked effects.</summary>
        public void Initialize()
        {
            _effects.Clear();
        }

        /// <summary>
        /// Apply a status effect to a target. If the same effectId already exists
        /// on the target, refresh its duration and value (no stacking).
        /// </summary>
        public void Apply(GameObject target, StatusEffectInstance effect)
        {
            if (target == null) return;

            if (!_effects.ContainsKey(target))
                _effects[target] = new List<StatusEffectInstance>();

            List<StatusEffectInstance> list = _effects[target];

            // Check for existing effect of same type — refresh, don't stack
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].effectId == effect.effectId)
                {
                    list[i] = new StatusEffectInstance
                    {
                        effectId = effect.effectId,
                        duration = effect.duration,
                        value = effect.value
                    };

                    RaiseEvent(target, effect, isRemoval: false);
                    return;
                }
            }

            list.Add(effect);
            RaiseEvent(target, effect, isRemoval: false);
        }

        /// <summary>
        /// Tick all effects on a target: decrement durations by 1 and remove
        /// any that reach 0. Raises removal events for expired effects.
        /// Call this at the end of a turn for each target.
        /// </summary>
        public void Tick(GameObject target)
        {
            if (target == null) return;
            if (!_effects.TryGetValue(target, out List<StatusEffectInstance> list)) return;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                StatusEffectInstance e = list[i];
                e.duration -= 1;

                if (e.duration <= 0)
                {
                    list.RemoveAt(i);
                    RaiseEvent(target, e, isRemoval: true);
                }
                else
                {
                    list[i] = e;
                }
            }
        }

        /// <summary>
        /// Process Burn damage for a target. Call at the start of that target's turn.
        /// Returns the total Burn damage dealt (0 if no Burn active).
        /// The caller is responsible for actually applying the HP damage.
        /// </summary>
        public int ProcessBurn(GameObject target)
        {
            if (target == null) return 0;
            if (!_effects.TryGetValue(target, out List<StatusEffectInstance> list)) return 0;

            int totalBurn = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].effectId == Burn)
                    totalBurn += list[i].value;
            }
            return totalBurn;
        }

        /// <summary>
        /// Check if a target is stunned.
        /// </summary>
        public bool IsStunned(GameObject target)
        {
            if (target == null) return false;
            if (!_effects.TryGetValue(target, out List<StatusEffectInstance> list)) return false;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].effectId == Stun)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Get the Bleed bonus damage value for a target.
        /// Returns 0 if no Bleed is active.
        /// </summary>
        public int GetBleedBonus(GameObject target)
        {
            if (target == null) return 0;
            if (!_effects.TryGetValue(target, out List<StatusEffectInstance> list)) return 0;

            int totalBleed = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].effectId == Bleed)
                    totalBleed += list[i].value;
            }
            return totalBleed;
        }

        /// <summary>
        /// Get all active effects on a target. Returns empty list if none.
        /// </summary>
        public List<StatusEffectInstance> GetEffects(GameObject target)
        {
            if (target == null) return new List<StatusEffectInstance>();
            if (!_effects.TryGetValue(target, out List<StatusEffectInstance> list))
                return new List<StatusEffectInstance>();
            return new List<StatusEffectInstance>(list);
        }

        /// <summary>
        /// Check if a target has a specific effect active.
        /// </summary>
        public bool HasEffect(GameObject target, string effectId)
        {
            if (target == null) return false;
            if (!_effects.TryGetValue(target, out List<StatusEffectInstance> list)) return false;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].effectId == effectId)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Clear all effects from a target. Raises removal events for each.
        /// </summary>
        public void ClearAll(GameObject target)
        {
            if (target == null) return;
            if (!_effects.TryGetValue(target, out List<StatusEffectInstance> list)) return;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                RaiseEvent(target, list[i], isRemoval: true);
            }
            list.Clear();
        }

        /// <summary>
        /// Remove a specific effect from a target by effectId.
        /// </summary>
        public void Remove(GameObject target, string effectId)
        {
            if (target == null) return;
            if (!_effects.TryGetValue(target, out List<StatusEffectInstance> list)) return;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].effectId == effectId)
                {
                    RaiseEvent(target, list[i], isRemoval: true);
                    list.RemoveAt(i);
                    return;
                }
            }
        }

        private void RaiseEvent(GameObject target, StatusEffectInstance effect, bool isRemoval)
        {
            if (BattleEventBus.Instance == null) return;

            BattleEventBus.Instance.Raise(new StatusEffectEvent
            {
                Target = target,
                EffectName = effect.effectId,
                Duration = effect.duration,
                IsRemoval = isRemoval
            });
        }
    }
}
