using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using CardBattle;

namespace Procedural
{
    /// <summary>
    /// Generates an office floor layout on a flat plane.
    /// - Special rooms (Bathroom, BreakRoom, BossRoom) snap to the edges of the floor,
    ///   doorway facing inward.
    /// - Cubicles are scattered in the interior, away from room doorways.
    /// - Enemies, WorkBoxes, and the floor exit are spawned at runtime.
    /// </summary>
    public class LevelGenerator : MonoBehaviour
    {
        [Header("Floor Bounds (match your floor plane)")]
        [SerializeField] float floorWidth  = 40f;
        [SerializeField] float floorDepth  = 40f;
        [SerializeField] float roomDepth   = 5f;    // half-depth of room prefab — back wall flush with map edge
        [SerializeField] float roomSpacing = 4f;     // min distance between room centres on same edge

        [Header("Config")]
        [SerializeField] GameConfig gameConfig;
        [SerializeField] int seed = -1;
        [SerializeField] int testFloor = 1;
        [SerializeField] MonoBehaviour navMeshSurface; // assign NavMeshSurface component here

        [Header("Room Prefabs")]
        [SerializeField] GameObject bathroomRoomPrefab;
        [SerializeField] GameObject breakRoomPrefab;
        [SerializeField] GameObject bossRoomPrefab;

        [Header("Room Size Variation")]
        [SerializeField] float roomScaleMin = 0.8f;  // min XZ scale (Y stays 1 — walls keep height)
        [SerializeField] float roomScaleMax = 1.3f;  // max XZ scale

        [Header("Furniture / Content Prefabs")]
        [SerializeField] GameObject cubiclesPrefab;
        [SerializeField] GameObject workBoxPrefab;
        [SerializeField] GameObject bathroomShopPrefab;
        [SerializeField] GameObject tradeNPCPrefab;
        [SerializeField] GameObject floorExitPrefab;
        [SerializeField] GameObject suicidalWorkerPrefab;

        [Header("Cubicle Placement")]
        [SerializeField] float cubicleMinDistFromRoom = 4f;
        [SerializeField] float cubicleSize = 1.94f;
        [SerializeField] float cubicleRowGap = 1.0f;    // walkable gap between rows
        [SerializeField] float columnGap = 5f;           // walkable corridor between the 2 columns
        [SerializeField] int cubiclesPerRow = 5;         // cubicles wide per column
        [SerializeField] int cubicleDepth = 3;           // cubicles deep per column
        [SerializeField] float playerSpawnClearRadius = 4f;

        [Header("Enemy Data")]
        [SerializeField] GameObject enemyPrefab;        // actual enemy prefab to spawn
        [SerializeField] List<EnemyCombatantData> coworkerEnemies;
        [SerializeField] List<EnemyCombatantData> creatureEnemies;
        [SerializeField] List<EnemyCombatantData> bossEnemies;

        // ── Runtime state ──────────────────────────────────────────────────────
        private int _currentFloor;
        private List<PlacedRoom> _placedRooms = new List<PlacedRoom>();
        private List<Vector3>    _cubiclePositions = new List<Vector3>();

        private static bool _suicidalWorkerPlacedThisRun = false;

        // ── Public API ─────────────────────────────────────────────────────────

        public static void ResetRunFlags() => _suicidalWorkerPlacedThisRun = false;

        private void Start() { /* SceneLoader calls Generate(floor) on scene load */ }

        public void Generate(int floor)
        {
            _currentFloor = floor;

            int s = seed >= 0 ? seed + floor : System.Environment.TickCount + floor;
            Random.InitState(s);

            ClearLevel();
            PlaceEdgeRooms();
            PlaceCubicles();
            SpawnEnemies();
            SpawnWorkBoxes();
            PlaceFloorExit();

            if (_currentFloor == (gameConfig != null ? gameConfig.workerEncounterFloor : 5)
                && !_suicidalWorkerPlacedThisRun && suicidalWorkerPrefab != null)
            {
                Instantiate(suicidalWorkerPrefab, RandomInteriorPoint(8f), Quaternion.identity, transform);
                _suicidalWorkerPlacedThisRun = true;
            }

            // Rebuild NavMesh after all objects are placed so enemies can navigate
            if (navMeshSurface != null)
            {
                var buildMethod = navMeshSurface.GetType().GetMethod("BuildNavMesh");
                buildMethod?.Invoke(navMeshSurface, null);
            }
        }

        public void Generate() => Generate(1);

        public void ClearLevel()
        {
            foreach (Transform child in transform)
                Destroy(child.gameObject);
            _placedRooms.Clear();
            _cubiclePositions.Clear();
        }

