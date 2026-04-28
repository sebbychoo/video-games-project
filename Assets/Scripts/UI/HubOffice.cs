using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace CardBattle
{
    /// <summary>
    /// 2D diorama-style Hub Office scene with cursor-based interaction only (no WASD).
    /// Players hover furniture to see upgrade options and Bad_Reviews costs,
    /// click to purchase upgrades. Accessible from main menu and during a run.
    /// Upgrades apply from the next run onward.
    /// </summary>
    public class HubOffice : MonoBehaviour
    {
        [Header("Furniture Items")]
        [SerializeField] private List<HubFurnitureItem> furnitureItems;

        [Header("Tooltip UI")]
        [SerializeField] private GameObject tooltipPanel;
        [SerializeField] private TextMeshProUGUI tooltipName;
        [SerializeField] private TextMeshProUGUI tooltipDescription;
        [SerializeField] private TextMeshProUGUI tooltipCost;
        [SerializeField] private TextMeshProUGUI tooltipLevel;

        [Header("Feedback UI")]
        [SerializeField] private TextMeshProUGUI feedbackText;
        [SerializeField] private float feedbackDuration = 2f;

        [Header("Bad Reviews Display")]
        [SerializeField] private TextMeshProUGUI badReviewsText;

        [Header("Navigation")]
        [SerializeField] private Button backButton;

        private float _feedbackTimer;
        private HubFurnitureItem _hoveredItem;

        private void Start()
        {
            // Ensure cursor is visible and unlocked for this scene
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);

                // Prevent the tooltip from intercepting raycasts, which causes
                // hover flickering (tooltip appears → steals hover → hides → repeat).
                CanvasGroup cg = tooltipPanel.GetComponent<CanvasGroup>();
                if (cg == null)
                    cg = tooltipPanel.AddComponent<CanvasGroup>();
                cg.blocksRaycasts = false;
                cg.interactable = false;
            }

            if (feedbackText != null)
                feedbackText.gameObject.SetActive(false);

            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);

            // Initialize all furniture visuals from MetaState
            InitializeFurnitureVisuals();
            RefreshBadReviewsDisplay();
        }

        private void Update()
        {
            // Fade out feedback text
            if (_feedbackTimer > 0f)
            {
                _feedbackTimer -= Time.unscaledDeltaTime;
                if (_feedbackTimer <= 0f && feedbackText != null)
                    feedbackText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Called by HubFurnitureItem when the player hovers over furniture.
        /// Shows the tooltip with upgrade info and cost.
        /// </summary>
        public void OnFurnitureHover(HubFurnitureItem item)
        {
            _hoveredItem = item;
            if (item == null || item.UpgradeData == null)
            {
                HideTooltip();
                return;
            }

            HubUpgradeData data = item.UpgradeData;
            int currentLevel = GetUpgradeLevel(data.upgradeId);

            if (tooltipPanel != null)
                tooltipPanel.SetActive(true);

            if (tooltipName != null)
                tooltipName.text = data.displayName;

            if (tooltipDescription != null)
                tooltipDescription.text = data.description;

            if (tooltipLevel != null)
                tooltipLevel.text = currentLevel >= data.maxLevel
                    ? $"Level {currentLevel} (MAX)"
                    : $"Level {currentLevel} / {data.maxLevel}";

            if (tooltipCost != null)
            {
                if (currentLevel >= data.maxLevel)
                    tooltipCost.text = "Fully Upgraded";
                else if (data.costPerLevel != null && currentLevel < data.costPerLevel.Count)
                    tooltipCost.text = $"Cost: {data.costPerLevel[currentLevel]} Bad Reviews";
                else
                    tooltipCost.text = "";
            }
        }

        /// <summary>
        /// Called by HubFurnitureItem when the player stops hovering.
        /// </summary>
        public void OnFurnitureExit(HubFurnitureItem item)
        {
            if (_hoveredItem == item)
            {
                _hoveredItem = null;
                HideTooltip();
            }
        }

        /// <summary>
        /// Called by HubFurnitureItem when the player clicks furniture.
        /// Attempts to purchase the next upgrade level.
        /// </summary>
        public void OnFurnitureClick(HubFurnitureItem item)
        {
            if (item == null || item.UpgradeData == null) return;

            HubUpgradeData data = item.UpgradeData;
            int currentLevel = GetUpgradeLevel(data.upgradeId);

            // Already at max level
            if (currentLevel >= data.maxLevel)
            {
                ShowFeedback("Already fully upgraded!");
                return;
            }

            // Get cost for next level
            if (data.costPerLevel == null || currentLevel >= data.costPerLevel.Count)
            {
                ShowFeedback("Upgrade data error.");
                return;
            }

            int cost = data.costPerLevel[currentLevel];
            MetaState meta = GetMeta();
            if (meta == null) return;

            // Check if player can afford it
            if (meta.badReviews < cost)
            {
                ShowFeedback($"Not enough Bad Reviews! Need {cost}, have {meta.badReviews}.");
                return;
            }

            // Purchase: deduct cost, increment level, save, update visuals
            meta.badReviews -= cost;
            SetUpgradeLevel(data.upgradeId, currentLevel + 1);

            if (SaveManager.Instance != null)
                SaveManager.Instance.SaveMeta();

            // Update furniture visual
            item.UpdateVisual(currentLevel + 1);

            // Refresh tooltip to show new level
            OnFurnitureHover(item);
            RefreshBadReviewsDisplay();

            ShowFeedback($"{data.displayName} upgraded to level {currentLevel + 1}!");
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private void InitializeFurnitureVisuals()
        {
            if (furnitureItems == null) return;

            foreach (HubFurnitureItem item in furnitureItems)
            {
                if (item == null || item.UpgradeData == null) continue;
                int level = GetUpgradeLevel(item.UpgradeData.upgradeId);
                item.UpdateVisual(level);
            }
        }

        private void HideTooltip()
        {
            if (tooltipPanel != null)
                tooltipPanel.SetActive(false);
        }

        private void ShowFeedback(string message)
        {
            if (feedbackText != null)
            {
                feedbackText.text = message;
                feedbackText.gameObject.SetActive(true);
                _feedbackTimer = feedbackDuration;
            }
        }

        private void RefreshBadReviewsDisplay()
        {
            MetaState meta = GetMeta();
            if (badReviewsText != null && meta != null)
                badReviewsText.text = $"Bad Reviews: {meta.badReviews}";
        }

        private MetaState GetMeta()
        {
            if (SaveManager.Instance != null)
                return SaveManager.Instance.CurrentMeta;
            return null;
        }

        /// <summary>
        /// Gets the current upgrade level for a given upgrade ID from MetaState.
        /// </summary>
        public static int GetUpgradeLevel(string upgradeId)
        {
            if (SaveManager.Instance == null) return 0;
            MetaState meta = SaveManager.Instance.CurrentMeta;
            if (meta?.hubUpgradeLevels == null) return 0;

            foreach (StringIntPair pair in meta.hubUpgradeLevels)
            {
                if (pair.key == upgradeId)
                    return pair.value;
            }
            return 0;
        }

        /// <summary>
        /// Sets the upgrade level for a given upgrade ID in MetaState.
        /// Creates the entry if it doesn't exist.
        /// </summary>
        private void SetUpgradeLevel(string upgradeId, int level)
        {
            MetaState meta = GetMeta();
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

            // Entry doesn't exist yet — create it
            meta.hubUpgradeLevels.Add(new StringIntPair { key = upgradeId, value = level });
        }

        private void OnBackClicked()
        {
            // If there's an active run starting, go to exploration.
            // Otherwise (accessed from main menu), return to menu.
            bool hasActiveRun = SaveManager.Instance != null
                && SaveManager.Instance.CurrentRun != null
                && SaveManager.Instance.CurrentRun.currentFloor >= 1;

            if (hasActiveRun)
            {
                if (SceneLoader.Instance != null)
                {
                    SceneLoader.Instance.useDefaultSpawn = true;
                    SceneLoader.Instance.enemyDefeated = false;
                    SceneLoader.Instance.LoadSceneUI("Explorationscene");
                }
                else
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene("Explorationscene");
                }
            }
            else
            {
                if (SceneLoader.Instance != null)
                    SceneLoader.Instance.LoadSceneMenu("Menu");
                else
                    UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
            }
        }
    }
}
