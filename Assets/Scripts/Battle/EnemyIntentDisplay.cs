using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CardBattle
{
    public class EnemyIntentDisplay : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] Image intentIcon;
        [SerializeField] TextMeshProUGUI damageText;

        [Header("Intent Icons")]
        [SerializeField] Sprite attackSprite;
        [SerializeField] Sprite defendSprite;
        [SerializeField] Sprite buffSprite;
        [SerializeField] Sprite specialSprite;
        [SerializeField] Sprite statusSprite;

        private EnemyCombatant _enemy;
        private bool _hidden;

        public void Initialize(EnemyCombatant enemy)
        {
            _enemy = enemy;
            _hidden = false;
            if (BattleEventBus.Instance != null)
            {
                BattleEventBus.Instance.OnDamageDealt += OnDamageDealt;
                BattleEventBus.Instance.OnTurnPhaseChanged += OnTurnPhaseChanged;
            }
            Refresh();
        }

        private void OnDestroy()
        {
            if (BattleEventBus.Instance != null)
            {
                BattleEventBus.Instance.OnDamageDealt -= OnDamageDealt;
                BattleEventBus.Instance.OnTurnPhaseChanged -= OnTurnPhaseChanged;
            }
        }

        public void Hide()
        {
            _hidden = true;
            if (intentIcon != null) intentIcon.enabled = false;
            if (damageText != null) damageText.enabled = false;
        }

        public void Refresh()
        {
            if (_hidden || _enemy == null || !_enemy.IsAlive)
            {
                Hide();
                return;
            }
            EnemyAction intent = _enemy.CurrentIntent;
            UpdateDisplay(intent);
        }

        private void UpdateDisplay(EnemyAction action)
        {
            if (intentIcon != null)
            {
                intentIcon.enabled = true;
                switch (action.actionType)
                {
                    case EnemyActionType.DealDamage:  intentIcon.sprite = attackSprite;  break;
                    case EnemyActionType.Defend:       intentIcon.sprite = defendSprite;  break;
                    case EnemyActionType.Buff:         intentIcon.sprite = buffSprite;    break;
                    case EnemyActionType.Special:      intentIcon.sprite = specialSprite; break;
                    case EnemyActionType.ApplyStatus:  intentIcon.sprite = statusSprite;  break;
                }
            }

            if (damageText != null)
            {
                if (action.actionType == EnemyActionType.DealDamage)
                {
                    damageText.enabled = true;
                    damageText.text = action.value.ToString();
                }
                else
                {
                    damageText.enabled = false;
                }
            }
        }

        private void OnDamageDealt(DamageEvent e)
        {
            if (BattleManager.Instance != null &&
                BattleManager.Instance.CurrentTurn == TurnPhase.Play)
            {
                Refresh();
            }
        }

        private void OnTurnPhaseChanged(TurnPhaseChangedEvent e)
        {
            if (e.NewPhase == TurnPhase.Draw || e.NewPhase == TurnPhase.Play)
            {
                Refresh();
            }
        }
    }
}
