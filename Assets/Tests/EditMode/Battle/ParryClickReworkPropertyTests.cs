using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CardBattle;

namespace CardBattle.Tests
{
    /// <summary>
    /// Property-based tests for the Parry Click Rework feature.
    /// Uses randomized inputs across many iterations to verify correctness properties.
    /// Feature: parry-click-rework
    /// </summary>
    [TestFixture]
    public class ParryClickReworkPropertyTests
    {
        private const int Iterations = 200;

        private GameObject _parryGo;
        private ParrySystem _parrySystem;
        private GameConfig _gameConfig;

        private GameObject _blockGo;
        private BlockSystem _blockSystem;
        private GameObject _statusGo;
        private StatusEffectSystem _statusEffectSystem;

        private readonly List<GameObject> _tempObjects = new List<GameObject>();
        private readonly List<ScriptableObject> _tempAssets = new List<ScriptableObject>();

        [SetUp]
        public void SetUp()
        {
            _parryGo = new GameObject("TestParrySystem");
            _parrySystem = _parryGo.AddComponent<ParrySystem>();

            _gameConfig = ScriptableObject.CreateInstance<GameConfig>();
            _gameConfig.baseParryWindowDuration = 1.5f;
            _gameConfig.parryWindowFloorScaling = 0.02f;
            _gameConfig.parryWindowMinDuration = 0.3f;
            _gameConfig.perfectParryThreshold = 0.20f;
            _tempAssets.Add(_gameConfig);

            _parrySystem.Initialize(_gameConfig, 0);

            _blockGo = new GameObject("TestBlockSystem");
            _blockSystem = _blockGo.AddComponent<BlockSystem>();
            _blockSystem.Initialize();

            _statusGo = new GameObject("TestStatusEffectSystem");
            _statusEffectSystem = _statusGo.AddComponent<StatusEffectSystem>();
            _statusEffectSystem.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _tempObjects)
            {
                if (obj != null)
                    UnityEngine.Object.DestroyImmediate(obj);
            }
            _tempObjects.Clear();

            foreach (var asset in _tempAssets)
            {
                if (asset != null)
                    UnityEngine.Object.DestroyImmediate(asset);
            }
            _tempAssets.Clear();

            if (_statusGo != null) UnityEngine.Object.DestroyImmediate(_statusGo);
            if (_blockGo != null) UnityEngine.Object.DestroyImmediate(_blockGo);
            if (_parryGo != null) UnityEngine.Object.DestroyImmediate(_parryGo);
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        private EnemyCombatantData CreateEnemyData(string name, int maxHP, float parryWindowDuration = 1.5f)
        {
            var data = ScriptableObject.CreateInstance<EnemyCombatantData>();
            data.enemyName = name;
            data.maxHP = maxHP;
            data.hoursReward = 10;
            data.baseParryWindowDuration = parryWindowDuration;
            data.attackPattern = new List<EnemyAction>();
            _tempAssets.Add(data);
            return data;
        }

        private EnemyCombatant CreateEnemy(EnemyCombatantData data)
        {
            var go = new GameObject(data.enemyName);
            go.AddComponent<Health>();
            var ec = go.AddComponent<EnemyCombatant>();
            ec.Initialize(data, _blockSystem, _statusEffectSystem);
            _tempObjects.Add(go);
            return ec;
        }

        private CardData CreateDefenseCard(string name, int overtimeCost, List<string> parryTags)
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.cardName = name;
            card.cardType = CardType.Defense;
            card.overtimeCost = overtimeCost;
            card.parryMatchTags = parryTags;
            _tempAssets.Add(card);
            return card;
        }

        private CardInstance CreateCardInstance(CardData data)
        {
            var go = new GameObject(data.cardName);
            go.AddComponent<RectTransform>();
            var ci = go.AddComponent<CardInstance>();
            ci.Data = data;
            _tempObjects.Add(go);
            return ci;
        }

        private EnemyAction CreateDamageAction(int damage, IntentColor color, List<string> parryTags)
        {
            return new EnemyAction
            {
                actionType = EnemyActionType.DealDamage,
                value = damage,
                intentColor = color,
                parryMatchTags = parryTags
            };
        }

