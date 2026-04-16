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

        /// <summary>
        /// Accumulated blood level on gloves (0.0–1.0). Purely cosmetic fight history.
        /// Increases when the player plays attack cards. Reset to 0 on defeat or new run.
        /// </summary>
        public float persistentBloodLevel;

        /// <summary>
        /// Last known OT value from the most recent battle. Drives vein glow in exploration.
        /// Reset to 10 on new run.
        /// </summary>
        public int persistentOTLevel = 10;

        /// <summary>
        /// List of bathroom instance IDs where the player has already washed blood.
        /// Each bathroom can only be used for washing once per run.
        /// </summary>
        public List<string> washedBathroomIds;
    }
}
