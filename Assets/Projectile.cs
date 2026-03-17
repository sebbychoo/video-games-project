using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 20;

    private void Update()
    {
        transform.Translate(Vector3.right * speed * Time.deltaTime); // moves the projectile to the right.
    }

    private void OnTriggerEnter(Collider other)
    {
        Health targetHealth = other.GetComponent<Health>();
        if (targetHealth != null )
        {
            targetHealth.TakeDamage(damage);
            Destroy(gameObject); // destroys the projectile once it hits a target
        }
    }
}
