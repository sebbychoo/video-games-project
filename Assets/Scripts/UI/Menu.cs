using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Pause menu controller. Escape key pauses during exploration and combat.
/// Options: Resume, Hub Office, Settings, View Deck, View Tools, Quit to Main Menu.
/// Freezes all gameplay while paused.
/// Requirements: 39.1–39.9
/// </summary>
public class Menu : MonoBehaviour
{
    [Header("Main Pause Canvas")]
    public GameObject menuCanvas;
    public GameObject playerCapsule;
    public GameObject playerFollowCamera;

    [Header("Pause Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button hubOfficeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button viewDeckButton;
    [SerializeField] private Button viewToolsButton;
    [SerializeField] private Button quitToMenuButton;

    [Header("Settings Panel")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Button settingsBackButton;

    [Header("View Deck Panel")]
    [SerializeField] private GameObject viewDeckPanel;
    [SerializeField] private Transform deckGridContent;
    [SerializeField] private GameObject deckCardEntryPrefab;
    [SerializeField] private Button deckBackButton;

    [Header("View Tools Panel")]
    [SerializeField] private GameObject viewToolsPanel;
    [SerializeField] private Transform toolsListContent;
    [SerializeField] private GameObject toolEntryPrefab;
    [SerializeField] private Button toolsBackButton;

    private bool menuOpen = false;

    private void Start()
    {
        menuCanvas.SetActive(false);

        // Hide sub-panels
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (viewDeckPanel != null) viewDeckPanel.SetActive(false);
        if (viewToolsPanel != null) viewToolsPanel.SetActive(false);

        // Wire buttons
        if (resumeButton != null) resumeButton.onClick.AddListener(SetPause);
        if (hubOfficeButton != null) hubOfficeButton.onClick.AddListener(OnHubOffice);
        if (settingsButton != null) settingsButton.onClick.AddListener(OnSettings);
        if (viewDeckButton != null) viewDeckButton.onClick.AddListener(OnViewDeck);
        if (viewToolsButton != null) viewToolsButton.onClick.AddListener(OnViewTools);
        if (quitToMenuButton != null) quitToMenuButton.onClick.AddListener(OnQuitToMenu);

        // Sub-panel back buttons
        if (settingsBackButton != null) settingsBackButton.onClick.AddListener(CloseSettings);
        if (deckBackButton != null) deckBackButton.onClick.AddListener(CloseDeck);
        if (toolsBackButton != null) toolsBackButton.onClick.AddListener(CloseTools);
    }

    private void Update()
    {
        // Don't open pause menu while interacting with any exploration UI (Req 39.7)
        if (Input.GetKeyDown(KeyCode.Escape)
            && !CardBattle.WorkBoxTrigger.IsInteracting
            && !CardBattle.BathroomShopTrigger.IsInteracting
            && !CardBattle.BreakRoomTradeTrigger.IsInteracting
            && !CardBattle.BossCutsceneController.IsInteracting)
        {
            // If a sub-panel is open, close it instead of toggling pause
            if (menuOpen && IsSubPanelOpen())
            {
                CloseAllSubPanels();
                return;
            }
            SetPause();
        }
    }

    /// <summary>Toggle pause state (Req 39.1, 39.3, 39.7).</summary>
    public void SetPause()
    {
        menuOpen = !menuOpen;

        menuCanvas.SetActive(menuOpen);
        Time.timeScale = menuOpen ? 0f : 1f; // Req 39.7: freeze all gameplay

        Cursor.lockState = menuOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = menuOpen;

        if (playerCapsule != null)
            playerCapsule.SetActive(!menuOpen);

        if (playerFollowCamera != null)
            playerFollowCamera.SetActive(!menuOpen);

        // Close sub-panels when unpausing
        if (!menuOpen)
            CloseAllSubPanels();
    }

    // ── Button Handlers ───────────────────────────────────────────────

    /// <summary>Hub Office → open hub scene, preserve run state (Req 39.4).</summary>
    private void OnHubOffice()
    {
        // Save current run state before leaving
        if (CardBattle.SaveManager.Instance != null)
            CardBattle.SaveManager.Instance.SaveRun();

        Time.timeScale = 1f;
        menuOpen = false;

        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadSceneMenu("HubOffice");
        else
            SceneManager.LoadScene("HubOffice");
    }

    /// <summary>Settings panel (Req 39.5).</summary>
    private void OnSettings()
    {
        CloseAllSubPanels();
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    /// <summary>View Deck → scrollable grid of all deck cards (Req 39.8).</summary>
    private void OnViewDeck()
    {
        CloseAllSubPanels();
        if (viewDeckPanel != null)
        {
            viewDeckPanel.SetActive(true);
            PopulateDeckView();
        }
    }

    /// <summary>View Tools → all collected Tools with details (Req 39.9).</summary>
    private void OnViewTools()
    {
        CloseAllSubPanels();
        if (viewToolsPanel != null)
        {
            viewToolsPanel.SetActive(true);
            PopulateToolsView();
        }
    }

    /// <summary>Quit to Main Menu → save run state, return to menu (Req 39.6).</summary>
    private void OnQuitToMenu()
    {
        // Save run state before quitting
        if (CardBattle.SaveManager.Instance != null)
        {
            CardBattle.SaveManager.Instance.CurrentRun.isActive = true;
            CardBattle.SaveManager.Instance.SaveRun();
        }

        Time.timeScale = 1f;
        menuOpen = false;

        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadSceneMenu("Menu");
        else
            SceneManager.LoadScene("Menu");
    }

    // ── Legacy methods for backward compatibility ─────────────────────

    public void Restart()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SceneManager.LoadScene(0);
    }

    public void Exit()
    {
        Application.Quit();
    }

    // ── Sub-panel Management ──────────────────────────────────────────

    private bool IsSubPanelOpen()
    {
        return (settingsPanel != null && settingsPanel.activeSelf)
            || (viewDeckPanel != null && viewDeckPanel.activeSelf)
            || (viewToolsPanel != null && viewToolsPanel.activeSelf);
    }

    private void CloseAllSubPanels()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (viewDeckPanel != null) viewDeckPanel.SetActive(false);
        if (viewToolsPanel != null) viewToolsPanel.SetActive(false);
    }

    private void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    private void CloseDeck()
    {
        if (viewDeckPanel != null) viewDeckPanel.SetActive(false);
    }

    private void CloseTools()
    {
        if (viewToolsPanel != null) viewToolsPanel.SetActive(false);
    }

    // ── View Deck Population (Req 39.8) ───────────────────────────────

    private void PopulateDeckView()
    {
        if (deckGridContent == null) return;

        // Clear existing entries
        foreach (Transform child in deckGridContent)
            Destroy(child.gameObject);

        CardBattle.RunState run = CardBattle.SaveManager.Instance != null
            ? CardBattle.SaveManager.Instance.CurrentRun
            : null;

        if (run == null || run.deckCardIds == null || run.deckCardIds.Count == 0)
        {
            CreateTextEntry(deckGridContent, "No cards in deck.");
            return;
        }

        // Sort alphabetically for display
        List<string> sortedIds = new List<string>(run.deckCardIds);
        sortedIds.Sort();

        foreach (string cardId in sortedIds)
        {
            CardBattle.CardData card = Resources.Load<CardBattle.CardData>(cardId);
            if (card == null)
                card = Resources.Load<CardBattle.CardData>("Cards/" + cardId);

            if (deckCardEntryPrefab != null)
            {
                GameObject entry = Instantiate(deckCardEntryPrefab, deckGridContent);
                // Try to populate prefab fields
                TextMeshProUGUI label = entry.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                {
                    if (card != null)
                        label.text = $"{card.cardName}  [{card.cardType}]  OT:{card.overtimeCost}";
                    else
                        label.text = cardId;
                }
                Image img = entry.transform.Find("CardSprite")?.GetComponent<Image>();
                if (img != null && card != null && card.cardSprite != null)
                    img.sprite = card.cardSprite;
            }
            else
            {
                string displayText = card != null
                    ? $"{card.cardName}  [{card.cardType}]  OT:{card.overtimeCost}"
                    : cardId;
                CreateTextEntry(deckGridContent, displayText);
            }
        }
    }

    // ── View Tools Population (Req 39.9) ──────────────────────────────

    private void PopulateToolsView()
    {
        if (toolsListContent == null) return;

        // Clear existing entries
        foreach (Transform child in toolsListContent)
            Destroy(child.gameObject);

        CardBattle.RunState run = CardBattle.SaveManager.Instance != null
            ? CardBattle.SaveManager.Instance.CurrentRun
            : null;

        if (run == null || run.toolIds == null || run.toolIds.Count == 0)
        {
            CreateTextEntry(toolsListContent, "No tools collected.");
            return;
        }

        foreach (string toolId in run.toolIds)
        {
            CardBattle.ToolData tool = Resources.Load<CardBattle.ToolData>(toolId);
            if (tool == null)
                tool = Resources.Load<CardBattle.ToolData>("Tools/" + toolId);

            if (toolEntryPrefab != null)
            {
                GameObject entry = Instantiate(toolEntryPrefab, toolsListContent);
                // Name
                TextMeshProUGUI nameLabel = entry.transform.Find("ToolName")?.GetComponent<TextMeshProUGUI>();
                if (nameLabel != null)
                    nameLabel.text = tool != null ? tool.toolName : toolId;
                // Description
                TextMeshProUGUI descLabel = entry.transform.Find("ToolDescription")?.GetComponent<TextMeshProUGUI>();
                if (descLabel != null && tool != null)
                    descLabel.text = tool.description;
                // Sprite
                Image spriteImg = entry.transform.Find("ToolSprite")?.GetComponent<Image>();
                if (spriteImg != null && tool != null && tool.toolSprite != null)
                    spriteImg.sprite = tool.toolSprite;
            }
            else
            {
                string displayText = tool != null
                    ? $"{tool.toolName} — {tool.description}"
                    : toolId;
                CreateTextEntry(toolsListContent, displayText);
            }
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────

    private void CreateTextEntry(Transform parent, string text)
    {
        GameObject entryGO = new GameObject("Entry");
        entryGO.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = entryGO.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 16f;
        tmp.color = Color.white;
        tmp.enableWordWrapping = true;

        var le = entryGO.AddComponent<UnityEngine.UI.LayoutElement>();
        le.preferredHeight = 28f;
        le.flexibleWidth = 1f;
    }
}
