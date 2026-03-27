using System.Collections.Generic;
using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// Utility for enforcing deck size limits. The base maximum comes from GameConfig,
    /// increased by the Filing Cabinet hub upgrade and MaxDeckSize tool modifiers.
    /// </summary>
    public static class DeckSizeLimiter
    {
        /// <summary>
        /// Returns the effective maximum deck size for the current run,
        /// accounting for hub upgrades and tool modifiers.
        /// </summary>
        public static int GetMaxDeckSize(GameConfig config)
        {
            int baseCap = config != null ? config.maximumDeckSize : 25;

            if (SaveManager.Instance == null || SaveManager.Instance.CurrentRun == null)
                return baseCap;

            int bonus = 0;

            // Tool modifiers (MaxDeckSize type)
            List<string> toolIds = SaveManager.Instance.CurrentRun.toolIds;
            if (toolIds != null)
            {
                foreach (string toolId in toolIds)
                {
                    ToolData tool = Resources.Load<ToolData>(toolId);
                    if (tool == null || tool.modifiers == null) continue;
                    foreach (ToolModifier mod in tool.modifiers)
                    {
                        if (mod.modifierType == ToolModifierType.MaxDeckSize)
                            bonus += mod.value;
                    }
                }
            }

            // Hub upgrade bonus (Filing Cabinet)
            if (SaveManager.Instance.CurrentMeta != null &&
                SaveManager.Instance.CurrentMeta.hubUpgradeLevels != null)
            {
                foreach (StringIntPair pair in SaveManager.Instance.CurrentMeta.hubUpgradeLevels)
                {
                    if (pair.key == "FilingCabinet")
                    {
                        // Load the HubUpgradeData to get the effect value per level
                        HubUpgradeData upgradeData = Resources.Load<HubUpgradeData>("FilingCabinet");
                        if (upgradeData != null && upgradeData.effectsPerLevel != null)
                        {
                            for (int i = 0; i < pair.value && i < upgradeData.effectsPerLevel.Count; i++)
                            {
                                if (upgradeData.effectsPerLevel[i].modifierType == ToolModifierType.MaxDeckSize)
                                    bonus += upgradeData.effectsPerLevel[i].value;
                            }
                        }
                        break;
                    }
                }
            }

            return baseCap + bonus;
        }

        /// <summary>
        /// Checks whether adding a card would exceed the deck size limit.
        /// Uses RunState.deckCardIds as the source of truth for total deck size
        /// (appropriate for between-encounter additions like shops and work boxes).
        /// </summary>
        public static bool CanAddCard(GameConfig config)
        {
            int max = GetMaxDeckSize(config);

            if (SaveManager.Instance == null || SaveManager.Instance.CurrentRun == null)
                return true;

            int currentSize = SaveManager.Instance.CurrentRun.deckCardIds != null
                ? SaveManager.Instance.CurrentRun.deckCardIds.Count
                : 0;

            return currentSize < max;
        }
    }
}
