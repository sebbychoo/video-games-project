using System.Collections.Generic;
using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// Manages the parry mechanic during Enemy_Phase attacks and enemy parry chance
    /// when the player plays Attack cards. During an enemy attack, a timed Parry_Window
    /// opens where the player can drag a matching Defense card to cancel damage.
    /// </summary>
    public class ParrySystem : MonoBehaviour
    {
        private bool _parryWindowActive;
        private float _parryWindowTimer;
        private float _parryWindowDuration;
        private EnemyAction _currentAttack;
        private EnemyCombatant _currentAttacker;
        private bool _parrySucceeded;

        private GameConfig _gameConfig;
        private int _currentFloor;

        /// <summary>Whether a parry window is currently open and accepting input.</summary>
        public bool IsParryWindowActive => _parryWindowActive && _parryWindowTimer > 0f;

        /// <summary>Remaining time on the current parry window (0 if inactive).</summary>
        public float ParryWindowTimeRemaining => _parryWindowActive ? Mathf.Max(0f, _parryWindowTimer) : 0f;

        /// <summary>Total duration of the current parry window.</summary>
        public float ParryWindowDuration => _parryWindowDuration;

        /// <summary>The enemy action being parried (valid while window is active).</summary>
        public EnemyAction CurrentAttack => _currentAttack;

        /// <summary>The enemy combatant attacking (valid while window is active).</summary>
        public EnemyCombatant CurrentAttacker => _currentAttacker;

        /// <summary>Whether the current parry window was resolved by a successful parry.</summary>
        public bool ParrySucceeded => _parrySucceeded;

        /// <summary>
        /// Initialize the parry system with config and floor depth for window scaling.
        /// </summary>
        public void Initialize(GameConfig gameConfig, int currentFloor)
        {
            _gameConfig = gameConfig;
            _currentFloor = currentFloor;
            _parryWindowActive = false;
            _parryWindowTimer = 0f;
            _parrySucceeded = false;
        }

        /// <summary>
        /// Open a parry window for an incoming enemy attack.
        /// Returns false if the attack is unparryable (no window opened).
        /// </summary>
        public bool StartParryWindow(EnemyAction attack, EnemyCombatant attacker)
        {
            _parrySucceeded = false;

            // Unparryable attacks skip the parry window entirely
            if (attack.intentColor == IntentColor.Unparryable)
            {
                Debug.Log("[ParrySystem] Attack is Unparryable — no window.");
                return false;
            }

            // Only DealDamage actions have parry windows
            if (attack.actionType != EnemyActionType.DealDamage)
            {
                Debug.Log($"[ParrySystem] Action type is {attack.actionType}, not DealDamage — no window.");
                return false;
            }

            _currentAttack = attack;
            _currentAttacker = attacker;
            _parryWindowDuration = CalculateParryWindowDuration(attacker);
            _parryWindowTimer = _parryWindowDuration;
            _parryWindowActive = true;

            Debug.Log($"[ParrySystem] Parry window OPENED — duration: {_parryWindowDuration:F2}s, intent: {attack.intentColor}");
            return true;
        }

        /// <summary>
        /// Attempt to parry the current attack with a Defense card.
        /// Returns true if the parry succeeds (card matches and window is active).
        /// On success the card should be moved to discard at no OT cost by the caller.
        /// </summary>
        public bool TryParry(CardInstance card)
        {
            if (!IsParryWindowActive)
            {
                Debug.Log("[ParrySystem] TryParry failed — window not active.");
                return false;
            }

            if (card == null || card.Data == null)
            {
                Debug.Log("[ParrySystem] TryParry failed — card is null.");
                return false;
            }

            // Only Defense cards can parry
            if (card.Data.cardType != CardType.Defense)
            {
                Debug.Log($"[ParrySystem] TryParry failed — card type is {card.Data.cardType}, not Defense.");
                return false;
            }

            // Check parry match tags
            if (!IsParryMatch(card.Data, _currentAttack))
            {
                Debug.Log("[ParrySystem] TryParry failed — tags don't match.");
                return false;
            }

            // Parry succeeds — close window
            _parrySucceeded = true;
            _parryWindowActive = false;
            _parryWindowTimer = 0f;

            Debug.Log($"[ParrySystem] PARRY SUCCESS with card: {card.Data.cardName}");

            // Raise parry event
            if (BattleEventBus.Instance != null)
            {
                BattleEventBus.Instance.Raise(new ParryEvent
                {
                    Player = null,
                    Enemy = _currentAttacker != null ? _currentAttacker.gameObject : null,
                    DefenseCard = card.Data,
                    Success = true
                });
            }

            return true;
        }

        /// <summary>
        /// Get all Defense cards in the hand that match the current attack's parry criteria.
        /// Returns empty list if no parry window is active.
        /// </summary>
        public List<CardInstance> GetMatchingCards(IReadOnlyList<CardInstance> handCards)
        {
            var matching = new List<CardInstance>();

            if (!IsParryWindowActive || handCards == null)
                return matching;

            foreach (CardInstance card in handCards)
            {
                if (card == null || card.Data == null)
                    continue;

                if (card.Data.cardType != CardType.Defense)
                    continue;

                if (IsParryMatch(card.Data, _currentAttack))
                    matching.Add(card);
            }

            return matching;
        }

        /// <summary>
        /// Close the parry window (called when time expires or manually).
        /// </summary>
        public void CloseParryWindow()
        {
            _parryWindowActive = false;
            _parryWindowTimer = 0f;
        }

        /// <summary>
        /// Evaluate whether an enemy parries a player's Attack card.
        /// Returns true if the enemy successfully parries (cancels the attack damage).
        /// </summary>
        public bool EvaluateEnemyParry(EnemyCombatant enemy)
        {
            if (enemy == null || enemy.Data == null)
                return false;

            float parryChance = enemy.Data.enemyParryChance;
            if (parryChance <= 0f)
                return false;

            return Random.value < parryChance;
        }

        /// <summary>
        /// Tick the parry window timer. Call from Update or coroutine.
        /// Returns true if the window just expired this tick.
        /// </summary>
        public bool TickParryWindow(float deltaTime)
        {
            if (!_parryWindowActive)
                return false;

            _parryWindowTimer -= deltaTime;

            if (_parryWindowTimer <= 0f)
            {
                _parryWindowTimer = 0f;
                _parryWindowActive = false;
                return true; // window just expired
            }

            return false;
        }

        // ── Private helpers ─────────────────────────────────────────────────

        /// <summary>
        /// Check if a Defense card's parry match tags satisfy the attack's requirements.
        /// A match occurs when the card and attack share at least one common parry tag.
        /// If the attack has no parry match tags, any Defense card matches.
        /// If the card has no parry match tags, it cannot match any attack.
        /// </summary>
        private bool IsParryMatch(CardData defenseCard, EnemyAction attack)
        {
            // Attack with no tags defined — any Defense card works
            if (attack.parryMatchTags == null || attack.parryMatchTags.Count == 0)
                return true;

            // Defense card with no tags — treat as generic parry, matches any attack
            if (defenseCard.parryMatchTags == null || defenseCard.parryMatchTags.Count == 0)
                return true;

            // Both have tags — check for at least one shared tag
            foreach (string attackTag in attack.parryMatchTags)
            {
                if (string.IsNullOrEmpty(attackTag))
                    continue;

                foreach (string cardTag in defenseCard.parryMatchTags)
                {
                    if (string.Equals(attackTag, cardTag, System.StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Calculate the parry window duration for a specific enemy and attack.
        /// Public so BattleManager can pass it to the animation system.
        /// </summary>
        public float CalculateWindowDuration(EnemyAction attack, EnemyCombatant enemy)
        {
            if (attack.intentColor == IntentColor.Unparryable || attack.actionType != EnemyActionType.DealDamage)
                return 0f;
            return CalculateParryWindowDuration(enemy);
        }

        /// <summary>
        /// Calculate the parry window duration for a specific enemy, factoring in
        /// the enemy's base duration, game config defaults, and floor-based scaling.
        /// </summary>
        private float CalculateParryWindowDuration(EnemyCombatant enemy)
        {
            // Use enemy-specific base duration if set, otherwise fall back to config default
            float baseDuration = (enemy != null && enemy.Data != null && enemy.Data.baseParryWindowDuration > 0f)
                ? enemy.Data.baseParryWindowDuration
                : (_gameConfig != null ? _gameConfig.baseParryWindowDuration : 1.5f);

            // Scale down with floor depth
            float floorScaling = _gameConfig != null ? _gameConfig.parryWindowFloorScaling : 0.02f;
            float minDuration = _gameConfig != null ? _gameConfig.parryWindowMinDuration : 0.3f;

            float duration = baseDuration - (floorScaling * _currentFloor);
            return Mathf.Max(duration, minDuration);
        }
    }
}
