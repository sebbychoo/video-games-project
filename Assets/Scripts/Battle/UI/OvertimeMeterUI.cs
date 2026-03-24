using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CardBattle
{
    /// <summary>
    /// Displays the current/max Overtime Meter value and the Overflow Buffer value.
    /// Subscribes to BattleEventBus events to refresh the display reactively.
    /// Overflow text is hidden when the buffer is 0.
    /// </summary>
    public class OvertimeMeterUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] OvertimeMeter overtimeMeter;
        [SerializeField] OverflowBuffer overflowBuffer;

        [Header("UI Elements")]
        [SerializeField] TextMeshProUGUI otText;
        [SerializeField] TextMeshProUGUI overflowText;
        [SerializeField] Image fillImage;

        [Header("Settings")]
        [SerializeField] float lerpSpeed = 8f;

        private float _targetFill;
        private float _currentFill;

        /// <summary>Initialize the UI with current meter values.</summary>
        public void Initialize(OvertimeMeter meter, OverflowBuffer overflow)
        {
            overtimeMeter = meter;
            overflowBuffer = overflow;

            Refresh();

            if (fillImage != null)
            {
                _targetFill = overtimeMeter != null && overtimeMeter.Max > 0
                    ? (float)overtimeMeter.Current / overtimeMeter.Max
                    : 1f;
                _currentFill = _targetFill;
                ApplyFill(_currentFill);
            }
        }

        private void OnEnable()
        {
            if (BattleEventBus.Instance != null)
            {
                BattleEventBus.Instance.OnCardPlayed += HandleCardPlayed;
                BattleEventBus.Instance.OnTurnPhaseChanged += HandleTurnPhaseChanged;
                BattleEventBus.Instance.OnOverflow += HandleOverflow;
                BattleEventBus.Instance.OnDamageReceived += HandleDamageReceived;
            }
        }

        private void OnDisable()
        {
            if (BattleEventBus.Instance != null)
            {
                BattleEventBus.Instance.OnCardPlayed -= HandleCardPlayed;
                BattleEventBus.Instance.OnTurnPhaseChanged -= HandleTurnPhaseChanged;
                BattleEventBus.Instance.OnOverflow -= HandleOverflow;
                BattleEventBus.Instance.OnDamageReceived -= HandleDamageReceived;
            }
        }

        private void Update()
        {
            if (fillImage == null || overtimeMeter == null) return;

            _currentFill = Mathf.Lerp(_currentFill, _targetFill, Time.deltaTime * lerpSpeed);
            ApplyFill(_currentFill);
        }

        private void HandleCardPlayed(CardPlayedEvent e) => Refresh();
        private void HandleTurnPhaseChanged(TurnPhaseChangedEvent e) => Refresh();
        private void HandleOverflow(OverflowEvent e) => Refresh();
        private void HandleDamageReceived(DamageEvent e) => Refresh();

        /// <summary>Refresh all displayed values from the current meter and buffer state.</summary>
        public void Refresh()
        {
            UpdateOTText();
            UpdateOverflowText();
            UpdateFill();
        }

        private void UpdateOTText()
        {
            if (otText == null || overtimeMeter == null) return;
            otText.text = $"{overtimeMeter.Current} / {overtimeMeter.Max}";
        }

        private void UpdateOverflowText()
        {
            if (overflowText == null) return;

            if (overflowBuffer != null && overflowBuffer.Current > 0)
            {
                overflowText.gameObject.SetActive(true);
                overflowText.text = $"+{overflowBuffer.Current}";
            }
            else
            {
                overflowText.gameObject.SetActive(false);
            }
        }

        private void UpdateFill()
        {
            if (fillImage == null || overtimeMeter == null) return;

            _targetFill = overtimeMeter.Max > 0
                ? (float)overtimeMeter.Current / overtimeMeter.Max
                : 0f;
        }

        private void ApplyFill(float value)
        {
            if (fillImage == null) return;

            if (fillImage.type == Image.Type.Filled)
                fillImage.fillAmount = value;
            else
                fillImage.rectTransform.localScale = new Vector3(value, 1f, 1f);
        }
    }
}
