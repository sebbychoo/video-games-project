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
}
