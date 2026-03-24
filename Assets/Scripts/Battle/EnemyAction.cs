using System;

namespace CardBattle
{
    public enum EnemyActionType { DealDamage, ApplyStatus, Defend, Buff, Special }
    public enum EnemyBuffType { None, DamageUp, DamageShield, Regen }
    public enum EnemyActionCondition { None, HPBelow50, HPBelow25, PlayerHasBlock }

    [Serializable]
    public struct EnemyAction
    {
        public EnemyActionType actionType;
        public int value;
        public string statusEffectId;
        public int statusDuration;
        public EnemyBuffType buffType;
        public int buffDuration;
        public EnemyActionCondition condition;
    }
}
