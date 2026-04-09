using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using CardBattle;

namespace CardBattle.Tests
{
    /// <summary>
    /// Property-based tests for boss attack animation lookup.
    /// Uses randomized inputs across many iterations to verify correctness properties.
    /// </summary>
    [TestFixture]
    public class BossAnimationPropertyTests
    {
        private const int Iterations = 200;

        /// <summary>
        /// All defined EnemyActionType values, cached once for reuse.
        /// </summary>
        private static readonly EnemyActionType[] AllActionTypes =
            (EnemyActionType[])Enum.GetValues(typeof(EnemyActionType));

        /// <summary>
        /// Lookup helper that mirrors the logic BossAnimationController will use:
        /// find the first BossAttackAnimation whose actionType matches, return its animation.
        /// Returns null if no mapping exists.
        /// </summary>
        private static SpriteFrameAnimation LookupAttackAnimation(
            BossAnimationData data, EnemyActionType actionType)
        {
            if (data?.attackAnimations == null)
                return null;

            foreach (var entry in data.attackAnimations)
            {
                if (entry.actionType == actionType)
                    return entry.animation;
            }

            return null;
        }

        /// <summary>
        /// Helper: create a minimal SpriteFrameAnimation with a unique frame count
        /// so we can distinguish different animations by reference or frame count.
        /// </summary>
        private static SpriteFrameAnimation CreateAnim(int frameCount)
        {
            return new SpriteFrameAnimation
            {
                frames = new Sprite[frameCount],
                frameRate = 8f,
                loop = false
            };
        }

        #region Property 6: Attack Animation Lookup

        /// <summary>
        /// Feature: boss-encounter-system, Property 6: Attack Animation Lookup
        ///
        /// For any BossAnimationData and any EnemyActionType with a mapped entry,
        /// verify lookup returns the correct SpriteFrameAnimation.
        /// For any action type without a mapping, verify lookup returns null.
        /// Validates: Requirements 8.8
        /// </summary>
        [Test]
        public void Property6_MappedActionTypeReturnsCorrectAnimation()
        {
            var rng = new System.Random(42);

            for (int i = 0; i < Iterations; i++)
            {
                // Pick a random non-empty subset of action types to map
                var shuffled = AllActionTypes.OrderBy(_ => rng.Next()).ToArray();
                int mappedCount = rng.Next(1, shuffled.Length + 1);
                var mappedTypes = shuffled.Take(mappedCount).ToArray();

                // Build BossAnimationData with those mappings
                var animData = new BossAnimationData
                {
                    attackAnimations = new List<BossAttackAnimation>()
                };

                var expectedAnims = new Dictionary<EnemyActionType, SpriteFrameAnimation>();
                for (int m = 0; m < mappedTypes.Length; m++)
                {
                    var anim = CreateAnim(m + 1); // unique frame count per mapping
                    animData.attackAnimations.Add(new BossAttackAnimation
                    {
                        actionType = mappedTypes[m],
                        animation = anim
                    });
                    expectedAnims[mappedTypes[m]] = anim;
                }

                // Verify each mapped type returns the correct animation
                foreach (var kvp in expectedAnims)
                {
                    var result = LookupAttackAnimation(animData, kvp.Key);
                    Assert.IsNotNull(result,
                        $"[Iter {i}] Lookup for mapped type {kvp.Key} should not return null");
                    Assert.AreSame(kvp.Value, result,
                        $"[Iter {i}] Lookup for {kvp.Key} should return the exact animation instance");
                }
            }
        }

        [Test]
        public void Property6_UnmappedActionTypeReturnsNull()
        {
            var rng = new System.Random(99);

            for (int i = 0; i < Iterations; i++)
            {
                // Pick a random strict subset of action types to map (leave at least one unmapped)
                var shuffled = AllActionTypes.OrderBy(_ => rng.Next()).ToArray();
                int mappedCount = rng.Next(0, shuffled.Length); // 0 to N-1
                var mappedTypes = new HashSet<EnemyActionType>(shuffled.Take(mappedCount));
                var unmappedTypes = AllActionTypes.Where(t => !mappedTypes.Contains(t)).ToArray();

                // Build BossAnimationData with only the mapped subset
                var animData = new BossAnimationData
                {
                    attackAnimations = new List<BossAttackAnimation>()
                };

                foreach (var actionType in mappedTypes)
                {
                    animData.attackAnimations.Add(new BossAttackAnimation
                    {
                        actionType = actionType,
                        animation = CreateAnim(1)
                    });
                }

                // Verify each unmapped type returns null
                foreach (var unmapped in unmappedTypes)
                {
                    var result = LookupAttackAnimation(animData, unmapped);
                    Assert.IsNull(result,
                        $"[Iter {i}] Lookup for unmapped type {unmapped} should return null");
                }
            }
        }

        [Test]
        public void Property6_NullOrEmptyAttackAnimationsReturnsNull()
        {
            var rng = new System.Random(77);

            for (int i = 0; i < Iterations; i++)
            {
                var actionType = AllActionTypes[rng.Next(AllActionTypes.Length)];

                // Null attackAnimations list
                var dataNullList = new BossAnimationData { attackAnimations = null };
                Assert.IsNull(LookupAttackAnimation(dataNullList, actionType),
                    $"[Iter {i}] Null attackAnimations should return null for {actionType}");

                // Empty attackAnimations list
                var dataEmptyList = new BossAnimationData
                {
                    attackAnimations = new List<BossAttackAnimation>()
                };
                Assert.IsNull(LookupAttackAnimation(dataEmptyList, actionType),
                    $"[Iter {i}] Empty attackAnimations should return null for {actionType}");

                // Null BossAnimationData
                Assert.IsNull(LookupAttackAnimation(null, actionType),
                    $"[Iter {i}] Null BossAnimationData should return null for {actionType}");
            }
        }

        [Test]
        public void Property6_LookupIsDeterministic()
        {
            var rng = new System.Random(55);

            for (int i = 0; i < Iterations; i++)
            {
                // Build a random mapping
                var shuffled = AllActionTypes.OrderBy(_ => rng.Next()).ToArray();
                int mappedCount = rng.Next(1, shuffled.Length + 1);

                var animData = new BossAnimationData
                {
                    attackAnimations = new List<BossAttackAnimation>()
                };

                for (int m = 0; m < mappedCount; m++)
                {
                    animData.attackAnimations.Add(new BossAttackAnimation
                    {
                        actionType = shuffled[m],
                        animation = CreateAnim(m + 1)
                    });
                }

                // Query each action type twice — results must be identical
                foreach (var actionType in AllActionTypes)
                {
                    var first = LookupAttackAnimation(animData, actionType);
                    var second = LookupAttackAnimation(animData, actionType);
                    Assert.AreSame(first, second,
                        $"[Iter {i}] Lookup for {actionType} must be deterministic");
                }
            }
        }

        #endregion
    }
}
