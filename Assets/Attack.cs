using UnityEngine;

public class Attack : MonoBehaviour
{
    void Update()

    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SceneLoader.Instance.LoadExploration();
        }
    }
}

    
    

