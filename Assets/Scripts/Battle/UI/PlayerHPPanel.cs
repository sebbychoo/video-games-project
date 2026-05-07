using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CardBattle
{
    /// <summary>
    /// Player HP panel — a flat UI bar with HP text and a "FINAL NOTICE" low-HP warning.
    /// Replaces the old badge-based system. Subscribes to BattleEventBus for reactive updates.
    /// </summary>
    public class PlayerHPPanel : MonoBehaviour
    {
        [Header("HP Bar")]
        [SerializeField] Image hpBarFill;
        [SerializeField] TextMeshProUGUI hpText;

        [Header("Low HP Warning")]
        [SerializeField] GameObject finalNoticeRoot;
        [SerializeField] float lowHPThreshold = 0.2f;

        [Header("Block Display")]
        [SerializeField] TextMeshProUGUI blockText;
        [SerializeField] GameObject blockRoot;

        [Header("Lerp")]
        [SerializeField] float lerpSpeed = 8f;

        private float _targetFill;
        private float _currentFill = 1f;
        private int _maxHP;
        private GameObject _playerTarget;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            // Snap to live Health values in Awake so the bar is correct before
            // any Start() runs — including BattleManager.Start().
            SnapToPlayerHealth();
        }

        private void Start()
        {
            // Re-snap in Start as a safety net in case the Player GameObject
            // wasn't ready yet during Awake (e.g. spawned at runtime).
            if (_playerTarget == null)
                SnapToPlayerHealth();
        }

        private void SnapToPlayerHealth()
        {
            if (_playerTarget != null) return; // Already initialized externally.

            GameObject player = GameObject.FindWithTag("Player");
            if (player == null) return;

            Health h = player.GetComponent<Health>();
            if (h == null) return;

            int cur = h.currentHealth > 0 ? h.currentHealth : h.maxHealth;
            int max = Mathf.Max(h.maxHealth, 1);
            _playerTarget = player;
            _maxHP = max;
            _targetFill = _currentFill = (float)cur / max;
            ApplyFill(_currentFill);
            UpdateText(cur, max);
            RefreshFinalNotice(cur, max);
            RefreshBlock(0);
        }

        // ── Initialization ────────────────────────────────────────────────────

        /// <summary>Initialize the panel with a player reference and starting HP values.</summary>
        public void Initialize(GameObject player, int currentHP, int maxHP)
        {
            _playerTarget = player;
            _maxHP = maxHP;
            _targetFill = _currentFill = maxHP > 0 ? (float)currentHP / maxHP : 0f;
            ApplyFill(_currentFill);
            UpdateText(currentHP, maxHP);
            RefreshFinalNotice(currentHP, maxHP);
            RefreshBlock(0);
        }

        /// <summary>Force an HP update without changing the tracked target.</summary>
        public void UpdateHP(int current, int max)
        {
            _maxHP = max;
            _targetFill = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
            UpdateText(current, max);
            RefreshFinalNotice(current, max);
        }

        // ── Event wiring ─────────────────────────────────────────────────────

        private void OnEnable()
        {
            if (BattleEventBus.Instance == null) return;
            BattleEventBus.Instance.OnDamageReceived += HandleDamage;
            BattleEventBus.Instance.OnBlockChanged   += HandleBlock;
        }

        private void OnDisable()
        {
            if (BattleEventBus.Instance == null) return;
            BattleEventBus.Instance.OnDamageReceived -= HandleDamage;
            BattleEventBus.Instance.OnBlockChanged   -= HandleBlock;
        }

        private void HandleDamage(DamageEvent e)
        {
            if (_playerTarget == null || e.Target != _playerTarget) return;
            Health h = _playerTarget.GetComponent<Health>();
            if (h != null) UpdateHP(h.currentHealth, h.maxHealth);
        }

        private void HandleBlock(BlockEvent e)
        {
            if (_playerTarget == null || e.Target != _playerTarget) return;
            RefreshBlock(e.NewTotal);
        }

        // ── Update (smooth fill lerp) ─────────────────────────────────────────

        private void Update()
        {
            if (Mathf.Approximately(_currentFill, _targetFill)) return;
            _currentFill = Mathf.Lerp(_currentFill, _targetFill, Time.deltaTime * lerpSpeed);
            ApplyFill(_currentFill);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private void ApplyFill(float fill)
        {
            if (hpBarFill != null)
                hpBarFill.fillAmount = fill;
        }

        private void UpdateText(int current, int max)
        {
            if (hpText != null)
                hpText.text = $"{Mathf.Max(current, 0)}<size=70%>/{max}</size>";
        }

        private void RefreshFinalNotice(int current, int max)
        {
            if (finalNoticeRoot == null) return;
            bool low = max > 0 && current > 0 && (float)current / max <= lowHPThreshold;
            finalNoticeRoot.SetActive(low);
        }

        private void RefreshBlock(int blockValue)
        {
            bool hasBlock = blockValue > 0;
            if (blockRoot != null) blockRoot.SetActive(hasBlock);
            if (blockText != null) blockText.text = blockValue.ToString();
        }
    }
}
