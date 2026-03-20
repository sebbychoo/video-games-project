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
        [SerializeField] BattleAnimations    battleAnimations;
        [SerializeField] PlayerHPStack       playerHPStack;
        [SerializeField] EnemyHPBar          enemyHPBar;
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

            // Lock physics during battle — no gravity drift
            Rigidbody enemyRb = enemyHealth.GetComponent<Rigidbody>();
            if (enemyRb != null) enemyRb.isKinematic = true;

            if (playerHealth != null)
            {
                playerHealth.suppressSceneLoad = true;
                playerHealth.maxHealth         = startingPlayerHealth;
                playerHealth.currentHealth     = startingPlayerHealth;

                Rigidbody playerRb = playerHealth.GetComponent<Rigidbody>();
                if (playerRb != null) playerRb.isKinematic = true;
            }

            deckManager.Initialize(startingDeck);

            var openingHand = new List<CardData>();
            for (int i = 0; i < openingHandSize; i++)
            {
                CardData card = deckManager.Draw();
                if (card != null) openingHand.Add(card);
            }
            handManager.AddCards(openingHand);

            // Initialize HP UI
            if (playerHPStack != null)
                playerHPStack.Initialize(startingPlayerHealth, startingPlayerHealth);
            if (enemyHPBar != null)
                enemyHPBar.Initialize(startingEnemyHealth, startingEnemyHealth);

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
        private int _lastEndTurnFrame = -1;

        public void EndTurn()
        {
            if (CurrentTurn != TurnState.PlayerTurn) return;

            // Guard against double-fire from UI in same frame
            if (Time.frameCount == _lastEndTurnFrame) return;
            _lastEndTurnFrame = Time.frameCount;

            // Discard all cards in hand back to the deck manager before destroying them
            foreach (CardInstance card in handManager.Cards)
                deckManager.Discard(card.Data);

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
                    // Dash player toward target, then deal damage + shake
                    if (battleAnimations != null && playerHealth != null && targetHealth != null)
                    {
                        battleAnimations.PlayAttackDash(
                            playerHealth.transform,
                            targetHealth.transform,
                            () =>
                            {
                                DealAttackDamage(data, targetHealth);
                                battleAnimations.PlayHitShake(targetHealth.transform);
                            });
                    }
                    else
                    {
                        DealAttackDamage(data, targetHealth);
                    }
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

        private void DealAttackDamage(CardData data, Health targetHealth)
        {
            targetHealth.TakeDamage(data.effectValue);

            if (targetHealth == enemyHealth)
            {
                EnemyHealth = enemyHealth.currentHealth;
                if (enemyHPBar != null)
                    enemyHPBar.UpdateHP(EnemyHealth, startingEnemyHealth);
            }
            else if (targetHealth == playerHealth)
            {
                PlayerHealth = playerHealth.currentHealth;
                if (playerHPStack != null)
                    playerHPStack.UpdateHP(PlayerHealth, startingPlayerHealth);
            }

            if (BattleEventBus.Instance != null)
            {
                BattleEventBus.Instance.Raise(new DamageEvent
                {
                    Source = gameObject,
                    Target = targetHealth.gameObject,
                    Amount = data.effectValue
                });
            }

            if (targetHealth == enemyHealth && enemyHealth.currentHealth <= 0)
                OnEnemyDefeated();
            else if (targetHealth == playerHealth && playerHealth.currentHealth <= 0)
                OnPlayerDefeated();
        }

        private void ExecuteEnemyTurn()
        {
            if (enemyActions == null || enemyActions.Count == 0)
            {
                FinishEnemyTurn();
                return;
            }

            // Start enemy turn with a delay so player sees the empty hand
            StartCoroutine(EnemyTurnRoutine());
        }

        private System.Collections.IEnumerator EnemyTurnRoutine()
        {
            // Wait a moment so player sees their cards are gone
            yield return new WaitForSeconds(0.5f);

            EnemyAction action = enemyActions[_enemyActionIndex % enemyActions.Count];
            _enemyActionIndex++;

            switch (action.actionType)
            {
                case EnemyActionType.DealDamage:
                    // Enemy dashes toward player, then shakes player on hit
                    bool dashDone = false;
                    if (battleAnimations != null && enemyHealth != null && playerHealth != null)
                    {
                        battleAnimations.PlayAttackDash(
                            enemyHealth.transform,
                            playerHealth.transform,
                            () =>
                            {
                                battleAnimations.PlayHitShake(playerHealth.transform);
                                dashDone = true;
                            });
                        // Wait for dash to finish
                        while (!dashDone)
                            yield return null;
                    }

                    PlayerHealth -= action.value;
                    if (playerHealth != null)
                        playerHealth.currentHealth = PlayerHealth;
                    Debug.Log($"Enemy deals {action.value} damage! Player HP: {PlayerHealth}");

                    if (playerHPStack != null)
                        playerHPStack.UpdateHP(PlayerHealth, startingPlayerHealth);

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
                        yield break;
                    }
                    break;

                case EnemyActionType.ApplyStatus:
                    break;
            }

            // Wait after attack before drawing new cards
            yield return new WaitForSeconds(0.5f);

            FinishEnemyTurn();
        }

        private void FinishEnemyTurn()
        {
            CurrentTurn = TurnState.PlayerTurn;
            Energy      = energyPerTurn;

            var drawn = new List<CardData>();
            for (int i = 0; i < drawPerTurn; i++)
            {
                CardData card = deckManager.Draw();
                if (card != null)
                    drawn.Add(card);
            }
            if (drawn.Count > 0)
                handManager.AddCards(drawn);
        }

        private void OnEnemyDefeated()
        {
            CurrentTurn = TurnState.BattleOver;

            if (battleAnimations != null && enemyHealth != null)
            {
                battleAnimations.PlayDeath(enemyHealth.transform, () =>
                {
                    if (SceneLoader.Instance != null)
                    {
                        SceneLoader.Instance.enemyDefeated = true;
                        SceneLoader.Instance.useDefaultSpawn = false;
                        SceneLoader.Instance.LoadExploration();
                    }
                });
            }
            else if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.enemyDefeated = true;
                SceneLoader.Instance.useDefaultSpawn = false;
                SceneLoader.Instance.LoadExploration();
            }
        }

        private void OnPlayerDefeated()
        {
            CurrentTurn = TurnState.BattleOver;

            if (battleAnimations != null && playerHealth != null)
            {
                battleAnimations.PlayDeath(playerHealth.transform, () =>
                {
                    if (!useLoseScreen && SceneLoader.Instance != null)
                    {
                        SceneLoader.Instance.useDefaultSpawn = true;
                        SceneLoader.Instance.LoadExploration();
                    }
                });
            }
            else if (!useLoseScreen && SceneLoader.Instance != null)
            {
                SceneLoader.Instance.useDefaultSpawn = true;
                SceneLoader.Instance.LoadExploration();
            }
        }
    }
}
