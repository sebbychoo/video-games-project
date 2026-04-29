using UnityEngine;
using CardBattle;

/// <summary>
/// Attaches to the Player in the exploration scene.
/// Runs after Health.Awake() and overwrites currentHealth / maxHealth
/// from the persisted RunState so battle damage carries over correctly.
/// Also notifies ExplorationHPBar after applying.
/// </summary>
[DefaultExecutionOrder(10)] // Runs after Health (DefaultExecutionOrder 0)
[RequireComponent(typeof(Health))]
public class PlayerHealthRestorer : MonoBehaviour
{
    private void Start()
    {
        RestoreFromRunState();
    }

    /// <summary>
    /// Reads RunState and applies persisted HP to the Health component.
    /// Safe to call multiple times — idempotent when RunState has not changed.
    /// </summary>
    public void RestoreFromRunState()
    {
        RunState run = SaveManager.Instance != null ? SaveManager.Instance.CurrentRun : null;
        if (run == null || run.playerMaxHP <= 0) return;

        Health h = GetComponent<Health>();
        if (h == null) return;

        h.maxHealth     = run.playerMaxHP;
        h.currentHealth = Mathf.Clamp(run.playerHP, 0, run.playerMaxHP);

        // Notify the exploration HP bar to refresh
        ExplorationHPBar.NotifyHPChanged();
    }
}
