using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using static CardBattle.BattleManager;

/// <summary>
/// Singleton that persists across scenes.
/// Handles scene switching and stores shared game state.
/// </summary>
public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance;

    public Vector3 playerPosition;
    public bool enemyDefeated = false;
    public bool useDefaultSpawn = false;

    public Transform defaultSpawnPoint;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "ExplorationScene")
        {
            StartCoroutine(SetSpawn());
        }
    }

    private IEnumerator SetSpawn()
    {
        yield return null;
        GameObject player = GameObject.FindWithTag("PLayer");

        if (player == null)
        {
            Debug.LogError("player not found");
            yield break;
        }

        if (useDefaultSpawn)
        {
            if (defaultSpawnPoint != null)
            {
                player.transform.position = defaultSpawnPoint.position;
            }
            useDefaultSpawn = false;
        }
        else
        {
            player.transform.position = playerPosition;
        }
    }


    //private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    //{
    //    if (scene.name == "Explorationscene")
    //        StartCoroutine(SetPlayerPositionNextFrame()); // SetPlayerPositionNextFrame
    //}


    //private IEnumerator SetPlayerPositionNextFrame()
    //{
    //    yield return null; // wait 1 frame

    //    GameObject player = GameObject.FindWithTag("Player");
    //    if (player == null || playerPosition == Vector3.zero) yield break;
    //        CharacterController cc = player.GetComponent<CharacterController>();
    //    if (cc != null) cc.enabled = false;
    //    player.transform.position = playerPosition;
    //    if (cc != null) cc.enabled = true;
    //}

    public void LoadBattle()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
            playerPosition = player.transform.position;

        // Unlock cursor for battle UI (card clicking needs a visible, free cursor)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        SceneManager.LoadScene("Battlescene");
    }

    public void LoadExploration()
    {
        // Re-lock cursor for 3D exploration
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible   = false;
        SceneManager.LoadScene("Explorationscene");
    }
}