        // ── Edge room placement ────────────────────────────────────────────────

        private void PlaceEdgeRooms()
        {
            bool isBoss     = IsBossFloor(_currentFloor);
            bool hasBreak   = HasBreakRoom(_currentFloor);
            bool hasShop    = _currentFloor == 1 || isBoss || Random.value < 0.5f;
            bool hasTradeNPC = Random.value < 0.6f;

            // Collect which rooms to place and on which edge (0=North,1=East,2=South,3=West)
            var toPlace = new List<(GameObject prefab, RoomType type)>();

            // Bathroom always spawns — shop inside is conditional
            if (bathroomRoomPrefab != null)
                toPlace.Add((bathroomRoomPrefab, RoomType.Bathroom));

            // Break room always spawns on even floors — trade NPC inside is conditional
            if (hasBreak && breakRoomPrefab != null)
                toPlace.Add((breakRoomPrefab, RoomType.BreakRoom));

            if (isBoss && bossRoomPrefab != null)
                toPlace.Add((bossRoomPrefab, RoomType.BossRoom));

            // On boss floors, guarantee break room even if not an even floor
            if (isBoss && !hasBreak && breakRoomPrefab != null)
                toPlace.Add((breakRoomPrefab, RoomType.BreakRoom));

            // Rooms can go on any of the 3 far walls (North=0, East=1, West=3)
            // South=2 is excluded — that's the player spawn side
            // Multiple rooms can share the same wall, positions are randomised along it
            int[] validEdges = { 0, 1, 3 };
            List<int> edges = new List<int>();
            for (int i = 0; i < toPlace.Count; i++)
                edges.Add(validEdges[Random.Range(0, validEdges.Length)]);

            float hw = floorWidth  * 0.5f;
            float hd = floorDepth  * 0.5f;

            for (int i = 0; i < toPlace.Count && i < edges.Count; i++)
            {
                int edge = edges[i];
                Vector3 pos = EdgePosition(edge, hw, hd);

                // Ensure rooms don't overlap each other (min distance scales with floor size)
                float minRoomDist = Mathf.Min(3f, floorWidth * 0.25f);
                bool tooClose = false;
                foreach (var placed in _placedRooms)
                    if (Vector3.Distance(pos, placed.position) < minRoomDist) { tooClose = true; break; }
                if (tooClose) continue;

                Quaternion rot = EdgeRotation(edge);

                GameObject go = Instantiate(toPlace[i].prefab, pos, rot, transform);
                go.name = $"{toPlace[i].type}_{i}";
                // Random XZ scale — Y stays 1 so walls keep their height
                float s = Random.Range(roomScaleMin, roomScaleMax);
                go.transform.localScale = new Vector3(s, 1f, s);

                // Wire BossFloorGate on boss room
                if (toPlace[i].type == RoomType.BossRoom)
                {
                    go.tag = "BossRoom";
                    BossFloorGate gate = go.GetComponent<BossFloorGate>();
                    if (gate == null) gate = go.AddComponent<BossFloorGate>();
                    gate.SetFloor(_currentFloor);
                }

                // Spawn shop trigger inside bathroom room — conditional
                if (toPlace[i].type == RoomType.Bathroom && bathroomShopPrefab != null && hasShop)
                {
                    Vector3 shopPos = pos + rot * new Vector3(0f, 0f, 1f);
                    Instantiate(bathroomShopPrefab, shopPos, rot, go.transform);
                }

                // Spawn trade NPC inside break room — conditional
                if (toPlace[i].type == RoomType.BreakRoom && tradeNPCPrefab != null && hasTradeNPC)
                {
                    Vector3 npcPos = pos + rot * new Vector3(0f, 0f, 1f);
                    Instantiate(tradeNPCPrefab, npcPos, rot, go.transform);
                }

                _placedRooms.Add(new PlacedRoom
                {
                    position = pos,
                    doorwayWorldPos = pos + rot * Vector3.forward * 3f, // approx doorway
                    roomType = toPlace[i].type
                });
            }
        }

        /// <summary>Returns a position snapped to the given edge, randomised along it.</summary>
        private Vector3 EdgePosition(int edge, float hw, float hd)
        {
            float margin = Mathf.Min(2f, hw * 0.3f); // scale margin to floor size
            // Inset by roomDepth so the back wall is flush with the map edge
            switch (edge)
            {
                case 0: // North (+Z) — back wall at +Z edge
                    return new Vector3(Random.Range(-hw + margin, hw - margin), 0f,  hd - roomDepth);
                case 1: // East (+X) — back wall at +X edge
                    return new Vector3( hw - roomDepth, 0f, Random.Range(-hd + margin, hd - margin));
                case 2: // South (-Z) — back wall at -Z edge
                    return new Vector3(Random.Range(-hw + margin, hw - margin), 0f, -hd + roomDepth);
                default: // West (-X) — back wall at -X edge
                    return new Vector3(-hw + roomDepth, 0f, Random.Range(-hd + margin, hd - margin));
            }
        }

