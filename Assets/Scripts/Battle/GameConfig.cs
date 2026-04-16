using UnityEngine;

namespace CardBattle
{
    [CreateAssetMenu(menuName = "CardBattle/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        public int finalFloor = 75;
        public int baseHandSize = 5;
        public int overtimeMaxCapacity = 10;
        public int overtimeRegenPerTurn = 2;
        public int workerEncounterFloor = 5;
        public int bossFloorInterval = 3;
        public int breakRoomFloorInterval = 2;
        public int workerHP = 12;
        public int workerSelfDamage = 4;
        public int playerBaseHP = 80;
        public int minimapBaseRevealLevel = 0;
        public int waterCoolerFloorInterval = 2;
        public float waterCoolerHealPercent = 0.35f;
        public int shopMinCards = 3;
        public int shopMaxCards = 5;
        public int shopMinTools = 0;
        public int shopMaxTools = 2;
        public int cardRemovalBaseCost = 25;
        public int cardRemovalCostIncrease = 10;
        public int minimumDeckSize = 1;
        public int maximumDeckSize = 25;
        public float safeRoomChaseTimeout = 5f;
        public float baseParryWindowDuration = 1.5f;
        public float parryWindowFloorScaling = 0.02f;
        public float parryWindowMinDuration = 0.3f;

        [Header("Perfect Parry")]
        [Tooltip("Fraction of parry window (from end) that counts as perfect timing.")]
        public float perfectParryThreshold = 0.20f;

        [Header("Enemy Attack Animation")]
        [Tooltip("How far the enemy dashes toward the player (world units).")]
        public float enemyDashDistance = 1.5f;
        [Tooltip("Base dash duration in seconds (fast phase uses half of this).")]
        public float enemyDashDuration = 0.25f;

        [Header("Boss Intro Screen")]
        public float bossIntroSlideDuration = 0.6f;
        public float bossIntroHoldDuration = 1.5f;

        [Header("Boss Phase Transition")]
        public float phaseTransitionPauseDuration = 1.0f;

        [Header("Blood Accumulation")]
        public float bloodBaseIncrement = 0.005f;
        public float bloodGrowthRate = 0.15f;
        public float regularBloodMultiplier = 1.0f;
        public float bossBloodMultiplier = 2.0f;
    }
}
