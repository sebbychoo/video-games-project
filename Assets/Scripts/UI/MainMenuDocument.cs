using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

namespace CardBattle
{
    /// <summary>
    /// Runtime controller for the OVERTIME main menu UI Toolkit document.
    ///
    /// The right panel IS the hub — desk items are hub upgrades.
    /// Two-panel layout behaviour:
    ///   • Both panels visible by default.
    ///   • Clicking the desk surface OR the HUB button slides the left panel out.
    ///   • Moving the cursor within <see cref="LeftEdgeReturnZonePx"/> of the
    ///     left screen edge slides the left panel back in.
    ///
    /// New run flow:
    ///   NEW RUN → OpeningDialogue → NO → returns here (hub visible) → NEW RUN again → Explorationscene
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuDocument : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────

        [Header("Scene Names")]
        [SerializeField] private string continueRunScene  = "Explorationscene";
        [SerializeField] private string achievementsScene = "";

        // ── USS class constants ───────────────────────────────────────────────

        private const string ClassHidden = "hidden";

        // ── Private state ────────────────────────────────────────────────────

        private UIDocument    _doc;
        private VisualElement _root;

        // Left panel elements
        private VisualElement _leftPanel;
        private VisualElement _panelDivider;
        private VisualElement _itemContinue;

        // Buttons
        private Button _btnNewRun;
        private Button _btnContinue;
        private Button _btnHub;
        private Button _btnAchievements;
        private Button _btnQuit;

        // Right panel elements
        private VisualElement _deskSurface;
        private VisualElement _hubHintBlock;
        private Label         _returnHint;

        // Slide state
        private bool _panelSlid;

        // How close (in px) the cursor must be to the left edge to recall the panel
        private const float LeftEdgeReturnZonePx = 40f;

        private const string ClassPanelHidden    = "left-panel--hidden";
        private const string ClassHintHidden     = "hub-hint-block--hidden";

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void Awake()
        {
            _doc = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            _root = _doc.rootVisualElement;
            QueryElements();
            RefreshContinueVisibility();
            RegisterCallbacks();
        }

        private void OnDisable()
        {
            UnregisterCallbacks();
        }

        private void Start()
        {
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible   = true;
            Time.timeScale = 1f;

            EnsureSaveManager();
            RefreshContinueVisibility();

            // Panel starts visible
            SetPanelSlid(false);
        }

        private void Update()
        {
            // Return the panel when the cursor enters the left-edge zone
            if (_panelSlid)
            {
                float cursorX = Input.mousePosition.x;
                if (cursorX <= LeftEdgeReturnZonePx)
                    SetPanelSlid(false);
            }
        }

        // ── Element queries ──────────────────────────────────────────────────

        /// <summary>Resolves all named elements from the UXML tree.</summary>
        private void QueryElements()
        {
            _leftPanel    = _root.Q<VisualElement>("left-panel");
            _panelDivider = _root.Q<VisualElement>("panel-divider");
            _itemContinue = _root.Q<VisualElement>("item-continue");
            _deskSurface  = _root.Q<VisualElement>("desk-surface");
            _hubHintBlock = _root.Q<VisualElement>("hub-hint-block");
            _returnHint   = _root.Q<Label>("return-hint");

            _btnNewRun       = _root.Q<Button>("btn-new-run");
            _btnContinue     = _root.Q<Button>("btn-continue");
            _btnHub          = _root.Q<Button>("btn-hub");
            _btnAchievements = _root.Q<Button>("btn-achievements");
            _btnQuit         = _root.Q<Button>("btn-quit");
        }

        // ── Callbacks ────────────────────────────────────────────────────────

        private void RegisterCallbacks()
        {
            _btnNewRun?.RegisterCallback<ClickEvent>(OnNewRun);
            _btnContinue?.RegisterCallback<ClickEvent>(OnContinue);
            _btnHub?.RegisterCallback<ClickEvent>(OnHubButton);
            _btnAchievements?.RegisterCallback<ClickEvent>(OnAchievements);
            _btnQuit?.RegisterCallback<ClickEvent>(OnQuit);
            _deskSurface?.RegisterCallback<ClickEvent>(OnDeskClicked);
        }

        private void UnregisterCallbacks()
        {
            _btnNewRun?.UnregisterCallback<ClickEvent>(OnNewRun);
            _btnContinue?.UnregisterCallback<ClickEvent>(OnContinue);
            _btnHub?.UnregisterCallback<ClickEvent>(OnHubButton);
            _btnAchievements?.UnregisterCallback<ClickEvent>(OnAchievements);
            _btnQuit?.UnregisterCallback<ClickEvent>(OnQuit);
            _deskSurface?.UnregisterCallback<ClickEvent>(OnDeskClicked);
        }

