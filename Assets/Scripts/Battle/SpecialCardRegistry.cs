using System.Collections.Generic;
using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// Context passed to special card effect implementations.
    /// </summary>
    public struct CardEffectContext
    {
        public CardData Card;
        public GameObject Source;
        public List<EnemyCombatant> Targets;
        public BattleManager Battle;
    }

    /// <summary>
    /// Interface for custom special card effects.
    /// Implement this to add new Special cards without modifying the core resolver.
    /// </summary>
    public interface ISpecialCardEffect
    {
        void Execute(CardEffectContext context);
    }

    /// <summary>
    /// Static registry mapping special card IDs to their effect implementations.
    /// New Special cards register here so CardEffectResolver can look them up at play time.
    /// </summary>
    public static class SpecialCardRegistry
    {
        private static readonly Dictionary<string, ISpecialCardEffect> _effects
            = new Dictionary<string, ISpecialCardEffect>();

        /// <summary>Register a special card effect by its unique ID.</summary>
        public static void Register(string id, ISpecialCardEffect effect)
        {
            if (string.IsNullOrEmpty(id) || effect == null) return;
            _effects[id] = effect;
        }

        /// <summary>Look up and execute a registered special card effect.</summary>
        public static void Execute(string id, CardEffectContext context)
        {
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning("SpecialCardRegistry: Attempted to execute with null/empty id.");
                return;
            }

            if (_effects.TryGetValue(id, out ISpecialCardEffect effect))
            {
                effect.Execute(context);
            }
            else
            {
                Debug.LogWarning($"SpecialCardRegistry: No effect registered for id '{id}'.");
            }
        }
    }
}
