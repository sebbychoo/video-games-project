using CardBattle;
using UnityEngine;

public class Death_To_Menu : MonoBehaviour
{
 void Start()
    {
        if (SaveManager.Instance == null)
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
        SceneLoader.Instance.LoadSceneMenu(sceneName);
    }
}
