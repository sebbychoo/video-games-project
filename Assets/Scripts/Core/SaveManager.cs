using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// Singleton MonoBehaviour that persists across scenes.
    /// Handles JSON serialization of RunState and MetaState to disk.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        private const string RunSaveFile = "run_save.json";
        private const string MetaSaveFile = "meta_save.json";
        private const string PreEncounterSaveFile = "pre_encounter_save.json";

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void JS_FileSystem_Sync();
#endif

        /// <summary>
        /// Flushes the in-memory filesystem to IndexedDB on WebGL.
        /// No-op on other platforms.
        /// </summary>
        private void SyncFileSystem()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try { JS_FileSystem_Sync(); }
            catch (Exception e) { Debug.LogWarning($"SaveManager: IndexedDB sync failed: {e.Message}"); }
#endif
        }

        /// <summary>Current in-progress run state.</summary>
        public RunState CurrentRun { get; private set; }

        /// <summary>Persistent meta-progression state.</summary>
        public MetaState CurrentMeta { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                if (Application.isPlaying)
                    DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Initializes the SaveManager state. Called automatically from Awake(),
        /// but can also be called manually for edit mode test setup.
        /// </summary>
        public void Initialize()
        {
            Instance = this;
            CurrentRun = new RunState();
            CurrentRun.runSeed = System.Environment.TickCount;
            CurrentMeta = new MetaState();
        }

        private string GetPath(string filename)
        {
            return Path.Combine(Application.persistentDataPath, filename);
        }

        // --- Run State ---

        public void SaveRun()
        {
            try
            {
                string json = JsonUtility.ToJson(CurrentRun, true);
                File.WriteAllText(GetPath(RunSaveFile), json);
                SyncFileSystem();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"SaveManager: Failed to save run state: {e.Message}");
            }
        }

        public void LoadRun()
        {
            string path = GetPath(RunSaveFile);
            if (!File.Exists(path))
            {
                Debug.Log("SaveManager: No run save file found, starting fresh.");
                CurrentRun = new RunState();
                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                CurrentRun = JsonUtility.FromJson<RunState>(json);
                if (CurrentRun == null)
                {
                    Debug.LogWarning("SaveManager: Run save deserialized to null, starting fresh.");
                    CurrentRun = new RunState();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"SaveManager: Corrupted run save file, starting fresh: {e.Message}");
                CurrentRun = new RunState();
            }
        }

        // --- Meta State ---

        public void SaveMeta()
        {
            try
            {
                string json = JsonUtility.ToJson(CurrentMeta, true);
                File.WriteAllText(GetPath(MetaSaveFile), json);
                SyncFileSystem();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"SaveManager: Failed to save meta state: {e.Message}");
            }
        }

        public void LoadMeta()
        {
            string path = GetPath(MetaSaveFile);
            if (!File.Exists(path))
            {
                Debug.Log("SaveManager: No meta save file found, starting fresh.");
                CurrentMeta = new MetaState();
                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                CurrentMeta = JsonUtility.FromJson<MetaState>(json);
                if (CurrentMeta == null)
                {
                    Debug.LogWarning("SaveManager: Meta save deserialized to null, starting fresh.");
                    CurrentMeta = new MetaState();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"SaveManager: Corrupted meta save file, starting fresh: {e.Message}");
                CurrentMeta = new MetaState();
            }
        }

        // --- Run Lifecycle ---

        /// <summary>
        /// Wipes the current run state (death or new game) but preserves meta state.
        /// Clears the run save file from disk.
        /// </summary>
        public void WipeRun()
        {
            CurrentRun = new RunState();
            CurrentRun.runSeed = System.Environment.TickCount;

            string path = GetPath(RunSaveFile);
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
                SyncFileSystem();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"SaveManager: Failed to delete run save file: {e.Message}");
            }

            // Also clean up any pre-encounter snapshot
            string snapshotPath = GetPath(PreEncounterSaveFile);
            try
            {
                if (File.Exists(snapshotPath))
                    File.Delete(snapshotPath);
                SyncFileSystem();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"SaveManager: Failed to delete pre-encounter snapshot: {e.Message}");
            }
        }

        // --- Pre-Encounter Snapshot (mid-combat quit recovery) ---

        /// <summary>
        /// Saves a snapshot of the current RunState before entering an encounter.
        /// If the player quits mid-combat, this snapshot is used to restore
        /// the pre-battle state so they resume in exploration.
        /// </summary>
        public void SnapshotPreEncounter()
        {
            try
            {
                string json = JsonUtility.ToJson(CurrentRun, true);
                File.WriteAllText(GetPath(PreEncounterSaveFile), json);
                SyncFileSystem();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"SaveManager: Failed to save pre-encounter snapshot: {e.Message}");
            }
        }

        /// <summary>
        /// Restores RunState from the pre-encounter snapshot.
        /// Used when the player quit mid-combat and relaunches.
        /// The player resumes in exploration at the pre-battle position.
        /// </summary>
        public void RestorePreEncounter()
        {
            string path = GetPath(PreEncounterSaveFile);
            if (!File.Exists(path))
            {
                Debug.Log("SaveManager: No pre-encounter snapshot found.");
                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                RunState snapshot = JsonUtility.FromJson<RunState>(json);
                if (snapshot != null)
                {
                    CurrentRun = snapshot;
                }
                else
                {
                    Debug.LogWarning("SaveManager: Pre-encounter snapshot deserialized to null.");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"SaveManager: Corrupted pre-encounter snapshot: {e.Message}");
            }

            // Clean up the snapshot file after restoring
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
                SyncFileSystem();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"SaveManager: Failed to delete pre-encounter snapshot after restore: {e.Message}");
            }
        }

        /// <summary>
        /// Checks whether a pre-encounter snapshot exists on disk.
        /// Used at game launch to detect mid-combat quit.
        /// </summary>
        public bool HasPreEncounterSnapshot()
        {
            return File.Exists(GetPath(PreEncounterSaveFile));
        }

        // --- Cutscene Flags ---

        /// <summary>
        /// Marks a cutscene as seen in the current run state and persists.
        /// </summary>
        public void MarkCutsceneSeen(string cutsceneId)
        {
            if (CurrentRun.seenCutsceneIds == null)
                CurrentRun.seenCutsceneIds = new System.Collections.Generic.List<string>();

            if (!CurrentRun.seenCutsceneIds.Contains(cutsceneId))
            {
                CurrentRun.seenCutsceneIds.Add(cutsceneId);
                SaveRun();
            }
        }

        /// <summary>
        /// Checks whether a cutscene has been seen in the current run.
        /// </summary>
        public bool HasSeenCutscene(string cutsceneId)
        {
            if (CurrentRun.seenCutsceneIds == null)
                return false;

            return CurrentRun.seenCutsceneIds.Contains(cutsceneId);
        }
    }
}
