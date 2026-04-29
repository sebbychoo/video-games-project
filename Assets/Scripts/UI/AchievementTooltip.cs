using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

namespace CardBattle
{
    /// <summary>
    /// Attach to each achievement row. Shows a floating tooltip on hover,
    /// driven by a shared singleton tooltip panel that lives on the Canvas.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class AchievementTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        // ── Singleton tooltip panel ───────────────────────────────────────

        private static TooltipPanel _panel;

        /// <summary>Called by MainMenu after it creates the tooltip panel.</summary>
        public static void RegisterPanel(TooltipPanel panel) => _panel = panel;

        // ── Instance data ─────────────────────────────────────────────────

        private string _tooltipText;

        /// <summary>Set before adding to the scene.</summary>
        public void Configure(string tooltipText) => _tooltipText = tooltipText;

        // ── Pointer events ────────────────────────────────────────────────

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_panel != null && !string.IsNullOrEmpty(_tooltipText))
                _panel.Show(_tooltipText);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _panel?.Hide();
        }

        private void OnDisable()
        {
            _panel?.Hide();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Tooltip panel — a simple dark card that follows the cursor.
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Managed singleton panel that renders the tooltip text near the pointer.
    /// </summary>
    public class TooltipPanel : MonoBehaviour
    {
        private const float HorizontalOffset = 16f;
        private const float VerticalOffset   = -12f;

        // Resolved lazily — no serialized reference needed.
        private TextMeshProUGUI _label;
        private RectTransform _rt;
        private Canvas _canvas;

        private void Awake()
        {
            _rt     = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();
            _label  = GetComponentInChildren<TextMeshProUGUI>(includeInactive: true);

            // If no TMP child exists yet, create one at runtime
            if (_label == null)
            {
                GameObject labelGO = new GameObject("Label");
                labelGO.transform.SetParent(transform, false);
                RectTransform lrt = labelGO.AddComponent<RectTransform>();
                lrt.anchorMin     = Vector2.zero;
                lrt.anchorMax     = Vector2.one;
                lrt.offsetMin     = new Vector2(12f,  10f);
                lrt.offsetMax     = new Vector2(-12f, -10f);
                _label            = labelGO.AddComponent<TextMeshProUGUI>();
                _label.fontSize   = 13f;
                _label.color      = new Color(0.88f, 0.88f, 0.84f, 1f);
                _label.enableWordWrapping = true;
                _label.raycastTarget     = false;
            }

            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!gameObject.activeSelf) return;
            FollowCursor();
        }

        public void Show(string text)
        {
            if (_label != null) _label.text = text;
            gameObject.SetActive(true);
            FollowCursor();
        }

        public void Hide() => gameObject.SetActive(false);

        private void FollowCursor()
        {
            if (_canvas == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.transform as RectTransform,
                Input.mousePosition,
                _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera,
                out Vector2 localPoint
            );

            _rt.anchoredPosition = localPoint + new Vector2(HorizontalOffset, VerticalOffset);
        }
    }
}
