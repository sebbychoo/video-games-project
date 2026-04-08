using CardBattle;
using UnityEngine;

public class MenuScript : MonoBehaviour
{
    private void Start()
    {
        if (SaveManager.Instance != null)
        {
            GameObject saveManagerObject = new GameObject("SaveManager");
            saveManagerObject.AddComponent<SaveManager>();
        }
        else
        {
            SaveManager.Instance.LoadRun();
        }
    }
    public void LoadScene(string sceneName)
    {
        SceneLoader.Instance.LoadSceneUI(sceneName);
    }
    public void OnPlayButtonPressed()
    {
        // reset the run so we always start at the beginning
        if (SaveManager.Instance != null)
        {
            // set first floor as default spawn
            SaveManager.Instance.WipeRun();
            SaveManager.Instance.CurrentRun.currentFloor = 1;
            SaveManager.Instance.CurrentRun.hasCustomSpawn = false;
        }

        // load first floor
        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadSceneUI("Explorationscene");
    }
}
