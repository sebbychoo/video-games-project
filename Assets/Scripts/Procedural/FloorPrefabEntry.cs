using UnityEngine;

namespace Procedural
{
    /// <summary>
    /// Wraps a floor prefab with floor range restrictions.
    /// Used by LevelGenerator to pick valid floors per level.
    /// </summary>
    [System.Serializable]
    public class FloorPrefabEntry
    {
        public GameObject prefab;

        [Tooltip("Minimum floor this can appear on (inclusive). 0 = no minimum.")]
        public int minFloor = 0;

        [Tooltip("Maximum floor this can appear on (inclusive). 0 = no maximum.")]
        public int maxFloor = 0;

        [Tooltip("If true, this floor only appears once per run.")]
        public bool uniquePerRun = false;

        public bool IsValidForFloor(int floor)
        {
            if (minFloor > 0 && floor < minFloor) return false;
            if (maxFloor > 0 && floor > maxFloor) return false;
            return true;
        }
    }
}
