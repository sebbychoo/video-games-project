using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CardBattle
{
    /// <summary>
    /// Slay the Spire-style info panel that appears when hovering a card.
    /// Shows stacked entries for card name, type, cost, and description.
    /// Positions above the card, flipping left/right based on screen side.
    /// </summary>
    public class CardDescriptionTooltip : MonoBehaviour
    {
        public static CardDescriptionTooltip Instance { get; private set; }

        [Header("Panel")]
        [SerializeField] private RectTransform tooltipPanel;

        [Header("Entry Prefab")]
        [SerializeField] private GameObject entryPrefab;

        [Header("Layout")]
        [SerializeField] private float entrySpacing = 4f;
        [SerializeField] private float panelPadding = 8f;
        [SerializeField] private float panelWidth = 350f;

        [Header("Colors")]
        [SerializeField] private Color panelColor = new Color(0.12f, 0.12f, 0.14f, 0.95f);
        [SerializeField] private Color entryBgColor = new Color(0.18f, 0.18f, 0.2f, 1f);
        [SerializeField] private Color nameColor = new Color(1f, 0.85f, 0.4f);
        [SerializeField] private Color descColor = new Color(0.85f, 0.85f, 0.85f);

        [Header("Text Settings")]
        [SerializeField] private float fontSize = 18f;
        [SerializeField] private TMP_FontAsset fontAsset;

        private RectTransform _canvasRect;
        private Canvas _canvas;
        private bool _showing;
        private RectTransform _currentCardRect;
        private float _hideTimer;
        private const float HideDelayDuration = 0.15f;
        private bool _pendingHide;

        private void Awake()
        {
            Instance = this;
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas != null)
                _canvasRect = _canvas.GetComponent<RectTransform>();

            if (tooltipPanel != null)
            {
                CanvasGroup cg = tooltipPanel.GetComponent<CanvasGroup>();
                if (cg == null) cg = tooltipPanel.gameObject.AddComponent<CanvasGroup>();
                cg.blocksRaycasts = false;
                cg.interactable = false;
            }

            Hide();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            // Process pending hide — only hide if no new Show came in
            if (_pendingHide)
            {
                _hideTimer -= Time.deltaTime;
                if (_hideTimer <= 0f)
                {
                    _pendingHide = false;
                    _showing = false;
                    _currentCardRect = null;
                    if (tooltipPanel != null)
                        tooltipPanel.gameObject.SetActive(false);
                }
                return;
            }

            if (!_showing) return;
            if (_currentCardRect == null) { HideImmediate(); return; }
            PositionAboveCard();
        }

        public void Show(string description, RectTransform cardRect)
        {
            Show(null, description, cardRect);
        }

        public void Show(CardData data, string description, RectTransform cardRect)
        {
            if (string.IsNullOrEmpty(description) && data == null) return;

            // Cancel any pending hide — we're switching to a new card
            _pendingHide = false;
            _hideTimer = 0f;
            _showing = true;

            _currentCardRect = cardRect;
            ClearEntries();
            BuildEntries(data, description);

            if (tooltipPanel != null)
            {
                // Force non-stretching anchors so width actually works
                tooltipPanel.anchorMin = new Vector2(0.5f, 1f);
                tooltipPanel.anchorMax = new Vector2(0.5f, 1f);
                tooltipPanel.pivot = new Vector2(0.5f, 1f);
                tooltipPanel.gameObject.SetActive(true);
                tooltipPanel.SetAsLastSibling();
                tooltipPanel.sizeDelta = new Vector2(panelWidth, tooltipPanel.sizeDelta.y);
            }
            _showing = true;
            PositionAboveCard();
        }

        public void Hide()
        {
            // Don't hide immediately — start a delay so switching between cards is smooth
            _pendingHide = true;
            _hideTimer = HideDelayDuration;
        }

        private void HideImmediate()
        {
            _pendingHide = false;
            _hideTimer = 0f;
            _showing = false;
            _currentCardRect = null;
            if (tooltipPanel != null)
                tooltipPanel.gameObject.SetActive(false);
        }

        private void BuildEntries(CardData data, string description)
        {
            if (tooltipPanel == null) return;

            // Set panel width
            tooltipPanel.sizeDelta = new Vector2(panelWidth, tooltipPanel.sizeDelta.y);

            // Set panel background
            Image panelImg = tooltipPanel.GetComponent<Image>();
            if (panelImg != null) panelImg.color = panelColor;

            float yOffset = -panelPadding;

            if (data != null)
            {
                // Card type entry
                string typeLabel = data.cardType.ToString();
                if (data.cardType == CardType.Attack)
                    typeLabel += $"  <color=#CCCCCC>DMG: {data.effectValue}</color>";
                else if (data.cardType == CardType.Utility)
                    typeLabel += $"  <color=#CCCCCC>{data.utilityEffectType}: {data.effectValue}</color>";
                else if (data.cardType == CardType.Effect && !string.IsNullOrEmpty(data.statusEffectId))
                    typeLabel += $"  <color=#CCCCCC>{data.statusEffectId} x{data.statusDuration}t</color>";

                yOffset = AddEntry(typeLabel, "", yOffset, true);

                // Cost entry
                yOffset = AddEntry($"Overtime Cost: {data.overtimeCost}", "", yOffset, false);
            }

            // Description entry
            if (!string.IsNullOrEmpty(description))
            {
                yOffset = AddEntry("", $"<i>{description}</i>", yOffset, false);
            }

            // Resize panel to fit
            float totalHeight = Mathf.Abs(yOffset) + panelPadding;
            tooltipPanel.sizeDelta = new Vector2(panelWidth, totalHeight);
        }

        private float AddEntry(string title, string body, float yOffset, bool isHeader)
        {
            if (entryPrefab != null)
            {
                GameObject entry = Instantiate(entryPrefab, tooltipPanel);
                RectTransform rt = entry.GetComponent<RectTransform>();

                // Try to find title and body text in the prefab
                TextMeshProUGUI[] texts = entry.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length >= 2)
                {
                    texts[0].text = title;
                    texts[0].color = nameColor;
                    texts[1].text = body;
                    texts[1].color = descColor;
                }
                else if (texts.Length == 1)
                {
                    texts[0].text = !string.IsNullOrEmpty(title) ? title : body;
                    texts[0].color = isHeader ? nameColor : descColor;
                }

                return yOffset; // Let VerticalLayoutGroup handle positioning
            }

            // Fallback: create entries from scratch
            GameObject go = new GameObject("Entry", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(tooltipPanel, false);
            RectTransform entryRT = go.GetComponent<RectTransform>();
            Image bg = go.GetComponent<Image>();
            bg.color = entryBgColor;
            bg.raycastTarget = false;

            // Create text
            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
            RectTransform textRT = textGo.GetComponent<RectTransform>();

            string fullText = "";
            if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(body))
                fullText = $"<color=#{ColorUtility.ToHtmlStringRGB(nameColor)}>{title}</color>\n{body}";
            else if (!string.IsNullOrEmpty(title))
                fullText = $"<color=#{ColorUtility.ToHtmlStringRGB(nameColor)}>{title}</color>";
            else
                fullText = body;

            tmp.text = fullText;
            tmp.fontSize = fontSize;
            tmp.color = descColor;
            tmp.enableWordWrapping = true;
            tmp.raycastTarget = false;
            tmp.margin = new Vector4(6f, 4f, 6f, 4f);
            if (fontAsset != null) tmp.font = fontAsset;

            // Size the text area
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;

            // Force layout to get preferred height
            float prefHeight = tmp.GetPreferredValues(fullText, panelWidth - 12f, 0f).y + 10f;

            entryRT.anchorMin = new Vector2(0f, 1f);
            entryRT.anchorMax = new Vector2(1f, 1f);
            entryRT.pivot = new Vector2(0.5f, 1f);
            entryRT.anchoredPosition = new Vector2(0f, yOffset);
            entryRT.sizeDelta = new Vector2(0f, prefHeight);

            return yOffset - prefHeight - entrySpacing;
        }

        private void ClearEntries()
        {
            if (tooltipPanel == null) return;
            for (int i = tooltipPanel.childCount - 1; i >= 0; i--)
                Destroy(tooltipPanel.GetChild(i).gameObject);
        }

        private void PositionAboveCard()
        {
            if (tooltipPanel == null || _canvas == null || _currentCardRect == null) return;

            Vector3[] corners = new Vector3[4];
            _currentCardRect.GetWorldCorners(corners);

            float cardTopY = corners[1].y;
            float cardCenterX = (corners[0].x + corners[3].x) * 0.5f;
            float screenMidX = Screen.width * 0.5f;

            Vector2 pos = new Vector2(cardCenterX, cardTopY + 10f);

            float tipWidth = tooltipPanel.rect.width * _canvas.scaleFactor;
            if (cardCenterX > screenMidX)
                pos.x -= tipWidth * 0.5f + 20f;
            else
                pos.x += tipWidth * 0.5f + 20f;

            if (_canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                tooltipPanel.position = pos;
            else
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRect, pos, _canvas.worldCamera, out Vector2 lp);
                tooltipPanel.anchoredPosition = lp;
            }
        }
    }
}
