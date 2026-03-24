using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle
{
    [Serializable]
    public struct HubUpgradeEffect
    {
        public ToolModifierType modifierType;
        public int value;
    }

    [CreateAssetMenu(menuName = "CardBattle/HubUpgradeData")]
    public class HubUpgradeData : ScriptableObject
    {
        public string upgradeId;
        public string displayName;
        public int maxLevel;
        public List<int> costPerLevel;
        public List<HubUpgradeEffect> effectsPerLevel;
        [TextArea] public string description;
        public List<Sprite> furnitureSprites;
    }
}
