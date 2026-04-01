using System;
using System.Collections.Generic;

namespace CardBattle
{
    [Serializable]
    public class RunState
    {
        public int currentFloor = 1;
        public int playerHP;
        public int playerMaxHP;
        public int hours;
        public int hoursEarnedTotal;
        public int badReviewsEarnedTotal;
        public List<string> deckCardIds;
        public List<string> toolIds;
        public List<string> seenCutsceneIds;
        public string startingDeckSetId;
        public bool isActive;
        public int enemiesDefeated;
        public int cardRemovalsThisRun;
        // Floor transition spawn position (where the exit was on the previous floor)
        public float spawnX;
        public float spawnZ;
        public bool hasCustomSpawn;
        /// <summary>Stable seed for this run, set once at run start.</summary>
        public int runSeed;
    }
}
