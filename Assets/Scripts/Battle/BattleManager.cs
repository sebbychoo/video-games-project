using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle
{
    public class BattleManager : MonoBehaviour
    {
        public static BattleManager Instance { get; private set; }

        // ── Public HP Accessors ───────────────────────────────────────────────
        public int PlayerHP => playerHealth != null ? playerHealth.currentHealth : 0;
        public int PlayerMaxHP => playerHealth != null ? playerHealth.maxHealth : 1;

        // ── Subsystem References ──────────────────────────────────────────────
        [Header("Subsystems")]
        [SerializeField] TurnPhaseController   turnPhaseController;
        [SerializeField] OvertimeMeter         overtimeMeter;
        [SerializeField] OverflowBuffer        overflowBuffer;
        [SerializeField] BlockSystem           blockSystem;
        [SerializeField] ParrySystem           parrySystem;
        [SerializeField] StatusEffectSystem    statusEffectSystem;
        [SerializeField] CardEffectResolver    cardEffectResolver;
        [SerializeField] DeckManager           deckManager;
        [SerializeField] HandManager           handManager;

        [Header("Config")]
        [SerializeField] GameConfig            gameConfig;

        [Header("Fallback (for testing without RunState)")]
        [SerializeField] EncounterData         fallbackEncounter;
        [SerializeField] List<CardData>        fallbackDeck;

        [Header("Animation")]
        [SerializeField] CardAnimator          cardAnimator;
        [SerializeField] BattleAnimations      battleAnimations;

        [Header("UI")]
        [SerializeField] PlayerHPStack         playerHPStack;
        [SerializeField] EnemyHPBar            playerHPBar;
        [SerializeField] EnemyHPBar            screenEnemyHPBar;
        [SerializeField] EnemyIntentDisplay    screenEnemyIntent;
        [SerializeField] StatusEffectIconStack enemyStatusEffectIcons;
        [SerializeField] OvertimeMeterUI       overtimeMeterUI;
        [SerializeField] DeckCounterUI         deckCounterUI;
        [SerializeField] BlockDisplay          blockDisplay;
        [SerializeField] TurnCounterUI         turnCounterUI;
        [SerializeField] VictoryScreen         victoryScreen;

        [Header("Spawning")]
        [SerializeField] GameObject            enemyPrefab;
        [SerializeField] Transform[]           enemySpawnPoints;

        [Header("Player")]
        [SerializeField] GameObject            playerObject;
        [SerializeField] Health                playerHealth;

        // ── State ─────────────────────────────────────────────────────────────
        private List<EnemyCombatant>    _enemies = new List<EnemyCombatant>();
        private List<EnemyHPBar>        _enemyHPBars = new List<EnemyHPBar>();
        private List<EnemyIntentDisplay> _enemyIntents = new List<EnemyIntentDisplay>();
        private int                  _handSize;
        private int                  _lastEndTurnFrame = -1;
        private bool                 _encounterActive;
        private EncounterData        _currentEncounter;

        // Stores the target while the exit animation plays
        private GameObject _pendingTarget;

        // Stores the enemy action before ExecuteAction advances the pattern index
        private EnemyAction _lastExecutedEnemyAction;

        /// <summary>Current turn phase — used by CardInteractionHandler and CardTargetingManager.</summary>
        public TurnPhase CurrentTurn => turnPhaseController != null
            ? turnPhaseController.CurrentPhase
            : TurnPhase.Play;

        public bool IsBattleOver => !_encounterActive;

        public int DeckCount    => deckManager != null ? deckManager.DeckCount    : 0;
        public int DiscardCount => deckManager != null ? deckManager.DiscardCount : 0;

        /// <summary>Read-only list of living enemies in the current encounter.</summary>
        public IReadOnlyList<EnemyCombatant> Enemies => _enemies;

        /// <summary>The HandManager subsystem for this battle.</summary>
        public HandManager HandManager => handManager;

        /// <summary>The ParrySystem subsystem for this battle.</summary>
        public ParrySystem ParrySystem => parrySystem;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                if (BattleEventBus.Instance != null)
                {
                    BattleEventBus.Instance.OnRageBurst -= OnRageBurstEvent;
                    BattleEventBus.Instance.OnOverflow -= OnOverflowEvent;
                }
                Instance = null;
            }
        }

        private void OnRageBurstEvent(RageBurstEvent e)
        {
            // Rage burst flash disabled for clean UI
        }

        private void OnOverflowEvent(OverflowEvent e)
        {
            // Overflow glow disabled for clean UI
        }

        private void Start()
        {
            // Ensure CardTargetingManager exists
            if (CardTargetingManager.Instance == null)
            {
                CardTargetingManager existing = FindObjectOfType<CardTargetingManager>();
                if (existing == null)
                    gameObject.AddComponent<CardTargetingManager>();
            }

            // Subscribe to events for visual effects
            if (BattleEventBus.Instance != null)
            {
                BattleEventBus.Instance.OnRageBurst += OnRageBurstEvent;
                BattleEventBus.Instance.OnOverflow += OnOverflowEvent;
            }

            // If an EncounterData is available via a static handoff, start it.
            // Otherwise try fallback encounter for scene testing.
            if (_pendingEncounter != null)
            {
                StartEncounter(_pendingEncounter);
                _pendingEncounter = null;
            }
            else if (fallbackEncounter != null)
            {
                StartEncounter(fallbackEncounter);
            }
        }

        // ── Static encounter handoff ──────────────────────────────────────────
        private static EncounterData _pendingEncounter;

        /// <summary>Set encounter data before loading the battle scene.</summary>
        public static void SetPendingEncounter(EncounterData data)
        {
            _pendingEncounter = data;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Initialize all subsystems and start a new encounter.
        /// Called from Start() via pending encounter or externally.
        /// </summary>
        public void StartEncounter(EncounterData encounter)
        {
            if (encounter == null || encounter.enemies == null || encounter.enemies.Count == 0)
            {
                Debug.LogError("BattleManager: EncounterData is null or has no enemies.");
                return;
            }

            _encounterActive = true;
            _currentEncounter = encounter;
            _handSize = gameConfig != null ? gameConfig.baseHandSize : 5;
            int otMax = gameConfig != null ? gameConfig.overtimeMaxCapacity : 10;
            int otRegen = gameConfig != null ? gameConfig.overtimeRegenPerTurn : 2;

            // Initialize subsystems
            if (overflowBuffer == null) { Debug.LogError("BattleManager: OverflowBuffer not assigned!"); return; }
            if (overtimeMeter == null) { Debug.LogError("BattleManager: OvertimeMeter not assigned!"); return; }
            if (blockSystem == null) { Debug.LogError("BattleManager: BlockSystem not assigned!"); return; }
            if (parrySystem == null) { Debug.LogError("BattleManager: ParrySystem not assigned!"); return; }
            if (statusEffectSystem == null) { Debug.LogError("BattleManager: StatusEffectSystem not assigned!"); return; }
            if (cardEffectResolver == null) { Debug.LogError("BattleManager: CardEffectResolver not assigned!"); return; }
            if (deckManager == null) { Debug.LogError("BattleManager: DeckManager not assigned!"); return; }
            if (handManager == null) { Debug.LogError("BattleManager: HandManager not assigned!"); return; }
            if (turnPhaseController == null) { Debug.LogError("BattleManager: TurnPhaseController not assigned!"); return; }

            overflowBuffer.Initialize();
            overtimeMeter.Initialize(otMax, otRegen, overflowBuffer);
            blockSystem.Initialize();
            statusEffectSystem.Initialize();

            // Initialize ParrySystem with config and current floor
            int currentFloor = 1;
            RunState runState = FindRunState();
            if (runState != null) currentFloor = runState.currentFloor;
            parrySystem.Initialize(gameConfig, currentFloor);

            // Apply Tool modifiers from RunState
            ApplyToolModifiers();

            // Initialize player HP
            InitializePlayerHP();

            // Spawn enemies
            SpawnEnemies(encounter);

            // Initialize deck from RunState or fallback
            InitializeDeck();

            // Initialize UI components
            if (overtimeMeterUI != null)
                overtimeMeterUI.Initialize(overtimeMeter, overflowBuffer);
            if (deckCounterUI != null)
                deckCounterUI.Initialize(deckManager);
            if (blockDisplay != null)
                blockDisplay.Initialize(playerObject);
            if (turnCounterUI != null)
                turnCounterUI.Initialize();

            // Initialize turn phase controller
            if (playerObject == null)
                playerObject = gameObject;
            turnPhaseController.Initialize(statusEffectSystem, playerObject);

            // Draw opening hand and begin
            DrawHand();
            turnPhaseController.AdvancePhase(); // Draw → Play (or Draw → Discard if stunned)
        }

        /// <summary>Called by CardTargetingManager when the player clicks a target.</summary>
        public void TryPlayCard(CardInstance card, GameObject target)
        {
            if (!_encounterActive) return;
            if (CurrentTurn != TurnPhase.Play) return;

            // Validate OT cost
            if (!overtimeMeter.Spend(card.Data.overtimeCost))
            {
                if (cardAnimator != null)
                    cardAnimator.PlayRejection(card);
                return;
            }

            // Refresh OT UI immediately after spend
            if (overtimeMeterUI != null)
                overtimeMeterUI.Refresh();

            _pendingTarget = target;

            if (cardAnimator != null)
            {
                cardAnimator.PlayExit(card, () => OnCardExitComplete(card));
            }
            else
            {
                OnCardExitComplete(card);
            }
        }

        /// <summary>Called by the End Turn UI button.</summary>
        public void EndTurn()
        {
            if (!_encounterActive) return;
            if (CurrentTurn != TurnPhase.Play) return;

            // Guard against double-fire from UI in same frame
            if (Time.frameCount == _lastEndTurnFrame) return;
            _lastEndTurnFrame = Time.frameCount;

            // Discard non-Defense cards; keep Defense cards in hand for parry during Enemy_Phase
            List<CardInstance> toDiscard = new List<CardInstance>();
            foreach (CardInstance card in handManager.Cards)
            {
                if (card.Data.cardType != CardType.Defense)
                    toDiscard.Add(card);
            }
            foreach (CardInstance card in toDiscard)
            {
                deckManager.Discard(card.Data);
                handManager.RemoveCard(card);
            }

            // Advance to Discard phase, then Enemy phase
            turnPhaseController.AdvancePhase(); // Play → Discard
            turnPhaseController.AdvancePhase(); // Discard → Enemy

            StartCoroutine(EnemyPhaseRoutine());
        }

        /// <summary>
        /// Attempt to parry the current enemy attack with a Defense card.
        /// Called by the UI when the player drags a Defense card during a Parry_Window.
        /// Returns true if the parry succeeded.
        /// </summary>
        public bool TryParryWithCard(CardInstance card)
        {
            if (!_encounterActive) return false;
            if (parrySystem == null || !parrySystem.IsParryWindowActive) return false;

            bool success = parrySystem.TryParry(card);
            if (success)
            {
                // Move card to discard at no OT cost
                deckManager.Discard(card.Data);
                handManager.RemoveCard(card);
            }
            return success;
        }

        // ── Card resolution callback ──────────────────────────────────────────

        private void OnCardExitComplete(CardInstance card)
        {
            CardData data = card.Data;
            GameObject target = _pendingTarget;
            _pendingTarget = null;

            // Get living enemies list for AoE resolution
            List<EnemyCombatant> livingEnemies = GetLivingEnemies();

            // Delegate resolution to CardEffectResolver
            cardEffectResolver.Resolve(card, playerObject ?? gameObject, target, livingEnemies);

            // Play hit animations on targets for Attack cards
            if (data.cardType == CardType.Attack && battleAnimations != null)
            {
                if (data.targetMode == TargetMode.AllEnemies)
                {
                    // For AoE, just shake all enemies (no dash)
                    foreach (var enemy in livingEnemies)
                    {
                        if (enemy != null && enemy.IsAlive)
                        {
                            battleAnimations.PlayHitShake(enemy.transform);
                            UpdateEnemyHPBar(enemy);
                        }
                    }
                }
                else if (target != null)
                {
                    // Single target: dash player toward enemy, then shake enemy
                    Transform playerT = playerObject != null ? playerObject.transform : transform;
                    battleAnimations.PlayAttackDash(playerT, target.transform, () =>
                    {
                        if (target != null)
                            battleAnimations.PlayHitShake(target.transform);
                    });
                    EnemyCombatant ec = target.GetComponent<EnemyCombatant>();
                    if (ec != null) UpdateEnemyHPBar(ec);
                }
            }

            // Update enemy HP bars for non-attack cards that might deal damage (burn via effects, etc.)
            if (data.cardType != CardType.Attack)
            {
                foreach (var enemy in GetLivingEnemies())
                    UpdateEnemyHPBar(enemy);
            }

            // Refresh player HP UI after any card resolution (covers self-targeting)
            if (playerHPStack != null && playerHealth != null)
                playerHPStack.UpdateHP(playerHealth.currentHealth, playerHealth.maxHealth);
            if (playerHPBar != null && playerHealth != null)
                playerHPBar.UpdateHP(playerHealth.currentHealth, playerHealth.maxHealth);

            // Check for enemy deaths after card resolution
            CheckEnemyDeaths();

            // Check victory
            if (GetLivingEnemies().Count == 0)
            {
                OnVictory();
            }
        }

        // ── Enemy Phase ────────────────────────────────────────────────────────

        private IEnumerator EnemyPhaseRoutine()
        {
            yield return new WaitForSeconds(0.5f);

            List<EnemyCombatant> living = GetLivingEnemies();

            for (int i = 0; i < living.Count; i++)
            {
                EnemyCombatant enemy = living[i];
                if (!enemy.IsAlive) continue;

                // Reset enemy Block at start of their turn (even if stunned)
                blockSystem.Reset(enemy.gameObject);

                // Process Burn at start of enemy's turn
                int burnDamage = statusEffectSystem.ProcessBurn(enemy.gameObject);
                if (burnDamage > 0)
                {
                    // Apply Bleed bonus to burn damage
                    int bleedBonus = statusEffectSystem.GetBleedBonus(enemy.gameObject);
                    int totalBurn = burnDamage + bleedBonus;

                    int remainingBurn = blockSystem.AbsorbDamage(totalBurn, enemy.gameObject);
                    if (remainingBurn > 0)
                    {
                        Health enemyHP = enemy.GetComponent<Health>();
                        if (enemyHP != null)
                            enemyHP.TakeDamage(remainingBurn);
                    }

                    if (BattleEventBus.Instance != null)
                    {
                        BattleEventBus.Instance.Raise(new DamageEvent
                        {
                            Source = enemy.gameObject,
                            Target = enemy.gameObject,
                            Amount = totalBurn
                        });
                    }

                    // Update enemy HP bar
                    UpdateEnemyHPBar(enemy);

                    if (!enemy.IsAlive)
                    {
                        CheckEnemyDeaths();
                        if (GetLivingEnemies().Count == 0)
                        {
                            OnVictory();
                            yield break;
                        }
                        continue;
                    }
                }

                // Check stun — skip action if stunned
                if (statusEffectSystem.IsStunned(enemy.gameObject))
                {
                    yield return new WaitForSeconds(0.3f);
                    continue;
                }

                // Execute enemy action — capture intent before it advances the pattern index
                _lastExecutedEnemyAction = enemy.CurrentIntent;
                EnemyActionResult result = enemy.ExecuteAction();
                if (result.WasSkipped)
                {
                    yield return new WaitForSeconds(0.3f);
                    continue;
                }

                yield return StartCoroutine(ProcessEnemyAction(enemy, result));

                if (!_encounterActive) yield break;

                yield return new WaitForSeconds(0.5f);
            }

            // Tick status effects for all enemies
            foreach (EnemyCombatant enemy in GetLivingEnemies())
                statusEffectSystem.Tick(enemy.gameObject);

            // Tick player status effects
            statusEffectSystem.Tick(playerObject ?? gameObject);

            // Finish enemy phase → advance to Draw
            FinishEnemyPhase();
        }

        private IEnumerator ProcessEnemyAction(EnemyCombatant enemy, EnemyActionResult result)
        {
            GameObject playerGO = playerObject ?? gameObject;

            switch (result.ActionType)
            {
                case EnemyActionType.DealDamage:
                    // Save enemy start position for dash-back
                    Vector3 enemyStartPos = enemy.transform.localPosition;

                    // Calculate parry window duration before starting the dash
                    float windowDuration = parrySystem.CalculateWindowDuration(_lastExecutedEnemyAction, enemy);
                    bool isParryable = _lastExecutedEnemyAction.intentColor != IntentColor.Unparryable
                        && _lastExecutedEnemyAction.actionType == EnemyActionType.DealDamage;

                    bool parried = false;
                    bool dashComplete = false;

                    if (isParryable && battleAnimations != null && playerHealth != null)
                    {
                        // Two-phase dash: fast start, slow finish over parry window duration
                        bool slowPhaseStarted = false;
                        battleAnimations.PlayDashWithSlowdown(
                            enemy.transform,
                            playerHealth.transform,
                            windowDuration,
                            onSlowPhaseStart: () => slowPhaseStarted = true,
                            onComplete: () => dashComplete = true);

                        // Wait for slow phase to begin, then open parry window
                        while (!slowPhaseStarted)
                            yield return null;

                        parrySystem.StartParryWindow(_lastExecutedEnemyAction, enemy);

                        // Tick parry window alongside the slow dash
                        while (!dashComplete && parrySystem.IsParryWindowActive)
                        {
                            parrySystem.TickParryWindow(Time.deltaTime);
                            if (parrySystem.ParrySucceeded)
                            {
                                parried = true;
                                break;
                            }
                            yield return null;
                        }
                        if (parrySystem.ParrySucceeded)
                            parried = true;
                        parrySystem.CloseParryWindow();

                        // Wait for dash to finish if parry didn't interrupt
                        while (!dashComplete && !parried)
                            yield return null;
                    }
                    else if (battleAnimations != null && playerHealth != null)
                    {
                        // Unparryable — normal fast dash
                        bool arrived = false;
                        battleAnimations.PlayDashForward(
                            enemy.transform,
                            playerHealth.transform,
                            () => arrived = true);
                        while (!arrived)
                            yield return null;
                    }

                    // Resolve — hit or parried
                    if (parried)
                    {
                        // Stop the dash animation so it doesn't fight with dash-back
                        if (battleAnimations != null)
                            battleAnimations.StopActiveDash();

                        // Parry succeeded — green flash, small shake, gain 1 OT, dash back
                        Debug.Log("[BattleManager] Parry SUCCESS — no damage dealt, +1 OT.");
                        if (battleAnimations != null)
                        {
                            battleAnimations.PlayParryFlash();
                            battleAnimations.PlayScreenShake(1, 10);
                        }

                        // Gain 1 Overtime on successful parry
                        if (overtimeMeter != null)
                            overtimeMeter.GainFlat(1);
                        if (overtimeMeterUI != null)
                            overtimeMeterUI.Refresh();

                        if (battleAnimations != null)
                        {
                            bool backDone = false;
                            battleAnimations.PlayDashBack(enemy.transform, enemyStartPos, () => backDone = true);
                            while (!backDone) yield return null;
                        }
                        break;
                    }

                    // Parry missed or unparryable — hit shake then dash back
                    if (battleAnimations != null)
                        battleAnimations.StopActiveDash();
                    if (battleAnimations != null && playerHealth != null)
                        battleAnimations.PlayHitShake(playerHealth.transform);

                    // Dash enemy back to start position
                    if (battleAnimations != null)
                    {
                        bool backDone2 = false;
                        battleAnimations.PlayDashBack(enemy.transform, enemyStartPos, () => backDone2 = true);
                        while (!backDone2) yield return null;
                    }

                    // Deal damage
                    int bleedBonus = statusEffectSystem.GetBleedBonus(playerGO);
                    int totalDamage = result.DamageValue + bleedBonus;

                    // Absorb through player Block
                    int remaining = blockSystem.AbsorbDamage(totalDamage, playerGO);

                    int hpBefore = playerHealth != null ? playerHealth.currentHealth : 0;

                    if (remaining > 0 && playerHealth != null)
                        playerHealth.TakeDamage(remaining);

                    int hpAfter = playerHealth != null ? playerHealth.currentHealth : 0;
                    int actualHPLost = hpBefore - hpAfter;

                    // Gain OT from damage taken
                    if (actualHPLost > 0 && playerHealth != null)
                        overtimeMeter.GainFromDamage(actualHPLost, playerHealth.maxHealth);

                    // Update player HP UI
                    if (playerHPStack != null && playerHealth != null)
                        playerHPStack.UpdateHP(playerHealth.currentHealth, playerHealth.maxHealth);
                    if (playerHPBar != null && playerHealth != null)
                        playerHPBar.UpdateHP(playerHealth.currentHealth, playerHealth.maxHealth);

                    // Refresh OT UI after damage-to-OT gain
                    if (overtimeMeterUI != null)
                        overtimeMeterUI.Refresh();

                    // Raise DamageEvent
                    if (BattleEventBus.Instance != null)
                    {
                        BattleEventBus.Instance.Raise(new DamageEvent
                        {
                            Source = enemy.gameObject,
                            Target = playerGO,
                            Amount = totalDamage
                        });
                    }

                    // Check player defeat
                    if (playerHealth != null && playerHealth.currentHealth <= 0)
                    {
                        OnDefeat();
                        yield break;
                    }

                    // Screen shake proportional to damage
                    if (battleAnimations != null && playerHealth != null)
                        battleAnimations.PlayScreenShake(totalDamage, playerHealth.maxHealth);

                    break;

                case EnemyActionType.ApplyStatus:
                    if (!string.IsNullOrEmpty(result.StatusEffectId))
                    {
                        statusEffectSystem.Apply(playerGO, new StatusEffectInstance
                        {
                            effectId = result.StatusEffectId,
                            duration = result.StatusDuration,
                            value = result.StatusValue
                        });
                    }
                    break;

                case EnemyActionType.Defend:
                    // Block is already applied inside EnemyCombatant.ExecuteAction()
                    // Play a subtle defensive animation on the enemy
                    if (battleAnimations != null)
                        battleAnimations.PlayHitShake(enemy.transform);
                    UpdateEnemyHPBar(enemy);
                    break;

                case EnemyActionType.Buff:
                    // Apply the buff as a status effect on the enemy itself
                    if (result.BuffType != EnemyBuffType.None)
                    {
                        string buffEffectId = "EnemyBuff_" + result.BuffType.ToString();
                        statusEffectSystem.Apply(enemy.gameObject, new StatusEffectInstance
                        {
                            effectId = buffEffectId,
                            duration = result.BuffDuration > 0 ? result.BuffDuration : 2,
                            value = result.DamageValue
                        });

                        // Visual feedback — subtle shake to indicate buff activation
                        if (battleAnimations != null)
                            battleAnimations.PlayHitShake(enemy.transform);
                    }
                    break;

                case EnemyActionType.Special:
                    // Special actions are enemy-specific; log for now
                    Debug.Log($"Enemy {enemy.Data?.enemyName} performed a special action.");
                    break;
            }
        }

        private void FinishEnemyPhase()
        {
            if (!_encounterActive) return;

            // Discard any Defense cards that survived the Enemy_Phase (unused parry cards)
            List<CardInstance> remainingCards = new List<CardInstance>(handManager.Cards);
            foreach (CardInstance card in remainingCards)
            {
                deckManager.Discard(card.Data);
                handManager.RemoveCard(card);
            }

            // Advance from Enemy → Draw (increments turn number)
            turnPhaseController.AdvancePhase();

            // Reset player Block at start of new turn
            blockSystem.Reset(playerObject ?? gameObject);

            // Process player Burn at start of player's turn
            GameObject playerGO = playerObject ?? gameObject;
            int burnDamage = statusEffectSystem.ProcessBurn(playerGO);
            if (burnDamage > 0)
            {
                int bleedBonus = statusEffectSystem.GetBleedBonus(playerGO);
                int totalBurn = burnDamage + bleedBonus;

                int remainingBurn = blockSystem.AbsorbDamage(totalBurn, playerGO);

                int hpBefore = playerHealth != null ? playerHealth.currentHealth : 0;

                if (remainingBurn > 0 && playerHealth != null)
                    playerHealth.TakeDamage(remainingBurn);

                int hpAfter = playerHealth != null ? playerHealth.currentHealth : 0;
                int actualHPLost = hpBefore - hpAfter;

                // OT gain from burn is capped at 1 per tick
                if (actualHPLost > 0 && playerHealth != null)
                    overtimeMeter.GainFromDamage(actualHPLost, playerHealth.maxHealth, isStatusTick: true);

                if (playerHPStack != null && playerHealth != null)
                    playerHPStack.UpdateHP(playerHealth.currentHealth, playerHealth.maxHealth);
                if (playerHPBar != null && playerHealth != null)
                    playerHPBar.UpdateHP(playerHealth.currentHealth, playerHealth.maxHealth);

                if (BattleEventBus.Instance != null)
                {
                    BattleEventBus.Instance.Raise(new DamageEvent
                    {
                        Source = playerGO,
                        Target = playerGO,
                        Amount = totalBurn
                    });
                }

                if (playerHealth != null && playerHealth.currentHealth <= 0)
                {
                    OnDefeat();
                    return;
                }
            }

            // Regenerate OT (turn 2 onward)
            if (turnPhaseController.TurnNumber >= 2)
                overtimeMeter.Regenerate();

            // Refresh OT UI after regen
            if (overtimeMeterUI != null)
                overtimeMeterUI.Refresh();

            // Refresh enemy intents for the new turn
            foreach (EnemyIntentDisplay intent in _enemyIntents)
            {
                if (intent != null) intent.Refresh();
            }
            if (screenEnemyIntent != null) screenEnemyIntent.Refresh();

            // Draw new hand
            DrawHand();

            // Advance from Draw → Play (or Draw → Discard if stunned)
            turnPhaseController.AdvancePhase();

            // If player is stunned, auto-advance through Discard → Enemy
            if (CurrentTurn == TurnPhase.Discard)
            {
                // Player was stunned — skip Play phase
                // Discard the hand we just drew
                foreach (CardInstance card in handManager.Cards)
                    deckManager.Discard(card.Data);
                handManager.DiscardAll();

                turnPhaseController.AdvancePhase(); // Discard → Enemy
                StartCoroutine(EnemyPhaseRoutine());
            }
        }

        // ── Initialization helpers ────────────────────────────────────────────

        private void ApplyToolModifiers()
        {
            // Reset subsystem modifiers before re-applying
            if (cardEffectResolver != null)
                cardEffectResolver.ResetModifiers();

            // Query RunState for tool IDs and load ToolData assets
            RunState runState = FindRunState();
            if (runState == null || runState.toolIds == null) return;

            foreach (string toolId in runState.toolIds)
            {
                ToolData tool = Resources.Load<ToolData>(toolId);
                if (tool == null) continue;

                foreach (ToolModifier mod in tool.modifiers)
                {
                    switch (mod.modifierType)
                    {
                        case ToolModifierType.OvertimeRegen:
                            if (overtimeMeter != null)
                                overtimeMeter.ApplyRegenModifier(mod.value);
                            break;
                        case ToolModifierType.HandSize:
                            _handSize += mod.value;
                            break;
                        case ToolModifierType.ParryWindowBonus:
                            if (parrySystem != null)
                                parrySystem.ApplyWindowDurationModifier(mod.value * 0.01f);
                            break;
                        case ToolModifierType.DamageBonus:
                            if (cardEffectResolver != null)
                                cardEffectResolver.ApplyDamageBonus(mod.value);
                            break;
                        case ToolModifierType.MaxHP:
                            if (playerHealth != null)
                            {
                                playerHealth.maxHealth += mod.value;
                                playerHealth.currentHealth += mod.value;
                            }
                            break;
                    }
                }
            }
        }

        private RunState FindRunState()
        {
            if (SaveManager.Instance != null)
                return SaveManager.Instance.CurrentRun;
            return null;
        }

        private void InitializePlayerHP()
        {
            if (playerObject == null)
            {
                GameObject found = GameObject.FindWithTag("Player");
                playerObject = found != null ? found : gameObject;
            }

            if (playerHealth == null)
                playerHealth = playerObject.GetComponent<Health>();

            if (playerHealth != null)
            {
                int maxHP = gameConfig != null ? gameConfig.playerBaseHP : 80;
                playerHealth.maxHealth = maxHP;
                playerHealth.currentHealth = maxHP;
                playerHealth.suppressSceneLoad = true;

                Rigidbody rb = playerHealth.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = true;
            }

            if (playerHPStack != null && playerHealth != null)
            {
                playerHPStack.SetTrackedHealth(playerHealth);
                playerHPStack.Initialize(playerHealth.currentHealth, playerHealth.maxHealth);
            }
            if (playerHPBar != null && playerHealth != null)
                playerHPBar.Initialize(playerHealth.currentHealth, playerHealth.maxHealth);
        }

        private void SpawnEnemies(EncounterData encounter)
        {
            // Clean up any existing managed enemies
            foreach (EnemyCombatant existing in _enemies)
            {
                if (existing != null)
                    Destroy(existing.gameObject);
            }
            _enemies.Clear();
            _enemyHPBars.Clear();
            _enemyIntents.Clear();

            // If no enemy prefab is set, try to use existing scene enemies
            if (enemyPrefab == null)
            {
                // Find pre-placed enemies in the scene
                Health[] sceneEnemies = FindObjectsOfType<Health>();
                List<GameObject> candidateEnemies = new List<GameObject>();
                foreach (Health h in sceneEnemies)
                {
                    // Skip the player
                    if (h.gameObject == playerObject) continue;
                    if (h.CompareTag("Player")) continue;
                    candidateEnemies.Add(h.gameObject);
                }

                for (int i = 0; i < encounter.enemies.Count && i < candidateEnemies.Count; i++)
                {
                    EnemyCombatantData enemyData = encounter.enemies[i];
                    if (enemyData == null) continue;

                    GameObject enemyGO = candidateEnemies[i];
                    PrepareEnemyForBattle(enemyGO, enemyData);
                }
                return;
            }

            for (int i = 0; i < encounter.enemies.Count && i < 4; i++)
            {
                EnemyCombatantData enemyData = encounter.enemies[i];
                if (enemyData == null) continue;

                // Determine spawn position
                Vector3 spawnPos = Vector3.zero;
                if (enemySpawnPoints != null && i < enemySpawnPoints.Length && enemySpawnPoints[i] != null)
                    spawnPos = enemySpawnPoints[i].position;

                GameObject enemyGO = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
                PrepareEnemyForBattle(enemyGO, enemyData);
            }
        }

        private void PrepareEnemyForBattle(GameObject enemyGO, EnemyCombatantData enemyData)
        {
            EnemyCombatant combatant = enemyGO.GetComponent<EnemyCombatant>();
            if (combatant == null)
                combatant = enemyGO.AddComponent<EnemyCombatant>();

            // Ensure Health component exists
            Health health = enemyGO.GetComponent<Health>();
            if (health == null)
                enemyGO.AddComponent<Health>();

            combatant.Initialize(enemyData, blockSystem, statusEffectSystem);

            // Lock physics and disable exploration AI during battle
            Rigidbody rb = enemyGO.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            EnemyFollow follow = enemyGO.GetComponent<EnemyFollow>();
            if (follow != null) follow.enabled = false;

            // Disable Battlescene_Trigger if present (exploration-only script)
            Battlescene_Trigger trigger = enemyGO.GetComponent<Battlescene_Trigger>();
            if (trigger != null) trigger.enabled = false;

            // Freeze NavMeshAgent if present
            UnityEngine.AI.NavMeshAgent agent = enemyGO.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null) agent.enabled = false;

            _enemies.Add(combatant);

            // Set up HP bar if available on the prefab
            EnemyHPBar hpBar = enemyGO.GetComponentInChildren<EnemyHPBar>();
            if (hpBar != null)
            {
                hpBar.Initialize(combatant);
                _enemyHPBars.Add(hpBar);
            }

            // Set up intent display if available on the prefab
            EnemyIntentDisplay intentDisplay = enemyGO.GetComponentInChildren<EnemyIntentDisplay>();
            if (intentDisplay != null)
            {
                intentDisplay.Initialize(combatant);
                _enemyIntents.Add(intentDisplay);
            }

            // Initialize screen-space enemy HP bar for first enemy
            if (screenEnemyHPBar != null && _enemies.Count == 1)
                screenEnemyHPBar.Initialize(combatant);

            // Initialize screen-space enemy intent display for first enemy
            if (screenEnemyIntent != null && _enemies.Count == 1)
                screenEnemyIntent.Initialize(combatant);

            // Initialize screen-space enemy status effect icons for first enemy
            if (enemyStatusEffectIcons != null && _enemies.Count == 1)
                enemyStatusEffectIcons.Initialize(combatant.gameObject);
        }

        private void InitializeDeck()
        {
            // Try to load deck from RunState; fallback to inspector deck
            RunState runState = FindRunState();
            List<CardData> deckCards = new List<CardData>();

            if (runState != null && runState.deckCardIds != null)
            {
                foreach (string cardId in runState.deckCardIds)
                {
                    CardData card = Resources.Load<CardData>(cardId);
                    if (card != null)
                        deckCards.Add(card);
                }
            }

            // Use fallback deck if RunState didn't provide cards
            if (deckCards.Count == 0 && fallbackDeck != null && fallbackDeck.Count > 0)
            {
                deckCards.AddRange(fallbackDeck);
                // If deck is too small for a full hand, duplicate cards
                while (deckCards.Count < _handSize && fallbackDeck.Count > 0)
                {
                    foreach (CardData c in fallbackDeck)
                    {
                        deckCards.Add(c);
                        if (deckCards.Count >= _handSize * 2) break;
                    }
                }
            }

            if (deckCards.Count > 0)
                deckManager.Initialize(deckCards);
            // If no cards loaded, assume DeckManager was already initialized externally
        }

        private void DrawHand()
        {
            List<CardData> drawn = new List<CardData>();
            for (int i = 0; i < _handSize; i++)
            {
                CardData card = deckManager.Draw();
                if (card != null)
                    drawn.Add(card);
            }
            if (drawn.Count > 0)
                handManager.AddCards(drawn);
        }

        // ── Enemy management ──────────────────────────────────────────────────

        private List<EnemyCombatant> GetLivingEnemies()
        {
            List<EnemyCombatant> living = new List<EnemyCombatant>();
            foreach (EnemyCombatant enemy in _enemies)
            {
                if (enemy != null && enemy.IsAlive)
                    living.Add(enemy);
            }
            return living;
        }

        private void CheckEnemyDeaths()
        {
            for (int i = _enemies.Count - 1; i >= 0; i--)
            {
                EnemyCombatant enemy = _enemies[i];
                if (enemy != null && !enemy.IsAlive)
                {
                    // Hide intent display for dead enemy
                    EnemyIntentDisplay intent = enemy.GetComponentInChildren<EnemyIntentDisplay>();
                    if (intent != null)
                        intent.Hide();

                    // Clean up the corresponding HP bar
                    if (i < _enemyHPBars.Count)
                    {
                        EnemyHPBar deadBar = _enemyHPBars[i];
                        if (deadBar != null)
                            deadBar.gameObject.SetActive(false);
                        _enemyHPBars.RemoveAt(i);
                    }

                    // Play death animation
                    if (battleAnimations != null)
                    {
                        Transform t = enemy.transform;
                        battleAnimations.PlayDeath(t, () =>
                        {
                            if (t != null)
                                Destroy(t.gameObject);
                        });
                    }
                    else
                    {
                        Destroy(enemy.gameObject);
                    }

                    // Clear status effects for dead enemy
                    statusEffectSystem.ClearAll(enemy.gameObject);

                    _enemies.RemoveAt(i);
                }
            }

            // Reassign screen-space enemy HP bar if the tracked enemy died
            if (screenEnemyHPBar != null)
            {
                EnemyCombatant tracked = screenEnemyHPBar.TrackedEnemy;
                bool needsReassign = tracked == null || !tracked.IsAlive;

                if (needsReassign)
                {
                    List<EnemyCombatant> living = GetLivingEnemies();
                    if (living.Count > 0)
                    {
                        screenEnemyHPBar.SetEnemy(living[0]);
                        // Also reassign screen-space intent and status icons
                        if (screenEnemyIntent != null)
                            screenEnemyIntent.Initialize(living[0]);
                        if (enemyStatusEffectIcons != null)
                            enemyStatusEffectIcons.Initialize(living[0].gameObject);
                    }
                    else
                    {
                        screenEnemyHPBar.gameObject.SetActive(false);
                    }
                }
            }
        }

        private void UpdateEnemyHPBar(EnemyCombatant enemy)
        {
            int idx = _enemies.IndexOf(enemy);
            if (idx >= 0 && idx < _enemyHPBars.Count && _enemyHPBars[idx] != null)
                _enemyHPBars[idx].UpdateHP(enemy.CurrentHP, enemy.MaxHP);

            // Update screen-space enemy HP bar if it's tracking this enemy
            if (screenEnemyHPBar != null && screenEnemyHPBar.TrackedEnemy == enemy)
                screenEnemyHPBar.UpdateHP(enemy.CurrentHP, enemy.MaxHP);
        }

        // ── Victory / Defeat ──────────────────────────────────────────────────

        private void OnVictory()
        {
            _encounterActive = false;

            // Stop visual effects
            if (battleAnimations != null)
                battleAnimations.StopOverflowGlow();

            // Clear player status effects on encounter end
            statusEffectSystem.ClearAll(playerObject ?? gameObject);

            // Award Hours from defeated enemies (sum of each enemy's hoursReward)
            _victoryHours = 0;
            List<string> enemyNames = new List<string>();
            foreach (EnemyCombatant enemy in _enemies)
            {
                // Unity destroys the GO but the C# reference isn't null — use ReferenceEquals check
                if (enemy == null) continue;

                Debug.Log($"[Victory] Enemy: {(enemy.Data != null ? enemy.Data.enemyName : "NULL DATA")}, HoursReward: {enemy.HoursReward}, IsAlive: {enemy.IsAlive}");
                _victoryHours += enemy.HoursReward;
                if (enemy.Data != null && !string.IsNullOrEmpty(enemy.Data.enemyName))
                    enemyNames.Add(enemy.Data.enemyName);
            }

            // If no hours were collected (enemies destroyed), use cached encounter data
            if (_victoryHours == 0 && _currentEncounter != null && _currentEncounter.enemies != null)
            {
                foreach (var enemyData in _currentEncounter.enemies)
                {
                    if (enemyData != null)
                    {
                        _victoryHours += enemyData.hoursReward;
                        if (!string.IsNullOrEmpty(enemyData.enemyName))
                            enemyNames.Add(enemyData.enemyName);
                    }
                }
                Debug.Log($"[Victory] Fallback from EncounterData: {_victoryHours} hours");
            }

            // Determine boss rewards
            _victoryIsBoss = _currentEncounter != null && _currentEncounter.isBossEncounter;
            _victoryBadReviews = _currentEncounter != null ? _currentEncounter.badReviewsReward : 0;

            // No card rewards from encounters (cards from Work_Boxes, shops, trades only)

            // Show victory screen if available, otherwise transition immediately
            if (victoryScreen != null)
            {
                victoryScreen.OnDismissed = () => ReturnToExploration();
                victoryScreen.Show(enemyNames, _victoryHours, _victoryBadReviews, _victoryIsBoss);
            }
            else
            {
                ReturnToExploration();
            }
        }

        // Cached victory rewards for use in ReturnToExploration
        private int _victoryHours;
        private int _victoryBadReviews;
        private bool _victoryIsBoss;

        private void ReturnToExploration()
        {
            if (SceneLoader.Instance != null)
            {
                string enemyId = SceneLoader.Instance.CurrentBattleEnemyId;
                SceneLoader.Instance.OnBattleVictory(enemyId, _victoryHours, _victoryBadReviews);
            }
        }

        private void OnDefeat()
        {
            _encounterActive = false;

            // Stop visual effects
            if (battleAnimations != null)
                battleAnimations.StopOverflowGlow();

            // Clear player status effects
            statusEffectSystem.ClearAll(playerObject ?? gameObject);

            // Play death animation then trigger run reset via SceneLoader
            if (battleAnimations != null && playerHealth != null)
            {
                battleAnimations.PlayDeath(playerHealth.transform, () =>
                {
                    if (SceneLoader.Instance != null)
                        SceneLoader.Instance.OnBattleDefeat();
                });
            }
            else if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.OnBattleDefeat();
            }
        }
    }
}
