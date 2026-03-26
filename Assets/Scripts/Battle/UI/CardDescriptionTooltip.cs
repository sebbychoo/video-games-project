using UnityEngine;
using TMPro;

namespace CardBattle
{
    /// <summary>
    /// Floating tooltip that shows card description above the hovered card.
    /// Flips left/right based on which side of the screen the card is on.
    /// Place on a Canvas child with a TextMeshProUGUI. Hidden by default.
    /// </summary>
    public class CardDescriptionTooltip : MonoBehaviour
    {
        public static CardDescriptionTooltip Instance { get; private set; }

        [SerializeField] private TextMeshProUGUI tooltipText;
        [SerializeField] private RectTransform tooltipPanel;
        [SerializeField] private Vector2 offset = new Vector2(15f, -15f);

        private RectTransform _canvasRect;
        private Canvas _canvas;
        private bool _showing;
        private RectTransform _currentCardRect;

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
            if (!_showing) return;
            PositionAboveCard();
        }

        public void Show(string description)
        {
            Show(description, null);
        }

        public void Show(string description, RectTransform cardRect)
        {
            if (string.IsNullOrEmpty(description)) return;
            if (tooltipText != null)
                tooltipText.text = $"<i>{description}</i>";
            _currentCardRect = cardRect;
            if (tooltipPanel != null)
            {
                tooltipPanel.gameObject.SetActive(true);
                tooltipPanel.SetAsLastSibling();
            }
            _showing = true;
            PositionAboveCard();
        }

        public void Hide()
        {
            _showing = false;
            _currentCardRect = null;
            if (tooltipPanel != null)
                tooltipPanel.gameObject.SetActive(false);
        }

        private void PositionAboveCard()
        {
            if (tooltipPanel == null || _canvas == null) return;

            if (_currentCardRect != null)
            {
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
                {
                    tooltipPanel.position = pos;
                }
                else
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        _canvasRect, pos, _canvas.worldCamera, out Vector2 lp);
                    tooltipPanel.anchoredPosition = lp;
                }
            }
            else
            {
                Vector2 mp = Input.mousePosition;
                if (_canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    tooltipPanel.position = mp + offset;
                else
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        _canvasRect, mp, _canvas.worldCamera, out Vector2 lp);
                    tooltipPanel.anchoredPosition = lp + offset;
                }
            }
        }
    }
}
