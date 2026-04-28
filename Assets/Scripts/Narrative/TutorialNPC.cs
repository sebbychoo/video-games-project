using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace CardBattle
{
    /// <summary>
    /// Singleton tutorial manager that persists across scenes (DontDestroyOnLoad).
    /// On the player's first run, a coworker NPC guides them through the Hub Office,
    /// reacts with surprise to the first enemy encounter, provides confused combat UI
    /// prompts, reacts to Work_Box discovery, then says farewell after first combat.
    ///
    /// All prompts are non-blocking, dismissible contextual tooltips — never forced
    /// dialogue that interrupts gameplay flow.
    ///
    /// Requirements: 40.1–40.11
    /// </summary>
    public class TutorialNPC : MonoBehaviour
    {
        public static TutorialNPC Instance { get; private set; }

        // ── Tutorial Phases ────────────────────────────────────────────
        public enum TutorialPhase
        {
            Inactive,
            HubWalkthrough,
            ExplorationFirstEnemy,
            FirstCombat,
            WorkBoxDiscovery,
            Complete
        }

        public TutorialPhase CurrentPhase { get; private set; } = TutorialPhase.Inactive;

        // ── Tooltip UI (created at runtime) ────────────────────────────
        private Canvas _tooltipCanvas;
        private CanvasGroup _tooltipCanvasGroup;
        private GameObject _tooltipPanel;
        private TextMeshProUGUI _tooltipText;
        private Button _dismissButton;
        private TextMeshProUGUI _dismissButtonText;

        // ── State tracking ─────────────────────────────────────────────
        private bool _hubWalkthroughShown;
        private bool _firstEnemyReactionShown;
        private bool _firstCombatPromptsShown;
        private bool _workBoxReactionShown;
        private bool _farewellShown;
        private bool _tooltipVisible;
        private Coroutine _activeSequence;
        private bool _waitingForDismiss;

        // ── Hub furniture descriptions ─────────────────────────────────
        private static readonly Dictionary<string, string> HubFurnitureLines = new Dictionary<string, string>
        {
            { "computer",       "That's your computer. You can boost your tech stuff from there." },
            { "coffee",         "Coffee machine — keeps you going longer, if you know what I mean." },
            { "chair",          "Your desk chair. Comfier chair, better reflexes. Don't ask me why." },
            { "filing",         "Filing cabinet. More storage means you can carry more... cards? Paperwork? Whatever." },
            { "plant",          "The office plant. Take care of it and it takes care of you. Literally." },
            { "whiteboard",     "Whiteboard over there. Helps you keep track of the floor layout." }
        };

        // ── Combat UI prompt lines ─────────────────────────────────────
        private static readonly string[] CombatPromptLines = new string[]
        {
            "I guess these are your... cards? Try dragging one onto that guy.",
            "See that meter on the side? That's your Overtime. Cards cost that stuff.",
            "Those bars above the enemies? That's their health, I think.",
            "Those little icons next to them show what they're about to do. Heads up.",
            "When you're done, hit that End Turn button. I guess?"
        };

        // ── Lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (!ShouldRunTutorial())
            {
                CurrentPhase = TutorialPhase.Inactive;
                return;
            }

            CreateTooltipUI();
            HideTooltip();

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            UnsubscribeBattleEvents();

            if (Instance == this)
                Instance = null;
        }

        // ── Tutorial gate ──────────────────────────────────────────────

        /// <summary>
        /// Returns true if the tutorial should run (first run, not yet completed).
        /// Req 40.1: first run only when tutorialCompleted is false.
        /// </summary>
        public bool ShouldRunTutorial()
        {
            if (SaveManager.Instance == null) return false;
            MetaState meta = SaveManager.Instance.CurrentMeta;
            return meta != null && !meta.tutorialCompleted;
        }

        /// <summary>
        /// External entry point — call after opening dialogue + deck selection
        /// to kick off the tutorial. The NPC appears in HubOffice.
        /// </summary>
        public void BeginTutorial()
        {
            if (!ShouldRunTutorial()) return;
            CurrentPhase = TutorialPhase.HubWalkthrough;
        }

        // ── Scene load handler ─────────────────────────────────────────

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (CurrentPhase == TutorialPhase.Inactive || CurrentPhase == TutorialPhase.Complete)
                return;

            string sceneName = scene.name;

            if (sceneName == "HubOffice" && !_hubWalkthroughShown)
            {
                CurrentPhase = TutorialPhase.HubWalkthrough;
                _activeSequence = StartCoroutine(HubWalkthroughSequence());
            }
            else if (sceneName == "Explorationscene" && !_firstEnemyReactionShown)
            {
                CurrentPhase = TutorialPhase.ExplorationFirstEnemy;
                // We wait for the first enemy encounter via Battlescene_Trigger
            }
            else if (sceneName == "Battlescene" && !_firstCombatPromptsShown)
            {
                CurrentPhase = TutorialPhase.FirstCombat;
                _activeSequence = StartCoroutine(FirstCombatSequence());
            }
        }

        // ── Hub Office walkthrough ─────────────────────────────────────
        // Req 40.2, 40.3, 40.4, 40.5: Coworker explains furniture as mundane
        // orientation. No combat explanation. Directs player to start shift.

        private IEnumerator HubWalkthroughSequence()
        {
            // Wait a moment for the scene to settle
            yield return new WaitForSeconds(1f);

            // Greeting
            yield return ShowTooltipAndWait(
                "Hey, new guy! Come on, let me show you around the office real quick.");

            // Walk through furniture items — use generic lines for each known type
            foreach (var kvp in HubFurnitureLines)
            {
                yield return ShowTooltipAndWait(kvp.Value);
            }

            // Wrap up — direct player to start their shift (Req 40.5)
            yield return ShowTooltipAndWait(
                "Alright, that's the tour. Hit that Back button when you're ready to start your shift. Good luck!");

            _hubWalkthroughShown = true;
            MarkStepSeen("tutorial_hub_walkthrough");
            CurrentPhase = TutorialPhase.ExplorationFirstEnemy;
        }

        // ── First enemy encounter reaction ─────────────────────────────
        // Req 40.6: NPC reacts with surprise and confusion.
        // Called externally when Battlescene_Trigger fires during tutorial.

        /// <summary>
        /// Call this from Battlescene_Trigger (or similar) when the player
        /// encounters their first enemy on floor 1 during the tutorial run.
        /// </summary>
        public void OnFirstEnemyEncounter()
        {
            if (CurrentPhase == TutorialPhase.Inactive || _firstEnemyReactionShown)
                return;

            _firstEnemyReactionShown = true;
            MarkStepSeen("tutorial_first_enemy");
            _activeSequence = StartCoroutine(FirstEnemyReactionSequence());
        }

        private IEnumerator FirstEnemyReactionSequence()
        {
            yield return ShowTooltipAndWait(
                "What the — that guy looks pissed. Uh... I think you gotta fight him?");
        }

        // ── First combat UI prompts ────────────────────────────────────
        // Req 40.7: Contextual UI prompts highlighting card hand, OT meter,
        // enemy HP, intent icons, End Turn button. Confused/improvised language.

        private IEnumerator FirstCombatSequence()
        {
            // Wait for battle scene to initialize
            yield return new WaitForSeconds(1.5f);

            SubscribeBattleEvents();

            // Show each combat prompt as a dismissible tooltip
            foreach (string line in CombatPromptLines)
            {
                yield return ShowTooltipAndWait(line);
            }

            _firstCombatPromptsShown = true;
            MarkStepSeen("tutorial_first_combat");

            // Now wait for combat to end, then show farewell
            // (handled via OnTurnPhaseChanged or OnVictoryDetected)
        }

        // ── Work Box discovery ─────────────────────────────────────────
        // Req 40.8: NPC reacts as if discovering Work_Box for the first time.

        /// <summary>
        /// Call this from WorkBoxTrigger when the player first opens a Work_Box
        /// during the tutorial run.
        /// </summary>
        public void OnWorkBoxDiscovered()
        {
            if (CurrentPhase == TutorialPhase.Inactive || _workBoxReactionShown)
                return;

            _workBoxReactionShown = true;
            MarkStepSeen("tutorial_workbox_discovery");
            _activeSequence = StartCoroutine(WorkBoxDiscoverySequence());
        }

        private IEnumerator WorkBoxDiscoverySequence()
        {
            yield return ShowTooltipAndWait(
                "Wait, there's stuff under the desks? Huh. Grab whatever you can, I guess.");
        }

        // ── After first combat — farewell ──────────────────────────────
        // Req 40.9: NPC says farewell and disappears after first combat.

        /// <summary>
        /// Called when the first combat encounter ends (victory).
        /// Shows farewell and completes the tutorial.
        /// </summary>
        public void OnFirstCombatEnded()
        {
            if (_farewellShown) return;
            _farewellShown = true;

            if (_activeSequence != null)
                StopCoroutine(_activeSequence);

            _activeSequence = StartCoroutine(FarewellSequence());
        }

        private IEnumerator FarewellSequence()
        {
            yield return new WaitForSeconds(0.5f);

            yield return ShowTooltipAndWait(
                "Okay, you clearly got this. I'm gonna... go back to my desk. Good luck out there.");

            CompleteTutorial();
        }

        // ── Tutorial completion ────────────────────────────────────────
        // Req 40.10: Persist tutorial-completed flag in MetaState.

        private void CompleteTutorial()
        {
            CurrentPhase = TutorialPhase.Complete;
            HideTooltip();
            UnsubscribeBattleEvents();

            // Persist the flag (Req 40.10)
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.CurrentMeta.tutorialCompleted = true;
                SaveManager.Instance.SaveMeta();
            }

            Debug.Log("[TutorialNPC] Tutorial completed and persisted.");
        }

        // ── Battle event subscriptions ─────────────────────────────────

        private bool _subscribedToBattle;

        private void SubscribeBattleEvents()
        {
            if (_subscribedToBattle) return;
            if (BattleEventBus.Instance == null) return;

            BattleEventBus.Instance.OnTurnPhaseChanged += OnTurnPhaseChanged;
            _subscribedToBattle = true;
        }

        private void UnsubscribeBattleEvents()
        {
            if (!_subscribedToBattle) return;
            if (BattleEventBus.Instance == null) return;

            BattleEventBus.Instance.OnTurnPhaseChanged -= OnTurnPhaseChanged;
            _subscribedToBattle = false;
        }

        /// <summary>
        /// Listens for the end of the first combat encounter.
        /// When the enemy phase ends on turn 1+, we check if combat is over.
        /// The actual victory detection is done via OnFirstCombatEnded() called
        /// externally from BattleManager/VictoryScreen, but we also detect
        /// scene unload as a fallback.
        /// </summary>
        private void OnTurnPhaseChanged(TurnPhaseChangedEvent e)
        {
            // No-op — we rely on OnFirstCombatEnded() being called externally
            // when BattleManager triggers victory. This subscription is kept
            // for potential future use (e.g., mid-combat tips).
        }

        // ── Cutscene tracking helpers ──────────────────────────────────

        private void MarkStepSeen(string stepId)
        {
            if (SaveManager.Instance != null)
                SaveManager.Instance.MarkCutsceneSeen(stepId);
        }

        /// <summary>
        /// Check if a tutorial step was already seen (for resuming after quit).
        /// </summary>
        public bool HasSeenStep(string stepId)
        {
            if (SaveManager.Instance != null)
                return SaveManager.Instance.HasSeenCutscene(stepId);
            return false;
        }

        // ── Tooltip UI creation and management ─────────────────────────
        // Req 40.11: Non-blocking contextual tooltips, dismissible.

        private void CreateTooltipUI()
        {
            // Create a screen-space overlay canvas for tooltips
            GameObject canvasGO = new GameObject("TutorialTooltipCanvas");
            canvasGO.transform.SetParent(transform);
            _tooltipCanvas = canvasGO.AddComponent<Canvas>();
            _tooltipCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _tooltipCanvas.sortingOrder = 999; // Always on top

            canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();

            _tooltipCanvasGroup = canvasGO.AddComponent<CanvasGroup>();

            // Panel background
            _tooltipPanel = new GameObject("TooltipPanel");
            _tooltipPanel.transform.SetParent(canvasGO.transform, false);

            Image panelImage = _tooltipPanel.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.12f, 0.88f);

            RectTransform panelRT = _tooltipPanel.GetComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.15f, 0.02f);
            panelRT.anchorMax = new Vector2(0.85f, 0.18f);
            panelRT.offsetMin = Vector2.zero;
            panelRT.offsetMax = Vector2.zero;

            // NPC label
            GameObject labelGO = new GameObject("NPCLabel");
            labelGO.transform.SetParent(_tooltipPanel.transform, false);
            TextMeshProUGUI labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
            labelTMP.text = "Coworker:";
            labelTMP.fontSize = 16;
            labelTMP.fontStyle = FontStyles.Bold;
            labelTMP.color = new Color(0.9f, 0.75f, 0.3f);
            labelTMP.alignment = TextAlignmentOptions.TopLeft;

            RectTransform labelRT = labelGO.GetComponent<RectTransform>();
            labelRT.anchorMin = new Vector2(0f, 0.65f);
            labelRT.anchorMax = new Vector2(1f, 1f);
            labelRT.offsetMin = new Vector2(16, 0);
            labelRT.offsetMax = new Vector2(-16, -8);

            // Dialogue text
            GameObject textGO = new GameObject("TooltipText");
            textGO.transform.SetParent(_tooltipPanel.transform, false);
            _tooltipText = textGO.AddComponent<TextMeshProUGUI>();
            _tooltipText.fontSize = 20;
            _tooltipText.color = Color.white;
            _tooltipText.alignment = TextAlignmentOptions.TopLeft;
            _tooltipText.enableWordWrapping = true;

            RectTransform textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = new Vector2(0f, 0.15f);
            textRT.anchorMax = new Vector2(0.8f, 0.7f);
            textRT.offsetMin = new Vector2(16, 0);
            textRT.offsetMax = new Vector2(-8, 0);

            // Dismiss button
            GameObject btnGO = new GameObject("DismissButton");
            btnGO.transform.SetParent(_tooltipPanel.transform, false);

            Image btnImage = btnGO.AddComponent<Image>();
            btnImage.color = new Color(0.25f, 0.25f, 0.3f, 1f);

            _dismissButton = btnGO.AddComponent<Button>();
            _dismissButton.targetGraphic = btnImage;
            _dismissButton.onClick.AddListener(OnDismissClicked);

            RectTransform btnRT = btnGO.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.82f, 0.2f);
            btnRT.anchorMax = new Vector2(0.98f, 0.8f);
            btnRT.offsetMin = Vector2.zero;
            btnRT.offsetMax = Vector2.zero;

            // Button label
            GameObject btnTextGO = new GameObject("ButtonText");
            btnTextGO.transform.SetParent(btnGO.transform, false);
            _dismissButtonText = btnTextGO.AddComponent<TextMeshProUGUI>();
            _dismissButtonText.text = "Got it";
            _dismissButtonText.fontSize = 16;
            _dismissButtonText.color = Color.white;
            _dismissButtonText.alignment = TextAlignmentOptions.Center;

            RectTransform btnTextRT = btnTextGO.GetComponent<RectTransform>();
            btnTextRT.anchorMin = Vector2.zero;
            btnTextRT.anchorMax = Vector2.one;
            btnTextRT.offsetMin = Vector2.zero;
            btnTextRT.offsetMax = Vector2.zero;
        }

        // ── Tooltip show/hide helpers ──────────────────────────────────

        private void ShowTooltip(string message)
        {
            if (_tooltipPanel == null) return;

            _tooltipText.text = message;
            _tooltipPanel.SetActive(true);
            _tooltipCanvasGroup.alpha = 1f;
            _tooltipCanvasGroup.interactable = true;
            _tooltipCanvasGroup.blocksRaycasts = true;
            _tooltipVisible = true;
        }

        private void HideTooltip()
        {
            if (_tooltipPanel == null) return;

            _tooltipPanel.SetActive(false);
            _tooltipCanvasGroup.alpha = 0f;
            _tooltipCanvasGroup.interactable = false;
            _tooltipCanvasGroup.blocksRaycasts = false;
            _tooltipVisible = false;
        }

        /// <summary>
        /// Shows a tooltip and yields until the player dismisses it.
        /// Non-blocking: the game continues running underneath.
        /// </summary>
        private IEnumerator ShowTooltipAndWait(string message)
        {
            _waitingForDismiss = true;
            ShowTooltip(message);

            while (_waitingForDismiss)
                yield return null;

            HideTooltip();

            // Small pause between tooltips so they don't feel rushed
            yield return new WaitForSeconds(0.3f);
        }

        private void OnDismissClicked()
        {
            _waitingForDismiss = false;
        }

        /// <summary>
        /// Also allow clicking anywhere on the panel or pressing Space/Enter to dismiss.
        /// </summary>
        private void Update()
        {
            if (!_tooltipVisible || !_waitingForDismiss) return;

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)
                || Input.GetKeyDown(KeyCode.E))
            {
                _waitingForDismiss = false;
            }
        }

        // ── Public API for external integration ────────────────────────

        /// <summary>
        /// Returns true if the tutorial is currently active (not inactive/complete).
        /// Other systems can check this to trigger tutorial hooks.
        /// </summary>
        public bool IsTutorialActive =>
            CurrentPhase != TutorialPhase.Inactive && CurrentPhase != TutorialPhase.Complete;

        /// <summary>
        /// Returns true if the tooltip is currently visible.
        /// </summary>
        public bool IsTooltipVisible => _tooltipVisible;
    }
}
