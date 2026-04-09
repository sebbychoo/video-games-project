using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle
{
    public enum BossPose { Standing, Sitting }

    [Serializable]
    public class SpriteFrameAnimation
    {
        public Sprite[] frames;
        public float frameRate = 8f;
        public bool loop = true;
    }

    [Serializable]
    public class BossAttackAnimation
    {
        public EnemyActionType actionType;
        public SpriteFrameAnimation animation;
    }

    [Serializable]
    public class BossAnimationData
    {
        public SpriteFrameAnimation idleAnimation;
        public SpriteFrameAnimation damagedAnimation;
        public SpriteFrameAnimation deathAnimation;
        public List<BossAttackAnimation> attackAnimations;
    }

    [Serializable]
    public class BossPhase2Data
    {
        [Range(0f, 1f)]
        public float hpThresholdPercent = 0.5f;
        public List<EnemyAction> phase2AttackPattern;
        public Sprite[] phase2SpriteSet;
        public BossAnimationData phase2Animations;
    }
}
