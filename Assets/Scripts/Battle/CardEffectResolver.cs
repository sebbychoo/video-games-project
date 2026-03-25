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
                    ResolveDefense(data, target);
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
            int baseDamage = data.effectValue;

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

        private void ResolveDefense(CardData data, GameObject target)
        {
            // Defense cards add Block to whatever entity is targeted (Req 4.5)
            if (blockSystem != null && target != null)
                blockSystem.AddBlock(data.blockValue, target);
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
                    // Placeholder: retrieve cards from discard to hand
                    Debug.Log($"Retrieve effect: would return {data.effectValue} card(s) from discard to hand.");
                    break;
                case UtilityEffectType.Reorder:
                    // Placeholder: reorder top N cards of draw pile
                    Debug.Log($"Reorder effect: would let player rearrange top {data.effectValue} card(s) of draw pile.");
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

            // GainFromDamage(hpLost, maxHP) calculates gain = floor(hpLost / maxHP * 10).
            // To add exactly 'amount' OT points: GainFromDamage(amount * 10, 100)
            // => floor(amount * 10 / 100 * 10) = amount.
            // Excess beyond max is automatically routed to OverflowBuffer by OvertimeMeter.
            overtimeMeter.GainFromDamage(amount * 10, 100);
        }

        private void ResolveUtilityHeal(int healAmount, GameObject target)
        {
            if (target == null) return;

            Health health = target.GetComponent<Health>();
            if (health == null) return;

            int actualHeal = Mathf.Min(healAmount, health.maxHealth - health.currentHealth);
            if (actualHeal > 0)
            {
                // Health.TakeDamage subtracts, so we add by using negative... 
                // Actually Health doesn't have a Heal method. We modify currentHealth directly.
                health.currentHealth += actualHeal;
            }
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
