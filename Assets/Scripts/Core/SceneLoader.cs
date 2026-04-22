using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using CardBattle;
using Procedural;

/// <summary>
/// Singleton that persists across scenes.
/// Handles scene switching, battle transitions, and shared game state.
/// </summary>
public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance;

    public Vector3 playerPosition;
    public bool useDefaultSpawn = true;

    public Transform defaultSpawnPoint;

    // --- Legacy field kept for backward compatibility (read-only accessor) ---
    /// <summary>
    /// Returns true if any enemy has been defeated. Kept for backward
    /// compatibility with code that checks the old single-bool field.
    /// </summary>
    public bool enemyDefeated
    {
        get => _defeatedEnemyIds.Count > 0;
        set
        {
            // Legacy setter: when set to false, clear all defeated enemies.
            // When set to true, this is a no-op (use MarkEnemyDefeated instead).
            if (!value)
                _defeatedEnemyIds.Clear();
        }
    }

    // --- Defeated enemy tracking (replaces single bool) ---
    private HashSet<string> _defeatedEnemyIds = new HashSet<string>();

    /// <summary>ID of the most recently defeated enemy trigger.</summary>
    public string CurrentBattleEnemyId => _defeatedEnemyId;
    private string _defeatedEnemyId;

    /// <summary>Check whether a specific enemy trigger has been defeated.</summary>
    public bool IsEnemyDefeated(string enemyId)
    {
        return !string.IsNullOrEmpty(enemyId) && _defeatedEnemyIds.Contains(enemyId);
    }

    /// <summary>Mark a specific enemy trigger as defeated.</summary>
    public void MarkEnemyDefeated(string enemyId)
    {
        if (!string.IsNullOrEmpty(enemyId))
            _defeatedEnemyIds.Add(enemyId);
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            useDefaultSpawn = true; // First load always uses PlayerSpawn marker
            playerPosition = Vector3.zero; // Clear any stale position
            if (defaultSpawnPoint != null)
                playerPosition = defaultSpawnPoint.position;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Explorationscene")
        {
            // If run was wiped (death) and we're on floor 1 with no custom spawn,
            // force default spawn — but NOT if we're returning from a won battle
            // (useDefaultSpawn == false means OnBattleVictory set it)
            SaveManager smCheck = FindObjectOfType<SaveManager>();
            if (useDefaultSpawn && smCheck != null && smCheck.CurrentRun != null && smCheck.CurrentRun.currentFloor == 1 && !smCheck.CurrentRun.hasCustomSpawn)
                useDefaultSpawn = true;
            // Regenerate the floor for the current run floor number
            LevelGenerator gen = FindObjectOfType<LevelGenerator>();
            if (gen != null)
            {
                SaveManager sm = FindObjectOfType<SaveManager>();
                int floor = sm != null && sm.CurrentRun != null ? sm.CurrentRun.currentFloor : 1;
                Debug.Log($"[SceneLoader] OnSceneLoaded: generating floor {floor}");
                gen.Generate(floor);

                // Only set elevator spawn if we don't already have a custom spawn
                // (the elevator sets hasCustomSpawn before loading the scene)
                if (sm != null && sm.CurrentRun != null && floor > 1 && !sm.CurrentRun.hasCustomSpawn)
                {
                    sm.CurrentRun.spawnX = gen.ElevatorSpawnPosition.x;
                    sm.CurrentRun.spawnZ = gen.ElevatorSpawnPosition.z;
                    sm.CurrentRun.hasCustomSpawn = true;
                }
            }
            StartCoroutine(SetSpawn());
        }
    }

    private IEnumerator SetSpawn()
    {
        yield return null;

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            Debug.LogError("Player not found");
            yield break;
        }

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        SaveManager sm = FindObjectOfType<SaveManager>();
        bool freshRun = sm == null || sm.CurrentRun == null || sm.CurrentRun.currentFloor <= 1;

        // Only force default spawn on truly fresh runs, not when returning from battle
        // useDefaultSpawn == false means we're returning from a won fight with a saved position
        if (freshRun && useDefaultSpawn)
            useDefaultSpawn = true;

        if (useDefaultSpawn)
        {
            if (sm != null && sm.CurrentRun != null && sm.CurrentRun.hasCustomSpawn)
            {
                Vector3 floorSpawn = new Vector3(sm.CurrentRun.spawnX, player.transform.position.y, sm.CurrentRun.spawnZ);
                player.transform.position = floorSpawn;
                sm.CurrentRun.hasCustomSpawn = false;
                Debug.Log("Spawned at floor transition position: " + floorSpawn);
            }
            else
            {
                GameObject playerSpawnMarker = GameObject.FindWithTag("PlayerSpawn");
                if (playerSpawnMarker != null)
                {
                    player.transform.position = playerSpawnMarker.transform.position;
                    player.transform.rotation = playerSpawnMarker.transform.rotation;
                    Debug.Log($"[Spawn] At PlayerSpawn marker: {player.transform.position}");
                }
                else if (defaultSpawnPoint != null)
                {
                    player.transform.position = defaultSpawnPoint.position;
                    Debug.Log("Spawned at DEFAULT");
                }
            }
        }
        else
        {
            player.transform.position = playerPosition;
            Debug.Log("Spawned at SAVED position (won fight): " + playerPosition);
        }

        useDefaultSpawn = false;
        if (cc != null) cc.enabled = true;
    }

    // --- Battle Transition API ---

    /// <summary>
    /// Load the battle scene with encounter data. Snapshots pre-encounter state,
    /// passes encounter to BattleManager, and transitions to the battle scene.
    /// </summary>
    public void LoadBattle(EncounterData encounter, string enemyId = null)
    {
        // Save player position before leaving exploration
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
            playerPosition = player.transform.position;

        // Track which enemy trigger initiated this battle
        _defeatedEnemyId = enemyId;

        // Snapshot pre-encounter state for mid-combat quit recovery
        if (SaveManager.Instance != null)
            SaveManager.Instance.SnapshotPreEncounter();

        // Pass encounter data to BattleManager before scene loads
        if (encounter != null)
            BattleManager.SetPendingEncounter(encounter);

        // Unlock cursor for battle UI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Hide first-person hands before the scene transition (Req 18.8)
        var hands = FindFirstObjectByType<CardBattle.FirstPersonHandsController>();
        if (hands != null) hands.HideHands();

        // Capture the exploration scene as a background image before
        // transitioning — needs end-of-frame so the frame is fully rendered.
        StartCoroutine(CaptureBackgroundAndLoadBattle());
    }

    private IEnumerator CaptureBackgroundAndLoadBattle()
    {
        yield return new WaitForEndOfFrame();
        BattleBackground.CaptureBackground();

        if (LoadingScreen.Instance != null)
            LoadingScreen.Instance.LoadBattle("Battlescene");
        else if (FindObjectOfType<LoadingScreen>() is LoadingScreen ls1)
            ls1.LoadBattle("Battlescene");
        else
            SceneManager.LoadScene("Battlescene");
    }

    /// <summary>
    /// Legacy no-arg LoadBattle for backward compatibility and testing.
    /// Does not pass encounter data or snapshot state.
    /// </summary>
    public void LoadBattle()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
            playerPosition = player.transform.position;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (LoadingScreen.Instance != null)
            LoadingScreen.Instance.LoadBattle("Battlescene");
        else if (FindObjectOfType<LoadingScreen>() is LoadingScreen ls2)
            ls2.LoadBattle("Battlescene");
        else
            SceneManager.LoadScene("Battlescene");
    }

    /// <summary>
    /// Called when the player wins a battle. Marks the enemy as defeated,
    /// updates RunState with earned rewards, and returns to exploration.
    /// </summary>
    public void OnBattleVictory(string enemyId, int hoursEarned, int badReviewsEarned)
    {
        // Mark the specific enemy as defeated
        MarkEnemyDefeated(enemyId);

        // Update RunState with earned rewards
        if (SaveManager.Instance != null)
        {
            RunState run = SaveManager.Instance.CurrentRun;
            if (run != null)
            {
                run.hours += hoursEarned;
                run.hoursEarnedTotal += hoursEarned;
                run.badReviewsEarnedTotal += badReviewsEarned;
                run.enemiesDefeated++;
            }

            // Also update meta state for Bad Reviews (persistent currency)
            if (badReviewsEarned > 0 && SaveManager.Instance.CurrentMeta != null)
            {
                SaveManager.Instance.CurrentMeta.badReviews += badReviewsEarned;
            }

            SaveManager.Instance.SaveRun();
            SaveManager.Instance.SaveMeta();
        }

        // Restore player to pre-battle position
        useDefaultSpawn = false;

        LoadExploration();
    }

    /// <summary>
    /// Called when the player loses a battle. Wipes run state and
    /// returns to exploration (death screen handled separately).
    /// </summary>
    public void OnBattleDefeat()
    {
        // Cache run stats for the GameOverScreen before wiping
        if (SaveManager.Instance != null)
            DeathScreen.CacheRunStats(SaveManager.Instance.CurrentRun);

        // Wipe run state (death resets the run)
        if (SaveManager.Instance != null)
            SaveManager.Instance.WipeRun();

        // Spawn at default position on a fresh run
        useDefaultSpawn = true;

        // Clear defeated enemies for the new run
        _defeatedEnemyIds.Clear();
        _defeatedEnemyId = null;

        LoadDeath();
    }

    public void LoadExploration()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (LoadingScreen.Instance != null)
            LoadingScreen.Instance.LoadSceneWithFade("Explorationscene");
        else if (FindObjectOfType<LoadingScreen>() is LoadingScreen ls3)
            ls3.LoadSceneWithFade("Explorationscene");
        else
            SceneManager.LoadScene("Explorationscene");
    }

    public void LoadDeath()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene("Death");
    }
    public void LoadSceneUI(string sceneName)
    {
        if(LoadingScreen.Instance != null)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            LoadingScreen.Instance.LoadSceneWithFade(sceneName);
        }
        else if (FindObjectOfType<LoadingScreen>() is LoadingScreen ls)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            ls.LoadSceneWithFade(sceneName);
        }
        else
        {
            // Direct load — cursor will be handled by the target scene
            SceneManager.LoadScene(sceneName);
        }
    }
    public void LoadSceneMenu(string sceneName)
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (LoadingScreen.Instance != null)
            LoadingScreen.Instance.LoadSceneWithFade(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }
}
