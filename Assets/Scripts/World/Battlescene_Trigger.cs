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
            // Don't start battle if player is interacting with something (WorkBox, shop, etc.)
            if (WorkBoxTrigger.IsInteracting) return;
            if (BathroomShopTrigger.IsInteracting) return;
            if (BreakRoomTradeTrigger.IsInteracting) return;

            EncounterData encounter = encounterData;

            // For roaming enemies without a pre-defined encounter, create one at runtime
            if (encounter == null && singleEnemyData != null)
            {
                encounter = ScriptableObject.CreateInstance<EncounterData>();
                encounter.enemies = new System.Collections.Generic.List<EnemyCombatantData> { singleEnemyData };
                encounter.isBossEncounter = singleEnemyData.isBoss;
                encounter.badReviewsReward = 0;
            }

            SceneLoader.Instance.LoadBattle(encounter, enemyId);
        }
    }
}
