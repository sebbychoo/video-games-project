using System;
using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// Per-boss component that manages which SpriteFrameAnimation to play based on
    /// battle state (idle, damaged, attack, death). Holds phase 1 and phase 2 animation
    /// sets and delegates playback to a SpriteFrameAnimator.
    /// </summary>
    public class BossAnimationController : MonoBehaviour
    {
        public BossAnimationData Phase1Animations;
        public BossAnimationData Phase2Animations;

        private BossAnimationData _activeAnimations;
        private SpriteFrameAnimator _animator;

        private void Awake()
        {
            _animator = GetComponent<SpriteFrameAnimator>();
            if (_animator == null)
                _animator = gameObject.AddComponent<SpriteFrameAnimator>();

            _activeAnimations = Phase1Animations;
        }

        /// <summary>
        /// Play the idle animation in a loop.
        /// </summary>
        public void PlayIdle()
        {
            EnsureInitialized();
            var anim = _activeAnimations?.idleAnimation;
            if (anim != null)
                _animator.Play(anim, loop: true);
        }

        private void EnsureInitialized()
        {
            if (_activeAnimations == null)
                _activeAnimations = Phase1Animations;
            if (_animator == null)
            {
                _animator = GetComponent<SpriteFrameAnimator>();
                if (_animator == null)
                    _animator = gameObject.AddComponent<SpriteFrameAnimator>();
            }
        }

        /// <summary>
        /// Play the damaged animation once, then invoke onComplete.
        /// </summary>
        public void PlayDamaged(Action onComplete)
        {
            EnsureInitialized();
            var anim = _activeAnimations?.damagedAnimation;
            if (anim != null)
                _animator.Play(anim, loop: false, onComplete: onComplete);
            else
                onComplete?.Invoke();
        }

        /// <summary>
        /// Play the attack animation mapped to the given action type.
        /// If no mapping exists, invokes onComplete immediately.
        /// </summary>
        public void PlayAttack(EnemyActionType actionType, Action onComplete)
        {
            EnsureInitialized();
            var anim = GetAttackAnimation(actionType);
            if (anim != null)
                _animator.Play(anim, loop: false, onComplete: onComplete);
            else
                onComplete?.Invoke();
        }

        /// <summary>
        /// Play the death animation once, then invoke onComplete.
        /// </summary>
        public void PlayDeath(Action onComplete)
        {
            EnsureInitialized();
            var anim = _activeAnimations?.deathAnimation;
            if (anim != null)
                _animator.Play(anim, loop: false, onComplete: onComplete);
            else
                onComplete?.Invoke();
        }

        /// <summary>
        /// Switch the active animation set to phase 2.
        /// </summary>
        public void SwitchToPhase2()
        {
            if (Phase2Animations != null)
                _activeAnimations = Phase2Animations;
        }

        /// <summary>
        /// Look up the attack animation for a given action type.
        /// Returns null if no mapping exists.
        /// </summary>
        public SpriteFrameAnimation GetAttackAnimation(EnemyActionType actionType)
        {
            if (_activeAnimations?.attackAnimations == null)
                return null;

            foreach (var entry in _activeAnimations.attackAnimations)
            {
                if (entry.actionType == actionType)
                    return entry.animation;
            }

            return null;
        }
    }
}
