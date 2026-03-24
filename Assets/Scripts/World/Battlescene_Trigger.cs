using UnityEngine;
using CardBattle;

/// <summary>
/// Triggers the battle scene when the player walks into this collider.
/// Also removes itself if the enemy was already defeated.
/// Attach to the enemy trigger zone. Set collider to Is Trigger.
/// </summary>
public class Battlescene_Trigger : MonoBehaviour
{
    [SerializeField] EncounterData encounterData;

    private void Start()
    {
        if (SceneLoader.Instance != null && SceneLoader.Instance.enemyDefeated)
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Pass encounter data to BattleManager before loading the scene
            if (encounterData != null)
                BattleManager.SetPendingEncounter(encounterData);

            SceneLoader.Instance.LoadBattle();
        }
    }
}
