using UnityEngine;

/// <summary>
/// Enemy AI: chases the player when in range, wanders otherwise.
/// Attach to enemy GameObject. Assign Player transform in Inspector.
/// </summary>
public class EnemyFollow : MonoBehaviour
{
    public Transform player;
    public float speed = 3f;
    public float chaseRange = 10f;
    public float wanderRadius = 5f;
    public float wanderInterval = 3f;

    private Vector3 wanderTarget;
    private float wanderTimer;


    private void Start()
    {
        
        wanderTarget = transform.position;
    }

    void Update()
    {
        if (player == null) return;

        // Don't move during battle
        if (CardBattle.BattleManager.Instance != null && !CardBattle.BattleManager.Instance.IsBattleOver)
            return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance < chaseRange)
            ChasePlayer();
        else
            Wander();
    }

    void ChasePlayer()
    {
        // Keep enemy on same Y level as itself (don't fly up to player)
        Vector3 target = new Vector3(player.position.x, transform.position.y, player.position.z);
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        
    }

    void Wander()
    {
        wanderTimer += Time.deltaTime;

        if (wanderTimer >= wanderInterval)
        {
            Vector3 random = Random.insideUnitSphere * wanderRadius + transform.position;
            wanderTarget = new Vector3(random.x, transform.position.y, random.z);
            wanderTimer = 0;
        }

        transform.position = Vector3.MoveTowards(transform.position, wanderTarget, speed * 0.5f * Time.deltaTime);
       
    }
}
