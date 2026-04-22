using System;
using System.Collections;
using UnityEngine;
using TMPro;

namespace CardBattle
{
    /// <summary>
    /// Manages the unique floor 5 non-combat encounter with a suicidal worker NPC.
    /// This is the emotional core of the game — all text, timing, and interaction
    /// must be handled with the highest care and respect.
    ///
    /// The encounter uses the player's normal run deck, hand, and Overtime Meter
    /// but with unique resolution mechanics:
    ///   - Shield: player parries the worker's self-harm with a Defense card
    ///   - Empathy: player damages themselves (Jean-Guy) before the worker acts
    ///   - Failure: worker's HP reaches 0 from self-harm
    ///
    /// Requirements: 32.1–32.11
    /// </summary>
    public class SuicidalWorkerEncounter : MonoBehaviour
    {
        // ── Resolution Enum ───────────────────────────────────────────────────

        public enum EncounterResolution { None, Shield, Empathy, Failure }

        // ── Serialized Fields ─────────────────────────────────────────────────

        [Header("Worker")]
        [SerializeField] private int workerMaxHP = 12;
        [SerializeField] private int workerSelfDamage = 4;
        [SerializeField] private float workerParryWindowDuration = 2f;

        [Header("Rewards")]
        [SerializeField] private ToolData shieldRewardTool;
        [SerializeField] private ToolData empathyRewardTool;

        [Header("UI")]
        [SerializeField] private CanvasGroup encounterPanel;
        [SerializeField] private TextMeshProUGUI workerDialogueText;
        [SerializeField] private TextMeshProUGUI workerHPText;
        [SerializeField] private CanvasGroup resolutionPanel;
        [SerializeField] private TextMeshProUGUI resolutionText;

        // ── State ─────────────────────────────────────────────────────────────

        private int _workerHP;
        private bool _encounterActive;
        private bool _playerSelfDamagedThisTurn;
        private bool _parryWindowOpen;
        private float _parryWindowTimer;
        private EncounterResolution _resolution;
        private int _turnNumber;

        // ── Constants ─────────────────────────────────────────────────────────

        private const string CutsceneId = "worker_encounter";

        // ── Narrative Text ────────────────────────────────────────────────────
        // Minimal, impactful, respectful. No humor, no gamification.

        private static readonly string[] WorkerDialogueLines = new string[]
        {
            "...",
            "He doesn't look at you.",
            "He raises his hand again.",
        };

        private const string ShieldResolutionText =
            "You block his hand. He looks at you, confused... then relieved.";

        private const string EmpathyResolutionText =
            "He watches you hurt yourself. He stops. He walks away.";

        private const string FailureResolutionText =
            "You were too late.";

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Whether the encounter is currently active.</summary>
        public bool IsEncounterActive => _encounterActive;

        /// <summary>The worker's current HP.</summary>
        public int WorkerHP => _workerHP;

        /// <summary>The worker's maximum HP.</summary>
        public int WorkerMaxHP => workerMaxHP;

        /// <summary>How the encounter resolved (None if still active or not started).</summary>
        public EncounterResolution Resolution => _resolution;

        /// <summary>Whether the parry window is currently open for the worker's self-harm.</summary>
        public bool IsParryWindowOpen => _parryWindowOpen;

        /// <summary>Remaining time on the parry window.</summary>
        public float ParryWindowTimeRemaining => _parryWindowOpen ? Mathf.Max(0f, _parryWindowTimer) : 0f;

        /// <summary>Current turn number within the encounter.</summary>
        public int TurnNumber => _turnNumber;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        /// <summary>
        /// Check whether this encounter should trigger. Returns false if already
        /// seen this run or not on the correct floor.
        /// </summary>
        public static bool ShouldTrigger(int currentFloor, GameConfig config)
        {
            int targetFloor = config != null ? config.workerEncounterFloor : 5;
            if (currentFloor != targetFloor) return false;

            if (SaveManager.Instance != null && SaveManager.Instance.HasSeenCutscene(CutsceneId))
                return false;

            return true;
        }

