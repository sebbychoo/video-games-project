using System;

namespace CardBattle
{
    public enum WorkBoxSize { Small, Big, Huge }

    [Serializable]
    public struct WorkBoxSpawnRates
    {
        public float smallRate;
        public float bigRate;
        public float hugeRate;
    }
}
