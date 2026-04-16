using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// Renders vein glow on the player's wrist sprites during exploration.
    /// Reads persistentOTLevel from RunState and overtimeMaxCapacity from GameConfig
    /// on Start, applies glow via VeinGlowCalculator. Always static during exploration —
    /// OT is strictly a battle resource, no updates after Start.
    /// Requirements: 4.5, 5.2, 8.2, 8.3, 11.3, 12.1
    /// </summary>
    public class ExplorationVeinsController : MonoBehaviour
    {
        [SerializeField] SpriteRenderer leftVeinRenderer;
        [SerializeField] SpriteRenderer rightVeinRenderer;
        [SerializeField] Color dimGlowColor = new Color(0.1f, 0.2f, 0.4f, 0.3f);
        [SerializeField] Color brightGlowColor = new Color(0.3f, 0.7f, 1f, 1f);

        private void Start()
        {
            int storedOT = 10;
            int maxOT = 10;

            if (SaveManager.Instance != null && SaveManager.Instance.CurrentRun != null)
            {
                storedOT = SaveManager.Instance.CurrentRun.persistentOTLevel;
            }
            else
            {
                Debug.LogWarning("ExplorationVeinsController: SaveManager or CurrentRun is null. Defaulting storedOT to 10.");
            }

            GameConfig config = Resources.Load<GameConfig>("GameConfig");
            if (config != null)
            {
                maxOT = config.overtimeMaxCapacity;
            }
            else
            {
                Debug.LogWarning("ExplorationVeinsController: GameConfig not found in Resources. Defaulting maxOT to 10.");
            }

            ApplyVeinGlow(storedOT, maxOT);
        }

        /// <summary>
        /// Apply vein glow to both wrist sprite renderers.
        /// Clamps negative OT to 0 and logs warnings.
        /// </summary>
        public void ApplyVeinGlow(int storedOT, int maxOT)
        {
            if (storedOT < 0)
            {
                Debug.LogWarning($"ExplorationVeinsController: Negative storedOT ({storedOT}), clamping to 0.");
                storedOT = 0;
            }

            Color glow = VeinGlowCalculator.ComputeGlowFromStored(storedOT, maxOT, dimGlowColor, brightGlowColor);

            if (leftVeinRenderer != null)
            {
                leftVeinRenderer.color = glow;
            }
            else
            {
                Debug.LogWarning("ExplorationVeinsController: leftVeinRenderer is null, skipping.");
            }

            if (rightVeinRenderer != null)
            {
                rightVeinRenderer.color = glow;
            }
            else
            {
                Debug.LogWarning("ExplorationVeinsController: rightVeinRenderer is null, skipping.");
            }
        }
    }
}
