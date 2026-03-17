using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections; // needed for IEnumerator

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance;
    public Vector3 playerPosition;

    public bool enemyDefeated = false; // new flag to track if enemy was defeated. Global flag.
    

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
    // called when a new scene is loaded
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Explorationscene")
        {
            StartCoroutine(SetPlayerPositionNextFrame());
        }
    }
    private IEnumerator SetPlayerPositionNextFrame()
    {
        yield return null;

        GameObject player = GameObject.FindWithTag("Player");

        if (player != null && playerPosition != Vector3.zero)
        {
            CharacterController cc = player.GetComponent<CharacterController>();

            if (cc != null)
            {
                cc.enabled = false;
                player.transform.position = playerPosition;
                cc.enabled = true;
            }
            else
            {
                player.transform.position = playerPosition;
            }

            Debug.Log("Player moved to: " + playerPosition);
        }
    }
    public void LoadBattle()
    {
        // saves the players position before switching scenes
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerPosition = player.transform.position; // save position
        }
        SceneManager.LoadScene("Battlescene");
    }

    public void LoadExploration()
        
    {
         enemyDefeated = true;
         SceneManager.LoadScene("Explorationscene");
    }
}
