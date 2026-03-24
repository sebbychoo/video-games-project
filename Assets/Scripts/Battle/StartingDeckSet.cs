using System.Collections.Generic;
using UnityEngine;

namespace CardBattle
{
    [CreateAssetMenu(menuName = "CardBattle/StartingDeckSet")]
    public class StartingDeckSet : ScriptableObject
    {
        public string setName;
        [TextArea] public string description;
        public List<CardData> cards;
    }
}