        /// <summary>Rotates the room so its open side (forward) faces the floor centre.</summary>
        private Quaternion EdgeRotation(int edge)
        {
            switch (edge)
            {
                case 0: return Quaternion.Euler(0f, 180f, 0f); // North wall → open side faces south (toward centre)
                case 1: return Quaternion.Euler(0f, 270f, 0f); // East wall  → open side faces west
                case 2: return Quaternion.identity;             // South wall → open side faces north
                default: return Quaternion.Euler(0f,  90f, 0f); // West wall  → open side faces east
            }
        }

        // ── Cubicle placement ──────────────────────────────────────────────────

        private void PlaceCubicles()
        {
            if (cubiclesPrefab == null) return;

            // Two columns of cubicles centred on the floor
            // Each column: cubiclesPerRow wide (X), cubicleDepth deep (Z)
            // Columns separated by columnGap, centred on X=0
            float totalWidth = cubiclesPerRow * cubicleSize;
            float col1StartX = -(columnGap * 0.5f) - totalWidth;
            float col2StartX =   columnGap * 0.5f;

            // Start cubicles near the player spawn (south end) and grow northward
            float totalDepth = cubicleDepth * (cubicleSize + cubicleRowGap);
            float startZ = -floorDepth * 0.5f + playerSpawnClearRadius + cubicleRowGap;

            float hwC = floorWidth  * 0.5f - 1f;
            float hdC = floorDepth  * 0.5f - 1f;

            for (int col = 0; col < 2; col++)
            {
                float baseX = col == 0 ? col1StartX : col2StartX;

                for (int row = 0; row < cubicleDepth; row++)
                {
                    float z = startZ + row * (cubicleSize + cubicleRowGap);

                    for (int c = 0; c < cubiclesPerRow; c++)
                    {
                        float x = baseX + c * cubicleSize;
                        Vector3 pos = new Vector3(x, 0f, z);

                        // Clamp to floor bounds
                        pos.x = Mathf.Clamp(pos.x, -hwC, hwC);
                        pos.z = Mathf.Clamp(pos.z, -hdC, hdC);

                        if (!IsTooCloseToRoom(pos) && Vector3.Distance(pos, Vector3.zero) > playerSpawnClearRadius)
                        {
                            Instantiate(cubiclesPrefab, pos, Quaternion.identity, transform);
                            _cubiclePositions.Add(pos);
                        }
                    }
                }
            }
        }

        private bool IsTooCloseToRoom(Vector3 pos)
        {
            foreach (var room in _placedRooms)
            {
                if (Vector3.Distance(pos, room.doorwayWorldPos) < cubicleMinDistFromRoom)
                    return true;
                if (Vector3.Distance(pos, room.position) < cubicleMinDistFromRoom * 0.6f)
                    return true;
            }
            // Also block the player spawn zone (south side of floor)
            if (pos.z < -floorDepth * 0.5f + roomDepth + 3f && Mathf.Abs(pos.x) < 3f)
                return true;
            return false;
        }

        // ── Enemy spawning ─────────────────────────────────────────────────────

        private void SpawnEnemies()
        {
            int count = Random.Range(2, 5);
            for (int i = 0; i < count; i++)
            {
                EnemyCombatantData data = PickEnemyForFloor(_currentFloor);
                if (data == null) continue;

                // Try to find a valid spawn position away from the player spawn
                Vector3 pos = Vector3.zero;
                bool found = false;
                for (int attempt = 0; attempt < 20; attempt++)
                {
                    Vector3 candidate = RandomInteriorPoint(5f);
                    if (Vector3.Distance(candidate, Vector3.zero) >= playerSpawnClearRadius)
                    {
                        pos = candidate;
                        found = true;
                        break;
                    }
                }
                if (!found) continue;

                // Spawn the actual enemy prefab — prefer per-data prefab, fall back to shared prefab
                GameObject prefabToUse = data.explorationPrefab != null ? data.explorationPrefab : enemyPrefab;
                GameObject go = prefabToUse != null
                    ? Instantiate(prefabToUse, pos, Quaternion.identity, transform)
                    : new GameObject($"Enemy_{data.enemyName}_{i}");

                if (prefabToUse == null) go.transform.SetParent(transform);
                go.transform.position = pos;
                go.name = $"Enemy_{data.enemyName}_{i}";
                EnemySpawnMarker marker = go.AddComponent<EnemySpawnMarker>();
                marker.enemyData = data;
                marker.floor = _currentFloor;
            }
        }

