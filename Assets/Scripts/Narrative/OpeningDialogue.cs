using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace CardBattle
{
    /// <summary>
    /// Opening dialogue cutscene — boss peeks over the cubicle wall and asks overtime question.
    /// Speech appears as floating text above the sprite; choices live in the bottom DialogueBar.
    /// YES → joke credits ending. NO → wipe run and load Explorationscene.
    /// </summary>
    public class OpeningDialogue : MonoBehaviour
    {
        [Header("Boss Sprite")]
        [SerializeField] private CanvasGroup bossPeekGroup;
        [SerializeField] private RectTransform bossCharacter;

        [Header("Floating Speech")]
        [Tooltip("CanvasGroup on SpeechLabel (sibling of BossCharacter inside BossPeekGroup).")]
        [SerializeField] private CanvasGroup speechLabelGroup;
        [SerializeField] private RectTransform speechLabelRect;
        [SerializeField] private TextMeshProUGUI speechText;

        [Header("Sidebar")]
        [Tooltip("Reference to MenuSlideController so dialogue always brings the sidebar back in.")]
        [SerializeField] private MenuSlideController slideController;
        [Tooltip("Reference to HubInfoPanel — the actual system that drives sidebar position via cursor.")]
        [SerializeField] private HubInfoPanel hubInfoPanel;

        [Header("Bottom Decision Bar")]
        [SerializeField] private CanvasGroup dialogueBar;
        [SerializeField] private Button yesButton;
        [SerializeField] private Button noButton;

        [Header("Joke Ending")]
        [SerializeField] private CanvasGroup jokeEndingPanel;
        [SerializeField] private TextMeshProUGUI overtimeText;
        [SerializeField] private TextMeshProUGUI creditsText;
        [SerializeField] private Button backToMenuButton;

        [Header("Position Markers")]
        [Tooltip("Drag in Scene View — boss starts here (hidden behind wall).")]
        [SerializeField] private RectTransform bossHiddenMarker;
        [Tooltip("Drag in Scene View — boss ends here (peeking over wall).")]
        [SerializeField] private RectTransform bossVisibleMarker;

        [Header("Animation")]
        [SerializeField] private float bossRiseDuration = 0.7f;
        [SerializeField] private float speechFadeDuration = 0.35f;
        [SerializeField] private float barFadeDuration = 0.3f;
        [SerializeField] private float creditsScrollSpeed = 30f;
        [SerializeField] private float textDelay = 1f;

        [Header("Content")]
        [SerializeField] private string bossQuestion = "Hey, you gonna work overtime tonight?";
        [SerializeField] [TextArea(5, 15)] private string creditsContent =
            "OVERTIME\n\nA game about never leaving the office.\n\n" +
            "Directed by: The Boss\nProduced by: Corporate\nWritten by: HR Department\n\n" +
            "Jean-Guy worked overtime.\nHe was never seen again.\n\nTHE END";

        private void Start()
        {
            if (bossPeekGroup != null) bossPeekGroup.gameObject.SetActive(false);
            if (jokeEndingPanel != null)
            {
                jokeEndingPanel.gameObject.SetActive(false);
                SetGroup(jokeEndingPanel, 0f, false, false);
            }
            SetGroup(dialogueBar, 0f, false, false);
        }

        /// <summary>Activate and play the boss entrance cutscene.</summary>
        public void Show()
        {
            if (bossPeekGroup == null) return;

            // Stop HubInfoPanel.Update() from cursor-tracking the sidebar away.
            // Snap sidebar visible first, then kill the update loop for the dialogue duration.
            hubInfoPanel?.LockSidebar();
            if (hubInfoPanel != null) hubInfoPanel.enabled = false;
            HubInfoPanel.SidebarLockedGlobal = true;
            slideController?.Lock();

            bossPeekGroup.gameObject.SetActive(true);
            if (jokeEndingPanel != null) jokeEndingPanel.gameObject.SetActive(false);

            SetGroup(speechLabelGroup, 0f, false, false);
            SetGroup(dialogueBar, 0f, false, false);

            if (bossCharacter != null && bossHiddenMarker != null)
                bossCharacter.anchoredPosition = bossHiddenMarker.anchoredPosition;

            if (backToMenuButton != null) backToMenuButton.gameObject.SetActive(false);

            if (yesButton != null) { yesButton.onClick.RemoveListener(OnYesSelected); yesButton.onClick.AddListener(OnYesSelected); yesButton.interactable = true; }
            if (noButton  != null) { noButton.onClick.RemoveListener(OnNoSelected);   noButton.onClick.AddListener(OnNoSelected);   noButton.interactable  = true; }

            StartCoroutine(BossEntrance());
        }

        private IEnumerator BossEntrance()
        {
            yield return new WaitForSeconds(0.3f);

            // Boss rises from hidden → visible position
            if (bossCharacter != null && bossHiddenMarker != null && bossVisibleMarker != null)
            {
                Vector2 from    = bossHiddenMarker.anchoredPosition;
                Vector2 to      = bossVisibleMarker.anchoredPosition;
                float   elapsed = 0f;
                while (elapsed < bossRiseDuration)
                {
                    elapsed += Time.deltaTime;
                    bossCharacter.anchoredPosition = Vector2.Lerp(from, to, Mathf.SmoothStep(0f, 1f, elapsed / bossRiseDuration));
                    yield return null;
                }
                bossCharacter.anchoredPosition = to;
            }

            yield return new WaitForSeconds(0.1f);

            // Floating label — position is set statically in the scene, just fade it in
            if (speechText != null) speechText.text = bossQuestion;
            yield return StartCoroutine(FadeCanvasGroup(speechLabelGroup, 0f, 1f, speechFadeDuration));
            SetGroup(speechLabelGroup, 1f, false, false);

            yield return new WaitForSeconds(0.4f);

            // Bottom decision bar fades in
            SetGroup(dialogueBar, 0f, true, true);
            yield return StartCoroutine(FadeCanvasGroup(dialogueBar, 0f, 1f, barFadeDuration));
            SetGroup(dialogueBar, 1f, true, true);
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
            if (noButton  != null) noButton.interactable  = false;
        }

        /// <summary>YES path: hide boss + bar, fade full-screen joke ending panel in, roll credits.</summary>
        private IEnumerator JokeEndingSequence()
        {
            HubInfoPanel.SidebarLockedGlobal = false;
            if (hubInfoPanel != null) hubInfoPanel.enabled = true;
            slideController?.Unlock();
            hubInfoPanel?.UnlockSidebar();

            yield return StartCoroutine(FadeCanvasGroup(dialogueBar, 1f, 0f, 0.3f));
            SetGroup(dialogueBar, 0f, false, false);

            if (bossPeekGroup != null) bossPeekGroup.gameObject.SetActive(false);

            if (jokeEndingPanel != null)
            {
                jokeEndingPanel.gameObject.SetActive(true);
                SetGroup(jokeEndingPanel, 0f, true, true);
                yield return StartCoroutine(FadeCanvasGroup(jokeEndingPanel, 0f, 1f, 0.6f));
            }

            yield return new WaitForSeconds(textDelay);
            if (overtimeText != null) { overtimeText.gameObject.SetActive(true); overtimeText.text = "YOU WORKED OVERTIME"; }

            yield return new WaitForSeconds(textDelay);
            if (creditsText != null)
            {
                creditsText.text = creditsContent;
                creditsText.gameObject.SetActive(true);
                yield return StartCoroutine(ScrollCredits());
            }

            if (backToMenuButton != null)
            {
                backToMenuButton.gameObject.SetActive(true);
                backToMenuButton.interactable = true;
                backToMenuButton.onClick.RemoveListener(OnBackToMenu);
                backToMenuButton.onClick.AddListener(OnBackToMenu);
            }
        }

        /// <summary>NO path: wipe run, fade everything out, load Explorationscene.</summary>
        private IEnumerator NoSequence()
        {
            HubInfoPanel.SidebarLockedGlobal = false;
            if (hubInfoPanel != null) hubInfoPanel.enabled = true;
            slideController?.Unlock();
            hubInfoPanel?.UnlockSidebar();
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.WipeRun();
                SaveManager.Instance.CurrentRun.currentFloor   = 1;
                SaveManager.Instance.CurrentRun.hasCustomSpawn = false;
            }

            yield return new WaitForSeconds(0.2f);

            yield return StartCoroutine(FadeCanvasGroup(dialogueBar, 1f, 0f, 0.3f));
            SetGroup(dialogueBar, 0f, false, false);

            if (bossPeekGroup != null) bossPeekGroup.gameObject.SetActive(false);

            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.useDefaultSpawn = true;
                SceneLoader.Instance.enemyDefeated   = false;
                SceneLoader.Instance.LoadSceneUI("Explorationscene");
            }
            else
            {
                SceneManager.LoadScene("Explorationscene");
            }
        }

        private void OnBackToMenu()
        {
            if (jokeEndingPanel != null) jokeEndingPanel.gameObject.SetActive(false);
            if (bossPeekGroup   != null) bossPeekGroup.gameObject.SetActive(false);
        }

        private static void SetGroup(CanvasGroup g, float alpha, bool interactable, bool blocksRaycasts)
        {
            if (g == null) return;
            g.alpha          = alpha;
            g.interactable   = interactable;
            g.blocksRaycasts = blocksRaycasts;
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
        {
            if (group == null) yield break;
            float elapsed = 0f;
            group.alpha = from;
            while (elapsed < duration)
            {
                elapsed    += Time.deltaTime;
                group.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            group.alpha = to;
        }

        private IEnumerator ScrollCredits()
        {
            if (creditsText == null) yield break;
            RectTransform rt = creditsText.GetComponent<RectTransform>();
            if (rt == null) yield break;
            Canvas.ForceUpdateCanvases();
            float endY = rt.anchoredPosition.y + creditsText.preferredHeight + 200f;
            while (rt.anchoredPosition.y < endY)
            {
                rt.anchoredPosition += Vector2.up * creditsScrollSpeed * Time.deltaTime;
                yield return null;
            }
        }
    }
}
