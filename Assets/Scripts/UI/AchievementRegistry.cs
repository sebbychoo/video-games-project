using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// A single achievement definition — matches the string IDs stored in MetaState.unlockedAchievements.
    /// </summary>
    [Serializable]
    public class AchievementDefinition
    {
        [Tooltip("Must match the string stored in MetaState.unlockedAchievements exactly.")]
        public string id;
        public string displayName;
        [TextArea(2, 4)]
        public string description;
        [Tooltip("Icon shown when the achievement is unlocked. Leave null — a ? badge is shown while locked.")]
        public Sprite icon;
    }

    /// <summary>
    /// ScriptableObject registry of every achievement in the game.
    /// Create one instance at Assets/ScriptableObjects/AchievementRegistry.asset
    /// and assign it to the MainMenu component.
    /// </summary>
    [CreateAssetMenu(fileName = "AchievementRegistry", menuName = "CardBattle/Achievement Registry")]
    public class AchievementRegistry : ScriptableObject
    {
        public List<AchievementDefinition> achievements = new();

        private Dictionary<string, AchievementDefinition> _lookup;

        /// <summary>Returns the definition for the given id, or null if not registered.</summary>
        public AchievementDefinition Get(string id)
        {
            if (_lookup == null)
                BuildLookup();
            _lookup.TryGetValue(id, out AchievementDefinition def);
            return def;
        }

        private void BuildLookup()
        {
            _lookup = new Dictionary<string, AchievementDefinition>(achievements.Count);
            foreach (AchievementDefinition def in achievements)
                if (!string.IsNullOrEmpty(def.id))
                    _lookup[def.id] = def;
        }

        private void OnEnable() => _lookup = null; // invalidate on reload
    }
}
