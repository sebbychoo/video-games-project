using System.Collections.Generic;
using UnityEngine;

namespace CardBattle
{
    public enum EnemyVariant { Coworker, Creature, Boss }

    [CreateAssetMenu(menuName = "CardBattle/EnemyCombatantData")]
    public class EnemyCombatantData : ScriptableObject
    {
        public string enemyName;
        public int maxHP;
        public int hoursReward;
        public EnemyVariant variant;
        public Sprite sprite;
        public List<EnemyAction> attackPattern;
        public bool isBoss;
        [TextArea] public string preFightDialogue;
        [TextArea] public string postFightDialogue;
    }
}
