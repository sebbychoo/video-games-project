using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace CardBattle
{
    /// <summary>
    /// Displays mana veins glow in the battle scene driven by OT values.
    /// Subscribes to BattleEventBus for real-time updates.
    /// Shows an OT tooltip on hover. Coexists with OvertimeMeterUI for now.
    /// </summary>
    public class BattleVeinsUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image veinImage;
        [SerializeField] private Color dimGlowColor = new Color(0.1f, 0.2f, 0.4f, 0.3f);
        [SerializeField] private Color brightGlowColor = new Color(0.3f, 0.7f, 1f, 1f);
        [SerializeField] private OvertimeMeter overtimeMeter;
        [SerializeField] private OverflowBuffer overflowBuffer;
        [SerializeField] private TextMeshProUGUI tooltipText;

        /// <summary>
        /// Initialize the veins UI at the start of an encounter.
        /// </summary>
        public void Initialize(OvertimeMeter meter, OverflowBuffer overflow)
        {
            overtimeMeter = meter;
            overflowBuffer = overflow;

            if (overtimeMeter == null)
                Debug.LogWarning("BattleVeinsUI: OvertimeMeter is null. Veins will display minimum glow.");

            Refresh();
        }

        private void OnEnable() => SubscribeToEvents();

        private void Start()
        {
            SubscribeToEvents();
            if (tooltipText != null)
                tooltipText.gameObject.SetActive(false);
        }

        private void SubscribeToEvents()
        {
            if (BattleEventBus.Instance == null) return;

            BattleEventBus.Instance.OnCardPlayed -= OnCardPlayed;
            BattleEventBus.Instance.OnTurnPhaseChanged -= OnTurnPhaseChanged;
            BattleEventBus.Instance.OnOverflow -= OnOverflow;
            BattleEventBus.Instance.OnDamageReceived -= OnDamageReceived;

            BattleEventBus.Instance.OnCardPlayed += OnCardPlayed;
            BattleEventBus.Instance.OnTurnPhaseChanged += OnTurnPhaseChanged;
            BattleEventBus.Instance.OnOverflow += OnOverflow;
            BattleEventBus.Instance.OnDamageReceived += OnDamageReceived;
        }

        private void OnDisable()
        {
            if (BattleEventBus.Instance == null) return;

            BattleEventBus.Instance.OnCardPlayed -= OnCardPlayed;
            BattleEventBus.Instance.OnTurnPhaseChanged -= OnTurnPhaseChanged;
            BattleEventBus.Instance.OnOverflow -= OnOverflow;
            BattleEventBus.Instance.OnDamageReceived -= OnDamageReceived;
        }

        private void OnCardPlayed(CardPlayedEvent e) => Refresh();
        private void OnTurnPhaseChanged(TurnPhaseChangedEvent e) => Refresh();
        private void OnOverflow(OverflowEvent e) => Refresh();
        private void OnDamageReceived(DamageEvent e) => Refresh();

        /// <summary>
        /// Refresh the vein glow based on current OT values.
        /// </summary>
        public void Refresh()
        {
            if (veinImage == null) return;

            if (overtimeMeter == null)
            {
                veinImage.color = dimGlowColor;
                UpdateTooltip();
                return;
            }

            int currentOT = overtimeMeter.Current;
            int maxOT = overtimeMeter.Max;
            int overflowOT = overflowBuffer != null ? overflowBuffer.Current : 0;

            veinImage.color = VeinGlowCalculator.ComputeGlow(currentOT, maxOT, overflowOT, dimGlowColor, brightGlowColor);
            UpdateTooltip();
        }

        private void UpdateTooltip()
        {
            if (tooltipText == null || !tooltipText.gameObject.activeSelf) return;

            tooltipText.text = FormatTooltip();
        }

        private string FormatTooltip()
        {
            if (overtimeMeter == null)
                return "Overtime: --/--";

            int current = overtimeMeter.Current;
            int overflow = overflowBuffer != null ? overflowBuffer.Current : 0;
            int max = overtimeMeter.Max;

            return $"Overtime: {current + overflow}/{max}";
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (tooltipText != null)
            {
                tooltipText.text = FormatTooltip();
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
