using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// Stationary boss entity for the exploration phase. Renders the boss
    /// sitting in a chair or standing based on <see cref="EnemyCombatantData.bossPose"/>.
    /// Applies Y-axis billboard rotation so the sprite always faces the player camera.
    /// No patrol, no wander, no chase — the boss stays at its fixed position.
    ///
    /// Requirements: 3.1, 3.2, 3.4, 7.2, 7.3, 7.4
    /// </summary>
    public class BossExplorationEntity : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpriteRenderer bossSprite;
        [SerializeField] private SpriteRenderer chairSprite;

        /// <summary>Boss data set by LevelGenerator when spawning the boss.</summary>
        public EnemyCombatantData BossData { get; set; }

        private Camera _mainCamera;

        private void Start()
        {
            _mainCamera = Camera.main;
            ApplyPose();
        }

        private void Update()
        {
            // Y-axis billboard rotation to face the player camera (Req 7.4)
            if (_mainCamera == null) return;

            Vector3 camPos = _mainCamera.transform.position;
            Vector3 direction = camPos - transform.position;
            direction.y = 0f; // lock to Y-axis only

            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        /// <summary>
        /// Configures the visual representation based on boss pose.
        /// Sitting renders chair + boss sprite; standing renders boss sprite only.
        /// </summary>
        private void ApplyPose()
        {
            if (BossData == null) return;

            if (bossSprite != null && BossData.sprite != null)
            {
                bossSprite.sprite = BossData.sprite;
            }

            bool sitting = BossData.bossPose == BossPose.Sitting;

            if (chairSprite != null)
            {
                chairSprite.gameObject.SetActive(sitting); // Req 7.2, 7.3
            }
        }
    }
}
