using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CardBattle
{
    /// <summary>
    /// Right-side info panel that slides in when a desk item is clicked.
    /// Displays item title, status, description, and — if the item has a
    /// HubUpgradeData asset linked — an upgrade section with level pips,
    /// cost, and a purchase button backed by SaveManager / MetaState.
    ///
    /// Also drives the left Sidebar hide/reveal based on cursor proximity.
    /// </summary>
    public class HubInfoPanel : MonoBehaviour
    {
        // ── Inspector — this panel ───────────────────────────────────────────

        [Header("This Panel")]
        [Tooltip("RectTransform of the info panel — animated in/out on the right.")]
        [SerializeField] private RectTransform panelRect;
        [SerializeField] private Button closeButton;

        [Header("Content")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI descriptionText;

        [Header("Status Colors")]
        [SerializeField] private Color onlineColor = new Color(0.769f, 0.824f, 0f, 1f);
        [SerializeField] private Color lockedColor  = new Color(0.408f, 0.345f, 0.251f, 1f);

        [Header("Info Panel Slide")]
        [SerializeField] private float slideDuration = 0.30f;
        [SerializeField] private AnimationCurve slideCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        // ── Inspector — upgrade section ──────────────────────────────────────

        [Header("Upgrade Section")]
        [Tooltip("Parent container for all upgrade UI — hidden when item has no upgrade data.")]
        [SerializeField] private GameObject upgradeSection;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI pipsText;
        [SerializeField] private TextMeshProUGUI upgradeEffectText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private Button purchaseButton;
        [SerializeField] private TextMeshProUGUI badReviewsText;
        [SerializeField] private TextMeshProUGUI feedbackText;

        // ── Inspector — sidebar ──────────────────────────────────────────────

        [Header("Sidebar")]
        [Tooltip("The left Sidebar RectTransform driven by cursor proximity.")]
        [SerializeField] private RectTransform sidebarRect;

        [Tooltip("How far left (in canvas units) past the sidebar's right edge the cursor " +
                 "must travel before the sidebar is fully revealed.")]
        [SerializeField] private float sidebarRevealDistance = 120f;

        [Tooltip("Speed at which the sidebar X position tracks the target value.")]
        [SerializeField] private float sidebarSmoothSpeed = 8f;

        // ── Private state ────────────────────────────────────────────────────

        private float              _infoPanelWidth;
        private bool               _isInfoVisible;
        private Coroutine          _infoSlideCoroutine;
        private HubItemInteraction _currentCaller;

        private float  _sidebarWidth;
        private float  _sidebarHiddenX;
        private float  _sidebarTargetX;
        private float  _sidebarGraceTimer;
        private bool   _sidebarLocked;

        /// <summary>When true, all HubInfoPanel instances hold the sidebar fully visible regardless of cursor.</summary>
        public static bool SidebarLockedGlobal;

        private const float _sidebarGraceDuration = 5f;

        private Canvas        _canvas;
        private RectTransform _canvasRect;

        private HubUpgradeData _currentUpgradeData;
        private Coroutine      _feedbackCoroutine;

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void Awake()
        {
            _infoPanelWidth = panelRect != null ? panelRect.sizeDelta.x : 300f;
            _canvas         = GetComponentInParent<Canvas>();

            if (sidebarRect != null)
            {
                _sidebarWidth   = sidebarRect.sizeDelta.x;
                _sidebarHiddenX = -_sidebarWidth;
                _sidebarTargetX = 0f;

                // Hard-snap sidebar visible before any Update runs.
                Vector2 pos = sidebarRect.anchoredPosition;
                pos.x = 0f;
                sidebarRect.anchoredPosition = pos;
            }

            _sidebarGraceTimer = _sidebarGraceDuration;
        }

        private void Start()
        {
            if (closeButton   != null) closeButton.onClick.AddListener(Hide);
            if (purchaseButton != null) purchaseButton.onClick.AddListener(OnPurchaseClicked);

            SetInfoPanelX(_infoPanelWidth);

            if (upgradeSection != null) upgradeSection.SetActive(false);
            if (feedbackText   != null) feedbackText.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (closeButton   != null) closeButton.onClick.RemoveListener(Hide);
            if (purchaseButton != null) purchaseButton.onClick.RemoveListener(OnPurchaseClicked);
        }

        private void Update()
        {
            if (_sidebarGraceTimer > 0f)
                _sidebarGraceTimer -= Time.unscaledDeltaTime;

            UpdateSidebar();
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Populates and slides the info panel in. Clicking the same item again closes it.
        /// Pass null for upgradeData if the item is not upgradeable.
        /// </summary>
        public void ShowItem(
            string             title,
            string             status,
            string             description,
            HubUpgradeData     upgradeData,
            HubItemInteraction caller)
        {
            if (_isInfoVisible && caller == _currentCaller)
            {
                Hide();
                return;
            }

            if (_currentCaller != null && _currentCaller != caller)
                _currentCaller.Deselect();

            _currentCaller      = caller;
            _currentUpgradeData = upgradeData;

            if (titleText       != null) titleText.text       = title;
            if (descriptionText != null) descriptionText.text = description;

            if (statusText != null)
            {
                bool online      = status.ToUpperInvariant() == "ONLINE";
                statusText.text  = status.ToUpperInvariant();
                statusText.color = online ? onlineColor : lockedColor;
            }

            RefreshUpgradeSection();
            RepositionUpgradeSection();
            SlideInfoIn();
        }

        /// <summary>Slides the info panel out and clears the current selection.</summary>
        public void Hide()
        {
            if (_currentCaller != null)
            {
                _currentCaller.Deselect();
                _currentCaller = null;
            }

            SlideInfoOut();
        }

        // ── Upgrade section ───────────────────────────────────────────────────

        /// <summary>
        /// Repositions UpgradeSection immediately below DescriptionText.
        /// Requires DescriptionText to have a ContentSizeFitter (verticalFit = PreferredSize)
        /// and UpgradeSection to be top-anchored.
        /// </summary>
        private void RepositionUpgradeSection()
        {
            if (upgradeSection == null || descriptionText == null) return;

            Canvas.ForceUpdateCanvases();

            RectTransform upgradeRT = upgradeSection.GetComponent<RectTransform>();
            if (upgradeRT == null) return;

            RectTransform descRT  = descriptionText.GetComponent<RectTransform>();
            float descTop         = descRT.anchoredPosition.y;
            float descHeight      = descRT.rect.height;
            upgradeRT.anchoredPosition = new Vector2(upgradeRT.anchoredPosition.x, descTop - descHeight - 10f);
        }

        private void RefreshUpgradeSection()
        {
            if (upgradeSection == null) return;

            if (_currentUpgradeData == null)
            {
                upgradeSection.SetActive(false);
                return;
            }

            upgradeSection.SetActive(true);

            int  currentLevel = HubOffice.GetUpgradeLevel(_currentUpgradeData.upgradeId);
            bool isMaxed      = currentLevel >= _currentUpgradeData.maxLevel;

            // "LEVEL 2 / 3"  or  "LEVEL 3  [MAX]"
            if (levelText != null)
                levelText.text = isMaxed
                    ? $"LEVEL {currentLevel}  [MAX]"
                    : $"LEVEL {currentLevel} / {_currentUpgradeData.maxLevel}";

            // Pip dots  ● filled  ○ empty
            if (pipsText != null)
            {
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < _currentUpgradeData.maxLevel; i++)
                {
                    if (i > 0) sb.Append("  ");
                    sb.Append(i < currentLevel ? '●' : '○');
                }
                pipsText.text = sb.ToString();
            }

            if (upgradeEffectText != null)
                upgradeEffectText.text = _currentUpgradeData.description;

            if (costText != null)
            {
                if (isMaxed)
                {
                    costText.text = "FULLY UPGRADED";
                }
                else if (_currentUpgradeData.costPerLevel != null
                         && currentLevel < _currentUpgradeData.costPerLevel.Count)
                {
                    costText.text = $"COST:  {_currentUpgradeData.costPerLevel[currentLevel]} \u2605";
                }
                else
                {
                    costText.text = string.Empty;
                }
            }

            RefreshBadReviews();

            if (purchaseButton != null)
            {
                purchaseButton.interactable = !isMaxed;
                var label = purchaseButton.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                    label.text = isMaxed ? "MAXED" : "UPGRADE";
            }
        }

        private void RefreshBadReviews()
        {
            if (badReviewsText == null) return;
            MetaState meta      = SaveManager.Instance?.CurrentMeta;
            badReviewsText.text = meta != null
                ? $"\u2605 {meta.badReviews}  BAD REVIEWS"
                : string.Empty;
        }

        private void OnPurchaseClicked()
        {
            if (_currentUpgradeData == null) return;

            int currentLevel = HubOffice.GetUpgradeLevel(_currentUpgradeData.upgradeId);

            if (currentLevel >= _currentUpgradeData.maxLevel)
            {
                ShowFeedback("Already fully upgraded!");
                return;
            }

            if (_currentUpgradeData.costPerLevel == null
                || currentLevel >= _currentUpgradeData.costPerLevel.Count)
            {
                ShowFeedback("Upgrade data error.");
                return;
            }

            int       cost = _currentUpgradeData.costPerLevel[currentLevel];
            MetaState meta = SaveManager.Instance?.CurrentMeta;
            if (meta == null) return;

            if (meta.badReviews < cost)
            {
                ShowFeedback($"Need {cost} \u2605 — have {meta.badReviews}.");
                return;
            }

            meta.badReviews -= cost;
            SetUpgradeLevel(_currentUpgradeData.upgradeId, currentLevel + 1);
            SaveManager.Instance?.SaveMeta();

            ShowFeedback($"Upgraded to level {currentLevel + 1}!");
            RefreshUpgradeSection();
        }

        private static void SetUpgradeLevel(string upgradeId, int level)
        {
            MetaState meta = SaveManager.Instance?.CurrentMeta;
            if (meta == null) return;

            if (meta.hubUpgradeLevels == null)
                meta.hubUpgradeLevels = new List<StringIntPair>();

            foreach (StringIntPair pair in meta.hubUpgradeLevels)
            {
                if (pair.key == upgradeId)
                {
                    pair.value = level;
                    return;
                }
            }

            meta.hubUpgradeLevels.Add(new StringIntPair { key = upgradeId, value = level });
        }

        private void ShowFeedback(string message)
        {
            if (feedbackText == null) return;
            if (_feedbackCoroutine != null) StopCoroutine(_feedbackCoroutine);
            _feedbackCoroutine = StartCoroutine(FeedbackRoutine(message));
        }

        private IEnumerator FeedbackRoutine(string message)
        {
            feedbackText.text = message;
            feedbackText.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(2f);
            feedbackText.gameObject.SetActive(false);
            _feedbackCoroutine = null;
        }

        // ── Sidebar cursor logic ──────────────────────────────────────────────

        /// <summary>Snap the sidebar fully visible and prevent cursor-driven hiding until Unlock() is called.</summary>
        public void LockSidebar()
        {
            _sidebarLocked = true;
            if (sidebarRect != null)
            {
                Vector2 pos = sidebarRect.anchoredPosition;
                pos.x = 0f;
                sidebarRect.anchoredPosition = pos;
            }
            _sidebarTargetX = 0f;
        }

        /// <summary>Restore cursor-driven sidebar behaviour after a LockSidebar() call.</summary>
        public void UnlockSidebar() => _sidebarLocked = false;

        private void UpdateSidebar()
        {
            if (_sidebarLocked || SidebarLockedGlobal)
            {
                if (sidebarRect != null)
                {
                    Vector2 pos = sidebarRect.anchoredPosition;
                    pos.x = 0f;
                    sidebarRect.anchoredPosition = pos;
                }
                return;
            }
            if (sidebarRect == null || _canvas == null) return;

            // Grace period: hard-snap visible every frame so no other script can override.
            if (_sidebarGraceTimer > 0f)
            {
                Vector2 snapPos = sidebarRect.anchoredPosition;
                snapPos.x = 0f;
                sidebarRect.anchoredPosition = snapPos;
                _sidebarTargetX = 0f;
                return;
            }

            if (_canvasRect == null)
                _canvasRect = (RectTransform)_canvas.transform;

            Camera eventCam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : (_canvas.worldCamera ?? Camera.main);

            Vector2 localCursor;
            bool insideCanvas = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, Input.mousePosition, eventCam, out localCursor);

            if (!insideCanvas)
            {
                _sidebarTargetX = 0f;
                ApplySidebarLerp();
                return;
            }

            float sidebarRightLocal = -_canvasRect.rect.width * 0.5f + _sidebarWidth;

            if (localCursor.x >= sidebarRightLocal)
            {
                _sidebarTargetX = _sidebarHiddenX;
            }
            else
            {
                float overshoot = sidebarRightLocal - localCursor.x;
                float t         = Mathf.Clamp01(overshoot / sidebarRevealDistance);
                _sidebarTargetX = Mathf.Lerp(_sidebarHiddenX, 0f, t);
            }

            ApplySidebarLerp();
        }

        private void ApplySidebarLerp()
        {
            float newX = Mathf.Lerp(
                sidebarRect.anchoredPosition.x,
                _sidebarTargetX,
                Time.unscaledDeltaTime * sidebarSmoothSpeed);

            Vector2 pos = sidebarRect.anchoredPosition;
            pos.x = newX;
            sidebarRect.anchoredPosition = pos;
        }

        // ── Info panel slide ──────────────────────────────────────────────────

        private void SlideInfoIn()
        {
            _isInfoVisible = true;
            SlideInfoTo(-_infoPanelWidth);
        }

        private void SlideInfoOut()
        {
            _isInfoVisible = false;
            SlideInfoTo(_infoPanelWidth);
        }

        private void SlideInfoTo(float targetX)
        {
            if (_infoSlideCoroutine != null) StopCoroutine(_infoSlideCoroutine);
            _infoSlideCoroutine = StartCoroutine(InfoSlideCoroutine(targetX));
        }

        private IEnumerator InfoSlideCoroutine(float targetX)
        {
            float startX  = panelRect.anchoredPosition.x;
            float elapsed = 0f;

            while (elapsed < slideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = slideCurve.Evaluate(Mathf.Clamp01(elapsed / slideDuration));
                SetInfoPanelX(Mathf.LerpUnclamped(startX, targetX, t));
                yield return null;
            }

            SetInfoPanelX(targetX);
            _infoSlideCoroutine = null;
        }

        private void SetInfoPanelX(float x)
        {
            if (panelRect == null) return;
            Vector2 pos = panelRect.anchoredPosition;
            pos.x = x;
            panelRect.anchoredPosition = pos;
        }
    }
}
