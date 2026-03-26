using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using CardBattle;

namespace CardBattle.Tests
{
    /// <summary>
    /// Property-based tests for persistence: SaveManager, RunState, MetaState.
    /// Uses randomized inputs across many iterations to verify correctness properties
    /// for death wipe, round-trip serialization, cutscene flag persistence, and mid-combat snapshots.
    /// </summary>
    [TestFixture]
    public class RunStatePropertyTests
    {
        private const int Iterations = 200;

        private GameObject _go;
        private SaveManager _saveManager;

        [SetUp]
        public void SetUp()
        {
            // Destroy any existing singleton first
            if (SaveManager.Instance != null)
            {
                UnityEngine.Object.DestroyImmediate(SaveManager.Instance.gameObject);
            }

            _go = new GameObject("TestSaveManager");
            _saveManager = _go.AddComponent<SaveManager>();
            // Awake() doesn't fire in edit mode tests, so initialize manually
            _saveManager.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up save files
            CleanUpFile("run_save.json");
            CleanUpFile("meta_save.json");
            CleanUpFile("pre_encounter_save.json");

            if (_go != null)
                UnityEngine.Object.DestroyImmediate(_go);
        }

        private void CleanUpFile(string filename)
        {
            try
            {
                string path = Path.Combine(Application.persistentDataPath, filename);
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception) { }
        }

        private RunState CreateRandomRunState(System.Random rng)
        {
            var state = new RunState();
            state.currentFloor = rng.Next(1, 26);
            state.playerHP = rng.Next(1, 81);
            state.playerMaxHP = rng.Next(state.playerHP, 120);
            state.hours = rng.Next(0, 500);
            state.hoursEarnedTotal = rng.Next(0, 1000);
            state.badReviewsEarnedTotal = rng.Next(0, 100);
            state.isActive = rng.Next(0, 2) == 1;
            state.enemiesDefeated = rng.Next(0, 50);
            state.cardRemovalsThisRun = rng.Next(0, 10);
            state.startingDeckSetId = $"deck_set_{rng.Next(0, 5)}";

            // Random deck card IDs
            int deckSize = rng.Next(1, 25);
            state.deckCardIds = new List<string>();
            for (int i = 0; i < deckSize; i++)
                state.deckCardIds.Add($"card_{rng.Next(0, 100)}");

            // Random tool IDs
            int toolCount = rng.Next(0, 6);
            state.toolIds = new List<string>();
            for (int i = 0; i < toolCount; i++)
                state.toolIds.Add($"tool_{rng.Next(0, 20)}");

            // Random cutscene IDs
            int cutsceneCount = rng.Next(0, 5);
            state.seenCutsceneIds = new List<string>();
            for (int i = 0; i < cutsceneCount; i++)
                state.seenCutsceneIds.Add($"cutscene_{rng.Next(0, 10)}");

            return state;
        }

        private MetaState CreateRandomMetaState(System.Random rng)
        {
            var meta = new MetaState();
            meta.badReviews = rng.Next(0, 500);
            meta.tutorialCompleted = rng.Next(0, 2) == 1;

            // Random hub upgrade levels using StringIntPair
            int upgradeCount = rng.Next(0, 6);
            meta.hubUpgradeLevels = new List<StringIntPair>();
            string[] upgradeNames = { "Computer", "CoffeeMachine", "DeskChair", "FilingCabinet", "Plant", "Whiteboard" };
            for (int i = 0; i < upgradeCount && i < upgradeNames.Length; i++)
            {
                meta.hubUpgradeLevels.Add(new StringIntPair
                {
                    key = upgradeNames[i],
                    value = rng.Next(0, 4)
                });
            }

            // Random achievements
            int achievementCount = rng.Next(0, 5);
            meta.unlockedAchievements = new List<string>();
            for (int i = 0; i < achievementCount; i++)
                meta.unlockedAchievements.Add($"achievement_{rng.Next(0, 20)}");

            return meta;
        }

