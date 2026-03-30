using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CardBattle
{
    /// <summary>
    /// Water cooler rest stop that appears every 2 floors during the elevator transition.
    /// Restores 35% of max HP (rounded down), one-time use per occurrence.
    /// Plant hub upgrade healing triggers first (on floor exit), water cooler afterward.
    /// Requirements: 42.1, 42.2, 42.3, 42.4, 42.5
    /// </summary>
    public class WaterCooler : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject confirmationPanel;
        [SerializeField] private TextMeshProUGUI healAmountText;
        [SerializeField] private Button useButton;
        [SerializeField] private Button skipButton;

        /// <summary>Heal percentage per Requirement 42.2 (35% of max HP, rounded down).</summary>
        private const float HealPercent = 0.35f;

        private bool _used;

        private void Start()
        {
            if (useButton != null)
                useButton.onClick.AddListener(UseWaterCooler);
            if (skipButton != null)
                skipButton.onClick.AddListener(Skip);

            RefreshUI();
        }

        /// <summary>
        /// Shows the confirmation panel with the heal amount preview.
        /// Called when the player reaches the elevator transition area on an eligible floor.
        /// </summary>
        public void ShowPrompt()
        {
            if (_used) return;

            int healAmount = CalculateHealAmount();
            if (healAmountText != null)
                healAmountText.text = $"Drink from the water cooler?\nRestores {healAmount} HP";

            if (confirmationPanel != null)
                confirmationPanel.SetActive(true);
        }

        /// <summary>
        /// Applies the heal and marks the cooler as used. Req 42.2, 42.3.
        /// </summary>
        public void UseWaterCooler()
        {
            if (_used) return;

            RunState run = GetRunState();
            if (run == null) return;

            int healAmount = CalculateHealAmount();
            run.playerHP = Mathf.Min(run.playerHP + healAmount, run.playerMaxHP);
            _used = true;

            SaveManager.Instance?.SaveRun();

            if (confirmationPanel != null)
                confirmationPanel.SetActive(false);

            RefreshUI();
        }

        /// <summary>
        /// Player skips the water cooler without using it.
        /// </summary>
        public void Skip()
        {
            if (confirmationPanel != null)
                confirmationPanel.SetActive(false);
        }

        /// <summary>
        /// Returns true if this water cooler has already been used. Req 42.3.
        /// </summary>
        public bool IsUsed => _used;

        /// <summary>
        /// Calculates heal amount: floor(maxHP * 0.35). Req 42.2, 42.4.
        /// </summary>
        public int CalculateHealAmount()
        {
            RunState run = GetRunState();
            if (run == null) return 0;
            return Mathf.FloorToInt(run.playerMaxHP * HealPercent);
        }

        /// <summary>
        /// Determines whether a water cooler should appear on the given floor.
        /// Appears every 2 floors: floors 2, 4, 6, 8, etc. Req 42.1.
        /// </summary>
        public static bool ShouldAppearOnFloor(int floor)
        {
            return floor > 0 && floor % 2 == 0;
        }

        private void RefreshUI()
        {
            if (useButton != null)
                useButton.interactable = !_used;
        }

        private RunState GetRunState()
        {
            if (SaveManager.Instance != null)
                return SaveManager.Instance.CurrentRun;
            return null;
        }
    }
}
