using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// Renders vein glow on the player's wrist sprites during exploration.
    /// Veins are invisible at rest and pulse in/out like a heartbeat,
    /// with intensity driven by the stored OT level.
    /// Requirements: 4.5, 5.2, 8.2, 8.3, 11.3, 12.1
    /// </summary>
    public class ExplorationVeinsController : MonoBehaviour
    {
        [SerializeField] SpriteRenderer leftVeinRenderer;
        [SerializeField] SpriteRenderer rightVeinRenderer;
        [SerializeField] Color dimGlowColor = new Color(0.1f, 0.2f, 0.4f, 0.3f);
        [SerializeField] Color brightGlowColor = new Color(0.3f, 0.7f, 1f, 1f);

        [Header("Pulse Settings")]
        [Tooltip("Pulses per second. Higher = faster heartbeat.")]
        [SerializeField] float pulseFrequency = 1.2f;
        [Tooltip("Minimum alpha during pulse (0 = fully invisible at rest).")]
        [SerializeField] float pulseMinAlpha = 0f;
        [Tooltip("Maximum alpha at peak pulse (scaled by OT intensity).")]
        [SerializeField] float pulseMaxAlpha = 1f;

        private Color _baseGlow;
        private float _intensity; // 0-1+ based on OT ratio

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

        private void Update()
        {
            // Heartbeat pulse: sine wave that goes from 0 to 1 and back
            // Using abs(sin) gives a smooth pulse that peaks twice per cycle
            float pulse = Mathf.Abs(Mathf.Sin(Time.time * pulseFrequency * Mathf.PI));
            float alpha = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha * _intensity, pulse);

            Color c = _baseGlow;
            c.a = alpha;

            if (leftVeinRenderer != null)
                leftVeinRenderer.color = c;
            if (rightVeinRenderer != null)
                rightVeinRenderer.color = c;
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

            _baseGlow = VeinGlowCalculator.ComputeGlowFromStored(storedOT, maxOT, dimGlowColor, brightGlowColor);
            _intensity = maxOT > 0 ? (float)storedOT / maxOT : 0f;

            // Set initial state to invisible — Update() handles the pulse
            Color c = _baseGlow;
            c.a = 0f;

            if (leftVeinRenderer != null)
            {
                leftVeinRenderer.color = c;
            }
            else
            {
                Debug.LogWarning("ExplorationVeinsController: leftVeinRenderer is null, skipping.");
            }

            if (rightVeinRenderer != null)
            {
                rightVeinRenderer.color = c;
            }
            else
            {
                Debug.LogWarning("ExplorationVeinsController: rightVeinRenderer is null, skipping.");
            }
        }
    }
}
