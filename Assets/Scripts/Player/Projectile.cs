using UnityEngine;
using CardBattle;

/// <summary>
/// Moves the projectile forward and deals damage on hit.
/// Attach to your projectile prefab.
/// </summary>
public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 20;

    private void Update()
    {
        transform.Translate(Vector3.right * speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        Health targetHealth = other.GetComponent<Health>();
        if (targetHealth != null)
        {
            targetHealth.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
