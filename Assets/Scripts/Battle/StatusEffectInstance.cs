using System;

namespace CardBattle
{
    [Serializable]
    public struct StatusEffectInstance
    {
        public string effectId;
        public int duration;
        public int value;
    }
}
