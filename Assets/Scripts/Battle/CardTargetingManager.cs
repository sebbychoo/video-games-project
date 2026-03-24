using System.Collections.Generic;
using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// Manages card selection and targeting.
    /// Flow: hover card to preview → click card to select it →
    ///   - SingleEnemy: highlight valid targets (enemies + player + allies), wait for click
    ///   - AllEnemies: highlight all enemies, play on confirmation click
    ///   - Self/NoTarget: play immediately without target selection
    /// Right-click or Escape cancels selection.
    /// Interactions blocked when not in Play_Phase.
    /// Supports targeting any entity including Jean-Guy and allied NPCs (Req 4.5).
    /// </summary>
    public class CardTargetingManager : MonoBehaviour
    {
        public static CardTargetingManager Instance { get; private set; }

        public CardInstance SelectedCard { get; private set; }
        public CardInstance HoveredCard  { get; private set; }

        public bool HasSelectedCard => SelectedCard != null;
        public bool HasHoveredCard  => HoveredCard  != null;

        /// <summary>True when we are in AllEnemies confirmation mode.</summary>
        public bool IsAoEConfirmMode { get; private set; }

        [SerializeField] Camera battleCamera;
        [SerializeField] float  tiltStrength = 30f;

        [Header("Highlight Colors")]
        [SerializeField] Color enemyHighlightColor  = Color.red;
        [SerializeField] Color playerHighlightColor  = Color.green;
        [SerializeField] Color aoeHighlightColor     = new Color(1f, 0.5f, 0f); // orange

        /// <summary>All OutlineEffects currently highlighted by targeting.</summary>
        private readonly List<OutlineEffect> _activeHighlights = new List<OutlineEffect>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            if (battleCamera == null)
                battleCamera = Camera.main;
        }

        public void SetHoveredCard(CardInstance card)
        {
            HoveredCard = card;
        }

        public void ClearHoveredCard(CardInstance card)
        {
            if (HoveredCard == card)
                HoveredCard = null;
        }

        /// <summary>Called when player clicks a card — selects it for targeting.</summary>
        public void SelectCard(CardInstance card)
        {
            if (BattleManager.Instance == null) return;
            if (BattleManager.Instance.CurrentTurn != TurnPhase.Play) return;

            // If clicking the already-selected card, deselect it
            if (SelectedCard == card)
            {
                CancelSelection();
                return;
            }

            // Deselect the previous card first
            if (SelectedCard != null)
            {
                HandManager hm = BattleManager.Instance != null ? BattleManager.Instance.HandManager : null;
                hm?.OnCardDeselected(SelectedCard);
                SelectedCard.IsSelected = false;
                ClearHighlights();
            }

            SelectedCard = card;
            card.IsSelected = true;
            IsAoEConfirmMode = false;

            {
                HandManager hm = BattleManager.Instance != null ? BattleManager.Instance.HandManager : null;
                hm?.OnCardSelected(card);
            }

            // Handle targeting based on TargetMode
            HandleTargetMode(card.Data);
        }

        /// <summary>Dispatch targeting behavior based on the card's TargetMode.</summary>
        private void HandleTargetMode(CardData data)
        {
            switch (data.targetMode)
            {
                case TargetMode.SingleEnemy:
                    // Don't highlight upfront — EnemyTargetable/PlayerTargetable
                    // handle hover-based highlighting when a card is selected
                    break;

                case TargetMode.AllEnemies:
                    // Highlight all enemies, wait for confirmation click
                    HighlightAllEnemies();
                    IsAoEConfirmMode = true;
                    break;

                case TargetMode.Self:
                    // Play immediately on the player
                    PlayCardOnSelf();
                    break;

                case TargetMode.NoTarget:
                    // Play immediately with no target
                    PlayCardNoTarget();
                    break;
            }
        }

        /// <summary>Highlight all valid targets: enemies, player, and allied NPCs.</summary>
        private void HighlightAllTargets()
        {
            // Highlight enemies
            EnemyTargetable[] enemies = FindObjectsOfType<EnemyTargetable>();
            foreach (EnemyTargetable et in enemies)
            {
                OutlineEffect outline = et.GetComponent<OutlineEffect>();
                if (outline != null)
                {
                    outline.ShowOutline(enemyHighlightColor);
                    _activeHighlights.Add(outline);
                }
            }

            // Highlight player
            PlayerTargetable pt = FindObjectOfType<PlayerTargetable>();
            if (pt != null)
            {
                OutlineEffect outline = pt.GetComponent<OutlineEffect>();
                if (outline != null)
                {
                    outline.ShowOutline(playerHighlightColor);
                    _activeHighlights.Add(outline);
                }
            }

            // Highlight allied NPCs (any AllyTargetable in the scene)
            AllyTargetable[] allies = FindObjectsOfType<AllyTargetable>();
            foreach (AllyTargetable at in allies)
            {
                OutlineEffect outline = at.GetComponent<OutlineEffect>();
                if (outline != null)
                {
                    outline.ShowOutline(playerHighlightColor);
                    _activeHighlights.Add(outline);
                }
            }
        }

        /// <summary>Highlight all enemies for AoE confirmation.</summary>
        private void HighlightAllEnemies()
        {
            EnemyTargetable[] enemies = FindObjectsOfType<EnemyTargetable>();
            foreach (EnemyTargetable et in enemies)
            {
                OutlineEffect outline = et.GetComponent<OutlineEffect>();
                if (outline != null)
                {
                    outline.ShowOutline(aoeHighlightColor);
                    _activeHighlights.Add(outline);
                }
            }
        }

        private void ClearHighlights()
        {
            foreach (OutlineEffect outline in _activeHighlights)
            {
                if (outline != null)
                    outline.HideOutline();
            }
            _activeHighlights.Clear();
        }

        /// <summary>Play the selected card immediately targeting the player.</summary>
        private void PlayCardOnSelf()
        {
            if (SelectedCard == null) return;

            GameObject playerGO = BattleManager.Instance != null
                ? BattleManager.Instance.gameObject
                : null;

            // Find the actual player object
            PlayerTargetable pt = FindObjectOfType<PlayerTargetable>();
            if (pt != null)
                playerGO = pt.gameObject;

            CardInstance card = SelectedCard;
            SelectedCard = null;
            card.IsSelected = false;
            IsAoEConfirmMode = false;
            ClearHighlights();

            if (BattleManager.Instance != null)
                BattleManager.Instance.TryPlayCard(card, playerGO);
        }

        /// <summary>Play the selected card with no specific target.</summary>
        private void PlayCardNoTarget()
        {
            if (SelectedCard == null) return;

            CardInstance card = SelectedCard;
            SelectedCard = null;
            card.IsSelected = false;
            IsAoEConfirmMode = false;
            ClearHighlights();

            if (BattleManager.Instance != null)
                BattleManager.Instance.TryPlayCard(card, null);
        }

        /// <summary>Called when a target is clicked while a card is selected.</summary>
        public void PlayOnTarget(GameObject target)
        {
            if (SelectedCard == null) return;
            if (BattleManager.Instance == null) return;
            if (BattleManager.Instance.CurrentTurn != TurnPhase.Play) return;

            CardInstance card = SelectedCard;
            SelectedCard = null;
            card.IsSelected = false;
            IsAoEConfirmMode = false;
            ClearHighlights();

            BattleManager.Instance.TryPlayCard(card, target);
        }

        /// <summary>
        /// Called when the player clicks anywhere during AoE confirmation mode.
        /// Confirms the AoE card play targeting all enemies.
        /// </summary>
        public void ConfirmAoE()
        {
            if (SelectedCard == null || !IsAoEConfirmMode) return;
            if (BattleManager.Instance == null) return;
            if (BattleManager.Instance.CurrentTurn != TurnPhase.Play) return;

            CardInstance card = SelectedCard;
            SelectedCard = null;
            card.IsSelected = false;
            IsAoEConfirmMode = false;
            ClearHighlights();

            // Pass null target — BattleManager/CardEffectResolver handles AllEnemies via the list
            BattleManager.Instance.TryPlayCard(card, null);
        }

        /// <summary>Cancel selection (right click or Escape).</summary>
        public void CancelSelection()
        {
            if (SelectedCard != null)
            {
                HandManager hm = BattleManager.Instance != null ? BattleManager.Instance.HandManager : null;
                hm?.OnCardDeselected(SelectedCard);
                SelectedCard.IsSelected = false;
                SelectedCard = null;
            }
            IsAoEConfirmMode = false;
            ClearHighlights();
        }

        private void Update()
        {
            // Block all interactions when not in Play_Phase
            if (BattleManager.Instance == null || BattleManager.Instance.CurrentTurn != TurnPhase.Play)
            {
                if (HasSelectedCard)
                    CancelSelection();
                return;
            }

            // Cancel on right click or Escape
            if (HasSelectedCard && (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape)))
            {
                CancelSelection();
                return;
            }

            // AoE confirmation: left click anywhere (not on a card) confirms
            if (IsAoEConfirmMode && Input.GetMouseButtonDown(0))
            {
                // Check we're not clicking on a card in the hand
                if (!IsPointerOverCard())
                {
                    ConfirmAoE();
                    return;
                }
            }

            // Tilt removed — the selected idle shake provides the "alive" feel
        }

        /// <summary>Check if the mouse is currently over a card UI element.</summary>
        private bool IsPointerOverCard()
        {
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem == null) return false;

            var pointerData = new UnityEngine.EventSystems.PointerEventData(eventSystem)
            {
                position = Input.mousePosition
            };
            var results = new List<UnityEngine.EventSystems.RaycastResult>();
            eventSystem.RaycastAll(pointerData, results);

            foreach (var result in results)
            {
                if (result.gameObject.GetComponent<CardInteractionHandler>() != null)
                    return true;
            }
            return false;
        }
    }
}
