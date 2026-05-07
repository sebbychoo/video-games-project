using UnityEngine;
using TMPro;

namespace CardBattle
{
    /// <summary>
    /// Displays Overtime as a plain number with an "OVERTIME POINTS" label above it.
    /// Shows total points (current + overflow). Color shifts as overflow builds.
    /// </summary>
    public class OvertimeMeterUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] OvertimeMeter overtimeMeter;
        [SerializeField] OverflowBuffer overflowBuffer;

        [Header("UI Elements")]
        [SerializeField] TextMeshProUGUI valueText;
        [SerializeField] TextMeshProUGUI labelText;

        [Header("Colors")]
        [SerializeField] Color normalColor    = new Color(1f, 0.82f, 0.25f);
        [SerializeField] Color overflow1Color = new Color(1f, 0.82f, 0.25f);
        [SerializeField] Color overflow2Color = new Color(1f, 1f,    0f);
        [SerializeField] Color overflow3Color = new Color(1f, 0.5f,  0f);
        [SerializeField] Color overflow4Color = new Color(1f, 0.12f, 0.12f);

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Wire references and trigger the first display update.</summary>
        public void Initialize(OvertimeMeter meter, OverflowBuffer overflow)
        {
            overtimeMeter  = meter;
            overflowBuffer = overflow;
            Refresh();
        }

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void OnEnable()  => Subscribe();
        private void OnDisable() => Unsubscribe();

        private void Start()
        {
            Subscribe();
            Refresh();
        }

        private void Update()
        {
            // Lazy-resolve until BattleManager wires us via Initialize().
            if (overtimeMeter  == null) overtimeMeter  = FindFirstObjectByType<OvertimeMeter>();
            if (overflowBuffer == null) overflowBuffer = FindFirstObjectByType<OverflowBuffer>();
        }

        // ── Events ────────────────────────────────────────────────────────────

        private void Subscribe()
        {
            if (BattleEventBus.Instance == null) return;
            BattleEventBus.Instance.OnCardPlayed       -= OnEvent;
            BattleEventBus.Instance.OnTurnPhaseChanged -= OnPhase;
            BattleEventBus.Instance.OnOverflow         -= OnOverflow;
            BattleEventBus.Instance.OnDamageReceived   -= OnDamage;
            BattleEventBus.Instance.OnCardPlayed       += OnEvent;
            BattleEventBus.Instance.OnTurnPhaseChanged += OnPhase;
            BattleEventBus.Instance.OnOverflow         += OnOverflow;
            BattleEventBus.Instance.OnDamageReceived   += OnDamage;
        }

        private void Unsubscribe()
        {
            if (BattleEventBus.Instance == null) return;
            BattleEventBus.Instance.OnCardPlayed       -= OnEvent;
            BattleEventBus.Instance.OnTurnPhaseChanged -= OnPhase;
            BattleEventBus.Instance.OnOverflow         -= OnOverflow;
            BattleEventBus.Instance.OnDamageReceived   -= OnDamage;
        }

        private void OnEvent(CardPlayedEvent e)       => Refresh();
        private void OnPhase(TurnPhaseChangedEvent e) => Refresh();
        private void OnOverflow(OverflowEvent e)      => Refresh();
        private void OnDamage(DamageEvent e)          => Refresh();

        // ── Display ───────────────────────────────────────────────────────────

        /// <summary>Update the number and color to reflect the current overtime state.</summary>
        public void Refresh()
        {
            if (overtimeMeter == null) return;

            int current  = overtimeMeter.Current;
            int overflow = overflowBuffer != null ? overflowBuffer.Current : 0;
            int total    = current + overflow;

            Color color = overflow == 0 ? normalColor : GetOverflowColor(overflow);

            if (valueText != null)
            {
                valueText.text  = total.ToString();
                valueText.color = color;
            }

            if (labelText != null)
                labelText.color = color;
        }

        private Color GetOverflowColor(int overflow)
        {
            if (overflow <= 1) return overflow1Color;
            if (overflow == 2) return overflow2Color;
            if (overflow == 3) return overflow3Color;
            return overflow4Color;
        }
    }
}
