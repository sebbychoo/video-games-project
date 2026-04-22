using System.Collections.Generic;
using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// Static helper that reads MetaState hub upgrade levels and returns
    /// effective modifier values for each upgrade type. Used by BattleManager,
    /// OvertimeMeter, ParrySystem, DeckManager, etc. at encounter/run start
    /// to apply hub upgrade effects. All effects apply from the next run onward.
    ///
    /// Upgrade effects:
    ///   Computer       — +TechCardDamage per level
    ///   CoffeeMachine  — +OvertimeRegen per level
    ///   DeskChair      — +ParryWindowBonus per level
    ///   FilingCabinet  — +HandSize (early levels), +MaxDeckSize (later levels)
    ///   Plant          — +MaxHP (early levels), +HealPerFloor (later levels)
    ///   Whiteboard     — minimap reveal level (handled by FloorMinimap directly)
    /// </summary>
    public static class HubUpgradeApplier
    {
        /// <summary>
        /// Returns the total modifier value for a given ToolModifierType
        /// from all hub upgrades in the current MetaState.
        /// </summary>
        public static int GetModifierValue(ToolModifierType modifierType)
        {
            if (SaveManager.Instance == null) return 0;
            MetaState meta = SaveManager.Instance.CurrentMeta;
            if (meta?.hubUpgradeLevels == null) return 0;

            int total = 0;

            foreach (StringIntPair pair in meta.hubUpgradeLevels)
            {
                int level = pair.value;
                if (level <= 0) continue;

                HubUpgradeData data = Resources.Load<HubUpgradeData>(pair.key);
                if (data == null || data.effectsPerLevel == null) continue;

                // Sum effects from all levels up to the current level
                for (int i = 0; i < level && i < data.effectsPerLevel.Count; i++)
                {
                    if (data.effectsPerLevel[i].modifierType == modifierType)
                        total += data.effectsPerLevel[i].value;
                }
            }

            return total;
        }

        /// <summary>
        /// Returns the effective hand size, accounting for base config + hub upgrades.
        /// </summary>
        public static int GetEffectiveHandSize(GameConfig config)
        {
            int baseSize = config != null ? config.baseHandSize : 5;
            return baseSize + GetModifierValue(ToolModifierType.HandSize);
        }

        /// <summary>
        /// Returns the effective OT regen per turn, accounting for hub upgrades.
        /// Tool modifiers are applied separately by BattleManager.ApplyToolModifiers.
        /// </summary>
        public static int GetHubOTRegenBonus()
        {
            return GetModifierValue(ToolModifierType.OvertimeRegen);
        }

        /// <summary>
        /// Returns the effective base HP, accounting for hub upgrades.
        /// </summary>
        public static int GetEffectiveBaseHP(GameConfig config)
        {
            int baseHP = config != null ? config.playerBaseHP : 80;
            return baseHP + GetModifierValue(ToolModifierType.MaxHP);
        }

        /// <summary>
        /// Returns the hub upgrade bonus to parry window duration (in seconds).
        /// Each level of DeskChair adds a fixed amount (stored as int in HubUpgradeEffect,
        /// converted to seconds by multiplying by 0.01f).
        /// </summary>
        public static float GetHubParryWindowBonus()
        {
            return GetModifierValue(ToolModifierType.ParryWindowBonus) * 0.01f;
        }

        /// <summary>
        /// Returns the hub upgrade bonus damage for Technology-themed cards.
        /// </summary>
        public static int GetTechCardDamageBonus()
        {
            return GetModifierValue(ToolModifierType.TechCardDamage);
        }

        /// <summary>
        /// Returns the passive heal-per-floor value from the Plant upgrade.
        /// Triggers when the player uses the floor exit.
        /// </summary>
        public static int GetHealPerFloor()
        {
            return GetModifierValue(ToolModifierType.HealPerFloor);
        }

        /// <summary>
        /// Returns the hub upgrade bonus to max deck size from Filing Cabinet.
        /// </summary>
        public static int GetMaxDeckSizeBonus()
        {
            return GetModifierValue(ToolModifierType.MaxDeckSize);
        }

        /// <summary>
        /// Applies all hub upgrade modifiers to the battle subsystems at encounter start.
        /// Called by BattleManager after tool modifiers are applied.
        /// Note: MaxHP bonus is applied separately in InitializePlayerHP.
        /// </summary>
        public static void ApplyToBattle(
            OvertimeMeter overtimeMeter,
            ParrySystem parrySystem,
            CardEffectResolver cardEffectResolver,
            ref int handSize)
        {
            // Coffee Machine: +OT regen
            int otRegenBonus = GetHubOTRegenBonus();
            if (otRegenBonus > 0 && overtimeMeter != null)
                overtimeMeter.ApplyRegenModifier(otRegenBonus);

            // Desk Chair: +parry window duration
            float parryBonus = GetHubParryWindowBonus();
            if (parryBonus > 0f && parrySystem != null)
                parrySystem.ApplyWindowDurationModifier(parryBonus);

            // Computer: +tech card damage
            int techDmg = GetTechCardDamageBonus();
            if (techDmg > 0 && cardEffectResolver != null)
                cardEffectResolver.ApplyTechCardDamageBonus(techDmg);

            // Filing Cabinet: +hand size (early levels)
            int handBonus = GetModifierValue(ToolModifierType.HandSize);
            handSize += handBonus;
        }
    }
}
