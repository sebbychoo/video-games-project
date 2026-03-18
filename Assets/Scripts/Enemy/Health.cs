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
        Debug.Log($"{gameObject.name} took {amount} damage. HP: {currentHealth}");

        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} defeated.");

        if (!suppressSceneLoad && SceneLoader.Instance != null)
            SceneLoader.Instance.LoadExploration();

        Destroy(gameObject);
    }
}

} // namespace CardBattle
