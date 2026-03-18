using UnityEngine;

/// <summary>
/// Restores the player's position when returning to the exploration scene.
/// Attach to the Player GameObject.
/// </summary>
public class SavePosition : MonoBehaviour
{
    void Start()
    {
        if (SceneLoader.Instance != null && SceneLoader.Instance.playerPosition != Vector3.zero)
            transform.position = SceneLoader.Instance.playerPosition;
    }
}
