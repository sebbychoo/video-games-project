using UnityEngine;
using TMPro;

namespace CardBattle
{
    /// <summary>
    /// Previously tracked the enemy in world space each frame.
    /// Now disabled — the EnemyHUDPanel is positioned as a fixed screen-space element
    /// since the enemy stands at a static position during combat.
    /// SetHPBar and SetEnemyName are called at spawn time by BattleManager.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class EnemyHUDFollow : MonoBehaviour
    {
        [SerializeField] private EnemyHPBar hpBar;
        [SerializeField] private TextMeshProUGUI dossierLabel;

        /// <summary>No-op. Kept for compatibility with spawn-time wiring.</summary>
        public void SetHPBar(EnemyHPBar bar) => hpBar = bar;

        /// <summary>Sets the enemy name displayed in the dossier label.</summary>
        public void SetEnemyName(string enemyName)
        {
            if (dossierLabel != null)
                dossierLabel.text = enemyName;
        }
    }
}
