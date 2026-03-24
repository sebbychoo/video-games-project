using System.Collections.Generic;
using UnityEngine;

namespace CardBattle
{
    [CreateAssetMenu(menuName = "CardBattle/EncounterData")]
    public class EncounterData : ScriptableObject
    {
        public List<EnemyCombatantData> enemies;
        public bool isBossEncounter;
        public int badReviewsReward;
    }
}
