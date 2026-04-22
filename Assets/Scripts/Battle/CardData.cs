using System.Collections.Generic;
using UnityEngine;

namespace CardBattle
{
    public enum CardType { Attack, Defense, Effect, Utility, Special }
    public enum CardRarity { Common, Rare, Legendary, Unknown }
    public enum TargetMode { SingleEnemy, AllEnemies, Self, NoTarget }
    public enum UtilityEffectType { None, Draw, Restore, Retrieve, Reorder, Heal }
    public enum ParryEffectType { None, CounterDamage, RestoreOT, DrawCard }

    [CreateAssetMenu(menuName = "CardBattle/CardData")]
    public class CardData : ScriptableObject
    {
        public string cardName;
        public int overtimeCost;
        [TextArea] public string description;
        public CardType cardType;
        public CardRarity cardRarity;
        public int effectValue;
        public int blockValue;
        public List<string> parryMatchTags; // Defense cards: defines which enemy attack types this card can parry
        public TargetMode targetMode;
        public Sprite cardSprite;

        // Effect card fields
        public string statusEffectId;
        public int statusDuration;

        // Special card fields
        public string specialCardId;

        // Utility card fields
        public UtilityEffectType utilityEffectType;

        // On-parry bonus effect (Defense cards only)
        public ParryEffectType onParryEffect = ParryEffectType.None;
        public int onParryEffectValue = 0;

        // Theme tag for hub upgrade bonuses (Computer upgrade boosts Technology-themed cards)
        public bool isTechnologyThemed;
    }
}
