using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle
{
    public enum ToolModifierType { OvertimeRegen, BlockBonus, HandSize, DamageBonus, MaxHP, HealPerFloor, TechCardDamage, MaxDeckSize, ParryWindowBonus }

    [Serializable]
    public struct ToolModifier
    {
        public ToolModifierType modifierType;
        public int value;
    }

    [CreateAssetMenu(menuName = "CardBattle/ToolData")]
    public class ToolData : ScriptableObject
    {
        public string toolName;
        [TextArea] public string description;
        public Sprite toolSprite;
        public CardRarity rarity;
        public List<ToolModifier> modifiers;
    }
}
