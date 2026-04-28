using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace CardBattle
{
    /// <summary>
    /// Main menu controller. Provides New Game, Continue, Settings, Achievements, and Quit.
    /// Attached to a GameObject in the Menu scene.
    /// Requirements: 35.1–35.9
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button achievementsButton;
        [SerializeField] private Button quitButton;

        [Header("Continue Button Styling")]
        [SerializeField] private Color disabledTextColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        private Color _defaultContinueTextColor;

        [Header("Panels")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject achievementsPanel;

        [Header("Settings Panel")]
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Button settingsBackButton;

        [Header("Achievements Panel")]
        [SerializeField] private Transform achievementListContent;
        [SerializeField] private GameObject achievementEntryPrefab;
        [SerializeField] private Button achievementsBackButton;

        private void Start()
        {
            // Ensure cursor is visible on menu (Req 35.1)
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 1f;

            // Ensure SaveManager exists
            EnsureSaveManager();

            // Wire buttons
            if (newGameButton != null)
                newGameButton.onClick.AddListener(OnNewGame);
            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinue);
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettings);
            if (achievementsButton != null)
                achievementsButton.onClick.AddListener(OnAchievements);
            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuit);

            // Settings panel back
            if (settingsBackButton != null)
                settingsBackButton.onClick.AddListener(CloseSettings);
            // Achievements panel back
            if (achievementsBackButton != null)
                achievementsBackButton.onClick.AddListener(CloseAchievements);

            // Hide sub-panels
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (achievementsPanel != null) achievementsPanel.SetActive(false);

            // Evaluate Continue button state (Req 35.4, 35.5)
            RefreshContinueButton();
        }

        // ── Button Handlers ───────────────────────────────────────────────

        /// <summary>New Game → opening dialogue (Req 35.3).</summary>
        private void OnNewGame()
        {
            // Wipe any existing run so the opening dialogue starts fresh
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.WipeRun();
                SaveManager.Instance.CurrentRun.currentFloor = 1;
                SaveManager.Instance.CurrentRun.hasCustomSpawn = false;
            }

            if (SceneLoader.Instance != null)
                SceneLoader.Instance.LoadSceneMenu("OpeningDialogue");
            else
                SceneManager.LoadScene("OpeningDialogue");
        }

        /// <summary>Continue → load saved run and resume (Req 35.4).</summary>
        private void OnContinue()
        {
            if (SaveManager.Instance == null) return;

            // If player quit mid-combat, restore pre-encounter snapshot (Req 27.7)
            if (SaveManager.Instance.HasPreEncounterSnapshot())
                SaveManager.Instance.RestorePreEncounter();

            RunState run = SaveManager.Instance.CurrentRun;
            if (run == null || !run.isActive) return;

            // Resume at exploration scene — SceneLoader handles spawn position
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.useDefaultSpawn = true;
                SceneLoader.Instance.LoadSceneUI("Explorationscene");
            }
            else
            {
                SceneManager.LoadScene("Explorationscene");
            }
        }

        /// <summary>Settings → open settings panel (Req 35.6).</summary>
        private void OnSettings()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(true);
        }

        /// <summary>Achievements → open achievements panel (Req 35.7).</summary>
        private void OnAchievements()
        {
            if (achievementsPanel != null)
            {
                achievementsPanel.SetActive(true);
                PopulateAchievements();
            }
        }

        /// <summary>Quit → close application (Req 35.8).</summary>
        private void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ── Sub-panel Close ───────────────────────────────────────────────

        private void CloseSettings()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(false);
        }

        private void CloseAchievements()
        {
            if (achievementsPanel != null)
                achievementsPanel.SetActive(false);
        }

        // ── Continue Button State ─────────────────────────────────────────

        /// <summary>
        /// Grey out Continue if no active save exists (Req 35.5).
        /// </summary>
        private void RefreshContinueButton()
        {
            if (continueButton == null) return;

            bool hasSave = false;
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.LoadRun();
                RunState run = SaveManager.Instance.CurrentRun;
                hasSave = run != null && run.isActive;

                // Also count a pre-encounter snapshot as a valid save
                if (!hasSave)
                    hasSave = SaveManager.Instance.HasPreEncounterSnapshot();
            }

            continueButton.interactable = hasSave;

            // Dim the text when disabled
            TextMeshProUGUI label = continueButton.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                if (_defaultContinueTextColor == default)
                    _defaultContinueTextColor = label.color;

                label.color = hasSave ? _defaultContinueTextColor : disabledTextColor;
            }
        }

        // ── Achievements Population ───────────────────────────────────────

        private void PopulateAchievements()
        {
            if (achievementListContent == null) return;

            // Clear existing entries
            foreach (Transform child in achievementListContent)
                Destroy(child.gameObject);

            MetaState meta = SaveManager.Instance != null
                ? SaveManager.Instance.CurrentMeta
                : null;

            List<string> unlocked = meta?.unlockedAchievements;

            // Stub: show unlocked achievements as text entries.
            // When real achievement definitions are added, iterate those instead.
            if (unlocked != null && unlocked.Count > 0)
            {
                foreach (string achievement in unlocked)
                    CreateAchievementEntry(achievement, true);
            }
            else
            {
                CreateAchievementEntry("No achievements unlocked yet.", false);
            }
        }

        private void CreateAchievementEntry(string text, bool unlocked)
        {
            if (achievementListContent == null) return;

            if (achievementEntryPrefab != null)
            {
                GameObject entry = Instantiate(achievementEntryPrefab, achievementListContent);
                TextMeshProUGUI label = entry.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                {
                    label.text = text;
                    label.color = unlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.7f);
                }
            }
            else
            {
                // Fallback: create a simple text object
                GameObject entryGO = new GameObject("AchievementEntry");
                entryGO.transform.SetParent(achievementListContent, false);
                TextMeshProUGUI tmp = entryGO.AddComponent<TextMeshProUGUI>();
                tmp.text = unlocked ? $"✓ {text}" : text;
                tmp.fontSize = 18f;
                tmp.color = unlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.7f);
                tmp.enableWordWrapping = true;

                // Give it a layout element for scroll view sizing
                var le = entryGO.AddComponent<UnityEngine.UI.LayoutElement>();
                le.preferredHeight = 30f;
                le.flexibleWidth = 1f;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private void EnsureSaveManager()
        {
            if (SaveManager.Instance == null)
            {
                GameObject smGO = new GameObject("SaveManager");
                smGO.AddComponent<SaveManager>();
            }
            SaveManager.Instance.LoadMeta();
        }
    }
}
