using UnityEngine;

public class Attack : MonoBehaviour
{
    public GameObject projectilePrefab;
    public Transform firePoint; // where the projectile spawns

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ShootProjectile();
        }
    }
    void ShootProjectile()
    {
        Instantiate(projectilePrefab, firePoint.position,firePoint.rotation); // Instantiate = create a new copy of a GameObject in the scene. Calling Instantiate duplicates a prefab and places it at a specific position and rotation. Useful for projectiles.
    }
}
    
    