        // ── Desk focus ───────────────────────────────────────────────────────

        private void OnDeskClicked(ClickEvent _) => SetPanelSlid(true);

        /// <summary>Slides the nav panel in (false) or out (true).</summary>
        private void SetPanelSlid(bool slid)
        {
            _panelSlid = slid;

            _leftPanel?.EnableInClassList(ClassPanelHidden, slid);
            _panelDivider?.EnableInClassList(ClassPanelHidden, slid);
            _hubHintBlock?.EnableInClassList(ClassHintHidden, slid);
            _returnHint?.EnableInClassList(ClassHidden, !slid);
        }

        // ── Button handlers ──────────────────────────────────────────────────

        /// <summary>New Run — shows the inline OpeningDialogue panel (boss question).
        /// On second click after NO path completes, goes straight to Explorationscene.</summary>
        private void OnNewRun(ClickEvent _)
        {
            if (SaveManager.Instance != null
                && SaveManager.Instance.CurrentRun != null
                && !SaveManager.Instance.CurrentRun.isActive
                && SaveManager.Instance.CurrentRun.deckCardIds != null
                && SaveManager.Instance.CurrentRun.deckCardIds.Count == 0)
            {
                // Run already wiped by NO path — go straight to exploration for deck carousel
                if (SceneLoader.Instance != null)
                {
                    SceneLoader.Instance.useDefaultSpawn = true;
                    SceneLoader.Instance.enemyDefeated   = false;
                    SceneLoader.Instance.LoadSceneUI("Explorationscene");
                }
                else
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene("Explorationscene");
                }
                return;
            }

            // Show the inline OpeningDialogue panel (handled by MainMenu/OpeningDialogue on MenuManager)
            // This document version delegates to the UGUI component on the same GameObject
            var dialogue = GetComponent<OpeningDialogue>();
            if (dialogue != null)
                dialogue.Show();
            else
                LoadScene("Explorationscene");
        }

        /// <summary>Restores any pre-encounter snapshot and resumes the run.</summary>
        private void OnContinue(ClickEvent _)
        {
            if (SaveManager.Instance == null) return;

            if (SaveManager.Instance.HasPreEncounterSnapshot())
                SaveManager.Instance.RestorePreEncounter();

            RunState run = SaveManager.Instance.CurrentRun;
            if (run == null || !run.isActive) return;

            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.useDefaultSpawn = true;
                SceneLoader.Instance.LoadSceneUI(continueRunScene);
            }
            else
            {
                SceneManager.LoadScene(continueRunScene);
            }
        }

        /// <summary>HUB button — slides the desk into focus (hub lives in the right panel).</summary>
        private void OnHubButton(ClickEvent _) => SetPanelSlid(true);

        /// <summary>Achievements — loads scene or opens overlay depending on config.</summary>
        private void OnAchievements(ClickEvent _)
        {
            if (!string.IsNullOrEmpty(achievementsScene))
                LoadScene(achievementsScene);
        }

        /// <summary>Quit — exits the application (stops play mode in the editor).</summary>
        private void OnQuit(ClickEvent _)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ── Continue Run visibility ──────────────────────────────────────────

        /// <summary>
        /// Shows the Continue Run row only when a valid active run exists —
        /// isActive must be true AND the run must have real content (HP or deck cards).
        /// The pre-encounter snapshot is deserialized and validated with the same check.
        /// </summary>
        public void RefreshContinueVisibility()
        {
            if (_itemContinue == null) return;

            bool hasSave = false;

            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.LoadRun();
                hasSave = IsRunValid(SaveManager.Instance.CurrentRun)
                       || IsSnapshotValid();
            }

            _itemContinue.EnableInClassList(ClassHidden, !hasSave);
        }

        private static bool IsRunValid(RunState run) =>
            run != null
            && run.isActive
            && (run.playerMaxHP > 0 || (run.deckCardIds != null && run.deckCardIds.Count > 0));

        private static bool IsSnapshotValid()
        {
            if (!SaveManager.Instance.HasPreEncounterSnapshot()) return false;
            try
            {
                string path = System.IO.Path.Combine(
                    Application.persistentDataPath, "pre_encounter_save.json");
                string json = System.IO.File.ReadAllText(path);
                return IsRunValid(JsonUtility.FromJson<RunState>(json));
            }
            catch { return false; }
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static void LoadScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return;

            if (SceneLoader.Instance != null)
                SceneLoader.Instance.LoadSceneMenu(sceneName);
            else
                SceneManager.LoadScene(sceneName);
        }

        private void EnsureSaveManager()
        {
            if (SaveManager.Instance == null)
                new GameObject("SaveManager").AddComponent<SaveManager>();

            SaveManager.Instance.LoadMeta();
        }
    }
}
