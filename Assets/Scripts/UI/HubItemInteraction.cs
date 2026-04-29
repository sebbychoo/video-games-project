using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace CardBattle
{
    /// <summary>
    /// Attach to each interactable desk item (FilingCabinet, CoffeeMug, PC, etc.).
    ///
    /// Outline technique:
    ///   glowBorder is a sibling Image placed BEFORE this object in the hierarchy
    ///   (so it renders behind). Its Image.color is kept fully transparent — it acts
    ///   only as a shape carrier. An Outline component on that sibling draws white
    ///   copies offset in 4 directions. The main sprite sits on top and covers the
    ///   interior; only the outline pixels that bleed past the sprite edge are visible.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class HubItemInteraction : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        // ── Inspector ────────────────────────────────────────────────────────

        [Header("Info Panel")]
        [Tooltip("The shared right-side info panel that shows item details.")]
        [SerializeField] private HubInfoPanel infoPanel;

        [Header("Item Data")]
        [SerializeField] private string itemTitle  = "ITEM";
        [SerializeField] private string itemStatus = "LOCKED";
        [SerializeField] [TextArea(2, 4)]
        private string itemDescription = "Description goes here.";

        [Header("Upgrade")]
        [Tooltip("HubUpgradeData ScriptableObject for this item. Leave null if not upgradeable.")]
        [SerializeField] private HubUpgradeData upgradeData;

        [Header("Outline Glow")]
        [Tooltip("Sibling Image placed BEFORE this object in the hierarchy. " +
                 "Its Image.color stays transparent — only its Outline is visible.")]
        [SerializeField] private Image glowBorder;

        [Tooltip("Outline pixel bleed on each side. Larger = thicker border.")]
        [SerializeField] private Vector2 outlineDistance = new Vector2(4f, -4f);

        [SerializeField] private Color glowHoverColor  = new Color(0.55f, 0.55f, 0.55f, 0.7f);
        [SerializeField] private Color glowActiveColor = new Color(1.00f, 1.00f, 1.00f, 1.0f);

        [Header("Timing")]
        [SerializeField] private float fadeDuration = 0.14f;

        // ── Private state ────────────────────────────────────────────────────

        private Button    _button;
        private Outline   _outline;
        private bool      _selected;
        private Coroutine _glowCoroutine;

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void Awake()
        {
            _button = GetComponent<Button>();

            if (glowBorder == null) return;

            // ① Copy this sprite onto the glowBorder so Outline follows the sprite alpha.
            Image ownImage = GetComponent<Image>();
            if (ownImage != null)
            {
                glowBorder.sprite         = ownImage.sprite;
                glowBorder.preserveAspect = false;  // must fill its rect exactly
            }

            // ② Keep the glowBorder Image invisible — it is only a shape carrier.
            //    The Outline component draws the visible white copies.
            glowBorder.color = Color.clear;

            // ③ Sync RectTransform exactly to this item so the outline aligns perfectly.
            RectTransform ownRect  = GetComponent<RectTransform>();
            RectTransform glowRect = glowBorder.GetComponent<RectTransform>();
            if (ownRect != null && glowRect != null)
            {
                glowRect.anchorMin        = ownRect.anchorMin;
                glowRect.anchorMax        = ownRect.anchorMax;
                glowRect.pivot            = ownRect.pivot;
                glowRect.anchoredPosition = ownRect.anchoredPosition;
                glowRect.sizeDelta        = ownRect.sizeDelta;
                glowRect.localScale       = ownRect.localScale;
            }

            // ④ Get or add the Outline on the glowBorder (not on this GameObject).
            _outline = glowBorder.GetComponent<Outline>();
            if (_outline == null)
                _outline = glowBorder.gameObject.AddComponent<Outline>();

            _outline.effectDistance  = outlineDistance;
            _outline.useGraphicAlpha = true;    // outline follows sprite alpha shape
            _outline.effectColor     = Color.clear;
        }

        private void Start()
        {
            _button.onClick.AddListener(OnClicked);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(OnClicked);
        }

        // ── Hover ────────────────────────────────────────────────────────────

        public void OnPointerEnter(PointerEventData _)
        {
            if (!_selected)
                FadeGlowTo(glowHoverColor);
        }

        public void OnPointerExit(PointerEventData _)
        {
            if (!_selected)
                FadeGlowTo(Color.clear);
        }

        // ── Click ────────────────────────────────────────────────────────────

        private void OnClicked()
        {
            if (infoPanel == null) return;

            _selected = true;
            FadeGlowTo(glowActiveColor);
            infoPanel.ShowItem(itemTitle, itemStatus, itemDescription, upgradeData, this);
        }

        /// <summary>Called by HubInfoPanel when this item is deselected.</summary>
        public void Deselect()
        {
            _selected = false;
            FadeGlowTo(Color.clear);
        }

        // ── Outline fade ─────────────────────────────────────────────────────

        private void FadeGlowTo(Color target)
        {
            if (_outline == null) return;
            if (_glowCoroutine != null) StopCoroutine(_glowCoroutine);
            _glowCoroutine = StartCoroutine(GlowFade(target));
        }

        private IEnumerator GlowFade(Color target)
        {
            Color start   = _outline.effectColor;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                _outline.effectColor = Color.Lerp(start, target, elapsed / fadeDuration);
                yield return null;
            }

            _outline.effectColor = target;
            _glowCoroutine       = null;
        }
    }
}
