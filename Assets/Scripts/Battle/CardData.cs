using UnityEngine;

namespace CardBattle
{
    public enum CardType { Attack, Skill, Power }

    [CreateAssetMenu(menuName = "CardBattle/CardData")]
    public class CardData : ScriptableObject
    {
        public string cardName;
        public int energyCost;
        [TextArea] public string description;
        public CardType cardType;
        public int effectValue; // damage, block amount, draw count, etc.
    }
}
