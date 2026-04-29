using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace CardBattle
{
    /// <summary>
    /// Death screen — fades in, shows a message, waits for player click, then loads GameOver.
    /// Office-horror tone. Req 30.
    /// </summary>
    public class DeathScreen : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI narrativeText;

        [Header("Message")]
        [SerializeField] private string deathMessage = "You were dragged back to your desk.";

        [Header("Timing")]
        [SerializeField] private float fadeInDuration = 1.5f;
        [SerializeField] private float clickEnableDelay = 1.8f;

        // --- Static run stat cache ---
        public static int LastFloorReached;
        public static int LastEnemiesDefeated;
        public static int LastHoursEarned;
        public static int LastBadReviewsEarned;

        /// <summary>Cache run stats before the run is wiped.</summary>
        public static void CacheRunStats(RunState run)
        {
            if (run == null)
            {
                LastFloorReached    = 0;
                LastEnemiesDefeated = 0;
                LastHoursEarned     = 0;
                LastBadReviewsEarned = 0;
                return;
            }
            LastFloorReached     = run.currentFloor;
            LastEnemiesDefeated  = run.enemiesDefeated;
            LastHoursEarned      = run.hoursEarnedTotal;
            LastBadReviewsEarned = run.badReviewsEarnedTotal;
        }

        private bool _ready;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
            Time.timeScale   = 1f;

            if (narrativeText != null)
                narrativeText.text = deathMessage + "\n\n<size=60%><alpha=#88>Click anywhere to continue.</alpha></size>";

            // Start fully transparent
            if (canvasGroup != null)
            {
                canvasGroup.alpha          = 0f;
                canvasGroup.interactable   = false;
                canvasGroup.blocksRaycasts = false;
            }

            StartCoroutine(FadeIn());
        }

        private IEnumerator FadeIn()
        {
            float elapsed = 0f;

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                if (canvasGroup != null)
                    canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
                yield return null;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha          = 1f;
                canvasGroup.interactable   = true;
                canvasGroup.blocksRaycasts = true;
            }

            // Allow click only after the fade delay
            yield return new WaitForSeconds(Mathf.Max(0f, clickEnableDelay - fadeInDuration));
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
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.LoadSceneMenu("GameOver");
            else
                SceneManager.LoadScene("GameOver");
        }
    }
}
