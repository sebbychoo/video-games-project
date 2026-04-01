using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CardBattle
{
    [RequireComponent(typeof(WorkBox))]
    public class WorkBoxTrigger : MonoBehaviour, IInteractable
    {
        public static bool IsInteracting { get; private set; }
        [SerializeField] private float interactRange = 2f;
        public string InteractPrompt => "Press E to search desk";
        public float InteractRange => interactRange;

        private WorkBox _workBox;
        private bool _playerInRange;
        private bool _isOpen;
        private bool _cursorUnlocked; // only true after safe delay
        private GameObject _playerRoot;

        private MonoBehaviour _fpsController;
        private MonoBehaviour _starterInputs;
        private MonoBehaviour _playerInput;
        private Transform _cameraTarget;
        private Camera _mainCam;

        private Quaternion _savedPlayerRot;
        private Quaternion _savedCamTargetRot;
        private Quaternion _savedMainCamRot;
        private Vector3 _savedMainCamPos;
        private float _savedPitch;

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
                CachePlayerComponents();
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
                // Only unlock cursor after the delayed unlock has fired
                if (_cursorUnlocked)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }

                if (Input.GetKeyDown(KeyCode.Escape))
                    CloseBox();
                return;
            }

            bool canInteract = _playerInRange;
            if (!canInteract)
            {
                Camera cam = Camera.main;
                if (cam != null)
                {
                    Ray ray = new Ray(cam.transform.position, cam.transform.forward);
                    if (Physics.Raycast(ray, out RaycastHit hit, interactRange))
                        canInteract = hit.collider != null &&
                                      hit.collider.GetComponentInParent<WorkBoxTrigger>() == this;
                }
            }

            if (canInteract && Input.GetKeyDown(KeyCode.E))
                OpenBox();
        }

        private void LateUpdate()
        {
            if (_isOpen)
            {
                RestoreCameraOrientation();
                RestorePitchField();
            }
        }

        private void OpenBox()
        {
            EnsurePlayerRoot();
            CachePlayerComponents();
            SaveCameraState();

            _isOpen = true;
            _cursorUnlocked = false; // DON'T unlock cursor yet
            IsInteracting = true;

            // Disable ALL input — PlayerInput, FPS controller, StarterAssetsInputs
            DisablePlayerInput();

            SetEnemiesActive(false);
            EnsureEventSystem();

            RestoreCameraOrientation();
            RestorePitchField();

            _workBox.Open();

            // Ensure the Canvas has a GraphicRaycaster and worldCamera for clicks
            EnsureCanvasRaycasting();

            // Unlock cursor after 2 frames — by then all input is dead
            StartCoroutine(DelayedCursorUnlock());
        }

        private IEnumerator DelayedCursorUnlock()
        {
            yield return null;
            yield return null;
            // Restore camera one more time before unlocking
            RestoreCameraOrientation();
            RestorePitchField();
            _cursorUnlocked = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void CloseBox()
        {
            if (!_isOpen) return;

            _isOpen = false;
            _cursorUnlocked = false;
            _workBox.CloseInventory();
            SetEnemiesActive(true);

            RestoreCameraOrientation();
            RestorePitchField();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            StartCoroutine(RestorePlayerDelayed());
        }

        private IEnumerator RestorePlayerDelayed()
        {
            yield return null;
            IsInteracting = false;
            yield return null;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            RestoreCameraOrientation();
            RestorePitchField();
            EnablePlayerInput();
        }

        // ── Input enable/disable ──────────────────────────────────────

        private void DisablePlayerInput()
        {
            if (_starterInputs != null)
            {
                var lookField = _starterInputs.GetType().GetField("look");
                lookField?.SetValue(_starterInputs, Vector2.zero);
                var moveField = _starterInputs.GetType().GetField("move");
                moveField?.SetValue(_starterInputs, Vector2.zero);
                _starterInputs.enabled = false;
            }
            if (_fpsController != null) _fpsController.enabled = false;
            if (_playerInput != null)
            {
                var method = _playerInput.GetType().GetMethod("DeactivateInput");
                method?.Invoke(_playerInput, null);
            }

            // Disable Cinemachine Brain so it stops overriding the camera
            _mainCam = Camera.main;
            if (_mainCam != null)
            {
                foreach (var mb in _mainCam.GetComponents<MonoBehaviour>())
                {
                    if (mb.GetType().Name.Contains("CinemachineBrain") ||
                        mb.GetType().Name.Contains("CinemachineCamera"))
                        mb.enabled = false;
                }
            }
        }

        private void EnablePlayerInput()
        {
            if (_starterInputs != null)
            {
                var lookField = _starterInputs.GetType().GetField("look");
                lookField?.SetValue(_starterInputs, Vector2.zero);
                var moveField = _starterInputs.GetType().GetField("move");
                moveField?.SetValue(_starterInputs, Vector2.zero);
                _starterInputs.enabled = true;
            }
            if (_playerInput != null)
            {
                var method = _playerInput.GetType().GetMethod("ActivateInput");
                method?.Invoke(_playerInput, null);
            }
            if (_fpsController != null) _fpsController.enabled = true;

            // Re-enable Cinemachine Brain
            if (_mainCam != null)
            {
                foreach (var mb in _mainCam.GetComponents<MonoBehaviour>())
                {
                    if (mb.GetType().Name.Contains("CinemachineBrain") ||
                        mb.GetType().Name.Contains("CinemachineCamera"))
                        mb.enabled = true;
                }
            }
        }

        // ── Camera state ──────────────────────────────────────────────

        private void SaveCameraState()
        {
            if (_playerRoot != null)
                _savedPlayerRot = _playerRoot.transform.rotation;
            if (_cameraTarget != null)
                _savedCamTargetRot = _cameraTarget.localRotation;
            if (_fpsController != null)
            {
                var f = _fpsController.GetType().GetField("_cinemachineTargetPitch",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (f != null) _savedPitch = (float)f.GetValue(_fpsController);
            }
            // Save main camera transform too
            _mainCam = Camera.main;
            if (_mainCam != null)
            {
                _savedMainCamRot = _mainCam.transform.rotation;
                _savedMainCamPos = _mainCam.transform.position;
            }
        }

        private void RestoreCameraOrientation()
        {
            if (_playerRoot != null)
                _playerRoot.transform.rotation = _savedPlayerRot;
            if (_cameraTarget != null)
                _cameraTarget.localRotation = _savedCamTargetRot;
            // Also force the main camera back — overrides Cinemachine
            if (_mainCam != null)
            {
                _mainCam.transform.rotation = _savedMainCamRot;
                _mainCam.transform.position = _savedMainCamPos;
            }
        }

        private void RestorePitchField()
        {
            if (_fpsController == null) return;
            var f = _fpsController.GetType().GetField("_cinemachineTargetPitch",
                BindingFlags.NonPublic | BindingFlags.Instance);
            f?.SetValue(_fpsController, _savedPitch);
        }

        // ── Caching ───────────────────────────────────────────────────

        private void EnsurePlayerRoot()
        {
            if (_playerRoot != null) return;
            var go = GameObject.FindWithTag("Player");
            if (go != null) _playerRoot = go.transform.root.gameObject;
        }

        private void CachePlayerComponents()
        {
            if (_playerRoot == null) return;
            if (_fpsController != null && _starterInputs != null && _playerInput != null) return;

            foreach (var mb in _playerRoot.GetComponentsInChildren<MonoBehaviour>())
            {
                string n = mb.GetType().Name;
                if (n == "FirstPersonController")
                {
                    _fpsController = mb;
                    var camField = mb.GetType().GetField("CinemachineCameraTarget");
                    if (camField != null)
                    {
                        var camGO = camField.GetValue(mb) as GameObject;
                        if (camGO != null) _cameraTarget = camGO.transform;
                    }
                }
                else if (n == "StarterAssetsInputs") _starterInputs = mb;
                else if (n == "PlayerInput") _playerInput = mb;
            }
        }

        // ── Enemies ───────────────────────────────────────────────────

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

        private static void EnsureEventSystem()
        {
            if (EventSystem.current == null)
            {
                var go = new GameObject("EventSystem");
                go.AddComponent<EventSystem>();
                go.AddComponent<StandaloneInputModule>();
            }
        }

        private void EnsureCanvasRaycasting()
        {
            // Find all Canvases in this WorkBox and ensure they can receive clicks
            foreach (var canvas in GetComponentsInChildren<Canvas>(true))
            {
                if (canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                    canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

                // World-space canvases need a camera for raycasting
                if (canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null)
                    canvas.worldCamera = Camera.main;
            }
        }
    }
}
