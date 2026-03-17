using UnityEngine;
using UnityEngine.InputSystem.Controls;

public class EnemyFollow : MonoBehaviour
{
    public Transform Player; // target to follow
    public float speed = 3f; // speed of the enemy
    public float stoppingDistance = 1.5f;
    private void Update()
    {
        if(Player != null)
        {
            float distance = Vector3.Distance(transform.position, Player.position);
            transform.position = Vector3.Lerp(transform.position, Player.position, speed * Time.deltaTime);
            
            if (distance <  stoppingDistance )
            {
                transform.position = Vector3.Lerp(transform.position, Player.position, speed * Time.deltaTime);
            }
        }

    }
}