        // ── WorkBox spawning ───────────────────────────────────────────────────

        private void SpawnWorkBoxes()
        {
            if (workBoxPrefab == null || _cubiclePositions.Count == 0) return;

            // Spawn one WorkBox near a random subset of cubicles (not every cubicle gets one)
            int count = Mathf.Min(Random.Range(2, 5), _cubiclePositions.Count);
            List<int> indices = new List<int>();
            for (int i = 0; i < _cubiclePositions.Count; i++) indices.Add(i);
            Shuffle(indices);

            for (int i = 0; i < count; i++)
            {
                Vector3 cubiclePos = _cubiclePositions[indices[i]];
                // Offset toward the front of the cubicle (open side) so box sits near the desk
                Vector3 pos = cubiclePos + new Vector3(
                    Random.Range(-0.3f, 0.3f), 0f, -0.5f);

                WorkBoxSize size = RollWorkBoxSize(_currentFloor);
                GameObject go = Instantiate(workBoxPrefab, pos, Quaternion.identity, transform);
                WorkBox wb = go.GetComponent<WorkBox>();
                if (wb != null) wb.InitializeForFloor(_currentFloor, size);
            }
        }

        // ── Floor exit ─────────────────────────────────────────────────────────

        /// <summary>World position of the floor exit — saved to RunState for next floor spawn.</summary>
        public Vector3 ExitPosition { get; private set; }

        private void PlaceFloorExit()
        {
            if (floorExitPrefab == null) return;

            // Place exit on the opposite side of the floor from the player spawn (origin)
            float hw = floorWidth * 0.5f - roomDepth - 1f;
            float hd = floorDepth * 0.5f - roomDepth - 1f;
            Vector3 exitPos = new Vector3(
                Random.Range(-hw * 0.3f, hw * 0.3f), 0f, hd);

            GameObject exit = Instantiate(floorExitPrefab, exitPos, Quaternion.identity, transform);
            exit.name = "FloorExit";
            ExitPosition = exitPos;

            if (IsBossFloor(_currentFloor))
            {
                BossFloorGate gate = exit.GetComponent<BossFloorGate>();
                if (gate == null) gate = exit.AddComponent<BossFloorGate>();
                gate.SetFloor(_currentFloor);
            }
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        /// <summary>Random point inside the floor, keeping away from the outer edge.</summary>
        private Vector3 RandomInteriorPoint(float edgePadding)
        {
            float hw = floorWidth  * 0.5f - edgePadding;
            float hd = floorDepth  * 0.5f - edgePadding;
            return new Vector3(Random.Range(-hw, hw), 0f, Random.Range(-hd, hd));
        }

        public bool IsBossFloor(int floor)
        {
            int interval = gameConfig != null ? gameConfig.bossFloorInterval : 3;
            return floor > 0 && floor % interval == 0;
        }

        private bool HasBreakRoom(int floor)
        {
            int interval = gameConfig != null ? gameConfig.breakRoomFloorInterval : 2;
            return floor > 0 && floor % interval == 0;
        }

        public EnemyCombatantData PickEnemyForFloor(int floor)
        {
            float creatureWeight = GetCreatureWeight(floor);
            bool useCreature = Random.value < creatureWeight;
            List<EnemyCombatantData> pool = (useCreature && creatureEnemies != null && creatureEnemies.Count > 0)
                ? creatureEnemies : coworkerEnemies;
            if (pool == null || pool.Count == 0) return null;
            return pool[Random.Range(0, pool.Count)];
        }

        public static float GetCreatureWeight(int floor)
        {
            if (floor <= 5)  return 0.10f;
            if (floor <= 10) return 0.50f;
            return 0.80f;
        }

        public static WorkBoxSize RollWorkBoxSize(int floor)
        {
            float roll = Random.value;
            if (floor <= 5)  return WorkBoxSize.Small;
            if (floor <= 10) return roll < 0.90f ? WorkBoxSize.Small : WorkBoxSize.Big;
            if (roll < 0.70f) return WorkBoxSize.Small;
            if (roll < 0.95f) return WorkBoxSize.Big;
            return WorkBoxSize.Huge;
        }

        private void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        // ── Data structures ────────────────────────────────────────────────────

        private struct PlacedRoom
        {
            public Vector3   position;
            public Vector3   doorwayWorldPos;
            public RoomType  roomType;
        }
    }

    public class EnemySpawnMarker : MonoBehaviour
    {
        public EnemyCombatantData enemyData;
        public int floor;
    }
}
