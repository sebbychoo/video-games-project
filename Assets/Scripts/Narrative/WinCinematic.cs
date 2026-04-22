using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace CardBattle
{
    /// <summary>
    /// Manages the win cinematic after defeating the final boss.
    /// A quiet, understated sequence: Jean-Guy puts down his stapler,
    /// walks out, arrives home, sees his family. No dialogue, no fanfare.
    /// Returns to Main Menu after the sequence completes.
    ///
    /// Loaded as a scene-based cinematic after the final boss victory.
    /// </summary>
    public class WinCinematic : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private CanvasGroup screenCanvasGroup;
        [SerializeField] private CanvasGroup textCanvasGroup;
        [SerializeField] private TextMeshProUGUI narrativeText;
        [SerializeField] private CanvasGroup endCanvasGroup;
        [SerializeField] private TextMeshProUGUI endText;

        [Header("Timing")]
        [SerializeField] private float screenFadeInDuration = 2f;
        [SerializeField] private float textFadeInDuration = 1.2f;
        [SerializeField] private float textHoldDuration = 2.5f;
        [SerializeField] private float textFadeOutDuration = 1f;
        [SerializeField] private float linePauseDuration = 0.8f;
        [SerializeField] private float endFadeInDuration = 1.5f;
        [SerializeField] private float endHoldDuration = 3f;
        [SerializeField] private float finalFadeOutDuration = 2f;
        [SerializeField] private float menuLoadDelay = 1f;

        [Header("Narrative Lines")]
        [SerializeField] private string[] narrativeLines = new string[]
        {
            "Jean-Guy puts down his stapler.",
            "He walks to the door.",
            "The night air is cool.",
            "He arrives home.",
            "His family is waiting.",
            "..."
        };

        [Header("End Text")]
        [SerializeField] private string endMessage = "THE END";

        private void Start()
        {
            // Ensure cursor is visible during cinematic (Req 31.4)
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Start fully transparent
            if (screenCanvasGroup != null)
            {
                screenCanvasGroup.alpha = 0f;
                screenCanvasGroup.interactable = false;
                screenCanvasGroup.blocksRaycasts = true;
            }
            if (textCanvasGroup != null)
                textCanvasGroup.alpha = 0f;
            if (endCanvasGroup != null)
                endCanvasGroup.alpha = 0f;

            if (narrativeText != null)
                narrativeText.text = "";
            if (endText != null)
                endText.text = "";

            StartCoroutine(WinSequence());
        }

        private IEnumerator WinSequence()
        {
            // Fade in from black (Req 31.3, 31.4)
            yield return StartCoroutine(FadeCanvasGroup(screenCanvasGroup, 0f, 1f, screenFadeInDuration));

            // Play each narrative line — quiet and understated
            if (narrativeLines != null)
            {
                for (int i = 0; i < narrativeLines.Length; i++)
                {
                    if (narrativeText != null)
                        narrativeText.text = narrativeLines[i];

                    // Fade text in
                    yield return StartCoroutine(FadeCanvasGroup(textCanvasGroup, 0f, 1f, textFadeInDuration));

                    // Hold
                    yield return new WaitForSeconds(textHoldDuration);

                    // Fade text out
                    yield return StartCoroutine(FadeCanvasGroup(textCanvasGroup, 1f, 0f, textFadeOutDuration));

                    // Pause between lines (skip after last line)
                    if (i < narrativeLines.Length - 1)
                        yield return new WaitForSeconds(linePauseDuration);
                }
            }

            // Brief pause before "THE END"
            yield return new WaitForSeconds(linePauseDuration);

            // Show "THE END"
            if (endText != null)
                endText.text = endMessage;

            yield return StartCoroutine(FadeCanvasGroup(endCanvasGroup, 0f, 1f, endFadeInDuration));

            // Hold "THE END"
            yield return new WaitForSeconds(endHoldDuration);

            // Fade everything to black
            yield return StartCoroutine(FadeCanvasGroup(endCanvasGroup, 1f, 0f, finalFadeOutDuration));

            // Pause before loading menu
            yield return new WaitForSeconds(menuLoadDelay);

            // Return to Main Menu (Req 31.5)
            LoadMainMenu();
        }

        private void LoadMainMenu()
        {
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.LoadSceneMenu("Menu");
            else
                SceneManager.LoadScene("Menu");
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
        {
            if (group == null) yield break;

            float elapsed = 0f;
            group.alpha = from;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            group.alpha = to;
        }

        // --- Static Helper ---

        /// <summary>
        /// Returns true if the given floor is the final floor as defined by config.
        /// Used by BattleManager or SceneLoader to detect when to trigger the
        /// win cinematic instead of normal victory flow.
        /// </summary>
        /// <param name="floor">The current floor number.</param>
        /// <param name="config">The GameConfig asset (uses config.finalFloor).</param>
        /// <returns>True if floor equals the configured final floor.</returns>
        public static bool IsFinalFloor(int floor, GameConfig config)
        {
            if (config == null)
                return floor == 75;

            return floor == config.finalFloor;
        }
    }
}
