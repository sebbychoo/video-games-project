using CardBattle;
using UnityEngine;

public class Start_Button : MonoBehaviour
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
        //if (SceneLoader.Instance == null)
        //{
        //    GameObject obj = new GameObject("SceneLoader");
        //    obj.AddComponent<SceneLoader>();
        //}
        
        SceneLoader.Instance.LoadSceneUI(sceneName);
    }
}
