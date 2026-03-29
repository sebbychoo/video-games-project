using UnityEngine;

namespace CardBattle
{
    [RequireComponent(typeof(WorkBox))]
    public class WorkBoxTrigger : MonoBehaviour
    {
        public static bool IsInteracting { get; private set; }

        private WorkBox _workBox;
        private bool _playerInRange;
        private bool _isOpen;
        private GameObject _playerRoot;

        private void Awake()
        {
            _workBox = GetComponent<WorkBox>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                _playerInRange = true;
                _playerRoot = other.transform.root.gameObject;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                _playerInRange = false;
                if (_isOpen) CloseBox();
            }
        }

        private void Update()
        {
            if (_isOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            if (_playerInRange && !_isOpen && Input.GetKeyDown(KeyCode.E))
                OpenBox();
            else if (_isOpen && Input.GetKeyDown(KeyCode.Escape))
                CloseBox();
        }

        private void OpenBox()
        {
            _isOpen = true;
            IsInteracting = true;
            SetPlayerMovement(false);
            SetEnemiesActive(false);
            _workBox.Open();
        }

        private void CloseBox()
        {
            _isOpen = false;
            IsInteracting = false;
            _workBox.CloseInventory();
            SetPlayerMovement(true);
            SetEnemiesActive(true);
        }

        private void SetPlayerMovement(bool enabled)
        {
            if (_playerRoot == null) return;
            foreach (var mb in _playerRoot.GetComponentsInChildren<MonoBehaviour>())
            {
                if (mb is WorkBox || mb is WorkBoxTrigger) continue;
                // Skip EventSystem and UI-related components
                string fullName = mb.GetType().FullName ?? "";
                if (fullName.Contains("UnityEngine.EventSystems")) continue;
                if (fullName.Contains("UnityEngine.UI")) continue;

                string t = mb.GetType().Name;
                // Only disable scripts that are clearly player movement/camera
                if (t == "PlayerMovement" || t == "PlayerController" ||
                    t == "FirstPersonController" || t == "CharacterController" ||
                    t == "MouseLook" || t == "CameraController" || t == "CameraManager" ||
                    t == "PlayerLook" || t == "FPSController" ||
                    t.Contains("Movement") || t.Contains("FPSControl"))
                    mb.enabled = enabled;
            }
        }

        private static void SetEnemiesActive(bool enabled)
        {
            // Freeze/unfreeze all EnemyFollow scripts and NavMeshAgents in the scene
            foreach (var enemy in Object.FindObjectsOfType<MonoBehaviour>())
            {
                string t = enemy.GetType().Name.ToLower();
                if (t.Contains("enemy") || t.Contains("follow") || t.Contains("patrol"))
                    enemy.enabled = enabled;
            }
            // Also freeze NavMeshAgents so enemies stop moving
            foreach (var agent in Object.FindObjectsOfType<UnityEngine.AI.NavMeshAgent>())
            {
                agent.isStopped = !enabled;
            }
        }
    }
}
