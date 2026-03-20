using UnityEngine;

namespace Procedural
{
    public enum PlacementType
    {
        Anywhere,       // free placement within zone
        AlongWall,      // snaps to nearest wall edge
        UnderFurniture, // must be beneath another placed object (e.g. box under desk)
        Cardinal        // random facing: 0, 90, 180, 270
    }

    /// <summary>
    /// Defines what prefab to spawn, how many, and placement constraints.
    /// </summary>
    [System.Serializable]
    public class SpawnRule
    {
        public GameObject prefab;
        public int minCount = 1;
        public int maxCount = 1;
        public PlacementType placement = PlacementType.Anywhere;

        [Tooltip("If placement is UnderFurniture, which prefab tag must it be under?")]
        public string parentTag = "";

        [Tooltip("Minimum distance between instances of this prefab")]
        public float minSpacing = 1f;
    }
}