        /// <summary>
        /// Initialize and start the encounter. Call this when the player
        /// triggers the worker NPC on floor 5.
        /// </summary>
        public void StartEncounter(GameConfig config = null)
        {
            // Pull values from GameConfig if available
            if (config != null)
            {
                workerMaxHP = config.workerHP;
                workerSelfDamage = config.workerSelfDamage;
            }

            _workerHP = workerMaxHP;
            _encounterActive = true;
            _resolution = EncounterResolution.None;
            _playerSelfDamagedThisTurn = false;
            _parryWindowOpen = false;
            _parryWindowTimer = 0f;
            _turnNumber = 0;

            // Mark cutscene as seen so it only happens once per run (Req 32.1)
            if (SaveManager.Instance != null)
                SaveManager.Instance.MarkCutsceneSeen(CutsceneId);

            // Subscribe to battle events
            SubscribeToEvents();

            // Show encounter UI
            SetEncounterPanelVisible(true);
            SetResolutionPanelVisible(false);
            UpdateWorkerHPDisplay();
            SetWorkerDialogue(WorkerDialogueLines[0]);

            Debug.Log($"[WorkerEncounter] Started. Worker HP: {_workerHP}/{workerMaxHP}");
        }

        // ── Event Subscriptions ───────────────────────────────────────────────

        private void SubscribeToEvents()
        {
            if (BattleEventBus.Instance == null) return;

            BattleEventBus.Instance.OnCardPlayed += OnCardPlayed;
            BattleEventBus.Instance.OnDamageDealt += OnDamageDealt;
            BattleEventBus.Instance.OnTurnPhaseChanged += OnTurnPhaseChanged;
        }

