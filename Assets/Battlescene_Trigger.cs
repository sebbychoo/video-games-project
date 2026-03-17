using UnityEngine;

public class Battlescene_Trigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            SceneLoader.Instance.LoadBattle();
        }
    }
    private void Start()
    {
        if (SceneLoader.Instance.enemyDefeated) // if the enemy was defeated in battle, destroy it right away
        {
            Destroy(gameObject); // remove the enemy from the exploration scene
        }
    }
}
