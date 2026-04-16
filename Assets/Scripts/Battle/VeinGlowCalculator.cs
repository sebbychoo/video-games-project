using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// Computes the vein glow color/intensity based on OT (Overtime) values.
    /// Pure static functions — no Unity state dependencies.
    /// </summary>
    public static class VeinGlowCalculator
    {
        /// <summary>
        /// Compute the vein glow color based on current OT, max OT, and overflow.
        /// Overflow intensifies the glow beyond the normal bright color.
        /// </summary>
        /// <param name="currentOT">Current Overtime meter value</param>
        /// <param name="maxOT">Maximum Overtime capacity (must be > 0)</param>
        /// <param name="overflowOT">Overflow buffer points (>= 0)</param>
        /// <param name="dimColor">Vein color at minimum glow</param>
        /// <param name="brightColor">Vein color at full glow</param>
        /// <returns>Interpolated glow color, intensified beyond brightColor for overflow</returns>
        public static Color ComputeGlow(int currentOT, int maxOT, int overflowOT,
                                         Color dimColor, Color brightColor)
        {
            if (maxOT <= 0) return dimColor;

            float ratio = (float)(currentOT + overflowOT) / maxOT;
            float t = Mathf.Clamp01(ratio);
            Color glow = Color.Lerp(dimColor, brightColor, t);

            if (ratio > 1f)
            {
                float excess = Mathf.Clamp01(ratio - 1f);
                glow = Color.Lerp(glow, new Color(0.4f, 0.8f, 1f, 1f), excess * 0.5f);
            }

            return glow;
        }

        /// <summary>
        /// Compute glow from a pre-stored OT value (for exploration scene).
        /// Equivalent to ComputeGlow with 0 overflow.
        /// </summary>
        /// <param name="storedOT">Persisted OT value from last battle</param>
        /// <param name="maxOT">Maximum Overtime capacity</param>
        /// <param name="dimColor">Vein color at minimum glow</param>
        /// <param name="brightColor">Vein color at full glow</param>
        /// <returns>Interpolated glow color</returns>
        public static Color ComputeGlowFromStored(int storedOT, int maxOT,
                                                    Color dimColor, Color brightColor)
        {
            return ComputeGlow(storedOT, maxOT, 0, dimColor, brightColor);
        }
    }
}
