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
        [SerializeField] List<GameObject> floorPrefabs;        // normal floors
        [SerializeField] List<GameObject> bossFloorPrefabs;    // boss-specific floors (optional)

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

        /// <summary>World position of the elevator interior — used as spawn point on next floor.</summary>
        public Vector3 ElevatorSpawnPosition { get; private set; }

        // ── Public API ─────────────────────────────────────────────────────────

        public static void ResetRunFlags() => _suicidalWorkerPlacedThisRun = false;

        private void Start()
        {
            // Don't auto-generate if SceneLoader exists — it will call Generate
            // with the correct floor number from OnSceneLoaded
            if (SceneLoader.Instance == null)
                Generate(testFloor);
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
        }

        // ── Floor selection ────────────────────────────────────────────────────

        private void SpawnFloor()
        {
            List<GameObject> pool = (IsBossFloor(_currentFloor) && bossFloorPrefabs != null && bossFloorPrefabs.Count > 0)
                ? bossFloorPrefabs : floorPrefabs;

            if (pool == null || pool.Count == 0)
            {
                Debug.LogWarning("LevelGenerator: No floor prefabs assigned.");
                return;
            }

            GameObject chosen = pool[Random.Range(0, pool.Count)];
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

            // Find all ElevatorSpawn points in the floor prefab and pick one randomly
            Transform[] points = GetAllTaggedPoints("ElevatorSpawn");
            if (points.Length == 0)
            {
                Debug.LogWarning("LevelGenerator: No ElevatorSpawn tags found in floor prefab.");
                return;
            }

            Transform chosen = points[Random.Range(0, points.Length)];
            GameObject elev = Instantiate(elevatorPrefab, chosen.position, chosen.rotation, _spawnedFloor.transform);
            elev.name = "Elevator";

            // Store elevator position as spawn point on next floor
            ElevatorSpawnPosition = chosen.position;

            // Wire BossFloorGate if needed
            if (IsBossFloor(_currentFloor))
            {
                BossFloorGate gate = elev.GetComponent<BossFloorGate>();
                if (gate == null) gate = elev.AddComponent<BossFloorGate>();
                gate.SetFloor(_currentFloor);
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

            int count = Mathf.Min(Random.Range(workBoxCountMin, workBoxCountMax + 1), shuffled.Count);
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
