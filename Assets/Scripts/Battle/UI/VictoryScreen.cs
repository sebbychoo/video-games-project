using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CardBattle
{
    /// <summary>
    /// Post-encounter victory splash screen. Shows a randomized victory verb,
    /// enemy name(s), Hours earned, and Bad_Reviews if a boss encounter.
    /// Fades to a black background, counts up the hours from 0, then
    /// dismisses on player click/key press or after a configurable auto-dismiss delay.
    /// Exposes an OnDismissed callback so BattleManager can proceed with scene transition.
    /// </summary>
    public class VictoryScreen : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI victoryText;
        [SerializeField] TextMeshProUGUI rewardsText;
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] Image blackOverlay;
        [SerializeField] float autoDismissDelay = 1.5f;
        [SerializeField] float fadeInDuration = 0.2f;
        [SerializeField] float fadeOutDuration = 0.2f;
        [SerializeField] float countUpDuration = 1f;

        /// <summary>Invoked when the victory screen is dismissed (fade-out complete).</summary>
        public Action OnDismissed;

        private static readonly string[] VictoryVerbs =
        {
            "Defeated",
            "Vanquished",
            "Dealt with",
            "Showed the door to",
            "Took care of",
            "Handled"
        };

        private bool _visible;
        private bool _dismissing;
        private bool _countingUp;
        private Coroutine _autoDismissCoroutine;
        private Coroutine _fadeCoroutine;
        private Coroutine _countUpCoroutine;

        // Cached for count-up display
        private int _targetHours;
        private int _targetBadReviews;
        private bool _isBoss;

        private void Awake()
        {
            // Start hidden
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            if (blackOverlay != null)
            {
                blackOverlay.color = new Color(0f, 0f, 0f, 0f);
                blackOverlay.raycastTarget = false;
            }
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Display the victory screen with the given encounter results.
        /// </summary>
        public void Show(List<string> enemyNames, int hoursEarned, int badReviewsEarned, bool isBoss)
        {
            _targetHours = hoursEarned;
            _targetBadReviews = badReviewsEarned;
            _isBoss = isBoss;

            // Pick a random victory verb
            string verb = VictoryVerbs[UnityEngine.Random.Range(0, VictoryVerbs.Length)];

            // Build victory text
            string names = enemyNames != null && enemyNames.Count > 0
                ? string.Join(" & ", enemyNames)
                : "the enemy";
            if (victoryText != null)
                victoryText.text = $"{verb} {names}!";

            // Start rewards at 0
            if (rewardsText != null)
                rewardsText.text = "+0 Hours";

            // Activate and fade in
            gameObject.SetActive(true);
            _visible = true;
            _dismissing = false;
            _countingUp = true;

            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeIn());

            // Start auto-dismiss timer (begins after count-up finishes)
        }

        private void Update()
        {
            if (!_visible || _dismissing) return;

            // Allow click/key to skip at any time (even during count-up)
            if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
            {
                if (_countingUp)
                {
                    // Skip count-up — show final values immediately, then dismiss
                    SkipCountUp();
                }
                else
                {
                    Dismiss();
                }
            }
        }

        private void SkipCountUp()
        {
            if (_countUpCoroutine != null)
            {
                StopCoroutine(_countUpCoroutine);
                _countUpCoroutine = null;
            }

            // Show final values
            string finalRewards = $"+{_targetHours} Hours";
            if (_isBoss && _targetBadReviews > 0)
                finalRewards += $"\n+{_targetBadReviews} Bad Reviews";
            if (rewardsText != null)
                rewardsText.text = finalRewards;

            _countingUp = false;
            Dismiss();
        }

        /// <summary>Begin dismissing the victory screen.</summary>
        public void Dismiss()
        {
            if (_dismissing) return;
            _dismissing = true;

            if (_autoDismissCoroutine != null)
            {
                StopCoroutine(_autoDismissCoroutine);
                _autoDismissCoroutine = null;
            }
            if (_countUpCoroutine != null)
            {
                StopCoroutine(_countUpCoroutine);
                _countUpCoroutine = null;
            }

            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeOut());
        }

        private IEnumerator FadeIn()
        {
            if (canvasGroup == null) yield break;

            float elapsed = 0f;
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeInDuration);
                canvasGroup.alpha = t;

                // Fade black overlay in sync
                if (blackOverlay != null)
                    blackOverlay.color = new Color(0f, 0f, 0f, t);

                yield return null;
            }
            canvasGroup.alpha = 1f;
            if (blackOverlay != null)
                blackOverlay.color = Color.black;

            // Start count-up after fade-in completes
            _countUpCoroutine = StartCoroutine(CountUpRewards());
        }

        private IEnumerator CountUpRewards()
        {
            float elapsed = 0f;

            while (elapsed < countUpDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / countUpDuration);

                int currentHours = Mathf.RoundToInt(Mathf.Lerp(0, _targetHours, t));
                string rewards = $"+{currentHours} Hours";

                if (_isBoss && _targetBadReviews > 0)
                {
                    int currentBR = Mathf.RoundToInt(Mathf.Lerp(0, _targetBadReviews, t));
                    rewards += $"\n+{currentBR} Bad Reviews";
                }

                if (rewardsText != null)
                    rewardsText.text = rewards;

                yield return null;
            }

            // Ensure final values are exact
            string finalRewards = $"+{_targetHours} Hours";
            if (_isBoss && _targetBadReviews > 0)
                finalRewards += $"\n+{_targetBadReviews} Bad Reviews";
            if (rewardsText != null)
                rewardsText.text = finalRewards;

            _countingUp = false;

            // Now start auto-dismiss timer
            if (_autoDismissCoroutine != null)
                StopCoroutine(_autoDismissCoroutine);
            _autoDismissCoroutine = StartCoroutine(AutoDismissTimer());
        }

        private IEnumerator FadeOut()
        {
            if (canvasGroup == null)
            {
                FinishDismiss();
                yield break;
            }

            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeOutDuration;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);

                if (blackOverlay != null)
                    blackOverlay.color = new Color(0f, 0f, 0f, Mathf.Lerp(1f, 0f, t));

                yield return null;
            }

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            if (blackOverlay != null)
                blackOverlay.color = new Color(0f, 0f, 0f, 0f);
            FinishDismiss();
        }

        private IEnumerator AutoDismissTimer()
        {
            yield return new WaitForSeconds(autoDismissDelay);
            if (_visible && !_dismissing)
                Dismiss();
        }

        private void FinishDismiss()
        {
            _visible = false;
            _dismissing = false;
            _countingUp = false;
            gameObject.SetActive(false);
            OnDismissed?.Invoke();
        }
    }
}
