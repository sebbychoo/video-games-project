using UnityEngine;
using CardBattle;

namespace Procedural
{
    /// <summary>
    /// Attached to the floor exit on boss floors.
    /// Locks the exit until the boss is defeated.
    /// If the player tries to leave while the boss is alive, the boss encounter
    /// is force-triggered instead (Req 38.3, 38.4, 38.5).
    /// </summary>
    public class BossFloorGate : MonoBehaviour
    {
        [SerializeField] GameObject lockedVisual;   // optional "locked" indicator
        [SerializeField] GameObject unlockedVisual; // optional "unlocked" indicator

        private int _floor;
        private bool _bossDefeated;
        private BossRoomTracker _bossTracker;

        public bool IsBossDefeated => _bossDefeated;

        public void SetFloor(int floor)
        {
            _floor = floor;
            _bossDefeated = false;
            RefreshVisuals();
        }

        private void Start()
        {
            // Find the boss room tracker in the scene (placed by LevelGenerator in the boss room).
            _bossTracker = FindObjectOfType<BossRoomTracker>();
            RefreshVisuals();
        }

        /// <summary>Called by BossRoomTracker when the boss is defeated.</summary>
        public void OnBossDefeated()
        {
            _bossDefeated = true;
            RefreshVisuals();
        }

        /// <summary>
        /// Called when the player interacts with the exit.
        /// Returns true if the player is allowed to proceed to the next floor.
        /// Returns false and force-triggers the boss encounter if the boss is still alive.
        /// </summary>
        public bool TryExit()
        {
            if (_bossDefeated)
                return true;

            // Boss is alive — intercept and force-trigger the encounter.
            if (_bossTracker != null)
                _bossTracker.ForceEngageBoss();
            else
                Debug.LogWarning("BossFloorGate: No BossRoomTracker found — cannot force boss encounter.");

            return false;
        }

        private void RefreshVisuals()
        {
            if (lockedVisual   != null) lockedVisual.SetActive(!_bossDefeated);
            if (unlockedVisual != null) unlockedVisual.SetActive(_bossDefeated);
        }
    }
}
