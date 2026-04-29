using UnityEngine;
using StarterAssets;

/// <summary>
/// Handles player ranged attacks in exploration.
/// Reads sprint input from StarterAssetsInputs to avoid legacy Input API.
/// Fires on Space via StarterAssetsInputs or falls back to legacy KeyCode.
/// </summary>
public class Attack : MonoBehaviour
{
    public GameObject projectilePrefab;
    public Transform firePoint;

    private StarterAssetsInputs _input;

    private void Awake()
    {
        _input = GetComponent<StarterAssetsInputs>();
    }

    private void Update()
    {
        // StarterAssetsInputs doesn't expose a fire action by default,
        // so we use a thin Input.GetKeyDown fallback here only for Space.
        // Swap this out if a custom Fire action is added to the input asset.
        if (Input.GetKeyDown(KeyCode.Space))
            ShootProjectile();
    }

    private void ShootProjectile()
    {
        if (projectilePrefab == null || firePoint == null) return;
        Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
    }
}
