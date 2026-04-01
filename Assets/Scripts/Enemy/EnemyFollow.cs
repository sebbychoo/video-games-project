using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Enemy AI: roams the floor, optionally chases the player on proximity,
/// respects safe rooms (bathrooms / break rooms), and stops at safe room
/// doorways when chasing. Uses NavMeshAgent for obstacle avoidance.
///
/// Requirements: 37.1, 37.2, 37.3, 37.4, 37.6, 37.7
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyFollow : MonoBehaviour
{
    [Header("References")]
    public Transform player;

    [Header("Movement")]
    public float speed = 3f;
    public float wanderRadius = 5f;
    public float wanderInterval = 3f;

    [Header("Aggro")]
    public bool isAggressive = true;
    public float chaseRange = 10f;
    [Tooltip("Field of view angle in degrees. Enemy only chases if player is within this cone.")]
    public float fieldOfView = 90f;

    [Header("Safe Room")]
    public float safeRoomGiveUpTime = 5f;

    private enum State { Patrol, Chase, WaitAtDoor }
    private State _state = State.Patrol;
    private float _wanderTimer;
    private float _doorWaitTimer;
    private SafeRoom[] _safeRooms;
    private NavMeshAgent _agent;

    private void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = speed;
        _agent.angularSpeed = 360f;
        _agent.acceleration = 12f;
        _agent.stoppingDistance = 0.5f;

        // Auto-find player if not assigned
        if (player == null)
        {
            GameObject playerGO = GameObject.FindWithTag("Player");
            if (playerGO != null) player = playerGO.transform;
        }

        _safeRooms = Object.FindObjectsByType<SafeRoom>(FindObjectsSortMode.None);

        var cfg = Resources.Load<CardBattle.GameConfig>("GameConfig");
        if (cfg != null) safeRoomGiveUpTime = cfg.safeRoomChaseTimeout;

        SetNewWanderTarget();
    }

    private void Update()
    {
        if (player == null) return;
        if (CardBattle.BattleManager.Instance != null && !CardBattle.BattleManager.Instance.IsBattleOver)
        {
            _agent.isStopped = true;
            return;
        }
        _agent.isStopped = false;

        switch (_state)
        {
            case State.Patrol:     UpdatePatrol();     break;
            case State.Chase:      UpdateChase();      break;
            case State.WaitAtDoor: UpdateWaitAtDoor(); break;
        }
    }

    private void UpdatePatrol()
    {
        if (isAggressive && Vector3.Distance(transform.position, player.position) <= chaseRange)
        {
            // Only chase if player is within the enemy's field of view
            Vector3 toPlayer = (player.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, toPlayer);
            if (angle <= fieldOfView * 0.5f)
            {
                _state = State.Chase;
                return;
            }
        }

        // Pick a new wander target when close to current one
        if (!_agent.pathPending && _agent.remainingDistance < 0.5f)
        {
            _wanderTimer += Time.deltaTime;
            if (_wanderTimer >= wanderInterval)
            {
                SetNewWanderTarget();
                _wanderTimer = 0f;
            }
        }
    }

    private void UpdateChase()
    {
        if (player == null) { _state = State.Patrol; return; }

        if (IsInSafeRoom(player.position))
        {
            _doorWaitTimer = 0f;
            _state = State.WaitAtDoor;
            _agent.ResetPath();
            return;
        }

        _agent.speed = speed;
        _agent.SetDestination(new Vector3(player.position.x, transform.position.y, player.position.z));
    }

    private void UpdateWaitAtDoor()
    {
        _doorWaitTimer += Time.deltaTime;

        if (!IsInSafeRoom(player.position))
        {
            _state = State.Chase;
            return;
        }

        if (_doorWaitTimer >= safeRoomGiveUpTime)
        {
            _state = State.Patrol;
            SetNewWanderTarget();
        }
    }

    private void SetNewWanderTarget()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 random = Random.insideUnitSphere * wanderRadius + transform.position;
            Vector3 candidate = new Vector3(random.x, transform.position.y, random.z);

            if (IsInSafeRoom(candidate)) continue;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(candidate, out hit, wanderRadius, NavMesh.AllAreas))
            {
                _agent.speed = speed * 0.5f;
                _agent.SetDestination(hit.position);
                return;
            }
        }
    }

    private bool IsInSafeRoom(Vector3 worldPos)
    {
        foreach (var room in _safeRooms)
            if (room != null && room.Contains(worldPos)) return true;
        return false;
    }

    public void OnEncounterTriggered()
    {
        _state = State.Patrol;
        _agent.ResetPath();
        SetNewWanderTarget();
    }

    public void ResumePatrol()
    {
        _state = State.Patrol;
        _agent.ResetPath();
        SetNewWanderTarget();
    }
}
