using UnityEngine;

public class Exit : MonoBehaviour
{
    public bool playerWon = true;
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Debug.Log("Exit Reached");
        if (SceneLoader.Instance != null)
        {
            if (playerWon)
            {
                SceneLoader.Instance.useDefaultSpawn = true;
            }
        }
        SceneLoader.Instance.LoadExploration();
        SceneLoader.Instance.enemyDefeated = false;
    }
}
