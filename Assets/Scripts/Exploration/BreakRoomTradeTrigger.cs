using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CardBattle
{
    [RequireComponent(typeof(BreakRoomTrade))]
    public class BreakRoomTradeTrigger : MonoBehaviour, IInteractable
    {
        public static bool IsInteracting { get; private set; }
        public string InteractPrompt => "Press E to trade";
        public float InteractRange => 3f;

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
                return;
            }
            if (_playerInRange && Input.GetKeyDown(KeyCode.E))
                OpenTradeUI();
        }

        private void LateUpdate()
        {
            if (_isOpen && Input.GetKeyDown(KeyCode.Escape))
                CloseTradeUI();
        }

        private void OpenTradeUI()
        {
            EnsurePlayerRoot();

            _isOpen = true;
            IsInteracting = true;
            SetPlayerControllers(false);
            if (!_trade.IsInitialized) _trade.Initialize();
            if (tradePanel != null) tradePanel.SetActive(true);
            SetEnemiesActive(false);
            EnsureEventSystem();

            GetComponent<SafeRoomNPCDialogue>()?.OnPlayerInteract();
        }

        public void CloseTradeUI()
        {
            _isOpen = false;
            IsInteracting = false;
            if (tradePanel != null) tradePanel.SetActive(false);
            SetEnemiesActive(true);
            StartCoroutine(RestorePlayerNextFrame());
        }

        private IEnumerator RestorePlayerNextFrame()
        {
            yield return null;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            SetPlayerControllers(true);
        }

        private void EnsurePlayerRoot()
        {
            if (_playerRoot != null) return;
            GameObject playerGO = GameObject.FindWithTag("Player");
            if (playerGO != null) _playerRoot = playerGO.transform.root.gameObject;
        }

        private void SetPlayerControllers(bool enabled)
        {
            if (_playerRoot == null) return;
            foreach (var mb in _playerRoot.GetComponentsInChildren<MonoBehaviour>())
            {
                string typeName = mb.GetType().Name;
                if (typeName == "FirstPersonController" || typeName == "StarterAssetsInputs")
                {
                    mb.enabled = enabled;
                    if (!enabled && typeName == "StarterAssetsInputs")
                    {
                        var lookField = mb.GetType().GetField("look");
                        lookField?.SetValue(mb, Vector2.zero);
                        var moveField = mb.GetType().GetField("move");
                        moveField?.SetValue(mb, Vector2.zero);
                    }
                }
            }
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current == null)
            {
                var go = new GameObject("EventSystem");
                go.AddComponent<EventSystem>();
                go.AddComponent<StandaloneInputModule>();
            }
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