        private bool RunStatesAreEqual(RunState a, RunState b)
        {
            if (a.currentFloor != b.currentFloor) return false;
            if (a.playerHP != b.playerHP) return false;
            if (a.playerMaxHP != b.playerMaxHP) return false;
            if (a.hours != b.hours) return false;
            if (a.hoursEarnedTotal != b.hoursEarnedTotal) return false;
            if (a.badReviewsEarnedTotal != b.badReviewsEarnedTotal) return false;
            if (a.isActive != b.isActive) return false;
            if (a.enemiesDefeated != b.enemiesDefeated) return false;
            if (a.cardRemovalsThisRun != b.cardRemovalsThisRun) return false;
            if (a.startingDeckSetId != b.startingDeckSetId) return false;

            if (!ListsAreEqual(a.deckCardIds, b.deckCardIds)) return false;
            if (!ListsAreEqual(a.toolIds, b.toolIds)) return false;
            if (!ListsAreEqual(a.seenCutsceneIds, b.seenCutsceneIds)) return false;

            return true;
        }

        private bool ListsAreEqual(List<string> a, List<string> b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }

        #region Property 22: Death Wipes Run State, Preserves Meta State

        /// <summary>
        /// Property 22: After death (WipeRun()), run state is wiped (isActive=false, empty deck,
        /// 0 hours, etc.), but Bad_Reviews and hub upgrades in MetaState are preserved unchanged.
        /// Validates: Requirements 24.1, 24.2, 24.3
        /// </summary>
        [Test]
        public void Property22_DeathWipesRunState_PreservesMetaState()
        {
            var rng = new System.Random(42);

            for (int i = 0; i < Iterations; i++)
            {
                // Set up random run state and meta state
                var runState = CreateRandomRunState(rng);
                runState.isActive = true;
                runState.hours = rng.Next(1, 500);
                runState.deckCardIds = new List<string> { "card_1", "card_2", "card_3" };
                runState.toolIds = new List<string> { "tool_1" };

                // Apply to SaveManager
                _saveManager.CurrentRun.currentFloor = runState.currentFloor;
                _saveManager.CurrentRun.playerHP = runState.playerHP;
                _saveManager.CurrentRun.playerMaxHP = runState.playerMaxHP;
                _saveManager.CurrentRun.hours = runState.hours;
                _saveManager.CurrentRun.hoursEarnedTotal = runState.hoursEarnedTotal;
                _saveManager.CurrentRun.badReviewsEarnedTotal = runState.badReviewsEarnedTotal;
                _saveManager.CurrentRun.isActive = runState.isActive;
                _saveManager.CurrentRun.enemiesDefeated = runState.enemiesDefeated;
                _saveManager.CurrentRun.cardRemovalsThisRun = runState.cardRemovalsThisRun;
                _saveManager.CurrentRun.startingDeckSetId = runState.startingDeckSetId;
                _saveManager.CurrentRun.deckCardIds = new List<string>(runState.deckCardIds);
                _saveManager.CurrentRun.toolIds = new List<string>(runState.toolIds);
                _saveManager.CurrentRun.seenCutsceneIds = runState.seenCutsceneIds != null
                    ? new List<string>(runState.seenCutsceneIds) : null;

                // Set up random meta state
                var metaState = CreateRandomMetaState(rng);
                _saveManager.CurrentMeta.badReviews = metaState.badReviews;
                _saveManager.CurrentMeta.tutorialCompleted = metaState.tutorialCompleted;
                _saveManager.CurrentMeta.hubUpgradeLevels = new List<StringIntPair>();
                if (metaState.hubUpgradeLevels != null)
                {
                    foreach (var pair in metaState.hubUpgradeLevels)
                        _saveManager.CurrentMeta.hubUpgradeLevels.Add(new StringIntPair { key = pair.key, value = pair.value });
                }
                _saveManager.CurrentMeta.unlockedAchievements = metaState.unlockedAchievements != null
                    ? new List<string>(metaState.unlockedAchievements) : null;

                // Capture meta state before wipe
                int badReviewsBefore = _saveManager.CurrentMeta.badReviews;
                bool tutorialBefore = _saveManager.CurrentMeta.tutorialCompleted;
                var upgradesBefore = new List<StringIntPair>();
                if (_saveManager.CurrentMeta.hubUpgradeLevels != null)
                {
                    foreach (var pair in _saveManager.CurrentMeta.hubUpgradeLevels)
                        upgradesBefore.Add(new StringIntPair { key = pair.key, value = pair.value });
                }

                // Perform death wipe
                _saveManager.WipeRun();

                // Verify run state is wiped to defaults
                var wiped = _saveManager.CurrentRun;
                Assert.AreEqual(0, wiped.currentFloor,
                    $"[Iter {i}] currentFloor should be 0 after wipe");
                Assert.AreEqual(0, wiped.playerHP,
                    $"[Iter {i}] playerHP should be 0 after wipe");
                Assert.AreEqual(0, wiped.hours,
                    $"[Iter {i}] hours should be 0 after wipe");
                Assert.IsFalse(wiped.isActive,
                    $"[Iter {i}] isActive should be false after wipe");

                // Verify deck and tools are empty/null
                Assert.IsTrue(wiped.deckCardIds == null || wiped.deckCardIds.Count == 0,
                    $"[Iter {i}] deckCardIds should be empty after wipe");
                Assert.IsTrue(wiped.toolIds == null || wiped.toolIds.Count == 0,
                    $"[Iter {i}] toolIds should be empty after wipe");

                // Verify meta state is preserved
                Assert.AreEqual(badReviewsBefore, _saveManager.CurrentMeta.badReviews,
                    $"[Iter {i}] Bad_Reviews should be preserved after death");
                Assert.AreEqual(tutorialBefore, _saveManager.CurrentMeta.tutorialCompleted,
                    $"[Iter {i}] tutorialCompleted should be preserved after death");

                // Verify hub upgrades preserved
                var upgradesAfter = _saveManager.CurrentMeta.hubUpgradeLevels;
                Assert.AreEqual(upgradesBefore.Count, upgradesAfter != null ? upgradesAfter.Count : 0,
                    $"[Iter {i}] Hub upgrade count should be preserved after death");
                for (int j = 0; j < upgradesBefore.Count; j++)
                {
                    Assert.AreEqual(upgradesBefore[j].key, upgradesAfter[j].key,
                        $"[Iter {i}] Hub upgrade key {j} should be preserved");
                    Assert.AreEqual(upgradesBefore[j].value, upgradesAfter[j].value,
                        $"[Iter {i}] Hub upgrade value {j} should be preserved");
                }
            }
        }

