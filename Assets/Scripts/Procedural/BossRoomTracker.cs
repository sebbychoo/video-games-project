using UnityEngine;
using CardBattle;

namespace Procedural
{
    /// <summary>
    /// Placed in the boss room by LevelGenerator.
    /// Tracks whether the boss is alive and notifies BossFloorGate on defeat.
    /// Also handles force-triggering the boss encounter when the player tries to leave.
    /// </summary>
    public class BossRoomTracker : MonoBehaviour
    {
        [SerializeField] Battlescene_Trigger bossEncounterTrigger;

        private BossFloorGate _gate;
        private bool _bossDefeated;

        public bool BossDefeated => _bossDefeated;

        private void Start()
        {
            _gate = FindObjectOfType<BossFloorGate>();
        }

        /// <summary>
        /// Called by the boss EnemyCombatant (or BattleManager) when the boss dies.
        /// </summary>
        public void NotifyBossDefeated()
        {
            if (_bossDefeated) return;
            _bossDefeated = true;
            _gate?.OnBossDefeated();
        }

        /// <summary>
        /// Force-triggers the boss encounter when the player tries to leave
        /// without defeating the boss (Req 38.4).
        /// </summary>
        public void ForceEngageBoss()
        {
            if (_bossDefeated) return;

            if (bossEncounterTrigger != null)
                bossEncounterTrigger.TriggerEncounter();
            else
                Debug.LogWarning("BossRoomTracker: No Battlescene_Trigger assigned for force-engage.");
        }
    }
}