        private void UnsubscribeFromEvents()
        {
            if (BattleEventBus.Instance == null) return;

            BattleEventBus.Instance.OnCardPlayed -= OnCardPlayed;
            BattleEventBus.Instance.OnDamageDealt -= OnDamageDealt;
            BattleEventBus.Instance.OnTurnPhaseChanged -= OnTurnPhaseChanged;
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        // ── Event Handlers ────────────────────────────────────────────────────

        /// <summary>
        /// Listens for card plays. Detects Attack cards targeting Jean-Guy
        /// (the player) for Empathy resolution (Req 32.7).
        /// </summary>
        private void OnCardPlayed(CardPlayedEvent e)
        {
            if (!_encounterActive || _resolution != EncounterResolution.None) return;

            // Empathy check: player plays an Attack card targeting themselves
            if (e.Card != null && e.Card.cardType == CardType.Attack
                && e.Source != null && e.Target != null
                && e.Source == e.Target)
            {
                // Player targeted themselves — flag for empathy check
                _playerSelfDamagedThisTurn = true;
                Debug.Log("[WorkerEncounter] Player played Attack card on themselves.");
            }
        }

        /// <summary>
        /// Listens for damage events. Detects when Jean-Guy takes self-damage
        /// for Empathy resolution (Req 32.7).
        /// </summary>
        private void OnDamageDealt(DamageEvent e)
        {
            if (!_encounterActive || _resolution != EncounterResolution.None) return;

            // Empathy check: player dealt damage to themselves
            if (e.Source != null && e.Target != null
                && e.Source == e.Target
                && e.Source.CompareTag("Player")
                && e.Amount > 0)
            {
                _playerSelfDamagedThisTurn = true;
                Debug.Log("[WorkerEncounter] Player self-damaged confirmed via DamageEvent.");
            }
        }

        /// <summary>
        /// Listens for turn phase changes to drive the encounter flow.
        /// </summary>
        private void OnTurnPhaseChanged(TurnPhaseChangedEvent e)
        {
            if (!_encounterActive || _resolution != EncounterResolution.None) return;

            switch (e.NewPhase)
            {
                case TurnPhase.Draw:
                    // New turn starting — reset per-turn tracking
                    _turnNumber = e.TurnNumber;
                    _playerSelfDamagedThisTurn = false;
                    UpdateWorkerDialogue();
                    break;

                case TurnPhase.Enemy:
                    // Worker's phase — check empathy first, then self-harm
                    StartCoroutine(WorkerPhaseCoroutine());
                    break;
            }
        }

        // ── Worker Phase (replaces normal Enemy Phase) ────────────────────────

        /// <summary>
        /// Executes the worker's turn. Checks for Empathy resolution first,
        /// then performs self-harm with a parry window for Shield resolution.
        /// </summary>
        private IEnumerator WorkerPhaseCoroutine()
        {
            if (!_encounterActive || _resolution != EncounterResolution.None)
                yield break;

            // Empathy resolution check (Req 32.7):
            // If the player damaged themselves this turn, the worker stops.
            if (_playerSelfDamagedThisTurn)
            {
                yield return StartCoroutine(ResolveEmpathy());
                yield break;
            }

            // Worker self-harm with parry window (Req 32.5, 32.6)
            yield return StartCoroutine(WorkerSelfHarmCoroutine());
        }

        // ── Self-Harm with Parry Window ───────────────────────────────────────

        /// <summary>
        /// The worker deals self-damage. A parry window opens during this action
        /// so the player can drag a Defense card onto the worker to intervene
        /// (Shield resolution, Req 32.6).
        /// </summary>
        private IEnumerator WorkerSelfHarmCoroutine()
        {
            // Open parry window
            _parryWindowOpen = true;
            _parryWindowTimer = workerParryWindowDuration;

            Debug.Log($"[WorkerEncounter] Parry window OPEN — {workerParryWindowDuration:F1}s");

            // Count down the parry window
            while (_parryWindowTimer > 0f)
            {
                // Check if Shield resolution happened during the window
                if (_resolution == EncounterResolution.Shield)
                {
                    _parryWindowOpen = false;
                    yield break;
                }

                _parryWindowTimer -= Time.deltaTime;
                yield return null;
            }

            // Parry window expired — worker takes self-damage
            _parryWindowOpen = false;

            if (_resolution != EncounterResolution.None)
                yield break;

            // Apply self-damage (Req 32.4, 32.5)
            _workerHP -= workerSelfDamage;
            if (_workerHP < 0) _workerHP = 0;

            UpdateWorkerHPDisplay();
            Debug.Log($"[WorkerEncounter] Worker self-harmed for {workerSelfDamage}. HP: {_workerHP}/{workerMaxHP}");

            // Check for failure (Req 32.8)
            if (_workerHP <= 0)
            {
                yield return StartCoroutine(ResolveFailure());
            }
        }

        // ── Shield Resolution (Parry) ────────────────────────────────────────

        /// <summary>
        /// Called when the player successfully parries the worker's self-harm
        /// by playing a Defense card targeting the worker during the parry window.
        /// This is the Shield resolution path (Req 32.6, 32.9).
        /// </summary>
        public bool TryShieldParry(CardData defenseCard)
        {
            if (!_encounterActive || !_parryWindowOpen || _resolution != EncounterResolution.None)
                return false;

            if (defenseCard == null || defenseCard.cardType != CardType.Defense)
                return false;

            // Shield resolution — parry succeeds, cancel self-damage
            _resolution = EncounterResolution.Shield;
            _parryWindowOpen = false;

            Debug.Log("[WorkerEncounter] Shield resolution — Defense card parried worker's self-harm.");

            StartCoroutine(ResolveShield());
            return true;
        }

        // ── Resolution Sequences ──────────────────────────────────────────────

        /// <summary>
        /// Shield resolution: player blocked the worker's self-harm (Req 32.6, 32.9).
        /// </summary>
        private IEnumerator ResolveShield()
        {
            _encounterActive = false;
            _resolution = EncounterResolution.Shield;

            SetWorkerDialogue(ShieldResolutionText);
            SetResolutionPanelVisible(true);

            Debug.Log("[WorkerEncounter] Resolved: Shield");

            // Brief pause for the player to read
            yield return new WaitForSeconds(3f);

            // Award Shield Tool reward (Req 32.9)
            AwardToolReward(shieldRewardTool);

            EndEncounter();
        }

        /// <summary>
        /// Empathy resolution: player hurt themselves, worker walks away (Req 32.7, 32.10).
        /// </summary>
        private IEnumerator ResolveEmpathy()
        {
            _encounterActive = false;
            _resolution = EncounterResolution.Empathy;

            SetWorkerDialogue(EmpathyResolutionText);
            SetResolutionPanelVisible(true);

            Debug.Log("[WorkerEncounter] Resolved: Empathy");

            // Brief pause for the player to read
            yield return new WaitForSeconds(3f);

            // Award Empathy Tool reward (Req 32.10)
            AwardToolReward(empathyRewardTool);

            EndEncounter();
        }

        /// <summary>
        /// Failure resolution: worker's HP reached 0 (Req 32.8).
        /// </summary>
        private IEnumerator ResolveFailure()
        {
            _encounterActive = false;
            _resolution = EncounterResolution.Failure;

            SetWorkerDialogue(FailureResolutionText);
            SetResolutionPanelVisible(true);

            Debug.Log("[WorkerEncounter] Resolved: Failure");

            // Longer pause — let the weight of the moment land
            yield return new WaitForSeconds(4f);

            // No reward on failure (Req 32.8)
            EndEncounter();
        }

        // ── Reward Delivery ───────────────────────────────────────────────────

        /// <summary>
        /// Adds the reward Tool to the player's RunState (Req 32.9, 32.10).
        /// </summary>
        private void AwardToolReward(ToolData tool)
        {
            if (tool == null)
            {
                Debug.LogWarning("[WorkerEncounter] No reward tool assigned.");
                return;
            }

            if (SaveManager.Instance == null || SaveManager.Instance.CurrentRun == null)
            {
                Debug.LogWarning("[WorkerEncounter] SaveManager or RunState not available for reward.");
                return;
            }

            RunState run = SaveManager.Instance.CurrentRun;
            if (run.toolIds == null)
                run.toolIds = new System.Collections.Generic.List<string>();

            run.toolIds.Add(tool.name);
            SaveManager.Instance.SaveRun();

            Debug.Log($"[WorkerEncounter] Awarded tool: {tool.toolName}");
        }

        // ── Encounter Cleanup ─────────────────────────────────────────────────

        private void EndEncounter()
        {
            _encounterActive = false;
            UnsubscribeFromEvents();

            Debug.Log($"[WorkerEncounter] Encounter ended. Resolution: {_resolution}");
        }

        // ── Worker Dialogue ───────────────────────────────────────────────────

        private void UpdateWorkerDialogue()
        {
            if (_turnNumber <= 0 || _resolution != EncounterResolution.None) return;

            // Cycle through dialogue lines based on turn number
            int index = Mathf.Clamp(_turnNumber - 1, 0, WorkerDialogueLines.Length - 1);
            SetWorkerDialogue(WorkerDialogueLines[index]);
        }

        private void SetWorkerDialogue(string text)
        {
            if (workerDialogueText != null)
                workerDialogueText.text = text;
        }

        // ── UI Helpers ────────────────────────────────────────────────────────

        private void UpdateWorkerHPDisplay()
        {
            if (workerHPText != null)
                workerHPText.text = $"{_workerHP} / {workerMaxHP}";
        }

        private void SetEncounterPanelVisible(bool visible)
        {
            if (encounterPanel == null) return;
            encounterPanel.alpha = visible ? 1f : 0f;
            encounterPanel.interactable = visible;
            encounterPanel.blocksRaycasts = visible;
        }

        private void SetResolutionPanelVisible(bool visible)
        {
            if (resolutionPanel == null) return;
            resolutionPanel.alpha = visible ? 1f : 0f;
            resolutionPanel.interactable = visible;
            resolutionPanel.blocksRaycasts = visible;
        }

        // ── Direct State Manipulation (for testing) ───────────────────────────

        /// <summary>
        /// Directly apply self-damage to the worker. Used by tests and
        /// external systems that need to simulate the worker's self-harm
        /// without going through the coroutine flow.
        /// </summary>
        public void ApplyWorkerSelfDamage()
        {
            if (!_encounterActive || _resolution != EncounterResolution.None) return;

            _workerHP -= workerSelfDamage;
            if (_workerHP < 0) _workerHP = 0;

            UpdateWorkerHPDisplay();

            if (_workerHP <= 0)
            {
                _encounterActive = false;
                _resolution = EncounterResolution.Failure;
                SetWorkerDialogue(FailureResolutionText);
                SetResolutionPanelVisible(true);
                EndEncounter();
            }
        }

        /// <summary>
        /// Notify the encounter that the player self-damaged this turn.
        /// Used by external systems to trigger empathy detection.
        /// </summary>
        public void NotifyPlayerSelfDamage()
        {
            if (!_encounterActive || _resolution != EncounterResolution.None) return;
            _playerSelfDamagedThisTurn = true;
        }

        /// <summary>
        /// Check empathy and resolve if the player self-damaged this turn.
        /// Returns true if empathy resolution was triggered.
        /// </summary>
        public bool TryResolveEmpathy()
        {
            if (!_encounterActive || _resolution != EncounterResolution.None) return false;
            if (!_playerSelfDamagedThisTurn) return false;

            _resolution = EncounterResolution.Empathy;
            _encounterActive = false;

            SetWorkerDialogue(EmpathyResolutionText);
            SetResolutionPanelVisible(true);
            AwardToolReward(empathyRewardTool);
            EndEncounter();

            Debug.Log("[WorkerEncounter] Empathy resolution triggered directly.");
            return true;
        }

        /// <summary>
        /// Open the parry window manually (for testing or external control).
        /// </summary>
        public void OpenParryWindow()
        {
            if (!_encounterActive || _resolution != EncounterResolution.None) return;
            _parryWindowOpen = true;
            _parryWindowTimer = workerParryWindowDuration;
        }

        /// <summary>
        /// Reset per-turn state. Called at the start of each new turn.
        /// </summary>
        public void ResetTurnState()
        {
            _playerSelfDamagedThisTurn = false;
        }
    }
}
