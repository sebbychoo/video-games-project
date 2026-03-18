using UnityEngine;

/// <summary>
/// Handles player shooting. Attach to the Player GameObject.
/// Assign projectilePrefab and firePoint in the Inspector.
/// </summary>
public class Attack : MonoBehaviour
{
    public GameObject projectilePrefab;
    public Transform firePoint;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            ShootProjectile();
    }

    void ShootProjectile()
    {
        Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
    }
}
