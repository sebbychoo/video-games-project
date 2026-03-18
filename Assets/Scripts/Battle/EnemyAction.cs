using System;

namespace CardBattle
{
    public enum EnemyActionType { DealDamage, ApplyStatus }

    [Serializable]
    public struct EnemyAction
    {
        public EnemyActionType actionType;
        public int value;
    }
}
