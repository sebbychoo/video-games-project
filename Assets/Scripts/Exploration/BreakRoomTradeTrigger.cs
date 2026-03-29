using UnityEngine;

namespace CardBattle
{
    [RequireComponent(typeof(BreakRoomTrade))]
    public class BreakRoomTradeTrigger : MonoBehaviour
    {
        public static bool IsInteracting { get; private set; }

        [Header("UI References")]
        [SerializeField] private GameObject tradePanel;

        private BreakRoomTrade _trade;
        private bool _playerInRange;
        private bool _isOpen;
        private GameObject _playerRoot;

        private void Awake()
        {
            _trade = GetComponent<BreakRoomTrade>();
            if (tradePanel != null) tradePanel.SetActive(false);
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
                if (_isOpen) CloseTradeUI();
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
                OpenTradeUI();
            else if (_isOpen && Input.GetKeyDown(KeyCode.Escape))
                CloseTradeUI();
        }

        private void OpenTradeUI()
        {
            _isOpen = true;
            IsInteracting = true;
            if (!_trade.IsInitialized) _trade.Initialize();
            if (tradePanel != null) tradePanel.SetActive(true);
            SetPlayerMovement(false);
            SetEnemiesActive(false);
        }

        public void CloseTradeUI()
        {
            _isOpen = false;
            IsInteracting = false;
            if (tradePanel != null) tradePanel.SetActive(false);
            SetPlayerMovement(true);
            SetEnemiesActive(true);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void OnAcceptClicked()
        {
            _trade.AcceptTrade();
            CloseTradeUI();
        }

        public void OnDeclineClicked()
        {
            _trade.DeclineTrade();
            CloseTradeUI();
        }

        private void SetPlayerMovement(bool enabled)
        {
            if (_playerRoot == null) return;
            foreach (var mb in _playerRoot.GetComponentsInChildren<MonoBehaviour>())
            {
                if (mb is BreakRoomTrade || mb is BreakRoomTradeTrigger) continue;
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
