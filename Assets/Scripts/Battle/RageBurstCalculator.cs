using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// Calculates the Rage Burst damage bonus from overflow points using
    /// piecewise linear interpolation between GDD reference points:
    ///   1 → 20%, 5 → 80%, 10 → 120%, 20 → 140%
    /// Values above 20 are clamped at 140%.
    /// </summary>
    public static class RageBurstCalculator
    {
        private static readonly Vector2[] ReferencePoints = new Vector2[]
        {
            new Vector2(1f, 20f),
            new Vector2(5f, 80f),
            new Vector2(10f, 120f),
            new Vector2(20f, 140f)
        };

        /// <summary>
        /// Returns the bonus damage percentage for the given overflow amount.
        /// E.g., 1 → 20f, 5 → 80f, 10 → 120f, 20 → 140f.
        /// </summary>
        public static float GetBonusPercent(int overflowPoints)
        {
            if (overflowPoints <= 0) return 0f;
            if (overflowPoints >= 20) return 140f;

            // Find the two reference points to interpolate between
            for (int i = 0; i < ReferencePoints.Length - 1; i++)
            {
                Vector2 a = ReferencePoints[i];
                Vector2 b = ReferencePoints[i + 1];

                if (overflowPoints <= b.x)
                {
                    float t = (overflowPoints - a.x) / (b.x - a.x);
                    return Mathf.Lerp(a.y, b.y, t);
                }
            }

            return 140f;
        }

        /// <summary>
        /// Calculate the bonus damage amount from base damage and overflow points.
        /// Returns the integer bonus to add to the base damage.
        /// </summary>
        public static int CalculateBonusDamage(int baseDamage, int overflowPoints)
        {
            float percent = GetBonusPercent(overflowPoints);
            return Mathf.FloorToInt(baseDamage * percent / 100f);
        }

        /// <summary>
        /// Attempt to consume overflow for a Rage Burst. Only Attack cards trigger consumption.
        /// Returns the bonus damage to apply (0 if not an Attack or no overflow).
        /// </summary>
        public static int TryConsume(OverflowBuffer buffer, CardType cardType, int baseDamage)
        {
            if (buffer == null || buffer.Current <= 0) return 0;
            if (cardType != CardType.Attack) return 0;

            int overflow = buffer.ConsumeAll();
            float bonusPercent = GetBonusPercent(overflow);
            int bonusDamage = Mathf.FloorToInt(baseDamage * bonusPercent / 100f);

            if (BattleEventBus.Instance != null)
            {
                BattleEventBus.Instance.Raise(new RageBurstEvent
                {
                    OverflowConsumed = overflow,
                    BonusPercent = bonusPercent,
                    BonusDamage = bonusDamage
                });
            }

            return bonusDamage;
        }
    }
}