        #endregion

        #region Property 23: Run State Persistence Round Trip

        /// <summary>
        /// Property 23: Serialize then deserialize RunState produces identical RunState.
        /// For any randomly generated RunState, SaveRun() then LoadRun() should produce
        /// an equivalent state with all fields matching.
        /// Validates: Requirements 27.5, 27.6
        /// </summary>
        [Test]
        public void Property23_RunStatePersistenceRoundTrip_ViaJsonUtility()
        {
            var rng = new System.Random(99);

            for (int i = 0; i < Iterations; i++)
            {
                var original = CreateRandomRunState(rng);

                // Serialize and deserialize via JsonUtility (same mechanism SaveManager uses)
                string json = JsonUtility.ToJson(original, true);
                var deserialized = JsonUtility.FromJson<RunState>(json);

                Assert.IsNotNull(deserialized,
                    $"[Iter {i}] Deserialized RunState should not be null");
                Assert.IsTrue(RunStatesAreEqual(original, deserialized),
                    $"[Iter {i}] Round-trip RunState should match original. " +
                    $"Floor: {original.currentFloor}=={deserialized.currentFloor}, " +
                    $"HP: {original.playerHP}=={deserialized.playerHP}, " +
                    $"Hours: {original.hours}=={deserialized.hours}, " +
                    $"Active: {original.isActive}=={deserialized.isActive}, " +
                    $"DeckSize: {original.deckCardIds?.Count}=={deserialized.deckCardIds?.Count}");
            }
        }

