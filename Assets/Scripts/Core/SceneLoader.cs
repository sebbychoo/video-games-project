using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using CardBattle;

/// <summary>
/// Singleton that persists across scenes.
/// Handles scene switching, battle transitions, and shared game state.
/// </summary>
public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance;

    public Vector3 playerPosition;
    public bool useDefaultSpawn = false;

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

        if (useDefaultSpawn)
        {
            // LOST: spawn at default
            if (defaultSpawnPoint != null)
            {
                player.transform.position = defaultSpawnPoint.position;
                Debug.Log("Spawned at DEFAULT (lost fight)");
            }
            else
            {
                Debug.LogWarning("Default spawn point not assigned!");
            }
        }
        else
        {
            // WON: spawn where player was
            player.transform.position = playerPosition;
            Debug.Log("Spawned at SAVED position (won fight): " + playerPosition);
        }

        // reset for next time
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

        // Unlock cursor for battle UI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

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
        // Wipe run state (death resets the run)
        if (SaveManager.Instance != null)
            SaveManager.Instance.WipeRun();

        // Spawn at default position on a fresh run
        useDefaultSpawn = true;

        // Clear defeated enemies for the new run
        _defeatedEnemyIds.Clear();
        _defeatedEnemyId = null;

        LoadExploration();
    }

    public void LoadExploration()
    {
        // Re-lock cursor for 3D exploration
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;
        SceneManager.LoadScene("Explorationscene");
    }
}
