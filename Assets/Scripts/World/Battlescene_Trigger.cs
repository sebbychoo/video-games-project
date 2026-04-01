using UnityEngine;
using CardBattle;

/// <summary>
/// Triggers the battle scene when the player walks into this collider.
/// Also removes itself if the enemy was already defeated.
/// Attach to the enemy trigger zone. Set collider to Is Trigger.
///
/// Supports two modes:
/// 1. Pre-defined encounter group: assign encounterData in the inspector (multi-enemy).
/// 2. Single roaming enemy: assign singleEnemyData instead (1v1 encounter created at runtime).
/// </summary>
public class Battlescene_Trigger : MonoBehaviour
{
    [Tooltip("Pre-defined multi-enemy encounter. Takes priority over singleEnemyData.")]
    [SerializeField] EncounterData encounterData;

    [Tooltip("Single roaming enemy data for 1v1 encounters. Used when encounterData is not assigned.")]
    [SerializeField] EnemyCombatantData singleEnemyData;

    [Tooltip("Unique identifier for this enemy trigger. Auto-generated from name + position if left empty.")]
    [SerializeField] string enemyId;

    private void Awake()
    {
        if (string.IsNullOrEmpty(enemyId))
        {
            enemyId = $"{gameObject.name}_{transform.position.GetHashCode()}";
        }
    }

    private void Start()
    {
        if (SceneLoader.Instance != null && SceneLoader.Instance.IsEnemyDefeated(enemyId))
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Don't start battle if player is interacting with something
            if (WorkBoxTrigger.IsInteracting) return;
            if (BathroomShopTrigger.IsInteracting) return;
            if (BreakRoomTradeTrigger.IsInteracting) return;
            if (CardBattle.Elevator.IsDescending) return;

            EncounterData encounter = encounterData;

            // For roaming enemies without a pre-defined encounter, create one at runtime
            if (encounter == null && singleEnemyData != null)
            {
                encounter = ScriptableObject.CreateInstance<EncounterData>();
                encounter.enemies = new System.Collections.Generic.List<EnemyCombatantData> { singleEnemyData };
                encounter.isBossEncounter = singleEnemyData.isBoss;
                encounter.badReviewsReward = 0;
            }

            // Only this enemy enters the encounter; all others resume patrol (req 37.6)
            NotifyEnemiesOfEncounter(GetComponent<EnemyFollow>());

            SceneLoader.Instance.LoadBattle(encounter, enemyId);
        }
    }

    /// <summary>
    /// Programmatically triggers the encounter (used by BossFloorGate force-engage, Req 38.4).
    /// </summary>
    public void TriggerEncounter()
    {
        EncounterData encounter = encounterData;

        if (encounter == null && singleEnemyData != null)
        {
            encounter = ScriptableObject.CreateInstance<EncounterData>();
            encounter.enemies = new System.Collections.Generic.List<EnemyCombatantData> { singleEnemyData };
            encounter.isBossEncounter = singleEnemyData.isBoss;
            encounter.badReviewsReward = 0;
        }

        if (encounter == null)
        {
            Debug.LogWarning("Battlescene_Trigger.TriggerEncounter: No encounter data configured.");
            return;
        }

        NotifyEnemiesOfEncounter(GetComponent<EnemyFollow>());
        SceneLoader.Instance.LoadBattle(encounter, enemyId);
    }

    /// <summary>
    /// Tells the catching enemy it triggered an encounter, and tells every
    /// other EnemyFollow on the floor to resume patrol.
    /// </summary>
    private static void NotifyEnemiesOfEncounter(EnemyFollow catchingEnemy)
    {
        foreach (var ef in Object.FindObjectsByType<EnemyFollow>(FindObjectsSortMode.None))
        {
            if (ef == catchingEnemy)
                ef.OnEncounterTriggered();
            else
                ef.ResumePatrol();
        }
    }
}
