using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// Computes blood accumulation per punch using an exponential curve,
    /// and applies the ratchet + cap logic.
    /// Pure static functions — no Unity state dependencies.
    /// </summary>
    public static class BloodAccumulationCalculator
    {
        /// <summary>
        /// Compute the blood increment for a single punch based on the exponential curve.
        /// Early punches produce barely any blood; later punches produce much more.
        /// </summary>
        /// <param name="punchCount">Number of attack cards played so far in this encounter (1-based)</param>
        /// <param name="bloodMultiplier">Encounter multiplier (1.0 for regular, higher for bosses)</param>
        /// <param name="baseIncrement">Base blood per punch at the start of the curve (e.g., 0.005)</param>
        /// <param name="growthRate">Exponential growth rate (e.g., 0.15)</param>
        /// <returns>Blood increment for this punch, always >= 0</returns>
        public static float ComputeIncrement(int punchCount, float bloodMultiplier,
                                              float baseIncrement, float growthRate)
        {
            if (punchCount <= 0) return 0f;
            float increment = baseIncrement * Mathf.Exp(growthRate * (punchCount - 1)) * bloodMultiplier;
            return Mathf.Max(0f, increment);
        }

        /// <summary>
        /// Compute the new Blood_Level after a punch, enforcing the ratchet (never decreases)
        /// and the 1.0 cap (fully red hands).
        /// </summary>
        /// <param name="currentBloodLevel">Current persistent Blood_Level (0.0–1.0)</param>
        /// <param name="increment">Blood increment from ComputeIncrement</param>
        /// <returns>New Blood_Level, clamped to [currentBloodLevel, 1.0]</returns>
        public static float ApplyIncrement(float currentBloodLevel, float increment)
        {
            float clamped = Mathf.Clamp01(currentBloodLevel);
            return Mathf.Clamp(clamped + Mathf.Max(0f, increment), clamped, 1f);
        }
    }
}
