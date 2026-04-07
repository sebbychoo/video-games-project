using UnityEngine;

namespace Procedural
{
    /// <summary>
    /// Per-floor settings. Attach to a floor prefab root to override
    /// default values from LevelGenerator.
    /// </summary>
    public class FloorSettings : MonoBehaviour
    {
        [Header("Work Boxes")]
        [Tooltip("Min work boxes to spawn on this floor.")]
        public int minWorkBoxes = 1;

        [Tooltip("Max work boxes to spawn on this floor.")]
        public int maxWorkBoxes = 3;

        [Header("Enemies")]
        [Tooltip("Min enemies to spawn. -1 = use LevelGenerator default.")]
        public int minEnemies = -1;

        [Tooltip("Max enemies to spawn. -1 = use LevelGenerator default.")]
        public int maxEnemies = -1;
    }
}
