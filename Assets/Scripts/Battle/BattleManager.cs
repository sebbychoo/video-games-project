using System.Collections.Generic;
using UnityEngine;

namespace CardBattle
{
    public class BattleManager : MonoBehaviour
    {
        public static BattleManager Instance { get; private set; }

        

        // ── Config ────────────────────────────────────────────────────────────
        [SerializeField] int  startingPlayerHealth = 50;
        [SerializeField] int  startingEnemyHealth  = 30;
        [SerializeField] int  startingEnergy       = 3;
        [SerializeField] int  energyPerTurn        = 3;
        [SerializeField] int  openingHandSize      = 6;
        [SerializeField] int  drawPerTurn          = 2;
        [SerializeField] bool useLoseScreen        = false;

        // ── References ────────────────────────────────────────────────────────
        [SerializeField] Health              playerHealth;
        [SerializeField] Health              enemyHealth;
        [SerializeField] DeckManager         deckManager;
        [SerializeField] HandManager         handManager;
        [SerializeField] CardAnimator        cardAnimator;
        [SerializeField] List<CardData>      startingDeck;
        [SerializeField] List<EnemyAction>   enemyActions;

        // ── State ─────────────────────────────────────────────────────────────
        public int       PlayerHealth { get; private set; }
        public int       EnemyHealth  { get; private set; }
        public int       Energy       { get; private set; }
        public TurnState CurrentTurn  { get; private set; }

        public int DeckCount    => deckManager != null ? deckManager.DeckCount    : 0;
        public int DiscardCount => deckManager != null ? deckManager.DiscardCount : 0;

        private int _enemyActionIndex = 0;

        // Stores the target while the exit animation plays
        private GameObject _pendingTarget;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (deckManager == null)
            {
                Debug.LogError("BattleManager: deckManager reference is null.", this);
                enabled = false;
                return;
            }
            if (handManager == null)
            {
                Debug.LogError("BattleManager: handManager reference is null.", this);
                enabled = false;
                return;
            }
            if (enemyHealth == null)
            {
                Debug.LogError("BattleManager: enemyHealth reference is null.", this);
                enabled = false;
                return;
            }
        }

        private void Start()
        {
            PlayerHealth = startingPlayerHealth;
            EnemyHealth  = startingEnemyHealth;
            Energy       = startingEnergy;

            enemyHealth.suppressSceneLoad = true;
            enemyHealth.maxHealth         = startingEnemyHealth;
            enemyHealth.currentHealth     = startingEnemyHealth;

            if (playerHealth != null)
            {
                playerHealth.suppressSceneLoad = true;
                playerHealth.maxHealth         = startingPlayerHealth;
                playerHealth.currentHealth     = startingPlayerHealth;
            }

            deckManager.Initialize(startingDeck);

            var openingHand = new List<CardData>();
            for (int i = 0; i < openingHandSize; i++)
            {
                CardData card = deckManager.Draw();
                if (card != null) openingHand.Add(card);
            }
            handManager.AddCards(openingHand);

            CurrentTurn = TurnState.PlayerTurn;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Called by CardTargetingManager when the player clicks a target.</summary>
        public void TryPlayCard(CardInstance card, GameObject target)
        {
            if (CurrentTurn != TurnState.PlayerTurn) return;

            if (Energy < card.Data.energyCost)
            {
                if (cardAnimator != null)
                    cardAnimator.PlayRejection(card);
                return;
            }

            Energy -= card.Data.energyCost;
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
            if (CurrentTurn != TurnState.PlayerTurn) return;

            handManager.DiscardAll();
            CurrentTurn = TurnState.EnemyTurn;
            ExecuteEnemyTurn();
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void OnCardExitComplete(CardInstance card)
        {
            CardData data = card.Data;
            GameObject target = _pendingTarget;
            _pendingTarget = null;

            deckManager.Discard(data);
            handManager.RemoveCard(card);

            ApplyCardEffect(data, target);

            if (BattleEventBus.Instance != null)
            {
                BattleEventBus.Instance.Raise(new CardPlayedEvent
                {
                    Card   = data,
                    Source = gameObject,
                    Target = target
                });
            }
        }

        private void ApplyCardEffect(CardData data, GameObject target)
        {
            // Try to get a Health component from the target
            Health targetHealth = target != null ? target.GetComponent<Health>() : null;

            // Fall back to enemy if no Health found on target
            if (targetHealth == null)
                targetHealth = enemyHealth;

            switch (data.cardType)
            {
                case CardType.Attack:
                    targetHealth.TakeDamage(data.effectValue);

                    // Update tracked health values
                    if (targetHealth == enemyHealth)
                        EnemyHealth = enemyHealth.currentHealth;
                    else if (targetHealth == playerHealth)
                        PlayerHealth = playerHealth.currentHealth;

                    if (BattleEventBus.Instance != null)
                    {
                        BattleEventBus.Instance.Raise(new DamageEvent
                        {
                            Source = gameObject,
                            Target = targetHealth.gameObject,
                            Amount = data.effectValue
                        });
                    }

                    // Check defeat conditions
                    if (targetHealth == enemyHealth && enemyHealth.currentHealth <= 0)
                        OnEnemyDefeated();
                    else if (targetHealth == playerHealth && playerHealth.currentHealth <= 0)
                        OnPlayerDefeated();
                    break;

                case CardType.Skill:
                    for (int i = 0; i < drawPerTurn; i++)
                    {
                        CardData drawn = deckManager.Draw();
                        if (drawn != null)
                            handManager.AddCard(drawn);
                    }
                    break;
            }
        }

        private void ExecuteEnemyTurn()
        {
            if (enemyActions == null || enemyActions.Count == 0)
            {
                FinishEnemyTurn();
                return;
            }

            EnemyAction action = enemyActions[_enemyActionIndex % enemyActions.Count];
            _enemyActionIndex++;

            switch (action.actionType)
            {
                case EnemyActionType.DealDamage:
                    PlayerHealth -= action.value;

                    if (BattleEventBus.Instance != null)
                    {
                        BattleEventBus.Instance.Raise(new DamageEvent
                        {
                            Source = enemyHealth != null ? enemyHealth.gameObject : null,
                            Target = gameObject,
                            Amount = action.value
                        });
                    }

                    if (PlayerHealth <= 0)
                    {
                        OnPlayerDefeated();
                        return;
                    }
                    break;

                case EnemyActionType.ApplyStatus:
                    break;
            }

            FinishEnemyTurn();
        }

        private void FinishEnemyTurn()
        {
            CurrentTurn = TurnState.PlayerTurn;
            Energy      = energyPerTurn;

            for (int i = 0; i < drawPerTurn; i++)
            {
                CardData card = deckManager.Draw();
                if (card != null)
                    handManager.AddCard(card);
            }
        }

        private void OnEnemyDefeated()
        {
            CurrentTurn = TurnState.BattleOver;

            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.enemyDefeated = true;
                SceneLoader.Instance.LoadExploration();
            }
        }

        private void OnPlayerDefeated()
        {
            CurrentTurn = TurnState.BattleOver;

            if (!useLoseScreen && SceneLoader.Instance != null)
                SceneLoader.Instance.LoadExploration();
        }
    }
}
