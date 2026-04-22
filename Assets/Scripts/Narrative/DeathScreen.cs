using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace CardBattle
{
    /// <summary>
    /// Death screen — shows a message, waits for player click, then loads GameOver.
    /// </summary>
    public class DeathScreen : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TextMeshProUGUI narrativeText;

        [Header("Message")]
        [SerializeField] private string deathMessage = "You were dragged back to your desk.";

        // --- Static run stat cache ---
        public static int LastFloorReached;
        public static int LastEnemiesDefeated;
        public static int LastHoursEarned;
        public static int LastBadReviewsEarned;

        public static void CacheRunStats(RunState run)
        {
            if (run == null)
            {
                LastFloorReached = 0;
                LastEnemiesDefeated = 0;
                LastHoursEarned = 0;
                LastBadReviewsEarned = 0;
                return;
            }
            LastFloorReached = run.currentFloor;
            LastEnemiesDefeated = run.enemiesDefeated;
            LastHoursEarned = run.hoursEarnedTotal;
            LastBadReviewsEarned = run.badReviewsEarnedTotal;
        }

        private bool _ready;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 1f;

            if (narrativeText != null)
                narrativeText.text = deathMessage + "\n\n<size=60%>Click anywhere to continue.</size>";

            // Small delay so the player doesn't accidentally skip instantly
            Invoke(nameof(EnableClick), 0.5f);
        }

        private void EnableClick()
        {
            _ready = true;
        }

        private void Update()
        {
            if (!_ready) return;

            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                _ready = false;
                LoadGameOver();
            }
        }

        private void LoadGameOver()
        {
            Debug.Log("[DeathScreen] Loading GameOver scene.");

            if (SceneLoader.Instance != null)
                SceneLoader.Instance.LoadSceneMenu("GameOver");
            else
                SceneManager.LoadScene("GameOver");
        }
    }
}
