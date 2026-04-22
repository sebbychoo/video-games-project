using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace CardBattle
{
    /// <summary>
    /// Displays run stats after the death sequence completes, then offers
    /// New Run or Main Menu options. Stats are read from DeathScreen static
    /// fields which are cached before the run is wiped.
    /// </summary>
    public class GameOverScreen : MonoBehaviour
    {
        [Header("Stats Panel")]
        [SerializeField] private CanvasGroup statsPanel;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI floorText;
        [SerializeField] private TextMeshProUGUI enemiesText;
        [SerializeField] private TextMeshProUGUI hoursText;
        [SerializeField] private TextMeshProUGUI badReviewsText;

        [Header("Buttons")]
        [SerializeField] private Button newRunButton;
        [SerializeField] private Button mainMenuButton;

        [Header("Timing")]
        [SerializeField] private float fadeInDuration = 1f;

        private void Start()
        {
            // Ensure cursor is visible for menu interaction
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Read cached stats from DeathScreen static fields
            if (titleText != null)
                titleText.text = "GAME OVER";

            if (floorText != null)
                floorText.text = $"Floor Reached: {DeathScreen.LastFloorReached}";

            if (enemiesText != null)
                enemiesText.text = $"Enemies Defeated: {DeathScreen.LastEnemiesDefeated}";

            if (hoursText != null)
                hoursText.text = $"Hours Earned: {DeathScreen.LastHoursEarned}";

            if (badReviewsText != null)
                badReviewsText.text = $"Bad Reviews Earned: {DeathScreen.LastBadReviewsEarned}";

            // Wire buttons
            if (newRunButton != null)
                newRunButton.onClick.AddListener(OnNewRun);

            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(OnMainMenu);

            // Fade in the stats panel
            if (statsPanel != null)
            {
                statsPanel.alpha = 0f;
                statsPanel.interactable = false;
                statsPanel.blocksRaycasts = false;
                StartCoroutine(FadeIn());
            }
        }

        private void OnNewRun()
        {
            // Wipe run state for a fresh start
            if (SaveManager.Instance != null)
                SaveManager.Instance.WipeRun();

            // Load Explorationscene — RunStartController handles deck selection
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.useDefaultSpawn = true;
                SceneLoader.Instance.enemyDefeated = false;
                SceneLoader.Instance.LoadSceneUI("Explorationscene");
            }
            else
            {
                SceneManager.LoadScene("Explorationscene");
            }
        }

        private void OnMainMenu()
        {
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.LoadSceneMenu("Menu");
            else
                SceneManager.LoadScene("Menu");
        }

        private IEnumerator FadeIn()
        {
            float elapsed = 0f;

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                statsPanel.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
                yield return null;
            }

            statsPanel.alpha = 1f;
            statsPanel.interactable = true;
            statsPanel.blocksRaycasts = true;
        }
    }
}
