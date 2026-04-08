using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using CardBattle;

namespace Procedural
{
    /// <summary>
    /// Picks a random full-floor prefab, instantiates it, then populates
    /// dynamic content (enemies, workboxes, elevator) at tagged spawn points
    /// defined inside the prefab.
    ///
    /// Floor prefab conventions:
    ///   - Tag empty GameObjects "EnemySpawn"    → enemy spawn points
    ///   - Tag empty GameObjects "WorkBoxSpawn"  → workbox spawn points
    ///   - Tag empty GameObjects "ElevatorSpawn" → where the elevator goes (pick one randomly)
    ///   - Tag empty GameObjects "PlayerSpawn"   → floor-1 player start (optional override)
    /// </summary>
    public class LevelGenerator : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] GameConfig gameConfig;
        [SerializeField] int seed = -1;
        [SerializeField] int testFloor = 1;

        [Header("Floor Prefabs")]
        [SerializeField] List<GameObject> floorPrefabs;        // normal floors (legacy, still works)
        [SerializeField] List<GameObject> bossFloorPrefabs;    // boss-specific floors (optional)

        [Header("Floor Prefabs (Advanced — with floor restrictions)")]
        [Tooltip("Floor 1 always uses this prefab. Leave null to use the regular pool.")]
        [SerializeField] GameObject firstFloorPrefab;
        [SerializeField] List<FloorPrefabEntry> floorPool;     // floors with min/max restrictions

        [Header("Dynamic Content Prefabs")]
        [SerializeField] GameObject elevatorPrefab;
        [SerializeField] GameObject workBoxPrefab;
        [SerializeField] int workBoxCountMin = 1;
        [SerializeField] int workBoxCountMax = 3;
        [SerializeField] GameObject enemyPrefab;               // fallback if data has no prefab
        [SerializeField] GameObject suicidalWorkerPrefab;

        [Header("Enemy Data")]
        [SerializeField] List<EnemyCombatantData> coworkerEnemies;
        [SerializeField] List<EnemyCombatantData> creatureEnemies;
        [SerializeField] List<EnemyCombatantData> bossEnemies;

        [Header("NavMesh")]
        [SerializeField] MonoBehaviour navMeshSurface;

        // ── Runtime state ──────────────────────────────────────────────────────
        private int _currentFloor;
        private GameObject _spawnedFloor;
        private static bool _suicidalWorkerPlacedThisRun = false;
        private static HashSet<GameObject> _usedUniqueFloors = new HashSet<GameObject>();

        /// <summary>World position of the elevator interior — used as spawn point on next floor.</summary>
        public Vector3 ElevatorSpawnPosition { get; private set; }

        // ── Public API ─────────────────────────────────────────────────────────

        public static void ResetRunFlags() { _suicidalWorkerPlacedThisRun = false; _usedUniqueFloors.Clear(); }

        private void Start()
        {
            StartCoroutine(DelayedGenerate());
        }
        private IEnumerator DelayedGenerate()
        {
            yield return null;

            int floorToGenerate = 1;
            if (SaveManager.Instance != null && SaveManager.Instance.CurrentRun != null)
                floorToGenerate = SaveManager.Instance.CurrentRun.currentFloor;

            Generate(floorToGenerate);
        }

        public void Generate(int floor)
        {
            _currentFloor = floor;

            // Use a stable seed per floor so the layout is consistent
            // within the same run (same enemies, same positions on reload)
            int runSeed = 0;
            if (SaveManager.Instance != null && SaveManager.Instance.CurrentRun != null)
                runSeed = SaveManager.Instance.CurrentRun.runSeed;
            int s = seed >= 0 ? seed + floor : runSeed + floor * 7919;
            Random.InitState(s);

            ClearLevel();
            SpawnFloor();
            PopulateFloor();
            RebuildNavMesh();
        }

        public void Generate() => Generate(1);

        public void ClearLevel()
        {
            if (_spawnedFloor != null)
                Destroy(_spawnedFloor);
            _spawnedFloor = null;

            // Destroy all dynamically spawned children (enemies, workboxes, etc.)
            // They're parented to this transform, not the floor
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);
        }

        // ── Floor selection ────────────────────────────────────────────────────

        private void SpawnFloor()
        {
            GameObject chosen = null;

            // Floor 1 always uses firstFloorPrefab if set
            if (_currentFloor == 1 && firstFloorPrefab != null)
            {
                chosen = firstFloorPrefab;
            }
            // Boss floors use boss pool
            else if (IsBossFloor(_currentFloor) && bossFloorPrefabs != null && bossFloorPrefabs.Count > 0)
            {
                chosen = bossFloorPrefabs[Random.Range(0, bossFloorPrefabs.Count)];
            }
            // Advanced pool with floor restrictions
            else if (floorPool != null && floorPool.Count > 0)
            {
                List<FloorPrefabEntry> valid = new List<FloorPrefabEntry>();
                foreach (var entry in floorPool)
                {
                    if (entry.prefab == null) continue;
                    if (!entry.IsValidForFloor(_currentFloor)) continue;
                    if (entry.uniquePerRun && _usedUniqueFloors.Contains(entry.prefab)) continue;
                    valid.Add(entry);
                }

                if (valid.Count > 0)
                {
                    FloorPrefabEntry pick = valid[Random.Range(0, valid.Count)];
                    chosen = pick.prefab;
                    if (pick.uniquePerRun)
                        _usedUniqueFloors.Add(pick.prefab);
                }
            }

            // Fallback to legacy pool
            if (chosen == null && floorPrefabs != null && floorPrefabs.Count > 0)
                chosen = floorPrefabs[Random.Range(0, floorPrefabs.Count)];

            // Last resort — use firstFloorPrefab
            if (chosen == null && firstFloorPrefab != null)
                chosen = firstFloorPrefab;

            if (chosen == null)
            {
                Debug.LogWarning("LevelGenerator: No valid floor prefab found.");
                return;
            }

            _spawnedFloor = Instantiate(chosen, Vector3.zero, Quaternion.identity, transform);
            _spawnedFloor.name = $"Floor_{_currentFloor}";
        }

        // ── Dynamic content population ─────────────────────────────────────────

        private void PopulateFloor()
        {
            if (_spawnedFloor == null) return;

            PlaceElevator();
            SpawnEnemies();
            SpawnWorkBoxes();

            // Floor 5 suicidal worker
            int workerFloor = gameConfig != null ? gameConfig.workerEncounterFloor : 5;
            if (_currentFloor == workerFloor && !_suicidalWorkerPlacedThisRun && suicidalWorkerPrefab != null)
            {
                Transform wp = GetRandomTaggedPoint("WorkBoxSpawn");
                if (wp != null)
                {
                    Instantiate(suicidalWorkerPrefab, wp.position, Quaternion.identity, _spawnedFloor.transform);
                    _suicidalWorkerPlacedThisRun = true;
                }
            }
        }

        private void PlaceElevator()
        {
            if (elevatorPrefab == null) return;

            Transform[] points = GetAllTaggedPoints("ElevatorSpawn");
            if (points.Length == 0)
            {
                Debug.LogWarning("LevelGenerator: No ElevatorSpawn tags found in floor prefab.");
                return;
            }

            // On floor 2+, spawn a closed "out of order" elevator where the player arrives
            // and the working elevator at a different spawn point
            if (_currentFloor > 1 && points.Length >= 2)
            {
                // Find the spawn point closest to the player's arrival position
                Transform arrivalPoint = points[0];
                float closestDist = float.MaxValue;
                SaveManager sm = SaveManager.Instance;
                if (sm != null && sm.CurrentRun != null && sm.CurrentRun.hasCustomSpawn)
                {
                    Vector3 spawnPos = new Vector3(sm.CurrentRun.spawnX, 0, sm.CurrentRun.spawnZ);
                    foreach (var p in points)
                    {
                        float d = Vector3.Distance(new Vector3(p.position.x, 0, p.position.z), spawnPos);
                        if (d < closestDist) { closestDist = d; arrivalPoint = p; }
                    }
                }

                // Spawn closed elevator at arrival point
                GameObject closedElev = Instantiate(elevatorPrefab, arrivalPoint.position, arrivalPoint.rotation, _spawnedFloor.transform);
                closedElev.name = "Elevator_Closed";
                var closedScript = closedElev.GetComponent<Elevator>();
                if (closedScript != null) closedScript.SetClosed();

                // Pick a different point for the working elevator
                Transform exitPoint = null;
                float farthestDist = 0f;
                foreach (var p in points)
                {
                    if (p == arrivalPoint) continue;
                    float d = Vector3.Distance(p.position, arrivalPoint.position);
                    if (d > farthestDist) { farthestDist = d; exitPoint = p; }
                }

                if (exitPoint != null)
                {
                    GameObject elev = Instantiate(elevatorPrefab, exitPoint.position, exitPoint.rotation, _spawnedFloor.transform);
                    elev.name = "Elevator";
                    ElevatorSpawnPosition = exitPoint.position;

                    if (IsBossFloor(_currentFloor))
                    {
                        BossFloorGate gate = elev.GetComponent<BossFloorGate>();
                        if (gate == null) gate = elev.AddComponent<BossFloorGate>();
                        gate.SetFloor(_currentFloor);
                    }
                }
            }
            else
            {
                // Floor 1 or only one spawn point — pick farthest from PlayerSpawn
                Transform playerSpawn = GetRandomTaggedPoint("PlayerSpawn");
                Transform chosen = points[0];

                if (playerSpawn != null && points.Length > 1)
                {
                    float farthest = 0f;
                    foreach (var p in points)
                    {
                        float d = Vector3.Distance(p.position, playerSpawn.position);
                        if (d > farthest) { farthest = d; chosen = p; }
                    }
                }

                GameObject elev = Instantiate(elevatorPrefab, chosen.position, chosen.rotation, _spawnedFloor.transform);
                elev.name = "Elevator";
                ElevatorSpawnPosition = chosen.position;

                if (IsBossFloor(_currentFloor))
                {
                    BossFloorGate gate = elev.GetComponent<BossFloorGate>();
                    if (gate == null) gate = elev.AddComponent<BossFloorGate>();
                    gate.SetFloor(_currentFloor);
                }
            }
        }

        private void SpawnEnemies()
        {
            Transform[] spawnPoints = GetAllTaggedPoints("EnemySpawn");
            if (spawnPoints.Length == 0) return;

            // Shuffle spawn points
            List<Transform> shuffled = new List<Transform>(spawnPoints);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            int count = Mathf.Min(Random.Range(2, 5), shuffled.Count);
            for (int i = 0; i < count; i++)
            {
                EnemyCombatantData data = PickEnemyForFloor(_currentFloor);
                if (data == null) continue;

                GameObject prefabToUse = data.explorationPrefab != null ? data.explorationPrefab : enemyPrefab;
                if (prefabToUse == null) continue;

                GameObject go = Instantiate(prefabToUse, shuffled[i].position, shuffled[i].rotation, transform);
                go.name = $"Enemy_{data.enemyName}_{i}";

                // Wire the enemy data into the battle trigger so hours/rewards work
                var trigger = go.GetComponent<Battlescene_Trigger>();
                if (trigger != null)
                {
                    var field = trigger.GetType().GetField("singleEnemyData",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    field?.SetValue(trigger, data);
                }
            }
        }

        private void SpawnWorkBoxes()
        {
            if (workBoxPrefab == null) return;

            Transform[] spawnPoints = GetAllTaggedPoints("WorkBoxSpawn");
            if (spawnPoints.Length == 0) return;

            List<Transform> shuffled = new List<Transform>(spawnPoints);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            // Use per-floor settings if available, otherwise use defaults
            int min = workBoxCountMin;
            int max = workBoxCountMax;
            FloorSettings fs = _spawnedFloor != null ? _spawnedFloor.GetComponent<FloorSettings>() : null;
            if (fs != null)
            {
                min = fs.minWorkBoxes;
                max = fs.maxWorkBoxes;
            }

            int count = Mathf.Min(Random.Range(min, max + 1), shuffled.Count);
            for (int i = 0; i < count; i++)
            {
                WorkBoxSize size = RollWorkBoxSize(_currentFloor);
                GameObject go = Instantiate(workBoxPrefab, shuffled[i].position, shuffled[i].rotation, transform);
                WorkBox wb = go.GetComponent<WorkBox>();
                if (wb != null) wb.InitializeForFloor(_currentFloor, size);
            }
        }

        // ── NavMesh ────────────────────────────────────────────────────────────

        private void RebuildNavMesh()
        {
            if (navMeshSurface == null) return;
            var buildMethod = navMeshSurface.GetType().GetMethod("BuildNavMesh");
            buildMethod?.Invoke(navMeshSurface, null);
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private Transform GetRandomTaggedPoint(string tag)
        {
            Transform[] all = GetAllTaggedPoints(tag);
            return all.Length > 0 ? all[Random.Range(0, all.Length)] : null;
        }

        private Transform[] GetAllTaggedPoints(string tag)
        {
            if (_spawnedFloor == null) return new Transform[0];
            var list = new List<Transform>();
            foreach (Transform t in _spawnedFloor.GetComponentsInChildren<Transform>())
                if (t.CompareTag(tag)) list.Add(t);
            return list.ToArray();
        }

        public bool IsBossFloor(int floor)
        {
            int interval = gameConfig != null ? gameConfig.bossFloorInterval : 3;
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
    }

    public class EnemySpawnMarker : MonoBehaviour
    {
        public EnemyCombatantData enemyData;
        public int floor;
    }
}
