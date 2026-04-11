using UnityEngine;

/// <summary>
/// Restores the player's position when returning to the exploration scene.
/// Attach to the Player GameObject.
/// </summary>
public class SavePosition : MonoBehaviour
{
    void Start()
    {
        // Only restore saved position if we're returning from battle (not a fresh start)
        if (SceneLoader.Instance != null
            && !SceneLoader.Instance.useDefaultSpawn
            && SceneLoader.Instance.playerPosition != Vector3.zero)
        {
            transform.position = SceneLoader.Instance.playerPosition;
        }
    }
}
