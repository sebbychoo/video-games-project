using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace CardBattle
{
    /// <summary>
    /// Manages the opening dialogue sequence when a new run starts.
    /// Boss leans over cubicle and asks "Hey, you gonna work overtime tonight?"
    /// YES → joke ending (fade to black, credits, back to menu).
    /// NO  → Jean-Guy stands up, proceed to deck selection via Explorationscene.
    /// Skipped entirely on saved mid-run load.
    /// </summary>
    public class OpeningDialogue : MonoBehaviour
    {
        [Header("Dialogue Panel")]
        [SerializeField] private CanvasGroup dialoguePanel;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private Button yesButton;
        [SerializeField] private Button noButton;

        [Header("Joke Ending Panel")]
        [SerializeField] private CanvasGroup jokeEndingPanel;
        [SerializeField] private Image blackOverlay;
        [SerializeField] private TextMeshProUGUI overtimeText;
        [SerializeField] private TextMeshProUGUI creditsText;
        [SerializeField] private Button backToMenuButton;

        [Header("Timing")]
        [SerializeField] private float fadeDuration = 1f;
        [SerializeField] private float textDelay = 1f;
        [SerializeField] private float creditsScrollSpeed = 30f;

        [Header("Dialogue Content")]
        [SerializeField] private string bossQuestion = "Hey, you gonna work overtime tonight?";

        [Header("Credits Content")]
        [SerializeField] [TextArea(5, 15)] private string creditsContent =
            "OVERTIME\n\nA game about never leaving the office.\n\n" +
            "Directed by: The Boss\n" +
            "Produced by: Corporate\n" +
            "Written by: HR Department\n\n" +
            "Jean-Guy worked overtime.\nHe was never seen again.\n\n" +
            "THE END";

        private void Start()
        {
            // Ensure cursor is visible for dialogue interaction
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Hide joke ending panel initially
            if (jokeEndingPanel != null)
            {
                jokeEndingPanel.alpha = 0f;
                jokeEndingPanel.interactable = false;
                jokeEndingPanel.blocksRaycasts = false;
            }

            if (backToMenuButton != null)
                backToMenuButton.gameObject.SetActive(false);

            // Check if we should skip the dialogue (mid-run load)
            if (ShouldSkipDialogue())
            {
                SkipToExploration();
                return;
            }

            // Show dialogue
            ShowDialogue();
        }

        private bool ShouldSkipDialogue()
        {
            if (SaveManager.Instance == null)
                return false;

            RunState run = SaveManager.Instance.CurrentRun;
            return run != null && run.isActive;
        }

        private void SkipToExploration()
        {
            // Hide dialogue UI
            if (dialoguePanel != null)
            {
                dialoguePanel.alpha = 0f;
                dialoguePanel.interactable = false;
                dialoguePanel.blocksRaycasts = false;
            }

            LoadExplorationScene();
        }

        private void ShowDialogue()
        {
            if (dialoguePanel != null)
            {
                dialoguePanel.alpha = 1f;
                dialoguePanel.interactable = true;
                dialoguePanel.blocksRaycasts = true;
            }

            if (dialogueText != null)
                dialogueText.text = bossQuestion;

            // Wire up buttons
            if (yesButton != null)
                yesButton.onClick.AddListener(OnYesSelected);

            if (noButton != null)
                noButton.onClick.AddListener(OnNoSelected);
        }

        private void OnYesSelected()
        {
            DisableButtons();
            StartCoroutine(JokeEndingSequence());
        }

        private void OnNoSelected()
        {
            DisableButtons();
            StartCoroutine(NoSequence());
        }

        private void DisableButtons()
        {
            if (yesButton != null) yesButton.interactable = false;
            if (noButton != null) noButton.interactable = false;
        }

        /// <summary>
        /// YES path: fade to black, show "YOU WORKED OVERTIME", roll credits, show Back to Menu.
        /// </summary>
        private IEnumerator JokeEndingSequence()
        {
            // Fade dialogue panel out
            yield return StartCoroutine(FadeCanvasGroup(dialoguePanel, 1f, 0f, fadeDuration));
            if (dialoguePanel != null)
            {
                dialoguePanel.interactable = false;
                dialoguePanel.blocksRaycasts = false;
            }

            // Fade joke ending panel in (black overlay)
            if (jokeEndingPanel != null)
            {
                jokeEndingPanel.interactable = true;
                jokeEndingPanel.blocksRaycasts = true;
            }
            yield return StartCoroutine(FadeCanvasGroup(jokeEndingPanel, 0f, 1f, fadeDuration));

            // Show "YOU WORKED OVERTIME" text after a delay
            yield return new WaitForSeconds(textDelay);
            if (overtimeText != null)
            {
                overtimeText.text = "YOU WORKED OVERTIME";
                overtimeText.gameObject.SetActive(true);
            }

            // Wait, then show credits
            yield return new WaitForSeconds(textDelay);
            if (creditsText != null)
            {
                creditsText.text = creditsContent;
                creditsText.gameObject.SetActive(true);
                yield return StartCoroutine(ScrollCredits());
            }

            // Show Back to Menu button
            if (backToMenuButton != null)
            {
                backToMenuButton.gameObject.SetActive(true);
                backToMenuButton.interactable = true;
                backToMenuButton.onClick.AddListener(OnBackToMenu);
            }
        }

        /// <summary>
        /// NO path: Jean-Guy stands up, wipe run, proceed to deck selection.
        /// </summary>
        private IEnumerator NoSequence()
        {
            // Fade dialogue out
            yield return StartCoroutine(FadeCanvasGroup(dialoguePanel, 1f, 0f, fadeDuration));
            if (dialoguePanel != null)
            {
                dialoguePanel.interactable = false;
                dialoguePanel.blocksRaycasts = false;
            }

            // Wipe run state for a fresh start
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.WipeRun();
                SaveManager.Instance.CurrentRun.currentFloor = 1;
                SaveManager.Instance.CurrentRun.hasCustomSpawn = false;
            }

            // Small delay for the "standing up" moment
            yield return new WaitForSeconds(0.5f);

            // Load Explorationscene — RunStartController will handle deck selection
            LoadExplorationScene();
        }

        private void OnBackToMenu()
        {
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.LoadSceneMenu("Menu");
            else
                SceneManager.LoadScene("Menu");
        }

        private void LoadExplorationScene()
        {
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

        private IEnumerator ScrollCredits()
        {
            if (creditsText == null) yield break;

            RectTransform rt = creditsText.GetComponent<RectTransform>();
            if (rt == null) yield break;

            // Force layout rebuild so we get accurate preferred height
            Canvas.ForceUpdateCanvases();
            float textHeight = creditsText.preferredHeight;
            float startY = rt.anchoredPosition.y;
            float endY = startY + textHeight + 200f;

            while (rt.anchoredPosition.y < endY)
            {
                rt.anchoredPosition += Vector2.up * creditsScrollSpeed * Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
        {
            if (group == null) yield break;

            float elapsed = 0f;
            group.alpha = from;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                group.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }

            group.alpha = to;
        }
    }
}
