using System.Collections;
using UnityEngine;
using TMPro;
using CardBattle;
using Procedural;

namespace CardBattle
{
    /// <summary>
    /// Floor exit elevator with sprite-based door animation.
    /// Flow: Press E → door close frames play → loading screen → new floor → door open frames play.
    /// No Animator needed — just drag sprite frames into the arrays.
    /// </summary>
    public class Elevator : MonoBehaviour, IInteractable
    {
        public string InteractPrompt => "Press E to descend";
        public float InteractRange => 4f;
        [SerializeField] float interactRange = 2f;

        [Header("Floor Display")]
        [SerializeField] TMP_Text floorLabel;

        [Header("Door Animation")]
        [SerializeField] SpriteRenderer doorSprite;
        [SerializeField] Sprite[] doorCloseFrames;
        [SerializeField] Sprite[] doorOpenFrames;
        [SerializeField] float frameRate = 8f;

        private bool _playerInRange;
        private BossFloorGate _gate;

        private static bool _anyElevatorDescending;
        public static bool IsDescending => _anyElevatorDescending;

        private void Awake()
        {
            _gate = GetComponent<BossFloorGate>();

            Collider col = GetComponent<Collider>();
            if (col == null)
            {
                BoxCollider box = gameObject.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(2f, 3f, 2f);
            }
            else if (!col.isTrigger)
            {
                col.isTrigger = true;
            }

            if (doorSprite == null)
                doorSprite = GetComponentInChildren<SpriteRenderer>();
        }

        private void Start()
        {
            int floor = 1;
            if (SaveManager.Instance != null && SaveManager.Instance.CurrentRun != null)
                floor = SaveManager.Instance.CurrentRun.currentFloor;
            SetFloorLabel(floor);

            // Play door open animation on arrival
            if (doorOpenFrames != null && doorOpenFrames.Length > 0)
                StartCoroutine(PlayFrames(doorOpenFrames));
        }

        public void SetFloorLabel(int floor)
        {
            if (floorLabel != null)
                floorLabel.text = $"{floor}";
        }

        private void LateUpdate()
        {
            if (floorLabel != null && Camera.main != null)
            {
                Vector3 dir = Camera.main.transform.position - floorLabel.transform.position;
                dir.y = 0;
                if (dir.sqrMagnitude > 0.001f)
                    floorLabel.transform.rotation = Quaternion.LookRotation(dir);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
                _playerInRange = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
                _playerInRange = false;
        }

        private void Update()
        {
            if (_anyElevatorDescending) return;
            if (!_playerInRange) return;
            if (!Input.GetKeyDown(KeyCode.E)) return;

            if (_gate != null && !_gate.TryExit())
                return;

            _anyElevatorDescending = true;
            enabled = false;
            StartCoroutine(DescendSequence());
        }

        private void OnDestroy()
        {
            _anyElevatorDescending = false;
        }

        private IEnumerator DescendSequence()
        {
            // 1. Play door close animation
            if (doorCloseFrames != null && doorCloseFrames.Length > 0)
                yield return StartCoroutine(PlayFrames(doorCloseFrames));

            // 2. Floor transition
            SaveManager sm = FindObjectOfType<SaveManager>();
            if (sm == null || sm.CurrentRun == null) yield break;

            sm.CurrentRun.spawnX = transform.position.x;
            sm.CurrentRun.spawnZ = transform.position.z;
            sm.CurrentRun.hasCustomSpawn = true;
            sm.CurrentRun.currentFloor++;
            int nextFloor = sm.CurrentRun.currentFloor;

            if (SceneLoader.Instance != null)
                SceneLoader.Instance.useDefaultSpawn = true;

            // 3. Load with loading screen (door open plays on Start of new elevator)
            LoadingScreen ls = LoadingScreen.Instance;
            if (ls == null) ls = FindObjectOfType<LoadingScreen>();

            if (ls != null)
                ls.LoadElevator("Explorationscene");
            else
                FindObjectOfType<SceneLoader>()?.LoadExploration();
        }

        /// <summary>
        /// Plays sprite frames on the door SpriteRenderer, then stops on the last frame.
        /// </summary>
        private IEnumerator PlayFrames(Sprite[] frames)
        {
            if (doorSprite == null || frames.Length == 0) yield break;

            float interval = 1f / Mathf.Max(frameRate, 1f);
            for (int i = 0; i < frames.Length; i++)
            {
                doorSprite.sprite = frames[i];
                yield return new WaitForSeconds(interval);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, interactRange);
        }
    }
}