        [Test]
        public void Property23_RunStatePersistenceRoundTrip_ViaSaveManager()
        {
            var rng = new System.Random(123);

            for (int i = 0; i < Iterations; i++)
            {
                var original = CreateRandomRunState(rng);

                // Copy state into SaveManager's CurrentRun
                CopyRunState(original, _saveManager.CurrentRun);

                // Save to disk
                _saveManager.SaveRun();

                // Load from disk
                _saveManager.LoadRun();

                var loaded = _saveManager.CurrentRun;
                Assert.IsNotNull(loaded,
                    $"[Iter {i}] Loaded RunState should not be null");
                Assert.IsTrue(RunStatesAreEqual(original, loaded),
                    $"[Iter {i}] SaveRun/LoadRun round-trip should produce identical state. " +
                    $"Floor: {original.currentFloor}=={loaded.currentFloor}, " +
                    $"HP: {original.playerHP}=={loaded.playerHP}, " +
                    $"Hours: {original.hours}=={loaded.hours}");
            }
        }

        private void CopyRunState(RunState source, RunState target)
        {
            target.currentFloor = source.currentFloor;
            target.playerHP = source.playerHP;
            target.playerMaxHP = source.playerMaxHP;
            target.hours = source.hours;
            target.hoursEarnedTotal = source.hoursEarnedTotal;
            target.badReviewsEarnedTotal = source.badReviewsEarnedTotal;
            target.isActive = source.isActive;
            target.enemiesDefeated = source.enemiesDefeated;
            target.cardRemovalsThisRun = source.cardRemovalsThisRun;
            target.startingDeckSetId = source.startingDeckSetId;
            target.deckCardIds = source.deckCardIds != null ? new List<string>(source.deckCardIds) : null;
            target.toolIds = source.toolIds != null ? new List<string>(source.toolIds) : null;
            target.seenCutsceneIds = source.seenCutsceneIds != null ? new List<string>(source.seenCutsceneIds) : null;
        }

        #endregion

        #region Property 24: Cutscene Seen Flag Persistence

        /// <summary>
        /// Property 24: Seen cutscene stays seen on load (MarkCutsceneSeen persists through
        /// SaveRun/LoadRun). New run (WipeRun) clears cutscene flags.
        /// Validates: Requirements 27.1, 27.2, 27.3, 27.4
        /// </summary>
        [Test]
        public void Property24_CutsceneSeenFlagPersistence_SurvivesRoundTrip()
        {
            var rng = new System.Random(200);

            for (int i = 0; i < Iterations; i++)
            {
                // Generate random cutscene IDs to mark as seen
                int cutsceneCount = rng.Next(1, 8);
                var cutsceneIds = new List<string>();
                for (int c = 0; c < cutsceneCount; c++)
                    cutsceneIds.Add($"cutscene_{rng.Next(0, 50)}_{i}_{c}");

                // Ensure fresh run state
                _saveManager.CurrentRun.seenCutsceneIds = new List<string>();

                // Mark cutscenes as seen (this also calls SaveRun internally)
                foreach (var id in cutsceneIds)
                    _saveManager.MarkCutsceneSeen(id);

                // Verify all are seen before save/load
                foreach (var id in cutsceneIds)
                {
                    Assert.IsTrue(_saveManager.HasSeenCutscene(id),
                        $"[Iter {i}] Cutscene '{id}' should be seen after marking");
                }

                // Load from disk
                _saveManager.LoadRun();

                // Verify all are still seen after load
                foreach (var id in cutsceneIds)
                {
                    Assert.IsTrue(_saveManager.HasSeenCutscene(id),
                        $"[Iter {i}] Cutscene '{id}' should still be seen after LoadRun");
                }
            }
        }

        [Test]
        public void Property24_CutsceneSeenFlags_ClearedOnWipeRun()
        {
            var rng = new System.Random(300);

            for (int i = 0; i < Iterations; i++)
            {
                // Mark some cutscenes as seen
                int cutsceneCount = rng.Next(1, 6);
                var cutsceneIds = new List<string>();

                _saveManager.CurrentRun.seenCutsceneIds = new List<string>();

                for (int c = 0; c < cutsceneCount; c++)
                {
                    string id = $"cutscene_wipe_{rng.Next(0, 50)}_{i}_{c}";
                    cutsceneIds.Add(id);
                    _saveManager.MarkCutsceneSeen(id);
                }

                // Verify they are seen
                foreach (var id in cutsceneIds)
                {
                    Assert.IsTrue(_saveManager.HasSeenCutscene(id),
                        $"[Iter {i}] Cutscene '{id}' should be seen before wipe");
                }

                // Wipe run (simulates death / new run)
                _saveManager.WipeRun();

                // Verify all cutscene flags are cleared
                foreach (var id in cutsceneIds)
                {
                    Assert.IsFalse(_saveManager.HasSeenCutscene(id),
                        $"[Iter {i}] Cutscene '{id}' should NOT be seen after WipeRun (new run clears flags)");
                }
            }
        }

