using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CardBattle
{
    /// <summary>
    /// Slides the left panel in/out on the UGUI main menu.
    /// FocusDesk slides it out; ShowMenu slides it back in.
    /// The cursor re-entering the left edge zone also recalls the panel.
    /// </summary>
    public class MenuSlideController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private RectTransform leftPanel;
        [SerializeField] private Image deskSurface;

        [Header("Hints")]
        [SerializeField] private GameObject deskHint;
        [SerializeField] private GameObject returnHint;

        [Header("Slide")]
        [SerializeField] private float slideDuration = 0.38f;

        private const float LeftEdgeReturnZonePx = 40f;

        private bool _slid;
        private bool _locked;
        private Coroutine _slideCoroutine;

        private void Start()
        {
            SetPanelInstant(false);
        }

        private void Update()
        {
            if (_slid && !_locked && Input.mousePosition.x <= LeftEdgeReturnZonePx)
                ShowMenu();
        }

        /// <summary>Prevent the panel from being slid out and immediately snap it visible. Call Unlock() to restore normal behaviour.</summary>
        public void Lock()
        {
            _locked = true;
            SetPanelInstant(false);
        }

        /// <summary>Restore normal slide behaviour after a Lock() call.</summary>
        public void Unlock() => _locked = false;

        /// <summary>Slide the panel out to show the desk.</summary>
        public void FocusDesk()
        {
            if (_slid || _locked) return;
            _slid = true;

            if (deskHint != null)   deskHint.SetActive(false);
            if (returnHint != null) returnHint.SetActive(true);

            if (leftPanel != null)
                StartSlide(-leftPanel.rect.width - 4f);
        }

        /// <summary>Slide the panel back in.</summary>
        public void ShowMenu()
        {
            if (!_slid) return;
            _slid = false;

            if (deskHint != null)   deskHint.SetActive(true);
            if (returnHint != null) returnHint.SetActive(false);

            if (leftPanel != null)
                StartSlide(0f);
        }

        private void StartSlide(float targetX)
        {
            if (_slideCoroutine != null)
                StopCoroutine(_slideCoroutine);
            _slideCoroutine = StartCoroutine(SlideTo(targetX));
        }

        private IEnumerator SlideTo(float targetX)
        {
            float startX  = leftPanel.anchoredPosition.x;
            float elapsed = 0f;

            while (elapsed < slideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / slideDuration));
                Vector2 pos = leftPanel.anchoredPosition;
                pos.x = Mathf.Lerp(startX, targetX, t);
                leftPanel.anchoredPosition = pos;
                yield return null;
            }

            Vector2 finalPos = leftPanel.anchoredPosition;
            finalPos.x = targetX;
            leftPanel.anchoredPosition = finalPos;
        }

        private void SetPanelInstant(bool slid)
        {
            if (_slideCoroutine != null)
            {
                StopCoroutine(_slideCoroutine);
                _slideCoroutine = null;
            }
            _slid = slid;
            if (leftPanel == null) return;

            Vector2 pos = leftPanel.anchoredPosition;
            pos.x = slid ? -leftPanel.rect.width - 4f : 0f;
            leftPanel.anchoredPosition = pos;

            if (deskHint != null)   deskHint.SetActive(!slid);
            if (returnHint != null) returnHint.SetActive(slid);
        }
    }
}
