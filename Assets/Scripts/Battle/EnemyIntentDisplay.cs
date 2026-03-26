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
        public void Initialize(EnemyCombatant enemy) { _enemy = enemy; _hidden = false; Refresh(); }
        public void Hide() { _hidden = true; if (intentIcon != null) intentIcon.enabled = false; if (damageText != null) damageText.enabled = false; }
        public void Refresh() { if (_hidden || _enemy == null || !_enemy.IsAlive) { Hide(); return; } UpdateDisplay(_enemy.CurrentIntent); }
        private void UpdateDisplay(EnemyAction action)
        {
            if (intentIcon != null) intentIcon.enabled = false;
            if (damageText == null) return;
            damageText.enabled = true;
            if (action.actionType == EnemyActionType.DealDamage) damageText.text = "EMAILS +" + action.value.ToString();
            else if (action.actionType == EnemyActionType.Defend) damageText.text = "POLICY UPDATE";
            else if (action.actionType == EnemyActionType.Buff) damageText.text = "SYNERGY MEETING";
            else if (action.actionType == EnemyActionType.ApplyStatus) damageText.text = "MEETING";
            else if (action.actionType == EnemyActionType.Special) damageText.text = "MEMO";
            else damageText.text = "...";
        }
        private void Update()
        {
            if (_enemy == null) { if (BattleManager.Instance != null && BattleManager.Instance.Enemies.Count > 0) Initialize(BattleManager.Instance.Enemies[0]); return; }
            if (!_hidden) Refresh();
        }
    }
}
