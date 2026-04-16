using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// Computes the blood tint color for gloves based on Blood_Level.
    /// Pure static function — no Unity state dependencies.
    /// </summary>
    public static class BloodTintCalculator
    {
        /// <summary>
        /// Compute the blood tint color by linearly interpolating between
        /// a base glove color and a full-blood color based on Blood_Level.
        /// </summary>
        /// <param name="bloodLevel">Accumulated blood level (0.0–1.0)</param>
        /// <param name="baseColor">Glove color at 0 blood (white)</param>
        /// <param name="fullBloodColor">Glove color at max blood (deep red)</param>
        /// <returns>Linearly interpolated tint color</returns>
        public static Color ComputeTint(float bloodLevel, Color baseColor, Color fullBloodColor)
        {
            float t = Mathf.Clamp01(bloodLevel);
            return Color.Lerp(baseColor, fullBloodColor, t);
        }
    }
}
