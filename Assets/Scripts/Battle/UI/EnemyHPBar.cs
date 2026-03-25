using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CardBattle
{
    /// <summary>
    /// Simple enemy HP bar. Attach to a UI panel above the enemy.
    /// Works with both Filled images (fillAmount) and regular images (scales X).
    /// Supports tracking an EnemyCombatant and displaying Block when > 0.
    /// </summary>
    public class EnemyHPBar : MonoBehaviour
    {
        [SerializeField] Image fillImage;
        [SerializeField] TextMeshProUGUI hpText;
        [SerializeField] float lerpSpeed = 8f;

        [Header("Block Display")]
        [SerializeField] TextMeshProUGUI blockText;
        [SerializeField] GameObject blockPanel;

        [Header("Low HP Warning")]
        [SerializeField] TextMeshProUGUI lowHPWarningText;
        [SerializeField] float lowHPThreshold = 0.1f;
        [SerializeField] string lowHPMessage = "FINAL NOTICE";

        private float _targetFill;
        private float _currentFill;
        private bool _useFillAmount;

        /// <summary>The GameObject this HP bar is tracking (for event filtering).</summary>
        private GameObject _trackedTarget;

        // ── Backward-compatible initialization ────────────────────────────────

        public void Initialize(int currentHP, int maxHP)
        {
            _targetFill = Mathf.Clamp01((float)currentHP / maxHP);
            _currentFill = _targetFill;

            if (fillImage != null)
                fillImage.rectTransform.localScale = new Vector3(_currentFill, 1f, 1f);

            UpdateText(currentHP, maxHP);
            UpdateBlockDisplay(0);
        }

        /// <summary>
        /// Initialize from an EnemyCombatant. Stores the target for event-driven
        /// Block and HP updates.
        /// </summary>
        public void Initialize(EnemyCombatant enemy)
        {
            _trackedTarget = enemy != null ? enemy.gameObject : null;
            Initialize(enemy.CurrentHP, enemy.MaxHP);
        }

        // ── Event subscriptions ───────────────────────────────────────────────

        private void OnEnable()
        {
            if (BattleEventBus.Instance != null)
            {
                BattleEventBus.Instance.OnBlockChanged += HandleBlockChanged;
                BattleEventBus.Instance.OnDamageDealt += HandleDamageDealt;
            }
        }

        private void OnDisable()
        {
            if (BattleEventBus.Instance != null)
            {
                BattleEventBus.Instance.OnBlockChanged -= HandleBlockChanged;
                BattleEventBus.Instance.OnDamageDealt -= HandleDamageDealt;
            }
        }

        private void HandleBlockChanged(BlockEvent e)
        {
            if (_trackedTarget == null || e.Target != _trackedTarget) return;
            UpdateBlockDisplay(e.NewTotal);
        }

        private void HandleDamageDealt(DamageEvent e)
        {
            if (_trackedTarget == null || e.Target != _trackedTarget) return;

            // Auto-refresh HP from the EnemyCombatant component
            EnemyCombatant ec = _trackedTarget.GetComponent<EnemyCombatant>();
            if (ec != null)
                UpdateHP(ec.CurrentHP, ec.MaxHP);
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void UpdateHP(int currentHP, int maxHP)
        {
            _targetFill = Mathf.Clamp01((float)currentHP / maxHP);
            UpdateText(currentHP, maxHP);
        }

        // ── Internals ────────────────────────────────────────────────────────

        private void Update()
        {
            if (fillImage == null) return;

            _currentFill = Mathf.Lerp(_currentFill, _targetFill, Time.deltaTime * lerpSpeed);
            fillImage.rectTransform.localScale = new Vector3(_currentFill, 1f, 1f);
        }

        private void UpdateText(int current, int max)
        {
            int display = Mathf.Max(current, 0);
            if (hpText != null)
                hpText.text = $"{display} / {max}";

            if (lowHPWarningText != null)
            {
                bool showWarning = current > 0 && max > 0 && (float)current / max <= lowHPThreshold;
                lowHPWarningText.gameObject.SetActive(showWarning);
                if (showWarning)
                    lowHPWarningText.text = lowHPMessage;
            }
        }

        private void UpdateBlockDisplay(int blockValue)
        {
            bool showBlock = blockValue > 0;

            if (blockPanel != null)
                blockPanel.SetActive(showBlock);

            if (blockText != null)
            {
                blockText.gameObject.SetActive(showBlock);
                if (showBlock)
                    blockText.text = blockValue.ToString();
            }
        }
    }
}