        #endregion

        #region Property 41: Mid-Combat Save Restores Pre-Encounter State

        /// <summary>
        /// Property 41: SnapshotPreEncounter() saves current RunState. If RunState is modified
        /// after snapshot (simulating combat changes), RestorePreEncounter() restores the
        /// pre-combat state. The restored state should match the state at snapshot time,
        /// not the modified state.
        /// Validates: Requirements 27.7
        /// </summary>
        [Test]
        public void Property41_MidCombatSave_RestoresPreEncounterState()
        {
            var rng = new System.Random(400);

            for (int i = 0; i < Iterations; i++)
            {
                // Create a random pre-encounter state
                var preEncounterState = CreateRandomRunState(rng);
                preEncounterState.isActive = true;

                // Copy into SaveManager
                CopyRunState(preEncounterState, _saveManager.CurrentRun);

                // Take pre-encounter snapshot
                _saveManager.SnapshotPreEncounter();

                // Simulate combat modifications to the run state
                _saveManager.CurrentRun.playerHP = Math.Max(0, _saveManager.CurrentRun.playerHP - rng.Next(1, 30));
                _saveManager.CurrentRun.hours += rng.Next(0, 50);
                _saveManager.CurrentRun.enemiesDefeated += rng.Next(1, 5);
                _saveManager.CurrentRun.currentFloor += rng.Next(0, 3);
                if (_saveManager.CurrentRun.deckCardIds == null)
                    _saveManager.CurrentRun.deckCardIds = new List<string>();
                _saveManager.CurrentRun.deckCardIds.Add($"combat_card_{i}");

                // Verify state has been modified
                Assert.IsFalse(RunStatesAreEqual(preEncounterState, _saveManager.CurrentRun),
                    $"[Iter {i}] State should be different after combat modifications");

                // Restore pre-encounter snapshot
                _saveManager.RestorePreEncounter();

                // Verify restored state matches the pre-encounter state
                Assert.IsTrue(RunStatesAreEqual(preEncounterState, _saveManager.CurrentRun),
                    $"[Iter {i}] Restored state should match pre-encounter snapshot. " +
                    $"Floor: {preEncounterState.currentFloor}=={_saveManager.CurrentRun.currentFloor}, " +
                    $"HP: {preEncounterState.playerHP}=={_saveManager.CurrentRun.playerHP}, " +
                    $"Hours: {preEncounterState.hours}=={_saveManager.CurrentRun.hours}, " +
                    $"Defeated: {preEncounterState.enemiesDefeated}=={_saveManager.CurrentRun.enemiesDefeated}");

                // Verify snapshot file is cleaned up after restore
                Assert.IsFalse(_saveManager.HasPreEncounterSnapshot(),
                    $"[Iter {i}] Pre-encounter snapshot file should be deleted after restore");
            }
        }

        [Test]
        public void Property41_RestorePreEncounter_NoSnapshot_DoesNotCorruptState()
        {
            var rng = new System.Random(500);

            for (int i = 0; i < Iterations; i++)
            {
                var currentState = CreateRandomRunState(rng);
                CopyRunState(currentState, _saveManager.CurrentRun);

                // Ensure no snapshot exists
                CleanUpFile("pre_encounter_save.json");

                // Attempt restore with no snapshot
                _saveManager.RestorePreEncounter();

                // State should remain unchanged
                Assert.IsTrue(RunStatesAreEqual(currentState, _saveManager.CurrentRun),
                    $"[Iter {i}] State should be unchanged when no snapshot exists");
            }
        }

        #endregion
    }
}
