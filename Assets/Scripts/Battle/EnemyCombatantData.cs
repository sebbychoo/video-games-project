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
        public GameObject explorationPrefab; // prefab spawned during exploration
        public List<EnemyAction> attackPattern;
        public bool isBoss;
        [Range(0f, 1f)]
        public float enemyParryChance;
        public float baseParryWindowDuration;
        [TextArea] public string preFightDialogue;
        [TextArea] public string postFightDialogue;
    }
}
