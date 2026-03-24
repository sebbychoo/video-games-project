using UnityEngine;

namespace CardBattle
{
    public enum CardType { Attack, Defense, Effect, Utility, Special }
    public enum CardRarity { Common, Rare, Epic, Legendary, Unknown }
    public enum TargetMode { SingleEnemy, AllEnemies, Self, NoTarget }
    public enum UtilityEffectType { None, Draw, Restore, Retrieve, Reorder, Heal }

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
        public TargetMode targetMode;
        public Sprite cardSprite;

        // Effect card fields
        public string statusEffectId;
        public int statusDuration;

        // Special card fields
        public string specialCardId;

        // Utility card fields
        public UtilityEffectType utilityEffectType;
    }
}
