using System;
using System.Collections.Generic;

namespace CardBattle
{
    [Serializable]
    public class RunState
    {
        public int currentFloor;
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
    }
}
