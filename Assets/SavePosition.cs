using UnityEngine;

public class SavePosition : MonoBehaviour
{
    void Start()
    {
        // only move the player if a valid position was stored
        if (SceneLoader.Instance.playerPosition != Vector3.zero)
        {
            transform.position = SceneLoader.Instance.playerPosition;
        }
        
    }
}