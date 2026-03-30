using UnityEngine;

/// <summary>
/// Enemy AI: roams the floor, optionally chases the player on proximity,
/// respects safe rooms (bathrooms / break rooms), and stops at safe room
/// doorways when chasing.
///
/// Requirements: 37.1, 37.2, 37.3, 37.4, 37.6, 37.7
/// </summary>
public class EnemyFollow : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────────────────

    [Header("References")]
    public Transform player;

    [Header("Movement")]
    public float speed = 3f;
    public float wanderRadius = 5f;
    public float wanderInterval = 3f;

    [Header("Aggro")]
    [Tooltip("Aggressive enemies chase the player on proximity. Passive enemies never chase.")]
    public bool isAggressive = true;
    public float chaseRange = 10f;

    [Header("Safe Room")]
    [Tooltip("Seconds to wait at a safe room doorway before giving up the chase. " +
             "Defaults to GameConfig.safeRoomChaseTimeout if a GameConfig is loaded.")]
    public float safeRoomGiveUpTime = 5f;

    // ── State ────────────────────────────────────────────────────────────────

    private enum State { Patrol, Chase, WaitAtDoor }

    private State _state = State.Patrol;
    private Vector3 _wanderTarget;
    private float _wanderTimer;
    private float _doorWaitTimer;

    // Cache all safe rooms once on Start for performance
    private SafeRoom[] _safeRooms;

    // ── Unity ────────────────────────────────────────────────────────────────

    private void Start()
    {
        _wanderTarget = transform.position;
        _safeRooms = Object.FindObjectsByType<SafeRoom>(FindObjectsSortMode.None);

        // Pull timeout from GameConfig if available
        var cfg = Resources.Load<CardBattle.GameConfig>("GameConfig");
        if (cfg != null)
            safeRoomGiveUpTime = cfg.safeRoomChaseTimeout;
    }

    private void Update()
    {
        if (player == null) return;

        // Freeze during an active battle
        if (CardBattle.BattleManager.Instance != null && !CardBattle.BattleManager.Instance.IsBattleOver)
            return;

        switch (_state)
        {
            case State.Patrol:    UpdatePatrol();    break;
            case State.Chase:     UpdateChase();     break;
            case State.WaitAtDoor: UpdateWaitAtDoor(); break;
        }
    }

    // ── State updates ────────────────────────────────────────────────────────

    private void UpdatePatrol()
    {
        // Passive enemies never leave patrol
        if (isAggressive && player != null)
        {
            float dist = Vector3.Distance(transform.position, player.position);
            if (dist <= chaseRange)
            {
                _state = State.Chase;
                return;
            }
        }

        Wander();
    }

    private void UpdateChase()
    {
        if (player == null)
        {
            _state = State.Patrol;
            return;
        }

        // Player entered a safe room — stop at doorway
        if (IsInSafeRoom(player.position))
        {
            _doorWaitTimer = 0f;
            _state = State.WaitAtDoor;
            return;
        }

        // Next step toward player would enter a safe room — stop here
        Vector3 nextPos = Vector3.MoveTowards(transform.position, GetChaseTarget(), speed * Time.deltaTime);
        if (IsInSafeRoom(nextPos))
        {
            _doorWaitTimer = 0f;
            _state = State.WaitAtDoor;
            return;
        }

        transform.position = nextPos;
    }

    private void UpdateWaitAtDoor()
    {
        _doorWaitTimer += Time.deltaTime;

        // Player left the safe room — resume chase
        if (!IsInSafeRoom(player.position))
        {
            _state = State.Chase;
            return;
        }

        // Give up after timeout
        if (_doorWaitTimer >= safeRoomGiveUpTime)
        {
            _state = State.Patrol;
            _wanderTimer = wanderInterval; // pick a new wander target immediately
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void Wander()
    {
        _wanderTimer += Time.deltaTime;

        if (_wanderTimer >= wanderInterval)
        {
            Vector3 random = Random.insideUnitSphere * wanderRadius + transform.position;
            Vector3 candidate = new Vector3(random.x, transform.position.y, random.z);

            // Don't wander into safe rooms (req 37.2)
            if (!IsInSafeRoom(candidate))
                _wanderTarget = candidate;

            _wanderTimer = 0f;
        }

        transform.position = Vector3.MoveTowards(transform.position, _wanderTarget, speed * 0.5f * Time.deltaTime);
    }

    private Vector3 GetChaseTarget()
    {
        return new Vector3(player.position.x, transform.position.y, player.position.z);
    }

    private bool IsInSafeRoom(Vector3 worldPos)
    {
        foreach (var room in _safeRooms)
        {
            if (room != null && room.Contains(worldPos))
                return true;
        }
        return false;
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Called by Battlescene_Trigger when this enemy catches the player.
    /// Other enemies that were chasing resume patrol (req 37.6).
    /// </summary>
    public void OnEncounterTriggered()
    {
        _state = State.Patrol;
        _wanderTimer = wanderInterval;
    }

    /// <summary>Force the enemy back to patrol (e.g. after a battle ends).</summary>
    public void ResumePatrol()
    {
        _state = State.Patrol;
        _wanderTimer = wanderInterval;
    }
}
