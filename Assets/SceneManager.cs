using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void LoadBattle()
    {
        SceneManager.LoadScene("Battlescene");
    }

    public void LoadExploration()
    {
        SceneManager.LoadScene("Explorationscene");
    }
}
