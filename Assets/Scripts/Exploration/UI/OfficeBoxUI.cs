using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CardBattle;

/// <summary>
/// Controls the new Office Box screen-space UI.
/// Attach to the OfficeBoxCanvas. WorkBoxTrigger shows/hides this.
/// </summary>
public class OfficeBoxUI : MonoBehaviour
{
    public static OfficeBoxUI Instance { get; private set; }

    [Header("Header")]
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text floorText;
    [SerializeField] Image stampImage;
    [SerializeField] Button closeButton;

    [Header("Left Panel — Box Contents")]
    [SerializeField] TMP_Text boxContentsLabel;
    [SerializeField] Transform cardRowContainer;
    [SerializeField] GameObject cardRowTemplate;

    [Header("Right Panel — Your Deck")]
    [SerializeField] TMP_Text deckLabel;
    [SerializeField] TMP_Text capacityText;
    [SerializeField] Transform deckRowContainer;
    [SerializeField] GameObject deckRowTemplate;

    [Header("Bottom")]
    [SerializeField] TMP_Text statusText;
    [SerializeField] Button addButton;
    [SerializeField] Button mehButton;

    [Header("Stamp Sprites (optional)")]
    [SerializeField] Sprite stampSmall;
    [SerializeField] Sprite stampBig;
    [SerializeField] Sprite stampHuge;

    private WorkBox _currentBox;
    private int _selectedIndex = -1;
    private List<GameObject> _spawnedCardRows = new List<GameObject>();
    private List<GameObject> _spawnedDeckRows = new List<GameObject>();

    private void Awake()
    {
        Instance = this;
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClicked);
        if (addButton != null)
            addButton.onClick.AddListener(OnBottomAddClicked);
        if (mehButton != null)
            mehButton.onClick.AddListener(OnBottomMehClicked);
        ShowBottomButtons(false);

        // Auto-setup scroll areas so it works regardless of manual Inspector config
        SetupScrollArea(cardRowContainer);
        SetupScrollArea(deckRowContainer);

