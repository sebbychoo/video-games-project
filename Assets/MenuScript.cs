using CardBattle;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuScript : MonoBehaviour
{
    private void Start()
    {
        // Ensure cursor is visible on menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Ensure SaveManager exists
        if (SaveManager.Instance == null)
        {
            GameObject saveManagerObject = new GameObject("SaveManager");
            saveManagerObject.AddComponent<SaveManager>();
        }
        SaveManager.Instance.LoadRun();
    }

    public void LoadScene(string sceneName)
    {
        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadSceneUI(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }

    public void OnPlayButtonPressed()
    {
        // Reset the run so we always start fresh
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.WipeRun();
            SaveManager.Instance.CurrentRun.currentFloor = 1;
            SaveManager.Instance.CurrentRun.hasCustomSpawn = false;
        }

        // Load exploration scene
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.useDefaultSpawn = true;
            SceneLoader.Instance.enemyDefeated = false; // clears defeated enemy list
            SceneLoader.Instance.LoadSceneUI("Explorationscene");
        }
        else
        {
            SceneManager.LoadScene("Explorationscene");
        }
    }
}
