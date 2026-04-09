using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CardBattle
{
    public class BossIntroScreen : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TextMeshProUGUI introducingLabel;
        [SerializeField] private TextMeshProUGUI nameLabel;
        [SerializeField] private TextMeshProUGUI titleLabel;

        [Header("Default Timing")]
        [SerializeField] private float defaultSlideDuration = 0.6f;
        [SerializeField] private float defaultHoldDuration = 1.5f;

        private Action _onComplete;
        private Coroutine _sequenceCoroutine;
        private Coroutine _bgAnimCoroutine;

        private void Awake()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }

        public static bool ShouldShowTitle(string bossTitle)
        {
            return !string.IsNullOrEmpty(bossTitle);
        }

        public void Play(string bossName, string bossTitle, BossIntroData introData, Action onComplete)
        {
            _onComplete = onComplete;

            float introLineDelay = introData != null ? introData.introLineDelay : 0f;
            float introSlideDur = introData != null ? introData.introLineSlideDuration : defaultSlideDuration;
            float nameDelay = introData != null ? introData.nameDelay : 0.3f;
            float nameSlideDur = introData != null ? introData.nameSlideDuration : defaultSlideDuration;
            float titleDelay = introData != null ? introData.titleDelay : 0.2f;
            float titleFadeDur = introData != null ? introData.titleFadeDuration : defaultSlideDuration * 0.5f;
            float holdDur = introData != null ? introData.holdDuration : defaultHoldDuration;
            string introText = introData != null && !string.IsNullOrEmpty(introData.introLine)
                ? introData.introLine : "Introducing...";

            // Show panel
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            // Background: animated if boss has frames, otherwise solid black
            bool hasAnim = introData != null
                && introData.backgroundAnimation != null
                && introData.backgroundAnimation.frames != null
                && introData.backgroundAnimation.frames.Length > 0;

            if (hasAnim)
            {
                if (_bgAnimCoroutine != null) StopCoroutine(_bgAnimCoroutine);
                _bgAnimCoroutine = StartCoroutine(AnimateBackground(introData.backgroundAnimation));
            }
            else if (backgroundImage != null)
            {
                backgroundImage.sprite = null;
                backgroundImage.color = Color.black;
            }

            // Set label text
            if (introducingLabel != null)
                introducingLabel.text = introText;
            if (nameLabel != null)
                nameLabel.text = bossName ?? string.Empty;

            bool showTitle = ShouldShowTitle(bossTitle);
            if (titleLabel != null)
            {
                titleLabel.text = showTitle ? bossTitle : string.Empty;
                titleLabel.gameObject.SetActive(showTitle);
            }

            if (_sequenceCoroutine != null)
                StopCoroutine(_sequenceCoroutine);
            _sequenceCoroutine = StartCoroutine(IntroSequence(
                showTitle, introText, introLineDelay, introSlideDur,
                nameDelay, nameSlideDur, titleDelay, titleFadeDur, holdDur));
        }

        public void Play(string bossName, string bossTitle, Action onComplete)
        {
            Play(bossName, bossTitle, null, onComplete);
        }

        private IEnumerator AnimateBackground(SpriteFrameAnimation anim)
        {
            float interval = 1f / Mathf.Max(anim.frameRate, 0.001f);
            int index = 0;
            if (backgroundImage != null)
                backgroundImage.color = Color.white; // show sprite colors properly

            while (true)
            {
                if (backgroundImage != null && anim.frames[index] != null)
                    backgroundImage.sprite = anim.frames[index];

                yield return new WaitForSeconds(interval);
                index++;
                if (index >= anim.frames.Length)
                {
                    if (anim.loop) index = 0;
                    else yield break;
                }
            }
        }

        private IEnumerator IntroSequence(
            bool showTitle, string introText,
            float introLineDelay, float introSlideDur,
            float nameDelay, float nameSlideDur,
            float titleDelay, float titleFadeDur, float holdDur)
        {
            SetLabelAlpha(introducingLabel, 0f);
            SetLabelAlpha(nameLabel, 0f);
            SetLabelAlpha(titleLabel, 0f);

            RectTransform introRT = introducingLabel != null ? introducingLabel.GetComponent<RectTransform>() : null;
            RectTransform nameRT = nameLabel != null ? nameLabel.GetComponent<RectTransform>() : null;

            float centerX = 0f;
            float offScreenX = -Screen.width;

            if (introLineDelay > 0f)
                yield return new WaitForSeconds(introLineDelay);

            if (introRT != null && !string.IsNullOrEmpty(introText))
            {
                SetLabelAlpha(introducingLabel, 1f);
                yield return StartCoroutine(SlideLabel(introRT, offScreenX, centerX, introSlideDur));
            }

            if (nameDelay > 0f)
                yield return new WaitForSeconds(nameDelay);

            if (nameRT != null)
            {
                SetLabelAlpha(nameLabel, 1f);
                yield return StartCoroutine(SlideLabel(nameRT, offScreenX, centerX, nameSlideDur));
            }

            if (showTitle && titleLabel != null)
            {
                if (titleDelay > 0f)
                    yield return new WaitForSeconds(titleDelay);
                SetLabelAlpha(titleLabel, 1f);
                yield return StartCoroutine(FadeInLabel(titleLabel, titleFadeDur));
            }

            yield return new WaitForSeconds(holdDur);
            Dismiss();
        }

        private IEnumerator SlideLabel(RectTransform rt, float fromX, float toX, float duration)
        {
            Vector2 pos = rt.anchoredPosition;
            pos.x = fromX;
            rt.anchoredPosition = pos;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / duration), 2f);
                pos = rt.anchoredPosition;
                pos.x = Mathf.Lerp(fromX, toX, eased);
                rt.anchoredPosition = pos;
                yield return null;
            }
            pos = rt.anchoredPosition;
            pos.x = toX;
            rt.anchoredPosition = pos;
        }

        private IEnumerator FadeInLabel(TextMeshProUGUI label, float duration)
        {
            if (label == null) yield break;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                label.alpha = Mathf.Clamp01(elapsed / duration);
                yield return null;
            }
            label.alpha = 1f;
        }

        private void SetLabelAlpha(TextMeshProUGUI label, float alpha)
        {
            if (label != null) label.alpha = alpha;
        }

        private void Dismiss()
        {
            if (_bgAnimCoroutine != null)
            {
                StopCoroutine(_bgAnimCoroutine);
                _bgAnimCoroutine = null;
            }
            if (backgroundImage != null)
            {
                backgroundImage.sprite = null;
                backgroundImage.color = Color.black;
            }
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            _onComplete?.Invoke();
            _onComplete = null;
        }
    }
}
