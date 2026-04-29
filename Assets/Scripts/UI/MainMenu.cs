using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace CardBattle
{
    /// <summary>
    /// Main menu controller. Fullscreen Settings and Achievements overlays,
    /// locked achievements shown as greyed-out ? rows with hover description tooltips.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        // ── Navigation buttons ─────────────────────────────────────────────
        [Header("Main Buttons")]
        [SerializeField] private Button newRunButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button achievementsButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button hubButton;
        [SerializeField] private Button quitButton;

        // ── Fullscreen overlay panels ──────────────────────────────────────
        [Header("Overlay Panels (fullscreen)")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject achievementsPanel;

        // ── Settings panel internals ───────────────────────────────────────
        [Header("Settings Panel")]
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Button settingsBackButton;

        // ── Achievements panel internals ───────────────────────────────────
        [Header("Achievements Panel")]
        [SerializeField] private Transform achievementListContent;
        [SerializeField] private Button achievementsBackButton;

        // ── Achievement row prefab / tooltip panel ─────────────────────────
        [Header("Achievement Row")]
        [SerializeField] private AchievementRegistry achievementRegistry;
        [SerializeField] private TooltipPanel tooltipPanel;

        [Header("Slide Controller")]
        [SerializeField] private MenuSlideController slideController;

        [Header("Opening Dialogue")]
        [SerializeField] private OpeningDialogue openingDialogue;

        // ── Achievement row colors — dark office / story tone ──────────────
        private static readonly Color LockedTextColor   = new Color(0.38f, 0.36f, 0.34f, 1f);
        private static readonly Color UnlockedTextColor = new Color(0.92f, 0.88f, 0.80f, 1f);
        private static readonly Color LockedBgColor     = new Color(0.10f, 0.09f, 0.09f, 1f);
        private static readonly Color UnlockedBgColor   = new Color(0.13f, 0.11f, 0.10f, 1f);

        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
            Time.timeScale   = 1f;

            EnsureSaveManager();

            if (tooltipPanel != null)
                AchievementTooltip.RegisterPanel(tooltipPanel);

            WireButton(newRunButton,       OnNewRun);
            WireButton(continueButton,     OnContinue);
            WireButton(achievementsButton, OnAchievements);
            WireButton(settingsButton,     OnSettings);
            WireButton(hubButton,          OnHub);
            WireButton(quitButton,         OnQuit);

            WireButton(settingsBackButton,     CloseSettings);
            WireButton(achievementsBackButton, CloseAchievements);

            settingsPanel?.SetActive(false);
            achievementsPanel?.SetActive(false);

            RefreshContinueButton();
        }

        // ── Wiring helper ─────────────────────────────────────────────────

        private static void WireButton(Button btn, UnityEngine.Events.UnityAction action)
        {
            if (btn != null) btn.onClick.AddListener(action);
        }

        // ── Button handlers ───────────────────────────────────────────────

        /// <summary>New Run — shows the inline OpeningDialogue boss cutscene.</summary>
        private void OnNewRun()
        {
            if (openingDialogue != null)
                openingDialogue.Show();
            else
                Load("Explorationscene");
        }

        /// <summary>HUB button — focuses the desk (the hub lives in this scene's right panel).</summary>
        private void OnHub() => slideController?.FocusDesk();

        /// <summary>Continue — restore any pre-encounter snapshot and resume exploration.</summary>
        private void OnContinue()
        {
            if (SaveManager.Instance == null) return;

            if (SaveManager.Instance.HasPreEncounterSnapshot())
                SaveManager.Instance.RestorePreEncounter();

            RunState run = SaveManager.Instance.CurrentRun;
            if (run == null || !run.isActive) return;

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

        /// <summary>Open fullscreen settings overlay.</summary>
        private void OnSettings() => settingsPanel?.SetActive(true);

        /// <summary>Open fullscreen achievements overlay and populate rows.</summary>
        private void OnAchievements()
        {
            if (achievementsPanel == null) return;
            achievementsPanel.SetActive(true);
            PopulateAchievements();
        }

        /// <summary>Quit — stops play mode in editor, quits build.</summary>
        private void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ── Panel close ───────────────────────────────────────────────────

        private void CloseSettings()     => settingsPanel?.SetActive(false);
        private void CloseAchievements() => achievementsPanel?.SetActive(false);

        // ── Continue visibility ───────────────────────────────────────────

        /// <summary>
        /// Shows Continue only when a valid, meaningful active run exists on disk.
        /// A run is considered valid if isActive is true AND it has either HP or deck cards.
        /// The pre-encounter snapshot is also checked with the same criteria.
        /// </summary>
        private void RefreshContinueButton()
        {
            if (continueButton == null) return;

            bool hasSave = false;

            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.LoadRun();
                hasSave = IsRunValid(SaveManager.Instance.CurrentRun)
                       || IsSnapshotValid();
            }

            continueButton.gameObject.SetActive(hasSave);
        }

        private static bool IsRunValid(RunState run) =>
            run != null
            && run.isActive
            && (run.playerMaxHP > 0 || (run.deckCardIds != null && run.deckCardIds.Count > 0));

        /// <summary>Deserialises the snapshot file and applies the same validity check.</summary>
        private static bool IsSnapshotValid()
        {
            if (!SaveManager.Instance.HasPreEncounterSnapshot()) return false;

            try
            {
                string path = System.IO.Path.Combine(
                    UnityEngine.Application.persistentDataPath, "pre_encounter_save.json");
                string json = System.IO.File.ReadAllText(path);
                RunState snap = UnityEngine.JsonUtility.FromJson<RunState>(json);
                return IsRunValid(snap);
            }
            catch
            {
                return false;
            }
        }

        // ── Achievement population ────────────────────────────────────────

        private void PopulateAchievements()
        {
            if (achievementListContent == null) return;

            // Clear previous rows
            foreach (Transform child in achievementListContent)
                Destroy(child.gameObject);

            MetaState meta    = SaveManager.Instance?.CurrentMeta;
            var unlocked      = meta?.unlockedAchievements ?? new List<string>();
            var unlockedSet   = new HashSet<string>(unlocked);

            if (achievementRegistry != null && achievementRegistry.achievements.Count > 0)
            {
                foreach (AchievementDefinition def in achievementRegistry.achievements)
                    BuildRow(def, unlockedSet.Contains(def.id));
            }
            else if (unlocked.Count > 0)
            {
                // Fallback: only show what's been unlocked, no registry
                foreach (string id in unlocked)
                    BuildFallbackRow(id, true);
            }
            else
            {
                BuildFallbackRow("No achievements unlocked yet.", false);
            }
        }

        /// <summary>Builds a fully-featured achievement row using registry data.</summary>
        private void BuildRow(AchievementDefinition def, bool isUnlocked)
        {
            // ── Row container ─────────────────────────────────────────────
            GameObject rowGO = new GameObject($"Achievement_{def.id}");
            rowGO.transform.SetParent(achievementListContent, false);

            RectTransform rowRT = rowGO.AddComponent<RectTransform>();
            rowRT.sizeDelta = new Vector2(0f, 72f);

            Image rowBg = rowGO.AddComponent<Image>();
            rowBg.color = isUnlocked ? UnlockedBgColor : LockedBgColor;

            LayoutElement le = rowGO.AddComponent<LayoutElement>();
            le.preferredHeight  = 72f;
            le.flexibleWidth    = 1f;
            le.minHeight        = 72f;

            HorizontalLayoutGroup hlg = rowGO.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing             = 16f;
            hlg.padding             = new RectOffset(16, 16, 12, 12);
            hlg.childAlignment      = TextAnchor.MiddleLeft;
            hlg.childControlHeight  = true;
            hlg.childControlWidth   = false;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = true;

            // ── Icon area ─────────────────────────────────────────────────
            GameObject iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(rowGO.transform, false);

            RectTransform iconRT = iconGO.AddComponent<RectTransform>();
            iconRT.sizeDelta = new Vector2(48f, 48f);

            Image iconImg = iconGO.AddComponent<Image>();
            LayoutElement iconLE = iconGO.AddComponent<LayoutElement>();
            iconLE.preferredWidth  = 48f;
            iconLE.preferredHeight = 48f;
            iconLE.flexibleWidth   = 0f;

            if (isUnlocked && def.icon != null)
            {
                iconImg.sprite = def.icon;
                iconImg.color  = Color.white;
            }
            else
            {
                // Locked: grey placeholder with ? text on top
                iconImg.color = new Color(0.25f, 0.25f, 0.28f, 1f);

                GameObject qGO = new GameObject("Question");
                qGO.transform.SetParent(iconGO.transform, false);

                RectTransform qRT = qGO.AddComponent<RectTransform>();
                qRT.anchorMin        = Vector2.zero;
                qRT.anchorMax        = Vector2.one;
                qRT.offsetMin        = Vector2.zero;
                qRT.offsetMax        = Vector2.zero;

                TextMeshProUGUI qTMP = qGO.AddComponent<TextMeshProUGUI>();
                qTMP.text           = "?";
                qTMP.fontSize       = 26f;
                qTMP.fontStyle      = FontStyles.Bold;
                qTMP.alignment      = TextAlignmentOptions.Center;
                qTMP.color          = new Color(0.55f, 0.55f, 0.6f, 1f);
                qTMP.raycastTarget  = false;
            }

            // ── Text column ───────────────────────────────────────────────
            GameObject textColGO = new GameObject("TextColumn");
            textColGO.transform.SetParent(rowGO.transform, false);

            LayoutElement textLE = textColGO.AddComponent<LayoutElement>();
            textLE.flexibleWidth = 1f;

            VerticalLayoutGroup vlg = textColGO.AddComponent<VerticalLayoutGroup>();
            vlg.spacing             = 4f;
            vlg.childAlignment      = TextAnchor.MiddleLeft;
            vlg.childControlWidth   = true;
            vlg.childControlHeight  = false;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;

            // Name
            GameObject nameGO = new GameObject("Name");
            nameGO.transform.SetParent(textColGO.transform, false);
            TextMeshProUGUI nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
            nameTMP.text           = isUnlocked ? def.displayName : "???";
            nameTMP.fontSize       = 15f;
            nameTMP.fontStyle      = FontStyles.Bold;
            nameTMP.color          = isUnlocked ? UnlockedTextColor : LockedTextColor;
            nameTMP.enableWordWrapping = false;
            nameTMP.raycastTarget  = false;
            nameGO.AddComponent<LayoutElement>().preferredHeight = 22f;

            // Description (only visible when unlocked)
            if (isUnlocked)
            {
                GameObject descGO = new GameObject("Description");
                descGO.transform.SetParent(textColGO.transform, false);
                TextMeshProUGUI descTMP = descGO.AddComponent<TextMeshProUGUI>();
                descTMP.text           = def.description;
                descTMP.fontSize       = 12f;
                descTMP.color          = new Color(0.70f, 0.70f, 0.65f, 1f);
                descTMP.enableWordWrapping = true;
                descTMP.raycastTarget  = false;
                descGO.AddComponent<LayoutElement>().preferredHeight = 18f;
            }

            // ── Tooltip on hover ──────────────────────────────────────────
            AchievementTooltip tooltip = rowGO.AddComponent<AchievementTooltip>();
            // Both locked and unlocked rows show a tooltip on hover
            string hoverText = isUnlocked
                ? $"<b>{def.displayName}</b>\n{def.description}"
                : $"<b>???</b>\n{def.description}";
            tooltip.Configure(hoverText);
        }

        /// <summary>Fallback row when no registry is assigned.</summary>
        private void BuildFallbackRow(string text, bool isUnlocked)
        {
            GameObject entryGO = new GameObject("AchievementEntry");
            entryGO.transform.SetParent(achievementListContent, false);

            Image bg = entryGO.AddComponent<Image>();
            bg.color = isUnlocked ? UnlockedBgColor : LockedBgColor;

            LayoutElement le = entryGO.AddComponent<LayoutElement>();
            le.preferredHeight = 48f;
            le.flexibleWidth   = 1f;

            TextMeshProUGUI tmp = entryGO.AddComponent<TextMeshProUGUI>();
            tmp.text           = text;
            tmp.fontSize       = 15f;
            tmp.color          = isUnlocked ? UnlockedTextColor : LockedTextColor;
            tmp.alignment      = TextAlignmentOptions.MidlineLeft;
            tmp.margin         = new Vector4(16f, 0f, 16f, 0f);
            tmp.enableWordWrapping = true;
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private static void Load(string scene)
        {
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.LoadSceneMenu(scene);
            else
                SceneManager.LoadScene(scene);
        }

        private void EnsureSaveManager()
        {
            if (SaveManager.Instance == null)
                new GameObject("SaveManager").AddComponent<SaveManager>();

            SaveManager.Instance.LoadMeta();
        }
    }
}
