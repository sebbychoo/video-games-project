using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// Renders blood tint on the player's glove sprites during exploration.
    /// Reads persistentBloodLevel from RunState on Start and applies tint
    /// via BloodTintCalculator. Static during exploration — no updates after Start.
    /// Requirements: 8.1, 8.3, 8.4, 11.1, 12.1, 12.4
    /// </summary>
    public class ExplorationGlovesController : MonoBehaviour
    {
        [SerializeField] SpriteRenderer leftGloveRenderer;
        [SerializeField] SpriteRenderer rightGloveRenderer;
        [SerializeField] Color baseGloveColor = Color.white;
        [SerializeField] Color fullBloodColor = new Color(0.8f, 0.05f, 0.05f, 1f);

        private void Start()
        {
            float bloodLevel = 0f;

            if (SaveManager.Instance != null && SaveManager.Instance.CurrentRun != null)
            {
                bloodLevel = SaveManager.Instance.CurrentRun.persistentBloodLevel;
                Debug.Log($"[BloodDebug] ExplorationGlovesController.Start: persistentBloodLevel = {bloodLevel}");
            }
            else
            {
                Debug.LogWarning("ExplorationGlovesController: SaveManager or CurrentRun is null. Defaulting bloodLevel to 0.");
            }

            ApplyBloodTint(bloodLevel);
        }

        /// <summary>
        /// Apply blood tint to both glove sprite renderers.
        /// Clamps invalid values and logs warnings.
        /// </summary>
        public void ApplyBloodTint(float bloodLevel)
        {
            if (float.IsNaN(bloodLevel) || bloodLevel < 0f)
            {
                Debug.LogWarning($"ExplorationGlovesController: Invalid bloodLevel ({bloodLevel}), clamping to 0.");
                bloodLevel = 0f;
            }
            else if (bloodLevel > 1f)
            {
                Debug.LogWarning($"ExplorationGlovesController: bloodLevel ({bloodLevel}) exceeds 1.0, clamping to 1.0.");
                bloodLevel = 1f;
            }

            Color tint = BloodTintCalculator.ComputeTint(bloodLevel, baseGloveColor, fullBloodColor);
            Debug.Log($"[BloodDebug] ApplyBloodTint: bloodLevel={bloodLevel}, baseColor={baseGloveColor}, fullBloodColor={fullBloodColor}, result={tint}");

            if (leftGloveRenderer != null)
            {
                leftGloveRenderer.color = tint;
            }
            else
            {
                Debug.LogWarning("ExplorationGlovesController: leftGloveRenderer is null, skipping.");
            }

            if (rightGloveRenderer != null)
            {
                rightGloveRenderer.color = tint;
            }
            else
            {
                Debug.LogWarning("ExplorationGlovesController: rightGloveRenderer is null, skipping.");
            }
        }
    }
}
