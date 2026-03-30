using UnityEngine;

namespace CardBattle
{
    [RequireComponent(typeof(BathroomShop))]
    public class BathroomShopTrigger : MonoBehaviour
    {
        public static bool IsInteracting { get; private set; }

        [Header("UI References")]
        [SerializeField] private GameObject shopPanel;

        private BathroomShop _shop;
        private bool _playerInRange;
        private bool _isOpen;
        private GameObject _playerRoot;

        private void Awake()
        {
            _shop = GetComponent<BathroomShop>();
            if (shopPanel != null) shopPanel.SetActive(false);
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
                if (_isOpen) CloseShop();
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
                OpenShop();
            else if (_isOpen && Input.GetKeyDown(KeyCode.Escape))
                CloseShop();
        }

        private void OpenShop()
        {
            _isOpen = true;
            IsInteracting = true;
            if (!_shop.IsInitialized) _shop.Initialize();
            if (shopPanel != null) shopPanel.SetActive(true);
            SetPlayerMovement(false);
            SetEnemiesActive(false);

            // Req 37.5 — NPC safety line on first interaction while enemies are on the floor
            GetComponent<SafeRoomNPCDialogue>()?.OnPlayerInteract();
        }

        public void CloseShop()
        {
            _isOpen = false;
            IsInteracting = false;
            _shop.ResetVisit();
            if (shopPanel != null) shopPanel.SetActive(false);
            SetPlayerMovement(true);
            SetEnemiesActive(true);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void SetPlayerMovement(bool enabled)
        {
            if (_playerRoot == null) return;
            foreach (var mb in _playerRoot.GetComponentsInChildren<MonoBehaviour>())
            {
                if (mb is BathroomShop || mb is BathroomShopTrigger) continue;
                string fullName = mb.GetType().FullName ?? "";
                if (fullName.Contains("UnityEngine.EventSystems") || fullName.Contains("UnityEngine.UI")) continue;
                string t = mb.GetType().Name;
                if (t.Contains("Movement") || t.Contains("Controller") || t.Contains("Look") || t.Contains("Camera"))
                    mb.enabled = enabled;
            }
        }

        private static void SetEnemiesActive(bool enabled)
        {
            foreach (var enemy in Object.FindObjectsOfType<MonoBehaviour>())
            {
                string t = enemy.GetType().Name.ToLower();
                if (t.Contains("enemy") || t.Contains("follow") || t.Contains("patrol"))
                    enemy.enabled = enabled;
            }
            foreach (var agent in Object.FindObjectsOfType<UnityEngine.AI.NavMeshAgent>())
                agent.isStopped = !enabled;
        }
    }
}
