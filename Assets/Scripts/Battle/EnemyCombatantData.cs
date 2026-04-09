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

        [Header("Boss Data")]
        public string bossTitle;
        public BossPose bossPose;
        public BossAnimationData bossAnimationData;
        public BossIntroData bossIntroData;

        [Header("Boss Phase 2")]
        public BossPhase2Data phase2Data;

        [Header("Badge Sprites (for battle UI — 5 states)")]
        public Sprite badgeHealthy;    // 75-100%
        public Sprite badgeConcerned;  // 50-74%
        public Sprite badgeStressed;   // 25-49%
        public Sprite badgeCritical;   // 1-24%
        public Sprite badgeDead;       // 0%
    }
}
