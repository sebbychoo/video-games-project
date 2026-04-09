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

    /// <summary>
    /// Per-boss intro screen configuration. Allows each boss to have custom
    /// intro text, timing, and background animation.
    /// </summary>
    [Serializable]
    public class BossIntroData
    {
        [Tooltip("First line of text (e.g. 'Introducing...'). Leave empty to skip this line.")]
        public string introLine = "Introducing...";

        [Tooltip("Delay in seconds before the intro line appears.")]
        public float introLineDelay = 0f;

        [Tooltip("Slide duration for the intro line.")]
        public float introLineSlideDuration = 0.6f;

        [Tooltip("Delay in seconds after intro line before the name appears.")]
        public float nameDelay = 0.3f;

        [Tooltip("Slide duration for the name line.")]
        public float nameSlideDuration = 0.6f;

        [Tooltip("Delay in seconds after name before the title appears.")]
        public float titleDelay = 0.2f;

        [Tooltip("Fade duration for the title.")]
        public float titleFadeDuration = 0.3f;

        [Tooltip("How long to hold the final state before dismissing.")]
        public float holdDuration = 1.5f;

        [Tooltip("Optional background animation that plays behind the text. Leave null for solid black.")]
        public SpriteFrameAnimation backgroundAnimation;
    }
}
