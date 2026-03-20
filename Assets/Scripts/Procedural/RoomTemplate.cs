using System.Collections.Generic;
using UnityEngine;

namespace Procedural
{
    /// <summary>
    /// ScriptableObject defining a room type and its spawn rules.
    /// Create via Assets > Create > Procedural > Room Template.
    /// </summary>
    [CreateAssetMenu(fileName = "NewRoom", menuName = "Procedural/Room Template")]
    public class RoomTemplate : ScriptableObject
    {
        public string roomName = "Office";
        public GameObject roomPrefab; // the shell (walls, floor, ceiling)
        public Vector2 roomSize = new Vector2(10f, 10f); // width x depth

        [Header("Furniture Rules")]
        public List<SpawnRule> spawnRules = new List<SpawnRule>();

        [Header("Doors")]
        [Tooltip("Local positions where doors can connect to other rooms")]
        public List<Vector3> doorPositions = new List<Vector3>();
    }
}