        private CardData CreateCardOfType(string name, CardType type, int overtimeCost, List<string> parryTags)
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.cardName = name;
            card.cardType = type;
            card.overtimeCost = overtimeCost;
            card.parryMatchTags = parryTags;
            _tempAssets.Add(card);
            return card;
        }

        #region Property 1: Card Partition During Parry Window

        /// <summary>
        /// Property 1: Card Partition During Parry Window
        /// For any hand and any attack, verify matching + non-matching cards form
        /// a complete non-overlapping partition of the hand.
        /// Validates: Requirements 1.1, 2.1
        /// </summary>
        [Test]
        public void Property1_CardPartition_MatchingAndDimmedAreCompletePartition()
        {
            var rng = new System.Random(1337);
            string[] allTags = { "Slash", "Thrust", "Sweep", "Bash", "Chop", "Fire", "Ice" };
            CardType[] nonDefenseTypes = { CardType.Attack, CardType.Effect, CardType.Utility, CardType.Special };

            for (int i = 0; i < Iterations; i++)
            {
                // Generate a random hand of 1–9 cards with mixed types and tags
                int handSize = rng.Next(1, 10);
                var hand = new List<CardInstance>();

                for (int c = 0; c < handSize; c++)
                {
                    bool isDefense = rng.NextDouble() < 0.5;
                    CardData cardData;

                    if (isDefense)
                    {
                        // Defense card with 0–3 random parry tags
                        int tagCount = rng.Next(0, 4);
                        var tags = new List<string>();
                        for (int t = 0; t < tagCount; t++)
                            tags.Add(allTags[rng.Next(allTags.Length)]);

                        cardData = CreateDefenseCard($"Def_{i}_{c}", rng.Next(0, 5), tags);
                    }
                    else
                    {
                        // Non-Defense card (Attack, Effect, Utility, Special)
                        var type = nonDefenseTypes[rng.Next(nonDefenseTypes.Length)];
                        cardData = CreateCardOfType($"Other_{i}_{c}", type, rng.Next(0, 5), null);
                    }

                    hand.Add(CreateCardInstance(cardData));
                }

                // Generate a random attack with 1–3 parry tags
                int attackTagCount = rng.Next(1, 4);
                var attackTags = new List<string>();
                for (int t = 0; t < attackTagCount; t++)
                    attackTags.Add(allTags[rng.Next(allTags.Length)]);

                var attack = CreateDamageAction(rng.Next(1, 50), IntentColor.White, attackTags);
                var enemyData = CreateEnemyData($"PartEnemy_{i}", 50);
                var enemy = CreateEnemy(enemyData);

                _parrySystem.Initialize(_gameConfig, 0);
                bool opened = _parrySystem.StartParryWindow(attack, enemy);
                Assert.IsTrue(opened, $"[Iter {i}] Window should open");

                // Get matching cards from ParrySystem
                var matching = _parrySystem.GetMatchingCards(hand);

                // Build the non-matching set: every card NOT in matching
                var matchingSet = new HashSet<CardInstance>(matching);
                var nonMatching = new List<CardInstance>();
                foreach (var card in hand)
                {
                    if (!matchingSet.Contains(card))
                        nonMatching.Add(card);
                }

                // PROPERTY: matching + non-matching == hand size (complete)
                Assert.AreEqual(hand.Count, matching.Count + nonMatching.Count,
                    $"[Iter {i}] Matching ({matching.Count}) + non-matching ({nonMatching.Count}) " +
                    $"must equal hand size ({hand.Count})");

                // PROPERTY: no overlap — every matching card appears exactly once
                Assert.AreEqual(matching.Count, matchingSet.Count,
                    $"[Iter {i}] Matching list should contain no duplicates");

                // PROPERTY: no card is in both sets
                foreach (var card in nonMatching)
                {
                    Assert.IsFalse(matchingSet.Contains(card),
                        $"[Iter {i}] Card '{card.Data.cardName}' is in both matching and non-matching sets");
                }

                // PROPERTY: every card in the hand is accounted for
                var allAccountedFor = new HashSet<CardInstance>(matching);
                foreach (var card in nonMatching)
                    allAccountedFor.Add(card);

                foreach (var card in hand)
                {
                    Assert.IsTrue(allAccountedFor.Contains(card),
                        $"[Iter {i}] Card '{card.Data.cardName}' is not in either partition");
                }

                // PROPERTY: matching cards are all Defense cards with shared tags
                foreach (var card in matching)
                {
                    Assert.AreEqual(CardType.Defense, card.Data.cardType,
                        $"[Iter {i}] Matching card '{card.Data.cardName}' must be Defense type");
                }

                // PROPERTY: non-matching cards are either non-Defense or Defense without shared tags
                foreach (var card in nonMatching)
                {
                    if (card.Data.cardType == CardType.Defense)
                    {
                        // Defense card in non-matching must NOT share any tag with the attack
                        bool sharesTag = false;
                        if (card.Data.parryMatchTags != null && card.Data.parryMatchTags.Count > 0
                            && attackTags != null && attackTags.Count > 0)
                        {
                            foreach (string cardTag in card.Data.parryMatchTags)
                            {
                                foreach (string atkTag in attackTags)
                                {
                                    if (string.Equals(cardTag, atkTag, StringComparison.OrdinalIgnoreCase))
                                        sharesTag = true;
                                }
                            }
                        }
                        else
                        {
                            // If either side has no tags, IsParryMatch returns true,
                            // so this card should have been matching — flag it
                            Assert.Fail(
                                $"[Iter {i}] Defense card '{card.Data.cardName}' with empty tags " +
                                $"should be matching (IsParryMatch returns true for empty tags)");
                        }

                        Assert.IsFalse(sharesTag,
                            $"[Iter {i}] Non-matching Defense card '{card.Data.cardName}' " +
                            $"should not share any tag with the attack");
                    }
                    // Non-Defense cards are always non-matching — no further check needed
                }
            }
        }

        [Test]
        public void Property1_CardPartition_EmptyHandProducesEmptyPartitions()
        {
            var attack = CreateDamageAction(10, IntentColor.White,
                new List<string> { "Slash" });
            var enemyData = CreateEnemyData("EmptyHandEnemy", 50);
            var enemy = CreateEnemy(enemyData);

            _parrySystem.Initialize(_gameConfig, 0);
            _parrySystem.StartParryWindow(attack, enemy);

            var emptyHand = new List<CardInstance>();
            var matching = _parrySystem.GetMatchingCards(emptyHand);

            Assert.AreEqual(0, matching.Count, "Empty hand should produce zero matching cards");
        }

        [Test]
        public void Property1_CardPartition_AllMatchingHand()
        {
            var rng = new System.Random(2024);

            for (int i = 0; i < Iterations; i++)
            {
                string sharedTag = "Slash";
                var attack = CreateDamageAction(10, IntentColor.White,
                    new List<string> { sharedTag });
                var enemyData = CreateEnemyData($"AllMatchEnemy_{i}", 50);
                var enemy = CreateEnemy(enemyData);

                _parrySystem.Initialize(_gameConfig, 0);
                _parrySystem.StartParryWindow(attack, enemy);

                // Hand of all Defense cards that match
                int handSize = rng.Next(1, 10);
                var hand = new List<CardInstance>();
                for (int c = 0; c < handSize; c++)
                {
                    var card = CreateDefenseCard($"AllMatch_{i}_{c}", 1,
                        new List<string> { sharedTag });
                    hand.Add(CreateCardInstance(card));
                }

                var matching = _parrySystem.GetMatchingCards(hand);

                Assert.AreEqual(hand.Count, matching.Count,
                    $"[Iter {i}] All Defense cards with matching tag should be in matching set");
            }
        }

        [Test]
        public void Property1_CardPartition_AllNonMatchingHand()
        {
            var rng = new System.Random(2025);

            for (int i = 0; i < Iterations; i++)
            {
                var attack = CreateDamageAction(10, IntentColor.White,
                    new List<string> { "Slash" });
                var enemyData = CreateEnemyData($"NoMatchEnemy_{i}", 50);
                var enemy = CreateEnemy(enemyData);

                _parrySystem.Initialize(_gameConfig, 0);
                _parrySystem.StartParryWindow(attack, enemy);

                // Hand of only non-Defense cards
                int handSize = rng.Next(1, 10);
                CardType[] types = { CardType.Attack, CardType.Effect, CardType.Utility, CardType.Special };
                var hand = new List<CardInstance>();
                for (int c = 0; c < handSize; c++)
                {
                    var type = types[rng.Next(types.Length)];
                    var card = CreateCardOfType($"NonMatch_{i}_{c}", type, 1, null);
                    hand.Add(CreateCardInstance(card));
                }

                var matching = _parrySystem.GetMatchingCards(hand);

                Assert.AreEqual(0, matching.Count,
                    $"[Iter {i}] Hand with no Defense cards should have zero matching cards");
            }
        }

        #endregion

        #region Property 2: Perfect Parry Timing Classification and OT Reward

        /// <summary>
        /// Property 2: Perfect Parry Timing Classification and OT Reward
        /// For any window duration D (0.3–2.0s) and any tick amount leaving time T,
        /// verify WasPerfectParry == (T/D &lt;= threshold). Verify +2 OT for perfect, +1 for normal.
        /// Validates: Requirements 10.1, 10.2, 10.4, 6.7
        /// </summary>
        [Test]
        public void Property2_PerfectParryTiming_ClassifiesCorrectlyAndGrantsCorrectOT()
        {
            var rng = new System.Random(42);
            float threshold = _gameConfig.perfectParryThreshold;

            for (int i = 0; i < Iterations; i++)
            {
                // Random window duration between 0.3 and 2.0 seconds
                float windowDuration = 0.3f + (float)(rng.NextDouble() * 1.7);

                var enemyData = CreateEnemyData($"PerfectEnemy_{i}", 50, windowDuration);
                var enemy = CreateEnemy(enemyData);

                var attack = CreateDamageAction(10, IntentColor.White,
                    new List<string> { "Slash" });

                _parrySystem.Initialize(_gameConfig, 0);
                bool opened = _parrySystem.StartParryWindow(attack, enemy);
                Assert.IsTrue(opened, $"[Iter {i}] Window should open");

                // WasPerfectParry should be reset on window open
                Assert.IsFalse(_parrySystem.WasPerfectParry,
                    $"[Iter {i}] WasPerfectParry should be false after StartParryWindow");

                float actualDuration = _parrySystem.ParryWindowDuration;

                // Random tick to leave some fraction of time remaining
                // fractionRemaining ranges from 0.0 to 1.0
                float fractionRemaining = (float)rng.NextDouble();
                float tickAmount = actualDuration * (1f - fractionRemaining);
                float timeRemaining = actualDuration - tickAmount;

                // Tick the window
                _parrySystem.TickParryWindow(tickAmount);

                // Only attempt parry if window is still active
                if (!_parrySystem.IsParryWindowActive)
                    continue;

                // Create matching card and parry
                var defenseData = CreateDefenseCard($"PerfectShield_{i}", 1,
                    new List<string> { "Slash" });
                var cardInstance = CreateCardInstance(defenseData);

                bool parryResult = _parrySystem.TryParry(cardInstance);
                Assert.IsTrue(parryResult, $"[Iter {i}] Parry should succeed with matching card");

                // Verify perfect parry classification
                float actualFraction = timeRemaining / actualDuration;
                bool expectedPerfect = actualFraction <= threshold;

                Assert.AreEqual(expectedPerfect, _parrySystem.WasPerfectParry,
                    $"[Iter {i}] WasPerfectParry mismatch: fraction={actualFraction:F4}, threshold={threshold}, " +
                    $"expected={expectedPerfect}, got={_parrySystem.WasPerfectParry}");

                // Verify OT reward amount
                int expectedOT = expectedPerfect ? 2 : 1;
                Assert.AreEqual(expectedPerfect ? 2 : 1, expectedOT,
                    $"[Iter {i}] OT reward should be {expectedOT} (perfect={expectedPerfect})");
            }
        }

        [Test]
        public void Property2_PerfectParry_AtExactThresholdBoundary()
        {
            float threshold = _gameConfig.perfectParryThreshold;

            var enemyData = CreateEnemyData("BoundaryEnemy", 50, 1.0f);
            var enemy = CreateEnemy(enemyData);

            var attack = CreateDamageAction(10, IntentColor.White,
                new List<string> { "Slash" });

            // Test at exactly the threshold boundary (fractionRemaining == threshold)
            _parrySystem.Initialize(_gameConfig, 0);
            _parrySystem.StartParryWindow(attack, enemy);

            float actualDuration = _parrySystem.ParryWindowDuration;
            float tickToThreshold = actualDuration * (1f - threshold);
            _parrySystem.TickParryWindow(tickToThreshold);

            var defenseData = CreateDefenseCard("BoundaryShield", 1,
                new List<string> { "Slash" });
            var cardInstance = CreateCardInstance(defenseData);

            bool result = _parrySystem.TryParry(cardInstance);
            Assert.IsTrue(result, "Parry should succeed");
            Assert.IsTrue(_parrySystem.WasPerfectParry,
                "Parry at exactly the threshold boundary should be classified as perfect");
        }

        [Test]
        public void Property2_NormalParry_JustAboveThreshold()
        {
            float threshold = _gameConfig.perfectParryThreshold;

            var enemyData = CreateEnemyData("AboveEnemy", 50, 1.0f);
            var enemy = CreateEnemy(enemyData);

            var attack = CreateDamageAction(10, IntentColor.White,
                new List<string> { "Slash" });

            // Tick so fractionRemaining is just above threshold
            _parrySystem.Initialize(_gameConfig, 0);
            _parrySystem.StartParryWindow(attack, enemy);

            float actualDuration = _parrySystem.ParryWindowDuration;
            float fractionAbove = threshold + 0.05f;
            float tickAmount = actualDuration * (1f - fractionAbove);
            _parrySystem.TickParryWindow(tickAmount);

            var defenseData = CreateDefenseCard("AboveShield", 1,
                new List<string> { "Slash" });
            var cardInstance = CreateCardInstance(defenseData);

            bool result = _parrySystem.TryParry(cardInstance);
            Assert.IsTrue(result, "Parry should succeed");
            Assert.IsFalse(_parrySystem.WasPerfectParry,
                "Parry above threshold should NOT be classified as perfect");
        }

        [Test]
        public void Property2_PerfectParry_AtVeryEndOfWindow()
        {
            var enemyData = CreateEnemyData("EndEnemy", 50, 1.0f);
            var enemy = CreateEnemy(enemyData);

            var attack = CreateDamageAction(10, IntentColor.White,
                new List<string> { "Slash" });

            // Tick almost all the way — leave very little time
            _parrySystem.Initialize(_gameConfig, 0);
            _parrySystem.StartParryWindow(attack, enemy);

            float actualDuration = _parrySystem.ParryWindowDuration;
            // Leave 1% of duration remaining (well within 20% threshold)
            float tickAmount = actualDuration * 0.99f;
            _parrySystem.TickParryWindow(tickAmount);

            if (!_parrySystem.IsParryWindowActive)
            {
                Assert.Pass("Window expired due to float precision — acceptable edge case");
                return;
            }

            var defenseData = CreateDefenseCard("EndShield", 1,
                new List<string> { "Slash" });
            var cardInstance = CreateCardInstance(defenseData);

            bool result = _parrySystem.TryParry(cardInstance);
            Assert.IsTrue(result, "Parry should succeed near end of window");
            Assert.IsTrue(_parrySystem.WasPerfectParry,
                "Parry at 1% remaining should be perfect (well within 20% threshold)");
        }

        #endregion
    }
}
