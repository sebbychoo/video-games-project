using System;
using System.Collections.Generic;

namespace CardBattle
{
    [Serializable]
    public class StringIntPair
    {
        public string key;
        public int value;
    }

    [Serializable]
    public class MetaState
    {
        public int badReviews;
        public List<StringIntPair> hubUpgradeLevels;
        public List<string> unlockedAchievements;
        public bool tutorialCompleted;
    }
}
