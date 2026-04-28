using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CardBattle
{
    /// <summary>
    /// Displays boss pre-fight and post-fight dialogue during battle.
    /// Pre-fight dialogue is handled by BossCutsceneController in exploration;
    /// this component handles post-fight dialogue shown after boss victory
    /// before the VictoryScreen appears.
    /// 
    /// Dialogue tone varies by floor:
    ///   Floors 1–9:  corporate business language
    ///   Floors 12+:  unnatural, unsettling tone
    /// 
    /// Requirements: 25.2, 25.3, 25.4, 25.5
    /// </summary>
    public class BossDialogueDisplay : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private TextMeshProUGUI dismissHint;
        [SerializeField] private Image backgroundPanel;

        [Header("Timing")]
        [SerializeField] private float fadeInDuration = 0.4f;
        [SerializeField] private float fadeOutDuration = 0.3f;

        /// <summary>Floor threshold where dialogue tone shifts from corporate to unsettling.</summary>
        private const int UnsettlingFloorThreshold = 12;

        private bool _active;
        private bool _dismissing;
        private Action _onDismissed;

        private void Awake()
        {
            // Start hidden
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Show post-fight dialogue. Calls onDismissed when the player dismisses it.
        /// </summary>
        public void ShowPostFightDialogue(string dialogue, int currentFloor, Action onDismissed)
        {
            if (string.IsNullOrWhiteSpace(dialogue))
            {
                onDismissed?.Invoke();
                return;
            }

            _onDismissed = onDismissed;
            _active = true;
            _dismissing = false;

            if (dialogueText != null)
                dialogueText.text = dialogue;

            // Apply tone styling based on floor (Req 25.4, 25.5)
            ApplyToneStyling(currentFloor);

            if (dismissHint != null)
                dismissHint.text = "Click to continue...";

            gameObject.SetActive(true);
            StartCoroutine(FadeIn());
        }

        /// <summary>
        /// Show pre-fight dialogue during battle (if needed as an alternative to
        /// BossCutsceneController). Calls onDismissed when dismissed.
        /// </summary>
        public void ShowPreFightDialogue(string dialogue, int currentFloor, Action onDismissed)
        {
            ShowPostFightDialogue(dialogue, currentFloor, onDismissed);
        }

        private void Update()
        {
            if (!_active || _dismissing) return;

            if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
                Dismiss();
        }

        private void Dismiss()
        {
            if (_dismissing) return;
            _dismissing = true;
            StartCoroutine(FadeOutAndFinish());
        }

        /// <summary>
        /// Applies visual styling based on floor depth to convey dialogue tone.
        /// Floors 1–9: clean white text on dark panel (corporate).
        /// Floors 12+: reddish tint, slightly distorted feel (unsettling).
        /// </summary>
        private void ApplyToneStyling(int floor)
        {
            if (floor >= UnsettlingFloorThreshold)
            {
                // Unsettling tone (Req 25.5)
                if (dialogueText != null)
                    dialogueText.color = new Color(0.9f, 0.6f, 0.6f, 1f);
                if (backgroundPanel != null)
                    backgroundPanel.color = new Color(0.15f, 0.02f, 0.02f, 0.85f);
                if (dismissHint != null)
                    dismissHint.color = new Color(0.7f, 0.4f, 0.4f, 0.6f);
            }
            else
            {
                // Corporate tone (Req 25.4)
                if (dialogueText != null)
                    dialogueText.color = Color.white;
                if (backgroundPanel != null)
                    backgroundPanel.color = new Color(0f, 0f, 0f, 0.8f);
                if (dismissHint != null)
                    dismissHint.color = new Color(1f, 1f, 1f, 0.5f);
            }
        }

        private IEnumerator FadeIn()
        {
            if (canvasGroup == null) yield break;

            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }

        private IEnumerator FadeOutAndFinish()
        {
            if (canvasGroup != null)
            {
                float elapsed = 0f;
                while (elapsed < fadeOutDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    canvasGroup.alpha = Mathf.Clamp01(1f - elapsed / fadeOutDuration);
                    yield return null;
                }
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            _active = false;
            gameObject.SetActive(false);
            _onDismissed?.Invoke();
        }

        /// <summary>
        /// Checks whether the given floor uses unsettling dialogue tone.
        /// Useful for external systems that need to know the tone.
        /// </summary>
        public static bool IsUnsettlingFloor(int floor)
        {
            return floor >= UnsettlingFloorThreshold;
        }
    }
}
