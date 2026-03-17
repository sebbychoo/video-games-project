using UnityEngine;
using UnityEngine.InputSystem.Controls;

public class EnemyFollow : MonoBehaviour
{
    public Transform Player; // target to follow
    public float speed = 3f; // speed of the enemy
    public float maxDistance = 10f;

    private Vector3 wanderTarget;
    private float wanderTimer;
    public float wanderRadius = 5f;
    public float wanderInterval = 3f;

    private void Start()
    {
        wanderTarget = transform.position;
    }
    void Update()
    {
        if (Player == null) return;

        float distance = Vector3.Distance(transform.position, Player.position);

        if (distance < maxDistance)
        {
            Vector3 groundLevelTarget = new Vector3(Player.position.x, transform.position.y, Player.position.z);
            transform.position = Vector3.MoveTowards(transform.position, groundLevelTarget, speed * Time.deltaTime);
        }
        else
        {
            Wander();
        }
    }

    void Wander()
    {
        wanderTimer += Time.deltaTime;

        if (wanderTimer >= wanderInterval)
        {
            Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
            randomDirection += transform.position;

            wanderTarget = new Vector3(randomDirection.x, transform.position.y, randomDirection.z); // keeps gameObject at the right Y level
            wanderTimer = 0;
        }

        transform.position = Vector3.MoveTowards(transform.position, wanderTarget, (speed * 0.5f) * Time.deltaTime);
    }
}
