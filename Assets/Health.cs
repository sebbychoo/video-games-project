using System.Runtime.CompilerServices;
using UnityEngine;

public class Health : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;
    private void Awake()
    {
        currentHealth = maxHealth; // start at full health
    }
    // call to deal damage
    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        Debug.Log(gameObject.name + "-" + amount + ". Current HP:" + currentHealth); // fix name issue

        if (currentHealth <= 0)
        {
            Die();
        }
    }
    private void Die()
    { 
        Debug.Log(gameObject.name + "Dead");
        //for now js destroy the character
        Destroy(gameObject);
    }
}