        gameObject.SetActive(false);
    }

    private void SetupScrollArea(Transform container)
    {
        if (container == null) return;
        Transform scrollParent = container.parent;
        if (scrollParent == null) return;

        // Ensure Scroll Rect
        var sr = scrollParent.GetComponent<ScrollRect>();
        if (sr == null) sr = scrollParent.gameObject.AddComponent<ScrollRect>();
        sr.content = container.GetComponent<RectTransform>();
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 30f;

        // Ensure Rect Mask 2D
        if (scrollParent.GetComponent<UnityEngine.UI.RectMask2D>() == null)
            scrollParent.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();

        // Ensure Vertical Layout Group on container
        var vlg = container.GetComponent<VerticalLayoutGroup>();
        if (vlg == null) vlg = container.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 6;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;

        // Ensure Content Size Fitter
        var csf = container.GetComponent<ContentSizeFitter>();
        if (csf == null) csf = container.gameObject.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Set container anchor to top-stretch with top pivot
        var rt = container.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(0, rt.offsetMin.y);
        rt.offsetMax = new Vector2(0, 0);
    }

    /// <summary>Show the UI for a specific WorkBox.</summary>
    public void Show(WorkBox box)
    {
        _currentBox = box;
        _selectedIndex = -1;
        ShowBottomButtons(false);
        gameObject.SetActive(true);

        int floor = 1;
        if (SaveManager.Instance != null && SaveManager.Instance.CurrentRun != null)
            floor = SaveManager.Instance.CurrentRun.currentFloor;

        if (floorText != null)
            floorText.text = $"Floor {floor}";

        // Stamp based on box size
        if (stampImage != null)
        {
            Sprite s = box.Size == WorkBoxSize.Huge ? stampHuge :
                       box.Size == WorkBoxSize.Big ? stampBig : stampSmall;
            if (s != null) stampImage.sprite = s;
        }

        if (boxContentsLabel != null)
            boxContentsLabel.text = $"BOX CONTENTS";

        PopulateCardRows();
        PopulateDeckRows();
        UpdateStatus();
    }

    /// <summary>Hide the UI.</summary>
    public void Hide()
    {
        _currentBox = null;
        gameObject.SetActive(false);
        ClearRows(_spawnedCardRows);
        ClearRows(_spawnedDeckRows);
    }

    private void OnCloseClicked()
    {
        if (_currentBox != null)
            _currentBox.OnCloseButtonClicked();
    }

    // ── Card rows (box contents) ──────────────────────────────

    private void PopulateCardRows()
    {
        ClearRows(_spawnedCardRows);
        if (_currentBox == null || cardRowTemplate == null) return;

        var cards = _currentBox.Cards;
        for (int i = 0; i < cards.Count; i++)
        {
            GameObject row = Instantiate(cardRowTemplate, cardRowContainer);
            row.SetActive(true);
            _spawnedCardRows.Add(row);
            EnsureRowHeight(row, 80);

            var card = cards[i];
            int index = i;

            // Card icon
            var icon = row.transform.Find("CardIcon")?.GetComponent<Image>();
            // Card name
            var nameText = row.transform.Find("CardName")?.GetComponent<TMP_Text>();
            // Stat line
            var statText = row.transform.Find("StatLine")?.GetComponent<TMP_Text>();
            // Rarity badge
            var badge = row.transform.Find("RarityBadge")?.GetComponent<Image>();
            var badgeText = row.transform.Find("RarityBadge/Text")?.GetComponent<TMP_Text>();
            // Buttons
            var addBtn = row.transform.Find("AddButton")?.GetComponent<Button>();
            var mehBtn = row.transform.Find("MehButton")?.GetComponent<Button>();

            CardData cardData = null;
            if (card.revealState == WorkBox.RevealState.FullReveal)
                cardData = Resources.Load<CardData>(card.cardId);

            // Fill in data
            if (card.kept)
            {
                if (nameText != null)
                {
                    nameText.text = cardData != null ? cardData.cardName : card.cardId;
                    nameText.color = new Color(0.5f, 0.5f, 0.5f);
                }
                if (statText != null)
                {
                    statText.text = "Added to deck";
                    statText.color = new Color(0.45f, 0.45f, 0.45f);
                }
                SetRarityBadge(badge, badgeText, card.rarity);
                if (icon != null && cardData != null && cardData.cardSprite != null)
                {
                    icon.sprite = cardData.cardSprite;
                    icon.color = new Color(1f, 1f, 1f, 0.4f);
                }
                if (addBtn != null) addBtn.gameObject.SetActive(false);
                if (mehBtn != null) mehBtn.gameObject.SetActive(false);
                var rowImg = row.GetComponent<Image>();
                if (rowImg != null) rowImg.color = new Color(0.75f, 0.75f, 0.75f, 0.6f);
            }
            else if (card.revealState == WorkBox.RevealState.FullReveal && cardData != null)
            {
                if (nameText != null)
                {
                    nameText.text = cardData.cardName;
                    nameText.color = GetRarityTextColor(card.rarity);
                }
                if (statText != null)
                {
                    statText.text = GetCardStatLine(cardData);
                    statText.color = new Color(0.35f, 0.3f, 0.25f);
                }
                SetRarityBadge(badge, badgeText, card.rarity);
                if (icon != null && cardData.cardSprite != null)
                    icon.sprite = cardData.cardSprite;

                // Row background tinted by rarity
                var rowImg = row.GetComponent<Image>();
                if (rowImg != null)
                {
                    bool isSelected = (_selectedIndex == index);
                    rowImg.color = isSelected
                        ? GetRarityRowColor(card.rarity) * 1.1f
                        : GetRarityRowColor(card.rarity);
                }

                // Hide per-row buttons — use bottom buttons instead
                if (addBtn != null) addBtn.gameObject.SetActive(false);
                if (mehBtn != null) mehBtn.gameObject.SetActive(false);

                // Click row to select it
                var selectBtn = row.GetComponent<Button>();
                if (selectBtn == null) selectBtn = row.AddComponent<Button>();
                selectBtn.targetGraphic = row.GetComponent<Image>();
                selectBtn.onClick.AddListener(() => SelectCard(index));
            }
            else
            {
                // Not yet revealed — whole row is grey and clickable
                if (nameText != null) nameText.text = "???";
                if (statText != null) statText.text = "Click to reveal";

                // Use reveal state color for the whole row
                var rowImg = row.GetComponent<Image>();
                Color revealCol = GetRevealRowColor(card.revealState);
                if (rowImg != null) rowImg.color = revealCol;

                // Text color contrasts with row
                float lum = 0.299f * revealCol.r + 0.587f * revealCol.g + 0.114f * revealCol.b;
                Color textCol = lum > 0.5f ? new Color(0.2f, 0.2f, 0.2f) : Color.white;
                if (nameText != null) nameText.color = textCol;
                if (statText != null) statText.color = new Color(textCol.r, textCol.g, textCol.b, 0.7f);

                SetRevealColor(badge, card.revealState);
                if (addBtn != null) addBtn.gameObject.SetActive(false);
                if (mehBtn != null) mehBtn.gameObject.SetActive(false);

                var btn = row.GetComponent<Button>();
                if (btn == null) btn = row.AddComponent<Button>();
                btn.targetGraphic = rowImg;
                btn.onClick.AddListener(() =>
                {
                    _currentBox.RevealCard(index);
                    PopulateCardRows();
                });
            }
        }
    }

    private void OnAddClicked(int index)
    {
        if (_currentBox == null) return;
        _currentBox.KeepCard(index);
        _selectedIndex = -1;
        ShowBottomButtons(false);
        PopulateCardRows();
        PopulateDeckRows();
        UpdateStatus();
    }

    private void OnMehClicked(int index)
    {
        if (_currentBox == null) return;
        _currentBox.LeaveCard(index);
        _selectedIndex = -1;
        ShowBottomButtons(false);
        PopulateCardRows();
    }

    private void SelectCard(int index)
    {
        _selectedIndex = index;
        ShowBottomButtons(true);
        PopulateCardRows(); // refresh to show highlight
    }

    private void OnBottomAddClicked()
    {
        if (_selectedIndex >= 0)
            OnAddClicked(_selectedIndex);
    }

    private void OnBottomMehClicked()
    {
        if (_selectedIndex >= 0)
            OnMehClicked(_selectedIndex);
    }

    private void ShowBottomButtons(bool show)
    {
        if (addButton != null) addButton.gameObject.SetActive(show);
        if (mehButton != null) mehButton.gameObject.SetActive(show);
    }

    // ── Deck rows (your deck) ─────────────────────────────────

    private void PopulateDeckRows()
    {
        ClearRows(_spawnedDeckRows);
        if (deckRowTemplate == null) return;

        RunState run = SaveManager.Instance != null ? SaveManager.Instance.CurrentRun : null;
        if (run == null || run.deckCardIds == null) return;

        int maxDeck = 25;
        if (SaveManager.Instance != null)
        {
            var cfg = Resources.Load<GameConfig>("GameConfig");
            if (cfg != null) maxDeck = cfg.maximumDeckSize;
        }

        if (capacityText != null)
            capacityText.text = $"{run.deckCardIds.Count} / {maxDeck}";

        foreach (string cardId in run.deckCardIds)
        {
            GameObject row = Instantiate(deckRowTemplate, deckRowContainer);
            row.SetActive(true);
            _spawnedDeckRows.Add(row);
            EnsureRowHeight(row, 80);

            CardData cardData = Resources.Load<CardData>(cardId);

            var nameText = row.transform.Find("CardName")?.GetComponent<TMP_Text>();
            var statText = row.transform.Find("StatLine")?.GetComponent<TMP_Text>();
            var badge = row.transform.Find("RarityBadge")?.GetComponent<Image>();
            var badgeText = row.transform.Find("RarityBadge/Text")?.GetComponent<TMP_Text>();
            var icon = row.transform.Find("CardIcon")?.GetComponent<Image>();

            if (cardData != null)
            {
                if (nameText != null)
                {
                    nameText.text = cardData.cardName;
                    nameText.color = GetRarityTextColor(cardData.cardRarity);
                }
                if (statText != null)
                {
                    statText.text = GetCardStatLine(cardData);
                    statText.color = new Color(0.35f, 0.3f, 0.25f);
                }
                SetRarityBadge(badge, badgeText, cardData.cardRarity);
                if (icon != null && cardData.cardSprite != null)
                    icon.sprite = cardData.cardSprite;

                // Tint row by rarity
                var rowImg = row.GetComponent<Image>();
                if (rowImg != null) rowImg.color = GetRarityRowColor(cardData.cardRarity);
            }
            else
            {
                if (nameText != null) nameText.text = cardId;
                if (statText != null) statText.text = "";
            }
        }
    }

    private void UpdateStatus()
    {
        if (statusText == null) return;
        RunState run = SaveManager.Instance != null ? SaveManager.Instance.CurrentRun : null;
        int count = run != null && run.deckCardIds != null ? run.deckCardIds.Count : 0;
        int max = 25;
        var cfg = Resources.Load<GameConfig>("GameConfig");
        if (cfg != null) max = cfg.maximumDeckSize;
        statusText.text = $"{count} / {max} cards in deck";
    }

    // ── Helpers ────────────────────────────────────────────────

    private static void ClearRows(List<GameObject> rows)
    {
        foreach (var r in rows)
            if (r != null) Destroy(r);
        rows.Clear();
    }

    private static void EnsureRowHeight(GameObject row, float height)
    {
        var le = row.GetComponent<LayoutElement>();
        if (le == null) le = row.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.minHeight = height;
    }

    private static string GetCardStatLine(CardData card)
    {
        string line = "";
        if (card.effectValue != 0)
        {
            string type = card.cardType == CardType.Attack ? "ATK" :
                          card.cardType == CardType.Defense ? "DEF" : "EFF";
            line += $"+{card.effectValue} {type}";
        }
        if (card.overtimeCost > 0)
            line += (line.Length > 0 ? " · " : "") + $"{card.overtimeCost} OT";
        return line.Length > 0 ? line : card.description ?? "";
    }

    private static void SetRarityBadge(Image badge, TMP_Text text, CardRarity rarity)
    {
        if (badge == null) return;
        badge.color = rarity switch
        {
            CardRarity.Common => new Color(0.6f, 0.6f, 0.6f),
            CardRarity.Rare => new Color(0.9f, 0.8f, 0.3f),
            CardRarity.Legendary => new Color(0.8f, 0.2f, 0.2f),
            CardRarity.Unknown => new Color(0.4f, 0.1f, 0.6f),
            _ => Color.gray
        };
        if (text != null) text.text = rarity.ToString().ToUpper();
    }

    private static void SetRevealColor(Image badge, WorkBox.RevealState state)
    {
        if (badge == null) return;
        badge.color = state switch
        {
            WorkBox.RevealState.Hidden => Color.gray,
            WorkBox.RevealState.Yellow => new Color(1f, 0.95f, 0.7f),
            WorkBox.RevealState.Red => new Color(0.8f, 0.2f, 0.2f),
            WorkBox.RevealState.Black => new Color(0.1f, 0.1f, 0.1f),
            _ => Color.gray
        };
    }

    /// <summary>Row background color based on rarity (revealed cards).</summary>
    private static Color GetRarityRowColor(CardRarity rarity)
    {
        return rarity switch
        {
            CardRarity.Common => new Color(0.95f, 0.93f, 0.88f),
            CardRarity.Rare => new Color(1f, 0.97f, 0.82f),
            CardRarity.Legendary => new Color(1f, 0.88f, 0.85f),
            CardRarity.Unknown => new Color(0.9f, 0.85f, 0.98f),
            _ => new Color(0.95f, 0.93f, 0.88f)
        };
    }

    /// <summary>Card name text color based on rarity.</summary>
    private static Color GetRarityTextColor(CardRarity rarity)
    {
        return rarity switch
        {
            CardRarity.Common => new Color(0.25f, 0.25f, 0.25f),
            CardRarity.Rare => new Color(0.55f, 0.45f, 0.05f),
            CardRarity.Legendary => new Color(0.7f, 0.1f, 0.1f),
            CardRarity.Unknown => new Color(0.35f, 0.1f, 0.55f),
            _ => new Color(0.2f, 0.2f, 0.2f)
        };
    }

    /// <summary>Row color for unrevealed cards based on reveal progress.</summary>
    private static Color GetRevealRowColor(WorkBox.RevealState state)
    {
        return state switch
        {
            WorkBox.RevealState.Hidden => new Color(0.55f, 0.55f, 0.55f),
            WorkBox.RevealState.Yellow => new Color(0.85f, 0.8f, 0.55f),
            WorkBox.RevealState.Red => new Color(0.7f, 0.25f, 0.25f),
            WorkBox.RevealState.Black => new Color(0.15f, 0.15f, 0.15f),
            _ => new Color(0.55f, 0.55f, 0.55f)
        };
    }
}
