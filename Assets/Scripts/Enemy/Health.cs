using UnityEngine;

namespace CardBattle
{

/// <summary>
/// Tracks health for any GameObject (player or enemy).
/// Call TakeDamage() from other scripts to deal damage.
/// </summary>
public class Health : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;
    public bool suppressSceneLoad = false;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        if (currentHealth < 0) currentHealth = 0;
        Debug.Log($"{gameObject.name} took {amount} damage. HP: {currentHealth}");

        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} defeated.");

        // If BattleManager is active, let it handle defeat flow
        // (death screen → game over screen) instead of loading exploration directly
        if (BattleManager.Instance != null && !BattleManager.Instance.IsBattleOver)
        {
            // BattleManager will detect HP <= 0 and call OnDefeat()
            return;
        }

        if (!suppressSceneLoad && SceneLoader.Instance != null)
            SceneLoader.Instance.LoadExploration();
    }
}

} // namespace CardBattle
