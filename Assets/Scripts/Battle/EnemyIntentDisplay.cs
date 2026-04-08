using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace CardBattle
{
    public class EnemyIntentDisplay : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] Image intentIcon;
        [SerializeField] TextMeshProUGUI damageText;

        [Header("Intent Icons — drag your sprites here")]
        [SerializeField] Sprite attackSprite;
        [SerializeField] Sprite defendSprite;
        [SerializeField] Sprite buffSprite;
        [SerializeField] Sprite specialSprite;
        [SerializeField] Sprite statusSprite;

        [Header("Hover Tooltips")]
        [SerializeField] string attackTooltip = "They're getting ready to attack you.";
        [SerializeField] string defendTooltip = "They're bracing for impact.";
        [SerializeField] string buffTooltip = "They're boosting themselves.";
        [SerializeField] string specialTooltip = "Something unusual is coming.";
        [SerializeField] string statusTooltip = "They're planning to inflict a condition.";

        [Header("Tooltip UI")]
        [SerializeField] GameObject tooltipPanel;
        [SerializeField] TextMeshProUGUI tooltipText;
        [Tooltip("The GameObject that detects hover. Can be an Image or the root. Must have Raycast Target enabled.")]
        [SerializeField] GameObject hoverTarget;

        [Header("World Bubble (optional)")]
        [SerializeField] WorldIntentBubble worldBubble;

        private EnemyCombatant _enemy;
        private bool _hidden;

        public EnemyCombatant TrackedEnemy => _enemy;

        public void Initialize(EnemyCombatant enemy)
        {
            _enemy = enemy;
            _hidden = false;

            // Auto-find world bubble on the enemy if not assigned
            if (worldBubble == null && enemy != null)
                worldBubble = enemy.GetComponent<WorldIntentBubble>();

            // Wire hover immediately on initialize
            WireHoverTarget();

            Refresh();
        }

        private void WireHoverTarget()
        {
            GameObject target = hoverTarget != null ? hoverTarget : (intentIcon != null ? intentIcon.gameObject : null);
            if (target != null)
            {
                IntentHoverHandler hover = target.GetComponent<IntentHoverHandler>();
                if (hover == null) hover = target.AddComponent<IntentHoverHandler>();
                hover.Setup(this);
                Debug.Log($"[IntentDisplay] Hover wired to {target.name}");
            }
            else
            {
                Debug.LogWarning("[IntentDisplay] No hover target found — assign Hover Target in Inspector");
            }
        }

        public void Hide()
        {
            _hidden = true;
            if (intentIcon != null) intentIcon.enabled = false;
            if (damageText != null) damageText.enabled = false;
            if (worldBubble != null) worldBubble.Hide();
        }

        public void Refresh()
        {
            if (_hidden || _enemy == null || !_enemy.IsAlive)
            {
                Hide();
                return;
            }
            UpdateDisplay(_enemy.CurrentIntent);
        }

        private string _currentTooltip = "";

        private void UpdateDisplay(EnemyAction action)
        {
            Sprite icon = null;
            string valueText = "";
            _currentTooltip = "";

            switch (action.actionType)
            {
                case EnemyActionType.DealDamage:
                    icon = attackSprite;
                    valueText = action.value.ToString();
                    _currentTooltip = attackTooltip;
                    break;
                case EnemyActionType.Defend:
                    icon = defendSprite;
                    _currentTooltip = defendTooltip;
                    break;
                case EnemyActionType.Buff:
                    icon = buffSprite;
                    _currentTooltip = buffTooltip;
                    break;
                case EnemyActionType.ApplyStatus:
                    icon = statusSprite;
                    _currentTooltip = statusTooltip;
                    break;
                case EnemyActionType.Special:
                    icon = specialSprite;
                    _currentTooltip = specialTooltip;
                    break;
            }

            // Show icon only
            if (intentIcon != null)
            {
                intentIcon.enabled = icon != null;
                if (icon != null) intentIcon.sprite = icon;

            // Wire hover events — use hoverTarget if set, otherwise fall back to intentIcon
            GameObject target = hoverTarget != null ? hoverTarget : (intentIcon != null ? intentIcon.gameObject : null);
            if (target != null)
            {
                IntentHoverHandler hover = target.GetComponent<IntentHoverHandler>();
                if (hover == null) hover = target.AddComponent<IntentHoverHandler>();
                hover.Setup(this);
            }
            }

            // Show damage number only for attacks, hide otherwise
            if (damageText != null)
            {
                damageText.enabled = valueText.Length > 0;
                damageText.text = valueText;
            }

            // Hide tooltip by default — only hide on intent change, not every frame
            // (tooltip visibility is managed by ShowTooltip/HideTooltip)

            // Update world-space bubble
            if (worldBubble != null)
                worldBubble.SetIntent(icon);
        }

        /// <summary>Call from UI EventTrigger PointerEnter on the intent icon.</summary>
        public void ShowTooltip()
        {
            if (tooltipPanel == null || _currentTooltip.Length == 0) return;

            tooltipPanel.SetActive(true);
            if (tooltipText != null)
                tooltipText.text = _currentTooltip;

            PositionTooltip();
        }

        private void PositionTooltip()
        {
            if (tooltipPanel == null) return;

            RectTransform rt = tooltipPanel.GetComponent<RectTransform>();
            if (rt == null) return;

            Canvas canvas = tooltipPanel.GetComponentInParent<Canvas>();
            if (canvas == null) return;

            Camera cam = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
            if (cam == null) return;

            // Convert mouse screen position to world position on the canvas plane
            Vector3 mouseScreen = Input.mousePosition;
            mouseScreen.z = canvas.planeDistance;
            Vector3 worldPos = cam.ScreenToWorldPoint(mouseScreen);

            // Offset: if mouse on right half, tooltip goes left; otherwise right
            float offset = 0.15f; // world units
            if (mouseScreen.x > Screen.width * 0.5f)
                worldPos.x -= offset;
            else
                worldPos.x += offset;

            rt.position = worldPos;
        }

        public void HideTooltip()
        {
            if (tooltipPanel != null)
                tooltipPanel.SetActive(false);
        }

        private bool _frozen;

        public void Freeze() => _frozen = true;
        public void Unfreeze() => _frozen = false;

        private void Update()
        {
            if (_frozen) return;
            if (_enemy == null)
            {
                if (BattleManager.Instance != null && BattleManager.Instance.Enemies.Count > 0)
                    Initialize(BattleManager.Instance.Enemies[0]);
                return;
            }
            if (!_hidden) Refresh();
        }
    }

    /// <summary>Auto-added to the intent icon to handle mouse hover.</summary>
    public class IntentHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
    {
        private EnemyIntentDisplay _display;

        public void Setup(EnemyIntentDisplay display) => _display = display;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_display != null) _display.ShowTooltip();
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (_display != null) _display.ShowTooltip(); // repositions each frame
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_display != null) _display.HideTooltip();
        }
    }
}
