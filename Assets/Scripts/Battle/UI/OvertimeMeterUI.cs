using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace CardBattle
{
    /// <summary>
    /// Displays Overtime as a single vertical column of Image dots.
    /// Normal OT fills bottom-to-top. Overflow dots stack on top with
    /// color shifting: 1=gold, 2=yellow, 3=orange, 4+=red.
    /// Shows total mana as "15/10" text. Hover shows tooltip.
    /// </summary>
    public class OvertimeMeterUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("References")]
        [SerializeField] OvertimeMeter overtimeMeter;
        [SerializeField] OverflowBuffer overflowBuffer;

        [Header("UI Elements")]
        [SerializeField] Transform dotContainer; // parent for dot images
        [SerializeField] TextMeshProUGUI manaText;
        [SerializeField] TextMeshProUGUI tooltipText;

        [Header("Dot Settings")]
        [SerializeField] Sprite filledSprite; // null = uses default white
        [SerializeField] Sprite emptySprite;  // null = uses default white
        [SerializeField] int maxDots = 10;
        [SerializeField] float dotSize = 16f;
        [SerializeField] float dotSpacing = 2f;

        [Header("Colors")]
        [SerializeField] Color normalColor = new Color(1f, 0.8f, 0f);
        [SerializeField] Color emptyColor = new Color(0.4f, 0.4f, 0.4f);
        [SerializeField] Color overflow1Color = new Color(1f, 0.8f, 0f);
        [SerializeField] Color overflow2Color = new Color(1f, 1f, 0f);
        [SerializeField] Color overflow3Color = new Color(1f, 0.5f, 0f);
        [SerializeField] Color overflow4PlusColor = new Color(1f, 0.1f, 0.1f);

        [Header("Tooltip")]
        [SerializeField] string tooltipLabel = "This is your Overtime";

        private readonly List<Image> _dots = new List<Image>();

        public void Initialize(OvertimeMeter meter, OverflowBuffer overflow)
        {
            overtimeMeter = meter;
            overflowBuffer = overflow;
            RebuildDots();
            Refresh();
        }

        private void OnEnable() => SubscribeToEvents();
        private void Start()
        {
            SubscribeToEvents();
            if (tooltipText != null) tooltipText.gameObject.SetActive(false);
            if (_dots.Count == 0) RebuildDots();
        }

        private void SubscribeToEvents()
        {
            if (BattleEventBus.Instance == null) return;
            BattleEventBus.Instance.OnCardPlayed -= OnEvent;
            BattleEventBus.Instance.OnTurnPhaseChanged -= OnPhase;
            BattleEventBus.Instance.OnOverflow -= OnOverflow;
            BattleEventBus.Instance.OnDamageReceived -= OnDamage;
            BattleEventBus.Instance.OnCardPlayed += OnEvent;
            BattleEventBus.Instance.OnTurnPhaseChanged += OnPhase;
            BattleEventBus.Instance.OnOverflow += OnOverflow;
            BattleEventBus.Instance.OnDamageReceived += OnDamage;
        }

        private void OnDisable()
        {
            if (BattleEventBus.Instance == null) return;
            BattleEventBus.Instance.OnCardPlayed -= OnEvent;
            BattleEventBus.Instance.OnTurnPhaseChanged -= OnPhase;
            BattleEventBus.Instance.OnOverflow -= OnOverflow;
            BattleEventBus.Instance.OnDamageReceived -= OnDamage;
        }

        private void Update()
        {
            if (overtimeMeter == null)
            {
                overtimeMeter = FindObjectOfType<OvertimeMeter>();
                if (overtimeMeter == null) return;
            }
            if (overflowBuffer == null)
                overflowBuffer = FindObjectOfType<OverflowBuffer>();
            UpdateDotColors();
            UpdateManaText();
        }

        private void OnEvent(CardPlayedEvent e) => Refresh();
        private void OnPhase(TurnPhaseChangedEvent e) => Refresh();
        private void OnOverflow(OverflowEvent e) => Refresh();
        private void OnDamage(DamageEvent e) => Refresh();

        public void Refresh() { UpdateDotColors(); UpdateManaText(); }

        // ── Dot creation ────────────────────────────────────────────────────

        private void RebuildDots()
        {
            // Clear existing
            foreach (var dot in _dots)
                if (dot != null) Destroy(dot.gameObject);
            _dots.Clear();

            if (dotContainer == null) return;

            // Create dots bottom-to-top (index 0 = bottom)
            for (int i = 0; i < maxDots; i++)
            {
                GameObject go = new GameObject($"Dot_{i}", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(dotContainer, false);

                RectTransform rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(dotSize, dotSize);
                rt.anchorMin = new Vector2(0.5f, 0f);
                rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0f, i * (dotSize + dotSpacing));

                Image img = go.GetComponent<Image>();
                img.sprite = emptySprite;
                img.color = emptyColor;
                img.raycastTarget = false;

                _dots.Add(img);
            }
        }

        // ── Dot color update ────────────────────────────────────────────────

        private void UpdateDotColors()
        {
            if (overtimeMeter == null || _dots.Count == 0) return;

            int current = Mathf.Min(overtimeMeter.Current, maxDots);
            int overflow = overflowBuffer != null ? overflowBuffer.Current : 0;

            // Each dot can have multiple "stacks" from overflow wrapping around
            // overflow 0-9: bottom N dots get 2 stacks (yellow), rest stay 1 (gold)
            // overflow 10-19: all dots are 2 stacks, bottom N get 3 stacks (orange)
            // overflow 20-29: all dots are 3 stacks, bottom N get 4 stacks (red)
            // etc.

            for (int i = 0; i < _dots.Count; i++)
            {
                if (_dots[i] == null) continue;

                if (i < current)
                {
                    // Calculate how many stacks this dot has
                    // Overflow fills bottom-to-top in rounds of maxDots
                    int stacks = 1; // base layer
                    if (overflow > 0)
                    {
                        int fullRounds = overflow / maxDots;
                        int remainder = overflow % maxDots;
                        stacks += fullRounds;
                        if (i < remainder)
                            stacks++;
                    }

                    _dots[i].sprite = filledSprite;
                    _dots[i].color = GetStackColor(stacks);
                }
                else
                {
                    _dots[i].sprite = emptySprite;
                    _dots[i].color = emptyColor;
                }
            }
        }

        private Color GetStackColor(int stacks)
        {
            if (stacks <= 1) return normalColor;       // gold
            if (stacks == 2) return overflow2Color;     // yellow
            if (stacks == 3) return overflow3Color;     // orange
            return overflow4PlusColor;                  // red
        }

        private Color GetOverflowColor(int overflow)
        {
            if (overflow <= 1) return overflow1Color;
            if (overflow == 2) return overflow2Color;
            if (overflow == 3) return overflow3Color;
            return overflow4PlusColor;
        }

        // ── Mana text ───────────────────────────────────────────────────────

        private void UpdateManaText()
        {
            if (manaText == null || overtimeMeter == null) return;

            int current = overtimeMeter.Current;
            int overflow = overflowBuffer != null ? overflowBuffer.Current : 0;
            int total = current + overflow;
            int max = overtimeMeter.Max;

            if (overflow > 0)
            {
                manaText.text = $"{total}/{max}";
                manaText.color = GetOverflowColor(overflow);
            }
            else
            {
                manaText.text = $"{current}/{max}";
                manaText.color = normalColor;
            }
        }

        // ── Tooltip ─────────────────────────────────────────────────────────

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (tooltipText != null)
            {
                tooltipText.text = tooltipLabel;
                tooltipText.gameObject.SetActive(true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (tooltipText != null)
                tooltipText.gameObject.SetActive(false);
        }
    }
}
