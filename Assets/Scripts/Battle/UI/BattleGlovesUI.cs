using UnityEngine;
using UnityEngine.UI;

namespace CardBattle
{
    /// <summary>
    /// Displays the blood tint on gloves in the battle scene.
    /// Purely visual — no tooltip, no gameplay effect.
    /// Updates tint in real-time as attack cards are played.
    /// </summary>
    public class BattleGlovesUI : MonoBehaviour
    {
        [SerializeField] private Image gloveImage;
        [SerializeField] private Color baseGloveColor = Color.white;
        [SerializeField] private Color fullBloodColor = new Color(0.8f, 0.05f, 0.05f, 1f);

        /// <summary>
        /// Initialize the glove tint at the start of an encounter.
        /// </summary>
        /// <param name="bloodLevel">Current persistent Blood_Level (0.0–1.0)</param>
        public void Initialize(float bloodLevel)
        {
            if (gloveImage == null)
            {
                Debug.LogWarning("BattleGlovesUI: gloveImage is null. Glove tint will not render.");
                return;
            }

            gloveImage.color = BloodTintCalculator.ComputeTint(bloodLevel, baseGloveColor, fullBloodColor);
        }

        /// <summary>
        /// Refresh the glove tint after a punch (attack card played).
        /// </summary>
        /// <param name="bloodLevel">Updated Blood_Level (0.0–1.0)</param>
        public void Refresh(float bloodLevel)
        {
            if (gloveImage == null) return;

            gloveImage.color = BloodTintCalculator.ComputeTint(bloodLevel, baseGloveColor, fullBloodColor);
        }
    }
}
