using System.Collections.Generic;
using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// Resolves card effects by switching on CardType and dispatching to type-specific handlers.
    /// After resolution, moves the card from hand to discard and raises CardPlayedEvent.
    /// </summary>
    public class CardEffectResolver : MonoBehaviour
    {
        [SerializeField] private BlockSystem blockSystem;
        [SerializeField] private StatusEffectSystem statusEffectSystem;
        [SerializeField] private OverflowBuffer overflowBuffer;
        [SerializeField] private OvertimeMeter overtimeMeter;
        [SerializeField] private DeckManager deckManager;
        [SerializeField] private HandManager handManager;
        [SerializeField] private BattleEventBus battleEventBus;

        /// <summary>Additive damage bonus from Tool modifiers, applied to Attack cards.</summary>
        private int _damageBonus;

        /// <summary>Additive damage bonus for Technology-themed cards from Computer hub upgrade.</summary>
        private int _techCardDamageBonus;

        /// <summary>Auto-wire from sibling components if serialized fields are empty.</summary>
        private void Awake()
        {
            if (blockSystem == null) blockSystem = GetComponent<BlockSystem>();
            if (statusEffectSystem == null) statusEffectSystem = GetComponent<StatusEffectSystem>();
            if (overflowBuffer == null) overflowBuffer = GetComponent<OverflowBuffer>();
            if (overtimeMeter == null) overtimeMeter = GetComponent<OvertimeMeter>();
            if (deckManager == null) deckManager = GetComponent<DeckManager>();
            if (handManager == null) handManager = GetComponent<HandManager>();
            if (battleEventBus == null) battleEventBus = GetComponent<BattleEventBus>();
        }

        /// <summary>Apply an additive damage bonus from Tool modifiers.</summary>
        public void ApplyDamageBonus(int bonus)
        {
            _damageBonus += bonus;
        }

        /// <summary>Reset all tool modifiers (called at encounter start before applying new ones).</summary>
        public void ResetModifiers()
        {
            _damageBonus = 0;
            _techCardDamageBonus = 0;
        }

        /// <summary>Current total damage bonus from tools.</summary>
        public int DamageBonus => _damageBonus;

        /// <summary>Apply a tech card damage bonus from the Computer hub upgrade.</summary>
        public void ApplyTechCardDamageBonus(int bonus)
        {
            _techCardDamageBonus += bonus;
        }

        /// <summary>Current tech card damage bonus.</summary>
        public int TechCardDamageBonus => _techCardDamageBonus;

        /// <summary>
        /// Resolve a played card's effect, move it from hand to discard, and raise events.
        /// </summary>
        /// <param name="card">The CardInstance being played.</param>
        /// <param name="source">The source GameObject (typically the player).</param>
        /// <param name="target">The primary target GameObject (enemy or self).</param>
        /// <param name="allEnemies">All living enemies in the encounter (for AllEnemies targeting).</param>
        public void Resolve(CardInstance card, GameObject source, GameObject target, List<EnemyCombatant> allEnemies)
        {
            CardData data = card.Data;

            switch (data.cardType)
            {
                case CardType.Attack:
                    ResolveAttack(data, source, target, allEnemies);
                    break;
                case CardType.Defense:
                    ResolveDefense(data);
                    break;
                case CardType.Effect:
                    ResolveEffect(data, target);
                    break;
                case CardType.Utility:
                    ResolveUtility(data, source, target);
                    break;
                case CardType.Special:
                    ResolveSpecial(data, source, allEnemies);
                    break;
            }

            // Move card from hand to discard
            deckManager.Discard(data);
            handManager.RemoveCard(card);

            // Raise CardPlayedEvent
            if (battleEventBus != null)
            {
                battleEventBus.Raise(new CardPlayedEvent
                {
                    Card = data,
                    Source = source,
                    Target = target
                });
            }
        }

        // ── Attack ──────────────────────────────────────────────────────────

        private void ResolveAttack(CardData data, GameObject source, GameObject target, List<EnemyCombatant> allEnemies)
        {
            int baseDamage = data.effectValue + _damageBonus;

            // Computer hub upgrade: +damage to Technology-themed cards
            if (data.isTechnologyThemed && _techCardDamageBonus > 0)
                baseDamage += _techCardDamageBonus;

            // Apply Rage Burst bonus (Attack cards only)
            int rageBurstBonus = RageBurstCalculator.TryConsume(overflowBuffer, data.cardType, baseDamage);
            int totalBase = baseDamage + rageBurstBonus;

            if (data.targetMode == TargetMode.AllEnemies)
            {
                if (allEnemies != null)
                {
                    for (int i = 0; i < allEnemies.Count; i++)
                    {
                        if (allEnemies[i] != null && allEnemies[i].IsAlive)
                            DealDamageToTarget(totalBase, source, allEnemies[i].gameObject);
                    }
                }
            }
            else
            {
                if (target != null)
                    DealDamageToTarget(totalBase, source, target);
            }
        }

        private void DealDamageToTarget(int baseDamage, GameObject source, GameObject target)
        {
            // Apply Bleed bonus from target
            int bleedBonus = statusEffectSystem != null
                ? statusEffectSystem.GetBleedBonus(target)
                : 0;
            int totalDamage = baseDamage + bleedBonus;

            // Absorb through Block first
            int remainingDamage = blockSystem != null
                ? blockSystem.AbsorbDamage(totalDamage, target)
                : totalDamage;

            // Apply remaining damage to HP
            if (remainingDamage > 0)
            {
                Health health = target.GetComponent<Health>();
                if (health != null)
                    health.TakeDamage(remainingDamage);

                // Grant OT when the player takes HP damage (Req 2.5)
                if (overtimeMeter != null && IsPlayerTarget(target))
                {
                    Health playerHealth = target.GetComponent<Health>();
                    if (playerHealth != null)
                        overtimeMeter.GainFromDamage(remainingDamage, playerHealth.maxHealth);
                }
            }

            // Raise DamageEvent
            if (battleEventBus != null)
            {
                battleEventBus.Raise(new DamageEvent
                {
                    Source = source,
                    Target = target,
                    Amount = totalDamage
                });
            }
        }

        /// <summary>Check if the target is the player (has PlayerTargetable or matches BattleManager's player).</summary>
        private bool IsPlayerTarget(GameObject target)
        {
            if (target == null) return false;
            if (target.GetComponent<PlayerTargetable>() != null) return true;
            if (BattleManager.Instance != null && target == BattleManager.Instance.gameObject) return true;
            return target.CompareTag("Player");
        }

        // ── Defense ─────────────────────────────────────────────────────────

        private void ResolveDefense(CardData data)
        {
            // Proactive parry during Play_Phase: OT cost already deducted by BattleManager.TryPlayCard.
            // Card moves to discard (handled by the caller after this method).
            // The card is prepared as a proactive parry but does not guarantee a match
            // against the next enemy attack. (Req 5.3, 6.5)
        }

        // ── Effect ──────────────────────────────────────────────────────────

        private void ResolveEffect(CardData data, GameObject target)
        {
            if (statusEffectSystem != null && target != null)
            {
                statusEffectSystem.Apply(target, new StatusEffectInstance
                {
                    effectId = data.statusEffectId,
                    duration = data.statusDuration,
                    value = data.effectValue
                });
            }
        }

        // ── Utility ─────────────────────────────────────────────────────────

        private void ResolveUtility(CardData data, GameObject source, GameObject target)
        {
            switch (data.utilityEffectType)
            {
                case UtilityEffectType.Draw:
                    ResolveUtilityDraw(data.effectValue);
                    break;
                case UtilityEffectType.Restore:
                    ResolveUtilityRestore(data.effectValue);
                    break;
                case UtilityEffectType.Retrieve:
                    ResolveUtilityRetrieve(data.effectValue);
                    break;
                case UtilityEffectType.Reorder:
                    ResolveUtilityReorder(data.effectValue);
                    break;
                case UtilityEffectType.Heal:
                    ResolveUtilityHeal(data.effectValue, target);
                    break;
            }
        }

        private void ResolveUtilityDraw(int count)
        {
            for (int i = 0; i < count; i++)
            {
                CardData drawn = deckManager.Draw();
                if (drawn != null)
                    handManager.AddCard(drawn);
                else
                    break; // No more cards available
            }
        }

        private void ResolveUtilityRestore(int amount)
        {
            if (overtimeMeter == null || amount <= 0) return;
            overtimeMeter.Restore(amount);
        }

        private void ResolveUtilityRetrieve(int count)
        {
            if (count <= 0) return;

            List<CardData> retrieved = deckManager.RetrieveFromDiscard(count);
            foreach (CardData card in retrieved)
                handManager.AddCard(card);
        }

        private void ResolveUtilityReorder(int count)
        {
            if (count <= 0) return;

            // Peek at the top N cards, then place them back in the same order.
            // Full interactive reorder requires UI — for now the cards are revealed
            // and returned to the top of the draw pile in their original order.
            List<CardData> topCards = deckManager.PeekTop(count);
            Debug.Log($"Reorder: viewing top {topCards.Count} card(s) of draw pile.");
        }

        private void ResolveUtilityHeal(int healAmount, GameObject target)
        {
            if (target == null) return;

            Health health = target.GetComponent<Health>();
            if (health == null) return;

            int actualHeal = Mathf.Min(healAmount, health.maxHealth - health.currentHealth);
            if (actualHeal > 0)
                health.currentHealth += actualHeal;
        }

        // ── Special ─────────────────────────────────────────────────────────

        private void ResolveSpecial(CardData data, GameObject source, List<EnemyCombatant> allEnemies)
        {
            SpecialCardRegistry.Execute(data.specialCardId, new CardEffectContext
            {
                Card = data,
                Source = source,
                Targets = allEnemies,
                Battle = BattleManager.Instance
            });
        }
    }
}
